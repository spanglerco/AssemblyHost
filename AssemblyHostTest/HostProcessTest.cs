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
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SpanglerCo.AssemblyHost;
using SpanglerCo.UnitTests.AssemblyHost.Mock;

namespace SpanglerCo.UnitTests.AssemblyHost
{
    /// <summary>
    ///This is a test class for HostProcessTest and is intended
    ///to contain all HostProcessTest Unit Tests
    ///</summary>
    [TestClass()]
    public class HostProcessTest
    {
        /// <summary>
        ///A test for ChildProcess
        ///</summary>
        [TestMethod()]
        public void ChildProcessTest()
        {
            Process p;
            MethodHostProcess process;

            using (process = new MethodHostProcess(new MethodArgument(typeof(MockMethodClass).GetMethod("StaticNoReturn"))))
            {
                TestUtilities.AssertThrows(() => { p = process.ChildProcess; }, typeof(InvalidOperationException));
                process.Start(true);
                p = process.ChildProcess;
            }

            TestUtilities.AssertThrows(() => { p.WaitForExit(0); }, typeof(InvalidOperationException));
        }

        /// <summary>
        ///A test for WaitStopped
        ///</summary>
        [TestMethod()]
        public void WaitStoppedTest()
        {
            // We can't mock HostProcess because the HostProcessFactory has to know about it.
            // Use the method host for testing return values and errors, and WCF host for blocking.

            // WaitStopped includes return value.
            using (MethodHostProcess process = new MethodHostProcess(new MethodArgument(typeof(MockMethodClass).GetMethod("StaticString"))))
            {
                process.Start(false);

                for (int x = 0; x < 3; x++)
                {
                    Assert.AreEqual("7", process.WaitStopped(true));
                    Assert.AreEqual(HostProcessStatus.Stopped, process.Status);
                    Assert.AreEqual("7", process.ExecutionResult);
                    Assert.IsNull(process.Error);
                }
            }

            // WaitStopped no return value.
            using (MethodHostProcess process = new MethodHostProcess(new MethodArgument(typeof(MockMethodClass).GetMethod("StaticNoString"))))
            {
                process.Start(false);

                for (int x = 0; x < 3; x++)
                {
                    Assert.IsNull(process.WaitStopped(true));
                    Assert.AreEqual(HostProcessStatus.Stopped, process.Status);
                    Assert.IsNull(process.ExecutionResult);
                    Assert.IsNull(process.Error);
                }
            }

            // WaitStopped exceptions thrown.
            using (MethodHostProcess process = new MethodHostProcess(new MethodArgument(typeof(MockMethodClass).GetMethod("Throw"))))
            {
                process.Start(false);

                for (int x = 0; x < 3; x++)
                {
                    TestUtilities.AssertThrows(() => { process.WaitStopped(true); }, typeof(InvalidOperationException));
                    Assert.AreEqual(HostProcessStatus.Error, process.Status);
                    Assert.IsNull(process.ExecutionResult);
                    Assert.IsInstanceOfType(process.Error, typeof(InvalidOperationException));
                }
            }

            // WaitStopped exceptions not thrown.
            using (MethodHostProcess process = new MethodHostProcess(new MethodArgument(typeof(MockMethodClass).GetMethod("Throw"))))
            {
                process.Start(false);

                for (int x = 0; x < 3; x++)
                {
                    Assert.IsNull(process.WaitStopped(false));
                    Assert.AreEqual(HostProcessStatus.Error, process.Status);
                    Assert.IsNull(process.ExecutionResult);
                    Assert.IsInstanceOfType(process.Error, typeof(InvalidOperationException));
                }
            }

            // WaitStopped blocks and throws after dispose.
            {
                WcfHostProcess process;
                using (process = new WcfHostProcess(new TypeArgument(typeof(MockWcfService))))
                {
                    process.Start(true);
                    Task waitTask = Task.Factory.StartNew(() => { Assert.IsNull(process.WaitStopped(true)); });
                    Assert.IsFalse(waitTask.Wait(500));
                    Assert.AreEqual(HostProcessStatus.Executing, process.Status);
                    process.Stop();
                    Assert.IsTrue(waitTask.Wait(10000));
                    Assert.AreEqual(HostProcessStatus.Stopped, process.Status);
                    Assert.IsNull(process.WaitStopped(true));
                }

                TestUtilities.AssertThrows(() => { process.WaitStopped(false); }, typeof(ObjectDisposedException));
            }
        }

        /// <summary>
        ///A test for WaitStopped
        ///</summary>
        [TestMethod()]
        public void WaitStoppedTimeoutTest()
        {
            // We can't mock HostProcess because the HostProcessFactory has to know about it.
            // Use the method host for testing return values and errors, and WCF host for blocking.

            // WaitStopped includes return value.
            using (MethodHostProcess process = new MethodHostProcess(new MethodArgument(typeof(MockMethodClass).GetMethod("StaticString"))))
            {
                process.Start(true);

                for (int x = 0; x < 3; x++)
                {
                    Assert.IsTrue(process.WaitStopped(x == 0 ? 10000 : 0, true));
                    Assert.AreEqual(HostProcessStatus.Stopped, process.Status);
                    Assert.AreEqual("7", process.ExecutionResult);
                    Assert.IsNull(process.Error);
                }
            }

            // WaitStopped exceptions thrown.
            using (MethodHostProcess process = new MethodHostProcess(new MethodArgument(typeof(MockMethodClass).GetMethod("Throw"))))
            {
                process.Start(true);

                for (int x = 0; x < 3; x++)
                {
                    TestUtilities.AssertThrows(() => { process.WaitStopped(x == 0 ? 10000 : 0, true); }, typeof(InvalidOperationException));
                    Assert.AreEqual(HostProcessStatus.Error, process.Status);
                    Assert.IsNull(process.ExecutionResult);
                    Assert.IsInstanceOfType(process.Error, typeof(InvalidOperationException));
                }
            }

            // WaitStopped exceptions not thrown.
            using (MethodHostProcess process = new MethodHostProcess(new MethodArgument(typeof(MockMethodClass).GetMethod("Throw"))))
            {
                process.Start(true);

                for (int x = 0; x < 3; x++)
                {
                    Assert.IsTrue(process.WaitStopped(x == 0 ? 10000 : 0, false));
                    Assert.AreEqual(HostProcessStatus.Error, process.Status);
                    Assert.IsNull(process.ExecutionResult);
                    Assert.IsInstanceOfType(process.Error, typeof(InvalidOperationException));
                }
            }

            // WaitStopped blocks and throws after dispose.
            {
                WcfHostProcess process;
                using (process = new WcfHostProcess(new TypeArgument(typeof(MockWcfService))))
                {
                    process.Start(true);
                    Assert.IsFalse(process.WaitStopped(500, true));
                    Assert.AreEqual(HostProcessStatus.Executing, process.Status);
                    process.Stop();
                    Assert.IsTrue(process.WaitStopped(10000, true));
                    Assert.AreEqual(HostProcessStatus.Stopped, process.Status);
                    Assert.IsTrue(process.WaitStopped(0, true));
                }

                // This should not wait the minute before throwing.
                TestUtilities.AssertThrows(() => { process.WaitStopped(60000, false); }, typeof(ObjectDisposedException));
            }
        }
    }
}
