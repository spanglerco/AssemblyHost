// This file is part of AssemblyHost.
// Copyright © 2014 Paul Spangler
//
// AssemblyHost is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// AssemblyHost is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with AssemblyHost.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;

using SpanglerCo.AssemblyHost.Ipc;

namespace SpanglerCo.AssemblyHost
{
    /// <summary>
    /// Serves as the base class for an AssemblyHost process.
    /// </summary>

    public abstract class HostProcess : IDisposable
    {
        private SpinLock _spinLock;
        private Process _childProcess;
        private List<string> _arguments;
        private HostProcessStatus _status;
        private ProcessStartInfo _childInfo;
        private ParentCommunication _communication;

        /// <summary>
        /// Occurs when the status of the host process has changed.
        /// </summary>

        public event EventHandler StatusChanged;

        /// <summary>
        /// Occurs when the host process reports progress.
        /// </summary>

        public event EventHandler<HostProgressEventArgs> HostProgress;

        /// <summary>
        /// Gets the status of the process.
        /// </summary>

        public HostProcessStatus Status
        {
            get
            {
                bool lockTaken = false;
                HostProcessStatus result;

                _spinLock.Enter(ref lockTaken);
                result = _status;
                _spinLock.Exit();

                return result;
            }

            private set
            {
                bool lockTaken = false;
                HostProcessStatus oldValue;

                _spinLock.Enter(ref lockTaken);
                oldValue = _status;
                _status = value;
                _spinLock.Exit();

                if (oldValue != value)
                {
                    OnStatusChanged();
                }
            }
        }

        /// <summary>
        /// Once started, gets the Process instance representing the child process.
        /// </summary>
        /// <remarks>
        /// The returned Process can still be used after the child process has stopped,
        /// but not after the HostProcess has been disposed.
        /// </remarks>
        /// <exception cref="InvalidOperationException">if the child has not previously been started.</exception>

        public Process ChildProcess
        {
            get
            {
                if (_childProcess == null)
                {
                    throw new InvalidOperationException("Must start the child before a process is available.");
                }

                return _childProcess;
            }
        }

        /// <summary>
        /// Gets an exception representing the process' error, or null if no error has been reported.
        /// </summary>

        public Exception Error { get; private set; }

        /// <summary>
        /// Gets the result of executing the hosted assembly, or null if the
        /// process has not stopped or the assembly did not return a result.
        /// </summary>

        public string ExecutionResult { get; private set; }

        /// <summary>
        /// Creates a new host process.
        /// </summary>
        /// <param name="assemblyPath">The absolute path containing the assembly to host in the process.</param>
        /// <exception cref="ArgumentNullException">if assemblyLoadPath is null.</exception>

        internal HostProcess(string assemblyPath)
            : this(assemblyPath, new ProcessStartInfo())
        {
            _childInfo.CreateNoWindow = true;
            _childInfo.UseShellExecute = false;
        }

        /// <summary>
        /// Creates a new host process with given start info.
        /// </summary>
        /// <param name="assemblyLoadPath">The absolute path containing the assembly to host in the process.</param>
        /// <param name="startInfo">The start info to use when creating the process.</param>
        /// <exception cref="ArgumentNullException">if assemblyLoadPath or startInfo are null.</exception>

        internal HostProcess(string assemblyLoadPath, ProcessStartInfo startInfo)
        {
            if (assemblyLoadPath == null)
            {
                throw new ArgumentNullException("assemblyLoadPath");
            }

            if (startInfo == null)
            {
                throw new ArgumentNullException("startInfo");
            }

            _childInfo = startInfo;
            _spinLock = new SpinLock(false);
            _arguments = new List<string>();
            _status = HostProcessStatus.NotStarted;

            _arguments.Add(assemblyLoadPath);
            _childInfo.UseShellExecute = false;
            _childInfo.FileName = Assembly.GetExecutingAssembly().Location;
        }

        /// <summary>
        /// Compares the status with another and, if they are equal, sets the status.
        /// </summary>
        /// <param name="value">The value to set as the status.</param>
        /// <param name="comparand">The value to compare to the status.</param>
        /// <returns>The original status.</returns>

        private HostProcessStatus CompareExchangeStatus(HostProcessStatus value, HostProcessStatus comparand)
        {
            bool lockTaken = false;
            HostProcessStatus result;

            _spinLock.Enter(ref lockTaken);
            result = _status;

            if (result == comparand)
            {
                _status = value;
            }

            _spinLock.Exit();

            if (result == comparand)
            {
                OnStatusChanged();
            }

            return result;
        }

        /// <summary>
        /// Compares the status with another and, if they are equal, sets the status and Error.
        /// </summary>
        /// <param name="value">The value to set as the status.</param>
        /// <param name="error">The value to set as the error.</param>
        /// <param name="comparand">The value to compare to the status.</param>
        /// <returns>The original status.</returns>

        private HostProcessStatus CompareSetErrorStatus(HostProcessStatus value, Exception error, HostProcessStatus comparand)
        {
            bool lockTaken = false;
            HostProcessStatus result;

            _spinLock.Enter(ref lockTaken);
            result = _status;

            if (result == comparand)
            {
                _status = value;
                Error = error;
            }

            _spinLock.Exit();

            if (result == comparand)
            {
                OnStatusChanged();
            }

            return result;
        }

        /// <summary>
        /// Starts the host process.
        /// </summary>
        /// <param name="waitForExecution">True to wait for execution of the assembly to begin before returning.</param>
        /// <exception cref="Win32Exception">if there is an error starting the process.</exception>
        /// <exception cref="InvalidOperationException">if the process had previously been started.</exception>

