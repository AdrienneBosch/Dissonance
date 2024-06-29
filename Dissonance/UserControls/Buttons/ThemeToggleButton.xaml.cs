using Dissonance.SettingsManagers;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Dissonance.UserControls.Buttons
{
	/// <summary>
	/// Interaction logic for ThemeToggleButton.xaml
	/// </summary>
	public partial class ThemeToggleButton : UserControl
	{
		private ISettingsManager _settingsManager;
		private AppSettings _appSettings;

		public ThemeToggleButton ( )
		{
			InitializeComponent ( );
		}

		public ISettingsManager SettingsManager
		{
			get { return _settingsManager; }
			set { _settingsManager = value; InitializeSettings ( ); }
		}

		public AppSettings AppSettings
		{
			get { return _appSettings; }
			set { _appSettings = value; InitializeSettings ( ); }
		}

		private void InitializeSettings ( )
		{
			if ( _appSettings != null )
			{
				ThemeToggleButton1.IsChecked = _appSettings.Theme.IsDarkMode;
			}
		}

		private void ThemeToggleButton_Checked ( object sender, RoutedEventArgs e )
		{
			_appSettings.Theme.IsDarkMode = true;
			ThemeManager.SetTheme ( true );
			SaveSettings ( );
		}

		private void ThemeToggleButton_Unchecked ( object sender, RoutedEventArgs e )
		{
			_appSettings.Theme.IsDarkMode = false;
			ThemeManager.SetTheme ( false );
			SaveSettings ( );
		}

		private void SaveSettings ( )
		{
			_settingsManager.SaveSettings ( _appSettings );
		}
	}
}
