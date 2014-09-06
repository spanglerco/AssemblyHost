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
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SpanglerCo.AssemblyHost;
using SpanglerCo.AssemblyHost.Ipc;
using SpanglerCo.AssemblyHost.Child;
using SpanglerCo.UnitTests.AssemblyHost.Mock;

namespace SpanglerCo.UnitTests.AssemblyHost
{
    /// <summary>
    ///This is a test class for InterfaceHostServerTest and is intended
    ///to contain all InterfaceHostServerTest Unit Tests
    ///</summary>
    [TestClass()]
    public class InterfaceHostServerTest
    {
        /// <summary>
        /// Creates the arguments Queue required for InterfaceHostServer.
        /// </summary>
        /// <param name="interfaceType">The type implementing the interface to use.</param>
        /// <param name="executeArgument">The argument to pass to the interface class.</param>
        /// <returns>The args Queue that can be passed to the InterfaceHostServer constructor.</returns>

        private Queue<string> InterfaceArgs(Type interfaceType, string executeArgument)
        {
            List<string> outArgs = new List<string>();
            TypeArgument arg = new TypeArgument(interfaceType);
            arg.AddArgs(outArgs);
            outArgs.Add(executeArgument);
            return new Queue<string>(outArgs);
        }

        /// <summary>
        ///A test for WaitForSignal
        ///</summary>
        [TestMethod()]
        public void WaitForSignalTest()
        {
            using (MockCommunication parent = new MockCommunication())
            {
                using (MockCommunication child = new MockCommunication(parent))
                {
                    // Send an unknown message.
                    using (InterfaceHostServer server = new InterfaceHostServer())
                    {
                        string result;
                        MockChildProcess.ReserveInstance(ExecutionMode.AsyncReturn, null, string.Empty);
                        Assert.IsTrue(server.ParseCommands(InterfaceArgs(typeof(MockChildProcess), string.Empty), child));
                        Assert.IsTrue(server.Execute(child));
                        Assert.IsTrue(parent.SendMessage(MessageType.NotSet));
                        Task thread = Task.Factory.StartNew(() => { Assert.IsTrue(server.WaitForSignal(child)); });
                        Assert.IsTrue(parent.SendMessage(MessageType.NotSet));
                        Assert.IsFalse(thread.Wait(500));
                        Assert.IsTrue(parent.SendMessage(MessageType.SignalTerminate));
                        Assert.IsTrue(thread.Wait(100));
                        Assert.IsTrue(server.TryTerminate(child, out result));
                        Assert.IsNull(result);
                    }

                    using (InterfaceHostServer server = new InterfaceHostServer())
                    {
                        Func<InterfaceHostServer, Communication, Action> waitForSignal = (_server, _com) => { return () => { _server.WaitForSignal(_com); }; };

                        TestUtilities.AssertThrows(waitForSignal(server, null), typeof(ArgumentNullException));
                    }
                }
            }
        }

