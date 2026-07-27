using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using FSChecklist.Features.SpeechRecognition;

namespace FSChecklist.Integrations.WindowsSpeech
{
    internal sealed class WindowsSpeechSynthesisService : ISpeechSynthesisService
    {
        private readonly SpeechSynthesizer synthesizer = new SpeechSynthesizer();
        private readonly object sync = new object();
        private readonly Dictionary<Prompt, TaskCompletionSource<bool>> pendingSpeech =
            new Dictionary<Prompt, TaskCompletionSource<bool>>();

        public WindowsSpeechSynthesisService()
        {
            synthesizer.SpeakCompleted += delegate(object sender, SpeakCompletedEventArgs args)
            {
                TaskCompletionSource<bool> completion = null;
                lock (sync)
                {
                    if (args.Prompt != null &&
                        pendingSpeech.TryGetValue(args.Prompt, out completion))
                    {
                        pendingSpeech.Remove(args.Prompt);
                    }
                }
                if (completion != null) completion.TrySetResult(true);
            };
        }

        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            synthesizer.SpeakAsyncCancelAll();
            synthesizer.SpeakAsync(text);
        }

        public Task SpeakAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Task.CompletedTask;

            Cancel();
            var completion = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            Prompt prompt = synthesizer.SpeakAsync(text);
            lock (sync)
            {
                pendingSpeech[prompt] = completion;
            }
            return completion.Task;
        }

        public void Cancel()
        {
            synthesizer.SpeakAsyncCancelAll();
            TaskCompletionSource<bool>[] completions;
            lock (sync)
            {
                completions = pendingSpeech.Values.ToArray();
                pendingSpeech.Clear();
            }
            foreach (TaskCompletionSource<bool> completion in completions)
                completion.TrySetResult(false);
        }

        public void Dispose()
        {
            synthesizer.Dispose();
        }
    }
}
