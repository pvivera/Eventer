﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;

namespace d60.Cirqus.Serialization
{
    public class CachingDomainEventSerializerDecorator : IDomainEventSerializer, IDisposable
    {
        static Logger _log;

        static CachingDomainEventSerializerDecorator()
        {
            CirqusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly ConcurrentDictionary<long, DomainEvent> _cachedDomainEvents = new ConcurrentDictionary<long, DomainEvent>();
        readonly IDomainEventSerializer _innerDomainEventSerializer;
        readonly int _approximateMaxNumberOfEntries;
        readonly Timer _purgeTimer = new Timer(30000);

        volatile bool _purging;
        readonly object _purgeLock = new object();

        public CachingDomainEventSerializerDecorator(IDomainEventSerializer innerDomainEventSerializer, int approximateMaxNumberOfEntries)
        {
            _innerDomainEventSerializer = innerDomainEventSerializer;
            _approximateMaxNumberOfEntries = approximateMaxNumberOfEntries;

            _purgeTimer.Elapsed += delegate
            {
                try
                {
                    PossiblyTrimCache();
                }
                catch (Exception exception)
                {
                    _log.Error(exception, "Eror while trimming cache");
                }
            };

            _purgeTimer.Start();
        }

        public EventData Serialize(DomainEvent e)
        {
            return _innerDomainEventSerializer.Serialize(e);
        }

        public DomainEvent Deserialize(EventData e)
        {
            var domainEvent = _cachedDomainEvents.GetOrAdd(e.GetGlobalSequenceNumber(),
                _ => _innerDomainEventSerializer.Deserialize(e));

            return domainEvent;
        }

        void PossiblyTrimCache()
        {
            if (_purging) return;

            lock (_purgeLock)
            {
                if (_purging) return;

                _purging = true;
            }

            var stopwatch = Stopwatch.StartNew();
            var didTrimCache = false;

            try
            {
                if (_cachedDomainEvents.Count < _approximateMaxNumberOfEntries) return;

                didTrimCache = true;
                _log.Debug("Trimming cache");

                while (_cachedDomainEvents.Count > _approximateMaxNumberOfEntries)
                {
                    DomainEvent dummy;
                    _cachedDomainEvents.TryRemove(_cachedDomainEvents.Keys.First(), out dummy);
                }
            }
            finally
            {
                _purging = false;

                if (didTrimCache)
                {
                    _log.Info("Cache purge operation took {0:0.0} s", stopwatch.Elapsed.TotalSeconds);
                }
            }
        }

        public void Dispose()
        {
            _purgeTimer.Stop();
            _purgeTimer.Dispose();
        }
    }
}