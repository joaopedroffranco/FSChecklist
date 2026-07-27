using System;

namespace FSChecklist.Features.Simulator
{
    internal interface ISimulatorConnection : IDisposable
    {
        bool IsConnected { get; }
        string Status { get; }
        event Action StatusChanged;
        void Start();
    }
}
