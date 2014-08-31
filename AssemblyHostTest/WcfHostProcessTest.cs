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
    ///This is a test class for WcfHostProcessTest and is intended
    ///to contain all WcfHostProcessTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WcfHostProcessTest
    {
        /// <summary>
        ///A test for WcfHostProcess Constructor
        ///</summary>
        [TestMethod()]
        public void WcfHostProcessFunctionalTest()
        {
            // This test ensures we go through every expected HostProcessStatus (and no others),
            // that we can create the correct channels, and that the child process exits.

            using (AutoResetEvent waitEvent = new AutoResetEvent(false))
            {
                Exception backgroundEx = null;
                HostProcessStatus expectedStatus = HostProcessStatus.NotStarted;
                EventHandler statusChanged = new EventHandler((sender, args) =>
                {
                    try
                    {
                        WcfHostProcess process = sender as WcfHostProcess;
                        Assert.IsNotNull(process);
                        Assert.AreEqual(expectedStatus, process.Status);

                        switch (process.Status)
                        {
                            case HostProcessStatus.Starting:
                                expectedStatus = HostProcessStatus.Executing;
                                break;

                            case HostProcessStatus.Executing:
                                expectedStatus = HostProcessStatus.Stopping;
                                break;

                            case HostProcessStatus.Stopping:
                                expectedStatus = HostProcessStatus.Stopped;
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

                using (WcfHostProcess process = new WcfHostProcess(new TypeArgument(typeof(MockWcfService))))
                {
                    int value;
                    Assert.AreEqual(HostProcessStatus.NotStarted, process.Status);
                    expectedStatus = HostProcessStatus.Starting;
                    process.StatusChanged += statusChanged;
                    process.Start(true);
                    Assert.AreEqual(HostProcessStatus.Executing, process.Status);
                    Assert.IsNotNull(process.ChildProcess);
                    Assert.IsFalse(process.ChildProcess.HasExited);

                    using (WcfChildContract<ITestContract> contract = process.CreateChannel<ITestContract>())
                    {
                        Assert.IsNotNull(contract);
                        Assert.IsNotNull(contract.Contract);
                        value = contract.Contract.GetValue();

                        using (WcfChildContract<ITestContract> contract2 = process.CreateChannel<ITestContract>())
                        {
                            Assert.IsNotNull(contract2);
                            Assert.IsNotNull(contract2.Contract);
                            Assert.AreEqual(contract.Contract.GetValue(), contract2.Contract.GetValue());
                        }

                        Assert.AreEqual(value, contract.Contract.GetValue());

                        using (WcfChildContract<ITestContract2> contract2 = process.CreateChannel<ITestContract2>())
                        {
                            Assert.IsNotNull(contract2);
                            Assert.IsNotNull(contract2.Contract);
                            contract2.Contract.IncrementValue();
                        }

                        Assert.AreEqual(value + 1, contract.Contract.GetValue());
                    }

                    using (WcfChildContract<ITestContract2> contract2 = process.CreateChannel<ITestContract2>())
                    {
                        value = 2772;
                        contract2.Contract.SetValue(value);
                    }

                    using (WcfChildContract<ITestContract> contract = process.CreateChannel<ITestContract>())
                    {
                        Assert.AreEqual(value, contract.Contract.GetValue());
                    }

                    TestUtilities.AssertThrows(() => { process.CreateChannel<INonContract>(); }, typeof(InvalidOperationException));
                    TestUtilities.AssertThrows(() => { process.Start(false); }, typeof(InvalidOperationException));

                    process.Stop();
                    TestUtilities.AssertThrows(() => { process.Start(false); }, typeof(InvalidOperationException));
                    Assert.IsTrue(waitEvent.WaitOne(2000));
                    Assert.AreEqual(HostProcessStatus.Stopped, process.Status);
                    Assert.IsTrue(process.ChildProcess.WaitForExit(2000));

                    TestUtilities.AssertThrows(() => { process.Start(false); }, typeof(InvalidOperationException));
                    TestUtilities.AssertThrows(() => { process.CreateChannel<ITestContract>(); }, typeof(InvalidOperationException));
                }

                if (backgroundEx != null)
                {
                    throw backgroundEx;
                }

                TestUtilities.AssertThrows(() => { new WcfHostProcess(null); }, typeof(ArgumentNullException));
            }

            using (WcfHostProcess process = new WcfHostProcess(new TypeArgument(typeof(MockWcfService))))
            { }
        }

        /// <summary>
        ///A test for WcfHostProcess Constructor
        ///</summary>
        [TestMethod()]
        public void WcfHostProcessStartInfoTest()
        {
            int expectedValue = 9928;
            ProcessStartInfo info = new ProcessStartInfo();
            info.EnvironmentVariables.Add("TestValue", expectedValue.ToString());

            using (WcfHostProcess process = new WcfHostProcess(new TypeArgument(typeof(MockWcfService)), info))
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
                    Assert.IsNotNull(process.ChildProcess);
                    Assert.IsFalse(process.ChildProcess.HasExited);

                    using (WcfChildContract<ITestContract> contract = process.CreateChannel<ITestContract>())
                    {
                        Assert.AreEqual(expectedValue, contract.Contract.GetValue());
                    }

                    process.Stop();
                    Assert.IsTrue(waitEvent.WaitOne(2000));
                    Assert.IsTrue(process.ChildProcess.WaitForExit(2000));
                }
            }
        }
    }
}
