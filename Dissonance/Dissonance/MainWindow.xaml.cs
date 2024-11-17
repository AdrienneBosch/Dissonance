using System.Windows;

using Dissonance.ViewModels;

namespace Dissonance
{
	public partial class MainWindow : Window
	{
		private readonly MainWindowViewModel _viewModel;

		public MainWindow ( MainWindowViewModel viewModel )
		{
			_viewModel = viewModel ?? throw new ArgumentNullException ( nameof ( viewModel ) );
			InitializeComponent ( );
			DataContext = _viewModel;
		}
	}
}
