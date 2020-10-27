﻿using System;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;

namespace d60.Cirqus.Aggregates
{
    public class FactoryBasedAggregateRootRepository : DefaultAggregateRootRepository
    {
        readonly Func<Type, AggregateRoot> _aggregateRootFactoryMethod;

        public FactoryBasedAggregateRootRepository(IEventStore eventStore, IDomainEventSerializer domainEventSerializer, IDomainTypeNameMapper domainTypeNameMapper, Func<Type, AggregateRoot> aggregateRootFactoryMethod)
            : base(eventStore, domainEventSerializer, domainTypeNameMapper)
        {
            if (aggregateRootFactoryMethod == null)
                throw new ArgumentNullException("aggregateRootFactoryMethod");

            _aggregateRootFactoryMethod = aggregateRootFactoryMethod;
        }

        protected override AggregateRoot CreateAggregateRootInstance(Type aggregateRootType)
        {
            return _aggregateRootFactoryMethod(aggregateRootType);
        }
    }
}