        /// <summary>
        ///A test for TryTerminate
        ///</summary>
        [TestMethod()]
        public void TryTerminateTest()
        {
            using (MockCommunication parent = new MockCommunication())
            {
                using (MockCommunication child = new MockCommunication(parent))
                {
                    MessageType message;
                    string data;
                    Exception ex;

                    // Throw exception on EndExecute in AsyncReturn mode.
                    using (InterfaceHostServer server = new InterfaceHostServer())
                    {
                        MockChildProcess.ReserveInstance(ExecutionMode.AsyncReturn, null, string.Empty, throwOnEnd: true);
                        Assert.IsTrue(server.ParseCommands(InterfaceArgs(typeof(MockChildProcess), string.Empty), child));
                        Assert.IsTrue(server.Execute(child));
                        Assert.IsTrue(parent.SendMessage(MessageType.SignalTerminate));
                        Assert.IsTrue(server.WaitForSignal(child));
                        Assert.IsFalse(server.TryTerminate(child, out data));
                        Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                        Assert.AreEqual(MessageType.ExecuteError, message);
                        Assert.IsInstanceOfType(ex, typeof(InvalidOperationException));
                    }

                    // Throw exception on EndExecute in AsyncThread mode.
                    using (InterfaceHostServer server = new InterfaceHostServer())
                    {
                        MockChildProcess.ReserveInstance(ExecutionMode.AsyncThread, null, string.Empty, throwOnEnd: true);
                        Assert.IsTrue(server.ParseCommands(InterfaceArgs(typeof(MockChildProcess), string.Empty), child));
                        Assert.IsTrue(server.Execute(child));
                        Assert.IsTrue(parent.SendMessage(MessageType.SignalTerminate));
                        Assert.IsTrue(server.WaitForSignal(child));
                        Assert.IsFalse(server.TryTerminate(child, out data));
                        Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                        Assert.AreEqual(MessageType.ExecuteError, message);
                        Assert.IsInstanceOfType(ex, typeof(InvalidOperationException));
                    }

                    // Throw exception on Result.
                    using (InterfaceHostServer server = new InterfaceHostServer())
                    {
                        MockChildProcess.ReserveInstance(ExecutionMode.AsyncReturn, null, string.Empty, throwOnResult: true);
                        Assert.IsTrue(server.ParseCommands(InterfaceArgs(typeof(MockChildProcess), string.Empty), child));
                        Assert.IsTrue(server.Execute(child));
                        Assert.IsTrue(parent.SendMessage(MessageType.SignalTerminate));
                        Assert.IsTrue(server.WaitForSignal(child));
                        Assert.IsFalse(server.TryTerminate(child, out data));
                        Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                        Assert.AreEqual(MessageType.ExecuteError, message);
                        Assert.IsInstanceOfType(ex, typeof(InvalidOperationException));
                    }
                }
            }

            using (InterfaceHostServer server = new InterfaceHostServer())
            {
                string result;
                Func<InterfaceHostServer, Communication, Action> terminate = (_server, _com) => { return () => { _server.TryTerminate(_com, out result); }; };

                TestUtilities.AssertThrows(terminate(server, null), typeof(ArgumentNullException));
            }
        }

        /// <summary>
        ///A test for ReportProgress
        ///</summary>
        [TestMethod()]
        public void ReportProgressTest()
        {
            using (MockCommunication parent = new MockCommunication())
            {
                using (MockCommunication child = new MockCommunication(parent))
                {
                    using (InterfaceHostServer server = new InterfaceHostServer())
                    {
                        MessageType message;
                        string data;
                        Exception ex;

                        MockChildProcess.ReserveInstance(ExecutionMode.AsyncReturn, null, string.Empty);
                        Assert.IsTrue(server.ParseCommands(InterfaceArgs(typeof(MockChildProcess), string.Empty), child));
                        Assert.IsTrue(server.Execute(child));
                        IProgressReporter progressReporter = MockChildProcess.LastInstance.ProgressReporter;
                        Assert.IsNotNull(progressReporter);

                        string progress = "test string";
                        progressReporter.ReportProgress(progress);
                        Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                        Assert.AreEqual(MessageType.Progress, message);
                        Assert.AreEqual(progress, data);

                        string progress2 = "another test for a string";
                        progressReporter.ReportProgress(progress2);
                        Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                        Assert.AreEqual(MessageType.Progress, message);
                        Assert.AreEqual(progress2, data);

                        progressReporter.ReportProgress(null);
                        Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                        Assert.AreEqual(MessageType.Progress, message);
                        Assert.IsNull(data);

                        Assert.IsTrue(parent.SendMessage(MessageType.SignalTerminate));
                        Assert.IsTrue(server.WaitForSignal(child));
                        Assert.IsTrue(server.TryTerminate(child, out data));
                        Assert.IsNull(data);
                    }
                }
            }
        }