        public void Start(bool waitForExecution)
        {
            if (CompareExchangeStatus(HostProcessStatus.Starting, HostProcessStatus.NotStarted) != HostProcessStatus.NotStarted)
            {
                throw new InvalidOperationException("The process has already been started.");
            }

            _communication = new ParentCommunication();
            _communication.AddChildArguments(_arguments);
            AddArguments(_arguments);

            StringBuilder argumentBuilder = new StringBuilder();

            foreach (string arg in _arguments)
            {
                if (arg.Length > 0 && arg[0] != '"' && arg.Contains(" "))
                {
                    argumentBuilder.Append('"');
                    argumentBuilder.Append(arg);
                    argumentBuilder.Append('"');
                }
                else
                {
                    argumentBuilder.Append(arg);
                }

                argumentBuilder.Append(' ');
            }

            // Remove the last space.

            _childInfo.Arguments = argumentBuilder.ToString(0, argumentBuilder.Length - 1);
            _childProcess = Process.Start(_childInfo);

            _communication.ChildProcessStarted();

            Thread listenThread = new Thread(ListenForChild);

            if (waitForExecution)
            {
                using (ManualResetEventSlim eventHandle = new ManualResetEventSlim(false))
                {
                    listenThread.Start(eventHandle);
                    eventHandle.Wait();
                }
            }
            else
            {
                listenThread.Start(null);
            }
        }

        /// <summary>
        /// Runs in another thread to receive messages from the child process.
        /// </summary>
        /// <param name="waitHandle">A ManualResetEventSlim to set upon receiving the first message, or null.</param>

        private void ListenForChild(object waitHandle)
        {
            string data;
            Exception ex;
            MessageType type;
            ManualResetEventSlim eventHandle = waitHandle as ManualResetEventSlim;
            bool result = _communication.TryReadMessage(out type, out data, out ex);

            // Only set the event once.

            if (eventHandle != null)
            {
                eventHandle.Set();
            }

            while (result)
            {
                switch (type)
                {
                    case MessageType.HostStarted:
                        CompareExchangeStatus(HostProcessStatus.Executing, HostProcessStatus.Starting);
                        break;

                    case MessageType.ExecuteError:
                    case MessageType.AssemblyLoadError:
                        Error = ex;
                        Status = HostProcessStatus.Error;
                        break;

                    case MessageType.InvalidTypeError:
                    case MessageType.InvalidExecuteError:
                        Error = new TargetInvocationException(data, ex);
                        Status = HostProcessStatus.Error;
                        break;

                    case MessageType.HostFinished:
                        ExecutionResult = data;
                        Status = HostProcessStatus.Stopped;
                        break;

                    case MessageType.Progress:
                        OnHostProgress(data);
                        break;

                    case MessageType.RequestTerminate:
                        bool lockTaken = false;
                        _spinLock.Enter(ref lockTaken);
                        
                        try
                        {
                            _communication.SendMessage(MessageType.SignalTerminate);
                        }
                        finally
                        {
                            _spinLock.Exit();
                        }
                        
                        break;

                    case MessageType.InternalError:
                    case MessageType.ArgumentParseError:
                    default:
                        Error = new TargetInvocationException("Internal error.", ex);
                        Status = HostProcessStatus.Error;
                        break;
                }

                result = _communication.TryReadMessage(out type, out data, out ex);
            }

            // If the read failed without the child sending a status update, it must have crashed.

            CompareSetErrorStatus(HostProcessStatus.Error, new TargetInvocationException("The child process ended unexpectedly.", null), HostProcessStatus.Executing);
        }

        /// <summary>
        /// Signals the child process to terminate.
        /// </summary>
        /// <returns>True if the message was sent successfully, false if the message could not be sent or did not need to be sent.</returns>
        /// <exception cref="InvalidOperationException">if the child process has not been started.</exception>

        protected bool StopChild()
        {
            bool lockTaken = false;
            HostProcessStatus status;

            _spinLock.Enter(ref lockTaken);
            status = _status;

            if (status == HostProcessStatus.Starting || status == HostProcessStatus.Executing)
            {
                _status = HostProcessStatus.Stopping;
            }

            if (status == HostProcessStatus.Executing || status == HostProcessStatus.Starting)
            {
                bool result;

                try
                {
                    result = _communication.SendMessage(MessageType.SignalTerminate);
                }
                finally
                {
                    _spinLock.Exit();
                }
                
                OnStatusChanged();
                return result;
            }

            _spinLock.Exit();

            if (status == HostProcessStatus.NotStarted)
            {
                throw new InvalidOperationException("The child process has not been started.");
            }

            return false;
        }

        /// <summary>
        /// Raises the StatusChanged event.
        /// </summary>

        protected virtual void OnStatusChanged()
        {
            if (StatusChanged != null)
            {
                StatusChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Raises the HostProgress event.
        /// </summary>
        /// <param name="progress">The progress reported by the host process.</param>

        protected virtual void OnHostProgress(string progress)
        {
            if (HostProgress != null)
            {
                HostProgress(this, new HostProgressEventArgs(progress));
            }
        }

        /// <summary>
        /// Adds command-line arguments to pass to the child process.
        /// </summary>
        /// <param name="args"></param>

        protected abstract void AddArguments(IList<string> args);

        /// <see cref="IDisposable.Dispose"/>

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources used by the class.
        /// </summary>
        /// <param name="disposing">True if being called by dispose, false if being finalized.</param>

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_communication != null)
                {
                    _communication.Dispose();
                }

                if (_childProcess != null)
                {
                    _childProcess.Dispose();
                    _childProcess = null;
                }
            }
        }
    }
}
