﻿using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Bugs
{
    [TestFixture]
    public class VerifyThatNewlyCreatedAggregateRootsAreAvailableForLoading : FixtureBase
    {
        TestContext _context;

        protected override void DoSetUp()
        {
            _context = RegisterForDisposal(TestContext.Create());
        }

        [Test]
        public void ItHasBeenFixed()
        {
            using (var uow = _context.BeginUnitOfWork())
            {
                var counterpart = uow.Load<Counterpart>("counterpartid");
                // create fresh counterpart AR by setting its name
                counterpart.SetName("muggie");

                var contract = uow.Load<Contract>("contractid");

                // now make the contract do something that causes the counterpart to be loaded
                // act
                contract.AssignWith(counterpart);

                uow.Commit();
            }

            // assert
            var reloadedContract = _context.BeginUnitOfWork().Load<Contract>("contractid");
            Assert.That(reloadedContract.AssignedCounterpartName, Is.EqualTo("muggie"));
        }

        public class Counterpart : AggregateRoot, IEmit<CounterpartNamed>
        {
            public string Name { get; private set; }

            public void SetName(string name)
            {
                Emit(new CounterpartNamed { Name = name });
            }

            public void Apply(CounterpartNamed e)
            {
                Name = e.Name;
            }
        }

        public class CounterpartNamed : DomainEvent<Counterpart>
        {
            public string Name { get; set; }
        }

        public class Contract : AggregateRoot, IEmit<ContractAssignedToCounterpart>
        {
            public string AssignedCounterpartName { get; private set; }

            public void AssignWith(Counterpart counterpart)
            {
                Emit(new ContractAssignedToCounterpart { CounterpartId = counterpart.Id });
            }

            public void Apply(ContractAssignedToCounterpart e)
            {
                AssignedCounterpartName = Load<Counterpart>(e.CounterpartId).Name;
            }
        }

        public class ContractAssignedToCounterpart : DomainEvent<Contract>
        {
            public string CounterpartId { get; set; }
        }
    }
}