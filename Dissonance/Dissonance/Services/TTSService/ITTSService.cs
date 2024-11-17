namespace Dissonance.Services.TTSService
{
	public interface ITTSService
	{
		/// <summary>
		/// Updates the TTS settings such as voice, rate, and volume.
		/// </summary>
		/// <param name="voice">
		/// The voice to use for TTS.
		/// </param>
		/// <param name="rate">
		/// The speed of speech.
		/// </param>
		/// <param name="volume">
		/// The volume level.
		/// </param>
		void SetTTSParameters ( string voice, double rate, int volume );

		/// <summary>
		/// Speaks the provided text using the current TTS settings.
		/// </summary>
		/// <param name="text">
		/// The text to speak.
		/// </param>
		void Speak ( string text );
	}
}