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
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using SpanglerCo.AssemblyHost.Ipc;

namespace SpanglerCo.AssemblyHost.Child
{
    /// <summary>
    /// Represents a host that calls methods on an interface during execution.
    /// </summary>

    internal sealed class InterfaceHostServer : HostServer, IProgressReporter
    {
        private const int SIGNAL_NONE = 0;
        private const int SIGNAL_MESSAGE = 1;
        private const int SIGNAL_THREAD = 2;
        private const int SIGNAL_ERROR = 3;

        private int _signal;
        private string _argument;
        private Thread _childThread;
        private ExecutionMode _mode;
        private IChildProcess _child;
        private Communication _communication;

        /// <see cref="HostServer.ParseCommands"/>

        public override bool ParseCommands(Queue<string> args, Communication communication)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            if (communication == null)
            {
                throw new ArgumentNullException("communication");
            }

            Type loadedType;
            TypeArgument argument = new TypeArgument(args);

            if (args.Count == 0)
            {
                throw new ArgumentException("Not enough arguments.", "args");
            }

            _argument = args.Dequeue();

            if (TryLoadType(argument, communication, out loadedType))
            {
                if (!CheckInterface(loadedType, typeof(IChildProcess)))
                {
                    communication.SendMessage(MessageType.InvalidTypeError, "The type must implement IChildProcess.");
                }
                else
                {
                    return TryCreateInstance(loadedType, communication, out _child);
                }
            }

            return false;
        }

        /// <see cref="HostServer.Execute"/>

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The exception is sent to the parent process and we exit safely.")]
        public override bool Execute(Communication communication)
        {
            if (_child == null)
            {
                throw new InvalidOperationException("The interface was not loaded correctly.");
            }

            if (communication == null)
            {
                throw new ArgumentNullException("communication");
            }

            _communication = communication;

            try
            {
                _mode = _child.Mode;
            }
            catch (Exception ex)
            {
                communication.SendMessage(MessageType.ExecuteError, ex);
                return false;
            }

            switch (_mode)
            {
                case ExecutionMode.Synchronous:
                case ExecutionMode.AsyncReturn:
                    try
                    {
                        _child.Execute(_argument, this);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        communication.SendMessage(MessageType.ExecuteError, ex);
                        return false;
                    }

                case ExecutionMode.AsyncThread:
                    _childThread = new Thread(ChildThread);
                    _childThread.Start();
                    return true;

                default:
                    communication.SendMessage(MessageType.ExecuteError, new InvalidOperationException("Unknown child execution mode."));
                    return false;
            }
        }

        /// <summary>
        /// Executes the child in a new thread.
        /// </summary>

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification="The exception is sent to the parent process and we exit safely.")]
        private void ChildThread()
        {
            try
            {
                _child.Execute(_argument, this);
            }
            catch (ThreadAbortException)
            {
                // EndExecution threw an exception that was already passed to the parent.
                // Don't pass this one as well.
                throw;
            }
            catch (Exception ex)
            {
                if (Interlocked.CompareExchange(ref _signal, SIGNAL_ERROR, SIGNAL_NONE) == SIGNAL_NONE)
                {
                    // The thread finished first. Ask the parent to wake up the main thread.

                    _communication.SendMessage(MessageType.RequestTerminate);
                }

                _communication.SendMessage(MessageType.ExecuteError, ex);
                return;
            }

            if (Interlocked.CompareExchange(ref _signal, SIGNAL_THREAD, SIGNAL_NONE) == SIGNAL_NONE)
            {
                // The thread finished first. Ask the parent to wake up the main thread.

                _communication.SendMessage(MessageType.RequestTerminate);
            }
        }

        /// <see cref="HostServer.WaitForSignal"/>

        public override bool WaitForSignal(Communication communication)
        {
            if (communication == null)
            {
                throw new ArgumentNullException("communication");
            }

            string data;
            Exception ex;
            MessageType type;

            if (_mode == ExecutionMode.Synchronous)
            {
                // Execute is already done, there is no stop.

                return true;
            }

            while (communication.TryReadMessage(out type, out data, out ex))
            {
                if (type == MessageType.SignalTerminate)
                {
                    break;
                }
            }

            // Either the thread finished first (AsyncThread mode) or there was a communication
            // failure. Either way, go ahead and call stop unless the thread had an error.

            return Interlocked.CompareExchange(ref _signal, SIGNAL_MESSAGE, SIGNAL_NONE) != SIGNAL_ERROR;
        }

        /// <see cref="HostServer.TryTerminate"/>

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The exception is sent to the parent process and we exit safely.")]
        public override bool TryTerminate(Communication communication, out string result)
        {
            if (communication == null)
            {
                throw new ArgumentNullException("communication");
            }

            if (_mode != ExecutionMode.Synchronous)
            {
                if (_signal == SIGNAL_MESSAGE)
                {
                    // Tell the child to stop executing and wait for the thread to finish.

                    try
                    {
                        _child.EndExecution();
                    }
                    catch (Exception ex)
                    {
                        communication.SendMessage(MessageType.ExecuteError, ex);
                        
                        if (_mode == ExecutionMode.AsyncThread)
                        {
                            _childThread.Abort();
                        }

                        result = null;
                        return false;
                    }

                    if (_mode == ExecutionMode.AsyncThread)
                    {
                        _childThread.Join();
                    }
                }
            }

            try
            {
                result = _child.Result;
                return true;
            }
            catch (Exception ex)
            {
                communication.SendMessage(MessageType.ExecuteError, ex);
                result = null;
                return false;
            }
        }

        /// <see cref="IProgressReporter.ReportProgress"/>

        public void ReportProgress(string progress)
        {
            _communication.SendMessage(MessageType.Progress, progress);
        }
    }
}
