using System;
using System.Collections.Generic;
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

    internal enum SpeechListeningState
    {
        Idle,
        Listening,
        SoundDetected,
        Processing
    }

    internal sealed class SpeechListeningStateChangedEventArgs : EventArgs
    {
        public SpeechListeningState State { get; private set; }

        public SpeechListeningStateChangedEventArgs(SpeechListeningState state)
        {
            State = state;
        }
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
        event EventHandler<SpeechRecognizedEventArgs> SpeechHypothesized;
        event EventHandler<SpeechListeningStateChangedEventArgs>
            ListeningStateChanged;
        event EventHandler RecognitionCompleted;

        void SetAcceptedResponses(IReadOnlyList<string> responses);
        Task InitializeAsync();
        Task StartAsync();
        Task StopAsync();
        Task<SpeechRecognizedEventArgs> RecognizeOnceAsync();
        Task CancelAsync();
    }
}
