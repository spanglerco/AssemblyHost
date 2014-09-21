using System;
using System.Threading;

using SpanglerCo.AssemblyHost;

namespace SpanglerCo.AssemblyHostExample
{
    /// <summary>
    /// The interface for all examples.
    /// </summary>

    public interface IExample
    {
        /// <summary>
        /// Gets the name of the example.
        /// </summary>

        string Name { get; }

        /// <summary>
        /// Gets a description for the example.
        /// </summary>

        string Description { get; }

        /// <summary>
        /// Gets a message to prompt for a parameter to pass to the example,
        /// or null if the example does not use a parameter.
        /// </summary>

        string ParameterPrompt { get; }

        /// <summary>
        /// Gets whether or not the example supports being stopped during execution.
        /// </summary>

        bool CanBeStopped { get; }

        /// <summary>
        /// Runs the example.
        /// </summary>
        /// <param name="logger">An object that the example can use to log messages during execution.</param>
        /// <param name="parameter">The parameter supplied to the example based on <see cref="ParameterPrompt"/>.</param>

        void Run(IExampleLogger logger, string parameter);

        /// <summary>
        /// Requests the example stop during a <see cref="Run"/>.
        /// </summary>
        /// <param name="logger">An object that the example can use to log messages while stopping.</param>

        void Stop(IExampleLogger logger);
    }
}
