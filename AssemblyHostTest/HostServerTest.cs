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
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SpanglerCo.AssemblyHost;
using SpanglerCo.AssemblyHost.Ipc;
using SpanglerCo.AssemblyHost.Child;
using SpanglerCo.UnitTests.AssemblyHost.Mock;

namespace SpanglerCo.UnitTests.AssemblyHost
{
    /// <summary>
    ///This is a test class for HostServerTest and is intended
    ///to contain all HostServerTest Unit Tests
    ///</summary>
    [TestClass()]
    public class HostServerTest
    {
        /// <summary>
        ///A test for CheckInterface
        ///</summary>
        [TestMethod()]
        public void CheckInterfaceTest()
        {
            Assert.IsTrue(MockHostServer.CheckInterface(typeof(TestInterface), typeof(IEnumerable)));
            Assert.IsTrue(MockHostServer.CheckInterface(typeof(TestInterface), typeof(IEnumerable<bool>)));
            Assert.IsTrue(MockHostServer.CheckInterface(typeof(TestInterface), typeof(IEnumerable<int>)));
            Assert.IsFalse(MockHostServer.CheckInterface(typeof(TestInterface), typeof(IEnumerator)));
            Assert.IsFalse(MockHostServer.CheckInterface(typeof(TestInterface), typeof(IEnumerable<>)));
            Assert.IsFalse(MockHostServer.CheckInterface(typeof(TestInterface), typeof(IEnumerable<short>)));
            Assert.IsTrue(MockHostServer.CheckInterface(typeof(IEnumerable<bool>), typeof(IEnumerable)));
            Assert.IsFalse(MockHostServer.CheckInterface(typeof(IEnumerable), typeof(IEnumerable)));

            Func<Type, Type, Action> checkInterface = (_type, _interface) => { return () => { MockHostServer.CheckInterface(_type, _interface); }; };

            TestUtilities.AssertThrows(checkInterface(null, typeof(IEnumerable)), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(checkInterface(typeof(IEnumerable), null), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(checkInterface(null, null), typeof(ArgumentNullException));
        }

        /// <summary>
        ///A test for TryCreateInstance
        ///</summary>
        [TestMethod()]
        public void TryCreateInstanceTest()
        {
            using (MockCommunication parent = new MockCommunication())
            {
                using (MockCommunication child = new MockCommunication(parent))
                {
                    MessageType message;
                    string data;
                    Exception ex;

                    TestInterface obj1;
                    Assert.IsTrue(MockHostServer.TryCreateInstance(typeof(TestInterface), child, out obj1));
                    Assert.IsNotNull(obj1);

                    TestThrow obj2;
                    Assert.IsFalse(MockHostServer.TryCreateInstance(typeof(TestThrow), child, out obj2));
                    Assert.IsNull(obj2);
                    Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                    Assert.AreEqual(MessageType.ExecuteError, message);

                    PrivateConstructor obj3;
                    Assert.IsFalse(MockHostServer.TryCreateInstance(typeof(PrivateConstructor), child, out obj3));
                    Assert.IsNull(obj3);
                    Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                    Assert.AreEqual(MessageType.InvalidTypeError, message);

                    NonDefaultConstructor obj4;
                    Assert.IsFalse(MockHostServer.TryCreateInstance(typeof(NonDefaultConstructor), child, out obj4));
                    Assert.IsNull(obj4);
                    Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                    Assert.AreEqual(MessageType.InvalidTypeError, message);

                    AbstractClass obj5;
                    Assert.IsFalse(MockHostServer.TryCreateInstance(typeof(AbstractClass), child, out obj5));
                    Assert.IsNull(obj5);
                    Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                    Assert.AreEqual(MessageType.InvalidTypeError, message);

                    MockHostServer obj6;
                    Assert.IsFalse(MockHostServer.TryCreateInstance(typeof(TestInterface), child, out obj6));
                    Assert.IsNull(obj6);
                    Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                    Assert.AreEqual(MessageType.InvalidTypeError, message);

                    object obj7;
                    Assert.IsFalse(MockHostServer.TryCreateInstance(typeof(List<>), child, out obj7));
                    Assert.IsNull(obj7);
                    Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                    Assert.AreEqual(MessageType.InvalidTypeError, message);

                    object obj8;
                    Assert.IsFalse(MockHostServer.TryCreateInstance(typeof(void), child, out obj8));
                    Assert.IsNull(obj8);
                    Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                    Assert.AreEqual(MessageType.InvalidTypeError, message);

                    Func<Type, Communication, Action> tryCreateInstance = (_type, _com) => { return () => { MockHostServer.TryCreateInstance(_type, _com, out obj8); }; };

                    TestUtilities.AssertThrows(tryCreateInstance(null, child), typeof(ArgumentNullException));
                    TestUtilities.AssertThrows(tryCreateInstance(GetType(), null), typeof(ArgumentNullException));
                }
            }
        }

        /// <summary>
        ///A test for TryGetMethod
        ///</summary>
        [TestMethod()]
        public void TryGetMethodTest()
        {
            using (MockCommunication parent = new MockCommunication())
            {
                using (MockCommunication child = new MockCommunication(parent))
                {
                    MethodInfo foundMethod;
                    MethodInfo method = typeof(TestInterface).GetMethod("Method");
                    Assert.IsTrue(MockHostServer.TryGetMethod(typeof(TestInterface), new MethodArgument(method), child, out foundMethod));
                    Assert.AreEqual(method, foundMethod);

                    method = typeof(TestInterface).GetMethod("MethodWithReturn");
                    Assert.IsTrue(MockHostServer.TryGetMethod(typeof(TestInterface), new MethodArgument(method), child, out foundMethod));
                    Assert.AreEqual(method, foundMethod);

                    MessageType message;
                    string data;
                    Exception ex;
                    method = typeof(TestInterface).GetMethod("MethodWithArg");
                    Assert.IsFalse(MockHostServer.TryGetMethod(typeof(TestInterface), new MethodArgument(method), child, out foundMethod));
                    Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                    Assert.AreEqual(MessageType.InvalidExecuteError, message);

                    method = typeof(TestInterface).GetMethod("StaticMethod");
                    Assert.IsTrue(MockHostServer.TryGetMethod(typeof(TestInterface), new MethodArgument(method), child, out foundMethod));
                    Assert.AreEqual(method, foundMethod);

                    Func<Type, MethodArgument, Communication, Action> tryGetMethod = (_type, _method, _com) => { return () => { MockHostServer.TryGetMethod(_type, _method, _com, out foundMethod); }; };

                    TestUtilities.AssertThrows(tryGetMethod(null, new MethodArgument(method), child), typeof(ArgumentNullException));
                    TestUtilities.AssertThrows(tryGetMethod(typeof(TestInterface), null, child), typeof(ArgumentNullException));
                    TestUtilities.AssertThrows(tryGetMethod(typeof(TestInterface), new MethodArgument(method), null), typeof(ArgumentNullException));
                    TestUtilities.AssertThrows(tryGetMethod(null, null, null), typeof(ArgumentNullException));
                }
            }
        }

        /// <summary>
        ///A test for TryLoadType
        ///</summary>
        [TestMethod()]
        public void TryLoadTypeTest()
        {
            using (MockCommunication parent = new MockCommunication())
            {
                using (MockCommunication child = new MockCommunication(parent))
                {
                    Type loadedType;
                    Assert.IsTrue(MockHostServer.TryLoadType(new TypeArgument(GetType()), child, out loadedType));
                    Assert.AreEqual(loadedType, GetType());

                    MessageType message;
                    string data;
                    Exception ex;
                    Assert.IsFalse(MockHostServer.TryLoadType(new TypeArgument(new AssemblyArgument(GetType(), HostBitness.Current), "DoesntExist"), child, out loadedType));
                    Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                    Assert.AreEqual(MessageType.InvalidTypeError, message);

                    Assert.IsFalse(MockHostServer.TryLoadType(new TypeArgument(new AssemblyArgument("bogus", "bogus"), GetType().FullName), child, out loadedType));
                    Assert.IsTrue(parent.TryReadMessage(out message, out data, out ex));
                    Assert.AreEqual(MessageType.AssemblyLoadError, message);

                    Func<TypeArgument, Communication, Action> tryLoadType = (_type, _com) => { return () => { MockHostServer.TryLoadType(_type, _com, out loadedType); }; };

                    TestUtilities.AssertThrows(tryLoadType(null, child), typeof(ArgumentNullException));
                    TestUtilities.AssertThrows(tryLoadType(new TypeArgument(GetType()), null), typeof(ArgumentNullException));
                    TestUtilities.AssertThrows(tryLoadType(null, null), typeof(ArgumentNullException));
                }
            }
        }

        #region Helper classes

        /// <summary>
        /// A class that throws an exception in the constructor.
        /// </summary>

        public class TestThrow
        {
            /// <summary>
            /// Throws an exception.
            /// </summary>
            /// <exception cref="NotSupportedException">always.</exception>

            public TestThrow()
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// A class whose constructor is private.
        /// </summary>

        public class PrivateConstructor
        {
            private PrivateConstructor()
            { }
        }

        /// <summary>
        /// A class that doesn't have a default constructor.
        /// </summary>

        public class NonDefaultConstructor
        {
            public NonDefaultConstructor(bool arg)
            { }
        }

        /// <summary>
        /// An abstract class.
        /// </summary>

        public abstract class AbstractClass
        { }

        /// <summary>
        /// A class that implements two versions of the same generic interface and a non-generic one.
        /// </summary>

        public class TestInterface : IEnumerable<bool>, IEnumerable<int>
        {
            /// <summary>
            /// A method with no arguments or return value.
            /// </summary>

            public void Method()
            { }

            /// <summary>
            /// A method with no arguments that returns a boolean.
            /// </summary>
            /// <returns>false</returns>

            public bool MethodWithReturn()
            {
                return false;
            }

            /// <summary>
            /// A method with a single argument and no return value.
            /// </summary>
            /// <param name="arg">An argument.</param>

            public void MethodWithArg(bool arg)
            { }

            /// <summary>
            /// A static method with no arguments or return value.
            /// </summary>

            public static void StaticMethod()
            { }

            /// <see cref="IEnumerable&lt;bool&gt;.GetEnumerator"/>

            public IEnumerator<bool> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            /// <see cref="IEnumerable.GetEnumerator"/>

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            /// <see cref="IEnumerable&lt;int&gt;.GetEnumerator"/>

            IEnumerator<int> IEnumerable<int>.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// A mock class derived from HostServer that grants access to protected methods.
        /// </summary>

        private class MockHostServer : HostServer
        {
            /// <see cref="HostServer.TryLoadType"/>

            public new static bool TryLoadType(TypeArgument type, Communication communication, out Type loadedType)
            {
                return HostServer.TryLoadType(type, communication, out loadedType);
            }

            /// <see cref="HostServer.CheckInterface"/>

            public new static bool CheckInterface(Type loadedType, Type interfaceType)
            {
                return HostServer.CheckInterface(loadedType, interfaceType);
            }

            /// <see cref="HostServer.TryGetMethod"/>

            public new static bool TryGetMethod(Type loadedType, MethodArgument methodInfo, Communication communication, out MethodInfo method)
            {
                return HostServer.TryGetMethod(loadedType, methodInfo, communication, out method);
            }

            /// <see cref="HostServer.TryCreateInstance&lt;TObject&gt;"/>

            public new static bool TryCreateInstance<TObject>(Type loadedType, Communication communication, out TObject instance)
            {
                return HostServer.TryCreateInstance<TObject>(loadedType, communication, out instance);
            }

            /// <see cref="HostServer.ParseCommands"/>

            public override bool ParseCommands(Queue<string> args, Communication communication)
            {
                throw new NotImplementedException();
            }

            /// <see cref="HostServer.Execute"/>

            public override bool Execute(Communication communication)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
