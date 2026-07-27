using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace FSChecklist
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class ChecklistRules
    {
        public bool acceptAnyAnswer { get; set; }
    }

    internal sealed class ChecklistDefinition
    {
        public string id { get; set; }
        public string name { get; set; }
        public string next { get; set; }
        public string completedCallout { get; set; }
        public List<object> items { get; set; }
    }

    internal sealed class ChecklistDocument
    {
        public string aircraft { get; set; }
        public string language { get; set; }
        public ChecklistRules rules { get; set; }
        public List<ChecklistDefinition> checklists { get; set; }
    }

    internal sealed class ChecklistItem
    {
        public string Callout { get; private set; }
        public List<string> Responses { get; private set; }

        public ChecklistItem(string callout, IEnumerable<string> responses)
        {
            Callout = callout ?? string.Empty;
            Responses = responses == null ? new List<string>() : responses.ToList();
        }

        public static ChecklistItem FromJson(object value)
        {
            string text = value as string;
            if (text != null)
                return new ChecklistItem(text, null);

            Dictionary<string, object> data = value as Dictionary<string, object>;
            if (data == null)
                return new ChecklistItem(Convert.ToString(value, CultureInfo.InvariantCulture), null);

            object calloutValue;
            string callout = data.TryGetValue("callout", out calloutValue)
                ? Convert.ToString(calloutValue, CultureInfo.InvariantCulture)
                : string.Empty;

            var responses = new List<string>();
            object responseValue;
            object[] responseArray = data.TryGetValue("responses", out responseValue)
                ? responseValue as object[]
                : null;
            if (responseArray != null)
                responses.AddRange(responseArray.Select(Convert.ToString));

            return new ChecklistItem(callout, responses);
        }
    }

    internal sealed class MainForm : Form
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

        private readonly List<ChecklistDocument> documents = new List<ChecklistDocument>();
        private readonly SpeechSynthesizer synthesizer = new SpeechSynthesizer();
        private SpeechRecognitionEngine recognizer;
        private ChecklistDocument activeDocument;
        private ChecklistDefinition activeChecklist;
        private int itemIndex = -1;
        private bool listening;

        public MainForm()
        {
            Text = "FSChecklist";
            BackColor = background;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(760, 590);
            MinimumSize = new Size(700, 570);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;

            BuildInterface();
            WireEvents();
            InitializeSpeech();
            LoadChecklists();
        }

        private void BuildInterface()
        {
            var title = MakeLabel("FSChecklist", 26F, FontStyle.Bold);
            title.SetBounds(24, 18, 400, 44);
            var subtitle = MakeLabel("Callouts locais, na ordem exata do seu JSON", 10F, FontStyle.Regular);
            subtitle.ForeColor = muted;
            subtitle.SetBounds(26, 61, 500, 25);

            var aircraftTitle = MakeLabel("Aeronave", 9F, FontStyle.Regular);
            aircraftTitle.ForeColor = muted;
            aircraftTitle.SetBounds(25, 102, 200, 20);
            aircraftBox.SetBounds(25, 123, 265, 34);
            aircraftBox.DropDownStyle = ComboBoxStyle.DropDownList;

            var checklistTitle = MakeLabel("Checklist", 9F, FontStyle.Regular);
            checklistTitle.ForeColor = muted;
            checklistTitle.SetBounds(305, 102, 200, 20);
            checklistBox.SetBounds(305, 123, 265, 34);
            checklistBox.DropDownStyle = ComboBoxStyle.DropDownList;

            ConfigureButton(startButton, "INICIAR", primary);
            startButton.SetBounds(585, 121, 150, 38);

            var centerPanel = new Panel();
            centerPanel.BackColor = panelColor;
            centerPanel.SetBounds(25, 181, 710, 280);
            centerPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

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
            challengeLabel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            expectedLabel.ForeColor = muted;
            expectedLabel.TextAlign = ContentAlignment.MiddleCenter;
            expectedLabel.SetBounds(30, 163, 650, 45);
            expectedLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            heardLabel.Text = "Aguardando inicio";
            heardLabel.ForeColor = Color.FromArgb(169, 183, 200);
            heardLabel.TextAlign = ContentAlignment.MiddleCenter;
            heardLabel.SetBounds(30, 226, 650, 35);
            heardLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            centerPanel.Controls.AddRange(new Control[]
            {
                progressLabel, stateLabel, challengeLabel, expectedLabel, heardLabel
            });

            ConfigureButton(backButton, "< VOLTAR", Color.FromArgb(52, 66, 86));
            backButton.SetBounds(25, 480, 155, 45);
            backButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            ConfigureButton(pttButton, "SEGURE PARA FALAR - F9", primary);
            pttButton.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            pttButton.SetBounds(190, 480, 380, 45);
            pttButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            ConfigureButton(repeatButton, "REPETIR", Color.FromArgb(52, 66, 86));
            repeatButton.SetBounds(580, 480, 155, 45);
            repeatButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            statusLabel.Text = "Carregando checklists...";
            statusLabel.ForeColor = muted;
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.SetBounds(25, 538, 710, 30);
            statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            Controls.AddRange(new Control[]
            {
                title, subtitle, aircraftTitle, aircraftBox, checklistTitle, checklistBox,
                startButton, centerPanel, backButton, pttButton, repeatButton, statusLabel
            });
        }

        private Label MakeLabel(string text, float size, FontStyle style)
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

        private void WireEvents()
        {
            aircraftBox.SelectedIndexChanged += AircraftChanged;
            startButton.Click += delegate { StartSelectedChecklist(); };
            backButton.Click += delegate
            {
                if (activeChecklist != null && itemIndex > 0)
                {
                    itemIndex--;
                    ShowCurrentItem();
                }
            };
            repeatButton.Click += delegate
            {
                ChecklistItem item = CurrentItem();
                if (item != null) Speak(item.Callout);
            };
            pttButton.MouseDown += delegate(object sender, MouseEventArgs args)
            {
                if (args.Button == MouseButtons.Left) StartListening();
            };
            pttButton.MouseUp += delegate(object sender, MouseEventArgs args)
            {
                if (args.Button == MouseButtons.Left) StopListening();
            };
            KeyDown += delegate(object sender, KeyEventArgs args)
            {
                if (args.KeyCode == Keys.F9 && !args.Handled)
                {
                    StartListening();
                    args.Handled = true;
                }
            };
            KeyUp += delegate(object sender, KeyEventArgs args)
            {
                if (args.KeyCode == Keys.F9)
                {
                    StopListening();
                    args.Handled = true;
                }
            };
            FormClosed += delegate
            {
                if (recognizer != null)
                {
                    try { recognizer.RecognizeAsyncCancel(); } catch { }
                    recognizer.Dispose();
                }
                synthesizer.Dispose();
            };
        }

        private void InitializeSpeech()
        {
            try
            {
                try
                {
                    recognizer = new SpeechRecognitionEngine(new CultureInfo("pt-BR"));
                }
                catch
                {
                    recognizer = new SpeechRecognitionEngine();
                    statusLabel.Text = "pt-BR nao instalado; usando o reconhecedor padrao.";
                }

                recognizer.LoadGrammar(new DictationGrammar());
                recognizer.SetInputToDefaultAudioDevice();
                recognizer.SpeechRecognized += Recognized;
                recognizer.RecognizeCompleted += delegate
                {
                    if (IsDisposed) return;
                    BeginInvoke((Action)ResetPtt);
                };
            }
            catch (Exception error)
            {
                if (recognizer != null) recognizer.Dispose();
                recognizer = null;
                pttButton.Enabled = false;
                statusLabel.Text = "Microfone indisponivel: " + error.Message;
            }
        }

        private void LoadChecklists()
        {
            string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "checklists");
            if (!Directory.Exists(directory))
            {
                statusLabel.Text = "Pasta de checklists nao encontrada: " + directory;
                return;
            }

            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            foreach (string file in Directory.GetFiles(directory, "*.json"))
            {
                try
                {
                    ChecklistDocument document =
                        serializer.Deserialize<ChecklistDocument>(File.ReadAllText(file, Encoding.UTF8));
                    if (document == null || string.IsNullOrWhiteSpace(document.aircraft) ||
                        document.checklists == null)
                        throw new InvalidDataException("Campos obrigatorios: aircraft e checklists.");
                    documents.Add(document);
                }
                catch (Exception error)
                {
                    statusLabel.Text = "JSON invalido em " + Path.GetFileName(file) + ": " + error.Message;
                }
            }

            foreach (string aircraft in documents.Select(d => d.aircraft).Distinct().OrderBy(x => x))
                aircraftBox.Items.Add(aircraft);

            if (aircraftBox.Items.Count > 0)
            {
                aircraftBox.SelectedIndex = 0;
                statusLabel.Text = documents.Count + " arquivo(s) carregado(s). Tudo funciona localmente.";
            }
            else
            {
                statusLabel.Text = "Nenhuma checklist encontrada em " + directory;
            }
        }

        private void AircraftChanged(object sender, EventArgs args)
        {
            checklistBox.Items.Clear();
            ChecklistDocument document = documents.FirstOrDefault(
                d => d.aircraft == Convert.ToString(aircraftBox.SelectedItem));
            if (document == null) return;
            foreach (ChecklistDefinition checklist in document.checklists)
                checklistBox.Items.Add(checklist.name);
            if (checklistBox.Items.Count > 0) checklistBox.SelectedIndex = 0;
        }

        private void StartSelectedChecklist()
        {
            activeDocument = documents.FirstOrDefault(
                d => d.aircraft == Convert.ToString(aircraftBox.SelectedItem));
            if (activeDocument == null) return;
            activeChecklist = activeDocument.checklists.FirstOrDefault(
                c => c.name == Convert.ToString(checklistBox.SelectedItem));
            if (activeChecklist == null) return;
            itemIndex = 0;
            ShowCurrentItem();
        }

        private ChecklistItem CurrentItem()
        {
            if (activeChecklist == null || activeChecklist.items == null ||
                itemIndex < 0 || itemIndex >= activeChecklist.items.Count)
                return null;
            return ChecklistItem.FromJson(activeChecklist.items[itemIndex]);
        }

        private void ShowCurrentItem()
        {
            if (activeChecklist == null) return;
            int count = activeChecklist.items == null ? 0 : activeChecklist.items.Count;
            if (itemIndex >= count)
            {
                challengeLabel.Text = activeChecklist.name + " completa";
                expectedLabel.Text = "Todos os itens foram confirmados.";
                progressLabel.Text = count + " de " + count;
                SetState("COMPLETA", success);
                heardLabel.Text = "Checklist completa";
                Speak(string.IsNullOrWhiteSpace(activeChecklist.completedCallout)
                    ? activeChecklist.name + " checklist complete"
                    : activeChecklist.completedCallout);
                return;
            }

            ChecklistItem item = CurrentItem();
            challengeLabel.Text = item.Callout;
            bool acceptsAny = activeDocument != null && activeDocument.rules != null &&
                              activeDocument.rules.acceptAnyAnswer;
            expectedLabel.Text = acceptsAny || item.Responses.Count == 0
                ? "Confirmacao por voz: qualquer resposta reconhecida"
                : "Resposta esperada: " + string.Join(" / ", item.Responses);
            progressLabel.Text = "Item " + (itemIndex + 1) + " de " + count;
            SetState("PENDENTE", warning);
            heardLabel.Text = "Segure o botao ou F9 para responder";
            Speak(item.Callout);
        }

        private void StartListening()
        {
            if (listening || recognizer == null || activeChecklist == null) return;
            synthesizer.SpeakAsyncCancelAll();
            listening = true;
            pttButton.BackColor = danger;
            pttButton.Text = "OUVINDO... SOLTE PARA ENVIAR";
            SetState("OUVINDO", Color.White);
            try
            {
                recognizer.RecognizeAsync(RecognizeMode.Single);
            }
            catch (Exception error)
            {
                ResetPtt();
                statusLabel.Text = "Microfone indisponivel: " + error.Message;
            }
        }

        private void StopListening()
        {
            if (!listening || recognizer == null) return;
            listening = false;
            try { recognizer.RecognizeAsyncStop(); } catch { }
            ResetPtt();
        }

        private void ResetPtt()
        {
            listening = false;
            pttButton.BackColor = primary;
            pttButton.Text = "SEGURE PARA FALAR - F9";
        }

        private void Recognized(object sender, SpeechRecognizedEventArgs args)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => Recognized(sender, args)));
                return;
            }
            if (args.Result.Confidence < 0.45F)
            {
                heardLabel.Text = "Fala incerta: " + args.Result.Text + " - tente novamente";
                return;
            }

            ChecklistItem item = CurrentItem();
            if (item == null) return;
            string heard = NormalizeSpeech(args.Result.Text);
            bool acceptsAny = activeDocument != null && activeDocument.rules != null &&
                              activeDocument.rules.acceptAnyAnswer;
            bool matched = acceptsAny && heard.Length > 0;
            if (!matched)
            {
                foreach (string response in item.Responses)
                {
                    string answer = NormalizeSpeech(response);
                    if (heard == answer ||
                        Regex.IsMatch(heard, "(^| )" + Regex.Escape(answer) + "( |$)"))
                    {
                        matched = true;
                        break;
                    }
                }
            }

            heardLabel.Text = "Ouvido: " + args.Result.Text;
            if (!matched)
            {
                SetState("NAO CONFIRMADO", danger);
                statusLabel.Text = "A resposta nao coincide com o JSON. O item permanece pendente.";
                Speak("Nao confirmado");
                return;
            }

            StopListening();
            SetState("CONFIRMADO", success);
            itemIndex++;
            var timer = new System.Windows.Forms.Timer { Interval = 550 };
            timer.Tick += delegate
            {
                timer.Stop();
                timer.Dispose();
                ShowCurrentItem();
            };
            timer.Start();
        }

        private void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            synthesizer.SpeakAsyncCancelAll();
            synthesizer.SpeakAsync(text);
        }

        private void SetState(string text, Color color)
        {
            stateLabel.Text = text;
            stateLabel.ForeColor = color;
        }

        private static string NormalizeSpeech(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            string decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (char character in decomposed)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category != UnicodeCategory.NonSpacingMark)
                    builder.Append(character);
            }
            return Regex.Replace(builder.ToString(), "[^a-z0-9]+", " ").Trim();
        }
    }
}
