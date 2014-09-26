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

    public sealed class WCF : IExample
    {
        private WcfHostProcess _host;
        private readonly object _hostLock = new object();

        /// <see cref="IExample.Name"/>

        public string Name
        {
            get
            {
                return "WCF";
            }
        }

        /// <see cref="IExample.Description"/>

        public string Description
        {
            get
            {
                return "An example of the WCF (Windows Communication Foundation) host, which registers all service bindings on a class.\n\n" +
                       "WcfHostProcess is useful when needing to communicate from the parent process to the child and transfer data between the two. The child process doesn't exit until stopped.\n\n" +
                       "The input parameter is a number on which to perform multiple operations. When entering a large number, use the Stop Example button to cancel the example.";
            }
        }

        /// <see cref="IExample.ParameterPrompt"/>

        public string ParameterPrompt
        {
            get
            {
                return "_Number:";
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
            double value;

            if (!double.TryParse(parameter, out value))
            {
                logger.Log("Error: the parameter must be a valid number.");
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

                        lock (_hostLock)
                        {
                            // Save the host so we can stop it later.
                            _host = host;
                        }

                        // At this point the child is listening for incoming requests.

                        logger.Log("Connecting to Math service");
                        using (WcfChildContract<IUnaryMath> math = host.CreateChannel<IUnaryMath>())
                        {
                            logger.Log(string.Format("Child reports {0} doubled is {1}", value, math.Contract.Double(value)));
                            logger.Log(string.Format("Child reports {0} halved is {1}", value, math.Contract.Half(value)));
                            logger.Log(string.Format("Child reports {0} squared is {1}", value, math.Contract.Square(value)));
                            logger.Log(string.Format("Child reports square root of {0} is {1}", value, math.Contract.Root(value)));

                            int integerValue = (int)value;
                            logger.Log(string.Format("Calculating Fibonacci number {0}", integerValue));
                            logger.Log(string.Format("Child reports Fibonacci number {0} is {1}", integerValue, math.Contract.Fibonacci(integerValue)));

                            logger.Log("Closing connection to Math service");
                        }

                        logger.Log("Finished using the service, stopping child");
                    }
                    finally
                    {
                        lock (_hostLock)
                        {
                            // Remove the saved reference prior to disposing: it's too late to stop the example.
                            _host = null;
                        }
                    }
                }
            }
            catch (FaultException ex)
            {
                // WCF returns expected errors as FaultException<TDetail>.
                // Rather than catching every possible exception, this will
                // extract the detail as a generic Exception.
                Type faultType = ex.GetType();

                if (faultType.IsGenericType && faultType.GetGenericTypeDefinition() == typeof(FaultException<>))
                {
                    Exception detailEx = faultType.GetProperty("Detail").GetValue(ex, null) as Exception;

                    if (detailEx != null)
                    {
                        logger.Log(detailEx);
                        return;
                    }
                }

                logger.Log(ex);
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
            lock (_hostLock)
            {
                // The host reference will be null if the example
                // isn't in a state where it can be stopped.

                if (_host != null)
                {
                    logger.Log("Connecting to service controller");
                    using (WcfChildContract<IServiceControl> control = _host.CreateChannel<IServiceControl>())
                    {
                        logger.Log("Signaling the child process to cancel");
                        control.Contract.CancelOperations();
                    }

                    // Don't actually signal the child to stop.
                    // Instead, let Run() finish having been canceled.
                }
            }
        }

        #region WCF Service Contracts

        /// <summary>
        /// A WCF service contract for unary math operations.
        /// </summary>

        [ServiceContract]
        public interface IUnaryMath
        {
            /// <summary>
            /// Doubles the value of x.
            /// </summary>
            /// <param name="x">The value to double.</param>
            /// <returns>x * 2</returns>

            [OperationContract]
            double Double(double x);

            /// <summary>
            /// Halves the value of x.
            /// </summary>
            /// <param name="x">The value to halve.</param>
            /// <returns>x / 2</returns>

            [OperationContract]
            double Half(double x);

            /// <summary>
            /// Squares the value of x.
            /// </summary>
            /// <param name="x">The value to square.</param>
            /// <returns>x ^ 2</returns>

            [OperationContract]
            double Square(double x);

            /// <summary>
            /// Takes the square root of x.
            /// </summary>
            /// <param name="x">The value to square root.</param>
            /// <returns>x ^ 1/2</returns>

            [OperationContract]
            double Root(double x);

            /// <summary>
            /// Calculates the x-th number in the Fibonacci sequence.
            /// </summary>
            /// <param name="x">The Fibonacci number to return.</param>
            /// <returns>Fib(x)</returns>
            /// <exception cref="FaultException&lt;ArgumentException&gt;">if x is not positive.</exception>
            /// <exception cref="FaultException&lt;OverflowException&gt;">if the operation exceeds the capacity of long.</exception>
            /// <exception cref="FaultException&lt;OperationCanceledException&gt;">if the operation is canceled before completing.</exception>

            [OperationContract]
            [FaultContract(typeof(ArgumentException))]
            [FaultContract(typeof(OverflowException))]
            [FaultContract(typeof(OperationCanceledException))]
            long Fibonacci(int x);
        }

        /// <summary>
        /// A WCF service contract for controlling the service.
        /// </summary>

        [ServiceContract]
        public interface IServiceControl
        {
            /// <summary>
            /// Signals the service to cancel any currently running operations.
            /// </summary>

            [OperationContract]
            void CancelOperations();
        }

        #endregion

        /// <summary>
        /// The class being hosted in the child process.
        /// </summary>
        /// <remarks>
        /// <see cref="ServiceBehaviorAttribute"/> is used to mark the
        /// contract as having multiple <see cref="ConcurrencyMode"/>. This
        /// allows multiple calls to run simultaneously on the same instance
        /// of the class. Another useful parameter to ServiceBehavior is
        /// <see cref="InstanceContextMode"/>.
        /// 
        /// Not IDisposable because AssemblyHost will not call Dispose anyway.
        /// This is okay because the resources will remain in use until right
        /// before the process is stopped.
        /// </remarks>

        [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
        public class HostedType : IUnaryMath, IServiceControl
        {
            // By default, WCF may create multiple instances of the class so make static.

            private static ManualResetEventSlim _cancel = new ManualResetEventSlim();

            /// <see cref="IUnaryMath.Double"/>

            public double Double(double x)
            {
                return x * 2;
            }

            /// <see cref="IUnaryMath.Half"/>

            public double Half(double x)
            {
                return x / 2;
            }

            /// <see cref="IUnaryMath.Square"/>

            public double Square(double x)
            {
                return Math.Pow(x, 2);
            }

            /// <see cref="IUnaryMath.Root"/>

            public double Root(double x)
            {
                return Math.Sqrt(x);
            }

            /// <see cref="IUnaryMath.Fibonacci"/>
            /// <remarks>
            /// WCF, built on SOAP, uses faults to convey error information and does
            /// not automatically map exceptions to faults. In a real remote WCF service,
            /// the service should declare a custom fault class that conveys specific
            /// information that doesn't contain things like call stacks. But for this
            /// example it is sufficient to simply wrap exceptions in a FaultException,
            /// which is how faults are returned from a service.
            /// </remarks>

            public long Fibonacci(int x)
            {
                if (x < 1)
                {
                    throw new FaultException<ArgumentException>(new ArgumentException("Must be a positive integer.", "x"));
                }

                long previous = 1;
                long result = 1;

                try
                {
                    for (int n = 3; n <= x; n++)
                    {
                        if (_cancel.Wait(0))
                        {
                            throw new FaultException<OperationCanceledException>(new OperationCanceledException());
                        }

                        // Artificially slow down the operation to show cancel behavior.
                        Thread.Sleep(100);

                        long temp = result;
                        result = checked(previous + result);
                        previous = temp;
                    }
                }
                catch (OverflowException ex)
                {
                    throw new FaultException<OverflowException>(ex);
                }

                return result;
            }

            /// <see cref="IServiceControl.CancelOperations"/>

            public void CancelOperations()
            {
                _cancel.Set();
            }
        }

        /// <see cref="IExample.ChildProcess"/>

        public Process ChildProcess { get; private set; }
    }
}
