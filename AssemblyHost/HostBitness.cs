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
using System.ComponentModel;

namespace SpanglerCo.AssemblyHost
{
    /// <summary>
    /// Represents the different ways to select a bitness for the process that hosts an assembly.
    /// </summary>

    public enum HostBitness
    {
        /// <summary>
        /// A HostBitness that does not have a value.
        /// </summary>

        NotSet = 0,

        /// <summary>
        /// Specifies that the host process should use the native platform's bitness.
        /// </summary>
        /// <remarks>
        /// When running on a 64-bit operating system, the host process will be 64-bit.
        /// Otherwise it will be 32-bit. This allows a 32-bit program to create a 64-bit child.
        /// Use this setting to prefer 64-bit but still allow 32-bit.
        /// </remarks>

        Native,

        /// <summary>
        /// Specifies that the host process should use the same bitness as the process
        /// that starts it. This is the default.
        /// </summary>
        /// <remarks>
        /// A 32-bit program will create a 32-bit child process even on a 64-bit operating system.
        /// Use this setting when needing consistent bitness between parent and child.
        /// </remarks>

        Current,

        /// <summary>
        /// Specifies that the host process should be 32-bit.
        /// </summary>
        /// <remarks>
        /// Use this setting when the type being hosted requires or is only available in 32-bit.
        /// </remarks>

        Force32,

        /// <summary>
        /// Specifies that the host process should be 64-bit. An exception will be thrown on 32-bit operating systems.
        /// </summary>
        /// <remarks>
        /// Use this setting when the type being hosted requires or is only available in 64-bit.
        /// </remarks>

        Force64
    }
}
