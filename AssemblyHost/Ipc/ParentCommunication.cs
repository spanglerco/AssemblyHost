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
using System.IO.Pipes;
using System.Collections.Generic;

namespace SpanglerCo.AssemblyHost.Ipc
{
    /// <summary>
    /// The communication layer for the parent process.
    /// </summary>

    internal sealed class ParentCommunication : Communication
    {
        private AnonymousPipeServerStream _readPipe;
        private AnonymousPipeServerStream _writePipe;

        /// <see cref="Communication.ReadPipe"/>

        protected override PipeStream ReadPipe
        {
            get
            {
                return _readPipe;
            }
        }

        /// <see cref="Communication.WritePipe"/>

        protected override PipeStream WritePipe
        {
            get
            {
                return _writePipe;
            }
        }

        /// <summary>
        /// Creates a new parent communication layer.
        /// </summary>

        public ParentCommunication()
        {
            _readPipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            _writePipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);

            CreateLink();
        }

        /// <summary>
        /// Adds the required command-line arguments for the child process to connect.
        /// </summary>
        /// <param name="args">The current list of arguments to add to.</param>

        public void AddChildArguments(IList<string> args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            // Reverse the order, because the parent's write pipe is the child's read pipe.

            args.Add(_writePipe.GetClientHandleAsString());
            args.Add(_readPipe.GetClientHandleAsString());
        }

        /// <summary>
        /// Notifies the parent communication that the child process has started.
        /// </summary>

        public void ChildProcessStarted()
        {
            _readPipe.DisposeLocalCopyOfClientHandle();
            _writePipe.DisposeLocalCopyOfClientHandle();
        }

        /// <see cref="IDisposable.Dispose"/>

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (_readPipe != null)
                {
                    _readPipe.Dispose();
                }

                if (_writePipe != null)
                {
                    _writePipe.Dispose();
                }
            }
        }
    }
}
