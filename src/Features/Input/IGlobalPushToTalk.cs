using System;
using FSChecklist.Domain.Settings;

namespace FSChecklist.Features.Input
{
    internal interface IGlobalPushToTalk : IDisposable
    {
        event Action<bool> StateChanged;
        HotkeySettings Hotkey { get; }
        void UpdateHotkey(HotkeySettings hotkey);
    }
}
