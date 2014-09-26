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

namespace SpanglerCo.AssemblyHostExample
{
    /// <summary>
    /// Represents a logger for examples to show current status.
    /// </summary>

    public interface IExampleLogger
    {
        /// <summary>
        /// Logs a message to show status.
        /// </summary>
        /// <param name="message">The message to log.</param>

        void Log(string message);

        /// <summary>
        /// Logs an exception message.
        /// </summary>
        /// <param name="ex">The exception to log.</param>

        void Log(Exception ex);
    }
}
