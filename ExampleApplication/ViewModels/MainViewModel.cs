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
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using SpanglerCo.AssemblyHostExample.Utility;

namespace SpanglerCo.AssemblyHostExample.ViewModels
{
    /// <summary>
    /// The main view model for the examples application.
    /// </summary>

    internal sealed class MainViewModel : INotifyPropertyChanged
    {
        private ExampleViewModel _selectedExample;
        private List<ExampleViewModel> _examples;
        private ReadOnlyCollection<ExampleViewModel> _readOnlyExamples;

        /// <see cref="INotifyPropertyChanged.PropertyChanged"/>

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets a collection of the available examples.
        /// </summary>

        public ReadOnlyCollection<ExampleViewModel> Examples
        {
            get
            {
                return _readOnlyExamples;
            }
        }

        /// <summary>
        /// Gets or sets the currently selected example.
        /// </summary>

        public ExampleViewModel SelectedExample
        {
            get
            {
                return _selectedExample;
            }

            set
            {
                if (_selectedExample != value)
                {
                    _selectedExample = value;
                    OnPropertyChanged("SelectedExample");
                }
            }
        }

        /// <summary>
        /// Creates a new main view model.
        /// </summary>

        public MainViewModel()
        {
            _examples = new List<ExampleViewModel>();

            // Find every example in this assembly, wrap it in a view model, and add it to the list.

            foreach (IExample example in InterfaceFactory<IExample>.CreateImplementingTypes())
            {
                _examples.Add(new ExampleViewModel(example));
            }

            _selectedExample = _examples.FirstOrDefault();
            _readOnlyExamples = new ReadOnlyCollection<ExampleViewModel>(_examples);
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The name of the property whose value changed.</param>

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
