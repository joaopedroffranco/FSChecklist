using System;
using System.Threading.Tasks;

namespace FSChecklist.Features.SpeechRecognition
{
    internal enum RecognitionConfidence
    {
        High,
        Medium,
        Low,
        Rejected
    }

    internal sealed class SpeechRecognizedEventArgs : EventArgs
    {
        public string Text { get; private set; }
        public RecognitionConfidence Confidence { get; private set; }

        public SpeechRecognizedEventArgs(string text, RecognitionConfidence confidence)
        {
            Text = text ?? string.Empty;
            Confidence = confidence;
        }
    }

    internal interface ISpeechRecognitionService : IDisposable
    {
        bool IsReady { get; }
        string Status { get; }

        event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;
        event EventHandler RecognitionCompleted;

        Task InitializeAsync();
        Task StartAsync();
        Task StopAsync();
    }
}
