namespace FSChecklist.Domain.Settings
{
    internal sealed class AppSettings
    {
        public string UiLanguage { get; set; } = "pt-BR";
        public string MicrophoneDeviceId { get; set; } = string.Empty;
        public HotkeySettings Hotkey { get; set; } = new HotkeySettings();

        public AppSettings Clone()
        {
            return new AppSettings
            {
                UiLanguage = UiLanguage,
                MicrophoneDeviceId = MicrophoneDeviceId,
                Hotkey = Hotkey == null
                    ? new HotkeySettings()
                    : Hotkey.Clone()
            };
        }
    }

    internal sealed class HotkeySettings
    {
        public int KeyCode { get; set; } = 120;
        public bool Control { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }

        public HotkeySettings Clone()
        {
            return new HotkeySettings
            {
                KeyCode = KeyCode,
                Control = Control,
                Alt = Alt,
                Shift = Shift
            };
        }
    }
}
