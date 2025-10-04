using System;
using System.Windows.Input;

namespace Dissonance.Infrastructure.Commands
{
	public class RelayCommand : ICommand
	{
                private readonly Predicate<object?>? _canExecute;
                private readonly Action<object?> _execute;

                public RelayCommand ( Action<object?> execute, Predicate<object?>? canExecute = null )
                {
                        _execute = execute ?? throw new ArgumentNullException ( nameof ( execute ) );
                        _canExecute = canExecute;
                }

                public event EventHandler? CanExecuteChanged;

                public bool CanExecute ( object? parameter ) => _canExecute == null || _canExecute ( parameter );

                public void Execute ( object? parameter ) => _execute ( parameter );

                public void RaiseCanExecuteChanged ( )
                {
                        CanExecuteChanged?.Invoke ( this, EventArgs.Empty );
                }
        }

        public class RelayCommandNoParam : ICommand
        {
                private readonly Func<bool> _canExecute;
                private readonly Action _execute;

                public RelayCommandNoParam ( Action execute, Func<bool>? canExecute = null )
                {
                        _execute = execute ?? throw new ArgumentNullException ( nameof ( execute ) );
                        _canExecute = canExecute;
                }

                public event EventHandler? CanExecuteChanged;

                public bool CanExecute ( object parameter ) => _canExecute == null || _canExecute ( );

                public void Execute ( object parameter ) => _execute ( );

                public void RaiseCanExecuteChanged ( )
                {
                        CanExecuteChanged?.Invoke ( this, EventArgs.Empty );
                }
        }
}
