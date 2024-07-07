using System;
using System.IO;

using Newtonsoft.Json;

using Formatting = Newtonsoft.Json.Formatting;

namespace Dissonance.SettingsManagers
{
	public class SettingsManager : ISettingsManager
	{
		private const string SettingsFilePath = "settings.json";
		private const string DefaultSettingsFilePath = "defaultSettings.json";

		public AppSettings LoadSettings ( string customFilePath = null )
		{
			EnsureSettingsFileExists ( );

			string path = customFilePath ?? SettingsFilePath;
			if ( !File.Exists ( path ) )
			{
				throw new FileNotFoundException ( $"The specified settings file does not exist: {path}" );
			}

			var json = File.ReadAllText(path);
			return JsonConvert.DeserializeObject<AppSettings> ( json );
		}

		public void SaveSettings ( AppSettings settings, string customFilePath = null )
		{
			var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
			string path = customFilePath ?? SettingsFilePath;
			File.WriteAllText ( path, json );
		}

		public void SaveAsDefaultConfiguration ( AppSettings settings )
		{
			var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
			File.WriteAllText ( DefaultSettingsFilePath, json );
		}

		public AppSettings LoadFactoryDefault ( )
		{
			if ( !File.Exists ( DefaultSettingsFilePath ) )
			{
				CreateDefaultSettingsFile ( DefaultSettingsFilePath );
			}

			var json = File.ReadAllText(DefaultSettingsFilePath);
			return JsonConvert.DeserializeObject<AppSettings> ( json );
		}

		private void EnsureSettingsFileExists ( )
		{
			if ( !File.Exists ( SettingsFilePath ) )
			{
				CreateDefaultSettingsFile ( SettingsFilePath );
			}
		}

		private void CreateDefaultSettingsFile ( string filePath )
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
				File.WriteAllText ( filePath, json );
			}
			catch ( Exception ex )
			{
				Console.WriteLine ( $"Error creating default settings file: {ex.Message}" );
				throw;
			}
		}
	}
}
