using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FSChecklist.Features.Main
{
    internal sealed partial class MainForm
    {
        private readonly Color background = Color.FromArgb(16, 20, 27);
        private readonly Color panelColor = Color.FromArgb(24, 32, 43);
        private readonly Color muted = Color.FromArgb(158, 171, 188);
        private readonly Color primary = Color.FromArgb(35, 116, 225);
        private readonly Color danger = Color.FromArgb(216, 75, 85);
        private readonly Color success = Color.FromArgb(94, 211, 155);
        private readonly Color warning = Color.FromArgb(255, 202, 88);
        private readonly Color disabledButton = Color.FromArgb(55, 64, 76);
        private readonly ToolTip actionToolTip = new ToolTip();

        private readonly ComboBox aircraftBox = new ComboBox();
        private readonly ComboBox checklistBox = new ComboBox();
        private readonly Button startButton = new Button();
        private readonly Button forceCheckButton = new Button();
        private readonly Button finishButton = new Button();
        private readonly Label microphoneStatusLabel = new Label();
        private readonly PictureBox logoPictureBox = new PictureBox();
        private readonly Label progressLabel = new Label();
        private readonly Label stateLabel = new Label();
        private readonly Label checklistNameLabel = new Label();
        private readonly Label challengeLabel = new Label();
        private readonly Label expectedLabel = new Label();
        private readonly Label heardLabel = new Label();
        private readonly Label statusLabel = new Label();
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
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(650, 920);
            MinimumSize = new Size(630, 890);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            Icon = LoadAppIcon();

            logoPictureBox.Image = LoadLogo();
            logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            logoPictureBox.SetBounds(25, 16, 48, 48);

            Label title = MakeLabel("FSChecklist", 23F, FontStyle.Bold);
            title.SetBounds(86, 20, 350, 42);

            Label aircraftTitle = MakeLabel("Aeronave", 9F, FontStyle.Regular);
            aircraftTitle.ForeColor = muted;
            aircraftTitle.SetBounds(25, 82, 200, 20);
            aircraftBox.SetBounds(25, 101, 180, 38);
            aircraftBox.DropDownStyle = ComboBoxStyle.DropDownList;
            aircraftBox.DrawMode = DrawMode.OwnerDrawFixed;
            aircraftBox.ItemHeight = 32;
            aircraftBox.DrawItem += DrawComboBoxItem;

            Label checklistTitle = MakeLabel("Checklist", 9F, FontStyle.Regular);
            checklistTitle.ForeColor = muted;
            checklistTitle.SetBounds(215, 82, 200, 20);
            checklistBox.SetBounds(215, 101, 205, 38);
            checklistBox.DropDownStyle = ComboBoxStyle.DropDownList;
            checklistBox.DrawMode = DrawMode.OwnerDrawFixed;
            checklistBox.ItemHeight = 32;
            checklistBox.DrawItem += DrawComboBoxItem;

            ConfigureButton(startButton, "INICIAR OU F9", primary);
            startButton.SetBounds(430, 101, 195, 38);

            var centerPanel = new Panel();
            centerPanel.BackColor = panelColor;
            centerPanel.BorderStyle = BorderStyle.FixedSingle;
            centerPanel.SetBounds(25, 158, 600, 690);
            centerPanel.Anchor =
                AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            progressLabel.Text = "Nenhuma checklist iniciada";
            progressLabel.ForeColor = muted;
            progressLabel.SetBounds(20, 14, 220, 24);
            stateLabel.Text = "PRONTO";
            stateLabel.ForeColor = success;
            stateLabel.Font = new Font(Font, FontStyle.Bold);
            stateLabel.TextAlign = ContentAlignment.TopRight;
            stateLabel.SetBounds(380, 14, 195, 24);
            stateLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            checklistNameLabel.Text = "CHECKLIST";
            checklistNameLabel.ForeColor = Color.White;
            checklistNameLabel.BackColor = Color.FromArgb(20, 26, 35);
            checklistNameLabel.BorderStyle = BorderStyle.FixedSingle;
            checklistNameLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            checklistNameLabel.TextAlign = ContentAlignment.MiddleCenter;
            checklistNameLabel.SetBounds(20, 40, 555, 38);
            checklistNameLabel.Visible = true;
            checklistNameLabel.Anchor =
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            challengeLabel.Text = "Selecione uma aeronave e uma checklist";
            challengeLabel.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            challengeLabel.TextAlign = ContentAlignment.MiddleLeft;
            challengeLabel.Visible = false;
            challengeLabel.Anchor =
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            expectedLabel.ForeColor = muted;
            expectedLabel.TextAlign = ContentAlignment.MiddleLeft;
            expectedLabel.TextAlign = ContentAlignment.MiddleCenter;
            expectedLabel.SetBounds(20, 82, 555, 30);
            expectedLabel.Anchor =
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            checklistItemsPanel.BackColor = panelColor;
            checklistItemsPanel.FlowDirection = FlowDirection.TopDown;
            checklistItemsPanel.WrapContents = false;
            checklistItemsPanel.AutoScroll = false;
            checklistItemsPanel.BorderStyle = BorderStyle.FixedSingle;
            checklistItemsPanel.Padding = new Padding(0);
            checklistItemsPanel.SetBounds(20, 116, 555, 405);
            checklistItemsPanel.Anchor =
                AnchorStyles.Top | AnchorStyles.Bottom |
                AnchorStyles.Left | AnchorStyles.Right;

            heardLabel.Text = "Aguardando inicio";
            heardLabel.ForeColor = Color.FromArgb(169, 183, 200);
            heardLabel.TextAlign = ContentAlignment.MiddleLeft;
            heardLabel.SetBounds(20, 560, 455, 32);
            heardLabel.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            microphoneStatusLabel.Text = "Microfone desligado";
            microphoneStatusLabel.BackColor = Color.Transparent;
            microphoneStatusLabel.ForeColor = muted;
            microphoneStatusLabel.Font =
                new Font("Segoe UI", 9F, FontStyle.Bold);
            microphoneStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            microphoneStatusLabel.SetBounds(20, 528, 555, 28);
            microphoneStatusLabel.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            centerPanel.Controls.AddRange(new Control[]
            {
                progressLabel, stateLabel, checklistNameLabel, challengeLabel,
                expectedLabel, checklistItemsPanel, microphoneStatusLabel, heardLabel
            });

            ConfigureButton(forceCheckButton, string.Empty, primary);
            forceCheckButton.Paint += DrawCheckIcon;
            forceCheckButton.SetBounds(475, 560, 48, 42);
            forceCheckButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            actionToolTip.SetToolTip(
                forceCheckButton,
                "Confirmar manualmente o item atual");
            SetCompactActionEnabled(forceCheckButton, false, primary);

            ConfigureButton(finishButton, string.Empty, danger);
            finishButton.Paint += DrawStopIcon;
            finishButton.SetBounds(527, 560, 48, 42);
            finishButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            actionToolTip.SetToolTip(
                finishButton,
                "Encerrar a checklist atual");
            SetCompactActionEnabled(finishButton, false, danger);

            centerPanel.Controls.Add(forceCheckButton);
            centerPanel.Controls.Add(finishButton);

            statusLabel.Text = "Carregando checklists...";
            statusLabel.ForeColor = muted;
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.SetBounds(25, 860, 600, 42);
            statusLabel.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            Controls.AddRange(new Control[]
            {
                logoPictureBox, title, aircraftTitle, aircraftBox, checklistTitle, checklistBox,
                startButton, centerPanel, statusLabel
            });
        }

        private void DrawComboBoxItem(object sender, DrawItemEventArgs args)
        {
            if (args.Index < 0) return;

            var comboBox = (ComboBox)sender;
            bool selected = (args.State & DrawItemState.Selected) != 0;
            using (var backgroundBrush = new SolidBrush(
                selected ? primary : Color.White))
            using (var textBrush = new SolidBrush(
                selected ? Color.White : Color.FromArgb(30, 35, 42)))
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
                ? Color.White
                : Color.FromArgb(125, 136, 150);
        }

        private void DrawCheckIcon(object sender, PaintEventArgs args)
        {
            var button = (Button)sender;
            Color color = button.Enabled
                ? Color.White
                : Color.FromArgb(125, 136, 150);
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
                ? Color.White
                : Color.FromArgb(125, 136, 150);
            const int iconSize = 12;
            int x = (button.ClientSize.Width - iconSize) / 2;
            int y = (button.ClientSize.Height - iconSize) / 2;
            using (var brush = new SolidBrush(color))
                args.Graphics.FillRectangle(brush, x, y, iconSize, iconSize);
        }

        private static Image LoadLogo()
        {
            using (System.IO.Stream stream = typeof(MainForm).Assembly
                .GetManifestResourceStream("FSChecklist.Assets.Logo.png"))
            {
                if (stream == null) return null;
                using (Image image = Image.FromStream(stream))
                    return new Bitmap(image);
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
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", size, style),
                AutoSize = false,
                BackColor = Color.Transparent
            };
        }

        private static void ConfigureButton(Button button, string text, Color color)
        {
            button.Text = text;
            button.BackColor = color;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }
    }
}
