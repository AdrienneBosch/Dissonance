using System.Speech.Synthesis;  // For TTS functionality

using NLog;

namespace Dissonance.Services.TTSService
{
	internal class TTSService : ITTSService
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger ( );

		private readonly SpeechSynthesizer _synthesizer;

		public TTSService ( )
		{
			_synthesizer = new SpeechSynthesizer ( );
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
					Logger.Info ( $"Selected voice: {voice}" );
				}
				else
				{
					Logger.Warn ( $"Voice '{voice}' not available. Using default voice." );
					_synthesizer.SelectVoice ( installedVoices.First ( ).VoiceInfo.Name );  // Use default if not found
				}

				_synthesizer.Rate = ( int ) rate;
				_synthesizer.Volume = volume;
			}
			catch ( Exception ex )
			{
				Logger.Error ( ex, "Failed to update TTS parameters." );
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
				Logger.Error ( ex, "Failed to speak text." );
			}
		}

		public void Stop ( )
		{
			_synthesizer.SpeakAsyncCancelAll ( );
		}
	}
}