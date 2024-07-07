using System;
using System.Windows;

using Dissonance.SettingsManagers;

using Microsoft.Extensions.Logging;

namespace Dissonance
{
	public class ThemeManager
	{
		private readonly AppSettings _appSettings;
		private readonly ILogger<ThemeManager> _logger;

		public ThemeManager ( AppSettings appSettings, ILogger<ThemeManager> logger )
		{
			_appSettings = appSettings;
			_logger = logger;
			_appSettings.PropertyChanged += AppSettings_PropertyChanged;
		}

		private void AppSettings_PropertyChanged ( object sender, System.ComponentModel.PropertyChangedEventArgs e )
		{
			if ( e.PropertyName == nameof ( AppSettings.Theme ) )
			{
				SetTheme ( _appSettings.Theme.IsDarkMode );
			}
		}

		public void SetTheme ( bool isDarkMode )
		{
			try
			{
				ResourceDictionary themeDictionary = new ResourceDictionary();
				if ( isDarkMode )
				{
					themeDictionary.Source = new Uri ( "pack://application:,,,/Dissonance;component/Assets/Themes/DarkTheme.xaml" );
				}
				else
				{
					themeDictionary.Source = new Uri ( "pack://application:,,,/Dissonance;component/Assets/Themes/LightTheme.xaml" );
				}

				// Clear existing merged dictionaries
				Application.Current.Resources.MergedDictionaries.Clear ( );

				// Apply the selected theme
				Application.Current.Resources.MergedDictionaries.Add ( themeDictionary );

				// Update AppSettings
				if ( _appSettings != null )
				{
					_appSettings.Theme.IsDarkMode = isDarkMode;
				}

				_logger.LogInformation ( "Theme changed to {Theme}.", isDarkMode ? "Dark" : "Light" );
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "Error occurred while setting the theme." );
				MessageBox.Show ( "An error occurred while changing the theme. Please see the logs for more details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}
	}
}
