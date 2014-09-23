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
using System.IO;
using System.Threading;
using System.Diagnostics;

using SpanglerCo.AssemblyHost;

namespace SpanglerCo.AssemblyHostExample.Examples
{
    /// <summary>
    /// An AssemblyHost example for <see cref="InterfaceHostProcess"/> in <see cref="ExecutionMode.AsyncReturn"/> mode.
    /// </summary>

    public sealed class InterfaceReturn : IExample
    {
        private InterfaceHostProcess _host;
        private readonly object _hostLock = new object();

        /// <see cref="IExample.Name"/>

        public string Name
        {
            get
            {
                return "Interface Return Mode";
            }
        }

        /// <see cref="IExample.Description"/>

        public string Description
        {
            get
            {
                return "An example of the Interface host in the AsyncReturn mode, which instantiates a class that implements IChildProcess, calls the Execute method, then calls EndExecution to stop.\n\n" +
                       "InterfaceHostProcess in AsyncReturn mode is useful when needing to host a service or indefinite task in the child process with progress reporting. The child process doesn't exit until stopped.\n\n" +
                       "The input parameter is the path to a directory to watch for changes. Try entering a non-existent path as well. Use the Stop Example button to complete the example.";
            }
        }

        /// <see cref="IExample.ParameterPrompt"/>

        public string ParameterPrompt
        {
            get
            {
                return "_Path to Watch:";
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
            private int _numChanges;
            private FileSystemWatcher _watcher;
            private IProgressReporter _reporter;
            private readonly object _reporterLock = new object();

            /// <see cref="IChildProcess.Result"/>

            public string Result { get; private set; }

            /// <see cref="IChildProcess.Mode"/>
            /// <see cref="ExecutionMode.AsyncThread"/>

            public ExecutionMode Mode
            {
                get
                {
                    // Tell the AssemblyHost to expect Execute to return but that the child
                    // process should stay alive until the parent process calls Stop.
                    // EndExecution will be called at that time. It's important for Execute
                    // to return in a reasonable amount of time because EndExecution won't
                    // be called until Execute has returned.
                    return ExecutionMode.AsyncReturn;
                }
            }

            /// <see cref="IChildProcess.Execute"/>

            public void Execute(string arguments, IProgressReporter progressReporter)
            {
                _reporter = progressReporter;

                // FileSystemWatcher will throw an exception if the path is invalid or doesn't exist.
                _watcher = new FileSystemWatcher(arguments);
                _watcher.Error += new ErrorEventHandler(OnError);
                _watcher.Renamed += new RenamedEventHandler(OnRenamed);
                _watcher.Changed += new FileSystemEventHandler(OnChanged);
                _watcher.Created += new FileSystemEventHandler(OnCreated);
                _watcher.Deleted += new FileSystemEventHandler(OnDeleted);
                _watcher.EnableRaisingEvents = true;

                // The FileSystemWatcher will continue to run until EndExecution is called
                // when the parent process calls Stop.
                progressReporter.ReportProgress("Monitoring enabled. Make changes within " + arguments + ".");
            }

            /// <see cref="IChildProcess.EndExecution"/>
            /// <remarks>
            /// In AsyncReturn mode, EndExecution will always be called once Execute
            /// has returned and the parent process has called Stop.
            /// The FileSystemWatcher will remain valid because it never gets manually disposed.
            /// </remarks>

            public void EndExecution()
            {
                _watcher.EnableRaisingEvents = false;

                lock (_reporterLock)
                {
                    _reporter = null;
                    Result = string.Format("Monitored {0} changes.", _numChanges);
                }
            }

            #region FileSystemWatcher event handlers

            private void OnError(object sender, ErrorEventArgs e)
            {
                lock (_reporterLock)
                {
                    if (_reporter != null)
                    {
                        _reporter.ReportProgress("Error: " + e.GetException().Message);
                    }
                }
            }

            private void OnRenamed(object sender, RenamedEventArgs e)
            {
                lock (_reporterLock)
                {
                    if (_reporter != null)
                    {
                        ++_numChanges;
                        _reporter.ReportProgress(string.Format("Renamed {0} to {1}.", e.OldName, e.Name));
                    }
                }
            }

            private void OnDeleted(object sender, FileSystemEventArgs e)
            {
                lock (_reporterLock)
                {
                    if (_reporter != null)
                    {
                        ++_numChanges;
                        _reporter.ReportProgress(string.Format("Deleted {0}.", e.Name));
                    }
                }
            }

            private void OnCreated(object sender, FileSystemEventArgs e)
            {
                lock (_reporterLock)
                {
                    if (_reporter != null)
                    {
                        ++_numChanges;
                        _reporter.ReportProgress(string.Format("Created {0}.", e.Name));
                    }
                }
            }

            private void OnChanged(object sender, FileSystemEventArgs e)
            {
                lock (_reporterLock)
                {
                    if (_reporter != null)
                    {
                        ++_numChanges;
                        _reporter.ReportProgress(string.Format("Changed {0}.", e.Name));
                    }
                }
            }

            #endregion
        }

        /// <see cref="IExample.ChildProcess"/>

        public Process ChildProcess { get; private set; }
    }
}
