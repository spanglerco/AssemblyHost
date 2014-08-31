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

namespace SpanglerCo.AssemblyHost
{
    /// <summary>
    /// Represents a type that can be hosted in a child process.
    /// Use the InterfaceHostProcess to create a new process that
    /// hosts a type that implements this interface.
    /// Implementing types must have a public, default constructor.
    /// </summary>

    public interface IChildProcess
    {
        /// <summary>
        /// Gets a result that is returned to the parent process before
        /// exiting. The value may be null to indicate there is no result.
        /// </summary>

        string Result { get; }

        /// <summary>
        /// Gets the ExecutionMode that determines how Execute and EndExecution is called.
        /// </summary>

        ExecutionMode Mode { get; }

        /// <summary>
        /// Called for the type to perform its main execution. The ExecutionMode
        /// determines the expected semantics.
        /// </summary>
        /// <param name="arguments">The argument string passed by the parent process, if any.</param>
        /// <param name="progressReporter">
        /// An object that allows the child process to report progress back to the parent.
        /// The progress reporter will be valid until the process begins exiting (as determined by
        /// the ExecutionMode). Calling methods on the object after, for example, a synchronous type
        /// returns from the execute method, may result in exceptions or lost progress.
        /// </param>
        /// <see cref="ExecutionMode"/>

        void Execute(string arguments, IProgressReporter progressReporter);

        /// <summary>
        /// Depending on the ExecutionMode, called to indicate that the type should
        /// complete its execution.
        /// </summary>
        /// <see cref="ExecutionMode"/>

        void EndExecution();
    }
}
