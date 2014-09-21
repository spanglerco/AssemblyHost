using System;
using System.Diagnostics;
using System.Collections.Generic;

using SpanglerCo.AssemblyHost;

namespace SpanglerCo.AssemblyHostExample.Examples
{
    /// <summary>
    /// An AssemblyHost example for <see cref="MethodHostProcess"/>.
    /// </summary>

    public sealed class Method : IExample
    {
        /// <summary>
        /// The name of the environment variable used to pass an argument to the child process.
        /// </summary>

        private const string ParameterVariable = "ExampleParameter";

        /// <see cref="IExample.Name"/>

        public string Name
        {
            get
            {
                return "Method";
            }
        }

        /// <see cref="IExample.Description"/>

        public string Description
        {
            get
            {
                return "An example of the Method host, which executes a single instance method and returns the result. A parameter is passed using the environment.\n\n" +
                       "MethodHostProcess is useful for its simplicity when needing to perform a predetermined task in the child process.";
            }
        }

        /// <see cref="IExample.ParameterPrompt"/>

        public string ParameterPrompt
        {
            get
            {
                return "_Message to echo:";
            }
        }

        /// <see cref="IExample.CanBeStopped"/>

        public bool CanBeStopped
        {
            get
            {
                return false;
            }
        }

        /// <see cref="IExample.Run"/>

        public void Run(IExampleLogger logger, string parameter)
        {
            try
            {
                logger.Log("Adding parameter to child environment");
                ProcessStartInfo info = new ProcessStartInfo();
                info.CreateNoWindow = true; // Hide the console window for the child process.
                info.EnvironmentVariables.Add(ParameterVariable, parameter);

                // Tell AssemblyHost to run a method on the HostedType class called Execute.
                // It's also possible to execute a method that isn't even loaded in the current
                // process by specifying a path to the assembly and the names of the type and method.

                MethodArgument argument = new MethodArgument(typeof(HostedType).GetMethod("Execute"));

                logger.Log("Creating MethodHostProcess");
                using (MethodHostProcess host = new MethodHostProcess(argument, info))
                {
                    // Log when the child process' status changes.
                    host.StatusChanged += (sender, args) => { logger.Log(string.Format("Child process moved to {0} status", host.Status)); };

                    logger.Log("Starting child process");
                    host.Start(false);

                    // Go do something useful if we don't need to wait for the child process to finish...

                    logger.Log("Waiting for child process to finish");
                    string result = host.WaitStopped(true);
                    logger.Log("Result from child process: " + result);
                }
            }
            catch (Exception ex)
            {
                // WaitStopped will by default throw an exception if the child process
                // encounters an error.
                logger.Log(ex);
            }
        }

        /// <summary>
        /// The class that is hosted in the child process by the AssemblyHost.
        /// </summary>
        /// <remarks>
        /// When AssemblyHost starts the child process, it will instantiate this class
        /// using the default constructor, then call the Execute method based on the
        /// settings passed in to <see cref="MethodHostProcess"/> in <see cref="Run"/>.
        /// 
        /// The parent process is able to send an argument to the child process using
        /// environment variables.
        /// 
        /// Note that this class, the outer class, the constructor, and Execute method
        /// all must be public when using <see cref="MethodHostProcess"/>.
        /// </remarks>

        public class HostedType
        {
            private string _parameter;

            /// <summary>
            /// Creates a new hosted type.
            /// </summary>

            public HostedType()
            {
                // Retrieve the argument passed by the parent process via environment variable.
                _parameter = Environment.GetEnvironmentVariable(ParameterVariable);

                if (string.IsNullOrEmpty(_parameter))
                {
                    throw new InvalidOperationException("Environment variable not set");
                }
            }

            /// <summary>
            /// The main method for the child process.
            /// </summary>
            /// <returns>A value to send back to the parent process.</returns>
            /// <remarks>
            /// For <see cref="MethodHostProcess"/>, the child process exits after
            /// Execute returns and cannot respond to a stop signal sent by the
            /// parent process. Any value returned is converted to a string (if it
            /// isn't already a string) and sent back to the parent process. Note
            /// that values which do not have a meaningful string representation
            /// are not converted to a string and so the parent process only sees null.
            /// </remarks>

            public string Execute()
            {
                return string.Format("Process {0} says {1}", Process.GetCurrentProcess().Id, _parameter);
            }
        };

        /// <see cref="IExample.Stop"/>

        void IExample.Stop(IExampleLogger logger)
        {
            throw new InvalidOperationException();
        }
    }
}
