using System;
using System.IO;
using System.Text.Json;
using FSChecklist.Domain.Settings;
using FSChecklist.Features.Settings;

namespace FSChecklist.Integrations.Settings
{
    internal sealed class JsonAppSettingsRepository : IAppSettingsRepository
    {
        private readonly string filePath;

        public JsonAppSettingsRepository()
        {
            string directory = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "FSChecklist");
            filePath = Path.Combine(directory, "settings.json");
        }

        public AppSettings Load()
        {
            try
            {
                if (!File.Exists(filePath))
                    return new AppSettings();

                AppSettings settings = JsonSerializer.Deserialize<AppSettings>(
                    File.ReadAllText(filePath));
                if (settings == null)
                    return new AppSettings();
                if (settings.Hotkey == null)
                    settings.Hotkey = new HotkeySettings();
                if (string.IsNullOrWhiteSpace(settings.UiLanguage))
                    settings.UiLanguage = "pt-BR";
                return settings;
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            string directory = Path.GetDirectoryName(filePath);
            Directory.CreateDirectory(directory);
            File.WriteAllText(
                filePath,
                JsonSerializer.Serialize(
                    settings,
                    new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
