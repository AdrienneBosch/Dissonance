using System;
using System.Windows;
using System.Windows.Controls;

using Dissonance.SettingsManagers;

using Microsoft.Win32;

namespace Dissonance.UserControls.SettingsMenu
{
	public partial class SettingsMennu : UserControl
	{
		private readonly SettingsManager _settingsManager;

		public SettingsMennu ( )
		{
			InitializeComponent ( );
			_settingsManager = new SettingsManager ( );
		}

		private void SetAsDefaultConfiguration_Click ( object sender, RoutedEventArgs e )
		{
			try
			{
				var settings = _settingsManager.LoadSettings();
				_settingsManager.SaveAsDefaultConfiguration ( settings );
				MessageBox.Show ( "Current settings have been saved as the default configuration.", "Success", MessageBoxButton.OK, MessageBoxImage.Information );
			}
			catch ( Exception ex )
			{
				MessageBox.Show ( $"Error saving default configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}

		private void SaveConfiguration_Click ( object sender, RoutedEventArgs e )
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
					var settings = _settingsManager.LoadSettings();
					_settingsManager.SaveSettings ( settings, saveFileDialog.FileName );
					MessageBox.Show ( "Configuration saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information );
				}
			}
			catch ( Exception ex )
			{
				MessageBox.Show ( $"Error saving configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}

		private void LoadConfiguration_Click ( object sender, RoutedEventArgs e )
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
					var settings = _settingsManager.LoadSettings(openFileDialog.FileName);
					_settingsManager.SaveSettings ( settings ); // Optionally save the loaded settings to the default path
					MessageBox.Show ( "Configuration loaded successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information );
				}
			}
			catch ( Exception ex )
			{
				MessageBox.Show ( $"Error loading configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}

		private void RestoreFactoryDefault_Click ( object sender, RoutedEventArgs e )
		{
			try
			{
				var settings = _settingsManager.LoadFactoryDefault();
				_settingsManager.SaveSettings ( settings ); // Optionally save the factory default settings to the default path
				MessageBox.Show ( "Factory default settings have been restored.", "Success", MessageBoxButton.OK, MessageBoxImage.Information );
			}
			catch ( Exception ex )
			{
				MessageBox.Show ( $"Error restoring factory default settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}
	}
}
