using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Formatting = Newtonsoft.Json.Formatting;

namespace Dissonance.SettingsManagers
{
	public class SettingsManager : ISettingsManager
	{
		private const string SettingsFilePath = "settings.json";
		private const string DefaultSettingsFilePath = "defaultSettings.json";
		private readonly ILogger<SettingsManager> _logger;

		public SettingsManager ( ILogger<SettingsManager> logger )
		{
			_logger = logger ?? throw new ArgumentNullException ( nameof ( logger ) );
		}

		public async Task<AppSettings> LoadSettingsAsync ( string customFilePath = null )
		{
			try
			{
				await EnsureSettingsFileExistsAsync ( );

				string path = customFilePath ?? SettingsFilePath;
				if ( !File.Exists ( path ) )
				{
					throw new FileNotFoundException ( $"The specified settings file does not exist: {path}" );
				}

				var json = await File.ReadAllTextAsync(path);
				return JsonConvert.DeserializeObject<AppSettings> ( json );
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "An error occurred while loading settings from {Path}", customFilePath ?? SettingsFilePath );
				throw;
			}
		}

		public async Task SaveSettingsAsync ( AppSettings settings, string customFilePath = null )
		{
			try
			{
				var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
				string path = customFilePath ?? SettingsFilePath;
				await File.WriteAllTextAsync ( path, json );
				_logger.LogInformation ( "Settings saved successfully to {Path}", path );
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "An error occurred while saving settings to {Path}", customFilePath ?? SettingsFilePath );
				throw;
			}
		}

		public async Task SaveAsDefaultConfigurationAsync ( AppSettings settings )
		{
			try
			{
				var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
				await File.WriteAllTextAsync ( DefaultSettingsFilePath, json );
				_logger.LogInformation ( "Default settings configuration saved successfully." );
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "An error occurred while saving the default configuration." );
				throw;
			}
		}

		public async Task<AppSettings> LoadFactoryDefaultAsync ( )
		{
			try
			{
				if ( !File.Exists ( DefaultSettingsFilePath ) )
				{
					await CreateDefaultSettingsFileAsync ( DefaultSettingsFilePath );
				}

				var json = await File.ReadAllTextAsync(DefaultSettingsFilePath);
				return JsonConvert.DeserializeObject<AppSettings> ( json );
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "An error occurred while loading the factory default settings." );
				throw;
			}
		}

		private async Task EnsureSettingsFileExistsAsync ( )
		{
			if ( !File.Exists ( SettingsFilePath ) )
			{
				await CreateDefaultSettingsFileAsync ( SettingsFilePath );
			}
		}

		private async Task CreateDefaultSettingsFileAsync ( string filePath )
		{
			try
			{
				var defaultSettings = new AppSettings
				{
					ScreenReader = new ScreenReaderSettings
					{
						Volume = 80,
						VoiceRate = 5
					},
					Magnifier = new MagnifierSettings
					{
						ZoomLevel = 3,
						InvertColors = true
					}
				};

				var json = JsonConvert.SerializeObject(defaultSettings, Formatting.Indented);
				await File.WriteAllTextAsync ( filePath, json );
				_logger.LogInformation ( "Default settings file created at {Path}", filePath );
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "Error creating default settings file: {Message}", ex.Message );
				throw;
			}
		}
	}
}
