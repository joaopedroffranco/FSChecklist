using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FSChecklist.Domain.Settings;
using FSChecklist.Features.Input;

namespace FSChecklist.Integrations.WindowsInput
{
    internal sealed class WindowsGlobalPushToTalk : IGlobalPushToTalk
    {
        private const int WhKeyboardLl = 13;
        private const int WmKeyDown = 0x0100;
        private const int WmKeyUp = 0x0101;
        private const int WmSysKeyDown = 0x0104;
        private const int WmSysKeyUp = 0x0105;

        private readonly HookProcedure procedure;
        private IntPtr hook;
        private bool hotkeyDown;
        private HotkeySettings hotkey;

        public event Action<bool> StateChanged;
        public HotkeySettings Hotkey
        {
            get { return hotkey.Clone(); }
        }

        public WindowsGlobalPushToTalk(HotkeySettings hotkey)
        {
            this.hotkey = hotkey == null
                ? new HotkeySettings()
                : hotkey.Clone();
            procedure = HookCallback;
            hook = SetWindowsHookEx(WhKeyboardLl, procedure, GetModuleHandle(null), 0);
            if (hook == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        private IntPtr HookCallback(int code, IntPtr message, IntPtr data)
        {
            if (code >= 0)
            {
                int value = message.ToInt32();
                bool isDown = value == WmKeyDown || value == WmSysKeyDown;
                bool isUp = value == WmKeyUp || value == WmSysKeyUp;
                if (isDown || isUp)
                {
                    KeyboardData keyboardData =
                        Marshal.PtrToStructure<KeyboardData>(data);
                    Keys key = (Keys)keyboardData.VirtualKeyCode;
                    if (key == (Keys)hotkey.KeyCode)
                    {
                        if (isDown &&
                            !hotkeyDown &&
                            ModifiersMatch())
                        {
                            hotkeyDown = true;
                            RaiseStateChanged(true);
                        }
                        else if (isUp && hotkeyDown)
                        {
                            hotkeyDown = false;
                            RaiseStateChanged(false);
                        }
                    }
                }
            }
            return CallNextHookEx(hook, code, message, data);
        }

        public void UpdateHotkey(HotkeySettings hotkey)
        {
            this.hotkey = hotkey == null
                ? new HotkeySettings()
                : hotkey.Clone();
            hotkeyDown = false;
        }

        private bool ModifiersMatch()
        {
            return IsPressed(Keys.ControlKey) == hotkey.Control &&
                   IsPressed(Keys.Menu) == hotkey.Alt &&
                   IsPressed(Keys.ShiftKey) == hotkey.Shift;
        }

        private static bool IsPressed(Keys key)
        {
            return (GetAsyncKeyState((int)key) & 0x8000) != 0;
        }

        private void RaiseStateChanged(bool isDown)
        {
            Action<bool> handler = StateChanged;
            if (handler != null) handler(isDown);
        }

        public void Dispose()
        {
            if (hook == IntPtr.Zero) return;
            UnhookWindowsHookEx(hook);
            hook = IntPtr.Zero;
        }

        private delegate IntPtr HookProcedure(int code, IntPtr message, IntPtr data);

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardData
        {
            public uint VirtualKeyCode;
            public uint ScanCode;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int hookId, HookProcedure callback, IntPtr module, uint threadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(
            IntPtr hook, int code, IntPtr message, IntPtr data);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string moduleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKey);
    }
}
