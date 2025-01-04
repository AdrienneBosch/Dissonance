using System;
using System.Speech.Synthesis;

using Dissonance.Infrastructure.Constants;

using Microsoft.Extensions.Logging;

namespace Dissonance.Services.TTSService
{
	internal class TTSService : ITTSService
	{
		private readonly ILogger<TTSService> _logger;
		private readonly Dissonance.Services.MessageService.IMessageService _messageService;
		private readonly SpeechSynthesizer _synthesizer;
		public event EventHandler SpeechCompleted;

		public TTSService ( ILogger<TTSService> logger, Dissonance.Services.MessageService.IMessageService messageService )
		{
			_logger = logger ?? throw new ArgumentNullException ( nameof ( logger ) );
			_messageService = messageService ?? throw new ArgumentNullException ( nameof ( messageService ) );
			_synthesizer = new SpeechSynthesizer ( );
			_synthesizer.SpeakCompleted += OnSpeakCompleted;
		}

		public void SetTTSParameters ( string voice, double rate, int volume )
		{
			try
			{
				var installedVoices = _synthesizer.GetInstalledVoices();
				bool voiceAvailable = installedVoices.Any(v => v.VoiceInfo.Name.Equals(voice, StringComparison.OrdinalIgnoreCase));

				if ( voiceAvailable )
				{
					_synthesizer.SelectVoice ( voice );
				}
				else
				{
					_logger.LogWarning ( $"Voice '{voice}' not available. Using default voice." );
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

		public void Speak ( string text )
		{
			try
			{
				_synthesizer.SpeakAsync ( text );
			}
			catch ( Exception ex )
			{
				_messageService.DissonanceMessageBoxShowError ( MessageBoxTitles.TTSServiceError, $"Failed to speak text due to an unhandled exception. \nText: {text}", ex );
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

		private void OnSpeakCompleted ( object sender, SpeakCompletedEventArgs e )
		{
			SpeechCompleted?.Invoke ( this, EventArgs.Empty );
		}
	}
}
