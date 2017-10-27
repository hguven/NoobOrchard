﻿using Nito.AsyncEx;
using Orchard.Caching;
using Orchard.Logging;
using Orchard.Serializer;
using Orchard.Threading.Tasks;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orchard.Redis.Caching
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RedisCacheClient : ICacheClient, IHaveSerializer {
        private readonly RedisCacheClientOptions _options;
        private readonly ILogger _logger;
        private readonly string prefixKey = "Orchard:Cache:";
        private readonly AsyncLock _lock = new AsyncLock();
        private bool _scriptsLoaded;
        private LoadedLuaScript _setIfHigherScript;
        private LoadedLuaScript _setIfLowerScript;
        private LoadedLuaScript _incrByAndExpireScript;
        private LoadedLuaScript _delByWildcardScript;
        public RedisCacheClient(RedisCacheClientOptions options) {
            options.ConnectionMultiplexer.ConnectionRestored += ConnectionMultiplexerOnConnectionRestored;
            options.Serializer = options.Serializer ?? new JsonNetSerializer();
            _options = options;
            if (options.LoggerFactory == null)
            {
                options.LoggerFactory = new NullLoggerFactory();
            }
            _logger = options.LoggerFactory.GetLogger(typeof(RedisCacheClient));
            if (!string.IsNullOrEmpty(options.PrefixKey))
            {
                prefixKey = options.PrefixKey;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public async Task<int> RemoveAllAsync(IEnumerable<string> keys = null) {
            if (keys == null) {
                var endpoints = _options.ConnectionMultiplexer.GetEndPoints(true);
                if (endpoints.Length == 0)
                    return 0;
                foreach (var endpoint in endpoints) {
                    var server = _options.ConnectionMultiplexer.GetServer(endpoint);

                    try {
                        await server.FlushDatabaseAsync(Database.Database).AnyContext();
                        continue;
                    } catch (Exception) {}
                    try {
                        var redisKeys = server.Keys(Database.Database).ToArray();
                        if (redisKeys.Length > 0)
                            await Database.KeyDeleteAsync(redisKeys).AnyContext();
                    } catch (Exception) {}
                }
            } else {
                var redisKeys = keys.Where(k => !String.IsNullOrEmpty(k)).Select(k => (RedisKey)(GetLocalizedKey(k))).ToArray();
                if (redisKeys.Length > 0)
                    return (int)await Database.KeyDeleteAsync(redisKeys).AnyContext();
            }

            return 0;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public async Task<int> RemoveByPrefixAsync(string prefix) {
            await LoadScriptsAsync().AnyContext();

            try {
                var result = await Database.ScriptEvaluateAsync(_delByWildcardScript, new { keys =GetLocalizedKey(prefix) + "*" }).AnyContext();
                return (int)result;
            } catch (RedisServerException) {
                return 0;
            }
        }

        private static readonly RedisValue _nullValue = "@@NULL";
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<CacheValue<T>> GetAsync<T>(string key) {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
            var redisValue = await Database.StringGetAsync(GetLocalizedKey(key)).AnyContext();
            return await RedisValueToCacheValueAsync<T>(redisValue).AnyContext();
        }

        private async Task<CacheValue<ICollection<T>>> RedisValuesToCacheValueAsync<T>(RedisValue[] redisValues) {
            var result = new List<T>();
            foreach (var redisValue in redisValues) {
                if (!redisValue.HasValue)
                    continue;
                if (redisValue == _nullValue)
                    continue;

                try {
                    var value = await redisValue.ToValueOfTypeAsync<T>(_options.Serializer).AnyContext();

                    result.Add(value);
                } catch (Exception ex) {
                    _logger.Error($"Unable to deserialize value \"{redisValue}\" to type { typeof(T).FullName}", ex);
                }
            }

            return new CacheValue<ICollection<T>>(result, true);
        }

        private async Task<CacheValue<T>> RedisValueToCacheValueAsync<T>(RedisValue redisValue) {
            if (!redisValue.HasValue) return CacheValue<T>.NoValue;
            if (redisValue == _nullValue) return CacheValue<T>.Null;

            try {
                var value = await redisValue.ToValueOfTypeAsync<T>(_options.Serializer).AnyContext();

                return new CacheValue<T>(value, true);
            } catch (Exception ex) {
                _logger.Error($"Unable to deserialize value \"{redisValue}\" to type {typeof(T).FullName}",ex);
                return CacheValue<T>.NoValue;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <returns></returns>
        public async Task<IDictionary<string, CacheValue<T>>> GetAllAsync<T>(IEnumerable<string> keys) {
            var keyArray = keys.ToArray();
            var values = await Database.StringGetAsync(keyArray.Select(k => (RedisKey)(GetLocalizedKey(k))).ToArray()).AnyContext();

            var result = new Dictionary<string, CacheValue<T>>();
            for (int i = 0; i < keyArray.Length; i++)
                result.Add(keyArray[i], await RedisValueToCacheValueAsync<T>(values[i]).AnyContext());

            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<CacheValue<ICollection<T>>> GetSetAsync<T>(string key) {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
            var set = await Database.SetMembersAsync(GetLocalizedKey(key)).AnyContext();
            return await RedisValuesToCacheValueAsync<T>(set).AnyContext();
        }

        public async Task<bool> AddAsync<T>(string key, T value, TimeSpan? expiresIn = null) {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

            if (expiresIn?.Ticks < 0) {
                _logger.Debug($"Removing expired key: {key}");
                await this.RemoveAsync(key).AnyContext();
                return false;
            }
            return await InternalSetAsync(key, value, expiresIn, When.NotExists).AnyContext();
        }

        public async Task<long> SetAddAsync<T>(string key, IEnumerable<T> values, TimeSpan? expiresIn = null) {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (expiresIn?.Ticks < 0) {
                _logger.Debug($"Removing expired key: {key}");

                await this.RemoveAsync(key).AnyContext();
                return default(long);
            }

            var redisValues = new List<RedisValue>();
            foreach (var value in values.Distinct())
                redisValues.Add(await value.ToRedisValueAsync(_options.Serializer).AnyContext());

            long result = await Database.SetAddAsync(GetLocalizedKey(key), redisValues.ToArray()).AnyContext();
            if (result > 0 && expiresIn.HasValue)
                await SetExpirationAsync(key, expiresIn.Value).AnyContext();

            return result;
        }

        public async Task<long> SetRemoveAsync<T>(string key, IEnumerable<T> values, TimeSpan? expiresIn = null) {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (expiresIn?.Ticks < 0) {
                _logger.Debug($"Removing expired key: {key}");

                await this.RemoveAsync(key).AnyContext();
                return default(long);
            }

            var redisValues = new List<RedisValue>();
            foreach (var value in values.Distinct())
                redisValues.Add(await value.ToRedisValueAsync(_options.Serializer).AnyContext());

            long result = await Database.SetRemoveAsync(GetLocalizedKey(key), redisValues.ToArray()).AnyContext();
            if (result > 0 && expiresIn.HasValue)
                await SetExpirationAsync(key, expiresIn.Value).AnyContext();

            return result;
        }

        public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiresIn = null) {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

            return InternalSetAsync(key, value, expiresIn);
        }

        public async Task<double> SetIfHigherAsync(string key, double value, TimeSpan? expiresIn = null) {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

            await LoadScriptsAsync().AnyContext();

            var result = await Database.ScriptEvaluateAsync(_setIfHigherScript, new { key=GetLocalizedKey(key), value, expires = expiresIn?.TotalSeconds }).AnyContext();
            return (double)result;
        }

        public async Task<double> SetIfLowerAsync(string key, double value, TimeSpan? expiresIn = null) {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

            await LoadScriptsAsync().AnyContext();

            var result = await Database.ScriptEvaluateAsync(_setIfLowerScript, new { key=GetLocalizedKey(key), value, expires = expiresIn?.TotalSeconds }).AnyContext();
            return (double)result;
        }

        private async Task<bool> InternalSetAsync<T>(string key, T value, TimeSpan? expiresIn = null, When when = When.Always, CommandFlags flags = CommandFlags.None) {
            var redisValue = await value.ToRedisValueAsync(_options.Serializer).AnyContext();
            return await Database.StringSetAsync( GetLocalizedKey(key), redisValue, expiresIn, when, flags).AnyContext();
        }

        public async Task<int> SetAllAsync<T>(IDictionary<string, T> values, TimeSpan? expiresIn = null) {
            if (values == null || values.Count == 0)
                return 0;

            var tasks = new List<Task>();
            foreach (var pair in values)
                tasks.Add(Database.StringSetAsync(GetLocalizedKey(pair.Key), await pair.Value.ToRedisValueAsync(_options.Serializer).AnyContext(), expiresIn));

            await Task.WhenAll(tasks).AnyContext();
            return values.Count;
        }

        public Task<bool> ReplaceAsync<T>(string key, T value, TimeSpan? expiresIn = null) {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
            return InternalSetAsync(key, value, expiresIn, When.Exists);
        }

        public async Task<double> IncrementAsync(string key, double amount = 1, TimeSpan? expiresIn = null) {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
            if (expiresIn?.Ticks < 0) {
                await this.RemoveAsync(key).AnyContext();
                return -1;
            }

            if (expiresIn.HasValue) {
                await LoadScriptsAsync().AnyContext();
                var result = await Database.ScriptEvaluateAsync(_incrByAndExpireScript, new {  key=GetLocalizedKey(key), value = amount, expires = expiresIn.Value.TotalSeconds }).AnyContext();
                return (long)result;
            }

            return await Database.StringIncrementAsync(GetLocalizedKey(key), amount).AnyContext();
        }

        public Task<bool> ExistsAsync(string key) {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
            return Database.KeyExistsAsync(GetLocalizedKey(key));
        }

        public Task<TimeSpan?> GetExpirationAsync(string key) {
            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
            }
            return Database.KeyTimeToLiveAsync(GetLocalizedKey(key));
        }

        public Task SetExpirationAsync(string key, TimeSpan expiresIn) {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
            if (expiresIn.Ticks < 0)
            {
                return this.RemoveAsync(key);
            }

            return Database.KeyExpireAsync(GetLocalizedKey(key), expiresIn);
        }

        private IDatabase Database => _options.ConnectionMultiplexer.GetDatabase();

        private async Task LoadScriptsAsync() {
            if (_scriptsLoaded)
                return;

            using (await _lock.LockAsync().AnyContext()) {
                if (_scriptsLoaded)
                    return;

                var setIfLower = LuaScript.Prepare(SET_IF_LOWER);
                var setIfHigher = LuaScript.Prepare(SET_IF_HIGHER);
                var incrByAndExpire = LuaScript.Prepare(INCRBY_AND_EXPIRE);
                var delByWildcard = LuaScript.Prepare(DEL_BY_WILDCARD);

                foreach (var endpoint in _options.ConnectionMultiplexer.GetEndPoints()) {
                    var server = _options.ConnectionMultiplexer.GetServer(endpoint);
                    _setIfHigherScript = await setIfHigher.LoadAsync(server).AnyContext();
                    _setIfLowerScript = await setIfLower.LoadAsync(server).AnyContext();
                    _incrByAndExpireScript = await incrByAndExpire.LoadAsync(server).AnyContext();
                    _delByWildcardScript = await delByWildcard.LoadAsync(server).AnyContext();
                }

                _scriptsLoaded = true;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private string GetLocalizedKey(string key)
        {
            return prefixKey+ key;
        }
        private void ConnectionMultiplexerOnConnectionRestored(object sender, ConnectionFailedEventArgs connectionFailedEventArgs) {
            _logger.Info("Redis connection restored.");
            _scriptsLoaded = false;
        }

        public void Dispose() {
            _options.ConnectionMultiplexer.ConnectionRestored -= ConnectionMultiplexerOnConnectionRestored;
        }

        ISerializer IHaveSerializer.Serializer => _options.Serializer;

        private const string SET_IF_HIGHER = @"local c = tonumber(redis.call('get', @key))
if c then
  if tonumber(@value) > c then
    redis.call('set', @key, @value)
    if (@expires) then
      redis.call('expire', @key, math.ceil(@expires))
    end
    return tonumber(@value) - c
  else
    return 0
  end
else
  redis.call('set', @key, @value)
  if (@expires) then
    redis.call('expire', @key, math.ceil(@expires))
  end
  return tonumber(@value)
end";

        private const string SET_IF_LOWER = @"local c = tonumber(redis.call('get', @key))
if c then
  if tonumber(@value) > c then
    redis.call('set', @key, @value)
    if (@expires) then
      redis.call('expire', @key, math.ceil(@expires))
    end
    return tonumber(@value) - c
  else
    return 0
  end
else
  redis.call('set', @key, @value)
  if (@expires) then
    redis.call('expire', @key, math.ceil(@expires))
  end
  return tonumber(@value)
end";

        private const string INCRBY_AND_EXPIRE = @"if math.modf(@value) == 0 then
  local v = redis.call('incrby', @key, @value)
  if (@expires) then
    redis.call('expire', @key, math.ceil(@expires))
  end
  return tonumber(v)
else
  local v = redis.call('incrbyfloat', @key, @value)
  if (@expires) then
    redis.call('expire', @key, math.ceil(@expires))
  end
  return tonumber(v)
end";

        private const string DEL_BY_WILDCARD = @"local remove = redis.call('keys', @keys)
if unpack(remove) ~= nil then
  return redis.call('del', unpack(remove))
else
  return 0
end";


    }



}
