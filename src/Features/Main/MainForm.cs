using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;
using FSChecklist.Domain.Checklists;
using FSChecklist.Features.Checklist;
using FSChecklist.Features.Input;
using FSChecklist.Features.Repository;
using FSChecklist.Features.SpeechRecognition;

namespace FSChecklist.Features.Main
{
    internal sealed partial class MainForm : Form
    {
        private readonly IChecklistRepository repository;
        private readonly ISpeechRecognitionService speechRecognition;
        private readonly ISpeechSynthesisService speechSynthesis;
        private readonly IGlobalPushToTalk globalPushToTalk;
        private readonly ChecklistSession session = new ChecklistSession();
        private readonly List<ChecklistDocument> documents = new List<ChecklistDocument>();
        private readonly Timer listeningAnimationTimer = new Timer { Interval = 350 };

        private bool listening;
        private bool responseHandled;
        private bool recognitionResultReceived;
        private int listeningAnimationStep;
        private string checklistStatus = string.Empty;
        private string speechStatus = "Reconhecimento: inicializando...";
        private string hotkeyStatus;

        public MainForm(
            IChecklistRepository repository,
            ISpeechRecognitionService speechRecognition,
            ISpeechSynthesisService speechSynthesis,
            IGlobalPushToTalk globalPushToTalk,
            string hotkeyError)
        {
            this.repository = repository;
            this.speechRecognition = speechRecognition;
            this.speechSynthesis = speechSynthesis;
            this.globalPushToTalk = globalPushToTalk;
            hotkeyStatus = globalPushToTalk == null
                ? "F9 global indisponivel: " + hotkeyError
                : "F9 global ativo.";

            BuildInterface();
            WireEvents();
            LoadChecklists();

            pttButton.Enabled = false;
            Shown += async delegate
            {
                await speechRecognition.InitializeAsync();
                speechStatus = speechRecognition.Status;
                pttButton.Enabled = speechRecognition.IsReady;
                if (!speechRecognition.IsReady)
                    pttButton.Text = "RECONHECIMENTO DE VOZ INDISPONIVEL";
                RefreshStatus();
            };
        }

        private void WireEvents()
        {
            aircraftBox.SelectedIndexChanged += AircraftChanged;
            startButton.Click += delegate { StartSelectedChecklist(); };
            repeatButton.Click += delegate
            {
                ChecklistItem item = session.CurrentItem;
                if (item != null) speechSynthesis.Speak(item.Callout);
            };
            pttButton.MouseDown += delegate(object sender, MouseEventArgs args)
            {
                if (args.Button == MouseButtons.Left) StartListening();
            };
            pttButton.MouseUp += delegate(object sender, MouseEventArgs args)
            {
                if (args.Button == MouseButtons.Left) StopListening();
            };
            listeningAnimationTimer.Tick += delegate { AnimateListening(); };

            speechRecognition.SpeechRecognized += OnSpeechRecognized;
            speechRecognition.RecognitionCompleted += delegate
            {
                RunOnUi(delegate
                {
                    ResetPtt();
                    if (!recognitionResultReceived &&
                        session.IsActive &&
                        !session.IsComplete)
                    {
                        heardLabel.Text =
                            "Nenhuma fala reconhecida. Segure F9, fale e solte.";
                    }
                });
            };

            if (globalPushToTalk != null)
            {
                globalPushToTalk.StateChanged += delegate(bool isDown)
                {
                    RunOnUi(isDown ? (Action)StartListening : StopListening);
                };
            }
            else
            {
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
                    if (args.KeyCode == Keys.F9) StopListening();
                };
            }

            FormClosed += delegate
            {
                if (globalPushToTalk != null) globalPushToTalk.Dispose();
                listeningAnimationTimer.Dispose();
                pendingItemFont.Dispose();
                currentItemFont.Dispose();
                completedItemFont.Dispose();
                speechRecognition.Dispose();
                speechSynthesis.Dispose();
            };
        }

