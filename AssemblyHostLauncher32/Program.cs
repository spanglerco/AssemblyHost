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

using SpanglerCo.AssemblyHost;

namespace SpanglerCo.AssemblyHostLauncher32
{
    /// <summary>
    /// Contains the entry point for the AssemblyHost 32-bit launcher.
    /// </summary>

    internal static class Program
    {
        /// <summary>
        /// The entry point for the AssemblyHost 32-bit launcher.
        /// </summary>
        /// <param name="args">The program arguments.</param>

        private static void Main(string[] args)
        {
            // Use a project reference to find the AssemblyHost and transfer control to it.
            AppDomain.CurrentDomain.ExecuteAssemblyByName(typeof(HostProcess).Assembly.FullName, args);
        }
    }
}
