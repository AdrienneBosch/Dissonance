using Dissonance.SettingsManagers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Dissonance.UserControls.SettingsMenu
{
	public partial class SettingsMennu : UserControl
	{
		private readonly ISettingsManager _settingsManager;
		private readonly AppSettings _appSettings;
		private readonly ThemeManager _themeManager;
		private readonly ILogger<SettingsMennu> _logger;

		public SettingsMennu ( )
		{
			InitializeComponent ( );

			// Retrieve instances from the service provider
			var serviceProvider = App.ServiceProvider;
			_settingsManager = serviceProvider.GetRequiredService<ISettingsManager> ( );
			_appSettings = serviceProvider.GetRequiredService<AppSettings> ( );
			_themeManager = serviceProvider.GetRequiredService<ThemeManager> ( );
			_logger = serviceProvider.GetRequiredService<ILogger<SettingsMennu>> ( );
		}

		private AppSettings GetCurrentAppSettings ( )
		{
			return _appSettings;
		}

		private async void SetAsDefaultConfiguration_Click ( object sender, RoutedEventArgs e )
		{
			try
			{
				var settings = GetCurrentAppSettings();
				await _settingsManager.SaveAsDefaultConfigurationAsync ( settings );
				MessageBox.Show ( "Current settings have been saved as the default configuration.", "Success", MessageBoxButton.OK, MessageBoxImage.Information );
				_logger.LogInformation ( "Default configuration saved successfully." );
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "Error saving default configuration." );
				MessageBox.Show ( $"Error saving default configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}

		private async void SaveConfiguration_Click ( object sender, RoutedEventArgs e )
		{
			try
			{
				var saveFileDialog = new SaveFileDialog
				{
					Filter = "JSON Files (*.json)|*.json",
					DefaultExt = "json"
				};

				if ( saveFileDialog.ShowDialog ( ) == true )
				{
					var settings = GetCurrentAppSettings();
					await _settingsManager.SaveSettingsAsync ( settings, saveFileDialog.FileName );
					MessageBox.Show ( "Configuration saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information );
					_logger.LogInformation ( "Configuration saved to {FileName}.", saveFileDialog.FileName );
				}
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "Error saving configuration." );
				MessageBox.Show ( $"Error saving configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}

		private async void LoadConfiguration_Click ( object sender, RoutedEventArgs e )
		{
			try
			{
				var openFileDialog = new OpenFileDialog
				{
					Filter = "JSON Files (*.json)|*.json",
					DefaultExt = "json"
				};

				if ( openFileDialog.ShowDialog ( ) == true )
				{
					var settings = await _settingsManager.LoadSettingsAsync(openFileDialog.FileName);
					_appSettings.CopyFrom ( settings );
					_themeManager.SetTheme ( _appSettings.Theme.IsDarkMode ); // Ensure the theme is updated
					MessageBox.Show ( "Configuration loaded successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information );
					_logger.LogInformation ( "Configuration loaded from {FileName}.", openFileDialog.FileName );
				}
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "Error loading configuration." );
				MessageBox.Show ( $"Error loading configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}

		private async void RestoreFactoryDefault_Click ( object sender, RoutedEventArgs e )
		{
			try
			{
				var settings = await _settingsManager.LoadFactoryDefaultAsync();
				_appSettings.CopyFrom ( settings );
				_themeManager.SetTheme ( _appSettings.Theme.IsDarkMode ); // Ensure the theme is updated
				MessageBox.Show ( "Factory default settings have been restored.", "Success", MessageBoxButton.OK, MessageBoxImage.Information );
				_logger.LogInformation ( "Factory default settings restored successfully." );
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "Error restoring factory default settings." );
				MessageBox.Show ( $"Error restoring factory default settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}
	}
}
