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
using System.ServiceModel;
using System.Threading.Tasks;
using System.Collections.Generic;

using SpanglerCo.AssemblyHost;

namespace SpanglerCo.AssemblyHostExample.Examples
{
    /// <summary>
    /// An AssemblyHost example for <see cref="WcfHostProcess"/> demonstrating more advanced features.
    /// </summary>
    /// <remarks>
    /// This example requires adding a reference to the System.ServiceModel assembly.
    /// It is suggested to become familiar with the WCF example first.
    /// </remarks>

    public sealed class WCFAdvanced : IExample
    {
        /// <see cref="IExample.Name"/>

        public string Name
        {
            get
            {
                return "WCF Advanced";
            }
        }

        /// <see cref="IExample.Description"/>

        public string Description
        {
            get
            {
                return "An example of the WCF (Windows Communication Foundation) host, which registers all service bindings on a class.\n\n" +
                       "This example shows more advanced features of WPF: OneWay operations, session data, and custom argument types.\n\n" +
                       "The input parameter is two numbers on which to perform multiple operations simultaneously.";
            }
        }

        /// <see cref="IExample.ParameterPrompt"/>

        public string ParameterPrompt
        {
            get
            {
                return "Two _Numbers:";
            }
        }

        /// <see cref="IExample.CanBeStopped"/>

        public bool CanBeStopped
        {
            get
            {
                // This example runs to completion and will stop the child process automatically.
                return false;
            }
        }

        /// <see cref="IExample.Run"/>

        public void Run(IExampleLogger logger, string parameter)
        {
            double value1, value2;
            string[] parameters = parameter.Split(' ');

            if (parameters.Length != 2
                || !double.TryParse(parameters[0], out value1)
                || !double.TryParse(parameters[1], out value2))
            {
                logger.Log("Error: the parameter must be two valid numbers.");
                return;
            }

            try
            {
                // Tell AssemblyHost to serve the HostedType class which implements one or more service contracts.
                // It's also possible to host a type that isn't even loaded in the current
                // process by specifying a path to the assembly and the name of the type.

                TypeArgument argument = new TypeArgument(typeof(HostedType));

                logger.Log("Creating WcfHostProcess");
                using (WcfHostProcess host = new WcfHostProcess(argument))
                {
                    // Log when the child process' status changes.
                    host.StatusChanged += (sender, args) => { logger.Log(string.Format("Child process moved to {0} status", host.Status)); };

                    logger.Log("Starting child process");
                    host.Start(true);
                    ChildProcess = Process.GetProcessById(host.ChildProcess.Id); // Needed for abort.

                    // At this point the child is listening for incoming requests.
                    // Create two simultaneous clients. Each will use its own instance of
                    // HostedType because it is marked as PerSession.

                    Action<object> clientTask = instanace =>
                    {
                        Random random = new Random((int)instanace);
                        string name = (int)instanace == 1 ? "math1" : "math2";
                        double value = (int)instanace == 1 ? value1 : value2;

                        logger.Log(name + ": Connecting to Math service");
                        using (WcfChildContract<IAsynchronousMath> math = host.CreateChannel<IAsynchronousMath>())
                        {
                            // Because BeginCalculations is marked as one way, the client can continue
                            // execution while the server's BeginCalculations method is still running.

                            logger.Log(name + ": Beginning calculations");
                            math.Contract.BeginCalculations(value);

                            // At this point the child process is performing a task for the parent,
                            // which could go do something useful while waiting for it to complete...
                            // Check the progress of the calculations until done.

                            bool running;
                            do
                            {
                                string currentOperation;
                                Thread.Sleep(random.Next(950) + 50);
                                running = math.Contract.IsCalculating(out currentOperation);
                                logger.Log(string.Format("{0}: {1}{2}", name, running ? "Running " : "Finished", currentOperation ?? string.Empty));
                            }
                            while (running);

                            logger.Log(name + ": Getting calculation results");
                            foreach (OperationResult result in math.Contract.GetResult())
                            {
                                logger.Log(string.Format("{0}: {1} {2} is {3}", name, value, result.Name, result.Result));
                            }

                            logger.Log(name + ": Closing connection");
                        }
                    };

                    // Start two clients and wait for them to both finish.
                    Task.WaitAll(Task.Factory.StartNew(clientTask, 1), Task.Factory.StartNew(clientTask, 2));
                    logger.Log("Finished using the service, stopping child");
                }
            }
            catch (Exception ex)
            {
                // WaitStopped will by default throw an exception if the child process
                // encounters an error.
                logger.Log(ex);
            }
        }

