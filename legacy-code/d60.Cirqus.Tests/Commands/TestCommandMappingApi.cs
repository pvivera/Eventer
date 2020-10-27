﻿using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Commands
{
    [TestFixture]
    public class TestCommandMappingApi : FixtureBase
    {
        ICommandProcessor _realCommandProcessor;
        TestContext _fakeCommandProcessor;

        protected override void DoSetUp()
        {
            var commandMappings = new CommandMappings()
                .Map<RawRootCommand>((context, command) =>
                {
                    var instance = context.TryLoad<Root>(command.AggregateRootId)
                               ?? context.Create<Root>(command.AggregateRootId);
                    
                    instance.DoStuff();
                })
                .Map<AnotherRawRootCommand>((context, command) =>
                {
                    var instance = context.TryLoad<Root>(command.AggregateRootId)
                                   ?? context.Create<Root>(command.AggregateRootId);

                    instance
                        .DoStuff()
                        .DoStuff();

                    var otherInstance = context.TryLoad<Root>(command.AggregateRootId + "w00t!")
                                        ?? context.Create<Root>(command.AggregateRootId + "w00t!");

                    otherInstance
                        .DoStuff()
                        .DoStuff()
                        .DoStuff();
                });

            _realCommandProcessor = CommandProcessor.With()
                .EventStore(e => e.Register<IEventStore>(c => new InMemoryEventStore()))
                .Options(o => o.AddCommandMappings(commandMappings))
                .Create();

            RegisterForDisposal(_realCommandProcessor);

            _fakeCommandProcessor = TestContext.With()
                .Options(x => x.AddCommandMappings(commandMappings))
                .Create();
        }

        [Test]
        public void CanExecuteRawBaseCommandWithRealCommandProcessor()
        {
            _realCommandProcessor.ProcessCommand(new RawRootCommand
            {
                AggregateRootId = "hej"
            });

            _realCommandProcessor.ProcessCommand(new AnotherRawRootCommand
            {
                AggregateRootId = "hej"
            });
        }

        [Test]
        public void CanExecuteRawBaseCommandWithFakeCommandProcessor()
        {
            _fakeCommandProcessor.ProcessCommand(new RawRootCommand
            {
                AggregateRootId = "hej"
            });

            _fakeCommandProcessor.ProcessCommand(new AnotherRawRootCommand
            {
                AggregateRootId = "hej"
            });
        }

        public class RawRootCommand : Command
        {
            public string AggregateRootId { get; set; }
        }

        public class AnotherRawRootCommand : Command
        {
            public string AggregateRootId { get; set; }
        }

        public class Root : AggregateRoot, IEmit<Event>
        {
            public Root DoStuff()
            {
                Emit(new Event());
                return this;
            }

            public void Apply(Event e)
            {

            }
        }

        public class Event : DomainEvent<Root>
        {
        }
    }
}