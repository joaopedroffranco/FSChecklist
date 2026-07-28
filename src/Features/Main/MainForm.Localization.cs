using FSChecklist.Features.Input;

namespace FSChecklist.Features.Main
{
    internal sealed partial class MainForm
    {
        private void ApplyLocalization()
        {
            aircraftTitle.Text = localizer.Get("Aircraft");
            checklistTitle.Text = localizer.Get("Checklist");
            startButton.Text = localizer.Format(
                "StartButton",
                CurrentHotkeyText());
            actionToolTip.SetToolTip(
                settingsButton,
                localizer.Get("Settings"));
            actionToolTip.SetToolTip(
                forceCheckButton,
                localizer.Get("ForceCheckTip"));
            actionToolTip.SetToolTip(
                finishButton,
                localizer.Get("FinishTip"));
            UpdateSimulatorStatus();

            if (!checklistRunning)
                UpdateReadyChecklist();
        }

        private string CurrentHotkeyText()
        {
            return HotkeyFormatter.Format(settings.Hotkey);
        }

        private void UpdateHotkeyStatus()
        {
            string shortcut = CurrentHotkeyText();
            hotkeyStatus = globalPushToTalk == null
                ? localizer.Format(
                    "HotkeyUnavailable",
                    shortcut,
                    hotkeyError)
                : localizer.Format("HotkeyActive", shortcut);
        }
    }
}
