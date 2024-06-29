using Dissonance.SettingsManagers;

using System.Windows;
using System.Windows.Controls;

namespace Dissonance.UserControls.Buttons
{
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
			if ( _appSettings != null )
			{
				_appSettings.Theme.IsDarkMode = true; // Modify the in-memory settings object
				ThemeManager.SetTheme ( true ); // Apply theme changes
			}
		}

		private void ThemeToggleButton_Unchecked ( object sender, RoutedEventArgs e )
		{
			if ( _appSettings != null )
			{
				_appSettings.Theme.IsDarkMode = false; // Modify the in-memory settings object
				ThemeManager.SetTheme ( false ); // Apply theme changes
			}
		}

		private void SaveSettings ( )
		{
			_settingsManager.SaveSettings ( _appSettings ); // Use the settings manager to save changes
		}
	}
}
