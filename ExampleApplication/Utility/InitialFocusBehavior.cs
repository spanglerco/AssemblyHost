using System;
using System.Windows;
using System.Windows.Threading;

namespace SpanglerCo.AssemblyHostExample.Utility
{
    /// <summary>
    /// Contains useful attached properties.
    /// </summary>

    internal static class Behaviors
    {
        /// <summary>
        /// Gets the value of the InitialFocus attached property.
        /// </summary>
        /// <param name="obj">The object whose InitialFocus property is being retrieved.</param>
        /// <returns>True if the object should have initial focus on start, false if not.</returns>

        public static bool GetInitialFocus(DependencyObject obj)
        {
            return (bool)obj.GetValue(InitialFocusProperty);
        }

        /// <summary>
        /// Sets the value of the InitialFocus attached property.
        /// </summary>
        /// <param name="obj">The object whose InitialFocus property is being set.</param>
        /// <param name="value">True if the object should have initial focus on start, false if not.</param>

        public static void SetInitialFocus(DependencyObject obj, bool value)
        {
            obj.SetValue(InitialFocusProperty, value);
        }

        /// <summary>
        /// An attached property for making a UIElement initially have focus on application start.
        /// </summary>

        public static readonly DependencyProperty InitialFocusProperty =
            DependencyProperty.RegisterAttached("InitialFocus", typeof(bool), typeof(Behaviors), new UIPropertyMetadata(false, OnInitialFocusChanged));

        /// <summary>
        /// Called when the InitialFocus attached property is changed for an object.
        /// </summary>
        /// <param name="obj">The object whose InitialFocus property changed.</param>
        /// <param name="e">The event args.</param>

        private static void OnInitialFocusChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            UIElement element = obj as UIElement;

            if (element == null)
            {
                throw new InvalidOperationException("InitialFocus can only be set on UIElements.");
            }

            if ((bool)e.NewValue)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => element.Focus()), DispatcherPriority.Input);
            }
        }
    }
}
