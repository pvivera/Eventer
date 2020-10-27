﻿using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Events
{
    /// <summary>
    /// Provides a bunch of static methods that make it easy to perform some simple integrity tests on an event batch that is to be committed
    /// </summary>
    public class EventValidation
    {
        /// <summary>
        /// Validates the integrity of the given event batch with respect to sequence numbers etc.
        /// </summary>
        public static void ValidateBatchIntegrity(Guid batchId, List<EventData> events)
        {
            EnsureAllEventsHaveSequenceNumbers(events);

            EnsureAllEventsHaveAggregateRootId(events);

            EnsureSeq(batchId, events);
        }

        static void EnsureAllEventsHaveAggregateRootId(List<EventData> events)
        {
            if (events.Any(e => !e.Meta.ContainsKey(DomainEvent.MetadataKeys.AggregateRootId)))
            {
                throw new InvalidOperationException("Can't save batch with event without an aggregate root id");
            }
        }

        static void EnsureAllEventsHaveSequenceNumbers(List<EventData> events)
        {
            if (events.Any(e => !e.Meta.ContainsKey(DomainEvent.MetadataKeys.SequenceNumber)))
            {
                throw new InvalidOperationException("Can't save batch with event without a sequence number");
            }

            if (events.Any(e => !e.Meta.ContainsKey(DomainEvent.MetadataKeys.GlobalSequenceNumber)))
            {
                throw new InvalidOperationException("Can't save batch with event without a global sequence number");
            }
        }

        static void EnsureSeq(Guid batchId, List<EventData> events)
        {
            var aggregateRootSeqs = events
                .GroupBy(e => e.GetAggregateRootId())
                .ToDictionary(g => g.Key, g => g.Min(e => e.GetSequenceNumber()));

            var expectedNextGlobalSeq = events.First().GetGlobalSequenceNumber();

            foreach (var e in events)
            {
                var sequenceNumberOfThisEvent = e.GetSequenceNumber();
                var globalSequenceNumberOfThisEvent = e.GetGlobalSequenceNumber();
                var aggregateRootId = e.GetAggregateRootId();
                var expectedSequenceNumber = aggregateRootSeqs[aggregateRootId];

                if (globalSequenceNumberOfThisEvent != expectedNextGlobalSeq)
                {
                    throw new InvalidOperationException(
                        string.Format(@"Attempted to save batch {0} which contained events with non-sequential global sequence numbers!

{1}", batchId,
                            string.Join(", ", events.Select(ev => ev.GetGlobalSequenceNumber()))));
                }

                if (sequenceNumberOfThisEvent != expectedSequenceNumber)
                {
                    throw new InvalidOperationException(
                        string.Format(@"Attempted to save batch {0} which contained events with non-sequential sequence numbers!

{1}", batchId,
                            string.Join(Environment.NewLine,
                                events.Select(
                                    ev => string.Format("    {0} / {1}", ev.GetAggregateRootId(), ev.GetSequenceNumber())))));
                }

                aggregateRootSeqs[aggregateRootId]++;
                expectedNextGlobalSeq++;
            }
        }
    }
}