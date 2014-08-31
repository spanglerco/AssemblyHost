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
using System.Reflection;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SpanglerCo.AssemblyHost;
using SpanglerCo.AssemblyHost.Ipc;
using SpanglerCo.AssemblyHost.Child;
using SpanglerCo.UnitTests.AssemblyHost.Mock;

namespace SpanglerCo.UnitTests.AssemblyHost
{
    /// <summary>
    ///This is a test class for MethodHostServerTest and is intended
    ///to contain all MethodHostServerTest Unit Tests
    ///</summary>
    [TestClass()]
    public class MethodHostServerTest
    {
        /// <summary>
        /// Creates the arguments Queue required for MethodHostServer.
        /// </summary>
        /// <param name="containingType">The type containing the method to call.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <returns>The args Queue that can be passed to the MethodHostServer constructor.</returns>

        private Queue<string> MethodArgs(Type containingType, string methodName)
        {
            List<string> outArgs = new List<string>();
            MethodArgument arg = new MethodArgument(containingType.GetMethod(methodName));
            arg.AddArgs(outArgs);
            return new Queue<string>(outArgs);
        }

        /// <summary>
        ///A test for Execute
        ///</summary>
        [TestMethod()]
        public void ExecuteTest()
        {
            using (MockCommunication parent = new MockCommunication())
            {
                using (MockCommunication child = new MockCommunication(parent))
                {
                    MessageType message;
                    string data;
                    Exception ex;

                    Action<string, string> testReturn = (methodName, expected) =>
                    {
                        using (MethodHostServer server = new MethodHostServer())
                        {
                            Assert.IsTrue(server.ParseCommands(MethodArgs(typeof(MockMethodClass), methodName), child));
                            Assert.IsTrue(server.Execute(child));
                            Assert.IsTrue(server.TryTerminate(child, out data));
                            Assert.AreEqual(expected, data);
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
                    using (MethodHostServer server = new MethodHostServer())
                    {
                        Assert.IsTrue(server.ParseCommands(MethodArgs(typeof(MockMethodClass), "Throw"), child));
                        Assert.IsFalse(server.Execute(child));
                        Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                        Assert.AreEqual(MessageType.ExecuteError, message);
                        Assert.IsInstanceOfType(ex, typeof(InvalidOperationException));
                    }

                    // Exceptions.
                    Func<MethodHostServer, Communication, Action> execute = (_server, _com) => { return () => { _server.Execute(_com); }; };

                    using (MethodHostServer server = new MethodHostServer())
                    {
                        TestUtilities.AssertThrows(execute(server, child), typeof(InvalidOperationException));
                        Assert.IsTrue(server.ParseCommands(MethodArgs(typeof(MockMethodClass), "String"), child));
                        TestUtilities.AssertThrows(execute(server, null), typeof(ArgumentNullException));
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

                    // Valid static method.
                    using (MethodHostServer server = new MethodHostServer())
                    {
                        Assert.IsTrue(server.ParseCommands(MethodArgs(typeof(MockMethodClass), "StaticNoReturn"), child)); 
                    }

                    // Valid instance method.
                    using (MethodHostServer server = new MethodHostServer())
                    {
                        Assert.IsTrue(server.ParseCommands(MethodArgs(typeof(MockMethodClass), "NoReturn"), child));
                    }

                    // Invalid type.
                    List<string> outArgs = new List<string>();
                    MethodArgument arg = new MethodArgument("bogus", "bogus", "bogus", "bogus", true);
                    arg.AddArgs(outArgs);
                    
                    using (MethodHostServer server = new MethodHostServer())
                    {
                        Assert.IsFalse(server.ParseCommands(new Queue<string>(outArgs), child));
                    }

                    Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                    Assert.AreEqual(MessageType.AssemblyLoadError, message);

                    // Invalid method.
                    outArgs.Clear();
                    arg = new MethodArgument(new TypeArgument(typeof(MockMethodClass)), "bogus", true);
                    arg.AddArgs(outArgs);
                    
                    using (MethodHostServer server = new MethodHostServer())
                    {
                        Assert.IsFalse(server.ParseCommands(new Queue<string>(outArgs), child));
                    }

                    Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                    Assert.AreEqual(MessageType.InvalidExecuteError, message);

                    // Unable to create instance.
                    using (MethodHostServer server = new MethodHostServer())
                    {
                        Assert.IsFalse(server.ParseCommands(MethodArgs(typeof(MockInvalidMethodClass), "Method"), child));
                    }

                    Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                    Assert.AreEqual(MessageType.InvalidTypeError, message);

                    // Exceptions.
                    Func<MethodHostServer, Queue<string>, Communication, Action> parseCommands = (_server, _args, _com) => { return () => { _server.ParseCommands(_args, _com); }; };

                    using (MethodHostServer server = new MethodHostServer())
                    {
                        TestUtilities.AssertThrows(parseCommands(server, null, child), typeof(ArgumentNullException));
                        TestUtilities.AssertThrows(parseCommands(server, new Queue<string>(), null), typeof(ArgumentNullException));
                        TestUtilities.AssertThrows(parseCommands(server, null, null), typeof(ArgumentNullException)); 
                    }
                }
            }
        }
    }
}
