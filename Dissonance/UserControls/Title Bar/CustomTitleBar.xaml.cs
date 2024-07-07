using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dissonance.UserControls.Title_Bar
{
	/// <summary>
	/// Interaction logic for CustomTitleBar.xaml
	/// </summary>
	public partial class CustomTitleBar : UserControl
	{
		public CustomTitleBar ( )
		{
			InitializeComponent ( );
		}

		private void MinimizeButton_Click ( object sender, RoutedEventArgs e )
		{
			Window.GetWindow ( this ).WindowState = WindowState.Minimized;
		}

		private void CloseButton_Click ( object sender, RoutedEventArgs e )
		{
			Window.GetWindow ( this ).Close ( );
		}

		private void MaximizeButton_Click ( object sender, RoutedEventArgs e )
		{
			Window.GetWindow ( this ).WindowState = WindowState.Maximized;
		}
		private void TitleBar_MouseDown ( object sender, MouseButtonEventArgs e )
		{
			if ( e.ChangedButton == MouseButton.Left )
			{
				Window.GetWindow ( this )?.DragMove ( );
			}
		}
	}
}
