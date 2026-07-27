using System.Speech.Synthesis;
using FSChecklist.Features.SpeechRecognition;

namespace FSChecklist.Integrations.WindowsSpeech
{
    internal sealed class WindowsSpeechSynthesisService : ISpeechSynthesisService
    {
        private readonly SpeechSynthesizer synthesizer = new SpeechSynthesizer();

        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            synthesizer.SpeakAsyncCancelAll();
            synthesizer.SpeakAsync(text);
        }

        public void Cancel()
        {
            synthesizer.SpeakAsyncCancelAll();
        }

        public void Dispose()
        {
            synthesizer.Dispose();
        }
    }
}
