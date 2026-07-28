using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using FSChecklist.Domain.Flight;
using FSChecklist.Features.Simulator;
using FSChecklist.Features.SpeechRecognition;

namespace FSChecklist.Features.FlightCallouts
{
    internal sealed class FlightCalloutService : IFlightCalloutService
    {
        private readonly ISimulatorConnection simulator;
        private readonly ISpeechSynthesisService speech;
        private readonly FlightCalloutEngine engine =
            new FlightCalloutEngine();
        private readonly BlockingCollection<FlightCallout> queue =
            new BlockingCollection<FlightCallout>();
        private readonly CancellationTokenSource cancellation =
            new CancellationTokenSource();
        private Task speechWorker;
        private bool started;
        private bool disposed;

        public FlightCalloutService(
            ISimulatorConnection simulator,
            ISpeechSynthesisService speech)
        {
            this.simulator = simulator;
            this.speech = speech;
        }

        public void Start()
        {
            if (started || disposed) return;
            started = true;
            simulator.TelemetryReceived += OnTelemetryReceived;
            speechWorker = Task.Run(ProcessSpeechQueueAsync);
        }

        private void OnTelemetryReceived(FlightTelemetry telemetry)
        {
            if (disposed || queue.IsAddingCompleted) return;
            foreach (FlightCallout callout in engine.Process(telemetry))
            {
                try
                {
                    queue.Add(callout);
                }
                catch (InvalidOperationException)
                {
                    return;
                }
            }
        }

        private async Task ProcessSpeechQueueAsync()
        {
            try
            {
                foreach (FlightCallout callout in queue.GetConsumingEnumerable(
                    cancellation.Token))
                {
                    await speech.SpeakAsync(callout.SpokenText);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            simulator.TelemetryReceived -= OnTelemetryReceived;
            queue.CompleteAdding();
            cancellation.Cancel();
            speech.Cancel();
            if (speechWorker != null)
            {
                try
                {
                    speechWorker.Wait(TimeSpan.FromSeconds(1));
                }
                catch (AggregateException)
                {
                }
            }
            speech.Dispose();
            cancellation.Dispose();
            queue.Dispose();
        }
    }
}
