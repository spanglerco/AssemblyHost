// Copyright © 2014 Paul Spangler
//
// Licensed under the MIT License (the "License");
// you may not use this file except in compliance with the License.
// You should have received a copy of the License with this software.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Threading;
using System.Diagnostics;

using SpanglerCo.AssemblyHost;

namespace SpanglerCo.AssemblyHostExample.Examples
{
    /// <summary>
    /// An AssemblyHost example for <see cref="InterfaceHostProcess"/> in <see cref="ExecutionMode.AsyncThread"/> mode.
    /// </summary>

    public sealed class InterfaceThread : IExample
    {
        private InterfaceHostProcess _host;
        private readonly object _hostLock = new object();

        /// <summary>
        /// The number of milliseconds between progress updates during the example.
        /// </summary>

        private const int ProgressInterval = 500;

        /// <see cref="IExample.Name"/>

        public string Name
        {
            get
            {
                return "Interface Thread Mode";
            }
        }

        /// <see cref="IExample.Description"/>

        public string Description
        {
            get
            {
                return "An example of the Interface host in the AsyncThread mode, which instantiates a class that implements IChildProcess and calls the Execute method asynchronously on another thread.\n\n" +
                       "InterfaceHostProcess in AsyncThread mode is useful when needing to perform a task in the child process with progress reporting and the ability to cancel.\n\n" +
                       "The input parameter is the number of seconds it will take for the child process to finish its task. Try entering a non-integer value as well. Use the Stop Example button to cancel the task.";
            }
        }

        /// <see cref="IExample.ParameterPrompt"/>

        public string ParameterPrompt
        {
            get
            {
                return "Secon_ds to Execute:";
            }
        }

        /// <see cref="IExample.CanBeStopped"/>

        public bool CanBeStopped
        {
            get
            {
                return true;
            }
        }

        /// <see cref="IExample.Run"/>

        public void Run(IExampleLogger logger, string parameter)
        {
            try
            {
                // Tell AssemblyHost to execute the HostedType class which implements IChildProcess.
                // It's also possible to host a type that isn't even loaded in the current
                // process by specifying a path to the assembly and the name of the type.

                TypeArgument argument = new TypeArgument(typeof(HostedType));

                logger.Log("Creating InterfaceHostProcess");
                using (InterfaceHostProcess host = new InterfaceHostProcess(argument, parameter))
                {
                    lock (_hostLock)
                    {
                        // Save the host so we can stop it later.
                        _host = host;
                    }

                    try
                    {
                        // Log when the child process' status changes or on receipt of progress from the child process.
                        host.HostProgress += (sender, args) => { logger.Log("Child process reported progress: " + args.Progress); };
                        host.StatusChanged += (sender, args) => { logger.Log(string.Format("Child process moved to {0} status", host.Status)); };

                        logger.Log("Starting child process");
                        host.Start(true);
                        ChildProcess = Process.GetProcessById(host.ChildProcess.Id); // Needed for abort.

                        // Go do something useful if we don't need to wait for the child process to finish...

                        logger.Log("Waiting for child process to finish");
                        string result = host.WaitStopped(true);
                        logger.Log("Result from child process: " + result);
                    }
                    finally
                    {
                        lock (_hostLock)
                        {
                            // Remove the saved reference prior to disposing: it's too late to stop the example.
                            _host = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // WaitStopped will by default throw an exception if the child process
                // encounters an error.
                logger.Log(ex);
            }
        }

        /// <see cref="IExample.Stop"/>

        public void Stop(IExampleLogger logger)
        {
            lock (_hostLock)
            {
                // The host reference will be null if the example
                // isn't in a state where it can be stopped.

                if (_host != null)
                {
                    logger.Log("Signaling the child process to stop.");
                    _host.Stop();
                }
            }
        }

        /// <summary>
        /// The class being hosted in the child process.
        /// </summary>
        /// <remarks>
        /// Not IDisposable because AssemblyHost will not call Dispose anyway.
        /// This is okay because the process will terminate soon after Execute
        /// is finished anyway, so there won't be any wasted resources for long.
        /// </remarks>

        public class HostedType : IChildProcess
        {
            private ManualResetEventSlim _cancel;

            /// <see cref="IChildProcess.Result"/>

            public string Result { get; private set; }

            /// <see cref="IChildProcess.Mode"/>
            /// <see cref="ExecutionMode.AsyncThread"/>

            public ExecutionMode Mode
            {
                get
                {
                    // Tell the AssemblyHost that Execute should be called asynchronously on a new thread.
                    // In AsyncThread mode, EndExecution will only be called if the parent process calls Stop.
                    // However, Execute may still return before Stop is called, finishing the child process.
                    // When that happens, it's possible for EndExecution to never be called.
                    return ExecutionMode.AsyncThread;
                }
            }

            /// <summary>
            /// Creates a new instance of the hosted type.
            /// </summary>
            /// <remarks>
            /// For a type to be hosted by AssemblyHost, it must have a public
            /// constructor that takes no parameters or have no constructors,
            /// in which case the compiler will generate a default constructor.
            /// </remarks>

            public HostedType()
            {
                _cancel = new ManualResetEventSlim();
            }

            /// <see cref="IChildProcess.Execute"/>

            public void Execute(string arguments, IProgressReporter progressReporter)
            {
                int seconds;

                if (!int.TryParse(arguments, out seconds))
                {
                    // This exception will be available in the parent process.
                    throw new ArgumentException("Must pass an integer number of seconds.", "arguments");
                }

                int remaining = seconds * 1000;

                while (remaining > 0)
                {
                    if (_cancel.Wait(0))
                    {
                        progressReporter.ReportProgress(string.Format("Canceling task with {0} milliseconds remaining.", remaining));
                        Result = string.Format("Task canceled after {0} seconds.", seconds - remaining / 1000.0);
                        return;
                    }

                    progressReporter.ReportProgress(string.Format("Task in progress, {0} milliseconds remain.", remaining));
                    Thread.Sleep(remaining > ProgressInterval ? ProgressInterval : remaining);
                    remaining -= ProgressInterval;
                }

                Result = string.Format("Task completed in {0} seconds.", seconds);
            }

            /// <see cref="IChildProcess.EndExecution"/>
            /// <remarks>
            /// In AsyncThread mode, EndExecution may be called after Execute has already returned.
            /// The event will remain valid because it never gets manually disposed.
            /// </remarks>

            public void EndExecution()
            {
                _cancel.Set();
            }
        }

        /// <see cref="IExample.ChildProcess"/>

        public Process ChildProcess { get; private set; }
    }
}
