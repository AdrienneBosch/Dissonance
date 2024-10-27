using System;
using System.Linq;
using System.Speech.Synthesis;  // For TTS functionality

using NLog;

namespace Dissonance.Services.TTSService
{
	internal class TTSService : ITTSService
	{
		private readonly SpeechSynthesizer _synthesizer;
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public TTSService ( )
		{
			_synthesizer = new SpeechSynthesizer ( );

			// Log all available voices
			foreach ( var voice in _synthesizer.GetInstalledVoices ( ) )
			{
				Logger.Info ( $"Installed voice: {voice.VoiceInfo.Name}" );
			}
		}

		/// <summary>
		/// Speaks the given text using the configured TTS settings.
		/// </summary>
		/// <param name="text">The text to convert to speech.</param>
		public void Speak ( string text )
		{
			try
			{
				_synthesizer.SpeakAsync ( text );
				Logger.Info ( "Speaking text: " + text );
			}
			catch ( Exception ex )
			{
				Logger.Error ( ex, "Failed to speak text." );
			}
		}

		/// <summary>
		/// Updates the TTS parameters (voice, rate, volume).
		/// </summary>
		/// <param name="voice">Voice to use.</param>
		/// <param name="rate">Speed of speech.</param>
		/// <param name="volume">Volume level.</param>
		public void SetTTSParameters ( string voice, double rate, int volume )
		{
			try
			{
				var installedVoices = _synthesizer.GetInstalledVoices();
				bool voiceAvailable = installedVoices.Any(v => v.VoiceInfo.Name.Equals(voice, StringComparison.OrdinalIgnoreCase));

				if ( voiceAvailable )
				{
					_synthesizer.SelectVoice ( voice );
					Logger.Info ( $"Selected voice: {voice}" );
				}
				else
				{
					Logger.Warn ( $"Voice '{voice}' not available. Using default voice." );
					_synthesizer.SelectVoice ( installedVoices.First ( ).VoiceInfo.Name );  // Use default if not found
				}

				_synthesizer.Rate = ( int ) rate;
				_synthesizer.Volume = volume;
				Logger.Info ( $"TTS parameters set: Voice = {_synthesizer.Voice.Name}, Rate = {rate}, Volume = {volume}" );
			}
			catch ( Exception ex )
			{
				Logger.Error ( ex, "Failed to update TTS parameters." );
			}
		}
	}
}
