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
using System.ServiceModel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using SpanglerCo.AssemblyHost.Ipc;

namespace SpanglerCo.AssemblyHost.Child
{
    /// <summary>
    /// Represents a host that hosts a WCF service from another assembly.
    /// </summary>

    internal sealed class WcfHostServer : HostServer
    {
        private ServiceHost _service;

        /// <see cref="HostServer.ParseCommands"/>

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The exception is sent to the parent process and we exit safely.")]
        public override bool ParseCommands(Queue<string> args, Communication communication)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            if (communication == null)
            {
                throw new ArgumentNullException("communication");
            }

            Type loadedType;
            TypeArgument argument = new TypeArgument(args);

            if (args.Count == 0)
            {
                throw new ArgumentException("Not enough arguments.", "args");
            }

            Uri address = new Uri(args.Dequeue());

            if (TryLoadType(argument, communication, out loadedType))
            {
                try
                {
                    _service = new ServiceHost(loadedType, address);
                    _service.AddDefaultEndpoints();
                    _service.Open();
                }
                catch (Exception ex)
                {
                    communication.SendMessage(MessageType.InvalidExecuteError, "Error creating WCF host", ex);
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <see cref="HostServer.Execute"/>

        public override bool Execute(Communication communication)
        {
            return true;
        }

        /// <see cref="HostServer.WaitForSignal"/>

        public override bool WaitForSignal(Communication communication)
        {
            if (communication == null)
            {
                throw new ArgumentNullException("communication");
            }

            string data;
            Exception ex;
            MessageType type;

            while (communication.TryReadMessage(out type, out data, out ex))
            {
                if (type == MessageType.SignalTerminate)
                {
                    return true;
                }
            }

            return false;
        }

        /// <see cref="HostServer.TryTerminate"/>

        public override bool TryTerminate(Communication communication, out string result)
        {
            if (_service != null)
            {
                try
                {
                    _service.Close();
                }
                catch (CommunicationException)
                {
                    _service.Abort();
                }
            }

            _service = null;
            result = null;
            return true;
        }

        /// <see cref="HostServer.Dispose(bool)"/>

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (_service != null)
                {
                    try
                    {
                        _service.Close();
                    }
                    catch (CommunicationException)
                    {
                        _service.Abort();
                    }

                    _service = null;
                }
            }
        }
    }
}
