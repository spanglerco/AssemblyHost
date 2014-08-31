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
using System.Diagnostics.CodeAnalysis;

namespace SpanglerCo.AssemblyHost.Ipc
{
    /// <summary>
    /// Represents the different types of messages to send with Communication.
    /// </summary>

    [SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32", Justification="Needs to be a type supported by BinaryWriter.")]
    internal enum MessageType : ushort
    {
        /// <summary>
        /// A MessageType that does not have a value.
        /// </summary>

        NotSet = 0,

        /// <summary>
        /// Reports the child has started executing. No data.
        /// </summary>

        HostStarted,

        /// <summary>
        /// Reports an error in parsing the command-line arguments. No data.
        /// </summary>

        ArgumentParseError,

        /// <summary>
        /// Reports an error loading the assembly. Data contains the exception type and message.
        /// </summary>

        AssemblyLoadError,

        /// <summary>
        /// Reports the given type is invalid (e.g. not found or no default constructor when needed). Data contains a message and may contain an exception.
        /// </summary>

        InvalidTypeError,

        /// <summary>
        /// Reports execution cannot start (e.g. method not found). Data contains a message and may contain an exception.
        /// </summary>

        InvalidExecuteError,

        /// <summary>
        /// Reports there was an error while executing. Data contains exception type and message.
        /// </summary>

        ExecuteError,

        /// <summary>
        /// Signals the child to stop executing. No data.
        /// </summary>

        SignalTerminate,

        /// <summary>
        /// Reports the child has stopped executing. Data is string results.
        /// </summary>

        HostFinished,

        /// <summary>
        /// Reports progress from the executing child. Data is string progress.
        /// </summary>

        Progress,

        /// <summary>
        /// Reports the an internal error in the child process. Data contains the exception type and message.
        /// </summary>

        InternalError,

        /// <summary>
        /// Signals the parent that the child is waiting for a SignalTerminate message. No data.
        /// </summary>

        RequestTerminate
    }
}
