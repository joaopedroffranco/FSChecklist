using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FSChecklist.Domain.Settings;
using FSChecklist.Features.AudioInput;
using FSChecklist.Features.Errors;
using FSChecklist.Features.Input;
using FSChecklist.Features.Localization;

namespace FSChecklist.Features.Settings
{
    internal sealed class SettingsForm : Form
    {
        private readonly IAppLocalizer localizer;
        private readonly IAudioInputDeviceService audioInput;
        private readonly AppSettings workingSettings;
        private readonly ComboBox languageBox = new ComboBox();
        private readonly ComboBox microphoneBox = new ComboBox();
        private readonly Label shortcutValueLabel = new Label();
        private readonly Button saveButton = new Button();

        public AppSettings ResultSettings
        {
            get { return workingSettings.Clone(); }
        }

        public SettingsForm(
            IAppLocalizer localizer,
            IAudioInputDeviceService audioInput,
            AppSettings settings)
        {
            this.localizer = localizer;
            this.audioInput = audioInput;
            workingSettings = settings.Clone();
            BuildInterface();
            Shown += async delegate { await LoadMicrophonesAsync(); };
        }

        private void BuildInterface()
        {
            Text = localizer.Get("SettingsTitle");
            ClientSize = new Size(500, 410);
            MinimumSize = MaximumSize = Size;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(16, 20, 27);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10F);

            Label title = MakeLabel(
                localizer.Get("SettingsTitle"),
                20F,
                FontStyle.Bold);
            title.SetBounds(25, 20, 440, 42);

            Label languageTitle = MakeLabel(
                localizer.Get("InterfaceLanguage"),
                9F,
                FontStyle.Regular);
            languageTitle.ForeColor = Color.FromArgb(158, 171, 188);
            languageTitle.SetBounds(25, 75, 440, 22);

            languageBox.DropDownStyle = ComboBoxStyle.DropDownList;
            languageBox.SetBounds(25, 99, 450, 34);
            languageBox.Items.Add(new LanguageOption(
                "pt-BR", localizer.Get("Portuguese")));
            languageBox.Items.Add(new LanguageOption(
                "en-US", localizer.Get("English")));
            LanguageOption selectedLanguage = languageBox.Items
                .Cast<LanguageOption>()
                .FirstOrDefault(option =>
                    option.Code == workingSettings.UiLanguage)
                ?? languageBox.Items.Cast<LanguageOption>().First();
            languageBox.SelectedItem = selectedLanguage;

            Label microphoneTitle = MakeLabel(
                localizer.Get("InputMicrophone"),
                9F,
                FontStyle.Regular);
            microphoneTitle.ForeColor = Color.FromArgb(158, 171, 188);
            microphoneTitle.SetBounds(25, 150, 440, 22);

            microphoneBox.DropDownStyle = ComboBoxStyle.DropDownList;
            microphoneBox.SetBounds(25, 174, 450, 34);
            microphoneBox.Items.Add(localizer.Get("LoadingMicrophones"));
            microphoneBox.SelectedIndex = 0;
            microphoneBox.Enabled = false;

            Label microphoneNote = MakeLabel(
                localizer.Get("DefaultMicrophoneNote"),
                8.5F,
                FontStyle.Regular);
            microphoneNote.ForeColor = Color.FromArgb(158, 171, 188);
            microphoneNote.SetBounds(25, 211, 450, 38);

            Label shortcutTitle = MakeLabel(
                localizer.Get("Shortcut"),
                9F,
                FontStyle.Regular);
            shortcutTitle.ForeColor = Color.FromArgb(158, 171, 188);
            shortcutTitle.SetBounds(25, 260, 440, 22);

            shortcutValueLabel.Text =
                HotkeyFormatter.Format(workingSettings.Hotkey);
            shortcutValueLabel.BackColor = Color.FromArgb(24, 32, 43);
            shortcutValueLabel.BorderStyle = BorderStyle.FixedSingle;
            shortcutValueLabel.Font =
                new Font("Segoe UI", 11F, FontStyle.Bold);
            shortcutValueLabel.TextAlign = ContentAlignment.MiddleCenter;
            shortcutValueLabel.SetBounds(25, 284, 245, 40);

