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
using System.Security;
using System.Reflection;
using System.Runtime.Remoting;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

using SpanglerCo.AssemblyHost.Ipc;
using SpanglerCo.AssemblyHost.Properties;

namespace SpanglerCo.AssemblyHost.Child
{
    /// <summary>
    /// The start-up class for the application.
    /// </summary>

    internal class Program : MarshalByRefObject
    {
        /// <summary>
        /// The main method for the application.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>

        private static void Main(string[] args)
        {
#if UNUSED // Used for debugging the child process. Remove this #if to enable.
            System.Diagnostics.Debugger.Launch();
#endif

            if (args.Length == 0)
            {
                Console.WriteLine(Resources.UsageError);
                return;
            }

            // The first argument is the absolute path to the assembly.

            AppDomainSetup setup = new AppDomainSetup();
            setup.ApplicationBase = args[0];

            AppDomain domain = AppDomain.CreateDomain("Assembly Host", null, setup);
            Program instance = (Program)domain.CreateInstanceFromAndUnwrap(Assembly.GetExecutingAssembly().Location, typeof(Program).FullName);

            instance.Execute(AppDomain.CurrentDomain, args);
        }

        private Communication _communication;
        private readonly object _lock = new object();

        /// <summary>
        /// Executes the hosted assembly.
        /// </summary>
        /// <param name="main">The main AppDomain for the application.</param>
        /// <param name="args">The command-line arguments.</param>

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Called cross-domain via instance.")]
        private void Execute(AppDomain main, string[] args)
        {
            // The first argument has already been processed.

            Queue<string> argList = new Queue<string>(args.Length - 1);

            for (int x = 1; x < args.Length; ++x)
            {
                argList.Enqueue(args[x]);
            }

            using (ChildCommunication communication = new ChildCommunication(argList))
            {
                HostServerType type;

                if (argList.Count == 0 || !Enum.TryParse(argList.Dequeue(), out type))
                {
                    communication.SendMessage(MessageType.InternalError, new ArgumentException("Invalid arguments."));
                }
                else
                {
                    lock (_lock)
                    {
                        // The UnhandledException event runs in the AppDomain that owns
                        // the thread, not the one that threw the exception. Additionally,
                        // the main AppDomain will raise the event for unhandled exceptions
                        // in any other AppDomain as well. So register for the event on the main.

                        _communication = communication;
                        main.UnhandledException += OnUnhandledException;
                    }

                    try
                    {
                        using (HostServer host = HostServerFactory.CreateHostServer(type))
                        {
                            if (host.ParseCommands(argList, communication))
                            {
                                communication.SendMessage(MessageType.HostStarted);

                                if (host.Execute(communication))
                                {
                                    if (host.WaitForSignal(communication))
                                    {
                                        string result;
                                        if (host.TryTerminate(communication, out result))
                                        {
                                            communication.SendMessage(MessageType.HostFinished, result);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (ArgumentException)
                    {
                        communication.SendMessage(MessageType.ArgumentParseError);
                    }
                    catch (Exception ex)
                    {
                        communication.SendMessage(MessageType.InternalError, ex);
                        communication.WaitForRead();
                        throw;
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            main.UnhandledException -= OnUnhandledException;
                            _communication = null;
                        }
                    }
                }

                communication.WaitForRead();
            }
        }

        /// <summary>
        /// Handles the <see cref="AppDomain.UnhandledException"/> event.
        /// </summary>
        /// <remarks>
        /// The <see cref="HandleProcessCorruptedStateExceptionsAttribute"/> and
        /// <see cref="SecurityCriticalAttribute"/> are required to catch certain
        /// exceptions like <see cref="AccessViolationException"/>.
        /// </remarks>

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            lock (_lock)
            {
                if (_communication != null)
                {
                    TargetInvocationException ex = e.ExceptionObject as TargetInvocationException;

                    if (ex != null)
                    {
                        _communication.SendMessage(MessageType.ExecuteError, ex.InnerException);
                    }
                    else
                    {
                        _communication.SendMessage(MessageType.ExecuteError, e.ExceptionObject as Exception);
                    }
                    
                    _communication.WaitForRead();
                }
            }

            Environment.Exit(1);
        }
    }
}
