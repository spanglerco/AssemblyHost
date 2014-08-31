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
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SpanglerCo.AssemblyHost.Ipc;

namespace SpanglerCo.UnitTests.AssemblyHost
{
    /// <summary>
    ///This is a test class for ParentCommunicationTest and is intended
    ///to contain all ParentCommunicationTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ParentCommunicationTest
    {
        /// <summary>
        ///A test for AddChildArguments
        ///</summary>
        [TestMethod()]
        public void AddChildArgumentsTest()
        {
            using (ParentCommunication parent = new ParentCommunication())
            {
                IList<string> outArgs = new List<string>();
                parent.AddChildArguments(outArgs);

                Queue<string> inArgs = new Queue<string>(outArgs);
                inArgs.Dequeue();
                TestUtilities.AssertThrows(() => { new ChildCommunication(inArgs); }, typeof(ArgumentException));
                TestUtilities.AssertThrows(() => { new ChildCommunication(null); }, typeof(ArgumentNullException));

                inArgs = new Queue<string>(outArgs);
                using (ChildCommunication child = new ChildCommunication(inArgs))
                { }

                TestUtilities.AssertThrows(() => { parent.AddChildArguments(null); }, typeof(ArgumentNullException));
            }
        }
    }
}
