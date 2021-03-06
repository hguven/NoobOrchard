﻿using Nito.AsyncEx;
using Orchard.Caching;
using Orchard.Queues;
using Orchard.Redis.Caching;
using Orchard.Threading.Locking;
using Orchard.Threading.Tasks;
using Orchard.Utility;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#pragma warning disable 4014

namespace Orchard.Redis.Queues
{
    public class RedisQueue<T> : QueueBase<T, RedisQueueOptions<T>> where T : class
    {
        private const string prefixKey = "Orchard:Queue:";
        private readonly AsyncLock _lock = new AsyncLock();
        private readonly AsyncAutoResetEvent _autoResetEvent = new AsyncAutoResetEvent();
        private readonly ISubscriber _subscriber;
        private readonly RedisCacheClient _cache;
        private long _enqueuedCount;
        private long _dequeuedCount;
        private long _completedCount;
        private long _abandonedCount;
        private long _workerErrorCount;
        private long _workItemTimeoutCount;
        private readonly ILockProvider _maintenanceLockProvider;
        private Task _maintenanceTask;
        private bool _isSubscribed;
        private readonly TimeSpan _payloadTimeToLive;
        public RedisQueue(RedisQueueOptions<T> options) : base(options)
        {
            if (options.ConnectionMultiplexer == null)
                throw new ArgumentException("ConnectionMultiplexer is required.");

            options.ConnectionMultiplexer.ConnectionRestored += ConnectionMultiplexerOnConnectionRestored;
            _cache = new RedisCacheClient(new RedisCacheClientOptions {
                ConnectionMultiplexer = options.ConnectionMultiplexer, Serializer = _serializer,PrefixKey= prefixKey
            });

            QueueListName = "q:" + _options.Name + ":in";
            WorkListName = "q:" + _options.Name + ":work";
            WaitListName = "q:" + _options.Name + ":wait";
            DeadListName = "q:" + _options.Name + ":dead";

            _payloadTimeToLive = GetPayloadTtl();
            _subscriber = _options.ConnectionMultiplexer.GetSubscriber();

            // min is 1 second, max is 1 minute
            var interval = _options.WorkItemTimeout > TimeSpan.FromSeconds(1) ? _options.WorkItemTimeout.Min(TimeSpan.FromMinutes(1)) : TimeSpan.FromSeconds(1);
            _maintenanceLockProvider = new ThrottlingLockProvider(_cache, 1, interval);

            _logger.DebugFormat("Queue {0} created. Retries: {1} Retry Delay: {2}", QueueId, _options.Retries, _options.RetryDelay.ToString());
        }

        protected override Task EnsureQueueCreatedAsync(CancellationToken cancellationToken = new CancellationToken()) => Task.CompletedTask;

        private async Task EnsureMaintenanceRunningAsync()
        {
            if (!_options.RunMaintenanceTasks || _maintenanceTask != null)
                return;

            using (await _lock.LockAsync().AnyContext())
            {
                if (_maintenanceTask != null)
                    return;

                _logger.Debug($"Starting maintenance for {_options.Name}.");
                _maintenanceTask = Task.Run(() => DoMaintenanceWorkLoopAsync(_queueDisposedCancellationTokenSource.Token));
            }
        }

        private async Task EnsureTopicSubscriptionAsync()
        {
            if (_isSubscribed)
                return;

            using (await _lock.LockAsync().AnyContext())
            {
                if (_isSubscribed)
                    return;

                _logger.Debug($"Subscribing to enqueue messages for {_options.Name}.");
                await _subscriber.SubscribeAsync(GetTopicName(), OnTopicMessage).AnyContext();
                _isSubscribed = true;
                _logger.Debug($"Subscribed to enqueue messages for {_options.Name}.");
            }
        }

        protected override async Task<QueueStats> GetQueueStatsImplAsync()
        {
            return new QueueStats
            {
                Queued = await Database.ListLengthAsync(GetLocalizedKey(QueueListName)).AnyContext() + await Database.ListLengthAsync(GetLocalizedKey(WaitListName)).AnyContext(),
                Working = await Database.ListLengthAsync(GetLocalizedKey(WorkListName)).AnyContext(),
                Deadletter = await Database.ListLengthAsync(GetLocalizedKey(DeadListName)).AnyContext(),
                Enqueued = _enqueuedCount,
                Dequeued = _dequeuedCount,
                Completed = _completedCount,
                Abandoned = _abandonedCount,
                Errors = _workerErrorCount,
                Timeouts = _workItemTimeoutCount
            };
        }

