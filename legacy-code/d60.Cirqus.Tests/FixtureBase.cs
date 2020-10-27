﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Numbers;
using NUnit.Framework;

namespace d60.Cirqus.Tests
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

            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel: Logger.Level.Debug);

            DoSetUp();
        }

        protected void SetLogLevel(Logger.Level newLogLevel)
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel: newLogLevel);
        }

        protected TDisposable RegisterForDisposal<TDisposable>(TDisposable disposable) where TDisposable : IDisposable
        {
            _stuffToDispose.Add(disposable);
            return disposable;
        }

        [TearDown]
        public void TearDown()
        {
            DoTearDown();

            DisposeStuff();
        }

        protected void DisposeStuff()
        {
            _stuffToDispose.ForEach(d => d.Dispose());
            _stuffToDispose.Clear();
        }

        protected virtual void DoSetUp()
        {
        }
        protected virtual void DoTearDown()
        {
        }

        public delegate void TimerCallback(TimeSpan elapsedTotal);

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

        protected async Task TakeTimeAsync(string description, Func<Task> action, TimerCallback periodicCallback = null)
        {
            Console.WriteLine("Begin: {0}", description);

            var stopwatch = Stopwatch.StartNew();

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

                await action();
            }
            var elapsed = stopwatch.Elapsed;
            Console.WriteLine("End: {0} - elapsed: {1:0.0} s", description, elapsed.TotalSeconds);
        }
    }
}