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
    ///This is a test class for TypeArgumentTest and is intended
    ///to contain all TypeArgumentTest Unit Tests
    ///</summary>
    [TestClass()]
    public class TypeArgumentTest
    {
        /// <summary>
        ///A test for TypeArgument Constructor
        ///</summary>
        [TestMethod()]
        public void TypeArgumentConstructorTest()
        {
            AssemblyArgument assembly = new AssemblyArgument(GetType(), HostBitness.Current);
            string typeName = GetType().FullName;
            TypeArgument arg = new TypeArgument(assembly, typeName);
            Assert.AreEqual(assembly, arg.ContainingAssembly);
            Assert.AreEqual(typeName, arg.Name);

            Func<AssemblyArgument, string, Action> ctor = (_assembly, _name) => { return () => { new TypeArgument(_assembly, _name); }; };

            TestUtilities.AssertThrows(ctor(null, typeName), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(assembly, null), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(null, null), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(assembly, string.Empty), typeof(ArgumentException));
        }

        /// <summary>
        ///A test for TypeArgument Constructor
        ///</summary>
        [TestMethod()]
        public void TypeArgumentConstructorStringsTest()
        {
            string name = Assembly.GetExecutingAssembly().FullName;
            string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string typeName = GetType().FullName;
            TypeArgument arg = new TypeArgument(location, name, typeName);
            Assert.AreEqual(name, arg.ContainingAssembly.Name);
            Assert.AreEqual(location, arg.ContainingAssembly.Location);
            Assert.AreEqual(HostBitness.Current, arg.ContainingAssembly.Bitness);
            Assert.AreEqual(typeName, arg.Name);

            Func<string, string, string, Action> ctor = (_location, _name, _typeName) => { return () => { new TypeArgument(_location, _name, _typeName); }; };

            TestUtilities.AssertThrows(ctor(null, name, typeName), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(location, null, typeName), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(location, name, null), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(null, null, null), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(string.Empty, name, typeName), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(location, string.Empty, typeName), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(location, name, string.Empty), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(string.Empty, string.Empty, string.Empty), typeof(ArgumentException));
        }

        /// <summary>
        ///A test for TypeArgument Constructor
        ///</summary>
        [TestMethod()]
        public void TypeArgumentConstructorStringsBitnessTest()
        {
            string name = Assembly.GetExecutingAssembly().FullName;
            string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string typeName = GetType().FullName;
            HostBitness bitness = HostBitness.Force32;
            TypeArgument arg = new TypeArgument(location, name, bitness, typeName);
            Assert.AreEqual(name, arg.ContainingAssembly.Name);
            Assert.AreEqual(location, arg.ContainingAssembly.Location);
            Assert.AreEqual(bitness, arg.ContainingAssembly.Bitness);
            Assert.AreEqual(typeName, arg.Name);

            Func<string, string, HostBitness, string, Action> ctor = (_location, _name, _bitness, _typeName) => { return () => { new TypeArgument(_location, _name, _bitness, _typeName); }; };

            TestUtilities.AssertThrows(ctor(null, name, HostBitness.Current, typeName), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(location, null, HostBitness.Current, typeName), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(location, name, HostBitness.Current, null), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(null, null, HostBitness.Current, null), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(string.Empty, name, HostBitness.Current, typeName), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(location, string.Empty, HostBitness.Current, typeName), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(location, name, HostBitness.NotSet, typeName), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(location, name, HostBitness.Current, string.Empty), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(string.Empty, string.Empty, HostBitness.Current, string.Empty), typeof(ArgumentException));

            if (!Environment.Is64BitOperatingSystem)
            {
                TestUtilities.AssertThrows(ctor(location, name, HostBitness.Force64, typeName), typeof(ArgumentException));
            }
        }

        /// <summary>
        ///A test for TypeArgument Constructor
        ///</summary>
        [TestMethod()]
        public void TypeArgumentConstructorTypeTest()
        {
            string name = Assembly.GetExecutingAssembly().FullName;
            string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            TypeArgument arg = new TypeArgument(GetType());
            Assert.AreEqual(name, arg.ContainingAssembly.Name);
            Assert.AreEqual(location, arg.ContainingAssembly.Location);
            Assert.AreEqual(HostBitness.Current, arg.ContainingAssembly.Bitness);
            Assert.AreEqual(GetType().FullName, arg.Name);

            arg = new TypeArgument(typeof(PublicNested));
            Assert.AreEqual(name, arg.ContainingAssembly.Name);
            Assert.AreEqual(location, arg.ContainingAssembly.Location);
            Assert.AreEqual(HostBitness.Current, arg.ContainingAssembly.Bitness);
            Assert.AreEqual(typeof(PublicNested).FullName, arg.Name);

            Func<Type, Action> ctor = (_type) => { return () => { new TypeArgument(_type); }; };

            TestUtilities.AssertThrows(ctor(null), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(typeof(TestInternalType)), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(TestInternalType.Nested)), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(PrivateNested)), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(GenericType<int>)), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(GenericType<int>.GenericNested)), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(GenericType<>)), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(GenericType<>).GetGenericArguments()[0]), typeof(ArgumentException));
        }

        /// <summary>
        ///A test for TypeArgument Constructor
        ///</summary>
        [TestMethod()]
        public void TypeArgumentConstructorTypeBitnessTest()
        {
            string name = Assembly.GetExecutingAssembly().FullName;
            string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            HostBitness bitness = HostBitness.Native;
            TypeArgument arg = new TypeArgument(GetType(), bitness);
            Assert.AreEqual(name, arg.ContainingAssembly.Name);
            Assert.AreEqual(location, arg.ContainingAssembly.Location);
            Assert.AreEqual(bitness, arg.ContainingAssembly.Bitness);
            Assert.AreEqual(GetType().FullName, arg.Name);

            arg = new TypeArgument(typeof(PublicNested), bitness);
            Assert.AreEqual(name, arg.ContainingAssembly.Name);
            Assert.AreEqual(location, arg.ContainingAssembly.Location);
            Assert.AreEqual(bitness, arg.ContainingAssembly.Bitness);
            Assert.AreEqual(typeof(PublicNested).FullName, arg.Name);

            Func<Type, HostBitness, Action> ctor = (_type, _bitness) => { return () => { new TypeArgument(_type, _bitness); }; };

            TestUtilities.AssertThrows(ctor(null, HostBitness.Current), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(GetType(), HostBitness.NotSet), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(TestInternalType), HostBitness.Current), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(TestInternalType.Nested), HostBitness.Current), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(PrivateNested), HostBitness.Current), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(GenericType<int>), HostBitness.Current), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(GenericType<int>.GenericNested), HostBitness.Current), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(GenericType<>), HostBitness.Current), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(GenericType<>).GetGenericArguments()[0], HostBitness.Current), typeof(ArgumentException));

            if (!Environment.Is64BitOperatingSystem)
            {
                TestUtilities.AssertThrows(ctor(GetType(), HostBitness.Force64), typeof(ArgumentException));
            }
        }

        /// <summary>
        ///A test for TypeArgument Constructor
        ///</summary>
        [TestMethod()]
        public void TypeArgumentConstructorMethodTest()
        {
            string name = Assembly.GetExecutingAssembly().FullName;
            string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            HostBitness bitness = HostBitness.Native;
            TypeArgument arg = new TypeArgument(GetType().GetMethod("TypeArgumentConstructorMethodTest"), bitness);
            Assert.AreEqual(name, arg.ContainingAssembly.Name);
            Assert.AreEqual(location, arg.ContainingAssembly.Location);
            Assert.AreEqual(bitness, arg.ContainingAssembly.Bitness);
            Assert.AreEqual(GetType().FullName, arg.Name);

            arg = new TypeArgument(typeof(PublicNested).GetMethod("Method"), HostBitness.Current);
            Assert.AreEqual(name, arg.ContainingAssembly.Name);
            Assert.AreEqual(location, arg.ContainingAssembly.Location);
            Assert.AreEqual(typeof(PublicNested).FullName, arg.Name);

            Func<MethodInfo, Action> ctor = (_type) => { return () => { new TypeArgument(_type, HostBitness.Current); }; };

            TestUtilities.AssertThrows(ctor(null), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(typeof(TestInternalType).GetMethod("Method")), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(TestInternalType.Nested).GetMethod("Method")), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(PrivateNested).GetMethod("Method")), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(GenericType<int>).GetMethod("Method")), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(GenericType<int>.GenericNested).GetMethod("Method")), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(typeof(GenericType<>).GetMethod("Method")), typeof(ArgumentException));
        }

        /// <summary>
        ///A test for AddArgs
        ///</summary>
        [TestMethod()]
        public void AddArgsTest()
        {
            List<string> argsOut = new List<string>();
            TypeArgument arg = new TypeArgument(GetType());
            arg.AddArgs(argsOut);
            Assert.IsTrue(argsOut.Count > 0);

            Queue<string> argsIn = new Queue<string>(argsOut);
            TypeArgument arg2 = new TypeArgument(argsIn);
            Assert.AreEqual(arg.Name, arg2.Name);
            Assert.AreEqual(arg.ContainingAssembly.Name, arg2.ContainingAssembly.Name);
            // Location is not deserialized. Instead, the location is used in the AppDomain setup.
            // Bitness is not deserialized. Instead, the bitness is used to determine which executable to run.

            // Wrap the calls into Actions.
            Func<Queue<String>, Action> ctor = (_args) => { return () => { new TypeArgument(_args); }; };
            Func<TypeArgument, IList<string>, Action> addArgs = (_arg, _args) => { return () => { _arg.AddArgs(_args); }; };

            TestUtilities.AssertThrows(addArgs(arg, null), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(null), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(new Queue<string>()), typeof(ArgumentException));

            argsIn = new Queue<string>(argsOut);
            argsIn.Dequeue();
            TestUtilities.AssertThrows(ctor(argsIn), typeof(ArgumentException));
        }

        #region Helper classes

        /// <summary>
        /// Public nested class used to test TypeArgument(Type) constructor.
        /// Should be accepted.
        /// </summary>

        public class PublicNested
        {
            public void Method()
            { }
        }

        /// <summary>
        /// Private nested class used to test TypeArgument(Type) constructor.
        /// Should not be accepted.
        /// </summary>

        private class PrivateNested
        {
            public void Method()
            { }
        }

        /// <summary>
        /// Generic class used to test TypeArgument(Type) constructor.
        /// Neither GenericType nor GenericNested should be accepted.
        /// </summary>

        public class GenericType<T>
        {
            public void Method()
            { }

            public class GenericNested
            {
                public void Method()
                { }
            }
        }

        #endregion
    }

    /// <summary>
    /// Internal class used to test TypeArgument(Type) constructor.
    /// Neither TestInternalType nor TestInternalType.Nested should be accepted.
    /// </summary>

    internal class TestInternalType
    {
        public void Method()
        { }

        public class Nested
        {
            public void Method()
            { }
        }
    }
}
