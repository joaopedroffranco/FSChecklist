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
        private readonly Button backButton = new Button();
        private readonly Button pttButton = new Button();
        private readonly Button repeatButton = new Button();
        private readonly Label progressLabel = new Label();
        private readonly Label stateLabel = new Label();
        private readonly Label challengeLabel = new Label();
        private readonly Label expectedLabel = new Label();
        private readonly Label heardLabel = new Label();
        private readonly Label statusLabel = new Label();

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

            Label title = MakeLabel("FSChecklist", 26F, FontStyle.Bold);
            title.SetBounds(24, 18, 400, 44);
            Label subtitle = MakeLabel(
                "Callouts locais, na ordem exata do seu JSON", 10F, FontStyle.Regular);
            subtitle.ForeColor = muted;
            subtitle.SetBounds(26, 61, 500, 25);

            Label aircraftTitle = MakeLabel("Aeronave", 9F, FontStyle.Regular);
            aircraftTitle.ForeColor = muted;
            aircraftTitle.SetBounds(25, 102, 200, 20);
            aircraftBox.SetBounds(25, 123, 265, 34);
            aircraftBox.DropDownStyle = ComboBoxStyle.DropDownList;

            Label checklistTitle = MakeLabel("Checklist", 9F, FontStyle.Regular);
            checklistTitle.ForeColor = muted;
            checklistTitle.SetBounds(305, 102, 200, 20);
            checklistBox.SetBounds(305, 123, 265, 34);
            checklistBox.DropDownStyle = ComboBoxStyle.DropDownList;

            ConfigureButton(startButton, "INICIAR", primary);
            startButton.SetBounds(585, 121, 150, 38);

            var centerPanel = new Panel();
            centerPanel.BackColor = panelColor;
            centerPanel.SetBounds(25, 181, 710, 280);
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

            challengeLabel.Text = "Selecione uma aeronave e uma checklist";
            challengeLabel.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            challengeLabel.TextAlign = ContentAlignment.MiddleCenter;
            challengeLabel.SetBounds(30, 68, 650, 90);
            challengeLabel.Anchor =
                AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            expectedLabel.ForeColor = muted;
            expectedLabel.TextAlign = ContentAlignment.MiddleCenter;
            expectedLabel.SetBounds(30, 163, 650, 45);
            expectedLabel.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            heardLabel.Text = "Aguardando inicio";
            heardLabel.ForeColor = Color.FromArgb(169, 183, 200);
            heardLabel.TextAlign = ContentAlignment.MiddleCenter;
            heardLabel.SetBounds(30, 226, 650, 35);
            heardLabel.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            centerPanel.Controls.AddRange(new Control[]
            {
                progressLabel, stateLabel, challengeLabel, expectedLabel, heardLabel
            });

            ConfigureButton(backButton, "< VOLTAR", Color.FromArgb(52, 66, 86));
            backButton.SetBounds(25, 480, 155, 45);
            backButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            ConfigureButton(pttButton, "SEGURE PARA FALAR - F9 GLOBAL", primary);
            pttButton.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            pttButton.SetBounds(190, 480, 380, 45);
            pttButton.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            ConfigureButton(repeatButton, "REPETIR", Color.FromArgb(52, 66, 86));
            repeatButton.SetBounds(580, 480, 155, 45);
            repeatButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            statusLabel.Text = "Carregando checklists...";
            statusLabel.ForeColor = muted;
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.SetBounds(25, 530, 710, 45);
            statusLabel.Anchor =
                AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            Controls.AddRange(new Control[]
            {
                title, subtitle, aircraftTitle, aircraftBox, checklistTitle, checklistBox,
                startButton, centerPanel, backButton, pttButton, repeatButton, statusLabel
            });
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
