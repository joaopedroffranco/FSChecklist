using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FSChecklist.Features.SpeechRecognition;
using Windows.Devices.Enumeration;
using Windows.Globalization;
using Windows.Media.Devices;
using Windows.Media.SpeechRecognition;

namespace FSChecklist.Integrations.WindowsSpeech
{
    internal sealed class WindowsSpeechRecognitionService : ISpeechRecognitionService
    {
        private readonly string languageTag;
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private SpeechRecognizer recognizer;
        private bool started;

        public bool IsReady { get; private set; }
        public string Status { get; private set; } = "Reconhecimento: inicializando...";

        public event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;
        public event EventHandler<SpeechListeningStateChangedEventArgs>
            ListeningStateChanged;
        public event EventHandler RecognitionCompleted;

        public WindowsSpeechRecognitionService(string languageTag)
        {
            this.languageTag = languageTag;
        }

        public async Task InitializeAsync()
        {
            try
            {
                Language language = SpeechRecognizer.SupportedTopicLanguages.FirstOrDefault(
                    item => string.Equals(item.LanguageTag, languageTag,
                        StringComparison.OrdinalIgnoreCase));
                if (language == null)
                    throw new InvalidOperationException(
                        "O Windows nao oferece reconhecimento para " + languageTag + ".");

                recognizer = new SpeechRecognizer(language);
                recognizer.Timeouts.EndSilenceTimeout =
                    TimeSpan.FromMilliseconds(500);
                recognizer.Constraints.Add(new SpeechRecognitionTopicConstraint(
                    SpeechRecognitionScenario.Dictation, "pilot-response"));

                SpeechRecognitionCompilationResult compilation =
                    await recognizer.CompileConstraintsAsync();
                if (compilation.Status != SpeechRecognitionResultStatus.Success)
                    throw new InvalidOperationException(
                        "Falha ao preparar reconhecimento: " + compilation.Status);

                recognizer.ContinuousRecognitionSession.ResultGenerated += OnResultGenerated;
                recognizer.ContinuousRecognitionSession.Completed += OnCompleted;
                recognizer.StateChanged += OnStateChanged;

                IsReady = true;
                string microphoneName = await GetDefaultMicrophoneNameAsync();
                Status = "Reconhecimento " + languageTag +
                         " pronto. Microfone: " + microphoneName + ".";
            }
            catch (Exception error)
            {
                IsReady = false;
                Status = "Voz indisponivel: " + error.GetBaseException().Message;
                if (recognizer != null)
                {
                    recognizer.Dispose();
                    recognizer = null;
                }
            }
        }

        public async Task StartAsync()
        {
            if (!IsReady || recognizer == null)
                throw new InvalidOperationException(Status);

            await gate.WaitAsync();
            try
            {
                if (started) return;
                await recognizer.ContinuousRecognitionSession.StartAsync();
                started = true;
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task StopAsync()
        {
            if (recognizer == null) return;

            await gate.WaitAsync();
            try
            {
                if (!started) return;
                await recognizer.ContinuousRecognitionSession.StopAsync();
                started = false;
            }
            finally
            {
                gate.Release();
            }
        }

        private void OnResultGenerated(
            SpeechContinuousRecognitionSession sender,
            SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            EventHandler<SpeechRecognizedEventArgs> handler = SpeechRecognized;
            if (handler == null) return;

            handler(this, new SpeechRecognizedEventArgs(
                args.Result.Text, MapConfidence(args.Result.Confidence)));
        }

        private void OnCompleted(
            SpeechContinuousRecognitionSession sender,
            SpeechContinuousRecognitionCompletedEventArgs args)
        {
            started = false;
            EventHandler handler = RecognitionCompleted;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void OnStateChanged(
            SpeechRecognizer sender,
            SpeechRecognizerStateChangedEventArgs args)
        {
            EventHandler<SpeechListeningStateChangedEventArgs> handler =
                ListeningStateChanged;
            if (handler == null) return;

            SpeechListeningState state;
            switch (args.State)
            {
                case SpeechRecognizerState.Capturing:
                    state = SpeechListeningState.Listening;
                    break;
                case SpeechRecognizerState.SoundStarted:
                    state = SpeechListeningState.SoundDetected;
                    break;
                case SpeechRecognizerState.Processing:
                case SpeechRecognizerState.SoundEnded:
                    state = SpeechListeningState.Processing;
                    break;
                default:
                    state = SpeechListeningState.Idle;
                    break;
            }
            handler(this, new SpeechListeningStateChangedEventArgs(state));
        }

        private static async Task<string> GetDefaultMicrophoneNameAsync()
        {
            try
            {
                string deviceId = MediaDevice.GetDefaultAudioCaptureId(
                    AudioDeviceRole.Default);
                if (string.IsNullOrWhiteSpace(deviceId))
                    return "nenhum dispositivo padrao";

                DeviceInformation device =
                    await DeviceInformation.CreateFromIdAsync(deviceId);
                return string.IsNullOrWhiteSpace(device.Name)
                    ? "dispositivo padrao"
                    : device.Name;
            }
            catch
            {
                return "dispositivo padrao";
            }
        }

        private static RecognitionConfidence MapConfidence(
            SpeechRecognitionConfidence confidence)
        {
            switch (confidence)
            {
                case SpeechRecognitionConfidence.High:
                    return RecognitionConfidence.High;
                case SpeechRecognitionConfidence.Medium:
                    return RecognitionConfidence.Medium;
                case SpeechRecognitionConfidence.Low:
                    return RecognitionConfidence.Low;
                default:
                    return RecognitionConfidence.Rejected;
            }
        }

        public void Dispose()
        {
            if (recognizer != null)
            {
                recognizer.ContinuousRecognitionSession.ResultGenerated -= OnResultGenerated;
                recognizer.ContinuousRecognitionSession.Completed -= OnCompleted;
                recognizer.StateChanged -= OnStateChanged;
                recognizer.Dispose();
                recognizer = null;
            }
            gate.Dispose();
        }
    }
}
