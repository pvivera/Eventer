﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;

namespace d60.Cirqus.Views
{
    /// <summary>
    /// Implementation of <see cref="IEventDispatcher"/> that wraps a <see cref="ViewManagerEventDispatcher"/> and ensures
    /// that it only dispatches events to views that are currently to be managed by that event dispatcher. It's complicated.
    /// </summary>
    public class AutoDistributionViewManagerEventDispatcher : IDisposable, IEventDispatcher
    {
        static Logger _logger;

        static AutoDistributionViewManagerEventDispatcher()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        readonly string _id;
        readonly IAutoDistributionState _autoDistributionState;
        readonly Timer _heartbeatTimer;
        readonly Timer _distributeViewsTimer;
        ViewManagerEventDispatcher _eventDispatcher;

        readonly List<IViewManager> _viewManagers = new List<IViewManager>();

        /// <summary>
        /// Constructs the <see cref="AutoDistributionViewManagerEventDispatcher"/>
        /// </summary>
        public AutoDistributionViewManagerEventDispatcher(string id, IAutoDistributionState autoDistributionState)
        {
            _id = id;
            _autoDistributionState = autoDistributionState;

            _heartbeatTimer = new Timer(1000);
            _heartbeatTimer.Elapsed += (sender, args) => EmitHeartbeat(true);

            _distributeViewsTimer = new Timer(4000);
            _distributeViewsTimer.Elapsed += (sender, args) => DistributeViews();
        }

        void EmitHeartbeat(bool runDistribution)
        {
            try
            {
                var idsOfViewsToBeManagedByMe = _autoDistributionState.Heartbeat(_id, true).ToList();

                if (!runDistribution) return;

                var idsOfCurrentlyManagedViews = _eventDispatcher.GetViewManagers().Select(v => v.Id).ToList();

                var idsOfViewsToStopManaging = idsOfCurrentlyManagedViews.Except(idsOfViewsToBeManagedByMe).ToList();
                var idsOfViewsToStartManaging = idsOfViewsToBeManagedByMe.Except(idsOfCurrentlyManagedViews).ToList();

                foreach (var viewManagerId in idsOfViewsToStopManaging)
                {
                    var viewManager = GetViewManager(viewManagerId);
                    _logger.Info("Event dispatcher {0} will stop managing {1} now", _id, viewManagerId);
                    _eventDispatcher.RemoveViewManager(viewManager);
                }

                foreach (var viewManagerId in idsOfViewsToStartManaging)
                {
                    var viewManager = GetViewManager(viewManagerId);
                    _logger.Info("Event dispatcher {0} will start managing {1} now", _id, viewManagerId);
                    _eventDispatcher.AddViewManager(viewManager);
                }
            }
            catch (Exception exception)
            {
                _logger.Warn(exception, "Could not emit heartbeat for {0}", _id);
            }
        }

        IViewManager GetViewManager(string id)
        {
            try
            {
                return _viewManagers.First(v => v.Id == id);
            }
            catch (Exception exception)
            {
                throw new ArgumentException(string.Format("Could not find view with ID '{0}'", id), exception);
            }
        }

        void DistributeViews()
        {
            try
            {
                var currentState = _autoDistributionState.GetCurrentState().ToList();

                if (!currentState.Any())
                {
                    _logger.Warn("Current state (based on heartbeats) yielded no active");
                    return;
                }

                var idsOfViewsThatNeedToBeDistributed = _viewManagers.Select(v => v.Id).OrderBy(id => id).ToList();

                if (!idsOfViewsThatNeedToBeDistributed.Any())
                {
                    _logger.Info("Apparently, there's no views that need to be managed");
                    return;
                }

                var newState = idsOfViewsThatNeedToBeDistributed
                    .Distribute(currentState.Count)
                    .Zip(currentState.Select(s => s.ManagerId), (viewIds, managerId) => new KeyValuePair<string, HashSet<string>>(managerId, new HashSet<string>(viewIds)))
                    .Select(kvp => new AutoDistributionState(kvp.Key, kvp.Value))
                    .ToList();

                _autoDistributionState.SetNewState(newState);
            }
            catch (Exception exception)
            {
                _logger.Warn(exception, "Error while distributing views for {0}", _id);
            }
        }

        /// <summary>
        /// Registers the given view manager event dispatcher
        /// </summary>
        public void Register(ViewManagerEventDispatcher eventDispatcher)
        {
            if (_eventDispatcher != null)
            {
                throw new InvalidOperationException(string.Format("Cannot register {0} because {1} was already registered!", eventDispatcher, _eventDispatcher));
            }

            _eventDispatcher = eventDispatcher;

            _viewManagers.AddRange(_eventDispatcher.GetViewManagers());
        }

        public void Initialize(bool purgeExistingViews = false)
        {
            if (_eventDispatcher == null)
            {
                throw new InvalidOperationException("Attempted to initialize AutoDistributionViewManagerEventDispatcher but no ViewManagerEventDispatcherWasRegistered");
            }

            _eventDispatcher.Initialize(purgeExistingViews);

            SignOn();

            _heartbeatTimer.Start();
            _distributeViewsTimer.Start();
        }

        void SignOn()
        {
            EmitHeartbeat(false);
        }

        public void Dispatch(IEnumerable<DomainEvent> events)
        {
            if (_eventDispatcher == null)
            {
                throw new InvalidOperationException("Attempted to dispatch to AutoDistributionViewManagerEventDispatcher but no ViewManagerEventDispatcherWasRegistered");
            }

            _eventDispatcher.Dispatch(events);
        }

        public void Dispose()
        {
            _heartbeatTimer.Dispose();
            _distributeViewsTimer.Dispose();

            SignOff();

            DistributeViews();
        }

        void SignOff()
        {
            try
            {
                _autoDistributionState.Heartbeat(_id, false);
            }
            catch (Exception exception)
            {
                _logger.Warn(exception, "Error during sign-off");
            }
        }
    }
}