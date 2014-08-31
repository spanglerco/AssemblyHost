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
    ///This is a test class for AssemblyArgumentTest and is intended
    ///to contain all AssemblyArgumentTest Unit Tests
    ///</summary>
    [TestClass()]
    public class AssemblyArgumentTest
    {
        /// <summary>
        ///A test for AssemblyArgument Constructor
        ///</summary>
        [TestMethod()]
        public void AssemblyArgumentConstructorTest()
        {
            string name = Assembly.GetExecutingAssembly().FullName;
            string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            AssemblyArgument arg = new AssemblyArgument(location, name);
            Assert.AreEqual(name, arg.Name);
            Assert.AreEqual(location, arg.Location);

            // Wrap the constructor into an Action.
            Func<string, string, Action> ctor = (_location, _name) => { return () => { new AssemblyArgument(_location, _name); }; };

            TestUtilities.AssertThrows(ctor(null, name), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(location, null), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(null, null), typeof(ArgumentNullException));

            TestUtilities.AssertThrows(ctor(string.Empty, name), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(location, string.Empty), typeof(ArgumentException));
            TestUtilities.AssertThrows(ctor(string.Empty, string.Empty), typeof(ArgumentException));
        }

        /// <summary>
        ///A test for AssemblyArgument Constructor
        ///</summary>
        [TestMethod()]
        public void AssemblyArgumentConstructorTypeTest()
        {
            string name = Assembly.GetExecutingAssembly().FullName;
            string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            AssemblyArgument arg = new AssemblyArgument(GetType());
            Assert.AreEqual(name, arg.Name);
            Assert.AreEqual(location, arg.Location);

            // Wrap the constructor into an Action.
            Func<Type, Action> ctor = (_type) => { return () => { new AssemblyArgument(_type); }; };

            TestUtilities.AssertThrows(ctor(null), typeof(ArgumentNullException));
        }

        /// <summary>
        ///A test for AddArgs
        ///</summary>
        [TestMethod()]
        public void AddArgsTest()
        {
            List<string> argsOut = new List<string>();
            AssemblyArgument arg = new AssemblyArgument(GetType());
            arg.AddArgs(argsOut);
            Assert.IsTrue(argsOut.Count > 0);

            Queue<string> argsIn = new Queue<string>(argsOut);
            AssemblyArgument arg2 = new AssemblyArgument(argsIn);
            Assert.AreEqual(arg.Name, arg2.Name);
            // Location is not deserialized. Instead, the location is used in the AppDomain setup.

            // Wrap the calls into Actions.
            Func<Queue<String>, Action> ctor = (_args) => { return () => { new AssemblyArgument(_args); }; };
            Func<AssemblyArgument, IList<string>, Action> addArgs = (_arg, _args) => { return () => { _arg.AddArgs(_args); }; };

            TestUtilities.AssertThrows(addArgs(arg, null), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(null), typeof(ArgumentNullException));
            TestUtilities.AssertThrows(ctor(new Queue<string>()), typeof(ArgumentException));
        }
    }
}
