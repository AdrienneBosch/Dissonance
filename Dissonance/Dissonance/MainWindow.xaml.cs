using System.Windows;

using Dissonance.ViewModels;

namespace Dissonance
{
	public partial class MainWindow : Window
	{
		private readonly MainWindowViewModel _viewModel;

		public MainWindow ( MainWindowViewModel viewModel )
		{
			InitializeComponent ( );
			_viewModel = viewModel;
			DataContext = _viewModel;
		}
	}
}
