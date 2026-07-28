using System;
using FSChecklist.Domain.Flight;

namespace FSChecklist.Features.Simulator
{
    internal interface ISimulatorConnection : IDisposable
    {
        bool IsConnected { get; }
        event Action StatusChanged;
        event Action<FlightTelemetry> TelemetryReceived;
        void Start();
    }
}
