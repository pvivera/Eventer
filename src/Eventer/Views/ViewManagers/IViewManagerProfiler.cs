using System;
using Eventer.Events;

namespace Eventer.Views.ViewManagers
{
    /// <summary>
    /// Profiler that can be registered in order to aggregate timing information from views
    /// </summary>
    public interface IViewManagerProfiler
    {
        /// <summary>
        /// Will be called by the view manager for each domain event that is dispatched.
        /// </summary>
        void RegisterTimeSpent(IViewManager viewManager, DomainEvent domainEvent, TimeSpan duration);
    }
}