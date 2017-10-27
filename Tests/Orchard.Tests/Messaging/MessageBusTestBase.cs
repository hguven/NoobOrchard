﻿using Nito.AsyncEx;
using NUnit.Framework;
using Orchard.Messaging;
using Orchard.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Orchard.Tests.Messaging
{
    public abstract class MessageBusTestBase : TestWithLoggingBase {
     

        protected virtual IMessageBus GetMessageBus() {
            return null;
        }

        protected virtual Task CleanupMessageBusAsync(IMessageBus messageBus) {
            messageBus?.Dispose();
            return Task.CompletedTask;
        }

        public virtual async Task CanSendMessageAsync() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            try {
                var resetEvent = new AsyncManualResetEvent(false);
                await messageBus.SubscribeAsync<SimpleMessageA>(msg => {
                    _logger.Debug("Got message");
                   Assert.AreEqual("Hello", msg.Data);
                    resetEvent.Set();
                    _logger.Debug("Set event");
                });

                await SystemClock.SleepAsync(100);
                await messageBus.PublishAsync(new SimpleMessageA {
                    Data = "Hello"
                });
                _logger.Debug("Published one...");

                await resetEvent.WaitAsync();
            } finally {
                await CleanupMessageBusAsync(messageBus);
            }
        }

        public virtual async Task CanHandleNullMessageAsync() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            try {
                var resetEvent = new AsyncManualResetEvent(false);
                await messageBus.SubscribeAsync<object>(msg => {
                    resetEvent.Set();
                    throw new Exception();
                });

                await SystemClock.SleepAsync(100);
                await messageBus.PublishAsync<object>(null);
                _logger.Debug("Published one...");

                Assert.ThrowsAsync<OperationCanceledException>(() => resetEvent.WaitAsync());
            } finally {
                await CleanupMessageBusAsync(messageBus);
            }
        }

        public virtual async Task CanSendDerivedMessageAsync() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            try {
                var resetEvent = new AsyncManualResetEvent(false);
                await messageBus.SubscribeAsync<SimpleMessageA>(msg => {
                    _logger.Debug("Got message");
                   Assert.AreEqual("Hello", msg.Data);
                    resetEvent.Set();
                    _logger.Debug("Set event");
                });

                await SystemClock.SleepAsync(100);
                await messageBus.PublishAsync(new DerivedSimpleMessageA {
                    Data = "Hello"
                });
                _logger.Debug("Published one...");

                await resetEvent.WaitAsync();
            } finally {
                await CleanupMessageBusAsync(messageBus);
            }
        }

        public virtual async Task CanSendDelayedMessageAsync() {
            const int numConcurrentMessages = 1000;
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            try {
                var countdown = new AsyncCountdownEvent(numConcurrentMessages);

                int messages = 0;
                await messageBus.SubscribeAsync<SimpleMessageA>(msg => {
                    if (++messages % 50 == 0)
                        _logger.Debug($"Totoal Processed {messages} messages");

                   Assert.AreEqual("Hello", msg.Data);
                    countdown.Signal();
                });

                var sw = Stopwatch.StartNew();
                await Run.InParallelAsync(numConcurrentMessages, async i => {
                    await messageBus.PublishAsync(new SimpleMessageA {
                        Data = "Hello",
                        Count = i
                    }, TimeSpan.FromMilliseconds(RandomData.GetInt(0, 100)));
                    if (i % 500 == 0)
                        _logger.Debug("Published 500 messages...");
                });

                await countdown.WaitAsync();
                sw.Stop();

                _logger.Debug($"Processed {numConcurrentMessages - countdown.CurrentCount} in {sw.ElapsedMilliseconds}ms");
               Assert.AreEqual(0, countdown.CurrentCount);
                XUnitAssert.InRange(sw.Elapsed.TotalMilliseconds, 50, 5000);
            } finally {
                await CleanupMessageBusAsync(messageBus);
            }
        }

        public virtual async Task CanSubscribeConcurrentlyAsync() {
            const int iterations = 100;
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            try {
                var countdown = new AsyncCountdownEvent(iterations * 10);
                await Run.InParallelAsync(10, i => {
                    return messageBus.SubscribeAsync<SimpleMessageA>(msg => {
                       Assert.AreEqual("Hello", msg.Data);
                        countdown.Signal();
                    });
                });

                await Run.InParallelAsync(iterations, i => messageBus.PublishAsync(new SimpleMessageA { Data = "Hello" }));
                await countdown.WaitAsync();
               Assert.AreEqual(0, countdown.CurrentCount);
            }
            finally {
                await CleanupMessageBusAsync(messageBus);
            }
        }

        public virtual async Task CanReceiveMessagesConcurrentlyAsync() {
            const int iterations = 100;
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            var messageBuses = new List<IMessageBus>(10);
            try {
                var countdown = new AsyncCountdownEvent(iterations * 10);
                await Run.InParallelAsync(10, async i => {
                    var bus = GetMessageBus();
                    await bus.SubscribeAsync<SimpleMessageA>(msg => {
                       Assert.AreEqual("Hello", msg.Data);
                        countdown.Signal();
                    });

                    messageBuses.Add(bus);
                });
                var subscribe = Run.InParallelAsync(iterations,
                    i => {
#pragma warning disable AsyncFixer02 // Long running or blocking operations under an async method
                        SystemClock.Sleep(RandomData.GetInt(0, 10));
#pragma warning restore AsyncFixer02 // Long running or blocking operations under an async method
                        return messageBuses.Random().SubscribeAsync<NeverPublishedMessage>(msg => Task.CompletedTask);
                    });

                var publish = Run.InParallelAsync(iterations + 3, i => {
                    switch (i) {
                        case 1:
                            return messageBus.PublishAsync(new DerivedSimpleMessageA { Data = "Hello" });
                        case 2:
                            return messageBus.PublishAsync(new Derived2SimpleMessageA { Data = "Hello" });
                        case 3:
                            return messageBus.PublishAsync(new Derived3SimpleMessageA { Data = "Hello" });
                        case 4:
                            return messageBus.PublishAsync(new Derived4SimpleMessageA { Data = "Hello" });
                        case 5:
                            return messageBus.PublishAsync(new Derived5SimpleMessageA { Data = "Hello" });
                        case 6:
                            return messageBus.PublishAsync(new Derived6SimpleMessageA { Data = "Hello" });
                        case 7:
                            return messageBus.PublishAsync(new Derived7SimpleMessageA { Data = "Hello" });
                        case 8:
                            return messageBus.PublishAsync(new Derived8SimpleMessageA { Data = "Hello" });
                        case 9:
                            return messageBus.PublishAsync(new Derived9SimpleMessageA { Data = "Hello" });
                        case 10:
                            return messageBus.PublishAsync(new Derived10SimpleMessageA { Data = "Hello" });
                        case iterations + 1:
                            return messageBus.PublishAsync(new { Data = "Hello" });
                        case iterations + 2:
                            return messageBus.PublishAsync(new SimpleMessageC { Data = "Hello" });
                        case iterations + 3:
                            return messageBus.PublishAsync(new SimpleMessageB { Data = "Hello" });
                        default:
                            return messageBus.PublishAsync(new SimpleMessageA { Data = "Hello" });
                    }
                });

                await Task.WhenAll(subscribe, publish);
                await countdown.WaitAsync();
               Assert.AreEqual(0, countdown.CurrentCount);
            } finally {
                foreach (var mb in messageBuses)
                    await CleanupMessageBusAsync(mb);

                await CleanupMessageBusAsync(messageBus);
            }
        }

        public virtual async Task CanSendMessageToMultipleSubscribersAsync() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            try {
                var countdown = new AsyncCountdownEvent(3);
                await messageBus.SubscribeAsync<SimpleMessageA>(msg => {
                   Assert.AreEqual("Hello", msg.Data);
                    countdown.Signal();
                });
                await messageBus.SubscribeAsync<SimpleMessageA>(msg => {
                   Assert.AreEqual("Hello", msg.Data);
                    countdown.Signal();
                });
                await messageBus.SubscribeAsync<SimpleMessageA>(msg => {
                   Assert.AreEqual("Hello", msg.Data);
                    countdown.Signal();
                });
                await messageBus.PublishAsync(new SimpleMessageA {
                    Data = "Hello"
                });

                await countdown.WaitAsync();
               Assert.AreEqual(0, countdown.CurrentCount);
            } finally {
                await CleanupMessageBusAsync(messageBus);
            }
        }

        public virtual async Task CanTolerateSubscriberFailureAsync() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            try {
                var countdown = new AsyncCountdownEvent(2);
                await messageBus.SubscribeAsync<SimpleMessageA>(msg => {
                    throw new Exception();
                });
                await messageBus.SubscribeAsync<SimpleMessageA>(msg => {
                   Assert.AreEqual("Hello", msg.Data);
                    countdown.Signal();
                });
                await messageBus.SubscribeAsync<SimpleMessageA>(msg => {
                   Assert.AreEqual("Hello", msg.Data);
                    countdown.Signal();
                });
                await messageBus.PublishAsync(new SimpleMessageA {
                    Data = "Hello"
                });

                await countdown.WaitAsync();
               Assert.AreEqual(0, countdown.CurrentCount);
            } finally {
                await CleanupMessageBusAsync(messageBus);
            }
        }

        public virtual async Task WillOnlyReceiveSubscribedMessageTypeAsync() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            try {
                var resetEvent = new AsyncManualResetEvent(false);
                await messageBus.SubscribeAsync<SimpleMessageB>(msg => {
                    Assert.True(false, "Received wrong message type.");
                });
                await messageBus.SubscribeAsync<SimpleMessageA>(msg => {
                   Assert.AreEqual("Hello", msg.Data);
                    resetEvent.Set();
                });
                await messageBus.PublishAsync(new SimpleMessageA {
                    Data = "Hello"
                });

                await resetEvent.WaitAsync();
            } finally {
                await CleanupMessageBusAsync(messageBus);
            }
        }

        public virtual async Task WillReceiveDerivedMessageTypesAsync() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            try {
                var countdown = new AsyncCountdownEvent(2);
                await messageBus.SubscribeAsync<ISimpleMessage>(msg => {
                   Assert.AreEqual("Hello", msg.Data);
                    countdown.Signal();
                });
                await messageBus.PublishAsync(new SimpleMessageA {
                    Data = "Hello"
                });
                await messageBus.PublishAsync(new SimpleMessageB {
                    Data = "Hello"
                });
                await messageBus.PublishAsync(new SimpleMessageC {
                    Data = "Hello"
                });

                await countdown.WaitAsync();
               Assert.AreEqual(0, countdown.CurrentCount);
            } finally {
                await CleanupMessageBusAsync(messageBus);
            }
        }

        public virtual async Task CanSubscribeToAllMessageTypesAsync() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            try {
                var countdown = new AsyncCountdownEvent(3);
                await messageBus.SubscribeAsync<object>(msg => {
                    countdown.Signal();
                });
                await messageBus.PublishAsync(new SimpleMessageA {
                    Data = "Hello"
                });
                await messageBus.PublishAsync(new SimpleMessageB {
                    Data = "Hello"
                });
                await messageBus.PublishAsync(new SimpleMessageC {
                    Data = "Hello"
                });

                await countdown.WaitAsync();
               Assert.AreEqual(0, countdown.CurrentCount);
            } finally {
                await CleanupMessageBusAsync(messageBus);
            }
        }

        public virtual async Task WontKeepMessagesWithNoSubscribersAsync() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            try {
                await messageBus.PublishAsync(new SimpleMessageA {
                    Data = "Hello"
                });

                await SystemClock.SleepAsync(100);
                var resetEvent = new AsyncAutoResetEvent(false);
                await messageBus.SubscribeAsync<SimpleMessageA>(msg => {
                   Assert.AreEqual("Hello", msg.Data);
                    resetEvent.Set();
                });

               Assert.ThrowsAsync<OperationCanceledException>(() => resetEvent.WaitAsync());
            } finally {
                await CleanupMessageBusAsync(messageBus);
            }
        }

        public virtual async Task CanCancelSubscriptionAsync() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            try {
                var countdown = new AsyncCountdownEvent(2);

                long messageCount = 0;
                var cancellationTokenSource = new CancellationTokenSource();
                await messageBus.SubscribeAsync<SimpleMessageA>(msg => {
                    _logger.Debug("SimpleAMessage received");
                    Interlocked.Increment(ref messageCount);
                    cancellationTokenSource.Cancel();
                    countdown.Signal();
                }, cancellationTokenSource.Token);

                await messageBus.SubscribeAsync<object>(msg => countdown.Signal());

                await messageBus.PublishAsync(new SimpleMessageA {
                    Data = "Hello"
                });

                await countdown.WaitAsync();
               Assert.AreEqual(0, countdown.CurrentCount);
               Assert.AreEqual(1, messageCount);

                countdown = new AsyncCountdownEvent(1);
                await messageBus.PublishAsync(new SimpleMessageA {
                    Data = "Hello"
                });

                await countdown.WaitAsync();
               Assert.AreEqual(0, countdown.CurrentCount);
               Assert.AreEqual(1, messageCount);
            } finally {
                await CleanupMessageBusAsync(messageBus);
            }
        }

        public virtual async Task CanReceiveFromMultipleSubscribersAsync() {
            var messageBus1 = GetMessageBus();
            if (messageBus1 == null)
                return;

            try {
                var countdown1 = new AsyncCountdownEvent(1);
                await messageBus1.SubscribeAsync<SimpleMessageA>(msg => {
                   Assert.AreEqual("Hello", msg.Data);
                    countdown1.Signal();
                });

                var messageBus2 = GetMessageBus();
                try {
                    var countdown2 = new AsyncCountdownEvent(1);
                    await messageBus2.SubscribeAsync<SimpleMessageA>(msg => {
                       Assert.AreEqual("Hello", msg.Data);
                        countdown2.Signal();
                    });

                    await messageBus1.PublishAsync(new SimpleMessageA {
                        Data = "Hello"
                    });

                    await countdown1.WaitAsync();
                   Assert.AreEqual(0, countdown1.CurrentCount);
                    await countdown2.WaitAsync();
                   Assert.AreEqual(0, countdown2.CurrentCount);
                } finally {
                    await CleanupMessageBusAsync(messageBus2);
                }
            } finally {
                await CleanupMessageBusAsync(messageBus1);
            }
        }

        public virtual void CanDisposeWithNoSubscribersOrPublishers() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            using (messageBus) {}
        }
    }
}
