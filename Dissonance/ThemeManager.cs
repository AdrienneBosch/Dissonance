using System.Windows;

using Microsoft.Extensions.Logging;

namespace Dissonance
{
	public class ThemeManager
	{
		private readonly AppSettings _appSettings;
		private readonly ILogger<ThemeManager> _logger;

		public ThemeManager ( AppSettings appSettings, ILogger<ThemeManager> logger )
		{
			_appSettings = appSettings ?? throw new ArgumentNullException ( nameof ( appSettings ) );
			_logger = logger ?? throw new ArgumentNullException ( nameof ( logger ) );
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
				_logger.LogInformation ( "Attempting to set theme to {Theme}.", isDarkMode ? "Dark" : "Light" );

				var themeDictionary = new ResourceDictionary
				{
					Source = new Uri($"pack://application:,,,/Dissonance;component/Assets/Themes/{(isDarkMode ? "DarkTheme.xaml" : "LightTheme.xaml")}")
				};

				Application.Current.Resources.MergedDictionaries.Clear ( );
				Application.Current.Resources.MergedDictionaries.Add ( themeDictionary );

				_logger.LogInformation ( "Theme changed to {Theme}.", isDarkMode ? "Dark" : "Light" );
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "Error occurred while setting the theme." );
				MessageBox.Show ( "An error occurred while changing the theme. Please see the logs for more details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
				RevertTheme ( );
			}
		}

		private void RevertTheme ( )
		{
			try
			{
				var revertToDarkMode = !_appSettings.Theme.IsDarkMode;
				var revertThemeDictionary = new ResourceDictionary
				{
					Source = new Uri($"pack://application:,,,/Dissonance;component/Assets/Themes/{(revertToDarkMode ? "DarkTheme.xaml" : "LightTheme.xaml")}")
				};

				Application.Current.Resources.MergedDictionaries.Clear ( );
				Application.Current.Resources.MergedDictionaries.Add ( revertThemeDictionary );

				_logger.LogInformation ( "Reverted theme to {Theme}.", revertToDarkMode ? "Dark" : "Light" );
			}
			catch ( Exception revertEx )
			{
				_logger.LogError ( revertEx, "Error occurred while reverting the theme." );
			}
		}
	}
}
