using System.Windows.Input;

namespace TheCup_Application.Commands
{
    public class ActionCommand : ICommand
    {

        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public ActionCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        public event EventHandler? CanExecuteChanged;
    }
}
