using System;
using System.Windows.Input;

using Dissonance.Infrastructure.Commands;
using Dissonance.Windows.Controls;

namespace Dissonance.ViewModels
{
	internal class DissonanceMessageBoxViewModel
	{
		public string Title { get; set; }
		public string Message { get; set; }
		public bool ShowCancelButton { get; set; }

		public ICommand OkCommand { get; }
		public ICommand CancelCommand { get; }

		private readonly DissonanceMessageBox _messageBox;

		public DissonanceMessageBoxViewModel ( DissonanceMessageBox messageBox )
		{
			_messageBox = messageBox;
			OkCommand = new RelayCommandNoParam ( Ok );
			CancelCommand = new RelayCommandNoParam ( Cancel );
		}

		private void Ok ( )
		{
			_messageBox.DialogResult = true;
			_messageBox.Close ( );
		}

		private void Cancel ( )
		{
			_messageBox.DialogResult = false;
			_messageBox.Close ( );
		}

		public static bool? Show ( string title, string message, bool showCancelButton = false )
		{
			var messageBox = new DissonanceMessageBox();
			var viewModel = new DissonanceMessageBoxViewModel(messageBox)
			{
				Title = title,
				Message = message,
				ShowCancelButton = showCancelButton
			};

			messageBox.DataContext = viewModel;
			return messageBox.ShowDialog ( );
		}
	}
}
