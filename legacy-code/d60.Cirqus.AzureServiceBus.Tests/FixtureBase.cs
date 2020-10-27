﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using d60.Cirqus.Numbers;
using NUnit.Framework;

namespace d60.Cirqus.AzureServiceBus.Tests
{
    public class FixtureBase
    {
        List<IDisposable> _stuffToDispose;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            TimeMachine.Reset();
        }

        [SetUp]
        public void SetUp()
        {
            _stuffToDispose = new List<IDisposable>();

            DoSetUp();
        }

        [TearDown]
        public void TearDown()
        {
            DoTearDown();

            DisposeStuff();
        }

        protected virtual  void DoSetUp()
        {
        }
        protected virtual  void DoTearDown()
        {
        }

        public delegate void TimerCallback(TimeSpan elapsedTotal);

        protected TDisposable RegisterForDisposal<TDisposable>(TDisposable disposable) where TDisposable : IDisposable
        {
            _stuffToDispose.Add(disposable);
            return disposable;
        }

        protected void DisposeStuff()
        {
            _stuffToDispose.ForEach(d =>
            {
                Console.WriteLine("Disposing {0}", d);
                d.Dispose();
            });
            _stuffToDispose.Clear();
        }

        protected void TakeTime(string description, Action action, TimerCallback periodicCallback = null)
        {
            Console.WriteLine("Begin: {0}", description);
            var stopwatch = Stopwatch.StartNew();
            var lastCallback = DateTime.UtcNow;

            using (var timer = new Timer())
            {
                if (periodicCallback != null)
                {
                    timer.Interval = 5000;
                    timer.Elapsed += delegate
                    {
                        periodicCallback(stopwatch.Elapsed);
                    };
                    timer.Start();
                }

                action();
            }
            var elapsed = stopwatch.Elapsed;
            Console.WriteLine("End: {0} - elapsed: {1:0.0} s", description, elapsed.TotalSeconds);
        }
    }
}