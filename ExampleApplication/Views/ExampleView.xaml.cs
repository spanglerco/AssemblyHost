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
using System.Windows.Controls;

namespace SpanglerCo.AssemblyHostExample.Views
{
    /// <summary>
    /// Interaction logic for ExampleView.xaml
    /// </summary>
    internal sealed partial class ExampleView : UserControl
    {
        /// <summary>
        /// Creates a new example view.
        /// </summary>

        public ExampleView()
        {
            InitializeComponent();
            DataContextChanged += (sender, args) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (Log.Items.Count > 0)
                    {
                        Log.ScrollIntoView(Log.Items[Log.Items.Count - 1]);
                    }

                    Parameter.Focus();
                    Parameter.SelectAll();
                }));
            };
        }
    }
}
