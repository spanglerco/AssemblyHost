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
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SpanglerCo.AssemblyHost;

namespace SpanglerCo.UnitTests.AssemblyHost
{
    /// <summary>
    ///This is a test class for MethodArgumentTest and is intended
    ///to contain all MethodArgumentTest Unit Tests
    ///</summary>
    [TestClass()]
    public class MethodArgumentTest
    {
        /// <summary>
        ///A test for MethodArgument Constructor
        ///</summary>
        [TestMethod()]
        public void MethodArgumentConstructorTest()
        {
            string methodName = "MethodArgumentConstructorTest";
            TypeArgument typeArg = new TypeArgument(GetType());
            MethodArgument arg = new MethodArgument(typeArg, methodName, false);
            Assert.AreEqual(typeArg, arg.ContainingType);
            Assert.AreEqual(methodName, arg.Name);
            Assert.IsFalse(arg.IsStatic);

            Func<TypeArgument, string, bool, Action> ctor = (_type, _name, _static) => { return () => { new MethodArgument(_type, _name, _static); }; };

            TestUtilities.AssertThrows(ctor(null, methodName, true), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(typeArg, null, true), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(null, null, false), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(typeArg, string.Empty, true), typeof(ArgumentException));
        }

        /// <summary>
        ///A test for MethodArgument Constructor
        ///</summary>
        [TestMethod()]
        public void MethodArgumentConstructorStringsTest()
        {
            string name = Assembly.GetExecutingAssembly().FullName;
            string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string typeName = GetType().FullName;
            string methodName = "MethodArgumentConstructorStringsTest";
            MethodArgument arg = new MethodArgument(location, name, typeName, methodName, true);
            Assert.AreEqual(name, arg.ContainingType.ContainingAssembly.Name);
            Assert.AreEqual(location, arg.ContainingType.ContainingAssembly.Location);
            Assert.AreEqual(typeName, arg.ContainingType.Name);
            Assert.AreEqual(methodName, arg.Name);
            Assert.IsTrue(arg.IsStatic);

            Func<string, string, string, string, bool, Action> ctor = (_location, _name, _typeName, _methodName, _static) =>
                { return () => { new MethodArgument(_location, _name, _typeName, _methodName, _static); }; };

            TestUtilities.AssertThrows(ctor(null, name, typeName, methodName, true), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(location, null, typeName, methodName, true), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(location, name, null, methodName, true), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(location, name, typeName, null, false), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(null, null, null, null, true), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(string.Empty, name, typeName, methodName, true), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(location, string.Empty, typeName, methodName, true), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(location, name, string.Empty, methodName, true), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(location, name, typeName, string.Empty, true), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(string.Empty, string.Empty, string.Empty, string.Empty, false), typeof(ArgumentException));
        }

