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
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace SpanglerCo.AssemblyHostExample.Utility
{
    /// <summary>
    /// Contains native imported methods.
    /// </summary>

    internal static class NativeMethods
    {
        [DllImport("ole32.dll")]
        public static extern int GetRunningObjectTable(uint reserved, out IRunningObjectTable rot);

        [DllImport("ole32.dll")]
        public static extern int CreateBindCtx(uint reserved, out IBindCtx bc);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
