using System;
using System.Threading.Tasks;

namespace FSChecklist.Features.SpeechRecognition
{
    internal interface ISpeechSynthesisService : IDisposable
    {
        void Speak(string text);
        Task SpeakAsync(string text);
        void Cancel();
    }
}
