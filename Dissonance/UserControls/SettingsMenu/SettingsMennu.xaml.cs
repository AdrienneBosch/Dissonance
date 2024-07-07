using Dissonance.SettingsManagers;

using Microsoft.Extensions.DependencyInjection;
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

		public SettingsMennu ( )
		{
			InitializeComponent ( );
			_settingsManager = App.ServiceProvider.GetRequiredService<ISettingsManager> ( );
			_appSettings = App.ServiceProvider.GetRequiredService<AppSettings> ( );
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
			}
			catch ( Exception ex )
			{
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
				}
			}
			catch ( Exception ex )
			{
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
					ThemeManager.SetTheme ( _appSettings.Theme.IsDarkMode ); // Ensure the theme is updated
					MessageBox.Show ( "Configuration loaded successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information );
				}
			}
			catch ( Exception ex )
			{
				MessageBox.Show ( $"Error loading configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}

		private async void RestoreFactoryDefault_Click ( object sender, RoutedEventArgs e )
		{
			try
			{
				var settings = await _settingsManager.LoadFactoryDefaultAsync();
				_appSettings.CopyFrom ( settings );
				ThemeManager.SetTheme ( _appSettings.Theme.IsDarkMode ); // Ensure the theme is updated
				MessageBox.Show ( "Factory default settings have been restored.", "Success", MessageBoxButton.OK, MessageBoxImage.Information );
			}
			catch ( Exception ex )
			{
				MessageBox.Show ( $"Error restoring factory default settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}
	}
}
