using System;

namespace FSChecklist.Features.Input
{
    internal interface IGlobalPushToTalk : IDisposable
    {
        event Action<bool> StateChanged;
    }
}
