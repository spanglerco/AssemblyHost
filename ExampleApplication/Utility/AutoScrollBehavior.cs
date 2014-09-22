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
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace SpanglerCo.AssemblyHostExample.Utility
{
    /// <summary>
    /// Contains useful attached properties.
    /// </summary>

    internal static partial class Behaviors
    {
        /// <summary>
        /// Gets the value of the AutoScroll attached property.
        /// </summary>
        /// <param name="obj">The object whose AutoScroll property is being retrieved.</param>
        /// <returns>True if the object should scroll to the end whenever an element is added, false if not.</returns>

        public static bool GetAutoScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollProperty);
        }

        /// <summary>
        /// Sets the value of the AutoScroll attached property.
        /// </summary>
        /// <param name="obj">The object whose AutoScroll property is being set.</param>
        /// <param name="value">True if the object should scroll to the end whenever an element is added, false if not.</param>

        public static void SetAutoScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollProperty, value);
        }

        /// <summary>
        /// An attached property for making a ListBox scroll to the end whenever an element is added.
        /// </summary>

        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.RegisterAttached("AutoScroll", typeof(bool), typeof(Behaviors), new UIPropertyMetadata(false, AutoScrollManager.OnAutoScrollChanged));

        /// <summary>
        /// Class used to manage the AutoScroll attached property.
        /// </summary>

        private class AutoScrollManager : IDisposable
        {
            private static Dictionary<ListBox, AutoScrollManager> _instances = new Dictionary<ListBox, AutoScrollManager>();
            private static readonly DependencyPropertyDescriptor _itemsSourceProperty = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ItemsControl));

            private ListBox _control;
            private INotifyCollectionChanged _itemsSource;

            /// <summary>
            /// Called when the AutoScroll attached property is changed for an object.
            /// </summary>

            public static void OnAutoScrollChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
            {
                ListBox control = obj as ListBox;

                if (control == null)
                {
                    throw new InvalidOperationException("AutoScroll can only be set on ListBoxes.");
                }

                AutoScrollManager manager;
                if (_instances.TryGetValue(control, out manager))
                {
                    manager.Dispose();
                }

                if ((bool)e.NewValue)
                {
                    _instances[control] = new AutoScrollManager(control);
                }
            }

            /// <summary>
            /// Creates a new manager for AutoScroll.
            /// </summary>
            /// <param name="control">The ListBox whose AutoScroll is being enabled.</param>
            /// <remarks>
            /// This implementation is only suitable for long-lived objects (e.g. a ListBox in the main view)
            /// and not for objects that are created an arbitrary number of times. This is because AddValueChanged
            /// will add a strong reference essentially pinning it in memory and preventing garbage collection.
            /// Could rewrite this using a Binding and WeakReferences...
            /// </remarks>

            public AutoScrollManager(ListBox control)
            {
                _control = control;
                _itemsSourceProperty.AddValueChanged(control, ItemsSourceChanged);

                if (control.ItemsSource != null)
                {
                    ItemsSourceChanged(control, EventArgs.Empty);
                }
            }

            /// <summary>
            /// Called when an AutoScroll ListBox's ItemsSource property changes.
            /// </summary>
            /// <remarks>
            /// Used to register for the CollectionChanged event on the ItemsSource collection.
            /// </remarks>

            private void ItemsSourceChanged(object source, EventArgs e)
            {
                if (_itemsSource != null)
                {
                    _itemsSource.CollectionChanged -= CollectionChanged;
                }

                _itemsSource = _control.ItemsSource as INotifyCollectionChanged;

                if (_itemsSource != null)
                {
                    _itemsSource.CollectionChanged += new NotifyCollectionChangedEventHandler(CollectionChanged);
                }
            }

            /// <summary>
            /// Called when an AutoScroll ListBox's ItemsSource has items added or removed.
            /// </summary>
            /// <remarks>
            /// Used to actually perform the scroll.
            /// </remarks>

            private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (_control.Items != null && _control.Items.Count > 0)
                {
                    _control.ScrollIntoView(_control.Items[_control.Items.Count - 1]);
                }
            }

            /// <see cref="IDisposable.Dispose"/>

            public void Dispose()
            {
                if (_itemsSource != null)
                {
                    _itemsSource.CollectionChanged -= CollectionChanged;
                    _itemsSource = null;
                }

                if (_control != null)
                {
                    _itemsSourceProperty.RemoveValueChanged(_control, ItemsSourceChanged);
                    _control = null;
                }
            }
        }
    }
}
