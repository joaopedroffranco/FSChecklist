using System.Collections.Generic;
using System.Windows.Forms;
using FSChecklist.Domain.Settings;

namespace FSChecklist.Features.Input
{
    internal static class HotkeyFormatter
    {
        public static string Format(HotkeySettings hotkey)
        {
            if (hotkey == null) hotkey = new HotkeySettings();
            var parts = new List<string>();
            if (hotkey.Control) parts.Add("Ctrl");
            if (hotkey.Alt) parts.Add("Alt");
            if (hotkey.Shift) parts.Add("Shift");
            parts.Add(((Keys)hotkey.KeyCode).ToString());
            return string.Join("+", parts);
        }

        public static bool Matches(
            HotkeySettings hotkey,
            Keys keyCode,
            Keys modifiers)
        {
            if (hotkey == null) return keyCode == Keys.F9;
            return keyCode == (Keys)hotkey.KeyCode &&
                   hotkey.Control == modifiers.HasFlag(Keys.Control) &&
                   hotkey.Alt == modifiers.HasFlag(Keys.Alt) &&
                   hotkey.Shift == modifiers.HasFlag(Keys.Shift);
        }
    }
}
