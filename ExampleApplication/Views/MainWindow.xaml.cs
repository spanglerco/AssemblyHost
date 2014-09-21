using System;
using System.Windows;
using System.Windows.Input;

namespace SpanglerCo.AssemblyHostExample.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    internal sealed partial class MainWindow : Window
    {
        /// <summary>
        /// Creates a new main window.
        /// </summary>
        
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Close routed event.
        /// </summary>

        private void OnCloseCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }
    }
}
