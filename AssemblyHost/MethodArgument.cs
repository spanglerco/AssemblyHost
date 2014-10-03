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
    /// Represents an argument for passing a method between processes.
    /// </summary>

    public sealed class MethodArgument
    {
        /// <summary>
        /// Gets the name of the method.
        /// </summary>

        public string Name { get; private set; }

        /// <summary>
        /// Gets whether or not the method is static.
        /// </summary>

        public bool IsStatic { get; private set; }

        /// <summary>
        /// Gets the containing type's information.
        /// </summary>

        public TypeArgument ContainingType { get; private set; }

        /// <summary>
        /// Creates a new argument.
        /// </summary>
        /// <param name="type">The type containing the method.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="isStatic">Whether or not the method is static.</param>
        /// <exception cref="ArgumentNullException">if type or methodName are null.</exception>
        /// <exception cref="ArgumentException">if methodName is empty.</exception>

        public MethodArgument(TypeArgument type, string methodName, bool isStatic)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (methodName == null)
            {
                throw new ArgumentNullException("methodName");
            }

            if (methodName.Length == 0)
            {
                throw new ArgumentException("methodName cannot be empty.", "methodName");
            }

            ContainingType = type;
            IsStatic = isStatic;
            Name = methodName;
        }

        /// <summary>
        /// Creates a new argument.
        /// </summary>
        /// <param name="assemblyLocation">The path containing the assembly.</param>
        /// <param name="assemblyName">The name of the assembly containing the type.</param>
        /// <param name="typeName">The name of the type containing the method.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="isStatic">Whether or not the method is static.</param>
        /// <exception cref="ArgumentNullException">if assemblyLocation, assemblyName, typeName, or methodName are null.</exception>
        /// <exception cref="ArgumentException">if assemblyLocation, assemblyName, typeName, or methodName is empty.</exception>

        public MethodArgument(string assemblyLocation, string assemblyName, string typeName, string methodName, bool isStatic)
            : this(assemblyLocation, assemblyName, HostBitness.Current, typeName, methodName, isStatic)
        { }

        /// <summary>
        /// Creates a new argument.
        /// </summary>
        /// <param name="assemblyLocation">The path containing the assembly.</param>
        /// <param name="assemblyName">The name of the assembly containing the type.</param>
        /// <param name="bitness">The bitness setting for the assembly.</param>
        /// <param name="typeName">The name of the type containing the method.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="isStatic">Whether or not the method is static.</param>
        /// <exception cref="ArgumentNullException">if assemblyLocation, assemblyName, typeName, or methodName are null.</exception>
        /// <exception cref="ArgumentException">if assemblyLocation, assemblyName, typeName, or methodName is empty.</exception>
        /// <exception cref="ArgumentException">if bitness is <see cref="HostBitness.Force64"/> when running on a 32-bit operating system.</exception>

        public MethodArgument(string assemblyLocation, string assemblyName, HostBitness bitness, string typeName, string methodName, bool isStatic)
        {
            if (methodName == null)
            {
                throw new ArgumentNullException("methodName");
            }

            if (methodName.Length == 0)
            {
                throw new ArgumentException("methodName cannot be empty.", "methodName");
            }

            IsStatic = isStatic;
            Name = methodName;
            ContainingType = new TypeArgument(assemblyLocation, assemblyName, bitness, typeName);
        }

        /// <summary>
        /// Creates a new argument.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <exception cref="ArgumentNullException">if method is null.</exception>
        /// <exception cref="ArgumentException">if method is generic, not public, or cannot be invoked.</exception>
        /// <exception cref="ArgumentException">if the type declaring the method is generic or not public.</exception>

        public MethodArgument(MethodInfo method)
            : this(method, HostBitness.Current)
        { }

        /// <summary>
        /// Creates a new argument.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="bitness">The bitness setting for the assembly.</param>
        /// <exception cref="ArgumentNullException">if method is null.</exception>
        /// <exception cref="ArgumentException">if method is generic, not public, or cannot be invoked.</exception>
        /// <exception cref="ArgumentException">if the type declaring the method is generic or not public.</exception>
        /// <exception cref="ArgumentException">if bitness is <see cref="HostBitness.Force64"/> when running on a 32-bit operating system.</exception>

        public MethodArgument(MethodInfo method, HostBitness bitness)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            if ((method.DeclaringType.IsAbstract && !method.IsStatic) || method.IsConstructor || method.IsGenericMethod || !method.IsPublic)
            {
                throw new ArgumentException("method must be a public, non-generic method that can be invoked.", "method");
            }

            Name = method.Name;
            IsStatic = method.IsStatic;
            ContainingType = new TypeArgument(method, bitness);
        }

        /// <summary>
        /// Restores a method argument.
        /// </summary>
        /// <param name="args">The current arguments.</param>

        internal MethodArgument(Queue<string> args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            ContainingType = new TypeArgument(args);

            if (args.Count < 2)
            {
                throw new ArgumentException("Not enough arguments.", "args");
            }

            Name = args.Dequeue();
            IsStatic = bool.Parse(args.Dequeue());
        }

        /// <summary>
        /// Adds the method to a list of arguments.
        /// </summary>
        /// <param name="args">The current arguments.</param>

        internal void AddArgs(IList<string> args)
        {
            ContainingType.AddArgs(args);
            args.Add(Name);
            args.Add(IsStatic.ToString());
        }
    }
}
