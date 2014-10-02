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
using SpanglerCo.AssemblyHost.Internal;

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
        private ManualResetEventSlim _stopListener;

        /// <summary>
        /// Occurs when the status of the host process has changed.
        /// </summary>

        public event EventHandler StatusChanged;

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
                    OnStatusChanged(value);
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
        /// <param name="assembly">The information for the assembly to host in the process.</param>
        /// <param name="locater">An <see cref="IAssemblyLocater"/> for locating the executable to use for the child process.</param>
        /// <exception cref="ArgumentNullException">if assembly or locater is null.</exception>

        internal HostProcess(AssemblyArgument assembly, IAssemblyLocater locater)
            : this(assembly, locater, new ProcessStartInfo())
        {
            _childInfo.CreateNoWindow = true;
            _childInfo.UseShellExecute = false;
        }

        /// <summary>
        /// Creates a new host process with given start info.
        /// </summary>
        /// <param name="assembly">The information for the assembly to host in the process.</param>
        /// <param name="locater">An <see cref="IAssemblyLocater"/> for locating the executable to use for the child process.</param>
        /// <param name="startInfo">The start info to use when creating the process.</param>
        /// <exception cref="ArgumentNullException">if assembly, locater, or startInfo are null.</exception>

        internal HostProcess(AssemblyArgument assembly, IAssemblyLocater locater, ProcessStartInfo startInfo)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            if (locater == null)
            {
                throw new ArgumentNullException("locater");
            }

            if (startInfo == null)
            {
                throw new ArgumentNullException("startInfo");
            }

            _childInfo = startInfo;
            _spinLock = new SpinLock(false);
            _arguments = new List<string>();
            _status = HostProcessStatus.NotStarted;
            _stopListener = new ManualResetEventSlim();

            _arguments.Add(assembly.Location);
            _childInfo.UseShellExecute = false;
            _childInfo.FileName = locater.LocateAssembly(assembly.Bitness);
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
                OnStatusChanged(value);
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
                OnStatusChanged(value);
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
        /// Waits for the host process to finish execution and enter the Stopped or Error state.
        /// Returns immediately if the host process has already finished execution.
        /// </summary>
        /// <param name="throwOnError">
        /// If true when the host process encounters an error, an exception representing the error will be thrown.
        /// Regardless of the value of throwOnError, the exception can be retrieved using the Error property.
        /// </param>
        /// <returns>
        /// The result of executing the hosted assembly, or null if the assembly did not return a result
        /// or encountered an error and throwOnError is false. The result can also be obtained using
        /// the ExecutionResult property.
        /// </returns>
        /// <exception cref="Exception">
        /// if throwOnError is true and the host process encounters an error.
        /// The type of exception is determined by hosted assembly.
        /// </exception>
        /// <exception cref="ObjectDisposedException">if Dispose has been called.</exception>
        /// <see cref="Status"/>
        /// <see cref="Error"/>
        /// <see cref="ExecutionResult"/>

        public string WaitStopped(bool throwOnError)
        {
            WaitStopped(Timeout.Infinite, throwOnError);
            return ExecutionResult;
        }

        /// <summary>
        /// Waits for the host process to finish execution and enter the Stopped or Error state.
        /// Returns immediately if the host process has already finished execution.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The maximum time to wait in milliseconds to wait,
        /// or System.Threading.Timeout.Infinite to wait indefinitely.
        /// </param>
        /// <param name="throwOnError">
        /// If true when the host process encounters an error, an exception representing the error will be thrown.
        /// Regardless of the value of throwOnError, the exception can be retrieved using the Error property.
        /// </param>
        /// <returns>
        /// True if the host process has finished, false on timeout.
        /// Use the ExecutionResult property to retrieve the result of executing the hosted assembly.
        /// </returns>
        /// <exception cref="Exception">
        /// if throwOnError is true and the host process encounters an error.
        /// The type of exception is determined by hosted assembly.
        /// </exception>
        /// <exception cref="ObjectDisposedException">if Dispose has been called.</exception>
        /// <see cref="Status"/>
        /// <see cref="Error"/>
        /// <see cref="ExecutionResult"/>

        public bool WaitStopped(int millisecondsTimeout, bool throwOnError)
        {
            if (!_stopListener.Wait(millisecondsTimeout))
            {
                return false;
            }

            if (throwOnError && Status == HostProcessStatus.Error)
            {
                throw Error;
            }

            return true;
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
                bool result;
                _status = HostProcessStatus.Stopping;

                try
                {
                    result = _communication.SendMessage(MessageType.SignalTerminate);
                }
                finally
                {
                    _spinLock.Exit();
                }

                OnStatusChanged(HostProcessStatus.Stopping);
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
        /// Performs tasks related to status changes then raises the StatusChanged event.
        /// </summary>
        /// <param name="newStatus">The status after the change.</param>

        private void OnStatusChanged(HostProcessStatus newStatus)
        {
            if (newStatus == HostProcessStatus.Stopped || newStatus == HostProcessStatus.Error)
            {
                _stopListener.Set();
            }

            OnStatusChanged();
        }

        /// <summary>
        /// Raises the StatusChanged event.
        /// </summary>

        protected virtual void OnStatusChanged()
        {
            var temp = StatusChanged;
            if (temp != null)
            {
                temp(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Called when the host process reports progress. Does nothing by default.
        /// </summary>
        /// <param name="progress">The progress reported by the host process.</param>

        protected virtual void OnHostProgress(string progress)
        { }

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

                _stopListener.Dispose();
            }
        }
    }
}
