using System.Windows;

using Dissonance.SettingsManagers;

using Microsoft.Extensions.Logging;

namespace Dissonance
{
	public partial class MainWindow : Window
	{
		private readonly ISettingsManager _settingsManager;
		private readonly ILogger<MainWindow> _logger;
		private readonly ThemeManager _themeManager;
		private AppSettings _appSettings;
		private readonly ILogger<Dissonance.UserControls.Buttons.ThemeToggleButton> _themeToggleButtonLogger;

		public MainWindow ( ISettingsManager settingsManager, ILogger<MainWindow> logger, ThemeManager themeManager, ILogger<Dissonance.UserControls.Buttons.ThemeToggleButton> themeToggleButtonLogger )
		{
			InitializeComponent ( );
			_settingsManager = settingsManager;
			_logger = logger;
			_themeManager = themeManager;
			_themeToggleButtonLogger = themeToggleButtonLogger;
			Loaded += MainWindow_Loaded;
		}

		private async void MainWindow_Loaded ( object sender, RoutedEventArgs e )
		{
			await InitializeSettingsAsync ( );
		}

		private async Task InitializeSettingsAsync ( )
		{
			try
			{
				_appSettings = await _settingsManager.LoadSettingsAsync ( );
				InitializeSettings ( );

				var themeToggleButton = new Dissonance.UserControls.Buttons.ThemeToggleButton(_settingsManager, _appSettings, _themeManager, _themeToggleButtonLogger);
				ThemeToggleButtonContainer.Children.Add ( themeToggleButton );

				_logger.LogInformation ( "Settings initialized successfully." );
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "An error occurred while initializing settings." );
				MessageBox.Show ( "An error occurred while initializing settings. Please see the logs for more details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}

		private void InitializeSettings ( )
		{
			var volume = _appSettings.ScreenReader.Volume;
			var voiceRate = _appSettings.ScreenReader.VoiceRate;
			var zoomLevel = _appSettings.Magnifier.ZoomLevel;
			var invertColors = _appSettings.Magnifier.InvertColors;

			bool isDarkMode = _appSettings.Theme.IsDarkMode;
			_themeManager.SetTheme ( isDarkMode );

			_logger.LogInformation ( "UI components initialized with settings." );
		}

		private async void SaveSettings ( )
		{
			try
			{
				await _settingsManager.SaveSettingsAsync ( _appSettings );
				_logger.LogInformation ( "Settings saved successfully." );
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "An error occurred while saving settings." );
				MessageBox.Show ( "An error occurred while saving settings. Please see the logs for more details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}

		private void HeadingLable_Loaded ( object sender, RoutedEventArgs e )
		{

        }
    }
}
