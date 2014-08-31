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
using System.Diagnostics;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using SpanglerCo.AssemblyHost.Child;

namespace SpanglerCo.AssemblyHost
{
    /// <summary>
    /// Represents an AssemblyHost process that executes a static or instance method in another assembly.
    /// The method must have no parameters, and for instance methods, the type must have a default constructor.
    /// If the method returns an object that overrides ToString, the Result property will contain this value.
    /// </summary>

    public sealed class MethodHostProcess : HostProcess
    {
        private MethodArgument _method;

        /// <summary>
        /// Gets the assembly load path for a method.
        /// </summary>
        /// <param name="method">The method whose load path will be returned.</param>
        /// <returns>The assembly load path.</returns>
        /// <exception cref="ArgumentNullException">if method is null.</exception>

        private static string GetAssemblyLoadPath(MethodArgument method)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            return method.ContainingType.ContainingAssembly.Location;
        }

        /// <summary>
        /// Creates a new MethodHostProcess.
        /// </summary>
        /// <param name="method">The method to host in the process.</param>
        /// <exception cref="ArgumentNullException">if method is null.</exception>
        /// <remarks>
        /// By default, the child process will not create a window.
        /// Use a custom ProcessStartInfo instance to change this and other options.
        /// </remarks>

        public MethodHostProcess(MethodArgument method)
            : base(GetAssemblyLoadPath(method))
        {
            _method = method;
        }

        /// <summary>
        /// Creates a new MethodHostProcess using custom process start info.
        /// </summary>
        /// <param name="method">The method to host in the process.</param>
        /// <param name="startInfo">The start info to use when creating the process.</param>
        /// <exception cref="ArgumentNullException">if method or startInfo are null.</exception>

        public MethodHostProcess(MethodArgument method, ProcessStartInfo startInfo)
            : base(GetAssemblyLoadPath(method), startInfo)
        {
            _method = method;
        }

        /// <see cref="HostProcess.AddArguments"/>

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Protected method of a sealed class whose parent is internal.")]
        protected override void AddArguments(IList<string> args)
        {
            args.Add(HostServerType.Method.ToString());
            _method.AddArgs(args);
        }
    }
}
