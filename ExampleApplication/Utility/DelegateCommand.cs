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
using System.Windows.Input;

namespace SpanglerCo.AssemblyHostExample.Utility
{
    /// <summary>
    /// An ICommand whose implementation comes from delegates.
    /// </summary>
    /// <typeparam name="TParameter">The type of parameter to accept when executing the command.</typeparam>

    public class DelegateCommand<TParameter> : ICommand
    {
        private Action<TParameter> _execute;
        private Predicate<TParameter> _canExecute;

        /// <see cref="ICommand.CanExecuteChanged"/>

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        /// <summary>
        /// Creates a new delegate command that always executes.
        /// </summary>
        /// <param name="execute">The delegate called to execute the command.</param>

        public DelegateCommand(Action<TParameter> execute)
            : this(execute, null)
        { }

        /// <summary>
        /// Creates a new delegate command.
        /// </summary>
        /// <param name="execute">The delegate called to execute the command.</param>
        /// <param name="canExecute">The delegate called to determine whether or not the command can execute.</param>

        public DelegateCommand(Action<TParameter> execute, Predicate<TParameter> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            _execute = execute;
            _canExecute = canExecute;
        }

        /// <see cref="ICommand.CanExecute"/>

        public bool CanExecute(object parameter)
        {
            if (_canExecute != null)
            {
                return _canExecute((TParameter)parameter);
            }

            return true;
        }

        /// <see cref="ICommand.Execute"/>

        public void Execute(object parameter)
        {
            _execute((TParameter)parameter);
        }
    }

    /// <summary>
    /// A version of <see cref="DelegateCommand&lt;TParameter&gt;"/> that don't provide a command parameter.
    /// </summary>

    public class DelegateCommand : DelegateCommand<object>
    {
        /// <summary>
        /// Creates a new delegate command that always executes.
        /// </summary>
        /// <param name="execute">The delegate called to execute the command.</param>

        public DelegateCommand(Action execute)
            : base((_) => { execute(); })
        { }

        /// <summary>
        /// Creates a new delegate command.
        /// </summary>
        /// <param name="execute">The delegate called to execute the command.</param>
        /// <param name="canExecute">The delegate called to determine whether or not the command can execute.</param>

        public DelegateCommand(Action execute, Func<bool> canExecute)
            : base((_) => { execute(); }, (_) => { return canExecute(); })
        { }
    }
}
