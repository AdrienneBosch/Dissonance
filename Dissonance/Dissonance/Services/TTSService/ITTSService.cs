using System;
using System.Speech.Synthesis;

namespace Dissonance.Services.TTSService
{
        public interface ITTSService
        {
                void SetTTSParameters ( string voice, double rate, int volume );

                Prompt? Speak ( string text );

                void Pause ( );

                void Resume ( );

                void Stop ( );

                event EventHandler<SpeakCompletedEventArgs> SpeechCompleted;

                event EventHandler<SpeakProgressEventArgs> SpeechProgress;
        }
}
