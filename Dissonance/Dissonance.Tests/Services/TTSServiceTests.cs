using System.Linq;
using System.Speech.Synthesis;

using Dissonance.Services.MessageService;
using Dissonance.Services.TTSService;
using Dissonance.Tests.TestInfrastructure;

using Microsoft.Extensions.Logging;

namespace Dissonance.Tests.Services
{
        public class TTSServiceTests
        {
                [WindowsFact]
                public void SetTTSParameters_UpdatesSynthesizerState()
                {
                        using var logger = new ListLogger<TTSService>();
                        var messageService = new FakeMessageService();
                        var service = new TTSService(logger, messageService);

                        var synthesizerField = typeof(TTSService).GetField("_synthesizer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        Assert.NotNull(synthesizerField);
                        var synthesizer = (SpeechSynthesizer)synthesizerField!.GetValue(service)!;

                        var voice = synthesizer.GetInstalledVoices().FirstOrDefault();
                        Skip.If(voice == null, "No installed TTS voices available on this system.");

                        service.SetTTSParameters(voice!.VoiceInfo.Name, 2.5, 70);

                        Assert.Equal(2, synthesizer.Rate);
                        Assert.Equal(70, synthesizer.Volume);
                        Assert.Empty(messageService.Errors);
                }

                [WindowsFact]
                public void SetTTSParameters_LogsWarning_WhenVoiceUnavailable()
                {
                        using var logger = new ListLogger<TTSService>();
                        var service = new TTSService(logger, new FakeMessageService());

                        service.SetTTSParameters("VoiceThatDoesNotExist", 1, 60);

                        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("Voice 'VoiceThatDoesNotExist' not available"));
                }

                [WindowsFact]
                public void Speak_NotifiesUser_WhenSpeechFails()
                {
                        var messageService = new FakeMessageService();
                        using var logger = new ListLogger<TTSService>();
                        var service = new TTSService(logger, messageService);

                        var synthesizerField = typeof(TTSService).GetField("_synthesizer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        Assert.NotNull(synthesizerField);
                        var synthesizer = (SpeechSynthesizer)synthesizerField!.GetValue(service)!;
                        synthesizer.Dispose();

                        service.Speak("Hello world");

                        Assert.Single(messageService.Errors);
                        var error = messageService.Errors[0];
                        Assert.Equal("TTS Service Failure", error.Title);
                        Assert.Contains("Failed to speak text", error.Message);
                        Assert.NotNull(error.Exception);
                }
        }
}
