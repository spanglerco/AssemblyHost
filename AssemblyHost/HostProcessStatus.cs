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

namespace SpanglerCo.AssemblyHost
{
    /// <summary>
    /// Represents the different statuses of a HostProcess.
    /// </summary>

    public enum HostProcessStatus
    {
        /// <summary>
        /// A HostProcessStatus that does not have a value.
        /// </summary>

        NotSet = 0,

        /// <summary>
        /// The process has not been started.
        /// </summary>

        NotStarted,

        /// <summary>
        /// The process has been started but is not yet responding.
        /// </summary>

        Starting,

        /// <summary>
        /// The process is executing the hosted assembly.
        /// </summary>

        Executing,

        /// <summary>
        /// The process has been signaled to stop but is still executing.
        /// </summary>

        Stopping,
        
        /// <summary>
        /// The process has completed execution and is stopped.
        /// </summary>

        Stopped,

        /// <summary>
        /// The process terminated with an error.
        /// </summary>

        Error
    }
}
