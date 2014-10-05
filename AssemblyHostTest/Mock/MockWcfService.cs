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
using System.Threading;
using System.ServiceModel;

namespace SpanglerCo.UnitTests.AssemblyHost.Mock
{
    /// <summary>
    /// Test service contract interface.
    /// </summary>

    [ServiceContract]
    public interface ITestContract
    {
        /// <summary>
        /// Returns the current value represented by the service.
        /// </summary>

        [OperationContract]
        int GetValue();
    }

    /// <summary>
    /// Test service contract interface containing additional operations.
    /// </summary>

    [ServiceContract]
    public interface ITestContract2
    {
        /// <summary>
        /// Increments the value represented by the service.
        /// </summary>

        [OperationContract]
        void IncrementValue();

        /// <summary>
        /// Sets the value represented by the service.
        /// </summary>
        /// <param name="value">The new value.</param>

        [OperationContract]
        void SetValue(int value);
    }

    /// <summary>
    /// Duplex service contract interface.
    /// </summary>

    [ServiceContract(CallbackContract = typeof(ICallbackContract))]
    public interface IDuplexContract
    {
        /// <summary>
        /// Registers the current session to be notified on the next value changed via callback.
        /// </summary>
        /// <param name="milliseconds">The maximum amount of time to wait for a value change.</param>

        [OperationContract(IsOneWay = true)]
        void NotifyValueChanged(int milliseconds);
    }

    /// <summary>
    /// The callback contract for <see cref="IDuplexContract"/>.
    /// </summary>

    public interface ICallbackContract
    {
        /// <summary>
        /// Called to signal that the value changed notification has been registered.
        /// </summary>

        [OperationContract(IsOneWay = true)]
        void NotifyReady();

        /// <summary>
        /// Called when the value represented by the service changes.
        /// </summary>
        /// <param name="newValue">The new value.</param>

        [OperationContract(IsOneWay = true)]
        void ValueChanged(int newValue);

        /// <summary>
        /// Called to signal that the value changed notification has been unregistered.
        /// </summary>

        [OperationContract(IsOneWay = true)]
        void NotifyDone();
    }

    /// <summary>
    /// Test service contract interface that isn't implemented.
    /// </summary>

    [ServiceContract]
    public interface IUnusedContract
    {
        /// <summary>
        /// Echoes a string value.
        /// </summary>
        /// <param name="value">The value to echo.</param>
        /// <returns>The echoed value.</returns>

        [OperationContract]
        string Echo(string value);
    }

    /// <summary>
    /// A test interface that is not a service contract.
    /// </summary>

    public interface INonContract
    {
        /// <summary>
        /// Echoes a string value.
        /// </summary>
        /// <param name="value">The value to echo.</param>
        /// <returns>The echoed value.</returns>

        string Echo(string value);
    }

    /// <summary>
    /// Test class that implements WCF service contracts.
    /// </summary>

    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple,
        IncludeExceptionDetailInFaults = true)]
    public sealed class MockWcfService : ITestContract, ITestContract2, IDuplexContract, INonContract
    {
        private int _value;

        /// <summary>
        /// Gets the last created instance of TestService.
        /// </summary>

        public static MockWcfService Instance { get; private set; }

        /// <summary>
        /// Signals that the value represented by this service has changed.
        /// </summary>

        private event EventHandler ValueChanged;

        /// <summary>
        /// Creates a new test service.
        /// </summary>

        public MockWcfService()
        {
            Instance = this;
            string variable = Environment.GetEnvironmentVariable("TestValue");

            if (variable == null)
            {
                _value = new Random().Next();
            }
            else
            {
                _value = Int32.Parse(variable);
            }
        }

        /// <see cref="ITestContract.GetValue"/>

        public int GetValue()
        {
            return _value;
        }

        /// <see cref="ITestContract2.IncrementValue"/>

        public void IncrementValue()
        {
            _value++;
            OnValueChanged();
        }

        /// <see cref="ITestContract2.SetValue"/>

        public void SetValue(int value)
        {
            _value = value;
            OnValueChanged();
        }

        /// <see cref="INonContract.Echo"/>

        public string Echo(string value)
        {
            return value;
        }

        /// <see cref="IDuplexContract.NotifyValueChanged"/>

        public void NotifyValueChanged(int milliseconds)
        {
            using (ManualResetEventSlim changed = new ManualResetEventSlim())
            {
                ICallbackContract callback = OperationContext.Current.GetCallbackChannel<ICallbackContract>();
                EventHandler handler = (sender, e) =>
                {
                    callback.ValueChanged(_value);
                    changed.Set();
                };

                ValueChanged += handler;

                try
                {
                    callback.NotifyReady();
                    changed.Wait(milliseconds);
                }
                finally
                {
                    ValueChanged -= handler;
                }

                callback.NotifyDone();
            }
        }

        /// <summary>
        /// Raises the <see cref="ValueChanged"/> event.
        /// </summary>

        private void OnValueChanged()
        {
            var temp = ValueChanged;
            if (temp != null)
            {
                temp(this, EventArgs.Empty);
            }
        }
    }
}
