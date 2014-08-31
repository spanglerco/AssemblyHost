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
using System.Reflection;
using System.Collections.Generic;

using SpanglerCo.AssemblyHost.Ipc;

namespace SpanglerCo.AssemblyHost.Child
{
    /// <summary>
    /// Represents a host that calls a static or instance method during execution.
    /// </summary>

    internal sealed class MethodHostServer : HostServer
    {
        private string _result;
        private object _instance;
        private MethodInfo _method;

        /// <see cref="HostServer.ParseCommands"/>

        public override bool ParseCommands(Queue<string> args, Communication communication)
        {
            if (communication == null)
            {
                throw new ArgumentNullException("communication");
            }

            Type loadedType;
            MethodArgument argument = new MethodArgument(args);

            if (TryLoadType(argument.ContainingType, communication, out loadedType) &&
                TryGetMethod(loadedType, argument, communication, out _method))
            {
                return argument.IsStatic || TryCreateInstance(loadedType, communication, out _instance);
            }

            return false;
        }
        
        /// <see cref="HostServer.Execute"/>

        public override bool Execute(Communication communication)
        {
            if (_method == null)
            {
                throw new InvalidOperationException("The method was not loaded correctly.");
            }

            if (communication == null)
            {
                throw new ArgumentNullException("communication");
            }

            try
            {
                object result = _method.Invoke(_instance, null);

                if (result != null)
                {
                    if (result.GetType().GetMethod("ToString", Type.EmptyTypes).DeclaringType != typeof(object))
                    {
                        _result = result.ToString();
                    }
                }

                return true;
            }
            catch (TargetInvocationException ex)
            {
                communication.SendMessage(MessageType.ExecuteError, ex.InnerException);
                return false;
            }
        }

        /// <see cref="HostServer.TryTerminate"/>

        public override bool TryTerminate(Communication communication, out string result)
        {
            result = _result;
            return true;
        }
    }
}
