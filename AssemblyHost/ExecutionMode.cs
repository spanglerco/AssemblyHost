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
    /// The different ways to execute an IChildProcess.
    /// </summary>

    public enum ExecutionMode
    {
        /// <summary>
        /// An ExecutionMode that does not have a value.
        /// </summary>

        NotSet = 0,

        /// <summary>
        /// The Execute method is called on the main thread.
        /// The child process exits when the Execute method returns.
        /// The EndExecution method will not be called.
        /// </summary>

        Synchronous,

        /// <summary>
        /// The Execute method is called on the main thread.
        /// The child process exits when the EndExecution method returns.
        /// The EndExecution method will not be called until the Execute method returns.
        /// </summary>

        AsyncReturn,

        /// <summary>
        /// The Execute method is called on a background thread.
        /// The child process exits when the Execute method returns.
        /// The EndExecution method may be called during or after Execution
        /// to signal the type should return from the Execute method. It may
        /// not be called if Execute has already returned.
        /// </summary>

        AsyncThread
    }
}
