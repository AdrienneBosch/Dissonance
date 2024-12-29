namespace Dissonance.Services.TTSService
{
	public interface ITTSService
	{
		void SetTTSParameters ( string voice, double rate, int volume );

		void Speak ( string text );

		void Stop ( );
	}
}