        /// <summary>
        ///A test for ParseCommands
        ///</summary>
        [TestMethod()]
        public void ParseCommandsTest()
        {
            using (MockCommunication parent = new MockCommunication())
            {
                using (MockCommunication child = new MockCommunication(parent))
                {
                    MessageType message;
                    string data;
                    Exception ex;

                    // Valid class.
                    using (InterfaceHostServer server = new InterfaceHostServer())
                    {
                        MockChildProcess.ReserveInstance(ExecutionMode.NotSet, null, string.Empty);
                        Assert.IsTrue(server.ParseCommands(InterfaceArgs(typeof(MockChildProcess), string.Empty), child));
                    }

                    // Invalid type.
                    List<string> outArgs = new List<string>();
                    TypeArgument arg = new TypeArgument("bogus", "bogus", "bogus");
                    arg.AddArgs(outArgs);
                    outArgs.Add(string.Empty);

                    using (InterfaceHostServer server = new InterfaceHostServer())
                    {
                        Assert.IsFalse(server.ParseCommands(new Queue<string>(outArgs), child));
                    }

                    Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                    Assert.AreEqual(MessageType.AssemblyLoadError, message);

                    // Class doesn't implement interface.
                    using (InterfaceHostServer server = new InterfaceHostServer())
                    {
                        Assert.IsFalse(server.ParseCommands(InterfaceArgs(GetType(), string.Empty), child));
                    }

                    Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                    Assert.AreEqual(MessageType.InvalidTypeError, message);

                    // Unable to create instance.
                    using (InterfaceHostServer server = new InterfaceHostServer())
                    {
                        // Don't reserve, constructor will throw.
                        Assert.IsFalse(server.ParseCommands(InterfaceArgs(typeof(MockChildProcess), string.Empty), child));
                    }

                    // Exceptions.
                    Func<InterfaceHostServer, Queue<string>, Communication, Action> parseCommands = (_server, _args, _com) => { return () => { _server.ParseCommands(_args, _com); }; };

                    using (InterfaceHostServer server = new InterfaceHostServer())
                    {
                        TestUtilities.AssertThrows(parseCommands(server, null, child), typeof(ArgumentNullException));
                        TestUtilities.AssertThrows(parseCommands(server, new Queue<string>(), null), typeof(ArgumentNullException));
                        TestUtilities.AssertThrows(parseCommands(server, null, null), typeof(ArgumentNullException));

                        Queue<string> inArgs = InterfaceArgs(GetType(), string.Empty);
                        inArgs.Dequeue();
                        TestUtilities.AssertThrows(parseCommands(server, inArgs, child), typeof(ArgumentException));
                    }
                }
            }
        }

