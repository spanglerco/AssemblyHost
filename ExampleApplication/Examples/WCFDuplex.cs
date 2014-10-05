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

using SpanglerCo.AssemblyHost;

namespace SpanglerCo.AssemblyHostExample.Examples
{
    /// <summary>
    /// An AssemblyHost example for <see cref="WcfHostProcess"/>.
    /// </summary>
    /// <remarks>
    /// This example requires adding a reference to the System.ServiceModel assembly.
    /// </remarks>

    public sealed class WCFDuplex : IExample
    {
        private Callbacks _callbacks = new Callbacks();

        /// <summary>
        /// The number of milliseconds between progress updates during the example.
        /// </summary>

        private const int ProgressInterval = 500;

        /// <summary>
        /// The number of progress updates between checks to cancel during the example.
        /// </summary>

        private const int CancelInterval = 2;

        /// <see cref="IExample.Name"/>

        public string Name
        {
            get
            {
                return "WCF Duplex";
            }
        }

        /// <see cref="IExample.Description"/>

        public string Description
        {
            get
            {
                return "An example of the WCF (Windows Communication Foundation) host, which registers all service bindings on a class.\n\n" +
                       "This example shows how a duplex WCF service can issue callbacks to enable features like progress reporting for long-running asynchronous operations.\n\n" +
                       "The input parameter is the number of seconds the operation will take to complete. When entering a large number, use the Stop Example button to cancel the example.";
            }
        }

        /// <see cref="IExample.ParameterPrompt"/>

        public string ParameterPrompt
        {
            get
            {
                return "Secon_ds to Execute:";
            }
        }

        /// <see cref="IExample.CanBeStopped"/>

        public bool CanBeStopped
        {
            get
            {
                return true;
            }
        }

        /// <see cref="IExample.Run"/>

        public void Run(IExampleLogger logger, string parameter)
        {
            int seconds;

            if (!int.TryParse(parameter, out seconds) || seconds < 0)
            {
                logger.Log("Error: the parameter must be a non-negative integer.");
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
                    try
                    {
                        // Log when the child process' status changes.
                        host.StatusChanged += (sender, args) => { logger.Log(string.Format("Child process moved to {0} status", host.Status)); };

                        logger.Log("Starting child process");
                        host.Start(true);
                        ChildProcess = Process.GetProcessById(host.ChildProcess.Id); // Needed for abort.

                        // At this point the child is listening for incoming requests.
                        // Create the channel and pass in the callback object.

                        _callbacks.Logger = logger;
                        _callbacks.ShouldStop = false;
                        logger.Log("Connecting to service");
                        using (WcfChildContract<ILongOperation> client = host.CreateChannel<ILongOperation>(_callbacks))
                        {
                            logger.Log("Starting the operation");
                            client.Contract.StartOperation(seconds);

                            logger.Log("Waiting for operation to complete");
                            bool finished = client.Contract.WaitForOperation();

                            logger.Log(finished ? "Operation complete" : "Operation canceled");
                            logger.Log("Closing connection to service");
                        }

                        logger.Log("Finished using the service, stopping child");
                    }
                    finally
                    {
                        _callbacks.Logger = null;
                    }
                }
            }
            catch (Exception ex)
            {
                // WaitStopped will by default throw an exception if the child process
                // encounters an error.
                logger.Log(ex);
            }
        }

        /// <see cref="IExample.Stop"/>

        public void Stop(IExampleLogger logger)
        {
            // The operation will actually stop after the next callback.
            _callbacks.ShouldStop = true;
        }

        /// <summary>
        /// Implements the callbacks for the WCF service <see cref="ILongOperation"/>.
        /// </summary>
        /// <remarks>
        /// This is a client-side class that allows the child process to
        /// call back into the parent process for reporting progress.
        /// </remarks>

        private class Callbacks : ICallbacks
        {
            /// <summary>
            /// Gets or sets whether or not the operation should be canceled.
            /// </summary>

            public bool ShouldStop { get; set; }

            /// <summary>
            /// Gets or sets the logger to use on progress updates.
            /// </summary>

            public IExampleLogger Logger { get; set; }

            /// <see cref="ICallbacks.ReportProgress"/>

            public void ReportProgress(int remainingMilliseconds)
            {
                Logger.Log(string.Format("Operation in progress, {0} milliseconds remain.", remainingMilliseconds));
            }

            /// <see cref="ICallbacks.ShouldCancel"/>

