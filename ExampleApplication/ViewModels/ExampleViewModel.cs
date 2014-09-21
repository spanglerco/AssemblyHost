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
using System.IO;
using System.Windows;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Input;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using SpanglerCo.AssemblyHostExample.Utility;

namespace SpanglerCo.AssemblyHostExample.ViewModels
{
    /// <summary>
    /// The view model for <see cref="IExample"/> objects.
    /// </summary>

    internal sealed class ExampleViewModel : INotifyPropertyChanged, IExampleLogger, IDataErrorInfo
    {
        private IExample _example;
        private string _parameter;
        private Task _runningExample;
        private DelegateCommand _runCommand;
        private DelegateCommand _stopCommand;
        private DelegateCommand _clearLogCommand;
        private DelegateCommand _openSourceCommand;
        private ObservableCollection<string> _log;
        private ReadOnlyObservableCollection<string> _readOnlyLog;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets a collection of log entries for the example.
        /// </summary>

        public ReadOnlyObservableCollection<string> Log
        {
            get
            {
                return _readOnlyLog;
            }
        }

        /// <summary>
        /// Gets the name of the example.
        /// </summary>

        public string Name
        {
            get
            {
                return _example.Name;
            }
        }

        /// <summary>
        /// Gets a description for the example.
        /// </summary>

        public string Description
        {
            get
            {
                return _example.Description;
            }
        }

        /// <summary>
        /// Gets a message to prompt for a parameter to pass to the example,
        /// or null if the example does not use a parameter.
        /// </summary>

        public string ParameterPrompt
        {
            get
            {
                return _example.ParameterPrompt;
            }
        }

        /// <summary>
        /// Gets whether or not the parameter prompt should be shown.
        /// </summary>

        public bool ShowParameterPrompt
        {
            get
            {
                return _example.ParameterPrompt != null;
            }
        }

        /// <summary>
        /// Gets or sets the parameter to pass to the example when run.
        /// </summary>

        public string Parameter
        {
            get
            {
                return _parameter;
            }

            set
            {
                if (_parameter != value)
                {
                    _parameter = value;
                    OnPropertyChanged("Parameter");
                }
            }
        }

        public string SourceFile
        {
            get
            {
                return _example.GetType().Name + ".cs";
            }
        }

        /// <summary>
        /// Gets a command used to run the example.
        /// </summary>

        public ICommand RunExample
        {
            get
            {
                return _runCommand;
            }
        }

        /// <summary>
        /// Gets a command used to stop the example while it is running.
        /// </summary>

        public ICommand StopExample
        {
            get
            {
                return _stopCommand;
            }
        }

        /// <summary>
        /// Gets a command used to clear the example log.
        /// </summary>

        public ICommand ClearLog
        {
            get
            {
                return _clearLogCommand;
            }
        }

        public ICommand OpenSourceFile
        {
            get
            {
                return _openSourceCommand;
            }
        }

        /// <summary>
        /// Creates a new example view model.
        /// </summary>
        /// <param name="example">The example to wrap.</param>

        public ExampleViewModel(IExample example)
        {
            if (example == null)
            {
                throw new ArgumentNullException("example");
            }

            _example = example;
            _log = new ObservableCollection<string>();
            _readOnlyLog = new ReadOnlyObservableCollection<string>(_log);
            _runCommand = new DelegateCommand(DoRunExample, CanRunExample);
            _stopCommand = new DelegateCommand(DoStopExample, CanStopExample);
            _clearLogCommand = new DelegateCommand(_log.Clear, CanClearLog);
            _openSourceCommand = new DelegateCommand(DoOpenSourceFile);
        }

        /// <see cref="Object.ToString"/>

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Runs the example asynchronously.
        /// </summary>

        private void DoRunExample()
        {
            ((IExampleLogger)this).Log("Running example");

            // First start a task that will run the example on another thread.
            _runningExample = Task.Factory.StartNew(() => _example.Run(this, _parameter), TaskCreationOptions.LongRunning);

            // Then add a continuation task that will clean up when the example is finished.
            _runningExample.ContinueWith(t =>
            {
                if (_runningExample.IsFaulted)
                {
                    ((IExampleLogger)this).Log(_runningExample.Exception.InnerException);
                }

                _runningExample.Dispose();
                _runningExample = null;
                ((IExampleLogger)this).Log("Example complete");

                // Inform the command manager that the run command's CanExecute status has changed.
                Application.Current.Dispatcher.BeginInvoke(new Action(CommandManager.InvalidateRequerySuggested));
            });
        }

        /// <summary>
        /// Returns whether or not the example can be run.
        /// </summary>
        /// <returns>True if the example can run, false if not.</returns>

        private bool CanRunExample()
        {
            return _runningExample == null && (_example.ParameterPrompt == null || !string.IsNullOrEmpty(_parameter));
        }

        /// <summary>
        /// Stops the example asynchronously.
        /// </summary>

        private void DoStopExample()
        {
            Task.Factory.StartNew(() => _example.Stop(this));
        }

        /// <summary>
        /// Returns whether or not the example can be stopped.
        /// </summary>
        /// <returns>True if the example can be stopped, false if not.</returns>

        private bool CanStopExample()
        {
            return _runningExample != null && _example.CanBeStopped;
        }

        /// <summary>
        /// Returns whether or not the log can be cleared.
        /// </summary>
        /// <returns>True if the log can be cleared, false if not.</returns>

        private bool CanClearLog()
        {
            return _log.Count > 0;
        }

        private void DoOpenSourceFile()
        {
            // See if we are running from the build output directory.

            string solutionFolderPath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)));
            string solutionFilePath = Path.Combine(solutionFolderPath, "AssemblyHostExample.sln");
            string sourceFilePath = Path.Combine(solutionFolderPath, "Examples", SourceFile);

            if (File.Exists(sourceFilePath))
            {
                Task.Factory.StartNew(() =>
                {
                    // First try to connect to a running Visual Studio instance.

                    using (VisualStudioCommunication communication = new VisualStudioCommunication(solutionFilePath))
                    {
                        communication.OpenFile(sourceFilePath);
                        communication.BringToFront();
                    }
                }).ContinueWith(t =>
                {
                    // On error fall back to opening the source file manually.

                    ProcessStartInfo info = new ProcessStartInfo(sourceFilePath);
                    info.UseShellExecute = true;
                    Process.Start(info).Dispose();
                }, TaskContinuationOptions.NotOnRanToCompletion);
            }
            else
            {
                MessageBox.Show("Ensure you are running the example application from the build output directory. Could not find " + sourceFilePath);
            }
        }

        /// <see cref="IExampleLogger.Log(string)"/>

        void IExampleLogger.Log(string message)
        {
            string formattedMessage = string.Format("{0}:\t{1}", DateTime.Now.ToLongTimeString(), message);
            Application.Current.Dispatcher.BeginInvoke(new Action(() => _log.Add(formattedMessage)));
        }

        /// <see cref="IExampleLogger.Log(Exception)"/>

        void IExampleLogger.Log(Exception ex)
        {
            ((IExampleLogger)this).Log("Exception - " + ex.Message);
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="property">The name of the property that changed.</param>

        private void OnPropertyChanged(string property)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }

        /// <see cref="IDataErrorInfo.Error"/>

        string IDataErrorInfo.Error
        {
            get
            {
                return string.Empty;
            }
        }

        /// <see cref="IDataErrorInfo.this"/>

        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                if (_example.ParameterPrompt != null && columnName == "Parameter" && string.IsNullOrEmpty(_parameter))
                {
                    return "This example requires an input parameter.";
                }

                return string.Empty;
            }
        }
    }
}
