using System.Drawing;
using System.Windows.Forms;
using FSChecklist.Domain.Settings;
using FSChecklist.Features.Input;
using FSChecklist.Features.Localization;

namespace FSChecklist.Features.Settings
{
    internal sealed class HotkeyCaptureForm : Form
    {
        private readonly IAppLocalizer localizer;
        private readonly Label shortcutLabel = new Label();
        private readonly Label validationLabel = new Label();
        private readonly Button applyButton = new Button();
        private HotkeySettings captured;

        public HotkeySettings CapturedHotkey
        {
            get { return captured == null ? null : captured.Clone(); }
        }

        public HotkeyCaptureForm(
            IAppLocalizer localizer,
            HotkeySettings current)
        {
            this.localizer = localizer;
            captured = current == null
                ? new HotkeySettings()
                : current.Clone();

            Text = localizer.Get("CaptureShortcutTitle");
            ClientSize = new Size(390, 220);
            MinimumSize = MaximumSize = Size;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            KeyPreview = true;
            BackColor = Color.FromArgb(16, 20, 27);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10F);

            var instruction = new Label
            {
                Text = localizer.Get("CaptureShortcutInstruction"),
                ForeColor = Color.FromArgb(158, 171, 188),
                TextAlign = ContentAlignment.MiddleCenter
            };
            instruction.SetBounds(25, 20, 340, 35);

            shortcutLabel.Text = HotkeyFormatter.Format(captured);
            shortcutLabel.BackColor = Color.FromArgb(24, 32, 43);
            shortcutLabel.BorderStyle = BorderStyle.FixedSingle;
            shortcutLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            shortcutLabel.TextAlign = ContentAlignment.MiddleCenter;
            shortcutLabel.SetBounds(25, 62, 340, 55);

            validationLabel.ForeColor = Color.FromArgb(216, 75, 85);
            validationLabel.TextAlign = ContentAlignment.MiddleCenter;
            validationLabel.SetBounds(25, 120, 340, 24);

            ConfigureButton(
                applyButton,
                localizer.Get("UseShortcut"),
                Color.FromArgb(35, 116, 225));
            applyButton.SetBounds(190, 158, 175, 40);
            applyButton.Click += delegate
            {
                DialogResult = DialogResult.OK;
                Close();
            };

            var cancelButton = new Button();
            ConfigureButton(
                cancelButton,
                localizer.Get("Cancel"),
                Color.FromArgb(55, 64, 76));
            cancelButton.SetBounds(25, 158, 150, 40);
            cancelButton.Click += delegate
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.AddRange(new Control[]
            {
                instruction, shortcutLabel, validationLabel,
                cancelButton, applyButton
            });

            KeyDown += CaptureKeyDown;
        }

        private void CaptureKeyDown(object sender, KeyEventArgs args)
        {
            if (args.KeyCode == Keys.ControlKey ||
                args.KeyCode == Keys.ShiftKey ||
                args.KeyCode == Keys.Menu)
            {
                validationLabel.Text = localizer.Get("InvalidShortcut");
                applyButton.Enabled = false;
                return;
            }

            captured = new HotkeySettings
            {
                KeyCode = (int)args.KeyCode,
                Control = args.Control,
                Alt = args.Alt,
                Shift = args.Shift
            };
            shortcutLabel.Text = HotkeyFormatter.Format(captured);
            validationLabel.Text = string.Empty;
            applyButton.Enabled = true;
            args.Handled = true;
            args.SuppressKeyPress = true;
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
    }
}
