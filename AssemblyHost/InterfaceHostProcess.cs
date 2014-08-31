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
using System.Diagnostics;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using SpanglerCo.AssemblyHost.Child;

namespace SpanglerCo.AssemblyHost
{
    /// <summary>
    /// Represents an AssemblyHost process that executes a type in another assembly based on an interface.
    /// The type must have a default constructor and implement IChildProcess.
    /// </summary>

    public sealed class InterfaceHostProcess : HostProcess
    {
        private string _arguments;
        private TypeArgument _type;

        /// <summary>
        /// Gets the assembly load path for a type.
        /// </summary>
        /// <param name="type">The type whose load path will be returned.</param>
        /// <returns>The assembly load path.</returns>
        /// <exception cref="ArgumentNullException">if type is null.</exception>

        private static string GetAssemblyLoadPath(TypeArgument type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            return type.ContainingAssembly.Location;
        }

        /// <summary>
        /// Creates a new InterfaceHostProcess.
        /// </summary>
        /// <param name="type">The type to host in the process.</param>
        /// <param name="arguments">The arguments to pass to IChildProcess.Execute in the process.</param>
        /// <exception cref="ArgumentNullException">if type is null.</exception>
        /// <remarks>
        /// By default, the child process will not create a window.
        /// Use a custom ProcessStartInfo instance to change this and other options.
        /// </remarks>

        public InterfaceHostProcess(TypeArgument type, string arguments)
            : base(GetAssemblyLoadPath(type))
        {
            _type = type;
            _arguments = arguments;
        }

        /// <summary>
        /// Creates a new InterfaceHostProcess using custom process start info.
        /// </summary>
        /// <param name="type">The type to host in the process.</param>
        /// <param name="startInfo">The start info to use when creating the process.</param>
        /// <param name="arguments">The arguments to pass to IChildProcess.Execute in the process.</param>
        /// <exception cref="ArgumentNullException">if type or startInfo are null.</exception>

        public InterfaceHostProcess(TypeArgument type, ProcessStartInfo startInfo, string arguments)
            : base(GetAssemblyLoadPath(type), startInfo)
        {
            _type = type;
            _arguments = arguments;
        }

        /// <see cref="HostProcess.AddArguments"/>

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification="Protected method of a sealed class whose parent is internal.")]
        protected override void AddArguments(IList<string> args)
        {
            args.Add(HostServerType.Interface.ToString());
            _type.AddArgs(args);
            args.Add(_arguments);
        }

        /// <summary>
        /// Signals the child process to terminate.
        /// </summary>
        /// <exception cref="InvalidOperationException">if the child process has not been started.</exception>

        public void Stop()
        {
            StopChild();
        }

        /// <see cref="HostProcess.Dispose(bool)"/>

        protected override void Dispose(bool disposing)
        {
            try
            {
                Stop();
            }
            catch (InvalidOperationException)
            { }

            base.Dispose(disposing);
        }
    }
}
