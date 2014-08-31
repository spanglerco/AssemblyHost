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
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SpanglerCo.AssemblyHost.Ipc
{
    /// <summary>
    /// Represents the communication layer between the parent and host processes.
    /// </summary>

    internal abstract class Communication : IDisposable
    {
        private BinaryReader _readStream;
        private BinaryWriter _writeStream;

        /// <summary>
        /// Represents the different types of payloads sent between processes.
        /// </summary>

        [Flags]
        private enum PayloadTypes : byte
        {
            /// <summary>
            /// The packet does not contain a payload.
            /// </summary>

            None = 0,

            /// <summary>
            /// The packet contains string data.
            /// </summary>

            String = 1,

            /// <summary>
            /// The packet that contains exception data.
            /// </summary>

            Exception = 2,
        }

        /// <summary>
        /// Gets the pipe used for reading.
        /// </summary>

        protected abstract PipeStream ReadPipe { get; }

        /// <summary>
        /// Gets the pipe used for writing.
        /// </summary>

        protected abstract PipeStream WritePipe { get; }

        /// <summary>
        /// Establishes the link between the parent and child processes using the read and write pipes.
        /// </summary>

        protected void CreateLink()
        {
            _readStream = new BinaryReader(ReadPipe);
            _writeStream = new BinaryWriter(WritePipe);
        }

        /// <summary>
        /// Waits for all of the sent data to be read by the other process.
        /// </summary>

        public void WaitForRead()
        {
            try
            {
                WritePipe.WaitForPipeDrain();
            }
            catch (ObjectDisposedException)
            { }
            catch (IOException)
            { }
        }

        /// <summary>
        /// Sends a message to the other process with no data to the listening process.
        /// </summary>
        /// <param name="type">The type of message to send.</param>
        /// <returns>True if the message was sent, false if the pipe is closed.</returns>

        public bool SendMessage(MessageType type)
        {
            return SendMessage(type, null, null);
        }

        /// <summary>
        /// Sends a message to the other process with string data to the listening process.
        /// </summary>
        /// <param name="type">The type of message to send.</param>
        /// <param name="data">The data to send with the message.</param>
        /// <returns>True if the message was sent, false if the pipe is closed.</returns>

        public bool SendMessage(MessageType type, string data)
        {
            return SendMessage(type, data, null);
        }

        /// <summary>
        /// Sends a message to the other process with exception information to the listening process.
        /// </summary>
        /// <param name="type">The type of message to send.</param>
        /// <param name="ex">The exception to send with the message.</param>
        /// <returns>True if the message was sent, false if the pipe is closed.</returns>
        /// <remarks>
        /// Only exception types in the System assembly will have their types maintained by TryReadMessage.
        /// In order to prevent loading unwanted assemblies in the parent process, other exception types
        /// will appear as TargetInvocationExceptions whose message will contain the original type.
        /// </remarks>

        public bool SendMessage(MessageType type, Exception ex)
        {
            return SendMessage(type, null, ex);
        }

        /// <summary>
        /// Sends a message to the other process with exception information to the listening process.
        /// </summary>
        /// <param name="type">The type of message to send.</param>
        /// <param name="data">The data to send with the message.</param>
        /// <param name="ex">The exception to send with the message.</param>
        /// <returns>True if the message was sent, false if the pipe is closed.</returns>
        /// <remarks>
        /// Only exception types in the System assembly will have their types maintained by TryReadMessage.
        /// In order to prevent loading unwanted assemblies in the parent process, other exception types
        /// will appear as TargetInvocationExceptions whose message will contain the original type.
        /// </remarks>

        public bool SendMessage(MessageType type, string data, Exception ex)
        {
            Debug.Assert(_writeStream != null, "CreateLink was not called");

            if (!WritePipe.IsConnected)
            {
                return false;
            }

            try
            {
                PayloadTypes payload = PayloadTypes.None;

                if (data != null)
                {
                    payload |= PayloadTypes.String;
                }

                if (ex != null)
                {
                    payload |= PayloadTypes.Exception;
                }

                _writeStream.Write((byte)payload);
                _writeStream.Write((ushort)type);

                if (data != null)
                {
                    _writeStream.Write(data);
                }

                if (ex != null)
                {
                    _writeStream.Write(ex.GetType().FullName); // Not AssemblyQualifiedName to prevent loading assemblies.
                    _writeStream.Write(ex.Message ?? string.Empty);
                }

                _writeStream.Flush();
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to read a message from the other process.
        /// </summary>
        /// <param name="type">On success, contains the type of message read.</param>
        /// <param name="stringData">On success, contains the string data of the message, or null if there is no string data.</param>
        /// <param name="exData">On success, contains the exception data of the message, or null if there is no exception data.</param>
        /// <returns>True if the read was successful, false if the pipe is closed.</returns>

        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", Justification = "Try pattern with multiple outputs.")]
        public bool TryReadMessage(out MessageType type, out string stringData, out Exception exData)
        {
            Debug.Assert(_readStream != null, "CreateLink was not called");

            type = MessageType.NotSet;
            stringData = null;
            exData = null;

            if (!ReadPipe.IsConnected)
            {
                return false;
            }

            try
            {
                PayloadTypes payload = (PayloadTypes)_readStream.ReadByte();
                type = (MessageType)_readStream.ReadUInt16();

                if (payload.HasFlag(PayloadTypes.String))
                {
                    stringData = _readStream.ReadString();
                }

                if (payload.HasFlag(PayloadTypes.Exception))
                {
                    string exceptionType = _readStream.ReadString();
                    string exceptionMessage = _readStream.ReadString();
                    Type exception = Type.GetType(exceptionType);

                    if (exception == null)
                    {
                        exception = typeof(TargetInvocationException);
                        exceptionMessage = exceptionType + ": " + exceptionMessage;
                    }

                    ConstructorInfo constructor = exception.GetConstructor(new Type[] { typeof(string) });

                    if (constructor != null)
                    {
                        try
                        {
                            exData = (Exception)constructor.Invoke(new object[] { exceptionMessage });
                        }
                        catch (MemberAccessException)
                        { }
                    }

                    if (exData == null)
                    {
                        exData = new TargetInvocationException(exceptionType + ": " + exceptionMessage, null);
                    }
                }

                return true;

            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        /// <see cref="IDisposable.Dispose"/>

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources used by the class.
        /// </summary>
        /// <param name="disposing">True if being called by dispose, false if being finalized.</param>

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _readStream.Dispose();
                _writeStream.Dispose();
            }
        }
    }
}
