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

namespace SpanglerCo.AssemblyHost.Internal
{
    /// <summary>
    /// Represents a type capable of locating an assembly based on bitness requirements.
    /// </summary>

    internal interface IAssemblyLocater
    {
        /// <summary>
        /// Locates an assembly that fulfills the given bitness requirements.
        /// </summary>
        /// <param name="bitness">The bitness requirements of the assembly to locate.</param>
        /// <returns>The path to the located assembly.</returns>
        /// <exception cref="ArgumentException">if bitness is invalid.</exception>
        /// <exception cref="FileNotFoundException">if the assembly could not be located.</exception>

        string LocateAssembly(HostBitness bitness);
    }
}
