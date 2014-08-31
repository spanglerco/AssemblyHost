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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using SpanglerCo.AssemblyHost.Ipc;

namespace SpanglerCo.AssemblyHost.Child
{
    /// <summary>
    /// Serves as the base class for the server-side host.
    /// </summary>

    internal abstract class HostServer : IDisposable
    {
        /// <summary>
        /// Attempts to load a type from an assembly.
        /// </summary>
        /// <param name="type">The type information to load.</param>
        /// <param name="communication">The object used to communication with the parent process.</param>
        /// <param name="loadedType">On success, contains the loaded type.</param>
        /// <returns>True if successful, false if not.</returns>

        protected static bool TryLoadType(TypeArgument type, Communication communication, out Type loadedType)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (communication == null)
            {
                throw new ArgumentNullException("communication");
            }

            try
            {
                Assembly loadedAssembly = Assembly.Load(type.ContainingAssembly.Name);
                loadedType = loadedAssembly.GetType(type.Name);

                if (loadedType == null)
                {
                    communication.SendMessage(MessageType.InvalidTypeError, "Type not found");
                    return false;
                }
                
                return true;
            }
            catch (ArgumentException ex)
            {
                communication.SendMessage(MessageType.InvalidTypeError, ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                communication.SendMessage(MessageType.AssemblyLoadError, ex);
            }
            catch (FileLoadException ex)
            {
                communication.SendMessage(MessageType.AssemblyLoadError, ex);
            }
            catch (BadImageFormatException ex)
            {
                communication.SendMessage(MessageType.AssemblyLoadError, ex);
            }

            loadedType = null;
            return false;
        }

        /// <summary>
        /// Determines whether or not a type implements another.
        /// </summary>
        /// <param name="loadedType">The type to check.</param>
        /// <param name="interfaceType">The type of the interface loadedType is required to implement.</param>
        /// <returns>True if loadedType implements interfaceType, false if not.</returns>

        protected static bool CheckInterface(Type loadedType, Type interfaceType)
        {
            if (loadedType == null)
            {
                throw new ArgumentNullException("loadedType");
            }

            if (interfaceType == null)
            {
                throw new ArgumentNullException("interfaceType");
            }

            try
            {
                string name;

                if (interfaceType.IsGenericType)
                {
                    name = interfaceType.GetGenericTypeDefinition().FullName;
                }
                else
                {
                    name = interfaceType.FullName;
                }

                return loadedType.GetInterface(name) == interfaceType;
            }
            catch (AmbiguousMatchException)
            {
                // The type implements multiple versions of the same generic interface.
                return loadedType.GetInterfaces().Contains(interfaceType);
            }
        }

        /// <summary>
        /// Attempts to get a type's method that takes no arguments.
        /// </summary>
        /// <param name="loadedType">The type whose method will be returned.</param>
        /// <param name="methodInfo">The method information to find.</param>
        /// <param name="communication">The object used to communication with the parent process.</param>
        /// <param name="method">On success, contains the method.</param>
        /// <returns>True if the method was found, false if not.</returns>

        protected static bool TryGetMethod(Type loadedType, MethodArgument methodInfo, Communication communication, out MethodInfo method)
        {
            if (loadedType == null)
            {
                throw new ArgumentNullException("loadedType");
            }

            if (methodInfo == null)
            {
                throw new ArgumentNullException("method");
            }

            if (communication == null)
            {
                throw new ArgumentNullException("communication");
            }

            BindingFlags flags;

            if (methodInfo.IsStatic)
            {
                flags = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static;
            }
            else
            {
                flags = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance;
            }

            method = loadedType.GetMethod(methodInfo.Name, flags, null, Type.EmptyTypes, null);

            if (method == null)
            {
                communication.SendMessage(MessageType.InvalidExecuteError, "Method not found");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Attempts to create an instance of a type using a default constructor.
        /// </summary>
        /// <param name="loadedType">The type whose instance will be created.</param>
        /// <param name="communication">The object used to communication with the parent process.</param>
        /// <param name="instance">On success, contains the created instance.</param>
        /// <returns>True if successful, false it not.</returns>

        protected static bool TryCreateInstance<TObject>(Type loadedType, Communication communication, out TObject instance)
        {
            if (loadedType == null)
            {
                throw new ArgumentNullException("loadedType");
            }

            if (communication == null)
            {
                throw new ArgumentNullException("communication");
            }

            try
            {
                instance = (TObject)Activator.CreateInstance(loadedType);
                return true;
            }
            catch (ArgumentException ex)
            {
                communication.SendMessage(MessageType.InvalidTypeError, ex.Message);
            }
            catch (NotSupportedException ex)
            {
                communication.SendMessage(MessageType.InvalidTypeError, ex.Message);
            }
            catch (TargetInvocationException ex)
            {
                communication.SendMessage(MessageType.ExecuteError, ex.InnerException);
            }
            catch (MemberAccessException ex)
            {
                communication.SendMessage(MessageType.InvalidTypeError, ex.Message);
            }
            catch (InvalidCastException ex)
            {
                communication.SendMessage(MessageType.InvalidTypeError, ex.Message);
            }

            instance = default(TObject);
            return false;
        }

        /// <summary>
        /// Parses the command-line arguments.
        /// </summary>
        /// <param name="args">The command-line arguments to parse.</param>
        /// <param name="communication">The object used to communication with the parent process.</param>
        /// <returns>True if successful, false if there was a parse error.</returns>

        public abstract bool ParseCommands(Queue<string> args, Communication communication);

        /// <summary>
        /// Executes the server command.
        /// This method must return before the process can exit.
        /// </summary>
        /// <param name="communication">The object used to communication with the parent process.</param>
        /// <returns>True if successful, false if there was an error executing and the process should exit immediately.</returns>

        public abstract bool Execute(Communication communication);

        /// <summary>
        /// Waits until it is time for the process to terminate.
        /// The default implementation returns immediately.
        /// </summary>
        /// <param name="communication">The object used to communication with the parent process.</param>
        /// <returns>True if successful, false if there was an error and the process should exit immediately.</returns>

        public virtual bool WaitForSignal(Communication communication)
        {
            return true;
        }

        /// <summary>
        /// Informs the server that the process has received the signal to terminate.
        /// The process will exit when this method returns.
        /// The default implementation returns immediately with no data.
        /// </summary>
        /// <param name="communication">The object used to communication with the parent process.</param>
        /// <param name="result">The data returned by the child.</param>
        /// <returns>True if successful, false if there was an error and the result should not be sent to the parent.</returns>

        public virtual bool TryTerminate(Communication communication, out string result)
        {
            result = null;
            return true;
        }

        /// <see cref="IDisposable.Dispose"/>

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes any resources used by this object. The default implementation does nothing.
        /// </summary>
        /// <param name="disposing">True if Dispose was called and managed resources should also be disposed..</param>

        protected virtual void Dispose(bool disposing)
        { }
    }
}
