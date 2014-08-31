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
        /// Creates a new assembly argument.
        /// </summary>
        /// <param name="assemblyLocation">The path to the assembly.</param>
        /// <param name="assemblyName">The name of the assembly.</param>
        /// <exception cref="ArgumentNullException">if assemblyLocation or assemblyName is null.</exception>
        /// <exception cref="ArgumentException">if assemblyLocation or assemblyName is empty.</exception>

        public AssemblyArgument(string assemblyLocation, string assemblyName)
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

            Name = assemblyName;
            Location = assemblyLocation;
        }

        /// <summary>
        /// Creates a new assembly argument for a type.
        /// </summary>
        /// <param name="type">The type whose assembly will be used.</param>
        /// <exception cref="ArgumentNullException">if type is null.</exception>

        internal AssemblyArgument(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

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
