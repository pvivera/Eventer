﻿using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Testing;
using d60.Cirqus.Tests.Contracts.Views.Factories;
using d60.Cirqus.Tests.Contracts.Views.Models.PurgeTest;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(PostgreSqlViewManagerFactory), Category = TestCategories.PostgreSql)]
    [TestFixture(typeof(MsSqlViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(EntityFrameworkViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(InMemoryViewManagerFactory))]
    [TestFixture(typeof(HybridDbViewManagerFactory), Category = TestCategories.MsSql)]
    public class PurgeTest<TFactory> : FixtureBase where TFactory : AbstractViewManagerFactory, new()
    {
        TFactory _factory;
        TestContext _context;
        IViewManager<PurgeTestView> _viewManager;

        protected override void DoSetUp()
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel: Logger.Level.Warn);

            _factory = RegisterForDisposal(new TFactory());

            _context = RegisterForDisposal(
                TestContext.With()
                    .Options(x => x.Asynchronous())
                    .Create());

            _viewManager = _factory.GetViewManager<PurgeTestView>();
            _context.AddViewManager(_viewManager);
        }

        [Test]
        public void CanPurgeTheView()
        {
            // arrange
            PurgeTestView.StaticBadBoy = "first value";
            _context.Save("id", new Event());
            PurgeTestView.StaticBadBoy = "new value";

            // act
            _viewManager.Purge();
            _context.WaitForViewsToCatchUp();

            // assert
            var view = _viewManager.Load(GlobalInstanceLocator.GetViewInstanceId());
            Assert.That(view.CaughtStaticBadBoy, Is.EqualTo("new value"));
        }
    }
}