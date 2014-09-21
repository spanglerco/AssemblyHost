using System;
using System.Windows;

using SpanglerCo.AssemblyHostExample.Views;
using SpanglerCo.AssemblyHostExample.ViewModels;

namespace AssemblyHostExample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <see cref="Application.OnStartup"/>

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MainViewModel viewModel = new MainViewModel();
            this.MainWindow = new MainWindow();
            this.MainWindow.DataContext = viewModel;
            
            this.MainWindow.Show();
        }
    }
}
