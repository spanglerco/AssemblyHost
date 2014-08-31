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

namespace SpanglerCo.UnitTests.AssemblyHost.Mock
{
    /// <summary>
    /// A class that can be used for testing the method host.
    /// </summary>

    public class MockMethodClass
    {
        /// <summary>
        /// A static method that has no return value.
        /// </summary>

        public static void StaticNoReturn()
        { }

        /// <summary>
        /// A static method that returns an object that does not override ToString.
        /// </summary>
        /// <returns>A new object.</returns>

        public static object StaticNoString()
        {
            return new object();
        }

        /// <summary>
        /// A static method that returns an object that overrides ToString.
        /// </summary>
        /// <returns>An integer.</returns>

        public static int StaticString()
        {
            return 7;
        }

        /// <summary>
        /// A static method that returns a null string.
        /// </summary>
        /// <returns>null</returns>

        public static string StaticNull()
        {
            return null;
        }

        /// <summary>
        /// A static method that throws an exception.
        /// </summary>
        /// <exception cref="InvalidOperationException">always</exception>

        public static void Throw()
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// An instance method that has no return value.
        /// </summary>

        public void NoReturn()
        { }

        /// <summary>
        /// An instance method that returns an object that does not override ToString.
        /// </summary>
        /// <returns>A new object.</returns>

        public object NoString()
        {
            return new object();
        }

        /// <summary>
        /// An instance method that returns an object that overrides ToString.
        /// </summary>
        /// <returns>An integer.</returns>

        public int String()
        {
            return 7;
        }

        /// <summary>
        /// An instance method that returns a null string.
        /// </summary>
        /// <returns>null</returns>

        public string Null()
        {
            return null;
        }

        /// <summary>
        /// An instance method that returns the value of the Test environment variable.
        /// </summary>
        /// <returns>The value of the Test variable.</returns>

        public string EnvironmentVariable()
        {
            return Environment.GetEnvironmentVariable("Test");
        }
    }

    /// <summary>
    /// A class that cannot be used with the method host.
    /// </summary>

    public class MockInvalidMethodClass
    {
        /// <summary>
        /// Non-default constructor that prevents this class from being used with the method host.
        /// </summary>
        /// <param name="arg">An argument.</param>

        public MockInvalidMethodClass(bool arg)
        { }

        /// <summary>
        /// An instance method that cannot be used because of the constructor.
        /// </summary>

        public void Method()
        { }
    }
}