        private void LoadChecklists()
        {
            try
            {
                string directory =
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "checklists");
                documents.AddRange(repository.LoadAll(directory));

                foreach (string aircraft in documents
                    .Select(document => document.aircraft)
                    .Distinct()
                    .OrderBy(value => value))
                {
                    aircraftBox.Items.Add(aircraft);
                }

                if (aircraftBox.Items.Count > 0)
                {
                    aircraftBox.SelectedIndex = 0;
                    checklistStatus = string.Empty;
                }
                else
                {
                    checklistStatus = "Nenhuma checklist encontrada.";
                }
            }
            catch (Exception error)
            {
                checklistStatus = error.Message;
            }
            RefreshStatus();
        }

        private void AircraftChanged(object sender, EventArgs args)
        {
            checklistBox.Items.Clear();
            ChecklistDocument document = documents.FirstOrDefault(
                item => item.aircraft == Convert.ToString(aircraftBox.SelectedItem));
            if (document == null) return;

            foreach (ChecklistDefinition checklist in document.checklists)
                checklistBox.Items.Add(checklist.name);
            if (checklistBox.Items.Count > 0) checklistBox.SelectedIndex = 0;
        }

        private void StartSelectedChecklist()
        {
            ChecklistDocument document = documents.FirstOrDefault(
                item => item.aircraft == Convert.ToString(aircraftBox.SelectedItem));
            if (document == null) return;

            ChecklistDefinition checklist = document.checklists.FirstOrDefault(
                item => item.name == Convert.ToString(checklistBox.SelectedItem));
            if (checklist == null) return;

            session.Start(document, checklist);
            ShowCurrentItem();
        }

        private void ShowCurrentItem()
        {
            if (!session.IsActive) return;
            if (session.IsComplete)
            {
                checklistNameLabel.Text = session.Checklist.name.ToUpperInvariant();
                challengeLabel.Text = session.Checklist.name + " completa";
                expectedLabel.Text = "Todos os itens foram confirmados.";
                progressLabel.Text = session.ItemCount + " de " + session.ItemCount;
                SetState("COMPLETA", success);
                heardLabel.Text = "Checklist completa";
                RefreshChecklistItems();
                speechSynthesis.Speak(string.IsNullOrWhiteSpace(
                    session.Checklist.completedCallout)
                    ? session.Checklist.name + " checklist complete"
                    : session.Checklist.completedCallout);
                return;
            }

            ChecklistItem item = session.CurrentItem;
            checklistNameLabel.Text = session.Checklist.name.ToUpperInvariant();
            challengeLabel.Text = item.Callout;
            bool acceptsAny = session.Document.rules != null &&
                              session.Document.rules.acceptAnyAnswer;
            expectedLabel.Text = acceptsAny || item.Responses.Count == 0
                ? "Confirmacao por voz: qualquer resposta reconhecida"
                : "Resposta esperada: " + string.Join(" / ", item.Responses);
            progressLabel.Text =
                "Item " + (session.ItemIndex + 1) + " de " + session.ItemCount;
            SetState("PENDENTE", warning);
            heardLabel.Text = "Segure o botao ou F9 para responder";
            RefreshChecklistItems();
            speechSynthesis.Speak(item.Callout);
        }

        private void RefreshChecklistItems()
        {
            checklistItemsPanel.SuspendLayout();
            while (checklistItemsPanel.Controls.Count > 0)
                checklistItemsPanel.Controls[0].Dispose();

            for (int index = 0; index < session.ItemCount; index++)
            {
                ChecklistItem item =
                    ChecklistItem.FromJson(session.Checklist.items[index]);
                bool completed = index < session.ItemIndex;
                bool current = !session.IsComplete && index == session.ItemIndex;

                var row = new Panel
                {
                    BackColor = current
                        ? System.Drawing.Color.FromArgb(31, 43, 58)
                        : panelColor,
                    Height = 27,
                    Width = Math.Max(100, checklistItemsPanel.ClientSize.Width - 24),
                    Margin = new Padding(0, 0, 0, 2)
                };

                var icon = new Label
                {
                    Text = completed ? "✓" : current ? "›" : string.Empty,
                    ForeColor = completed ? success : primary,
                    Font = currentItemFont,
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    BackColor = System.Drawing.Color.Transparent
                };
                icon.SetBounds(4, 2, 24, 23);

                var text = new Label
                {
                    Text = item.Callout,
                    ForeColor = completed
                        ? System.Drawing.Color.FromArgb(125, 143, 163)
                        : current ? System.Drawing.Color.White : muted,
                    Font = completed
                        ? completedItemFont
                        : current ? currentItemFont : pendingItemFont,
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    AutoEllipsis = true,
                    BackColor = System.Drawing.Color.Transparent
                };
                text.SetBounds(32, 2, row.Width - 40, 23);
                text.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                row.Controls.Add(icon);
                row.Controls.Add(text);
                checklistItemsPanel.Controls.Add(row);
            }

            checklistItemsPanel.ResumeLayout();
        }

        private async void StartListening()
        {
            if (listening || !speechRecognition.IsReady || !session.IsActive ||
                session.IsComplete)
                return;

            listening = true;
            responseHandled = false;
            recognitionResultReceived = false;
            speechSynthesis.Cancel();
            SystemSounds.Beep.Play();
            listeningAnimationStep = 0;
            pttButton.BackColor = success;
            AnimateListening();
            listeningAnimationTimer.Start();
            SetState("OUVINDO", success);

            try
            {
                await speechRecognition.StartAsync();
            }
            catch (Exception error)
            {
                speechStatus = "Microfone indisponivel: " + DescribeSpeechError(error);
                ResetPtt();
                RefreshStatus();
            }
        }

        private async void StopListening()
        {
            if (!listening) return;
            listening = false;
            ResetPtt();
            try
            {
                await speechRecognition.StopAsync();
            }
            catch (Exception error)
            {
                speechStatus = "Falha ao encerrar microfone: " +
                               DescribeSpeechError(error);
                RefreshStatus();
            }
        }

        private static string DescribeSpeechError(Exception error)
        {
            Exception rootError = error.GetBaseException();
            string errorCode = "0x" + rootError.HResult.ToString("X8");

            switch (unchecked((uint)rootError.HResult))
            {
                case 0x80045509:
                    return "ative o Reconhecimento de fala online em " +
                           "Configuracoes > Privacidade e seguranca > Fala. " +
                           "(" + errorCode + ")";
                case 0x80070005:
                    return "acesso negado. Libere o FSChecklist em " +
                           "Configuracoes > Privacidade e seguranca > Microfone. " +
                           "(" + errorCode + ")";
                default:
                    return rootError.Message + " (" + errorCode + ")";
            }
        }

        private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs args)
        {
            RunOnUi(delegate
            {
                if (responseHandled) return;
                recognitionResultReceived = true;
                heardLabel.Text = "Ouvido: " + args.Text +
                                  " (" + args.Confidence + ")";

                bool acceptsAny = session.Document != null &&
                                  session.Document.rules != null &&
                                  session.Document.rules.acceptAnyAnswer;
                if (!acceptsAny &&
                    (args.Confidence == RecognitionConfidence.Low ||
                     args.Confidence == RecognitionConfidence.Rejected))
                {
                    heardLabel.Text =
                        "Fala incerta: " + args.Text +
                        " (" + args.Confidence + ") - tente novamente";
                    return;
                }

                if (!session.TryConfirm(args.Text))
                {
                    SetState("NAO CONFIRMADO", danger);
                    checklistStatus =
                        "A resposta nao coincide com o JSON. O item permanece pendente.";
                    RefreshStatus();
                    speechSynthesis.Speak("Nao confirmado");
                    return;
                }

                responseHandled = true;
                StopListening();
                SetState("CONFIRMADO", success);
                var timer = new Timer { Interval = 550 };
                timer.Tick += delegate
                {
                    timer.Stop();
                    timer.Dispose();
                    ShowCurrentItem();
                };
                timer.Start();
            });
        }

        private void ResetPtt()
        {
            listening = false;
            listeningAnimationTimer.Stop();
            pttButton.BackColor = primary;
            pttButton.Text = speechRecognition.IsReady
                ? "SEGURE PARA FALAR - F9 GLOBAL"
                : "RECONHECIMENTO DE VOZ INDISPONIVEL";
        }

        private void AnimateListening()
        {
            if (!listening) return;
            listeningAnimationStep = (listeningAnimationStep % 3) + 1;
            string dots = new string('.', listeningAnimationStep);
            pttButton.Text = "OUVINDO" + dots + "  SOLTE PARA ENVIAR";
            stateLabel.Text = "OUVINDO" + dots;
        }

        private void RefreshStatus()
        {
            statusLabel.Text = string.Join(" ", new[]
            {
                checklistStatus, speechStatus, hotkeyStatus
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        private void SetState(string text, System.Drawing.Color color)
        {
            stateLabel.Text = text;
            stateLabel.ForeColor = color;
        }

        private void RunOnUi(Action action)
        {
            if (IsDisposed) return;
            if (InvokeRequired) BeginInvoke(action);
            else action();
        }
    }
}
