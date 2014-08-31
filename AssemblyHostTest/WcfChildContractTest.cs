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
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SpanglerCo.AssemblyHost;
using SpanglerCo.UnitTests.AssemblyHost.Mock;

namespace SpanglerCo.UnitTests.AssemblyHost
{
    /// <summary>
    ///This is a test class for WcfChildContractTest and is intended
    ///to contain all WcfChildContractTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WcfChildContractTest
    {
        /// <summary>
        ///A test for WcfChildContract Constructor
        ///</summary>
        [TestMethod()]
        public void WcfChildContractConstructorTest()
        {
            // Note: This test should not take a long time. If it appears to hang for several
            // seconds, that's a bug and probably indicates a channel isn't being closed.

            Uri serviceUri = new Uri("net.pipe://localhost/assembly.host.test");
            NetNamedPipeBinding binding = new NetNamedPipeBinding();
            EndpointAddress serviceEndpoint = new EndpointAddress(serviceUri);

            using (ServiceHost host = new ServiceHost(typeof(MockWcfService), serviceUri))
            {
                host.AddDefaultEndpoints();
                host.Open();

                // Valid contracts.
                for (int x = 0; x < 2; x++)
                {
                    using (WcfChildContract<ITestContract> contract = new WcfChildContract<ITestContract>(ChannelFactory<ITestContract>.CreateChannel(binding, serviceEndpoint)))
                    {
                        Assert.IsNotNull(contract.Contract);
                        Assert.IsNotNull(MockWcfService.Instance);
                        Assert.AreEqual(MockWcfService.Instance.GetValue(), contract.Contract.GetValue());
                    }
                }

                for (int x = 0; x < 2; x++)
                {
                    using (WcfChildContract<ITestContract2> contract = new WcfChildContract<ITestContract2>(ChannelFactory<ITestContract2>.CreateChannel(binding, serviceEndpoint)))
                    {
                        int expectedValue = 5;
                        Assert.IsNotNull(contract.Contract);
                        Assert.IsNotNull(MockWcfService.Instance);
                        contract.Contract.SetValue(expectedValue);
                        Assert.AreEqual(expectedValue, MockWcfService.Instance.GetValue());
                    }
                }

                // Not implemented contract.
                using (WcfChildContract<IUnusedContract> contract = new WcfChildContract<IUnusedContract>(ChannelFactory<IUnusedContract>.CreateChannel(binding, serviceEndpoint)))
                {
                    TestUtilities.AssertThrows(() => { contract.Contract.Echo("test"); }, typeof(ActionNotSupportedException));
                }

                // Invalid arguments.
                TestUtilities.AssertThrows(() => { new WcfChildContract<ITestContract>(null); }, typeof(ArgumentNullException));
                TestUtilities.AssertThrows(() => { new WcfChildContract<ITestContract>(MockWcfService.Instance); }, typeof(ArgumentException));
                TestUtilities.AssertThrows(() => { new WcfChildContract<INonContract>(ChannelFactory<INonContract>.CreateChannel(binding, serviceEndpoint)); }, typeof(InvalidOperationException));

                // Already open channel.
                ITestContract openContract = ChannelFactory<ITestContract>.CreateChannel(binding, serviceEndpoint);
                ((ICommunicationObject)openContract).Open();
                TestUtilities.AssertThrows(() => { new WcfChildContract<ITestContract>(openContract); }, typeof(ArgumentException));
                ((ICommunicationObject)openContract).Close();

                {
                    // Close failure on dispose should not throw.
                    WcfChildContract<ITestContract> contract;
                    using (contract = new WcfChildContract<ITestContract>(ChannelFactory<ITestContract>.CreateChannel(binding, serviceEndpoint)))
                    {
                        Assert.AreEqual(MockWcfService.Instance.GetValue(), contract.Contract.GetValue());
                        host.Abort();
                    }

                    // Disposed.
                    TestUtilities.AssertThrows(() => { contract.Contract.GetValue(); }, typeof(ObjectDisposedException));
                }
            }

            // Endpoint not listening.
            TestUtilities.AssertThrows(() => { new WcfChildContract<ITestContract>(ChannelFactory<ITestContract>.CreateChannel(binding, serviceEndpoint)); }, typeof(CommunicationException));
        }
    }
}
