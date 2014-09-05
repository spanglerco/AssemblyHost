using System;
using System.Threading;

using SpanglerCo.AssemblyHost;

namespace SpanglerCo.AssemblyHostExample.Examples
{
    /// <summary>
    /// The base class for all examples.
    /// </summary>

    public abstract class ExampleBase
    {
        private ManualResetEventSlim _stoppedEvent;

        protected IExampleLogger Logger { get; private set; }

        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract string ParameterPrompt { get; }

        public void Run(IExampleLogger logger, string parameter)
        {
            Logger = logger;
            Run(parameter);
        }

        protected void LogStatusChanges(HostProcess host, ManualResetEventSlim stoppedEvent)
        {
            _stoppedEvent = stoppedEvent;
            host.StatusChanged += OnStatusChangedLogger;
        }

        private void OnStatusChangedLogger(object sender, EventArgs e)
        {
            HostProcess host = sender as HostProcess;

            if (host == null)
            {
                throw new ArgumentException("Expected a HostProcess", "sender");
            }

            Logger.Log(string.Format("Child process moved to {0} status", host.Status));

            if (_stoppedEvent != null
                && (host.Status == HostProcessStatus.Stopping
                 || host.Status == HostProcessStatus.Error))
            {
                _stoppedEvent.Set();
                _stoppedEvent = null;
            }
        }

        protected abstract void Run(string parameter);
    }
}
