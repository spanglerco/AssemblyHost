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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace SpanglerCo.AssemblyHostExample.Utility
{
    /// <summary>
    /// A class that can find and construct all objects the implements an interface or base class.
    /// </summary>
    /// <typeparam name="TInterface">The type of interface or base class to search for.</typeparam>
    /// <remarks>
    /// If TInterface is a covariant generic type, this class may return more types than expected.
    /// For example, an object implementing IEnumerable&lt;string%gt; would be returned when TInterface
    /// is IEnumerable&lt;object&gt; because <see cref="IEnumerable&lt;T&gt;"/> is covariant.
    /// </remarks>

    internal static class InterfaceFactory<TInterface>
    {
        /// <summary>
        /// Returns a sequence of non-abstract types that implements TInterface.
        /// </summary>
        /// <param name="assembly">
        /// The assembly for which to search for types.
        /// If null, searches the assembly containing the caller.
        /// </param>
        /// <returns>An object that can be enumerated for all types.</returns>

        public static IEnumerable<Type> FindImplementingTypes(Assembly assembly = null)
        {
            if (assembly == null)
            {
                assembly = Assembly.GetCallingAssembly();
            }

            return assembly.GetTypes().Where(t => !t.IsAbstract && !t.IsInterface && typeof(TInterface).IsAssignableFrom(t));
        }

        /// <summary>
        /// Returns a sequence containing all types that implement TInterface
        /// that were able to be constructed using a default constructor.
        /// </summary>
        /// <param name="assembly">
        /// The assembly for which to search for types.
        /// If null, searches the assembly containing the caller.
        /// </param>
        /// <returns>An object that can be enumerated for all objects.</returns>

        public static IEnumerable<TInterface> CreateImplementingTypes(Assembly assembly = null)
        {
            foreach (Type t in FindImplementingTypes(assembly))
            {
                TInterface obj;

                try
                {
                    obj = (TInterface)Activator.CreateInstance(t, true);
                }
                catch (MemberAccessException)
                {
                    // No default constructor we can use, skip this one.
                    continue;
                }

                yield return obj;
            }
        }
    }
}