            public bool ShouldCancel()
            {
                if (ShouldStop)
                {
                    Logger.Log("Sending cancel response from callback.");
                }

                return ShouldStop;
            }
        }

        #region WCF Service Contracts

        /// <summary>
        /// A WCF service contract for performing a long-running operation asynchronously.
        /// </summary>
        /// <remarks>
        /// Note the CallbackContract property of the <see cref="ServiceContractAttribute"/>,
        /// which specifies that the <see cref="ICallbacks"/> interface will be used for callbacks.
        /// </remarks>

        [ServiceContract(CallbackContract = typeof(ICallbacks))]
        public interface ILongOperation
        {
            /// <summary>
            /// Signals the service to start the long-running operation.
            /// </summary>
            /// <param name="seconds">How long the operation should take.</param>

            [OperationContract(IsOneWay = true)]
            void StartOperation(int seconds);

            /// <summary>
            /// Waits for the operation to complete before returning.
            /// </summary>
            /// <returns>True if the operation completed, false if it canceled.</returns>

            [OperationContract]
            bool WaitForOperation();
        }

        /// <summary>
        /// A WCF callback contract for reporting progress.
        /// </summary>
        /// <remarks>
        /// Note that the callback contract does not use <see cref="ServiceContractAttribute"/>
        /// but does use <see cref="OperationContractAttribute"/> for methods.
        /// </remarks>

        public interface ICallbacks
        {
            /// <summary>
            /// Callback to report progress from the long-running operation.
            /// </summary>
            /// <param name="remainingMilliseconds">The number of milliseconds remaining in the operation.</param>
            /// <remarks>
            /// Note how a callback may be one-way as well to avoid blocking the service.
            /// </remarks>

            [OperationContract(IsOneWay = true)]
            void ReportProgress(int remainingMilliseconds);

            /// <summary>
            /// Callback to determine if the long-running operation should cancel.
            /// </summary>
            /// <returns>True to cancel the operation, false to continue.</returns>
            /// <remarks>
            /// Also note that a callback doesn't have to be one-way. In this manner, a callback can
            /// be used to provide the service with additional information during a request.
            /// </remarks>

            [OperationContract]
            bool ShouldCancel();
        }

        #endregion

        /// <summary>
        /// The class being hosted in the child process.
        /// </summary>
        /// <remarks>
        /// <see cref="ServiceBehaviorAttribute"/> is used to mark the
        /// contract as having multiple <see cref="ConcurrencyMode"/>. This
        /// allows multiple calls to run simultaneously on the same instance
        /// of the class. Note that when using callbacks, the concurrency mode
        /// is important: two-way callbacks require reentrant or multiple.
        /// Reentrant mode allows another method to run while a callback is
        /// in progress, but the callback can't return until there are no other
        /// methods running.
        /// 
        /// Not IDisposable because AssemblyHost will not call Dispose anyway.
        /// This is okay because the resources will remain in use until right
        /// before the process is stopped.
        /// </remarks>

        [ServiceBehavior(
            ConcurrencyMode = ConcurrencyMode.Multiple,
            InstanceContextMode = InstanceContextMode.PerSession)]
        public class HostedType : ILongOperation
        {
            private bool _canceled;
            private ManualResetEventSlim _done = new ManualResetEventSlim();

            /// <see cref="ILongOperation.StartOperation"/>
            /// <remarks>
            /// Since this method is one-way, the client won't wait for it to complete.
            /// </remarks>

            public void StartOperation(int seconds)
            {
                // Get access to the callbacks provided by the client.
                ICallbacks callbacks = OperationContext.Current.GetCallbackChannel<ICallbacks>();
                int remaining = seconds * 1000;
                int cancelCheck = CancelInterval;

                while (remaining > 0)
                {
                    callbacks.ReportProgress(remaining);
                    --cancelCheck;

                    if (cancelCheck <= 0)
                    {
                        _canceled = callbacks.ShouldCancel();

                        if (_canceled)
                        {
                            break;
                        }

                        cancelCheck = CancelInterval;
                    }

                    Thread.Sleep(remaining > ProgressInterval ? ProgressInterval : remaining);
                    remaining -= ProgressInterval;
                }

                _done.Set();
            }

            /// <see cref="ILongOperation.WaitForOperation"/>

            public bool WaitForOperation()
            {
                _done.Wait();
                return !_canceled;
            }
        }

        /// <see cref="IExample.ChildProcess"/>

        public Process ChildProcess { get; private set; }
    }
}