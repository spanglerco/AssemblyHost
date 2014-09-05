using System;

namespace SpanglerCo.AssemblyHostExample
{
    /// <summary>
    /// Represents a logger for examples to show current status.
    /// </summary>

    public interface IExampleLogger
    {
        /// <summary>
        /// Logs a message to show status.
        /// </summary>
        /// <param name="message">The message to log.</param>

        void Log(string message);
    }
}
