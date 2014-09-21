// Copyright © 2014 Paul Spangler
//
// Licensed under the MIT License (the "License");
// you may not use this file except in compliance with the License.
// You should have received a copy of the License with this software.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Diagnostics;

using EnvDTE;

namespace SpanglerCo.AssemblyHostExample.Utility
{
    /// <summary>
    /// Provides a wrapper for communicating with an instance of Visual Studio over DTE.
    /// </summary>

    internal sealed class VisualStudioCommunication : IDisposable
    {
        private DTE _dte;

        /// <summary>
        /// Connects to an instance of Visual Studio that has a solution open.
        /// </summary>
        /// <param name="solutionFile">The path to the solution file to look for.</param>
        /// <exception cref="COMException">on error looking for a Visual Studio instance.</exception>
        /// <exception cref="ArgumentException">if there isn't an instance of Visual Studio with the specified solution open.</exception>

        public VisualStudioCommunication(string solutionFile)
        {
            IRunningObjectTable table;

            if (NativeMethods.GetRunningObjectTable(0, out table) != 0)
            {
                throw new COMException("Unable to query running object table.");
            }

            try
            {
                IEnumMoniker enumMoniker;
                table.EnumRunning(out enumMoniker);

                if (enumMoniker == null)
                {
                    throw new COMException("Unable to enumerate running object table.");
                }

                try
                {
                    IMoniker[] moniker = new IMoniker[1];

                    while (enumMoniker.Next(1, moniker, IntPtr.Zero) == 0)
                    {
                        try
                        {
                            IBindCtx context;
                            if (NativeMethods.CreateBindCtx(0, out context) != 0)
                            {
                                throw new COMException("Unable to create binding context.");
                            }

                            string name;
                            moniker[0].GetDisplayName(context, null, out name);

                            if (name != null && name.StartsWith("!VisualStudio.DTE."))
                            {
                                object dteObject;

                                if (table.GetObject(moniker[0], out dteObject) == 0)
                                {
                                    try
                                    {
                                        DTE dte = dteObject as DTE;

                                        if (dte != null && dte.Solution != null && dte.Solution.FullName == solutionFile)
                                        {
                                            _dte = dte;
                                            dteObject = null;
                                            return;
                                        }
                                    }
                                    finally
                                    {
                                        if (dteObject != null)
                                        {
                                            Marshal.ReleaseComObject(dteObject);
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            Marshal.ReleaseComObject(moniker[0]);
                        }
                    }

                    throw new ArgumentException("No instance for the solution was found.", "solutionFile");
                }
                finally
                {
                    Marshal.ReleaseComObject(enumMoniker);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(table);
            }
        }

        /// <summary>
        /// Opens a file in the connected Visual Studio instance.
        /// </summary>
        /// <param name="file">The path to the file to open.</param>
        /// <exception cref="COMException">if there is an error making the request to open the file.</exception>

        public void OpenFile(string file)
        {
            _dte.ExecuteCommand("File.OpenFile", file);
        }

        /// <summary>
        /// Brings the connected Visual Studio instance's main window to the foreground.
        /// </summary>

        public void BringToFront()
        {
            int handle = _dte.MainWindow.HWnd;
            NativeMethods.SetForegroundWindow(new IntPtr(handle));
        }

        /// <see cref="IDisposable.Dispose"/>

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer for disposing unmanaged resources.
        /// </summary>

        ~VisualStudioCommunication()
        {
            Dispose(false);
        }

        /// <summary>
        /// Called to dispose resources.
        /// </summary>
        /// <param name="disposing">True if called by Dispose, false if not.</param>

        private void Dispose(bool disposing)
        {
            if (_dte != null)
            {
                Marshal.FinalReleaseComObject(_dte);
                _dte = null;
            }
        }
    }
}
