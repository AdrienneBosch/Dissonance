using System.Windows;
using System.Windows.Controls;

using Dissonance.SettingsManagers;

using Microsoft.Extensions.Logging;

namespace Dissonance.UserControls.Buttons
{
	public partial class ThemeToggleButton : UserControl
	{
		private readonly ISettingsManager _settingsManager;
		private readonly AppSettings _appSettings;
		private readonly ThemeManager _themeManager;
		private readonly ILogger<ThemeToggleButton> _logger;

		public ThemeToggleButton ( ISettingsManager settingsManager, AppSettings appSettings, ThemeManager themeManager, ILogger<ThemeToggleButton> logger )
		{
			_settingsManager = settingsManager;
			_appSettings = appSettings;
			_themeManager = themeManager;
			_logger = logger;

			InitializeComponent ( );
			InitializeSettings ( );
		}


		private void InitializeSettings ( )
		{
			try
			{
				if ( _appSettings != null )
				{
					ThemeToggleButton1.IsChecked = _appSettings.Theme.IsDarkMode;
					_logger.LogInformation ( "Theme toggle button initialized with current theme setting." );
				}
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "Error initializing theme settings." );
				MessageBox.Show ( "An error occurred while initializing theme settings. Please see the logs for more details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}

		private void ThemeToggleButton_Checked ( object sender, RoutedEventArgs e )
		{
			try
			{
				if ( _appSettings != null )
				{
					_appSettings.Theme.IsDarkMode = true;
					_themeManager.SetTheme ( true );
					_logger.LogInformation ( "Theme set to Dark mode." );
				}
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "Error setting theme to Dark mode." );
				MessageBox.Show ( "An error occurred while setting the theme to Dark mode. Please see the logs for more details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}

		private void ThemeToggleButton_Unchecked ( object sender, RoutedEventArgs e )
		{
			try
			{
				if ( _appSettings != null )
				{
					_appSettings.Theme.IsDarkMode = false;
					_themeManager.SetTheme ( false );
					_logger.LogInformation ( "Theme set to Light mode." );
				}
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "Error setting theme to Light mode." );
				MessageBox.Show ( "An error occurred while setting the theme to Light mode. Please see the logs for more details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}
	}
}
