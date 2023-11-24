using System;
using System.Windows.Input;

namespace n42_Robot_PROTO_III
{ 
    public class RelayCommand : ICommand
    {
        private Action _action;

        public RelayCommand(Action action)
        {
            _action = action;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _action();
        }

#pragma warning disable CS0067 // The event 'RelayCommand.CanExecuteChanged' is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067 // The event 'RelayCommand.CanExecuteChanged' is never used
    }
}