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
using SpanglerCo.UnitTests.AssemblyHost.Mock;

namespace SpanglerCo.UnitTests.AssemblyHost
{
    /// <summary>
    ///This is a test class for MethodHostProcessTest and is intended
    ///to contain all MethodHostProcessTest Unit Tests
    ///</summary>
    [TestClass()]
    public class MethodHostProcessTest
    {
        /// <summary>
        ///A test for MethodHostProcess Constructor
        ///</summary>
        [TestMethod()]
        public void MethodHostProcessFunctionalTest()
        {
            // This test ensures we go through every expected HostProcessStatus (and no others),
            // that we get the correct result, and that the child process exits.

            bool expectError = false;
            AutoResetEvent waitEvent = new AutoResetEvent(false);
            HostProcessStatus expectedStatus = HostProcessStatus.NotStarted;
            EventHandler statusChanged = new EventHandler((sender, args) =>
            {
                MethodHostProcess process = sender as MethodHostProcess;
                Assert.IsNotNull(process);
                Assert.AreEqual(expectedStatus, process.Status);

                switch (process.Status)
                {
                    case HostProcessStatus.Starting:
                        expectedStatus = HostProcessStatus.Executing;
                        break;

                    case HostProcessStatus.Executing:
                        expectedStatus = expectError ? HostProcessStatus.Error : HostProcessStatus.Stopped;
                        break;

                    case HostProcessStatus.Stopped:
                    case HostProcessStatus.Error:
                        waitEvent.Set();
                        break;

                    default:
                        Assert.Fail("Unexpected status {0}", process.Status);
                        break;
                }
            });

            Action<string, string> testReturn = (methodName, expected) =>
            {
                using (MethodHostProcess process = new MethodHostProcess(new MethodArgument(typeof(MockMethodClass).GetMethod(methodName))))
                {
                    Assert.AreEqual(HostProcessStatus.NotStarted, process.Status);
                    expectedStatus = HostProcessStatus.Starting;
                    process.StatusChanged += statusChanged;
                    process.Start(false);
                    Assert.IsTrue(waitEvent.WaitOne(10000));
                    Assert.AreEqual(HostProcessStatus.Stopped, process.Status);
                    Assert.IsNull(process.Error);
                    Assert.AreEqual(expected, process.ExecutionResult);
                    TestUtilities.AssertThrows(() => { process.Start(false); }, typeof(InvalidOperationException));
                    Assert.IsTrue(process.ChildProcess.WaitForExit(10000));
                }
            };

            // Valid methods.
            testReturn("StaticNoReturn", null);
            testReturn("StaticString", "7");
            testReturn("StaticNull", null);
            testReturn("NoReturn", null);
            testReturn("String", "7");
            testReturn("Null", null);

            // Method throws an exception.
            using (MethodHostProcess process = new MethodHostProcess(new MethodArgument(typeof(MockMethodClass).GetMethod("Throw"))))
            {
                expectError = true;
                Assert.AreEqual(HostProcessStatus.NotStarted, process.Status);
                expectedStatus = HostProcessStatus.Starting;
                process.StatusChanged += statusChanged;
                process.Start(false);
                Assert.IsTrue(waitEvent.WaitOne(100000));
                Assert.AreEqual(HostProcessStatus.Error, process.Status);
                Assert.IsNull(process.ExecutionResult);
                Assert.IsInstanceOfType(process.Error, typeof(InvalidOperationException));
                Assert.IsTrue(process.ChildProcess.WaitForExit(10000));
            }

            TestUtilities.AssertThrows(() => { new MethodHostProcess(null); }, typeof(ArgumentNullException));

            using (MethodHostProcess process = new MethodHostProcess(new MethodArgument(typeof(MockMethodClass).GetMethod("NoReturn"))))
            { }
        }

        /// <summary>
        ///A test for MethodHostProcess Constructor
        ///</summary>
        [TestMethod()]
        public void MethodHostProcessStartInfoTest()
        {
            AutoResetEvent waitEvent = new AutoResetEvent(false);
            HostProcessStatus expectedStatus = HostProcessStatus.NotStarted;
            EventHandler statusChanged = new EventHandler((sender, args) =>
            {
                MethodHostProcess process = sender as MethodHostProcess;
                Assert.IsNotNull(process);
                Assert.AreEqual(expectedStatus, process.Status);

                switch (process.Status)
                {
                    case HostProcessStatus.Starting:
                        expectedStatus = HostProcessStatus.Executing;
                        break;

                    case HostProcessStatus.Executing:
                        expectedStatus = HostProcessStatus.Stopped;
                        break;

                    case HostProcessStatus.Stopped:
                        waitEvent.Set();
                        break;

                    default:
                        Assert.Fail("Unexpected status {0}", process.Status);
                        break;
                }
            });

            string expected = "environment variable value";
            ProcessStartInfo info = new ProcessStartInfo();
            info.EnvironmentVariables.Add("Test", expected);

            using (MethodHostProcess process = new MethodHostProcess(new MethodArgument(typeof(MockMethodClass).GetMethod("EnvironmentVariable")), info))
            {
                Assert.AreEqual(HostProcessStatus.NotStarted, process.Status);
                expectedStatus = HostProcessStatus.Starting;
                process.StatusChanged += statusChanged;
                process.Start(true);
                Assert.IsTrue(waitEvent.WaitOne(10000));
                Assert.AreEqual(HostProcessStatus.Stopped, process.Status);
                Assert.IsNull(process.Error);
                Assert.AreEqual(expected, process.ExecutionResult);
                TestUtilities.AssertThrows(() => { process.Start(false); }, typeof(InvalidOperationException));
                Assert.IsTrue(process.ChildProcess.WaitForExit(10000));
            }

            TestUtilities.AssertThrows(() => { new MethodHostProcess(null, null); }, typeof(ArgumentNullException));
            TestUtilities.AssertThrows(() => { new MethodHostProcess(null, new ProcessStartInfo()); }, typeof(ArgumentNullException));
            TestUtilities.AssertThrows(() => { new MethodHostProcess(new MethodArgument(typeof(MockMethodClass).GetMethod("Null")), null); }, typeof(ArgumentNullException));
        }
    }
}
