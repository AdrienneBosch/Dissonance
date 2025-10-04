using System;
using System.Linq;
using System.Speech.Synthesis;

using Dissonance.Infrastructure.Constants;
using Dissonance.Services.MessageService;

using Microsoft.Extensions.Logging;

namespace Dissonance.Services.TTSService
{
        internal class TTSService : ITTSService
        {
                private readonly ILogger<TTSService> _logger;
                private readonly IMessageService _messageService;
                private readonly SpeechSynthesizer _synthesizer;

                public event EventHandler<SpeakCompletedEventArgs>? SpeechCompleted;
                public event EventHandler<SpeakProgressEventArgs>? SpeechProgress;

                public TTSService ( ILogger<TTSService> logger, IMessageService messageService )
                {
                        _logger = logger ?? throw new ArgumentNullException ( nameof ( logger ) );
                        _messageService = messageService ?? throw new ArgumentNullException ( nameof ( messageService ) );
                        _synthesizer = new SpeechSynthesizer ( );
                        _synthesizer.SpeakCompleted += OnSpeakCompleted;
                        _synthesizer.SpeakProgress += OnSpeakProgress;
                }

                public void SetTTSParameters ( string voice, double rate, int volume )
                {
                        try
                        {
                                var installedVoices = _synthesizer.GetInstalledVoices ( );
                                var voiceInfo = installedVoices.FirstOrDefault ( v => v.VoiceInfo.Name.Equals ( voice, StringComparison.OrdinalIgnoreCase ) );

                                if ( voiceInfo != null )
                                {
                                        _synthesizer.SelectVoice ( voiceInfo.VoiceInfo.Name );
                                }
                                else if ( installedVoices.Any ( ) )
                                {
                                        _logger.LogWarning ( "Voice '{Voice}' not available. Using default voice.", voice );
                                        _synthesizer.SelectVoice ( installedVoices.First ( ).VoiceInfo.Name );
                                }

                                _synthesizer.Rate = ( int ) rate;
                                _synthesizer.Volume = volume;
                        }
                        catch ( Exception ex )
                        {
                                _messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.TTSServiceError, $"Failed to update TTS parameters for: \nVoice: {voice} \nRate: {rate} \nVolume: {volume}", ex );
                        }
                }

                public Prompt? Speak ( string text )
                {
                        try
                        {
                                return _synthesizer.SpeakAsync ( text );
                        }
                        catch ( Exception ex )
                        {
                                _messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.TTSServiceError, $"Failed to speak text due to an unhandled exception. \nText: {text}", ex );
                                return null;
                        }
                }

                public void Stop ( )
                {
                        try
                        {
                                _synthesizer.SpeakAsyncCancelAll ( );
                        }
                        catch ( Exception ex )
                        {
                                _messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.TTSServiceError, "Failed to stop speaking text due to an unhandled exception.", ex );
                        }
                }

                private void OnSpeakCompleted ( object? sender, SpeakCompletedEventArgs e )
                {
                        SpeechCompleted?.Invoke ( this, e );
                }

                private void OnSpeakProgress ( object? sender, SpeakProgressEventArgs e )
                {
                        SpeechProgress?.Invoke ( this, e );
                }
        }
}
