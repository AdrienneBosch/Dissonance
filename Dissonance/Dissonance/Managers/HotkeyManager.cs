using System;
using System.Speech.Synthesis;

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

                public event EventHandler<bool>? SpeakingStateChanged;

                public bool IsSpeaking => _isSpeaking;

                public void Initialize ( MainWindow mainWindow )
                {
                        _hotkeyService.Initialize ( mainWindow );
                        var settings = _settingsService.GetCurrentSettings ( );

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

                public bool TrySpeakClipboardFromRequest ( string source )
                {
                        if ( _isSpeaking )
                        {
                                _logger.LogInformation ( "Ignoring clipboard speak request from {Source} because narration is already in progress.", source );
                                return false;
                        }

                        return TrySpeakClipboard ( source );
                }

                public void StopSpeakingFromRequest ( string source )
                {
                        if ( !_isSpeaking )
                        {
                                return;
                        }

                        _ttsService.Stop ( );
                        UpdateSpeakingState ( false );
                        _logger.LogInformation ( "TTS playback stopped by {Source}.", source );
                }

                private void OnHotkeyPressed ( )
                {
                        if ( _isSpeaking )
                        {
                                StopSpeakingFromRequest ( "hotkey" );
                                return;
                        }

                        TrySpeakClipboard ( "hotkey" );
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

                private void OnSpeechCompleted ( object? sender, SpeakCompletedEventArgs e )
                {
                        UpdateSpeakingState ( false );
                        _logger.LogInformation ( "TTS playback completed." );
                }

                private bool TrySpeakClipboard ( string source )
                {
                        var clipboardText = _clipboardManager.CopySelectionAndGetValidatedText ( );
                        if ( string.IsNullOrEmpty ( clipboardText ) )
                        {
                                clipboardText = _clipboardManager.GetValidatedClipboardText ( );
                        }

                        if ( string.IsNullOrEmpty ( clipboardText ) )
                        {
                                _logger.LogWarning ( "Nothing to read. The selection and clipboard are empty or invalid." );
                                return false;
                        }

                        return StartSpeaking ( clipboardText, source );
                }

                private bool StartSpeaking ( string clipboardText, string source )
                {
                        var settings = _settingsService.GetCurrentSettings ( );
                        _ttsService.SetTTSParameters ( settings.Voice, settings.VoiceRate, settings.Volume );
                        var prompt = _ttsService.Speak ( clipboardText );
                        var started = prompt != null;
                        UpdateSpeakingState ( started );
                        _logger.LogInformation ( "Speaking clipboard text triggered by {Source}.", source );
                        return started;
                }

                private void UpdateSpeakingState ( bool isSpeaking )
                {
                        if ( _isSpeaking == isSpeaking )
                                return;

                        _isSpeaking = isSpeaking;
                        SpeakingStateChanged?.Invoke ( this, _isSpeaking );
                }
        }
}
