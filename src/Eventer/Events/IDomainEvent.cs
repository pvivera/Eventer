using Eventer.Numbers;

namespace Eventer.Events
{
    /// <summary>
    /// Interface that all domain events implements
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>
        /// Gets the domain event's metadata
        /// </summary>
        Metadata Meta { get; }
    }
}