﻿using BenchmarkDotNet.Attributes;
using Orchard.Queues;
using Orchard.Redis.Queues;
using StackExchange.Redis;
using System;

namespace Orchard.Benchmarks.Queues
{
    public class QueueBenchmarks {
        private const int ITEM_COUNT = 1000;
        private readonly IQueue<QueueItem> _inMemoryQueue = new InMemoryQueue<QueueItem>(new InMemoryQueueOptions<QueueItem>());
        private readonly IQueue<QueueItem> _redisQueue = new RedisQueue<QueueItem>(new RedisQueueOptions<QueueItem> { ConnectionMultiplexer = ConnectionMultiplexer.Connect("localhost") });

        [IterationSetup]
        public void Setup() {
            _inMemoryQueue.DeleteQueueAsync().GetAwaiter().GetResult();
            _redisQueue.DeleteQueueAsync().GetAwaiter().GetResult();
        }

        [IterationSetup(Target = nameof(DequeueInMemoryQueue))]
        [Benchmark]
        public void EnqueueInMemoryQueue() {
            EnqueueQueue(_inMemoryQueue);
        }

        [Benchmark]
        public void DequeueInMemoryQueue() {
            DequeueQueue(_inMemoryQueue);
        }

        [IterationSetup(Target = nameof(DequeueRedisQueue))]
        [Benchmark]
        public void EnqueueRedisQueue() {
            EnqueueQueue(_redisQueue);
        }

        [Benchmark]
        public void DequeueRedisQueue() {
            DequeueQueue(_redisQueue);
        }

        private void EnqueueQueue(IQueue<QueueItem> queue) {
            try {
                for (int i = 0; i < ITEM_COUNT; i++)
                    queue.EnqueueAsync(new QueueItem { Id = i }).GetAwaiter().GetResult();
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        private void DequeueQueue(IQueue<QueueItem> queue) {
            try {
                for (int i = 0; i < ITEM_COUNT; i++) {
                    var entry = queue.DequeueAsync(TimeSpan.Zero).GetAwaiter().GetResult();
                    entry.CompleteAsync().GetAwaiter().GetResult();
                }
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }
    }
}
