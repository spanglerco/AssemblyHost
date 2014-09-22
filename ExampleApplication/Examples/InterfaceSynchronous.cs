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

using SpanglerCo.AssemblyHost;
using System.Threading;
using System.Diagnostics;

namespace SpanglerCo.AssemblyHostExample.Examples
{
    /// <summary>
    /// An AssemblyHost example for <see cref="InterfaceHostProcess"/> in <see cref="ExecutionMode.Synchronous"/> mode.
    /// </summary>

    public sealed class InterfaceSynchronous : IExample
    {
        /// <summary>
        /// The number of milliseconds between progress updates during the example.
        /// </summary>

        private const int ProgressInterval = 500;

        /// <see cref="IExample.Name"/>

        public string Name
        {
            get
            {
                return "Interface Synchronous Mode";
            }
        }

        /// <see cref="IExample.Description"/>

        public string Description
        {
            get
            {
                return "An example of the Interface host in the Synchronous mode, which instantiates a class that implements IChildProcess and calls the Execute method synchronously.\n\n" +
                       "InterfaceHostProcess in Synchronous mode is useful when needing to perform a predetermined task in the child process with progress reporting.\n\n" +
                       "The input parameter is the number of seconds it will take for the child process to finish its task. Try entering a non-integer value as well.";
            }
        }

        /// <see cref="IExample.ParameterPrompt"/>

        public string ParameterPrompt
        {
            get
            {
                return "_Seconds to Execute:";
            }
        }

        /// <see cref="IExample.CanBeStopped"/>

        public bool CanBeStopped
        {
            get
            {
                // Synchronous mode doesn't support being stopped.
                return false;
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
            }
            catch (Exception ex)
            {
                // WaitStopped will by default throw an exception if the child process
                // encounters an error.
                logger.Log(ex);
            }
        }

        /// <summary>
        /// The class being hosted in the child process.
        /// </summary>

        public class HostedType : IChildProcess
        {
            /// <see cref="IChildProcess.Result"/>

            public string Result { get; private set; }

            /// <see cref="IChildProcess.Mode"/>

            public ExecutionMode Mode
            {
                get
                {
                    return ExecutionMode.Synchronous;
                }
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
                    progressReporter.ReportProgress(string.Format("Task in progress, {0} milliseconds remain.", remaining));
                    Thread.Sleep(remaining > ProgressInterval ? ProgressInterval : remaining);
                    remaining -= ProgressInterval;
                }

                Result = string.Format("Task completed in {0} seconds.", seconds);
            }

            /// <see cref="IChildProcess.EndExecution"/>

            void IChildProcess.EndExecution()
            {
                throw new NotImplementedException();
            }
        }

        /// <see cref="IExample.Stop"/>

        void IExample.Stop(IExampleLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <see cref="IExample.ChildProcess"/>

        public Process ChildProcess { get; private set; }
    }
}