        #region WCF Service Contracts

        /// <summary>
        /// Contains the result on a single operation.
        /// </summary>
        /// <remarks>
        /// WCF allows any serializable type to be passed as arguments
        /// or return values. An alternative is the DataContract attribute.
        /// </remarks>

        [Serializable]
        public struct OperationResult
        {
            /// <summary>
            /// Gets the name of the operation.
            /// </summary>

            public string Name { get; private set; }

            /// <summary>
            /// Gets the result of the operation.
            /// </summary>

            public double Result { get; private set; }

            /// <summary>
            /// Creates a new operation result.
            /// </summary>
            /// <param name="name">The name of the operation.</param>
            /// <param name="result">The result of the operation.</param>

            public OperationResult(string name, double result)
                : this()
            {
                Name = name;
                Result = result;
            }
        }

        /// <summary>
        /// A WCF service contract for unary math operations performed asynchronously.
        /// </summary>

        [ServiceContract]
        public interface IAsynchronousMath
        {
            /// <summary>
            /// Begins running multiple calculations on a value. Only one calculation may be running at a time.
            /// </summary>
            /// <param name="value">The value on which to perform calculations.</param>
            /// <remarks>
            /// This operation is marked as one way. This means while the method continues running,
            /// the client will return after it starts. This allows the child process to perform
            /// work without the parent having to spawn a new thread to wait.
            /// </remarks>

            [OperationContract(IsOneWay = true)]
            void BeginCalculations(double value);

            /// <summary>
            /// Checks on the currently running calculations.
            /// </summary>
            /// <param name="currentOperation">
            /// If the operation is still running, contains the name of the current operation.
            /// </param>
            /// <returns>True if currently running calculations, false if complete.</returns>
            /// <remarks>Out parameters are also supported by WCF to return multiple values.</remarks>

            [OperationContract]
            bool IsCalculating(out string currentOperation);

            /// <summary>
            /// Returns the results from the completed calculations.
            /// </summary>
            /// <returns>The results from each operation.</returns>

            [OperationContract]
            OperationResult[] GetResult();
        }

        #endregion

        /// <summary>
        /// The class being hosted in the child process.
        /// </summary>
        /// <remarks>
        /// <see cref="InstanceContextMode"/> is used to tell WCF when to create
        /// a new instance of this class. For this example, the class needs to
        /// maintain state for each client that connects, called a session.
        /// 
        /// <see cref="ConcurrencyMode"/> is needed to allow progress checking.
        /// </remarks>

        [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession,
                         ConcurrencyMode = ConcurrencyMode.Multiple)]
        public class HostedType : IAsynchronousMath
        {
            private string _current = string.Empty;
            private Barrier _barrier = new Barrier(2);
            private List<OperationResult> _result = new List<OperationResult>();

            /// <see cref="IAsynchronousMath.BeginCalculations"/>
            /// <remarks>
            /// Because this operation is one way and concurrency is multiple,
            /// WCF handles the threading and the client doesn't block either.
            /// </remarks>

            public void BeginCalculations(double value)
            {
                PerformOperation("Double", value * 2);
                PerformOperation("Halve", value / 2);
                PerformOperation("Square", Math.Pow(value, 2));
                PerformOperation("Square Root", Math.Sqrt(value));

                _current = null; // Indicate done
                _barrier.RemoveParticipant();
            }

            /// <see cref="IAsynchronousMath.IsCalculating"/>

            public bool IsCalculating(out string currentOperation)
            {
                _barrier.SignalAndWait(); // Wait for an operation to start.
                currentOperation = _current;
                bool running = _current != null;

                _barrier.SignalAndWait(); // Tell the operation to finish.
                return running;
            }

            /// <see cref="IAsynchronousMath.GetResult"/>

            public OperationResult[] GetResult()
            {
                return _result.ToArray();
            }

            /// <summary>
            /// Performs a single operation.
            /// </summary>
            /// <param name="name">The name of the operation to perform.</param>
            /// <param name="result">The result of the operation.</param>
            /// <remarks>
            /// Demonstrates progress checking by only completing an
            /// operation each time IsCalculating is called.
            /// </remarks>

            private void PerformOperation(string name, double result)
            {
                _current = name;
                _barrier.SignalAndWait(); // Tell IsCalculating an operation has started.
                _barrier.SignalAndWait(); // Wait before finishing.
                _result.Add(new OperationResult(name, result));
            }
        }

        /// <see cref="IExample.Stop"/>

        void IExample.Stop(IExampleLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <see cref="IExample.ChildProcess"/>

        public Process ChildProcess { get; private set; }
    }
}
