using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FSChecklist.Features.Main
{
    internal sealed partial class MainForm
    {
        private readonly Color background = Color.FromArgb(13, 20, 29);
        private readonly Color panelColor = Color.FromArgb(23, 35, 49);
        private readonly Color textPrimary = Color.FromArgb(242, 246, 250);
        private readonly Color muted = Color.FromArgb(158, 173, 189);
        private readonly Color primary = Color.FromArgb(47, 129, 247);
        private readonly Color success = Color.FromArgb(88, 214, 155);
        private readonly Color warning = Color.FromArgb(246, 200, 95);
        private readonly Color danger = Color.FromArgb(239, 100, 109);
        private readonly Color borderColor = Color.FromArgb(52, 68, 86);
        private readonly Color currentItemBackground = Color.FromArgb(32, 48, 64);
        private readonly Color disabledButton = Color.FromArgb(59, 70, 84);
        private readonly Color disabledText = Color.FromArgb(125, 136, 150);
        private readonly ToolTip actionToolTip = new ToolTip();

        private readonly ComboBox aircraftBox = new ComboBox();
        private readonly ComboBox checklistBox = new ComboBox();
        private readonly Button startButton = new Button();
        private readonly Button openChecklistsFolderButton = new Button();
        private readonly Button refreshChecklistsButton = new Button();
        private readonly Button settingsButton = new Button();
        private readonly Button forceCheckButton = new Button();
        private readonly Button finishButton = new Button();
        private readonly Label aircraftTitle = new Label();
        private readonly Label checklistTitle = new Label();
        private readonly Label microphoneStatusLabel = new Label();
        private readonly Label progressLabel = new Label();
        private readonly Label stateLabel = new Label();
        private readonly Label checklistNameLabel = new Label();
        private readonly Label challengeLabel = new Label();
        private readonly Label expectedLabel = new Label();
        private readonly Label heardLabel = new Label();
        private readonly Label statusLabel = new Label();
        private readonly Label simulatorStatusLabel = new Label();
        private readonly Label simulatorStatusDescriptionLabel = new Label();
        private readonly FlowLayoutPanel checklistItemsPanel = new FlowLayoutPanel();
        private readonly Font pendingItemFont =
            new Font("Segoe UI", 10.5F, FontStyle.Regular);
        private readonly Font currentItemFont =
            new Font("Segoe UI", 10.5F, FontStyle.Bold);
        private readonly Font completedItemFont =
            new Font("Segoe UI", 10.5F, FontStyle.Strikeout);

        private void BuildInterface()
        {
            Text = "FSChecklist";
            BackColor = background;
            ForeColor = textPrimary;
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(650, 920);
            MinimumSize = new Size(630, 890);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            Icon = LoadAppIcon();

            ConfigureLabel(
                aircraftTitle,
                localizer.Get("Aircraft"),
                9F,
                FontStyle.Regular);
            aircraftTitle.ForeColor = muted;
            aircraftTitle.SetBounds(25, 14, 160, 20);
            aircraftBox.SetBounds(25, 33, 160, 38);
            aircraftBox.DropDownStyle = ComboBoxStyle.DropDownList;
            aircraftBox.BackColor = panelColor;
            aircraftBox.ForeColor = textPrimary;
            aircraftBox.DrawMode = DrawMode.OwnerDrawFixed;
            aircraftBox.ItemHeight = 32;
            aircraftBox.DrawItem += DrawComboBoxItem;

            ConfigureLabel(
            checklistTitle,
                localizer.Get("Checklist"),
                9F,
                FontStyle.Regular);
            checklistTitle.ForeColor = muted;
            checklistTitle.SetBounds(195, 14, 165, 20);
            checklistBox.SetBounds(195, 33, 165, 38);
            checklistBox.DropDownStyle = ComboBoxStyle.DropDownList;
            checklistBox.BackColor = panelColor;
            checklistBox.ForeColor = textPrimary;
            checklistBox.DrawMode = DrawMode.OwnerDrawFixed;
            checklistBox.ItemHeight = 32;
            checklistBox.DrawItem += DrawComboBoxItem;

            ConfigureButton(
                startButton,
                localizer.Format("StartButton", CurrentHotkeyText()),
                primary);
            startButton.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            startButton.SetBounds(460, 33, 115, 38);

            ConfigureButton(openChecklistsFolderButton, string.Empty, disabledButton);
            openChecklistsFolderButton.Name = "OpenChecklistsFolderButton";
            openChecklistsFolderButton.SetBounds(370, 33, 36, 38);
            openChecklistsFolderButton.Paint += DrawFolderIcon;
            actionToolTip.SetToolTip(openChecklistsFolderButton, localizer.Get("OpenChecklistsFolder"));

            ConfigureButton(refreshChecklistsButton, string.Empty, disabledButton);
            refreshChecklistsButton.Name = "RefreshChecklistsButton";
            refreshChecklistsButton.SetBounds(414, 33, 36, 38);
            refreshChecklistsButton.Paint += DrawRefreshIcon;
            actionToolTip.SetToolTip(refreshChecklistsButton, localizer.Get("RefreshChecklists"));

            ConfigureButton(settingsButton, string.Empty, disabledButton);
            settingsButton.Name = "SettingsButton";
            settingsButton.SetBounds(585, 33, 40, 38);
            settingsButton.Paint += DrawSettingsIcon;
            actionToolTip.SetToolTip(
                settingsButton,
                localizer.Get("Settings"));

            var centerPanel = new Panel();
            centerPanel.BackColor = panelColor;
            centerPanel.BorderStyle = BorderStyle.FixedSingle;
            centerPanel.SetBounds(25, 90, 600, 812);
            centerPanel.Anchor =
                AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            simulatorStatusLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            simulatorStatusLabel.TextAlign = ContentAlignment.TopLeft;
            simulatorStatusLabel.SetBounds(20, 14, 355, 24);
            simulatorStatusLabel.Anchor =
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            simulatorStatusDescriptionLabel.ForeColor = muted;
            simulatorStatusDescriptionLabel.Font =
                new Font("Segoe UI", 8.5F, FontStyle.Regular);
            simulatorStatusDescriptionLabel.TextAlign =
                ContentAlignment.TopLeft;
            simulatorStatusDescriptionLabel.SetBounds(20, 35, 555, 34);
            simulatorStatusDescriptionLabel.Anchor =
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            progressLabel.Text = localizer.Get("NoChecklistStarted");
            progressLabel.ForeColor = muted;
            progressLabel.SetBounds(20, 38, 220, 24);
            stateLabel.Text = localizer.Get("Ready");
            stateLabel.ForeColor = success;
            stateLabel.Font = new Font(Font, FontStyle.Bold);
            stateLabel.TextAlign = ContentAlignment.TopRight;
            stateLabel.SetBounds(380, 38, 195, 24);
            stateLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            checklistNameLabel.Text = "CHECKLIST";
            checklistNameLabel.ForeColor = textPrimary;
            checklistNameLabel.BackColor = background;
            checklistNameLabel.BorderStyle = BorderStyle.FixedSingle;
            checklistNameLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            checklistNameLabel.TextAlign = ContentAlignment.MiddleCenter;
            checklistNameLabel.SetBounds(20, 64, 555, 38);
            checklistNameLabel.Visible = true;
            checklistNameLabel.Anchor =
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            challengeLabel.Text = localizer.Get("SelectChecklist");
            challengeLabel.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            challengeLabel.TextAlign = ContentAlignment.MiddleLeft;
            challengeLabel.Visible = false;
            challengeLabel.Anchor =
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            expectedLabel.ForeColor = muted;
            expectedLabel.TextAlign = ContentAlignment.MiddleLeft;
            expectedLabel.TextAlign = ContentAlignment.MiddleCenter;
            expectedLabel.SetBounds(20, 106, 555, 30);
            expectedLabel.Anchor =
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            checklistItemsPanel.BackColor = panelColor;
            checklistItemsPanel.FlowDirection = FlowDirection.TopDown;
            checklistItemsPanel.WrapContents = false;
            checklistItemsPanel.AutoScroll = false;
            checklistItemsPanel.BorderStyle = BorderStyle.FixedSingle;
            checklistItemsPanel.Padding = new Padding(0);
            checklistItemsPanel.SetBounds(20, 140, 555, 568);
            checklistItemsPanel.Anchor =
                AnchorStyles.Top | AnchorStyles.Bottom |
                AnchorStyles.Left | AnchorStyles.Right;

            heardLabel.Text = localizer.Get("WaitingStart");
            heardLabel.ForeColor = muted;
            heardLabel.TextAlign = ContentAlignment.MiddleLeft;
            heardLabel.SetBounds(20, 716, 555, 28);
            heardLabel.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            microphoneStatusLabel.Text = localizer.Get("MicrophoneOff");
            microphoneStatusLabel.BackColor = Color.Transparent;
            microphoneStatusLabel.ForeColor = muted;
            microphoneStatusLabel.Font =
                new Font("Segoe UI", 9F, FontStyle.Bold);
            microphoneStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            microphoneStatusLabel.SetBounds(20, 748, 455, 42);
            microphoneStatusLabel.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            heardLabel.TextChanged += (_, __) => UpdateFooterLayout();
            UpdateFooterLayout();

            centerPanel.Controls.AddRange(new Control[]
            {
                simulatorStatusLabel, simulatorStatusDescriptionLabel,
                progressLabel, stateLabel, checklistNameLabel, challengeLabel,
                expectedLabel, checklistItemsPanel, microphoneStatusLabel, heardLabel
            });

            ConfigureButton(forceCheckButton, string.Empty, primary);
            forceCheckButton.Paint += DrawCheckIcon;
            forceCheckButton.SetBounds(475, 748, 48, 42);
            forceCheckButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            actionToolTip.SetToolTip(
                forceCheckButton,
                localizer.Get("ForceCheckTip"));
            SetCompactActionEnabled(forceCheckButton, false, primary);

            ConfigureButton(finishButton, string.Empty, danger);
            finishButton.Paint += DrawStopIcon;
            finishButton.SetBounds(527, 748, 48, 42);
            finishButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            actionToolTip.SetToolTip(
                finishButton,
                localizer.Get("FinishTip"));
            SetCompactActionEnabled(finishButton, false, danger);

            centerPanel.Controls.Add(forceCheckButton);
            centerPanel.Controls.Add(finishButton);

            UpdateSimulatorStatus();

            Controls.AddRange(new Control[]
            {
                aircraftTitle, aircraftBox, checklistTitle, checklistBox,
                settingsButton, openChecklistsFolderButton,
                refreshChecklistsButton, startButton, centerPanel
            });
        }

        private void DrawComboBoxItem(object sender, DrawItemEventArgs args)
        {
            if (args.Index < 0) return;

            var comboBox = (ComboBox)sender;
            bool selected = (args.State & DrawItemState.Selected) != 0;
            using (var backgroundBrush = new SolidBrush(
                selected ? primary : panelColor))
            using (var textBrush = new SolidBrush(
                textPrimary))
            {
                args.Graphics.FillRectangle(backgroundBrush, args.Bounds);
                args.Graphics.DrawString(
                    comboBox.Items[args.Index].ToString(),
                    comboBox.Font,
                    textBrush,
                    new RectangleF(
                        args.Bounds.X + 6,
                        args.Bounds.Y + 6,
                        args.Bounds.Width - 12,
                        args.Bounds.Height - 8));
            }
            args.DrawFocusRectangle();
        }

        private void SetCompactActionEnabled(
            Button button,
            bool enabled,
            Color activeColor)
        {
            button.Enabled = enabled;
            button.BackColor = enabled ? activeColor : disabledButton;
            button.ForeColor = enabled
                ? textPrimary
                : disabledText;
        }

        private void UpdateFooterLayout()
        {
            bool showHeardMessage =
                !string.IsNullOrWhiteSpace(heardLabel.Text);
            heardLabel.Visible = showHeardMessage;

            int listBottom = showHeardMessage
                ? heardLabel.Top - 8
                : microphoneStatusLabel.Top - 8;
            checklistItemsPanel.Height =
                listBottom - checklistItemsPanel.Top;
        }

        private void UpdateSimulatorStatus()
        {
            bool connected = simulator.IsConnected;
            simulatorStatusLabel.Text = localizer.Get(
                connected ? "SimConnected" : "SimDisconnected");
            simulatorStatusLabel.ForeColor = connected ? success : danger;
            simulatorStatusDescriptionLabel.Text =
                localizer.Get("SimDisconnectedDescription");
            simulatorStatusDescriptionLabel.Visible = !connected;

            int contentOffset = connected ? 0 : 32;
            progressLabel.Top = 38 + contentOffset;
            stateLabel.Top = 38 + contentOffset;
            checklistNameLabel.Top = 64 + contentOffset;
            expectedLabel.Top = 106 + contentOffset;
            checklistItemsPanel.Top = 140 + contentOffset;
            UpdateFooterLayout();
        }

        private void DrawCheckIcon(object sender, PaintEventArgs args)
        {
            var button = (Button)sender;
            Color color = button.Enabled
                ? textPrimary
                : disabledText;
            args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(color, 2.5F))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                float centerX = button.ClientSize.Width / 2F;
                float centerY = button.ClientSize.Height / 2F;
                args.Graphics.DrawLine(
                    pen,
                    centerX - 7,
                    centerY,
                    centerX - 2,
                    centerY + 6);
                args.Graphics.DrawLine(
                    pen,
                    centerX - 2,
                    centerY + 6,
                    centerX + 8,
                    centerY - 7);
            }
        }

        private void DrawStopIcon(object sender, PaintEventArgs args)
        {
            var button = (Button)sender;
            Color color = button.Enabled
                ? textPrimary
                : disabledText;
            const int iconSize = 12;
            int x = (button.ClientSize.Width - iconSize) / 2;
            int y = (button.ClientSize.Height - iconSize) / 2;
            using (var brush = new SolidBrush(color))
                args.Graphics.FillRectangle(brush, x, y, iconSize, iconSize);
        }

        private void DrawSettingsIcon(object sender, PaintEventArgs args)
        {
            var button = (Button)sender;
            float centerX = button.ClientSize.Width / 2F;
            float centerY = button.ClientSize.Height / 2F;

            args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(textPrimary, 2F))
            {
                for (int index = 0; index < 8; index++)
                {
                    double angle = index * Math.PI / 4D;
                    args.Graphics.DrawLine(
                        pen,
                        centerX + (float)(Math.Cos(angle) * 6F),
                        centerY + (float)(Math.Sin(angle) * 6F),
                        centerX + (float)(Math.Cos(angle) * 9F),
                        centerY + (float)(Math.Sin(angle) * 9F));
                }

                args.Graphics.DrawEllipse(
                    pen,
                    centerX - 6F,
                    centerY - 6F,
                    12F,
                    12F);
                args.Graphics.DrawEllipse(
                    pen,
                    centerX - 1.5F,
                    centerY - 1.5F,
                    3F,
                    3F);
            }
        }

        private void DrawFolderIcon(object sender, PaintEventArgs args)
        {
            var button = (Button)sender;
            args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(textPrimary, 2F))
            {
                var bounds = new Rectangle(button.ClientSize.Width / 2 - 10,
                    button.ClientSize.Height / 2 - 6, 20, 14);
                args.Graphics.DrawRectangle(pen, bounds);
                args.Graphics.DrawLines(pen, new[]
                {
                    new Point(bounds.Left + 2, bounds.Top),
                    new Point(bounds.Left + 5, bounds.Top - 4),
                    new Point(bounds.Left + 11, bounds.Top - 4),
                    new Point(bounds.Left + 14, bounds.Top)
                });
            }
        }

        private void DrawRefreshIcon(object sender, PaintEventArgs args)
        {
            var button = (Button)sender;
            args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(textPrimary, 2F))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                var bounds = new Rectangle(button.ClientSize.Width / 2 - 8,
                    button.ClientSize.Height / 2 - 8, 16, 16);
                args.Graphics.DrawArc(pen, bounds, 35, 285);
                args.Graphics.DrawLines(pen, new[]
                {
                    new Point(bounds.Right - 1, bounds.Top + 1),
                    new Point(bounds.Right, bounds.Top + 7),
                    new Point(bounds.Right - 6, bounds.Top + 6)
                });
            }
        }

        private static Icon LoadAppIcon()
        {
            using (System.IO.Stream stream = typeof(MainForm).Assembly
                .GetManifestResourceStream("FSChecklist.Assets.AppIcon.ico"))
            {
                if (stream == null) return null;
                using (var icon = new Icon(stream))
                    return (Icon)icon.Clone();
            }
        }

        private static Label MakeLabel(string text, float size, FontStyle style)
        {
            var label = new Label();
            ConfigureLabel(label, text, size, style);
            return label;
        }

        private static void ConfigureLabel(
            Label label,
            string text,
            float size,
            FontStyle style)
        {
            label.Text = text;
            label.Font = new Font("Segoe UI", size, style);
            label.AutoSize = false;
            label.BackColor = Color.Transparent;
        }

        private void ConfigureButton(Button button, string text, Color color)
        {
            button.Text = text;
            button.BackColor = color;
            button.ForeColor = textPrimary;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }
    }
}
