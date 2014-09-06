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
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SpanglerCo.AssemblyHost;

namespace SpanglerCo.UnitTests.AssemblyHost
{
    /// <summary>
    ///This is a test class for InterfaceHostProcessTest and is intended
    ///to contain all InterfaceHostProcessTest Unit Tests
    ///</summary>
    [TestClass()]
    public class InterfaceHostProcessTest
    {
        /// <summary>
        ///A test for InterfaceHostProcess Constructor
        ///</summary>
        [TestMethod()]
        public void InterfaceHostProcessFunctionalTest()
        {
            // This test ensures we go through every expected HostProcessStatus (and no others),
            // all progress is reported, that we get the correct result, and that the child process exits.

            using (AutoResetEvent waitEvent = new AutoResetEvent(false))
            {
                Exception backgroundEx = null;
                bool expectStopping = false;
                bool expectEndError = false;
                bool expectExecuteError = false;
                HostProcessStatus expectedStatus = HostProcessStatus.NotStarted;

                // Event handler to verify status changes are correct.
                EventHandler statusChanged = new EventHandler((sender, args) =>
                {
                    try
                    {
                        InterfaceHostProcess process = sender as InterfaceHostProcess;
                        Assert.IsNotNull(process);
                        Assert.AreEqual(expectedStatus, process.Status);

                        switch (process.Status)
                        {
                            case HostProcessStatus.Starting:
                                expectedStatus = HostProcessStatus.Executing;
                                break;

                            case HostProcessStatus.Executing:
                                expectedStatus = expectExecuteError ? HostProcessStatus.Error : expectStopping ? HostProcessStatus.Stopping : HostProcessStatus.Stopped;
                                break;

                            case HostProcessStatus.Stopping:
                                expectedStatus = expectEndError ? HostProcessStatus.Error : HostProcessStatus.Stopped;
                                break;

                            case HostProcessStatus.Stopped:
                            case HostProcessStatus.Error:
                                waitEvent.Set();
                                break;

                            default:
                                Assert.Fail("Unexpected status {0}", process.Status);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        backgroundEx = ex;
                    }
                });

                Action<Type, string, int> testReturn = (type, argument, expectedProgress) =>
                {
                    int numProgress = 0;
                    ExecutionMode mode = ((IChildProcess)Activator.CreateInstance(type)).Mode;

                    // Event handler to verify progress updates are received correctly.
                    EventHandler<HostProgressEventArgs> hostProgress = new EventHandler<HostProgressEventArgs>((sender, args) =>
                    {
                        try
                        {
                            switch (numProgress++)
                            {
                                case 0:
                                    Assert.AreEqual(argument, args.Progress);
                                    break;

                                case 1:
                                    Assert.AreEqual(mode.ToString(), args.Progress);
                                    break;

                                case 2:
                                case 3:
                                case 4:
                                    if (args.Progress == "waiting")
                                    {
                                        Assert.IsTrue(numProgress <= 4);
                                        waitEvent.Set();
                                    }
                                    else
                                    {
                                        Assert.IsTrue(args.Progress == "Execute return" || args.Progress == "EndExecution");
                                    }
                                    break;

                                default:
                                    Assert.Fail("Too many progress reports.");
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            backgroundEx = ex;
                        }
                    });

                    // Functional test code.
                    using (InterfaceHostProcess process = new InterfaceHostProcess(new TypeArgument(type), argument))
                    {
                        Assert.AreEqual(HostProcessStatus.NotStarted, process.Status);
                        expectedStatus = HostProcessStatus.Starting;
                        process.HostProgress += hostProgress;
                        process.StatusChanged += statusChanged;
                        process.Start(false);
                        Assert.IsTrue(waitEvent.WaitOne());

                        if (expectExecuteError)
                        {
                            Assert.AreEqual(HostProcessStatus.Error, process.Status);
                            Assert.IsInstanceOfType(process.Error, typeof(InvalidOperationException));
                        }
                        else
                        {
                            if (expectStopping)
                            {
                                Assert.AreEqual(HostProcessStatus.Executing, process.Status);
                                TestUtilities.AssertThrows(() => { process.Start(false); }, typeof(InvalidOperationException));
                                Assert.IsFalse(process.ChildProcess.HasExited);
                                process.Stop();
                                Assert.IsTrue(waitEvent.WaitOne(10000));
                            }

                            if (expectEndError)
                            {
                                Assert.AreEqual(HostProcessStatus.Error, process.Status);
                                Assert.IsInstanceOfType(process.Error, typeof(InvalidOperationException));
                            }
                            else
                            {
                                Assert.AreEqual(HostProcessStatus.Stopped, process.Status);
                                Assert.IsNull(process.Error);
                                Assert.AreEqual(argument, process.ExecutionResult);
                            }
                        }

                        TestUtilities.AssertThrows(() => { process.Start(false); }, typeof(InvalidOperationException));
                        Assert.IsTrue(process.ChildProcess.WaitForExit(10000));
                    }

                    Assert.AreEqual(expectedProgress, numProgress);

                    if (backgroundEx != null)
                    {
                        throw backgroundEx;
                    }
                };

                // Valid calls.
                testReturn(typeof(MockChildProcessSynchronous), "test argument", 3);
                testReturn(typeof(MockChildProcessAsyncThread), "test argument", 3);
                expectStopping = true;
                testReturn(typeof(MockChildProcessAsyncThread), "wait", 5);
                testReturn(typeof(MockChildProcessAsyncReturn), "test argument", 4);

                // Method throws an exception during Execute.
                expectStopping = false;
                expectExecuteError = true;
                testReturn(typeof(MockChildProcessSynchronous), "throw", 1);
                testReturn(typeof(MockChildProcessAsyncThread), "throw", 1);
                testReturn(typeof(MockChildProcessAsyncReturn), "throw", 1);

                // EndExecution is not called in the synchronous case.
                expectExecuteError = false;
                testReturn(typeof(MockChildProcessSynchronous), "throwEnd", 3);

                // Method throws an exception during EndExecution.
                expectEndError = true;
                expectStopping = true;
                testReturn(typeof(MockChildProcessAsyncThread), "throwEnd", 4);
                testReturn(typeof(MockChildProcessAsyncReturn), "throwEnd", 4);

                TestUtilities.AssertThrows(() => { new InterfaceHostProcess(null, string.Empty); }, typeof(ArgumentNullException));
            }

            using (InterfaceHostProcess process = new InterfaceHostProcess(new TypeArgument(typeof(MockChildProcessSynchronous)), null))
            { }
        }

        /// <summary>
        ///A test for InterfaceHostProcess Constructor
        ///</summary>
        [TestMethod()]
        public void InterfaceHostProcessStartInfoTest()
        {
            string expected = "environment variable value";
            ProcessStartInfo info = new ProcessStartInfo();
            info.EnvironmentVariables.Add("Test", expected);

            using (InterfaceHostProcess process = new InterfaceHostProcess(new TypeArgument(typeof(MockChildProcessAsyncReturn)), info, "environment"))
            {
                using (ManualResetEvent waitEvent = new ManualResetEvent(false))
                {
                    process.StatusChanged += (sender, args) =>
                    {
                        if (process.Status == HostProcessStatus.Stopped)
                        {
                            waitEvent.Set();
                        }
                    };

                    process.Start(true);
                    Assert.AreEqual(HostProcessStatus.Executing, process.Status);
                    Assert.IsFalse(process.ChildProcess.HasExited);
                    process.Stop();
                    Assert.IsTrue(waitEvent.WaitOne(10000));
                    Assert.IsTrue(process.ChildProcess.WaitForExit(10000));
                    Assert.AreEqual(expected, process.ExecutionResult);
                }
            }
        }

        #region Helper classes

        /// <summary>
        /// A class for testing interactions with IChildProcess using the Synchronous mode.
        /// </summary>

        public class MockChildProcessSynchronous : MockChildProcessBase
        {
            /// <see cref="IChildProcess.Mode"/>

            public override ExecutionMode Mode
            {
                get
                {
                    return ExecutionMode.Synchronous;
                }
            }
        }

        /// <summary>
        /// A class for testing interactions with IChildProcess using the AsyncReturn mode.
        /// </summary>

        public class MockChildProcessAsyncReturn : MockChildProcessBase
        {
            /// <see cref="IChildProcess.Mode"/>

            public override ExecutionMode Mode
            {
                get
                {
                    return ExecutionMode.AsyncReturn;
                }
            }
        }

        /// <summary>
        /// A class for testing interactions with IChildProcess using the AsyncThread mode.
        /// </summary>

        public class MockChildProcessAsyncThread : MockChildProcessBase
        {
            /// <see cref="IChildProcess.Mode"/>

            public override ExecutionMode Mode
            {
                get
                {
                    return ExecutionMode.AsyncThread;
                }
            }
        }

        /// <summary>
        /// A class for testing interactions with IChildProcess.
        /// </summary>
        /// <see cref="IChildProcess"/>

        public abstract class MockChildProcessBase : IChildProcess
        {
            private Thread _thread;
            private bool _throwOnEnd;
            private ManualResetEventSlim _event;
            private IProgressReporter _progressReporter;

            /// <see cref="IChildProcess.Result"/>

            public string Result { get; private set; }

            /// <see cref="IChildProcess.Mode"/>

            public abstract ExecutionMode Mode { get; }

            /// <summary>
            /// Creates a new mock child process using options set by ReserveInstance.
            /// </summary>

            public MockChildProcessBase()
            {
                _event = new ManualResetEventSlim();
            }

            /// <see cref="IChildProcess.Execute"/>

            public void Execute(string arguments, IProgressReporter progressReporter)
            {
                progressReporter.ReportProgress(arguments);

                if (arguments == "throw")
                {
                    throw new InvalidOperationException("throw argument passed");
                }

                if (arguments == "throwEnd")
                {
                    _throwOnEnd = true;
                }

                if (arguments == "environment")
                {
                    Result = Environment.GetEnvironmentVariable("Test");
                }
                else
                {
                    Result = arguments;
                }

                progressReporter.ReportProgress(Mode.ToString());

                switch (Mode)
                {
                    case ExecutionMode.Synchronous:
                        // No EndExecution.
                        break;

                    case ExecutionMode.AsyncThread:
                        if (arguments == "wait" || _throwOnEnd)
                        {
                            // Wait for EndExecution.
                            _progressReporter = progressReporter;
                            progressReporter.ReportProgress("waiting");
                            _event.Wait();
                        }
                        break;

                    case ExecutionMode.AsyncReturn:
                        // Spawn a thread to wait for EndExecution then return.
                        _thread = new Thread(() => { progressReporter.ReportProgress("waiting"); _event.Wait(); });
                        _thread.IsBackground = true;
                        _thread.Start();
                        break;

                    default:
                        throw new InvalidOperationException("Execute should not have been called with invalid mode.");
                }

                progressReporter.ReportProgress("Execute return");
                _progressReporter = null;
            }

            /// <see cref="IChildProcess.EndExecution"/>

            public void EndExecution()
            {
                if (_progressReporter != null)
                {
                    _progressReporter.ReportProgress("EndExecution");
                }

                if (_throwOnEnd)
                {
                    throw new InvalidOperationException("throwEnd argument passed");
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
