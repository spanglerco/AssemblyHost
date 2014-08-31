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
    /// The communication layer for the child process.
    /// </summary>

    internal sealed class ChildCommunication : Communication
    {
        private AnonymousPipeClientStream _readPipe;
        private AnonymousPipeClientStream _writePipe;

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
        /// Creates a new child communication layer.
        /// </summary>
        /// <param name="args">The current list of command-line args.</param>
        /// <exception cref="ArgumentException">if the list of arguments does not contain enough elements.</exception>

        public ChildCommunication(Queue<string> args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            if (args.Count < 2)
            {
                throw new ArgumentException("Not enough arguments.", "args");
            }

            string readName = args.Dequeue();
            string writeName = args.Dequeue();

            _readPipe = new AnonymousPipeClientStream(PipeDirection.In, readName);
            _writePipe = new AnonymousPipeClientStream(PipeDirection.Out, writeName);

            CreateLink();
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
