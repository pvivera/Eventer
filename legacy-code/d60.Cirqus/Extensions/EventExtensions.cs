using System;
using System.Linq;
using d60.Cirqus.Events;

namespace d60.Cirqus.Extensions
{
    /// <summary>
    /// Extensions that make it easier to work with domain events
    /// </summary>
    public static class EventExtensions
    {
        /// <summary>
        /// Metadata key to use for specifying a content type (can be used as a technical header if the event is serialized with an introspectable format, like e.g. JSON)
        /// </summary>
        public const string ContentTypeMetadataKey = "content-type";

        /// <summary>
        /// Value to use with the <see cref="ContentTypeMetadataKey"/> header if it's JSON-serialized
        /// </summary>
        public const string Utf8JsonMetadataValue = "application/json;charset=utf8";

        /// <summary>
        /// Gets the aggregate root ID from the domain event
        /// </summary>
        public static string GetAggregateRootId(this EventData domainEvent, bool throwIfNotFound = true)
        {
            return GetMetadataField(domainEvent, DomainEvent.MetadataKeys.AggregateRootId, x => x, throwIfNotFound);
        }

        /// <summary>
        /// Gets the batch ID from the domain event
        /// </summary>
        public static Guid GetBatchId(this EventData domainEvent, bool throwIfNotFound = true)
        {
            return GetMetadataField(domainEvent, DomainEvent.MetadataKeys.BatchId, value => new Guid(Convert.ToString(value)), throwIfNotFound);
        }

        /// <summary>
        /// Gets the (root-local) sequence number from the domain event
        /// </summary>
        public static long GetSequenceNumber(this EventData domainEvent, bool throwIfNotFound = true)
        {
            return GetMetadataField(domainEvent, DomainEvent.MetadataKeys.SequenceNumber, Convert.ToInt64, throwIfNotFound);
        }

        /// <summary>
        /// Gets the global sequence number from the domain event
        /// </summary>
        public static long GetGlobalSequenceNumber(this EventData domainEvent, bool throwIfNotFound = true)
        {
            return GetMetadataField(domainEvent, DomainEvent.MetadataKeys.GlobalSequenceNumber, Convert.ToInt64, throwIfNotFound);
        }

        /// <summary>
        /// Gets whther the serialized event data is UTF8-encoded JSON
        /// </summary>
        public static bool IsJson(this EventData e)
        {
            return e.Meta.ContainsKey(ContentTypeMetadataKey)
                   && e.Meta[ContentTypeMetadataKey] == Utf8JsonMetadataValue;
        }

        /// <summary>
        /// Addes the <see cref="ContentTypeMetadataKey"/> header with the <see cref="Utf8JsonMetadataValue"/> to mark the serialized
        /// event data as UTF8-encoded JSON
        /// </summary>
        public static void MarkAsJson(this EventData e)
        {
            e.Meta[ContentTypeMetadataKey] = Utf8JsonMetadataValue;
        }

        static TValue GetMetadataField<TValue>(EventData domainEvent, string key, Func<string, TValue> converter, bool throwIfNotFound)
        {
            var metadata = domainEvent.Meta;

            if (metadata.ContainsKey(key)) return converter(metadata[key]);

            if (!throwIfNotFound) return converter(null);

            var metadataString = string.Join(", ", metadata.Select(kvp => string.Format("{0}: {1}", kvp.Key, kvp.Value)));
            var message = string.Format("Attempted to get value of key '{0}' from event, but only the following" +
                                        " metadata were available: {1}", key, metadataString);

            throw new InvalidOperationException(message);
        }
    }
}