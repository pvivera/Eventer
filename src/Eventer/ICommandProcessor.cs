using System;
using Eventer.Commands;

namespace Eventer
{
    /// <summary>
    /// Command processor API - basically just processes commands :)
    /// </summary>
    public interface ICommandProcessor : IDisposable
    {
        /// <summary>
        /// Processes the specified command by invoking the generic eventDispatcher method
        /// </summary>
        CommandProcessingResult ProcessCommand(Command command);
    }
}