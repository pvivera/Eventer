﻿using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Bugs
{
    [TestFixture]
    public class Issue27_CallingLoadFromWithinRootInvokesCreatedCallback : FixtureBase
    {
        [Test]
        public void IsFixed()
        {
            var unitOfWork = GetUnitOfWork();

            var root1 = new SomeRoot
            {
                UnitOfWork = unitOfWork
            };

            root1.DoIt();

            Assert.IsNotEmpty(unitOfWork.EmittedEvents.OfType<Created>());
        }

        public class SomeRoot : AggregateRoot
        {
            public void DoIt()
            {
                var instance = TryLoad<SomeOtherRoot>("newid") ?? Create<SomeOtherRoot>("newid");
            }
        }

        public class SomeOtherRoot : AggregateRoot, IEmit<Created>
        {
            protected override void Created()
            {
                Emit(new Created());
            }

            public void Apply(Created e)
            {
            }
        }

        public class Created : DomainEvent<SomeOtherRoot> { }

        static InMemoryUnitOfWork GetUnitOfWork()
        {
            var serializer = new JsonDomainEventSerializer();
            var mapper = new DefaultDomainTypeNameMapper();
            var eventStore = new InMemoryEventStore();
            var repository = new DefaultAggregateRootRepository(eventStore, serializer, mapper);
            return new InMemoryUnitOfWork(repository, mapper);
        }
    }
}