        /// <summary>
        ///A test for Execute
        ///</summary>
        [TestMethod()]
        public void ExecuteTest()
        {
            MessageType message;
            string data;
            Exception ex;

            Action<ExecutionMode, string, string, bool> testExecute = (mode, expectedArgs, expectedResult, sendTerminate) =>
            {
                using (MockCommunication parent = new MockCommunication())
                {
                    using (MockCommunication child = new MockCommunication(parent))
                    {
                        using (InterfaceHostServer server = new InterfaceHostServer())
                        {
                            MockChildProcess.ReserveInstance(mode, expectedResult, expectedArgs);
                            Assert.IsTrue(server.ParseCommands(InterfaceArgs(typeof(MockChildProcess), expectedArgs), child));
                            MockChildProcess instance = MockChildProcess.LastInstance;

                            Task parentTask = Task.Factory.StartNew(() =>
                            {
                                switch (mode)
                                {
                                    case ExecutionMode.Synchronous:
                                        instance.SignalExecute();
                                        break;

                                    case ExecutionMode.AsyncReturn:
                                        sendTerminate = true;
                                        break;

                                    case ExecutionMode.AsyncThread:
                                        if (!sendTerminate)
                                        {
                                            instance.SignalExecute();
                                            Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                                            Assert.AreEqual(MessageType.RequestTerminate, message);
                                            sendTerminate = true;
                                        }
                                        break;

                                    default:
                                        throw new ArgumentException("Invalid mode.");
                                }

                                if (sendTerminate)
                                {
                                    Assert.IsTrue(parent.SendMessage(MessageType.SignalTerminate));
                                }
                            });

                            Assert.IsTrue(server.Execute(child));
                            Assert.IsTrue(server.WaitForSignal(child));
                            Assert.IsTrue(server.TryTerminate(child, out data));
                            Assert.AreEqual(expectedResult, data);
                            Assert.IsTrue(parentTask.Wait(2000));
                        }
                    }
                }
            };

            // Valid execution.
            testExecute(ExecutionMode.Synchronous, "arg1 arg2", "return value", false);
            testExecute(ExecutionMode.Synchronous, "arg1 arg2", "return value", true);
            testExecute(ExecutionMode.AsyncReturn, "arg1 arg2", "return value", true);
            testExecute(ExecutionMode.AsyncThread, "arg1 arg2", "return value", false);
            testExecute(ExecutionMode.AsyncThread, "arg1 arg2", "return value", true);

            using (MockCommunication parent = new MockCommunication())
            {
                using (MockCommunication child = new MockCommunication(parent))
                {
                    // Throw on getting mode.
                    using (InterfaceHostServer server = new InterfaceHostServer())
                    {
                        MockChildProcess.ReserveInstance(ExecutionMode.AsyncThread, null, string.Empty, throwOnMode: true);
                        Assert.IsTrue(server.ParseCommands(InterfaceArgs(typeof(MockChildProcess), string.Empty), child));
                        Assert.IsFalse(server.Execute(child));
                        Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                        Assert.AreEqual(MessageType.ExecuteError, message);
                        Assert.IsInstanceOfType(ex, typeof(InvalidOperationException));
                    }

                    // Throw on execute on main thread.
                    using (InterfaceHostServer server = new InterfaceHostServer())
                    {
                        MockChildProcess.ReserveInstance(ExecutionMode.AsyncReturn, null, string.Empty, throwOnExecute: true);
                        Assert.IsTrue(server.ParseCommands(InterfaceArgs(typeof(MockChildProcess), string.Empty), child));
                        Assert.IsFalse(server.Execute(child));
                        Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                        Assert.AreEqual(MessageType.ExecuteError, message);
                        Assert.IsInstanceOfType(ex, typeof(InvalidOperationException));
                    }

                    // Throw on execute on child thread.
                    using (InterfaceHostServer server = new InterfaceHostServer())
                    {
                        MockChildProcess.ReserveInstance(ExecutionMode.AsyncThread, null, string.Empty, throwOnExecute: true);
                        Assert.IsTrue(server.ParseCommands(InterfaceArgs(typeof(MockChildProcess), string.Empty), child));
                        Assert.IsTrue(server.Execute(child));
                        Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                        Assert.AreEqual(MessageType.RequestTerminate, message);
                        Assert.IsTrue(parent.SendMessage(MessageType.SignalTerminate));
                        Assert.IsFalse(server.WaitForSignal(child));
                        Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                        Assert.AreEqual(MessageType.ExecuteError, message);
                        Assert.IsInstanceOfType(ex, typeof(InvalidOperationException));
                    }

                    // Invalid mode.
                    using (InterfaceHostServer server = new InterfaceHostServer())
                    {
                        MockChildProcess.ReserveInstance(ExecutionMode.NotSet, null, string.Empty);
                        Assert.IsTrue(server.ParseCommands(InterfaceArgs(typeof(MockChildProcess), string.Empty), child));
                        Assert.IsFalse(server.Execute(child));
                        Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                        Assert.AreEqual(MessageType.ExecuteError, message);
                        Assert.IsInstanceOfType(ex, typeof(InvalidOperationException));
                    }

                    // Exceptions.
                    Func<InterfaceHostServer, Communication, Action> execute = (_server, _com) => { return () => { _server.Execute(_com); }; };

                    using (InterfaceHostServer server = new InterfaceHostServer())
                    {
                        TestUtilities.AssertThrows(execute(server, child), typeof(InvalidOperationException));
                        
                        MockChildProcess.ReserveInstance(ExecutionMode.AsyncReturn, null, string.Empty);
                        Assert.IsTrue(server.ParseCommands(InterfaceArgs(typeof(MockChildProcess), string.Empty), child));
                        TestUtilities.AssertThrows(execute(server, null), typeof(ArgumentNullException));
                    }
                }
            }
        }

        #region Helper classes

        /// <summary>
        /// A class for testing interactions with IChildProcess.
        /// Must call ReserveInstance prior to instantiating.
        /// </summary>
        /// <see cref="IChildProcess"/>

        public class MockChildProcess : IChildProcess
        {
            [ThreadStatic]
            private static bool _reserveCalled;

            [ThreadStatic]
            private static string _nextResult;
            
            [ThreadStatic]
            private static bool _nextThrowOnEnd;

            [ThreadStatic]
            private static bool _nextThrowOnMode;

            [ThreadStatic]
            private static bool _nextThrowOnResult;
            
            [ThreadStatic]
            private static ExecutionMode _nextMode;
            
            [ThreadStatic]
            private static bool _nextThrowOnExecute;
            
            [ThreadStatic]
            private static string _nextExpectedArguments;

            [ThreadStatic]
            private static MockChildProcess _lastInstance;

            private Thread _thread;
            private string _result;
            private bool _throwOnEnd;
            private bool _throwOnMode;
            private bool _throwOnResult;
            private ExecutionMode _mode;
            private bool _throwOnExecute;
            private string _expectedArguments;
            private ManualResetEventSlim _event;
            private IProgressReporter _progressReporter;

