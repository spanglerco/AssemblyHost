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
using System.Collections.Generic;

namespace SpanglerCo.AssemblyHost.Child
{
    /// <summary>
    /// A factory for creating HostServers.
    /// </summary>

    internal static class HostServerFactory
    {
        /// <summary>
        /// Creates a new host server of the specified type.
        /// </summary>
        /// <param name="type">The type of host server to create.</param>
        /// <returns>The created host.</returns>
        /// <exception cref="ArgumentException">if there is no host implemented for type.</exception>

        public static HostServer CreateHostServer(HostServerType type)
        {
            switch (type)
            {
                case HostServerType.Method:
                    return new MethodHostServer();

                case HostServerType.Interface:
                    return new InterfaceHostServer();

                case HostServerType.WcfService:
                    return new WcfHostServer();
                
                default:
                    throw new ArgumentException("Unknown HostServer type", "type");
            }
        }
    }
}
