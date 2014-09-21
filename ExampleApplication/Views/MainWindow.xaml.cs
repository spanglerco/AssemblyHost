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
