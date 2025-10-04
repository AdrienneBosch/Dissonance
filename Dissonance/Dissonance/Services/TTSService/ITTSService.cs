using System;
using System.Speech.Synthesis;

namespace Dissonance.Services.TTSService
{
        public interface ITTSService
        {
                void SetTTSParameters ( string voice, double rate, int volume );

                Prompt? Speak ( string text );

                void Stop ( );

                event EventHandler<SpeakCompletedEventArgs> SpeechCompleted;
        }
}
