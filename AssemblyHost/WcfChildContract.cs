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
using System.ServiceModel;

namespace SpanglerCo.AssemblyHost
{
    /// <summary>
    /// Serves as a wrapper around a WCF channel for a service contract interface.
    /// </summary>
    /// <typeparam name="TContract">An interface with the ServiceContract attribute being wrapped.</typeparam>

    public class WcfChildContract<TContract> : IDisposable
        where TContract : class
    {
        private TContract _contract;
        private ICommunicationObject _object;

        /// <summary>
        /// Gets the contract associated with this instance.
        /// </summary>

        public TContract Contract
        {
            get
            {
                if (_contract == null)
                {
                    throw new ObjectDisposedException("WcfChildContract");
                }

                return _contract;
            }
        }

        /// <summary>
        /// Creates a new WCF contract wrapper.
        /// </summary>
        /// <param name="contract">The contract to wrap.</param>
        /// <exception cref="ArgumentNullException">if contract is null.</exception>
        /// <exception cref="ArgumentException">if contract is not a valid, unopened WCF channel.</exception>
        /// <exception cref="InvalidOperationException">if TContract is not a WCF ServiceContract.</exception>
        /// <exception cref="CommunicationException">if the contract is unable to establish a WCF connection.</exception>
        /// <remarks>
        /// This class does not validate that the endpoint actually implements TContract.
        /// Callers should catch CommunicationException when making method calls on the contract.
        /// </remarks>

        public WcfChildContract(TContract contract)
        {
            if (contract == null)
            {
                throw new ArgumentNullException("contract");
            }

            _object = contract as ICommunicationObject;

            if (_object == null)
            {
                throw new ArgumentException("Must be a valid WCF channel", "contract");
            }

            if (_object.State != CommunicationState.Created)
            {
                throw new ArgumentException("WCF channel has already been opened", "contract");
            }

            _object.Open();
            _contract = contract;
        }

        /// <see cref="IDisposable.Dispose"/>

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the resources being used by this instance.
        /// </summary>
        /// <param name="disposing">True if Dispose was called.</param>

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_object != null)
                {
                    try
                    {
                        _object.Close();
                    }
                    catch (CommunicationException)
                    {
                        _object.Abort();
                    }
                }

                _object = null;
                _contract = null;
            }
        }
    }
}
