using System.Windows;
using System.Windows.Controls;

using Dissonance.SettingsManagers;

namespace Dissonance.UserControls.Buttons
{
	public partial class ThemeToggleButton : UserControl
	{
		private readonly ISettingsManager _settingsManager;
		private readonly AppSettings _appSettings;
		private readonly ThemeManager _themeManager;

		public ThemeToggleButton ( ISettingsManager settingsManager, AppSettings appSettings, ThemeManager themeManager )
		{
			InitializeComponent ( );
			_settingsManager = settingsManager;
			_appSettings = appSettings;
			_themeManager = themeManager;

			InitializeSettings ( );
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
				_appSettings.Theme.IsDarkMode = true;
				_themeManager.SetTheme ( true );
			}
		}

		private void ThemeToggleButton_Unchecked ( object sender, RoutedEventArgs e )
		{
			if ( _appSettings != null )
			{
				_appSettings.Theme.IsDarkMode = false;
				_themeManager.SetTheme ( false );
			}
		}
	}
}
