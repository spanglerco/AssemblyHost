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
using System.Threading;
using System.Diagnostics;

using SpanglerCo.AssemblyHost;

namespace SpanglerCo.AssemblyHostExample
{
    /// <summary>
    /// The interface for all examples.
    /// </summary>

    public interface IExample
    {
        /// <summary>
        /// Gets the name of the example.
        /// </summary>

        string Name { get; }

        /// <summary>
        /// Gets a description for the example.
        /// </summary>

        string Description { get; }

        /// <summary>
        /// Gets a message to prompt for a parameter to pass to the example,
        /// or null if the example does not use a parameter.
        /// </summary>

        string ParameterPrompt { get; }

        /// <summary>
        /// Gets whether or not the example supports being stopped during execution.
        /// </summary>

        bool CanBeStopped { get; }

        /// <summary>
        /// Runs the example.
        /// </summary>
        /// <param name="logger">An object that the example can use to log messages during execution.</param>
        /// <param name="parameter">The parameter supplied to the example based on <see cref="ParameterPrompt"/>.</param>

        void Run(IExampleLogger logger, string parameter);

        /// <summary>
        /// Requests the example stop during a <see cref="Run"/>.
        /// </summary>
        /// <param name="logger">An object that the example can use to log messages while stopping.</param>

        void Stop(IExampleLogger logger);

        /// <summary>
        /// Gets the process information for the child for the current execution.
        /// </summary>

        Process ChildProcess { get; }
    }
}
