using System.Drawing;
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
            new Font("Segoe UI", 9.5F, FontStyle.Regular);
        private readonly Font currentItemFont =
            new Font("Segoe UI", 9.5F, FontStyle.Bold);
        private readonly Font completedItemFont =
            new Font("Segoe UI", 9.5F, FontStyle.Strikeout);

        private void BuildInterface()
        {
            Text = "FSChecklist";
            BackColor = background;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(760, 590);
            MinimumSize = new Size(700, 570);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            Icon = LoadAppIcon();

            logoPictureBox.Image = LoadLogo();
            logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            logoPictureBox.SetBounds(24, 16, 54, 54);

            Label title = MakeLabel("FSChecklist", 26F, FontStyle.Bold);
            title.SetBounds(90, 21, 400, 44);

            Label aircraftTitle = MakeLabel("Aeronave", 9F, FontStyle.Regular);
            aircraftTitle.ForeColor = muted;
            aircraftTitle.SetBounds(25, 82, 200, 20);
            aircraftBox.SetBounds(25, 103, 265, 34);
            aircraftBox.DropDownStyle = ComboBoxStyle.DropDownList;

            Label checklistTitle = MakeLabel("Checklist", 9F, FontStyle.Regular);
            checklistTitle.ForeColor = muted;
            checklistTitle.SetBounds(305, 82, 200, 20);
            checklistBox.SetBounds(305, 103, 265, 34);
            checklistBox.DropDownStyle = ComboBoxStyle.DropDownList;

            ConfigureButton(startButton, "INICIAR", primary);
            startButton.SetBounds(585, 101, 150, 38);

            var centerPanel = new Panel();
            centerPanel.BackColor = panelColor;
            centerPanel.SetBounds(25, 161, 710, 300);
            centerPanel.Anchor =
                AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            progressLabel.Text = "Nenhuma checklist iniciada";
            progressLabel.ForeColor = muted;
            progressLabel.SetBounds(20, 18, 400, 24);
            stateLabel.Text = "PRONTO";
            stateLabel.ForeColor = success;
            stateLabel.Font = new Font(Font, FontStyle.Bold);
            stateLabel.TextAlign = ContentAlignment.TopRight;
            stateLabel.SetBounds(480, 18, 205, 24);
            stateLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            checklistNameLabel.Text = "CHECKLIST";
            checklistNameLabel.ForeColor = primary;
            checklistNameLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            checklistNameLabel.SetBounds(20, 45, 665, 20);
            checklistNameLabel.Anchor =
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            challengeLabel.Text = "Selecione uma aeronave e uma checklist";
            challengeLabel.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            challengeLabel.TextAlign = ContentAlignment.MiddleLeft;
            challengeLabel.SetBounds(20, 66, 665, 46);
            challengeLabel.Anchor =
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            expectedLabel.ForeColor = muted;
            expectedLabel.TextAlign = ContentAlignment.MiddleLeft;
            expectedLabel.SetBounds(20, 108, 665, 28);
            expectedLabel.Anchor =
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            checklistItemsPanel.BackColor = panelColor;
            checklistItemsPanel.FlowDirection = FlowDirection.TopDown;
            checklistItemsPanel.WrapContents = false;
            checklistItemsPanel.AutoScroll = true;
            checklistItemsPanel.Padding = new Padding(0);
            checklistItemsPanel.SetBounds(20, 142, 665, 110);
            checklistItemsPanel.Anchor =
                AnchorStyles.Top | AnchorStyles.Bottom |
                AnchorStyles.Left | AnchorStyles.Right;

            heardLabel.Text = "Aguardando inicio";
            heardLabel.ForeColor = Color.FromArgb(169, 183, 200);
            heardLabel.TextAlign = ContentAlignment.MiddleLeft;
            heardLabel.SetBounds(20, 258, 665, 28);
            heardLabel.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            centerPanel.Controls.AddRange(new Control[]
            {
                progressLabel, stateLabel, checklistNameLabel, challengeLabel,
                expectedLabel, checklistItemsPanel, heardLabel
            });

            microphoneStatusLabel.Text = "MICROFONE: DESLIGADO - F9 INICIA";
            microphoneStatusLabel.BackColor = Color.FromArgb(31, 43, 58);
            microphoneStatusLabel.ForeColor = muted;
            microphoneStatusLabel.Font =
                new Font("Segoe UI", 9.5F, FontStyle.Bold);
            microphoneStatusLabel.TextAlign = ContentAlignment.MiddleCenter;
            microphoneStatusLabel.SetBounds(25, 480, 415, 45);
            microphoneStatusLabel.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            ConfigureButton(forceCheckButton, "FORCAR CHECK", primary);
            forceCheckButton.SetBounds(450, 480, 130, 45);
            forceCheckButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            forceCheckButton.Enabled = false;

            ConfigureButton(finishButton, "TERMINAR", danger);
            finishButton.SetBounds(590, 480, 145, 45);
            finishButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            finishButton.Enabled = false;

            statusLabel.Text = "Carregando checklists...";
            statusLabel.ForeColor = muted;
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.SetBounds(25, 530, 710, 45);
            statusLabel.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            Controls.AddRange(new Control[]
            {
                logoPictureBox, title, aircraftTitle, aircraftBox, checklistTitle, checklistBox,
                startButton, centerPanel, microphoneStatusLabel, forceCheckButton,
                finishButton, statusLabel
            });
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
