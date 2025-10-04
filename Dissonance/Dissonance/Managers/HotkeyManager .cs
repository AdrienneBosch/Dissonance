using System;

using Dissonance.Services.HotkeyService;
using Dissonance.Services.SettingsService;
using Dissonance.Services.TTSService;

using Microsoft.Extensions.Logging;

namespace Dissonance.Managers
{
        public class HotkeyManager : IDisposable
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
                        _ttsService.SpeechCompleted += OnSpeechCompleted;
                        _clipboardManager.ClipboardTextReady += OnClipboardTextReady;
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

                public void Dispose ( )
                {
                        _hotkeyService.HotkeyPressed -= OnHotkeyPressed;
                        _clipboardManager.ClipboardTextReady -= OnClipboardTextReady;
                        _ttsService.SpeechCompleted -= OnSpeechCompleted;
                        _hotkeyService.Dispose ( );
                        _logger.LogInformation ( "HotkeyManager disposed." );
                }

                private void OnHotkeyPressed ( )
                {
                        if ( _isSpeaking )
                        {
                                _ttsService.Stop ( );
                                _isSpeaking = false;
                                _logger.LogInformation ( "TTS playback stopped by hotkey." );
                                return;
                        }

                        SpeakClipboardContents ( );
                }

                private void OnClipboardTextReady ( object? sender, string clipboardText )
                {
                        if ( _isSpeaking )
                        {
                                _logger.LogInformation ( "Skipping automatic clipboard read because speech is already in progress." );
                                return;
                        }

                        StartSpeaking ( clipboardText, "clipboard" );
                }

                private void OnSpeechCompleted ( object sender, EventArgs e )
                {
                        _isSpeaking = false;
                        _logger.LogInformation ( "TTS playback completed." );
                }

                private void SpeakClipboardContents ( )
                {
                        var clipboardText = _clipboardManager.CopySelectionAndGetValidatedText ( );
                        if ( string.IsNullOrEmpty ( clipboardText ) )
                        {
                                clipboardText = _clipboardManager.GetValidatedClipboardText ( );
                        }

                        if ( string.IsNullOrEmpty ( clipboardText ) )
                        {
                                _logger.LogWarning ( "Nothing to read. The selection and clipboard are empty or invalid." );
                                return;
                        }

                        StartSpeaking ( clipboardText, "hotkey" );
                }

                private void StartSpeaking ( string clipboardText, string source )
                {
                        var settings = _settingsService.GetCurrentSettings();
                        _ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, settings.Volume );
                        _ttsService.Speak ( clipboardText );
                        _isSpeaking = true;
                        _logger.LogInformation ( "Speaking clipboard text triggered by {Source}.", source );
                }
        }
}
