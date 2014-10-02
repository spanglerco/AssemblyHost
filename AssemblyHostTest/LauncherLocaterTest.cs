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
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SpanglerCo.AssemblyHost;
using SpanglerCo.AssemblyHost.Internal;

namespace SpanglerCo.UnitTests.AssemblyHost
{
    /// <summary>
    ///This is a test class for LauncherLocaterTest and is intended
    ///to contain all LauncherLocaterTest Unit Tests
    ///</summary>
    [TestClass()]
    public class LauncherLocaterTest
    {
        private static readonly string AssemblyHostPath = typeof(SpanglerCo.AssemblyHost.Child.Program).Assembly.Location;
        private static readonly string AssemblyHostLauncher32Path = Path.Combine(Path.GetDirectoryName(AssemblyHostPath), "AssemblyHostLauncher32.exe");

        /// <summary>
        /// A test for LocateAssembly for <see cref="HostBitness.NotSet"/>.
        ///</summary>

        [TestMethod()]
        public void LocateAssembly_NotSetTest()
        {
            LauncherLocater target = new LauncherLocater();
            TestUtilities.AssertThrows(() => target.LocateAssembly(HostBitness.NotSet), typeof(ArgumentException));
        }

        /// <summary>
        /// A test for LocateAssembly for <see cref="HostBitness.Native"/>.
        ///</summary>

        [TestMethod()]
        public void LocateAssembly_NativeTest()
        {
            LauncherLocater target = new LauncherLocater();
            Assert.AreEqual(AssemblyHostPath, target.LocateAssembly(HostBitness.Native));
        }

        /// <summary>
        /// A test for LocateAssembly for <see cref="HostBitness.Current"/>.
        ///</summary>

        [TestMethod()]
        public void LocateAssembly_CurrentTest()
        {
            LauncherLocater target = new LauncherLocater();
            bool expectLauncher = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess;
            Assert.AreEqual(expectLauncher ? AssemblyHostLauncher32Path : AssemblyHostPath, target.LocateAssembly(HostBitness.Current));
        }

        /// <summary>
        /// A test for LocateAssembly for <see cref="HostBitness.Force32"/>.
        ///</summary>

        [TestMethod()]
        public void LocateAssembly_Force32Test()
        {
            LauncherLocater target = new LauncherLocater();
            bool expectLauncher = Environment.Is64BitOperatingSystem;
            Assert.AreEqual(expectLauncher ? AssemblyHostLauncher32Path : AssemblyHostPath, target.LocateAssembly(HostBitness.Force32));
        }

        /// <summary>
        /// A test for LocateAssembly for <see cref="HostBitness.Force64"/>.
        ///</summary>

        [TestMethod()]
        public void LocateAssembly_Force64Test()
        {
            LauncherLocater target = new LauncherLocater();

            if (Environment.Is64BitOperatingSystem)
            {
                Assert.AreEqual(AssemblyHostPath, target.LocateAssembly(HostBitness.Force64));
            }
            else
            {
                TestUtilities.AssertThrows(() => target.LocateAssembly(HostBitness.Force64), typeof(FileNotFoundException));
            }
        }
    }
}