        private string QueueListName { get; set; }
        private string WorkListName { get; set; }
        private string WaitListName { get; set; }
        private string DeadListName { get; set; }

        private IDatabase Database => _options.ConnectionMultiplexer.GetDatabase();

        private string GetPayloadKey(string id)
        {
            return String.Concat("q:", _options.Name, ":", id);
        }

        private TimeSpan GetPayloadTtl()
        {
            var ttl = TimeSpan.Zero;
            for (int attempt = 1; attempt <= _options.Retries + 1; attempt++)
                ttl = ttl.Add(GetRetryDelay(attempt));

            // minimum of 7 days for payload
            return TimeSpan.FromMilliseconds(Math.Max(ttl.TotalMilliseconds * 1.5, TimeSpan.FromDays(7).TotalMilliseconds));
        }

        private string GetAttemptsKey(string id)
        {
            return String.Concat("q:", _options.Name, ":", id, ":attempts");
        }

        private TimeSpan GetAttemptsTtl()
        {
            return _payloadTimeToLive;
        }

        private string GetEnqueuedTimeKey(string id)
        {
            return String.Concat("q:", _options.Name, ":", id, ":enqueued");
        }

        private string GetDequeuedTimeKey(string id)
        {
            return String.Concat("q:", _options.Name, ":", id, ":dequeued");
        }

        private string GetRenewedTimeKey(string id)
        {
            return String.Concat("q:", _options.Name, ":", id, ":renewed");
        }

        private TimeSpan GetWorkItemTimeoutTimeTtl()
        {
            return TimeSpan.FromMilliseconds(Math.Max(_options.WorkItemTimeout.TotalMilliseconds * 1.5, TimeSpan.FromHours(1).TotalMilliseconds));
        }

        private string GetWaitTimeKey(string id)
        {
            return  String.Concat("q:", _options.Name, ":", id, ":wait");
        }

        private TimeSpan GetWaitTimeTtl()
        {
            return _payloadTimeToLive;
        }

        private string GetTopicName()
        {
            return String.Concat("q:", _options.Name, ":in");
        }

        protected override async Task<string> EnqueueImplAsync(T data)
        {
            string id = Guid.NewGuid().ToString("N");
            _logger.Debug($"Queue {_options.Name} enqueue item: {id}");

            if (!await OnEnqueuingAsync(data).AnyContext())
            {
                _logger.Debug($"Aborting enqueue item: {id}");
                return null;
            }

            var now = SystemClock.UtcNow;
            bool success = await Run.WithRetriesAsync(() => _cache.AddAsync(GetPayloadKey(id), data, _payloadTimeToLive), logger: _logger).AnyContext();
            if (!success)
                throw new InvalidOperationException("Attempt to set payload failed.");

            await Run.WithRetriesAsync(() => _cache.SetAsync(GetEnqueuedTimeKey(id), now.Ticks, _payloadTimeToLive), logger: _logger).AnyContext();
            await Run.WithRetriesAsync(() => Database.ListLeftPushAsync(GetLocalizedKey(QueueListName), id), logger: _logger).AnyContext();

            try
            {
                _autoResetEvent.Set();
                await Run.WithRetriesAsync(() => _subscriber.PublishAsync(GetTopicName(), id), logger: _logger).AnyContext();
            }
            catch { }

            Interlocked.Increment(ref _enqueuedCount);
            var entry = new QueueEntry<T>(id, data, this, now, 0);
            await OnEnqueuedAsync(entry).AnyContext();

            _logger.Debug("Enqueue done");
            return id;
        }

        protected override void StartWorkingImpl(Func<IQueueEntry<T>, CancellationToken, Task> handler, bool autoComplete, CancellationToken cancellationToken)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var linkedCancellationToken = GetLinkedDisposableCanncellationToken(cancellationToken);

