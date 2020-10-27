﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Logging;

namespace d60.Cirqus.Snapshotting
{
    /// <summary>
    /// In-memory implementation of <see cref="ISnapshotCache"/> that can hold approximately <seealso cref="ApproximateMaxNumberOfCacheEntries"/> entries in
    /// memory. Each entry is assigned value that is computed based on the number of applied events, the number of times the entry has been hit, and the
    /// time since last hit (implementation). 
    /// </summary>
    public class InMemorySnapshotCache : ISnapshotCache
    {
        static Logger _logger;

        static InMemorySnapshotCache()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        readonly ConcurrentDictionary<string, ConcurrentDictionary<long, CacheEntry>> _cacheEntries =
            new ConcurrentDictionary<string, ConcurrentDictionary<long, CacheEntry>>();

        long _currentNumberOfCacheEntries; //<long because of Interlocked.Read
        int _approximateMaxNumberOfCacheEntries = 1000; //< approximate because who cares if we're slightly off

        public int ApproximateMaxNumberOfCacheEntries
        {
            get { return _approximateMaxNumberOfCacheEntries; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(string.Format("Cannot set maximum number of cache entries to {0} - it must be 0 or greater", value));
                }
                _approximateMaxNumberOfCacheEntries = value;

                PossiblyTrimCache();
            }
        }

        internal class CacheEntry
        {
            static readonly Sturdylizer Sturdylizer = new Sturdylizer();

            CacheEntry()
            {
            }

            public static CacheEntry Create(AggregateRoot aggregateRootInfo)
            {
                var rootInstance = aggregateRootInfo;
                var data = Sturdylizer.SerializeObject(rootInstance);

                return new CacheEntry
                {
                    SequenceNumber = aggregateRootInfo.CurrentSequenceNumber,
                    GlobalSequenceNumber = aggregateRootInfo.GlobalSequenceNumberCutoff,
                    AggregateRootId = aggregateRootInfo.Id,
                    AggregateRootType = rootInstance.GetType(),
                    Hits = 0,
                    TimeOfLastHit = DateTime.UtcNow,
                    Data = data
                };
            }

            public string Data { get; set; }

            public long SequenceNumber { get; private set; }

            public long GlobalSequenceNumber { get; private set; }

            public string AggregateRootId { get; private set; }

            public Type AggregateRootType { get; private set; }

            public int Hits { get; private set; }

            public DateTime TimeOfLastHit { get; private set; }

            public void IncrementHits()
            {
                Hits++;
                TimeOfLastHit = DateTime.UtcNow;
            }

            public double ComputeValue()
            {
                var totalSecondsSinceLastHit = (DateTime.UtcNow - TimeOfLastHit).TotalSeconds;

                var ensureAlwaysPositive = Math.Max(0.01, totalSecondsSinceLastHit);

                return SequenceNumber * Hits / ensureAlwaysPositive;
            }

            public AggregateRoot GetClone()
            {
                try
                {
                    var deserializedObject = Sturdylizer.DeserializeObject(Data);

                    var instance = (AggregateRoot)deserializedObject;

                    return instance;
                }
                catch (Exception exception)
                {
                    throw new SerializationException(string.Format("An error occured when attempting to deserialize {0} into an aggregate root",
                        Data), exception);
                }
            }
        }

        public AggregateRoot GetCloneFromCache(string aggregateRootId, long globalSequenceNumber)
        {
            try
            {
                var entriesForThisRoot = _cacheEntries.GetOrAdd(aggregateRootId,
                    id => new ConcurrentDictionary<long, CacheEntry>());

                CacheEntry entry;

                var availableSequenceNumbersForThisRoot = entriesForThisRoot
                    .Where(e => e.Value.GlobalSequenceNumber <= globalSequenceNumber)
                    .Select(e => e.Value.GlobalSequenceNumber)
                    .ToArray();

                if (!availableSequenceNumbersForThisRoot.Any()) return null;

                var highestSequenceNumberAvailableForThisRoot = availableSequenceNumbersForThisRoot.Max();

                if (!entriesForThisRoot.TryGetValue(highestSequenceNumberAvailableForThisRoot, out entry))
                    return null;

                var aggregateRootInfoToReturn = entry.GetClone();

                entry.IncrementHits();

                return aggregateRootInfoToReturn;
            }
            catch (Exception exception)
            {
                _logger.Warn("An error occurred while attempting to load cache entry for {0}/{1}: {2}", aggregateRootId, globalSequenceNumber, exception);

                return null;
            }
        }

        public void PutCloneToCache(AggregateRoot aggregateRootInfo)
        {
            // don't waste cycles add/trimming
            if (IsEffectivelyDisabled) return;

            var aggregateRootId = aggregateRootInfo.Id;
            var entriesForThisRoot = _cacheEntries.GetOrAdd(aggregateRootId, id => new ConcurrentDictionary<long, CacheEntry>());

            var entrySuccessfullyAdded = entriesForThisRoot.TryAdd(aggregateRootInfo.GlobalSequenceNumberCutoff, CacheEntry.Create(aggregateRootInfo));

            if (entrySuccessfullyAdded)
            {
                Interlocked.Increment(ref _currentNumberOfCacheEntries);
            }

            PossiblyTrimCache();
        }

        bool IsEffectivelyDisabled
        {
            get { return _approximateMaxNumberOfCacheEntries == 0; }
        }

        void PossiblyTrimCache()
        {
            var numberOfEntriesCurrentlyInTheCache = Interlocked.Read(ref _currentNumberOfCacheEntries);

            if (numberOfEntriesCurrentlyInTheCache <= _approximateMaxNumberOfCacheEntries) return;

            var removalsToPerform = Math.Max(10, _approximateMaxNumberOfCacheEntries / 10);

            _logger.Debug("Performing cache trimming - cache currently holds {0} entries divided among {1} aggregate roots - will attempt to perform {2} removals",
                numberOfEntriesCurrentlyInTheCache, _cacheEntries.Count, removalsToPerform);

            var entriesOrderedByValue = _cacheEntries
                .SelectMany(kvp => kvp.Value)
                .Select(kvp => kvp.Value)
                .Select(entry => new
                {
                    Entry = entry,
                    Value = entry.ComputeValue()
                })
                .OrderBy(a => a.Value)
                .ToArray();

            var index = 0;

            while (index < entriesOrderedByValue.Length && removalsToPerform > 0)
            {
                ConcurrentDictionary<long, CacheEntry> entriesForThisRoot;

                var entryToRemove = entriesOrderedByValue[index++];

                if (!_cacheEntries.TryGetValue(entryToRemove.Entry.AggregateRootId, out entriesForThisRoot))
                    continue;

                CacheEntry removedEntry;

                var entrySuccessfullyRemoved = entriesForThisRoot.TryRemove(entryToRemove.Entry.GlobalSequenceNumber, out removedEntry);

                if (entrySuccessfullyRemoved)
                {
                    removalsToPerform--;

                    Interlocked.Decrement(ref _currentNumberOfCacheEntries);
                }
            }

            if (removalsToPerform > 0)
            {
                _logger.Debug("Cache trimming could not remove {0} cache entries (no worries, they have probably been removed by another thread)", removalsToPerform);
            }
        }
    }
}