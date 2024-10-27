using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
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
			// Set default parameters if needed, can be updated via SetTTSParameters
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
				_synthesizer.SelectVoice ( voice );
				_synthesizer.Rate = ( int ) rate;
				_synthesizer.Volume = volume;
				Logger.Info ( $"TTS parameters updated: Voice = {voice}, Rate = {rate}, Volume = {volume}" );
			}
			catch ( Exception ex )
			{
				Logger.Error ( ex, "Failed to update TTS parameters." );
			}
		}
	}
}
