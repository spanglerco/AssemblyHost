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
using System.ServiceModel;
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
    ///This is a test class for WcfHostServerTest and is intended
    ///to contain all WcfHostServerTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WcfHostServerTest
    {
        /// <summary>
        /// Creates the arguments Queue required for WCFHostServer.
        /// </summary>
        /// <param name="serviceType">The type of the WCF service.</param>
        /// <param name="serviceUri">The Uri on which to host the service.</param>
        /// <returns>The args Queue that can be passed to the WCFHostServer constructor.</returns>

        private Queue<string> WCFArgs(Type serviceType, Uri serviceUri)
        {
            List<string> outArgs = new List<string>();
            TypeArgument arg = new TypeArgument(serviceType);
            arg.AddArgs(outArgs);
            outArgs.Add(serviceUri.ToString());
            return new Queue<string>(outArgs);
        }

        /// <summary>
        /// Creates a WcfChildContract for the given type hosted at the given Uri.
        /// </summary>
        /// <typeparam name="TContract">The type of the service contract.</typeparam>
        /// <param name="serviceUri">The Uri on which the service was hosted.</param>
        /// <returns>The created contract.</returns>

        private WcfChildContract<TContract> CreateContract<TContract>(Uri serviceUri) where TContract : class
        {
            return new WcfChildContract<TContract>(ChannelFactory<TContract>.CreateChannel(new NetNamedPipeBinding(), new EndpointAddress(serviceUri)));
        }

        /// <summary>
        ///A test for ParseCommands
        ///</summary>
        [TestMethod()]
        public void ParseCommandsTest()
        {
            // Note: This test should not take a long time. If it appears to hang for several
            // seconds, that's a bug and probably indicates a channel isn't being closed.

            using (MockCommunication parent = new MockCommunication())
            {
                using (MockCommunication child = new MockCommunication(parent))
                {
                    MessageType message;
                    string data;
                    Exception ex;
                    Uri serviceUri = new Uri("net.pipe://localhost/assembly.host.test");

                    for (int x = 0; x < 2; x++)
                    {
                        using (WcfHostServer server = new WcfHostServer())
                        {
                            // Fail to connect before server is created.
                            TestUtilities.AssertThrows(() => { CreateContract<ITestContract>(serviceUri); }, typeof(CommunicationException));

                            int newValue = 5;
                            Assert.IsTrue(server.ParseCommands(WCFArgs(typeof(MockWcfService), serviceUri), child));

                            // Valid connection.
                            using (WcfChildContract<ITestContract> contract = CreateContract<ITestContract>(serviceUri))
                            {
                                Assert.IsNotNull(contract.Contract);
                                Assert.IsNotNull(MockWcfService.Instance);
                                Assert.AreEqual(MockWcfService.Instance.GetValue(), contract.Contract.GetValue());

                                // Valid second simultaneous connection.
                                using (WcfChildContract<ITestContract2> contract2 = CreateContract<ITestContract2>(serviceUri))
                                {
                                    Assert.IsNotNull(contract2.Contract);
                                    contract2.Contract.SetValue(newValue);
                                    Assert.AreEqual(newValue, MockWcfService.Instance.GetValue());
                                    contract2.Contract.IncrementValue();
                                }

                                // First connection remains valid after second is closed.
                                Assert.AreEqual(newValue + 1, contract.Contract.GetValue());
                            }
                        }

                        // Fail to connect after server is shut down.
                        TestUtilities.AssertThrows(() => { CreateContract<ITestContract>(serviceUri); }, typeof(CommunicationException));
                    }

                    // Invalid type.
                    List<string> outArgs = new List<string>();
                    TypeArgument arg = new TypeArgument("bogus", "bogus", "bogus");
                    arg.AddArgs(outArgs);
                    outArgs.Add(serviceUri.ToString());

                    using (WcfHostServer server = new WcfHostServer())
                    {
                        Assert.IsFalse(server.ParseCommands(new Queue<string>(outArgs), child));
                    }

                    Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                    Assert.AreEqual(MessageType.AssemblyLoadError, message);

                    // Unable to start service host.
                    using (WcfHostServer server = new WcfHostServer())
                    {
                        Assert.IsFalse(server.ParseCommands(WCFArgs(typeof(MockWcfService), new Uri("ftp://bogus")), child));
                        Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                        Assert.AreEqual(MessageType.InvalidExecuteError, message);
                    }

                    // Exceptions.
                    Func<WcfHostServer, Queue<string>, Communication, Action> parseCommands = (_server, _args, _com) => { return () => { _server.ParseCommands(_args, _com); }; };

                    using (WcfHostServer server = new WcfHostServer())
                    {
                        TestUtilities.AssertThrows(parseCommands(server, null, child), typeof(ArgumentNullException));
                        TestUtilities.AssertThrows(parseCommands(server, new Queue<string>(), null), typeof(ArgumentNullException));
                        TestUtilities.AssertThrows(parseCommands(server, null, null), typeof(ArgumentNullException));

                        Queue<string> inArgs = WCFArgs(GetType(), serviceUri);
                        inArgs.Dequeue();
                        TestUtilities.AssertThrows(parseCommands(server, inArgs, child), typeof(ArgumentException));
                    }
                }
            }
        }

        /// <summary>
        ///A test for Terminate
        ///</summary>
        [TestMethod()]
        public void TerminateTest()
        {
            using (MockCommunication parent = new MockCommunication())
            {
                using (MockCommunication child = new MockCommunication(parent))
                {
                    MessageType message;
                    string data;
                    Exception ex;
                    Uri serviceUri = new Uri("net.pipe://localhost/assembly.host.test");

                    // Normal terminate.
                    using (WcfHostServer server = new WcfHostServer())
                    {
                        Assert.IsTrue(server.ParseCommands(WCFArgs(typeof(MockWcfService), serviceUri), child));
                        Assert.IsTrue(server.Execute(child));
                        Assert.IsTrue(parent.SendMessage(MessageType.SignalTerminate));
                        Assert.IsTrue(server.WaitForSignal(child));
                        Assert.IsTrue(server.TryTerminate(child, out data));
                        Assert.IsNull(data);
                    }

                    // Terminate after fault.
                    using (WcfHostServer server = new WcfHostServer())
                    {
                        Assert.IsFalse(server.ParseCommands(WCFArgs(typeof(MockWcfService), new Uri("ftp://bogus")), child));
                        Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                        Assert.AreEqual(MessageType.InvalidExecuteError, message);
                        Assert.IsTrue(server.TryTerminate(child, out data));
                        Assert.IsNull(data);
                    }
                }
            }
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
                    Uri serviceUri = new Uri("net.pipe://localhost/assembly.host.test");

                    // Send an unknown message.
                    using (WcfHostServer server = new WcfHostServer())
                    {
                        Assert.IsTrue(server.ParseCommands(WCFArgs(typeof(MockWcfService), serviceUri), child));
                        Assert.IsTrue(server.Execute(child));
                        Assert.IsTrue(parent.SendMessage(MessageType.NotSet));
                        Task thread = Task.Factory.StartNew(() => { Assert.IsTrue(server.WaitForSignal(child)); });
                        Assert.IsTrue(parent.SendMessage(MessageType.NotSet));
                        Assert.IsFalse(thread.Wait(500));
                        Assert.IsTrue(parent.SendMessage(MessageType.SignalTerminate));
                        Assert.IsTrue(thread.Wait(100));
                    }

                    using (WcfHostServer server = new WcfHostServer())
                    {
                        Func<WcfHostServer, Communication, Action> waitForSignal = (_server, _com) => { return () => { _server.WaitForSignal(_com); }; };

                        TestUtilities.AssertThrows(waitForSignal(server, null), typeof(ArgumentNullException));
                    }
                }
            }
        }
    }
}
