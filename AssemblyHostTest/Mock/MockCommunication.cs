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

using SpanglerCo.AssemblyHost.Ipc;

namespace SpanglerCo.UnitTests.AssemblyHost.Mock
{
    /// <summary>
    /// Test class that serves as either end of the communication pipe.
    /// </summary>

    internal class MockCommunication : Communication
    {
        private PipeStream _readPipe;
        private PipeStream _writePipe;

        /// <summary>
        /// Returns an open delegate that can be used to call Communication.WaitForRead.
        /// </summary>
        /// <see cref="Communication.WaitForRead"/>

        public static Action<MockCommunication> WaitForReadDelegate
        {
            get
            {
                return (Action<Communication>)Delegate.CreateDelegate(typeof(Action<Communication>), typeof(Communication).GetMethod("WaitForRead"));
            }
        }

        /// <summary>
        /// Returns a delegate that can be used to call Communication.TryReadMessage and return the result in a struct.
        /// </summary>
        /// <see cref="Communication.TryReadMessage"/>

        public static Func<MockCommunication, ReadMessageResult> TryReadMessageDelegate
        {
            get
            {
                return TryReadMessageTask;
            }
        }

        /// <summary>
        /// A struct containing each output from the Communication.TryReadMessage method.
        /// </summary>
        /// <see cref="Communication.TryReadMessage"/>

        public struct ReadMessageResult
        {
            public bool Success { get; private set; }
            public MessageType Type { get; private set; }
            public string Data { get; private set; }
            public Exception Exception { get; private set; }

            public ReadMessageResult(bool success, MessageType type, string data, Exception ex)
                : this()
            {
                Success = success;
                Type = type;
                Data = data;
                Exception = ex;
            }
        }

        /// <summary>
        /// Wrapper around TryReadMessage that returns all results in a struct so it can
        /// be called as a delegate.
        /// </summary>
        /// <param name="communication">The communication to read from.</param>
        /// <returns>The results of the read in a single struct.</returns>

        private static ReadMessageResult TryReadMessageTask(Communication communication)
        {
            MessageType type;
            string data;
            Exception ex;
            bool result = communication.TryReadMessage(out type, out data, out ex);
            return new ReadMessageResult(result, type, data, ex);
        }

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
        /// Creates a new parent Communication instance.
        /// </summary>

        public MockCommunication()
        {
            _readPipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.None);
            _writePipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None);

            CreateLink();
        }

        /// <summary>
        /// Creates a new child Communication instance.
        /// </summary>
        /// <param name="parent">The parent Communication instance to connect to.</param>

        public MockCommunication(MockCommunication parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            AnonymousPipeServerStream serverRead = parent.ReadPipe as AnonymousPipeServerStream;
            AnonymousPipeServerStream serverWrite = parent.WritePipe as AnonymousPipeServerStream;

            if (serverRead == null || serverWrite == null)
            {
                throw new ArgumentException("Must pass in a parent communication instance.", "parent");
            }

            _readPipe = new AnonymousPipeClientStream(PipeDirection.In, serverWrite.ClientSafePipeHandle);
            _writePipe = new AnonymousPipeClientStream(PipeDirection.Out, serverRead.ClientSafePipeHandle);

            CreateLink();
        }

        /// <see cref="Communication.Dispose"/>

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _readPipe.Dispose();
                _writePipe.Dispose();
            }
        }
    }
}
