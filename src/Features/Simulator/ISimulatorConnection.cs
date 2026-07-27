using System;

namespace FSChecklist.Features.Simulator
{
    internal interface ISimulatorConnection : IDisposable
    {
        bool IsConnected { get; }
        event Action StatusChanged;
        void Start();
    }
}
