using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfApp4
{
    public class Command : ICommand
    {
        Action<object> _execute;
        Func<object, bool> _executeFunc;

        public Command(Action<object> execute, Func<object, bool> executeFunc)
        {
            _execute = execute;
            _executeFunc = executeFunc;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }
}
