using System;

namespace FSChecklist.Features.SpeechRecognition
{
    internal interface ISpeechSynthesisService : IDisposable
    {
        void Speak(string text);
        void Cancel();
    }
}
