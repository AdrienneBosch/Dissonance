using System.Text;
using System.Windows;

using Dissonance.SettingsManagers;

namespace Dissonance
{
	public partial class MainWindow : Window
	{
		private readonly ISettingsManager _settingsManager;
		private AppSettings _appSettings;

		public MainWindow ( ISettingsManager settingsManager )
		{
			InitializeComponent ( );
			_settingsManager = settingsManager;
			_appSettings = _settingsManager.LoadSettings ( );
			InitializeSettings ( );

			// Create instance of ThemeToggleButton
			var themeToggleButton = new Dissonance.UserControls.Buttons.ThemeToggleButton
			{
				SettingsManager = _settingsManager,
				AppSettings = _appSettings
			};

			// Add ThemeToggleButton to the container
			ThemeToggleButtonContainer.Children.Add ( themeToggleButton );
		}

		private void InitializeSettings ( )
		{
			var volume = _appSettings.ScreenReader.Volume;
			var voiceRate = _appSettings.ScreenReader.VoiceRate;
			var zoomLevel = _appSettings.Magnifier.ZoomLevel;
			var invertColors = _appSettings.Magnifier.InvertColors;

			// Initialize the theme based on saved settings
			bool isDarkMode = _appSettings.Theme.IsDarkMode;
			ThemeToggleButton.IsChecked = isDarkMode;
			ThemeManager.SetTheme ( isDarkMode );
		}

		private void SaveSettings ( )
		{
			_settingsManager.SaveSettings ( _appSettings );
		}

		private void ThemeToggleButton_Checked ( object sender, RoutedEventArgs e )
		{
			_appSettings.Theme.IsDarkMode = true; // Set dark mode
			ThemeManager.SetTheme ( true ); // Apply dark mode
			SaveSettings ( ); // Save settings
		}

		private void ThemeToggleButton_Unchecked ( object sender, RoutedEventArgs e )
		{
			_appSettings.Theme.IsDarkMode = true;
			ThemeManager.SetTheme ( false );
			SaveSettings ( );
		}
	}
}
