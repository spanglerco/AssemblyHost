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
using System.Diagnostics;
using System.ServiceModel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using SpanglerCo.AssemblyHost.Child;
using SpanglerCo.AssemblyHost.Internal;

namespace SpanglerCo.AssemblyHost
{
    /// <summary>
    /// Represents an AssemblyHost process that hosts a type as a WCF service.
    /// The type must implement one or more WCF service contract interfaces.
    /// </summary>

    public sealed class WcfHostProcess : HostProcess
    {
        private TypeArgument _type;
        private string _serviceAddress;

        /// <summary>
        /// Gets the assembly information for a type.
        /// </summary>
        /// <param name="type">The type whose load path will be returned.</param>
        /// <returns>The assembly argument.</returns>
        /// <exception cref="ArgumentNullException">if type is null.</exception>

        private static AssemblyArgument GetAssemblyArgument(TypeArgument type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            return type.ContainingAssembly;
        }

        /// <summary>
        /// Creates a new WcfHostProcess.
        /// </summary>
        /// <param name="type">The type to host in the process.</param>
        /// <exception cref="ArgumentNullException">if type is null.</exception>
        /// <remarks>
        /// By default, the child process will not create a window.
        /// Use a custom ProcessStartInfo instance to change this and other options.
        /// </remarks>

        public WcfHostProcess(TypeArgument type)
            : base(GetAssemblyArgument(type), new LauncherLocater())
        {
            _type = type;
        }

        /// <summary>
        /// Creates a new WcfHostProcess using custom process start info.
        /// </summary>
        /// <param name="type">The type to host in the process.</param>
        /// <param name="startInfo">The start info to use when creating the process.</param>
        /// <exception cref="ArgumentNullException">if type or startInfo are null.</exception>
        /// <exception cref="FileNotFoundException">if the requested bitness requires 32-bit but the launcher cannot be found.</exception>

        public WcfHostProcess(TypeArgument type, ProcessStartInfo startInfo)
            : base(GetAssemblyArgument(type), new LauncherLocater(), startInfo)
        {
            _type = type;
        }

        /// <see cref="HostProcess.AddArguments"/>

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Protected method of a sealed class whose parent is internal.")]
        protected override void AddArguments(IList<string> args)
        {
            _serviceAddress = "net.pipe://localhost/" + Guid.NewGuid();

            args.Add(HostServerType.WcfService.ToString());
            _type.AddArgs(args);
            args.Add(_serviceAddress);
        }

        /// <summary>
        /// Signals the child process to terminate.
        /// </summary>
        /// <exception cref="InvalidOperationException">if the child process has not been started.</exception>

        public void Stop()
        {
            StopChild();
        }

        /// <summary>
        /// Creates a channel for communicating with the WCF service in the child process.
        /// </summary>
        /// <typeparam name="TContract">An interface with the ServiceContract attribute implemented by the type loaded in the child process.</typeparam>
        /// <returns>The created channel.</returns>
        /// <exception cref="InvalidOperationException">if the child process is not executing.</exception>
        /// <exception cref="InvalidOperationException">if TContract is not a WCF ServiceContract.</exception>
        /// <exception cref="CommunicationException">if the host is unable to establish a WCF connection to the child process.</exception>
        /// <remarks>
        /// This method does not validate that the child actually implements TContract.
        /// Callers should catch CommunicationException when making method calls on the contract.
        /// </remarks>

        public WcfChildContract<TContract> CreateChannel<TContract>() where TContract : class
        {
            if (Status != HostProcessStatus.Executing)
            {
                throw new InvalidOperationException("The child process is not executing.");
            }

            return new WcfChildContract<TContract>(ChannelFactory<TContract>.CreateChannel(new NetNamedPipeBinding(), new EndpointAddress(_serviceAddress)));
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
