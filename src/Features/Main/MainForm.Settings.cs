using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using FSChecklist.Domain.Settings;
using FSChecklist.Features.Settings;

namespace FSChecklist.Features.Main
{
    internal sealed partial class MainForm
    {
        private async Task OpenSettingsAsync()
        {
            if (checklistRunning) return;

            try
            {
                AppSettings updated;
                using (var form = new SettingsForm(
                    localizer,
                    audioInput,
                    settings))
                {
                    if (form.ShowDialog(this) != DialogResult.OK) return;
                    updated = form.ResultSettings;
                }

                bool languageChanged = !string.Equals(
                    settings.UiLanguage,
                    updated.UiLanguage,
                    StringComparison.OrdinalIgnoreCase);
                bool microphoneSelected = !string.IsNullOrWhiteSpace(
                    updated.MicrophoneDeviceId);

                if (microphoneSelected)
                    await audioInput.SetDefaultDeviceAsync(
                        updated.MicrophoneDeviceId);

                settings = updated;
                settingsRepository.Save(settings);
                localizer.SetLanguage(settings.UiLanguage);

                if (globalPushToTalk != null)
                    globalPushToTalk.UpdateHotkey(settings.Hotkey);

                ApplyLocalization();
                UpdateHotkeyStatus();

                if (microphoneSelected || languageChanged)
                {
                    speechStatus = localizer.Get("SpeechInitializing");
                    RefreshStatus();
                    await speechRecognition.InitializeAsync();
                }

                speechStatus = speechRecognition.Status;
                checklistStatus = localizer.Get("SettingsSaved");
                SetRunControls(true);
                UpdateReadyChecklist();
                RefreshStatus();
            }
            catch (Exception error)
            {
                checklistStatus = localizer.Format(
                    "SettingsFailure",
                    error.GetBaseException().Message);
                RefreshStatus();
                ShowError(checklistStatus);
            }
        }
    }
}