            Task.Run(async () =>
            {
                _logger.Debug($"WorkerLoop Start {_options.Name}");

                while (!linkedCancellationToken.IsCancellationRequested)
                {
                    _logger.Debug($"WorkerLoop Signaled {_options.Name}");

                    IQueueEntry<T> queueEntry = null;
                    try
                    {
                        queueEntry = await DequeueImplAsync(linkedCancellationToken).AnyContext();
                    }
                    catch (TimeoutException) { }

                    if (linkedCancellationToken.IsCancellationRequested || queueEntry == null)
                        continue;

                    try
                    {
                        await handler(queueEntry, linkedCancellationToken).AnyContext();
                        if (autoComplete && !queueEntry.IsAbandoned && !queueEntry.IsCompleted)
                            await queueEntry.CompleteAsync().AnyContext();
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref _workerErrorCount);
                        _logger.ErrorFormat(ex, "Worker error: {0}", ex.Message);

                        if (!queueEntry.IsAbandoned && !queueEntry.IsCompleted)
                            await queueEntry.AbandonAsync().AnyContext();
                    }
                }

                _logger.DebugFormat("Worker exiting: {0} Cancel Requested: {1}", _options.Name, linkedCancellationToken.IsCancellationRequested);
            }, linkedCancellationToken);
        }

        protected override async Task<IQueueEntry<T>> DequeueImplAsync(CancellationToken linkedCancellationToken)
        {
            _logger.Debug($"Queue {_options.Name} dequeuing item...");
            long now = SystemClock.UtcNow.Ticks;

            await EnsureMaintenanceRunningAsync().AnyContext();
            await EnsureTopicSubscriptionAsync().AnyContext();

            var value = await DequeueIdAsync(linkedCancellationToken).AnyContext();
            if (linkedCancellationToken.IsCancellationRequested && value.IsNullOrEmpty)
                return null;

            _logger.DebugFormat("Initial list value: {0}", value.IsNullOrEmpty ? "<null>" : value.ToString());

            while (value.IsNullOrEmpty && !linkedCancellationToken.IsCancellationRequested)
            {
                _logger.Debug("Waiting to dequeue item...");
                var sw = Stopwatch.StartNew();

                try
                {
                    await _autoResetEvent.WaitAsync(GetDequeueCanncellationToken(linkedCancellationToken)).AnyContext();
                }
                catch (OperationCanceledException) { }

                sw.Stop();
                _logger.DebugFormat("Waited for dequeue: {0}", sw.Elapsed.ToString());

                value = await DequeueIdAsync(linkedCancellationToken).AnyContext();
                _logger.DebugFormat("List value: {0}", value.IsNullOrEmpty ? "<null>" : value.ToString());
            }

            if (value.IsNullOrEmpty)
                return null;

            await Run.WithRetriesAsync(() => _cache.SetAsync(GetDequeuedTimeKey(value), now, GetWorkItemTimeoutTimeTtl()), logger: _logger).AnyContext();
            await Run.WithRetriesAsync(() => _cache.SetAsync(GetRenewedTimeKey(value), now, GetWorkItemTimeoutTimeTtl()), logger: _logger).AnyContext();

            try
            {
                var entry = await GetQueueEntryAsync(value).AnyContext();
                if (entry == null)
                    return null;

                Interlocked.Increment(ref _dequeuedCount);
                await OnDequeuedAsync(entry).AnyContext();

                _logger.DebugFormat("Dequeued item: {0}", value);
                return entry;
            }
            catch (Exception ex)
            {
                _logger.ErrorFormat(ex, "Error getting dequeued item payload: {0}", value);
                throw;
            }
        }

        public override async Task RenewLockAsync(IQueueEntry<T> entry)
        {
            _logger.DebugFormat("Queue {0} renew lock item: {1}", _options.Name, entry.Id);
            await Run.WithRetriesAsync(() => _cache.SetAsync(GetRenewedTimeKey(entry.Id), SystemClock.UtcNow.Ticks, GetWorkItemTimeoutTimeTtl()), logger: _logger).AnyContext();
            await OnLockRenewedAsync(entry).AnyContext();
            _logger.DebugFormat("Renew lock done: {0}", entry.Id);
        }

        private async Task<QueueEntry<T>> GetQueueEntryAsync(string workId)
        {
            var payload = await Run.WithRetriesAsync(() => _cache.GetAsync<T>(GetPayloadKey(workId)), logger: _logger).AnyContext();
            if (payload.IsNull)
            {
                _logger.ErrorFormat("Error getting queue payload: {0}", workId);
                await Database.ListRemoveAsync(GetLocalizedKey(WorkListName), workId).AnyContext();
                return null;
            }

            long enqueuedTimeTicks = await Run.WithRetriesAsync(() => _cache.GetAsync<long>(GetEnqueuedTimeKey(workId), 0), logger: _logger).AnyContext();
            int attemptsValue = await Run.WithRetriesAsync(() => _cache.GetAsync<int>(GetAttemptsKey(workId), 1), logger: _logger).AnyContext();

            return new QueueEntry<T>(workId, payload.Value, this, new DateTime(enqueuedTimeTicks, DateTimeKind.Utc), attemptsValue);
        }

        private async Task<RedisValue> DequeueIdAsync(CancellationToken linkedCancellationToken)
        {
            try
            {
                return await Run.WithRetriesAsync(() => Database.ListRightPopLeftPushAsync(GetLocalizedKey(QueueListName), GetLocalizedKey(WorkListName)), 3, TimeSpan.FromMilliseconds(100), linkedCancellationToken, _logger).AnyContext();
            }
            catch (Exception)
            {
                return RedisValue.Null;
            }
        }

        public override async Task CompleteAsync(IQueueEntry<T> entry)
        {
            _logger.DebugFormat("Queue {0} complete item: {1}", _options.Name, entry.Id);
            if (entry.IsAbandoned || entry.IsCompleted)
                throw new InvalidOperationException("Queue entry has already been completed or abandoned.");

            long result = await Run.WithRetriesAsync(() => Database.ListRemoveAsync(GetLocalizedKey(WorkListName), entry.Id), logger: _logger).AnyContext();
            if (result == 0)
                throw new InvalidOperationException("Queue entry not in work list, it may have been auto abandoned.");

            await Run.WithRetriesAsync(() => Task.WhenAll(
                Database.KeyDeleteAsync(GetLocalizedKey(GetPayloadKey(entry.Id))),
                Database.KeyDeleteAsync(GetLocalizedKey(GetAttemptsKey(entry.Id))),
                Database.KeyDeleteAsync(GetLocalizedKey(GetEnqueuedTimeKey(entry.Id))),
                Database.KeyDeleteAsync(GetLocalizedKey(GetDequeuedTimeKey(entry.Id))),
                Database.KeyDeleteAsync(GetLocalizedKey(GetRenewedTimeKey(entry.Id))),
                Database.KeyDeleteAsync(GetLocalizedKey(GetWaitTimeKey(entry.Id)))
            ), logger: _logger).AnyContext();

            Interlocked.Increment(ref _completedCount);
            entry.MarkCompleted();
            await OnCompletedAsync(entry).AnyContext();
            _logger.DebugFormat("Complete done: {0}", entry.Id);
        }

        public override async Task AbandonAsync(IQueueEntry<T> entry)
        {
            _logger.Debug($"Queue {_options.Name}:{QueueId} abandon item: {entry.Id}");
            if (entry.IsAbandoned || entry.IsCompleted)
                throw new InvalidOperationException("Queue entry has already been completed or abandoned.");

            var attemptsCachedValue = await Run.WithRetriesAsync(() => _cache.GetAsync<int>(GetAttemptsKey(entry.Id)), logger: _logger).AnyContext();
            int attempts = 1;
            if (attemptsCachedValue.HasValue)
                attempts = attemptsCachedValue.Value + 1;

            var retryDelay = GetRetryDelay(attempts);
            _logger.Debug($"Item: { entry.Id} Retry attempts: {attempts} delay: {retryDelay} allowed: {_options.Retries}");

            if (attempts > _options.Retries)
            {
                _logger.Debug($"Exceeded retry limit moving to deadletter: {entry.Id}");

                var tx = Database.CreateTransaction();
                tx.AddCondition(Condition.KeyExists(GetLocalizedKey(GetRenewedTimeKey(entry.Id))));
                tx.ListRemoveAsync(GetLocalizedKey(WorkListName), entry.Id);
                tx.ListLeftPushAsync(GetLocalizedKey(DeadListName), entry.Id);
                tx.KeyDeleteAsync(GetLocalizedKey(GetRenewedTimeKey(entry.Id)));
                tx.KeyExpireAsync(GetLocalizedKey(GetPayloadKey(entry.Id)), _options.DeadLetterTimeToLive);
                bool success = await Run.WithRetriesAsync(() => tx.ExecuteAsync(), logger: _logger).AnyContext();
                if (!success)
                    throw new InvalidOperationException($"Queue entry not in work list, it may have been auto abandoned.");

                await Run.WithRetriesAsync(() => _cache.IncrementAsync(GetAttemptsKey(entry.Id), 1, GetAttemptsTtl()), logger: _logger).AnyContext();
                await Run.WithRetriesAsync(() => Database.KeyDeleteAsync(GetLocalizedKey(GetDequeuedTimeKey(entry.Id))), logger: _logger).AnyContext();
                await Run.WithRetriesAsync(() => Database.KeyDeleteAsync(GetLocalizedKey(GetWaitTimeKey(entry.Id))), logger: _logger).AnyContext();
            }
            else if (retryDelay > TimeSpan.Zero)
            {
                _logger.Debug($"Adding item to wait list for future retry: { entry.Id}");

                await Run.WithRetriesAsync(() => _cache.SetAsync(GetWaitTimeKey(entry.Id), SystemClock.UtcNow.Add(retryDelay).Ticks, GetWaitTimeTtl()), logger: _logger).AnyContext();
                await Run.WithRetriesAsync(() => _cache.IncrementAsync(GetAttemptsKey(entry.Id), 1, GetAttemptsTtl()), logger: _logger).AnyContext();

                var tx = Database.CreateTransaction();
                tx.AddCondition(Condition.KeyExists(GetLocalizedKey(GetRenewedTimeKey(entry.Id))));
                tx.ListRemoveAsync(GetLocalizedKey(WorkListName), entry.Id);
                tx.ListLeftPushAsync(GetLocalizedKey(WaitListName), entry.Id);
                tx.KeyDeleteAsync(GetLocalizedKey(GetRenewedTimeKey(entry.Id)));
                bool success = await Run.WithRetriesAsync(() => tx.ExecuteAsync()).AnyContext();
                if (!success)
                    throw new InvalidOperationException($"Queue entry not in work list, it may have been auto abandoned.");

                await Run.WithRetriesAsync(() => Database.KeyDeleteAsync(GetLocalizedKey(GetDequeuedTimeKey(entry.Id))), logger: _logger).AnyContext();
            }
            else
            {
                _logger.Debug($"Adding item back to queue for retry: { entry.Id}");

                await Run.WithRetriesAsync(() => _cache.IncrementAsync(GetAttemptsKey(entry.Id), 1, GetAttemptsTtl()), logger: _logger).AnyContext();

                var tx = Database.CreateTransaction();
                tx.AddCondition(Condition.KeyExists(GetLocalizedKey(GetRenewedTimeKey(entry.Id))));
                tx.ListRemoveAsync(GetLocalizedKey(WorkListName), entry.Id);
                tx.ListLeftPushAsync(GetLocalizedKey(QueueListName), entry.Id);
                tx.KeyDeleteAsync(GetLocalizedKey(GetRenewedTimeKey(entry.Id)));
                bool success = await Run.WithRetriesAsync(() => tx.ExecuteAsync(), logger: _logger).AnyContext();
                if (!success)
                    throw new InvalidOperationException($"Queue entry not in work list, it may have been auto abandoned.");

                // This should pulse the monitor.
                await _subscriber.PublishAsync(GetTopicName(), entry.Id).AnyContext();
                await Run.WithRetriesAsync(() => Database.KeyDeleteAsync(GetLocalizedKey(GetDequeuedTimeKey(entry.Id))), logger: _logger).AnyContext();
            }

            Interlocked.Increment(ref _abandonedCount);
            entry.MarkAbandoned();
            await OnAbandonedAsync(entry).AnyContext();
            _logger.Debug($"Abandon complete: { entry.Id}");
        }

        private TimeSpan GetRetryDelay(int attempts)
        {
            if (_options.RetryDelay <= TimeSpan.Zero)
                return TimeSpan.Zero;

            int maxMultiplier = _options.RetryMultipliers.Length > 0 ? _options.RetryMultipliers.Last() : 1;
            int multiplier = attempts <= _options.RetryMultipliers.Length ? _options.RetryMultipliers[attempts - 1] : maxMultiplier;
            return TimeSpan.FromMilliseconds(_options.RetryDelay.TotalMilliseconds * multiplier);
        }

        protected override Task<IEnumerable<T>> GetDeadletterItemsImplAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override async Task DeleteQueueAsync()
        {
            _logger.Debug($"Deleting queue: {_options.Name}");
            await DeleteListAsync(GetLocalizedKey(QueueListName)).AnyContext();
            await DeleteListAsync(GetLocalizedKey(WorkListName)).AnyContext();
            await DeleteListAsync(GetLocalizedKey(WaitListName)).AnyContext();
            await DeleteListAsync(GetLocalizedKey(DeadListName)).AnyContext();
            _enqueuedCount = 0;
            _dequeuedCount = 0;
            _completedCount = 0;
            _abandonedCount = 0;
            _workerErrorCount = 0;
        }

        private async Task DeleteListAsync(string name)
        {
            var itemIds = await Database.ListRangeAsync(name).AnyContext();
            foreach (var id in itemIds)
            {
                await Task.WhenAll(
                    Database.KeyDeleteAsync(GetLocalizedKey(GetPayloadKey(id))),
                    Database.KeyDeleteAsync(GetLocalizedKey(GetAttemptsKey(id))),
                    Database.KeyDeleteAsync(GetLocalizedKey(GetEnqueuedTimeKey(id))),
                    Database.KeyDeleteAsync(GetLocalizedKey(GetDequeuedTimeKey(id))),
                    Database.KeyDeleteAsync(GetLocalizedKey(GetRenewedTimeKey(id))),
                    Database.KeyDeleteAsync(GetLocalizedKey(GetWaitTimeKey(id)))
                ).AnyContext();
            }

            await Database.KeyDeleteAsync(GetLocalizedKey(name)).AnyContext();
        }

        private async Task TrimDeadletterItemsAsync(int maxItems)
        {
            var itemIds = (await Database.ListRangeAsync(DeadListName).AnyContext()).Skip(maxItems);
            foreach (var id in itemIds)
            {
                await Task.WhenAll(
                    Database.KeyDeleteAsync(GetLocalizedKey(GetPayloadKey(id))),
                    Database.KeyDeleteAsync(GetLocalizedKey(GetAttemptsKey(id))),
                    Database.KeyDeleteAsync(GetLocalizedKey(GetEnqueuedTimeKey(id))),
                    Database.KeyDeleteAsync(GetLocalizedKey(GetDequeuedTimeKey(id))),
                    Database.KeyDeleteAsync(GetLocalizedKey(GetRenewedTimeKey(id))),
                    Database.KeyDeleteAsync(GetLocalizedKey(GetWaitTimeKey(id))),
                    Database.ListRemoveAsync(GetLocalizedKey(QueueListName), id),
                    Database.ListRemoveAsync(GetLocalizedKey(WorkListName), id),
                    Database.ListRemoveAsync(GetLocalizedKey(WaitListName), id),
                    Database.ListRemoveAsync(GetLocalizedKey(DeadListName), id)
                ).AnyContext();
            }
        }

        private void OnTopicMessage(RedisChannel redisChannel, RedisValue redisValue)
        {
            _logger.DebugFormat("Queue OnMessage {0}: {1}", _options.Name, redisValue);
            _autoResetEvent.Set();
        }

        private void ConnectionMultiplexerOnConnectionRestored(object sender, ConnectionFailedEventArgs connectionFailedEventArgs)
        {
            _logger.Info("Redis connection restored.");
            _autoResetEvent.Set();
        }

        public async Task DoMaintenanceWorkAsync()
        {
            _logger.DebugFormat("DoMaintenance: Name={0} Id={1}", _options.Name, QueueId);
            var utcNow = SystemClock.UtcNow;

            try
            {
                var workIds = await Database.ListRangeAsync(GetLocalizedKey(WorkListName)).AnyContext();
                foreach (var workId in workIds)
                {
                    var renewedTimeTicks = await _cache.GetAsync<long>(GetRenewedTimeKey(workId)).AnyContext();
                    if (!renewedTimeTicks.HasValue)
                        continue;

                    var renewedTime = new DateTime(renewedTimeTicks.Value);
                    _logger.Debug($"Renewed time {renewedTime:o}");

                    if (utcNow.Subtract(renewedTime) <= _options.WorkItemTimeout)
                        continue;

                    _logger.Info($"Auto abandon item {workId}: renewed: {renewedTime:o} current: {utcNow:o} timeout: {_options.WorkItemTimeout}");

                    var entry = await GetQueueEntryAsync(workId).AnyContext();
                    if (entry == null)
                        continue;

                    await AbandonAsync(entry).AnyContext();
                    Interlocked.Increment(ref _workItemTimeoutCount);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorFormat(ex, "Error checking for work item timeouts: {0}", ex.Message);
            }

            try
            {
                var waitIds = await Database.ListRangeAsync(WaitListName).AnyContext();
                foreach (var waitId in waitIds)
                {
                    var waitTimeTicks = await _cache.GetAsync<long>(GetWaitTimeKey(waitId)).AnyContext();

                    _logger.DebugFormat("Wait time: {0}", waitTimeTicks);

                    if (waitTimeTicks.HasValue && waitTimeTicks.Value > utcNow.Ticks)
                        continue;

                    _logger.Debug("Getting retry lock");
                    _logger.DebugFormat("Adding item back to queue for retry: {0}", waitId);

                    var tx = Database.CreateTransaction();
                    tx.ListRemoveAsync(GetLocalizedKey(WaitListName), waitId);
                    tx.ListLeftPushAsync(GetLocalizedKey(QueueListName), waitId);
                    bool success = await Run.WithRetriesAsync(() => tx.ExecuteAsync(), logger: _logger).AnyContext();
                    if (!success)
                        throw new Exception("Unable to move item to queue list.");

                    await Run.WithRetriesAsync(() => Database.KeyDeleteAsync(GetLocalizedKey(GetWaitTimeKey(waitId))), logger: _logger).AnyContext();
                    await _subscriber.PublishAsync(GetTopicName(), waitId).AnyContext();
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorFormat(ex, "Error adding items back to the queue after the retry delay: {0}", ex.Message);
            }

            try
            {
                await TrimDeadletterItemsAsync(_options.DeadLetterMaxItems).AnyContext();
            }
            catch (Exception ex)
            {
                _logger.ErrorFormat(ex, "Error trimming deadletter items: {0}", ex.Message);
            }
        }

        private async Task DoMaintenanceWorkLoopAsync(CancellationToken disposedCancellationToken)
        {
            while (!disposedCancellationToken.IsCancellationRequested)
            {
                _logger.DebugFormat("Requesting Maintenance Lock: Name={0} Id={1}", _options.Name, QueueId);
                bool gotLock = await _maintenanceLockProvider.TryUsingAsync(_options.Name + "-maintenance", DoMaintenanceWorkAsync, acquireTimeout: TimeSpan.FromSeconds(30)).AnyContext();
                _logger.DebugFormat("{0} Maintenance Lock: Name={1} Id={2}", gotLock ? "Acquired" : "Failed to acquire", _options.Name, QueueId);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _options.ConnectionMultiplexer.ConnectionRestored -= ConnectionMultiplexerOnConnectionRestored;

            if (_isSubscribed)
            {
                lock (_lock.Lock())
                {
                    if (_isSubscribed)
                    {
                        _logger.DebugFormat("Unsubscribing from topic {0}", GetTopicName());
                        _subscriber.Unsubscribe(GetTopicName(), OnTopicMessage, CommandFlags.FireAndForget);
                        _isSubscribed = false;
                        _logger.DebugFormat("Unsubscribed from topic {0}", GetTopicName());
                    }
                }
            }

            _cache.Dispose();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private string GetLocalizedKey(string key)
        {
            return prefixKey + key;
        }
    }
}