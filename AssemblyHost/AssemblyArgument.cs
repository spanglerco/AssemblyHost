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
using System.Collections.Generic;

namespace SpanglerCo.AssemblyHost
{
    /// <summary>
    /// Represents an argument for passing an assembly between processes.
    /// </summary>

    public sealed class AssemblyArgument
    {
        /// <summary>
        /// Gets the name of the assembly.
        /// </summary>

        public string Name { get; private set; }

        /// <summary>
        /// Gets the location of the assembly.
        /// </summary>

        public string Location { get; private set; }

        /// <summary>
        /// Gets the bitness setting for the assembly.
        /// </summary>

        public HostBitness Bitness { get; private set; }

        /// <summary>
        /// Creates a new assembly argument.
        /// </summary>
        /// <param name="assemblyLocation">The path to the assembly.</param>
        /// <param name="assemblyName">The name of the assembly.</param>
        /// <exception cref="ArgumentNullException">if assemblyLocation or assemblyName is null.</exception>
        /// <exception cref="ArgumentException">if assemblyLocation or assemblyName is empty.</exception>

        public AssemblyArgument(string assemblyLocation, string assemblyName)
            : this(assemblyLocation, assemblyName, HostBitness.Current)
        { }

        /// <summary>
        /// Creates a new assembly argument, specifying the bitness.
        /// </summary>
        /// <param name="assemblyLocation">The path to the assembly.</param>
        /// <param name="assemblyName">The name of the assembly.</param>
        /// <param name="bitness">The bitness setting for the assembly.</param>
        /// <exception cref="ArgumentNullException">if assemblyLocation or assemblyName is null.</exception>
        /// <exception cref="ArgumentException">if assemblyLocation or assemblyName is empty.</exception>
        /// <exception cref="ArgumentException">if bitness is <see cref="HostBitness.Force64"/> when running on a 32-bit operating system.</exception>

        public AssemblyArgument(string assemblyLocation, string assemblyName, HostBitness bitness)
        {
            if (assemblyLocation == null)
            {
                throw new ArgumentNullException("assemblyLocation");
            }

            if (assemblyLocation.Length == 0)
            {
                throw new ArgumentException("assemblyLocation cannot be empty.", "assemblyLocation");
            }

            if (assemblyName == null)
            {
                throw new ArgumentNullException("assemblyName");
            }

            if (assemblyName.Length == 0)
            {
                throw new ArgumentException("assemblyName cannot be empty.", "assemblyName");
            }

            if (bitness == HostBitness.Force64 && !Environment.Is64BitOperatingSystem)
            {
                throw new ArgumentException("Cannot force 64-bit on a 32-bit operating system.", "bitness");
            }

            if (bitness == HostBitness.NotSet)
            {
                throw new ArgumentException("Must specify a valid bitness.", "bitness");
            }

            Bitness = bitness;
            Name = assemblyName;
            Location = assemblyLocation;
        }

        /// <summary>
        /// Creates a new assembly argument for a type.
        /// </summary>
        /// <param name="type">The type whose assembly will be used.</param>
        /// <param name="bitness">The bitness setting for the assembly.</param>
        /// <exception cref="ArgumentNullException">if type is null.</exception>
        /// <exception cref="ArgumentException">if bitness is <see cref="HostBitness.Force64"/> when running on a 32-bit operating system.</exception>

        internal AssemblyArgument(Type type, HostBitness bitness)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (bitness == HostBitness.Force64 && !Environment.Is64BitOperatingSystem)
            {
                throw new ArgumentException("Cannot force 64-bit on a 32-bit operating system.", "bitness");
            }

            if (bitness == HostBitness.NotSet)
            {
                throw new ArgumentException("Must specify a valid bitness.", "bitness");
            }

            Bitness = bitness;
            Name = type.Assembly.FullName;
            Location = Path.GetDirectoryName(type.Assembly.Location);
        }

        /// <summary>
        /// Restores an assembly argument.
        /// </summary>
        /// <param name="args">The current arguments.</param>

        internal AssemblyArgument(Queue<string> args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            if (args.Count == 0)
            {
                throw new ArgumentException("Not enough arguments.", "args");
            }

            Name = args.Dequeue();
        }

        /// <summary>
        /// Adds the type to a list of arguments.
        /// </summary>
        /// <param name="args">The current arguments.</param>

        internal void AddArgs(IList<string> args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            args.Add(Name);
        }
    }
}
