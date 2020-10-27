﻿using d60.Cirqus.Numbers;

namespace d60.Cirqus.Events
{
    /// <summary>
    /// Contains event data for one domain event, possibly with the original <see cref="DomainEvent"/> instance included (if the event was just emitted)
    /// </summary>
    public class EventData
    {
        readonly Metadata _meta;
        readonly byte[] _data;

        /// <summary>
        /// Creates the event data
        /// </summary>
        protected EventData(byte[] data, Metadata meta)
        {
            _data = data;
            _meta = meta;
        }

        /// <summary>
        /// Constructs an <see cref="EventData"/> that wraps the given <see cref="DomainEvent"/> and the raw serialized body of that event
        /// </summary>
        public static EventData FromDomainEvent(DomainEvent domainEvent, byte[] data)
        {
            return new EventData(data, domainEvent.Meta);
        }

        /// <summary>
        /// Constructs an <see cref="EventData"/> that wraps the given raw serialized body and the given <see cref="Metadata"/>
        /// </summary>
        public static EventData FromMetadata(Metadata meta, byte[] data)
        {
            return new EventData(data, meta);
        }

        /// <summary>
        /// Gets the metadata that is included with this event, either from the wrapped <see cref="DomainEvent"/> or from the wrapped <see cref="Metadata"/>
        /// </summary>
        public Metadata Meta
        {
            get { return _meta; }
        }

        /// <summary>
        /// Gets the raw serialized body of the event
        /// </summary>
        public virtual byte[] Data
        {
            get { return _data; }
        }

        public override string ToString()
        {
            return string.Format("Event({0})", Meta.ContainsKey(DomainEvent.MetadataKeys.GlobalSequenceNumber)
                ? Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber]
                : "?");
        }

        public bool IsSameAs(EventData otherEvent)
        {
            var otherMeta = otherEvent.Meta;
            var otherData = otherEvent.Data;

            var meta = Meta;
            var data = Data;

            if (otherMeta.Count != meta.Count) return false;

            if (data.Length != otherData.Length) return false;

            foreach (var kvp in meta)
            {
                if (!otherMeta.ContainsKey(kvp.Key))
                    return false;

                if (otherMeta[kvp.Key] != kvp.Value)
                    return false;
            }

            for (var index = 0; index < data.Length; index++)
            {
                if (data[index] != otherData[index]) return false;
            }

            return true;
        }
    }
}