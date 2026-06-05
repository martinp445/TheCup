using System.Windows.Input;

namespace TheCup_Application.Commands
{
    public class ActionCommand : ICommand
    {
        private readonly Action? _execute;
        private readonly Action<object?>? _executeWithParam;
        private readonly Func<bool>? _canExecute;
        private readonly Func<object?, bool>? _canExecuteWithParam;

        public ActionCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public ActionCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _executeWithParam = execute;
            _canExecuteWithParam = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecuteWithParam != null)
                return _canExecuteWithParam(parameter);

            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            if (_executeWithParam != null)
                _executeWithParam(parameter);
            else
                _execute?.Invoke();
        }

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        public event EventHandler? CanExecuteChanged;
    }
}
