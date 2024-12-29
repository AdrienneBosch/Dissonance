using Dissonance.Services.HotkeyService;
using Dissonance.Services.SettingsService;
using Dissonance.Services.TTSService;

using Microsoft.Extensions.Logging;

namespace Dissonance.Managers
{
	public class HotkeyManager
	{
		private readonly ClipboardManager _clipboardManager;
		private readonly IHotkeyService _hotkeyService;
		private readonly ILogger<HotkeyManager> _logger;
		private readonly ISettingsService _settingsService;
		private readonly ITTSService _ttsService;
		private bool _isSpeaking;

		public HotkeyManager ( IHotkeyService hotkeyService, ISettingsService settingsService, ITTSService ttsService, ClipboardManager clipboardManager, ILogger<HotkeyManager> logger )
		{
			_hotkeyService = hotkeyService ?? throw new ArgumentNullException ( nameof ( hotkeyService ) );
			_settingsService = settingsService ?? throw new ArgumentNullException ( nameof ( settingsService ) );
			_ttsService = ttsService ?? throw new ArgumentNullException ( nameof ( ttsService ) );
			_clipboardManager = clipboardManager ?? throw new ArgumentNullException ( nameof ( clipboardManager ) );
			_logger = logger ?? throw new ArgumentNullException ( nameof ( logger ) );
		}

		private void OnHotkeyPressed ( )
		{
			if ( _isSpeaking )
			{
				_ttsService.Stop ( );
				_isSpeaking = false;
				_logger.LogInformation ( "TTS playback stopped." );
				return;
			}

			var clipboardText = _clipboardManager.GetValidatedClipboardText();
			if ( !string.IsNullOrEmpty ( clipboardText ) )
			{
				var settings = _settingsService.GetCurrentSettings();
				_ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, settings.Volume );
				_ttsService.Speak ( clipboardText );
				_isSpeaking = true;
				_logger.LogInformation ( $"Speaking clipboard text: {clipboardText}" );
			}
			else
			{
				_logger.LogWarning ( "Clipboard is empty or contains invalid text." );
			}
		}

		public void Dispose ( )
		{
			_hotkeyService.Dispose ( );
			_logger.LogInformation ( "HotkeyManager disposed." );
		}

		public void Initialize ( MainWindow mainWindow )
		{
			_hotkeyService.Initialize ( mainWindow );
			var settings = _settingsService.GetCurrentSettings();

			_hotkeyService.RegisterHotkey ( new AppSettings.HotkeySettings
			{
				Modifiers = settings.Hotkey.Modifiers,
				Key = settings.Hotkey.Key
			} );

			_hotkeyService.HotkeyPressed += OnHotkeyPressed;
			_logger.LogInformation ( "HotkeyManager initialized and hotkey registered." );
		}
	}
}