            var changeShortcutButton = new Button();
            changeShortcutButton.Name = "ChangeShortcutButton";
            ConfigureButton(
                changeShortcutButton,
                localizer.Get("ChangeShortcut"),
                Color.FromArgb(55, 64, 76));
            changeShortcutButton.SetBounds(280, 284, 195, 40);
            changeShortcutButton.Click += ChangeShortcut;

            var cancelButton = new Button();
            ConfigureButton(
                cancelButton,
                localizer.Get("Cancel"),
                Color.FromArgb(55, 64, 76));
            cancelButton.SetBounds(25, 350, 180, 40);
            cancelButton.Click += delegate
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            ConfigureButton(
                saveButton,
                localizer.Get("Save"),
                Color.FromArgb(35, 116, 225));
            saveButton.SetBounds(295, 350, 180, 40);
            saveButton.Click += SaveSettings;

            Controls.AddRange(new Control[]
            {
                title, languageTitle, languageBox,
                microphoneTitle, microphoneBox, microphoneNote,
                shortcutTitle, shortcutValueLabel, changeShortcutButton,
                cancelButton, saveButton
            });
        }

        private async Task LoadMicrophonesAsync()
        {
            try
            {
                IReadOnlyList<AudioInputDevice> devices =
                    await audioInput.GetDevicesAsync();
                microphoneBox.Items.Clear();
                foreach (AudioInputDevice device in devices)
                {
                    microphoneBox.Items.Add(new MicrophoneOption(
                        device,
                        device.IsDefault
                            ? localizer.Format("DefaultDevice", device.Name)
                            : device.Name));
                }

                MicrophoneOption selected = microphoneBox.Items
                    .Cast<MicrophoneOption>()
                    .FirstOrDefault(option =>
                        option.Device.Id == workingSettings.MicrophoneDeviceId)
                    ?? microphoneBox.Items
                        .Cast<MicrophoneOption>()
                        .FirstOrDefault(option => option.Device.IsDefault)
                    ?? microphoneBox.Items
                        .Cast<MicrophoneOption>()
                        .FirstOrDefault();

                microphoneBox.SelectedItem = selected;
                microphoneBox.Enabled = selected != null;
            }
            catch (Exception error)
            {
                microphoneBox.Items.Clear();
                microphoneBox.Items.Add(error.GetBaseException().Message);
                microphoneBox.SelectedIndex = 0;
                microphoneBox.Enabled = false;
                ErrorDialog.Show(
                    this,
                    error.GetBaseException().Message,
                    localizer);
            }
        }

        private void ChangeShortcut(object sender, EventArgs args)
        {
            using (var capture = new HotkeyCaptureForm(
                localizer,
                workingSettings.Hotkey))
            {
                if (capture.ShowDialog(this) != DialogResult.OK) return;
                workingSettings.Hotkey = capture.CapturedHotkey;
                shortcutValueLabel.Text =
                    HotkeyFormatter.Format(workingSettings.Hotkey);
            }
        }

        private void SaveSettings(object sender, EventArgs args)
        {
            LanguageOption language = languageBox.SelectedItem as LanguageOption;
            if (language != null) workingSettings.UiLanguage = language.Code;

            MicrophoneOption microphone =
                microphoneBox.SelectedItem as MicrophoneOption;
            if (microphone != null)
                workingSettings.MicrophoneDeviceId = microphone.Device.Id;

            DialogResult = DialogResult.OK;
            Close();
        }

        private static Label MakeLabel(
            string text,
            float size,
            FontStyle style)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", size, style),
                AutoSize = false,
                BackColor = Color.Transparent
            };
        }

        private static void ConfigureButton(
            Button button,
            string text,
            Color color)
        {
            button.Text = text;
            button.BackColor = color;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

        private sealed class LanguageOption
        {
            public string Code { get; private set; }
            private string Name { get; set; }

            public LanguageOption(string code, string name)
            {
                Code = code;
                Name = name;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        private sealed class MicrophoneOption
        {
            public AudioInputDevice Device { get; private set; }
            private string Name { get; set; }

            public MicrophoneOption(AudioInputDevice device, string name)
            {
                Device = device;
                Name = name;
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
