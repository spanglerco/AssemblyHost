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

        /// <summary>
        /// Logs an exception message.
        /// </summary>
        /// <param name="ex">The exception to log.</param>

        void Log(Exception ex);
    }
}
