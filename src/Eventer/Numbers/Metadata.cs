using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Eventer.Extensions;

namespace Eventer.Numbers
{
    /// <summary>
    /// Metadata collection that stores a bunch of key-value pairs that can be used for
    /// cross-cutting concerns like e.g. handling multi-tenancy, auditing, etc.
    /// </summary>
    [Serializable]
    public sealed class Metadata : Dictionary<string, string>
    {
        public static readonly CultureInfo NumberCulture = CultureInfo.InvariantCulture;

        Metadata(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public Metadata()
        {
        }

        internal void Merge(Metadata otherMeta)
        {
            foreach (var kvp in otherMeta)
            {
                if (ContainsKey(kvp.Key)) continue;

                this[kvp.Key] = kvp.Value;
            }
        }

        internal void TakeFromAttributes(ICustomAttributeProvider provider)
        {
            foreach (var meta in provider.GetAttributes<MetaAttribute>())
            {
                if (ContainsKey(meta.Key)) continue;

                this[meta.Key] = meta.Value;
            }
        }

        internal Metadata Clone()
        {
            var clone = new Metadata();
            foreach (var key in Keys.ToArray())
            {
                clone.Add(key, this[key]);
            }
            return clone;
        }

        public override string ToString()
        {
            return string.Join(", ", this.Select(kvp => $@"""{kvp.Key}"": ""{kvp.Value}"""));
        }
    }
}