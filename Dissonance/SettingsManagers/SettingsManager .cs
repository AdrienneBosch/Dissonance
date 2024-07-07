using System.IO;

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
			_logger = logger;
		}

		public async Task<AppSettings> LoadSettingsAsync ( string customFilePath = null )
		{
			await EnsureSettingsFileExistsAsync ( );

			string path = customFilePath ?? SettingsFilePath;
			if ( !File.Exists ( path ) )
			{
				_logger.LogError ( $"The specified settings file does not exist: {path}" );
				throw new FileNotFoundException ( $"The specified settings file does not exist: {path}" );
			}

			var json = await File.ReadAllTextAsync(path);
			return JsonConvert.DeserializeObject<AppSettings> ( json );
		}

		public async Task SaveSettingsAsync ( AppSettings settings, string customFilePath = null )
		{
			var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
			string path = customFilePath ?? SettingsFilePath;
			await File.WriteAllTextAsync ( path, json );
			_logger.LogInformation ( "Settings saved successfully to {path}.", path );
		}

		public async Task SaveAsDefaultConfigurationAsync ( AppSettings settings )
		{
			var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
			await File.WriteAllTextAsync ( SettingsFilePath, json );
			_logger.LogInformation ( "Default settings configuration saved." );
		}

		public async Task<AppSettings> LoadFactoryDefaultAsync ( )
		{
			if ( !File.Exists ( DefaultSettingsFilePath ) )
			{
				await CreateDefaultSettingsFileAsync ( DefaultSettingsFilePath );
			}

			var json = await File.ReadAllTextAsync(DefaultSettingsFilePath);
			return JsonConvert.DeserializeObject<AppSettings> ( json );
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
				_logger.LogInformation ( "Default settings file created at {filePath}.", filePath );
			}
			catch ( Exception ex )
			{
				_logger.LogError ( ex, "Error creating default settings file." );
				throw;
			}
		}
	}
}
