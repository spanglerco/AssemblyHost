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
