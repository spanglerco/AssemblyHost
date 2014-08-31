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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SpanglerCo.UnitTests.AssemblyHost
{
    /// <summary>
    /// Collection of helper functions for unit tests.
    /// </summary>

    internal static class TestUtilities
    {
        /// <summary>
        /// Verifies a call throws an exception of any type.
        /// </summary>
        /// <param name="action">A delegate that should throw an exception.</param>

        public static void AssertThrows(Action action)
        {
            AssertThrows(action, null, null, null);
        }

        /// <summary>
        /// Verifies a call throws an exception of any type.
        /// </summary>
        /// <param name="action">A delegate that should throw an exception.</param>
        /// <param name="message">The message to display if the assertion fails.</param>

        public static void AssertThrows(Action action, string message)
        {
            AssertThrows(action, null, message, null);
        }

        /// <summary>
        /// Verifies a call throws an exception of any type.
        /// </summary>
        /// <param name="action">A delegate that should throw an exception.</param>
        /// <param name="message">The message to display if the assertion fails.</param>
        /// <param name="parameters">The parameters used to format the message.</param>

        public static void AssertThrows(Action action, string message, params object[] parameters)
        {
            AssertThrows(action, null, message, parameters);
        }

        /// <summary>
        /// Verifies a call throws an exception of a certain type.
        /// </summary>
        /// <param name="action">A delegate that should throw an exception.</param>
        /// <param name="expectedException">The type of exception to expect.</param>

        public static void AssertThrows(Action action, Type expectedException)
        {
            AssertThrows(action, expectedException, null, null);
        }

        /// <summary>
        /// Verifies a call throws an exception of a certain type.
        /// </summary>
        /// <param name="action">A delegate that should throw an exception.</param>
        /// <param name="expectedException">The type of exception to expect.</param>
        /// <param name="message">The message to display if the assertion fails.</param>

        public static void AssertThrows(Action action, Type expectedException, string message)
        {
            AssertThrows(action, expectedException, message, null);
        }

        /// <summary>
        /// Verifies a call throws an exception of a certain type.
        /// </summary>
        /// <param name="action">A delegate that should throw an exception.</param>
        /// <param name="expectedException">The type of exception to expect.</param>
        /// <param name="message">The message to display if the assertion fails.</param>
        /// <param name="parameters">The parameters used to format the message.</param>

        public static void AssertThrows(Action action, Type expectedException, string message, params object[] parameters)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (expectedException == null || expectedException.IsAssignableFrom(ex.GetType()))
                {
                    return;
                }

                throw;
            }

            Assert.Fail(message, parameters);
        }
    }
}
