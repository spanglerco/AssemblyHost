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
using System.Reflection;
using System.Collections.Generic;

namespace SpanglerCo.AssemblyHost
{
    /// <summary>
    /// Represents an argument for passing a type between processes.
    /// </summary>

    public sealed class TypeArgument
    {
        /// <summary>
        /// Gets the name of the type.
        /// </summary>

        public string Name { get; private set; }

        /// <summary>
        /// Gets the containing assembly's information.
        /// </summary>

        public AssemblyArgument ContainingAssembly { get; private set; }

        /// <summary>
        /// Creates a new argument.
        /// </summary>
        /// <param name="assembly">The assembly containing the type.</param>
        /// <param name="typeName">The name of the type.</param>
        /// <exception cref="ArgumentNullException">if assembly or typeName are null.</exception>
        /// <exception cref="ArgumentException">if typeName is empty.</exception>

        public TypeArgument(AssemblyArgument assembly, string typeName)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            if (typeName == null)
            {
                throw new ArgumentNullException("typeName");
            }

            if (typeName.Length == 0)
            {
                throw new ArgumentException("typeName cannot be empty.", "typeName");
            }

            ContainingAssembly = assembly;
            Name = typeName;
        }

        /// <summary>
        /// Creates a new argument.
        /// </summary>
        /// <param name="assemblyLocation">The path containing the assembly.</param>
        /// <param name="assemblyName">The name of the assembly containing the type.</param>
        /// <param name="typeName">The name of the type.</param>
        /// <exception cref="ArgumentNullException">if assemblyLocation, assemblyName, or typeName are null.</exception>
        /// <exception cref="ArgumentException">if assemblyLocation, assemblyName, or typeName is empty.</exception>

        public TypeArgument(string assemblyLocation, string assemblyName, string typeName)
            : this(assemblyLocation, assemblyName, HostBitness.Current, typeName)
        { }

        /// <summary>
        /// Creates a new argument.
        /// </summary>
        /// <param name="assemblyLocation">The path containing the assembly.</param>
        /// <param name="assemblyName">The name of the assembly containing the type.</param>
        /// <param name="bitness">The bitness setting for the assembly.</param>
        /// <param name="typeName">The name of the type.</param>
        /// <exception cref="ArgumentNullException">if assemblyLocation, assemblyName, or typeName are null.</exception>
        /// <exception cref="ArgumentException">if assemblyLocation, assemblyName, or typeName is empty.</exception>
        /// <exception cref="ArgumentException">if bitness is <see cref="HostBitness.Force64"/> when running on a 32-bit operating system.</exception>

        public TypeArgument(string assemblyLocation, string assemblyName, HostBitness bitness, string typeName)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException("typeName");
            }

            if (typeName.Length == 0)
            {
                throw new ArgumentException("typeName cannot be empty.", "typeName");
            }

            Name = typeName;
            ContainingAssembly = new AssemblyArgument(assemblyLocation, assemblyName, bitness);
        }

        /// <summary>
        /// Creates a new argument.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <exception cref="ArgumentNullException">if type is null.</exception>
        /// <exception cref="ArgumentException">if type is generic or not public.</exception>

        public TypeArgument(Type type)
        {
            LoadFromType(type, HostBitness.Current);
        }

        /// <summary>
        /// Creates a new argument.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="bitness">The bitness setting for the assembly.</param>
        /// <exception cref="ArgumentNullException">if type is null.</exception>
        /// <exception cref="ArgumentException">if type is generic or not public.</exception>
        /// <exception cref="ArgumentException">if bitness is <see cref="HostBitness.Force64"/> when running on a 32-bit operating system.</exception>

        public TypeArgument(Type type, HostBitness bitness)
        {
            LoadFromType(type, bitness);
        }

        /// <summary>
        /// Creates a new argument.
        /// </summary>
        /// <param name="method">A method whose declaring type will be used as the argument.</param>
        /// <param name="bitness">The bitness setting for the assembly.</param>
        /// <exception cref="ArgumentNullException">if method is null.</exception>
        /// <exception cref="ArgumentException">if the type declaring the method is generic or not public.</exception>
        /// <exception cref="ArgumentException">if bitness is <see cref="HostBitness.Force64"/> when running on a 32-bit operating system.</exception>

        internal TypeArgument(MethodInfo method, HostBitness bitness)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            LoadFromType(method.DeclaringType, bitness);
        }

        /// <summary>
        /// Restores a type argument.
        /// </summary>
        /// <param name="args">The current arguments.</param>

        internal TypeArgument(Queue<string> args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            ContainingAssembly = new AssemblyArgument(args);

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

            ContainingAssembly.AddArgs(args);
            args.Add(Name);
        }

        /// <summary>
        /// Initializes the argument from a type.
        /// </summary>
        /// <param name="type">The type to load.</param>
        /// <param name="bitness">The bitness setting for the assembly.</param>
        /// <exception cref="ArgumentNullException">if type is null.</exception>
        /// <exception cref="ArgumentException">if type is generic or not public.</exception>
        /// <exception cref="ArgumentException">if bitness is <see cref="HostBitness.Force64"/> when running on a 32-bit operating system.</exception>

        private void LoadFromType(Type type, HostBitness bitness)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            Type temp = type;
            bool isPublic = true;

            while (isPublic)
            {
                if (temp.IsNested)
                {
                    isPublic = temp.IsNestedPublic;
                    temp = temp.DeclaringType;
                }
                else
                {
                    isPublic = temp.IsPublic;
                    break;
                }
            }

            if (type.IsGenericType || type.ContainsGenericParameters || !isPublic)
            {
                throw new ArgumentException("type must be a public, non-generic type.", "type");
            }

            ContainingAssembly = new AssemblyArgument(type, bitness);
            Name = type.FullName;
        }
    }
}
