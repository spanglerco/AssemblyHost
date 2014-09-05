using System;
using System.Diagnostics;
using System.Collections.Generic;

using SpanglerCo.AssemblyHost;
using System.Threading;

namespace SpanglerCo.AssemblyHostExample.Examples
{
    public class Method : ExampleBase
    {
        private const string ParameterVariable = "ExampleParameter";

        public override string Name
        {
            get
            {
                return "Method";
            }
        }

        public override string Description
        {
            get
            {
                return "An example of the Method host, which executes a single instance method and returns the result. A parameter is passed using the environment.";
            }
        }

        public override string ParameterPrompt
        {
            get
            {
                return "Message to echo";
            }
        }

        public override void Run(IExampleLogger logger, string parameter)
        {
            logger.Log("Adding parameter to child environment");
            ProcessStartInfo info = new ProcessStartInfo();
            info.CreateNoWindow = true;
            info.EnvironmentVariables.Add(ParameterVariable, parameter);

            logger.Log("Creating MethodHostProcess");
            using (MethodHostProcess host = new MethodHostProcess(new MethodArgument(typeof(HostedType).GetMethod("Execute")), info))
            {
                using (ManualResetEventSlim stoppedEvent = new ManualResetEventSlim())
                {
                    LogStatusChanges(host, stoppedEvent);
                    logger.Log("Starting child process");
                    host.Start(false);

                    // Go do something useful while we wait for the child to finish...

                    logger.Log("Waiting for child process to finish");
                    
                    if (!stoppedEvent.Wait(5000))
                    {
                        logger.Log("Timeout waiting for child process!");
                        throw new TimeoutException();
                    }

                    
                }
            }
        }

        public class HostedType
        {
            private string _parameter;

            public HostedType()
            {
                _parameter = Environment.GetEnvironmentVariable(ParameterVariable);

                if (string.IsNullOrEmpty(_parameter))
                {
                    throw new InvalidOperationException("Environment variable not set");
                }
            }

            public string Execute()
            {
                return string.Format("Process {0} says {1}", Process.GetCurrentProcess().Id, _parameter);
            }
        };
    }
}