        /// <summary>
        ///A test for MethodArgument Constructor
        ///</summary>
        [TestMethod()]
        public void MethodArgumentConstructorMethodTest()
        {
            string methodName = "MethodArgumentConstructorMethodTest";
            string name = Assembly.GetExecutingAssembly().FullName;
            string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            MethodArgument arg = new MethodArgument(GetType().GetMethod(methodName));
            Assert.AreEqual(name, arg.ContainingType.ContainingAssembly.Name);
            Assert.AreEqual(location, arg.ContainingType.ContainingAssembly.Location);
            Assert.AreEqual(GetType().FullName, arg.ContainingType.Name);
            Assert.AreEqual(methodName, arg.Name);
            Assert.IsFalse(arg.IsStatic);

            arg = new MethodArgument(typeof(PublicNested).GetMethod("StaticMethod"));
            Assert.AreEqual(name, arg.ContainingType.ContainingAssembly.Name);
            Assert.AreEqual(location, arg.ContainingType.ContainingAssembly.Location);
            Assert.AreEqual(typeof(PublicNested).FullName, arg.ContainingType.Name);
            Assert.AreEqual("StaticMethod", arg.Name);
            Assert.IsTrue(arg.IsStatic);

            arg = new MethodArgument(typeof(PublicNested).GetMethod("Method"));
            Assert.AreEqual(name, arg.ContainingType.ContainingAssembly.Name);
            Assert.AreEqual(location, arg.ContainingType.ContainingAssembly.Location);
            Assert.AreEqual(typeof(PublicNested).FullName, arg.ContainingType.Name);
            Assert.AreEqual("Method", arg.Name);
            Assert.IsFalse(arg.IsStatic);

            arg = new MethodArgument(typeof(PublicNested).GetMethod("AbstractMethod"));
            Assert.AreEqual(name, arg.ContainingType.ContainingAssembly.Name);
            Assert.AreEqual(location, arg.ContainingType.ContainingAssembly.Location);
            Assert.AreEqual(typeof(PublicNested).FullName, arg.ContainingType.Name);
            Assert.AreEqual("AbstractMethod", arg.Name);
            Assert.IsFalse(arg.IsStatic);

            arg = new MethodArgument(typeof(PublicAbstract).GetMethod("StaticMethod"));
            Assert.AreEqual(name, arg.ContainingType.ContainingAssembly.Name);
            Assert.AreEqual(location, arg.ContainingType.ContainingAssembly.Location);
            Assert.AreEqual(typeof(PublicAbstract).FullName, arg.ContainingType.Name);
            Assert.AreEqual("StaticMethod", arg.Name);
            Assert.IsTrue(arg.IsStatic);

            Func<MethodInfo, Action> ctor = (_method) => { return () => { new MethodArgument(_method); }; };

            TestUtilities.AssertThrows(ctor(null), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(typeof(PrivateNested).GetMethod("Method")), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(PrivateNested).GetMethod("StaticMethod")), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(PublicAbstract).GetMethod("Method")), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(PublicAbstract).GetMethod("AbstractMethod")), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(PublicNested).GetMethod("GenericMethod")), typeof(ArgumentException));
        }

        /// <summary>
        ///A test for AddArgs
        ///</summary>
        [TestMethod()]
        public void AddArgsTest()
        {
            List<string> argsOut = new List<string>();
            MethodArgument arg = new MethodArgument(typeof(PublicNested).GetMethod("StaticMethod"));
            arg.AddArgs(argsOut);
            Assert.IsTrue(argsOut.Count > 0);

            Queue<string> argsIn = new Queue<string>(argsOut);
            MethodArgument arg2 = new MethodArgument(argsIn);
            Assert.AreEqual(arg.Name, arg2.Name);
            Assert.AreEqual(arg.IsStatic, arg2.IsStatic);
            Assert.AreEqual(arg.ContainingType.Name, arg2.ContainingType.Name);
            Assert.AreEqual(arg.ContainingType.ContainingAssembly.Name, arg2.ContainingType.ContainingAssembly.Name);
            // Location is not deserialized. Instead, the location is used in the AppDomain setup.

            // Wrap the calls into Actions.
            Func<Queue<String>, Action> ctor = (_args) => { return () => { new MethodArgument(_args); }; };
            Func<MethodArgument, IList<string>, Action> addArgs = (_arg, _args) => { return () => { _arg.AddArgs(_args); }; };

            TestUtilities.AssertThrows(addArgs(arg, null), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(null), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(new Queue<string>()), typeof(ArgumentException));

            argsIn = new Queue<string>(argsOut);
            argsIn.Dequeue();
            TestUtilities.AssertThrows(ctor(argsIn), typeof(ArgumentException));
        }

        #region Helper classes

        /// <summary>
        /// Public abstract class used to test MethodArgument(MethodInfo) constructor.
        /// Only StaticMethod should be accepted.
        /// </summary>

        public abstract class PublicAbstract
        {
            public static void StaticMethod()
            { }

            public void Method()
            { }

            public abstract void AbstractMethod();
        }

        /// <summary>
        /// Public nested class used to test MethodArgument(MethodInfo) constructor.
        /// Should be accepted.
        /// </summary>

        public class PublicNested : PublicAbstract
        {
            public new static void StaticMethod()
            { }

            public new void Method()
            { }

            public override void AbstractMethod()
            { }

            public void GenericMethod<T>()
            { }
        }

        /// <summary>
        /// Private nested class used to test MethodArgument(MethodInfo) constructor.
        /// Should not be accepted.
        /// </summary>

        private class PrivateNested
        {
            public static void StaticMethod()
            { }

            public void Method()
            { }
        }

        #endregion
    }
}
