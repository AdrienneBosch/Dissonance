using System;
using System.Windows.Input;

namespace Dissonance.Infrastructure.Commands
{
	public class RelayCommand : ICommand
	{
		private readonly Action<object> _execute;
		private readonly Predicate<object> _canExecute;

		public RelayCommand ( Action<object> execute, Predicate<object> canExecute = null )
		{
			_execute = execute ?? throw new ArgumentNullException ( nameof ( execute ) );
			_canExecute = canExecute;
		}

		public bool CanExecute ( object parameter ) => _canExecute == null || _canExecute ( parameter );

		public void Execute ( object parameter ) => _execute ( parameter );

		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}
	}

	public class RelayCommandNoParam : ICommand
	{
		private readonly Action _execute;
		private readonly Func<bool> _canExecute;

		public RelayCommandNoParam ( Action execute, Func<bool> canExecute = null )
		{
			_execute = execute ?? throw new ArgumentNullException ( nameof ( execute ) );
			_canExecute = canExecute;
		}

		public bool CanExecute ( object parameter ) => _canExecute == null || _canExecute ( );

		public void Execute ( object parameter ) => _execute ( );

		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}
	}
}
