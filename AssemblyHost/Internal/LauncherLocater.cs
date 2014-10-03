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

namespace SpanglerCo.AssemblyHost.Internal
{
    /// <summary>
    /// An <see cref="IAssemblyLocater"/> capable of finding the AssemblyHost
    /// executable to use for launching the child process.
    /// </summary>

    internal class LauncherLocater : IAssemblyLocater
    {
        /// <summary>
        /// The assembly name of the 32-bit launcher.
        /// </summary>

        private static string Launcher32Name = "AssemblyHostLauncher32";

        /// <see cref="IAssemblyLocater.LocateAssembly"/>

        public string LocateAssembly(HostBitness bitness)
        {
            if (bitness == HostBitness.NotSet)
            {
                throw new ArgumentException("Must specify a bitness.", "bitness");
            }

            // Determine whether to use AssemblyHost directly (native bitness) or the launcher (32-bit).
            // Never use the launcher on a 32-bit OS, and only use it when needing 32-bit.

            if (Environment.Is64BitOperatingSystem
                && (bitness == HostBitness.Force32
                || (bitness == HostBitness.Current && !Environment.Is64BitProcess)))
            {
                try
                {
                    // Without having a reference, find the full name of the launcher
                    // knowing that the launcher will use the same signing settings,
                    // including the key, as this assembly. Note that ReflectionOnlyLoad
                    // will even work for a 32-bit assembly in a 64-bit process.

                    AssemblyName name = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
                    name.Name = Launcher32Name;
                    Assembly launcher = Assembly.ReflectionOnlyLoad(name.FullName);
                    return launcher.Location;
                }
                catch (FileLoadException ex)
                {
                    throw new FileNotFoundException(ex.Message, ex.FileName, ex);
                }
                catch (BadImageFormatException ex)
                {
                    throw new FileNotFoundException(ex.Message, ex.FileName, ex);
                }
            }
            else
            {
                return Assembly.GetExecutingAssembly().Location;
            }
        }
    }
}