            /// <see cref="IChildProcess.Result"/>

            public string Result
            {
                get
                {
                    if (_throwOnResult)
                    {
                        throw new InvalidOperationException("throwOnResult set");
                    }

                    return _result; 
                }
            }

            /// <see cref="IChildProcess.Mode"/>

            public ExecutionMode Mode
            {
                get
                {
                    if (_throwOnMode)
                    {
                        throw new InvalidOperationException("throwOnMode set");
                    }

                    return _mode;
                }
            }

            /// <summary>
            /// Gets the progress reporter given to the process. Only valid during execute.
            /// </summary>

            public IProgressReporter ProgressReporter
            {
                get
                {
                    if (_progressReporter == null)
                    {
                        throw new InvalidOperationException("Must be in execute to get progress reporter.");
                    }

                    return _progressReporter;
                }
            }

            /// <summary>
            /// Gets the last instance created on this thread.
            /// Only valid once before the next call to ReserveInstance.
            /// </summary>

            public static MockChildProcess LastInstance
            {
                get
                {
                    MockChildProcess instance = _lastInstance;
                    _lastInstance = null;
                    return instance;
                }
            }

            /// <summary>
            /// Reserves the options for the next mock child process created on this thread.
            /// </summary>
            /// <param name="mode">The execution mode for the child process.</param>
            /// <param name="result">The result to return after executing.</param>
            /// <param name="throwOnExecute">True to throw an exception when Execute is called.</param>
            /// <param name="throwOnEnd">True to throw an exception when EndExecute is called.</param>

            public static void ReserveInstance(ExecutionMode mode, string result, string expectedArguments, bool throwOnMode = false, bool throwOnExecute = false, bool throwOnEnd = false, bool throwOnResult = false)
            {
                _nextMode = mode;
                _nextResult = result;
                _lastInstance = null;
                _reserveCalled = true;
                _nextThrowOnEnd = throwOnEnd;
                _nextThrowOnMode = throwOnMode;
                _nextThrowOnResult = throwOnResult;
                _nextThrowOnExecute = throwOnExecute;
                _nextExpectedArguments = expectedArguments;
            }
            
            /// <summary>
            /// Creates a new mock child process using options set by ReserveInstance.
            /// </summary>
            /// <exception cref="InvalidOperationException">if ReserveInstance was not called.</exception>

            public MockChildProcess()
            {
                if (!_reserveCalled)
                {
                    throw new InvalidOperationException("Must call ReserveInstance before each constructor call.");
                }

                _mode = _nextMode;
                _lastInstance = this;
                _result = _nextResult;
                _reserveCalled = false;
                _throwOnEnd = _nextThrowOnEnd;
                _throwOnMode = _nextThrowOnMode;
                _throwOnResult = _nextThrowOnResult;
                _event = new ManualResetEventSlim();
                _throwOnExecute = _nextThrowOnExecute;
                _expectedArguments = _nextExpectedArguments;
            }

            /// <summary>
            /// Signals Execute to return without waiting for EndExecute.
            /// </summary>

            public void SignalExecute()
            {
                _event.Set();
            }

            /// <see cref="IChildProcess.Execute"/>

            public void Execute(string arguments, IProgressReporter progressReporter)
            {
                Assert.AreEqual(_expectedArguments, arguments);

                if (_throwOnExecute)
                {
                    throw new InvalidOperationException("throwOnExecute set");
                }

                _progressReporter = progressReporter;

                switch (Mode)
                {
                    case ExecutionMode.Synchronous:
                    case ExecutionMode.AsyncThread:
                        _event.Wait();
                        _progressReporter = null;
                        break;

                    case ExecutionMode.AsyncReturn:
                        _thread = new Thread(() => { _event.Wait(); });
                        _thread.Start();
                        break;

                    default:
                        throw new InvalidOperationException("Execute should not have been called with invalid mode.");
                }
            }

            /// <see cref="IChildProcess.EndExecution"/>

            public void EndExecution()
            {
                _progressReporter = null;

                if (_throwOnEnd)
                {
                    throw new InvalidOperationException("throwOnEnd set");
                }

                _event.Set();

                if (Mode == ExecutionMode.AsyncReturn)
                {
                    _thread.Join();
                }
            }
        }

        #endregion
    }
}
