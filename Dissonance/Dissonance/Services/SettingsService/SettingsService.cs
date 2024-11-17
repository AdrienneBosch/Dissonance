using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

using static Dissonance.AppSettings;

namespace Dissonance.Services.SettingsService
{
    internal class SettingsService : ISettingsService
	{
		private const string SettingsFilePath = "appsettings.json";
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private AppSettings _currentSettings;

		public SettingsService ( )
		{
			if ( !File.Exists ( SettingsFilePath ) )
			{
				_currentSettings = GetDefaultSettings ( );
				SaveSettings ( _currentSettings );
			}
			else
			{
				_currentSettings = LoadSettings ( );
			}
		}

		public AppSettings LoadSettings ( )
		{
			try
			{
				var json = File.ReadAllText(SettingsFilePath);
				return JsonConvert.DeserializeObject<AppSettings> ( json ) ?? GetDefaultSettings ( );
			}
			catch ( Exception ex )
			{
				Logger.Error ( ex, "Failed to load settings, reverting to default." );
				return GetDefaultSettings ( );
			}
		}

		public void SaveSettings ( AppSettings settings )
		{
			try
			{
				var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
				File.WriteAllText ( SettingsFilePath, json );
				_currentSettings = settings;
			}
			catch ( Exception ex )
			{
				Logger.Error ( ex, "Failed to save settings." );
			}
		}

		public void ResetToFactorySettings ( )
		{
			_currentSettings = GetDefaultSettings ( );
			SaveSettings ( _currentSettings );
		}

		public AppSettings GetCurrentSettings ( ) => _currentSettings;

		private AppSettings GetDefaultSettings ( )
		{
			return new AppSettings
			{
				VoiceRate = 1.0,
				Volume = 50,
				Voice = "Microsoft David",
				Hotkey = new HotkeySettings { Modifiers = "Alt", Key = "E" },
			};
		}
	}
}
