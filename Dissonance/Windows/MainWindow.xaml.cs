using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Dissonance.SettingsManagers;

using Microsoft.Extensions.Logging;

namespace Dissonance
{
	public partial class MainWindow : Window
	{
		private readonly ISettingsManager _settingsManager;
		private readonly ILogger<MainWindow> _logger;
		private AppSettings _appSettings;

		public MainWindow ( ISettingsManager settingsManager, ILogger<MainWindow> logger )
		{
			InitializeComponent ( );
			_settingsManager = settingsManager;
			_logger = logger;
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

				var themeToggleButton = new Dissonance.UserControls.Buttons.ThemeToggleButton
				{
					SettingsManager = _settingsManager,
					AppSettings = _appSettings
				};

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
			ThemeManager.SetTheme ( isDarkMode );

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
	}
}
