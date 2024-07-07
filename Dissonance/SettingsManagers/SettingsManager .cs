using System;
using System.IO;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Formatting = Newtonsoft.Json.Formatting;

namespace Dissonance.SettingsManagers
{
	public class SettingsManager : ISettingsManager
	{
		private const string SettingsFilePath = "settings.json";
		private const string DefaultSettingsFilePath = "defaultSettings.json";

		public async Task<AppSettings> LoadSettingsAsync ( string customFilePath = null )
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

		public async Task SaveSettingsAsync ( AppSettings settings, string customFilePath = null )
		{
			var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
			string path = customFilePath ?? SettingsFilePath;
			await File.WriteAllTextAsync ( path, json );
		}

		public async Task SaveAsDefaultConfigurationAsync ( AppSettings settings )
		{
			var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
			await File.WriteAllTextAsync ( SettingsFilePath, json );
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
			}
			catch ( Exception ex )
			{
				// Log the exception using a logging framework
				// Log.Error(ex, $"Error creating default settings file: {ex.Message}");
				Console.WriteLine ( $"Error creating default settings file: {ex.Message}" );
				throw;
			}
		}
	}
}
