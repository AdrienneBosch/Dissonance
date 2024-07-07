using System;
using System.Windows;

namespace Dissonance
{
	public static class ThemeManager
	{
		private static AppSettings _appSettings;

		public static void Initialize ( AppSettings appSettings )
		{
			_appSettings = appSettings;
		}

		public static void SetTheme ( bool isDarkMode )
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
		}
	}
}
