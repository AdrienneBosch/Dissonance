using System.IO;

using Dissonance.Infrastructure.Constants;
using Dissonance.Services.MessageService;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using NLog;

using static Dissonance.AppSettings;

namespace Dissonance.Services.SettingsService
{
	internal class SettingsService : ISettingsService
	{
		private const string SettingsFilePath = "appsettings.json";
		private AppSettings _currentSettings;
		private readonly ILogger<SettingsService> _logger;
		private readonly Dissonance.Services.MessageService.IMessageService _messageService;

		public SettingsService ( ILogger<SettingsService> logger, Dissonance.Services.MessageService.IMessageService messageService )
		{
			 _logger = logger ?? throw new ArgumentNullException ( nameof ( logger ) );
			_messageService = messageService ?? throw new ArgumentNullException ( nameof ( messageService ) );

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

		public AppSettings GetCurrentSettings ( ) => _currentSettings;

		public AppSettings LoadSettings ( )
		{
			try
			{
				var json = File.ReadAllText(SettingsFilePath);
				return JsonConvert.DeserializeObject<AppSettings> ( json ) ?? GetDefaultSettings ( );
			}
			catch ( Exception ex )
			{
				_messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.SettingsServiceError, "Failed to load settings, reverting to default.", ex );
				return GetDefaultSettings ( );
			}
		}

		public void ResetToFactorySettings ( )
		{
			_currentSettings = GetDefaultSettings ( );
			SaveSettings ( _currentSettings );
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
				_messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.SettingsServiceError, "Failed to save settings.", ex );
			}
		}
	}
}