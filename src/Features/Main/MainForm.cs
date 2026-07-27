using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        private bool checklistRunning;
        private bool awaitingResponse;
        private bool processingResponse;
        private bool recognitionStarted;
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

            Shown += async delegate
            {
                await speechRecognition.InitializeAsync();
                speechStatus = speechRecognition.Status;
                startButton.Enabled = speechRecognition.IsReady;
                if (!speechRecognition.IsReady)
                    microphoneStatusLabel.Text =
                        "MICROFONE: RECONHECIMENTO INDISPONIVEL";
                UpdateReadyChecklist();
                RefreshStatus();
            };
        }

        private void WireEvents()
        {
            aircraftBox.SelectedIndexChanged += AircraftChanged;
            checklistBox.SelectedIndexChanged += delegate
            {
                if (!checklistRunning) UpdateReadyChecklist();
            };
            startButton.Click += async delegate { await StartCurrentChecklistAsync(); };
            forceCheckButton.Click +=
                async delegate { await ForceCurrentItemAsync(); };
            finishButton.Click +=
                async delegate { await FinishChecklistAsync(); };
            listeningAnimationTimer.Tick += delegate { AnimateListening(); };

            speechRecognition.SpeechRecognized += OnSpeechRecognized;
            speechRecognition.ListeningStateChanged += OnListeningStateChanged;
            speechRecognition.RecognitionCompleted += delegate
            {
                RunOnUi(delegate
                {
                    if (!checklistRunning || !recognitionStarted) return;
                    recognitionStarted = false;
                    awaitingResponse = false;
                    SetState("MICROFONE ENCERRADO", danger);
                    heardLabel.Text =
                        "A escuta foi encerrada pelo Windows. Pressione F9 para reiniciar.";
                    EndChecklistRun();
                });
            };

            if (globalPushToTalk != null)
            {
                globalPushToTalk.StateChanged += delegate(bool isDown)
                {
                    if (isDown)
                        RunOnUi(async delegate { await StartCurrentChecklistAsync(); });
                };
            }
            else
            {
                KeyDown += async delegate(object sender, KeyEventArgs args)
                {
                    if (args.KeyCode == Keys.F9 && !args.Handled)
                    {
                        args.Handled = true;
                        await StartCurrentChecklistAsync();
                    }
                };
            }

            FormClosed += delegate
            {
                checklistRunning = false;
                if (globalPushToTalk != null) globalPushToTalk.Dispose();
                listeningAnimationTimer.Dispose();
                pendingItemFont.Dispose();
                currentItemFont.Dispose();
                completedItemFont.Dispose();
                if (Icon != null) Icon.Dispose();
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
            ChecklistDocument document = SelectedDocument();
            if (document == null) return;

            foreach (ChecklistDefinition checklist in document.checklists)
                checklistBox.Items.Add(checklist.name);
            if (checklistBox.Items.Count > 0) checklistBox.SelectedIndex = 0;
        }

        private ChecklistDocument SelectedDocument()
        {
            return documents.FirstOrDefault(
                item => item.aircraft == Convert.ToString(aircraftBox.SelectedItem));
        }

        private ChecklistDefinition SelectedChecklist(ChecklistDocument document)
        {
            if (document == null) return null;
            return document.checklists.FirstOrDefault(
                item => item.name == Convert.ToString(checklistBox.SelectedItem));
        }

        private void UpdateReadyChecklist()
        {
            ChecklistDocument document = SelectedDocument();
            ChecklistDefinition checklist = SelectedChecklist(document);
            if (checklist == null) return;

            checklistNameLabel.Text =
                "CURRENT CHECKLIST: " + checklist.name.ToUpperInvariant();
            challengeLabel.Text = checklist.name;
            expectedLabel.Text = "Pressione F9 para iniciar o ciclo completo";
            progressLabel.Text = checklist.items.Count + " itens";
            heardLabel.Text = "Microfone desligado";
            SetState("PRONTO", success);
            SetMicrophoneStatus(
                speechRecognition.IsReady
                    ? "MICROFONE: DESLIGADO - F9 INICIA"
                    : "MICROFONE: RECONHECIMENTO INDISPONIVEL",
                System.Drawing.Color.FromArgb(31, 43, 58),
                muted);
            RefreshPreviewItems(checklist);
        }

        private async Task StartCurrentChecklistAsync()
        {
            if (checklistRunning || !speechRecognition.IsReady) return;

            ChecklistDocument document = SelectedDocument();
            ChecklistDefinition checklist = SelectedChecklist(document);
            if (document == null || checklist == null) return;

            checklistRunning = true;
            awaitingResponse = false;
            processingResponse = true;
            session.Start(document, checklist);
            SetRunControls(false);

            try
            {
                await StopRecognitionAsync();
                await StartRecognitionAsync();
                checklistNameLabel.Text =
                    "CURRENT CHECKLIST: " + checklist.name.ToUpperInvariant();
                heardLabel.Text = "Iniciando checklist...";
                SetState("INICIANDO", primary);
                await speechSynthesis.SpeakAsync(checklist.name + " checklist");
                processingResponse = false;
                await PresentCurrentItemAsync();
            }
            catch (Exception error)
            {
                HandleSpeechFailure("Falha ao iniciar checklist: ", error);
                EndChecklistRun();
            }
        }

        private async Task PresentCurrentItemAsync()
        {
            if (!checklistRunning) return;
            if (session.IsComplete)
            {
                await CompleteCurrentChecklistAsync();
                return;
            }

            ChecklistItem item = session.CurrentItem;
            UpdateCurrentItemUi(item);
            awaitingResponse = false;
            processingResponse = true;
            forceCheckButton.Enabled = false;
            SetMicrophoneStatus(
                "MICROFONE: PAUSADO - COPILOTO FALANDO",
                primary,
                System.Drawing.Color.White);
            SetState("CALLOUT", primary);

            await speechSynthesis.SpeakAsync(item.Callout);
            if (!checklistRunning) return;

            // The microphone stays open for the entire checklist. Results
            // produced by the copilot voice are ignored until this guard ends.
            await Task.Delay(500);
            processingResponse = false;
            awaitingResponse = true;
            listeningAnimationStep = 0;
            listeningAnimationTimer.Start();
            forceCheckButton.Enabled = true;
            AnimateListening();
        }

        private void UpdateCurrentItemUi(ChecklistItem item)
        {
            checklistNameLabel.Text =
                "CURRENT CHECKLIST: " + session.Checklist.name.ToUpperInvariant();
            challengeLabel.Text = item.Callout;
            bool acceptsAny = session.Document.rules != null &&
                              session.Document.rules.acceptAnyAnswer;
            expectedLabel.Text = acceptsAny || item.Responses.Count == 0
                ? "Responda normalmente; qualquer resposta reconhecida confirma"
                : "Resposta esperada: " + string.Join(" / ", item.Responses);
            progressLabel.Text =
                "Item " + (session.ItemIndex + 1) + " de " + session.ItemCount;
            heardLabel.Text = "Aguardando seu readback...";
            RefreshChecklistItems();
        }

        private void RefreshPreviewItems(ChecklistDefinition checklist)
        {
            checklistItemsPanel.SuspendLayout();
            ClearChecklistItems();
            foreach (object value in checklist.items)
                AddChecklistItemRow(ChecklistItem.FromJson(value), false, false);
            checklistItemsPanel.ResumeLayout();
        }

        private void RefreshChecklistItems()
        {
            checklistItemsPanel.SuspendLayout();
            ClearChecklistItems();

            for (int index = 0; index < session.ItemCount; index++)
            {
                ChecklistItem item =
                    ChecklistItem.FromJson(session.Checklist.items[index]);
                AddChecklistItemRow(
                    item,
                    index < session.ItemIndex,
                    !session.IsComplete && index == session.ItemIndex);
            }
            checklistItemsPanel.ResumeLayout();
        }

        private void ClearChecklistItems()
        {
            while (checklistItemsPanel.Controls.Count > 0)
                checklistItemsPanel.Controls[0].Dispose();
        }

        private void AddChecklistItemRow(
            ChecklistItem item,
            bool completed,
            bool current)
        {
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

        private void OnSpeechRecognized(
            object sender,
            SpeechRecognizedEventArgs args)
        {
            RunOnUi(async delegate { await HandleSpeechRecognizedAsync(args); });
        }

        private void OnListeningStateChanged(
            object sender,
            SpeechListeningStateChangedEventArgs args)
        {
            RunOnUi(delegate
            {
                if (!checklistRunning || !awaitingResponse) return;

                switch (args.State)
                {
                    case SpeechListeningState.SoundDetected:
                        listeningAnimationTimer.Stop();
                        SetState("SOM DETECTADO", success);
                        heardLabel.Text =
                            "O microfone detectou audio; continue falando.";
                        SetMicrophoneStatus(
                            "MICROFONE: SOM DETECTADO - CONTINUE FALANDO",
                            success,
                            System.Drawing.Color.White);
                        break;
                    case SpeechListeningState.Processing:
                        listeningAnimationTimer.Stop();
                        SetState("PROCESSANDO FALA", warning);
                        heardLabel.Text =
                            "Audio recebido; convertendo para texto...";
                        SetMicrophoneStatus(
                            "MICROFONE: PROCESSANDO FALA...",
                            warning,
                            background);
                        break;
                    case SpeechListeningState.Listening:
                        if (!listeningAnimationTimer.Enabled)
                            listeningAnimationTimer.Start();
                        SetState("OUVINDO", success);
                        break;
                }
            });
        }

        private async Task HandleSpeechRecognizedAsync(
            SpeechRecognizedEventArgs args)
        {
            if (!checklistRunning || !awaitingResponse || processingResponse)
                return;
            if (string.IsNullOrWhiteSpace(args.Text)) return;

            processingResponse = true;
            awaitingResponse = false;
            forceCheckButton.Enabled = false;
            listeningAnimationTimer.Stop();
            heardLabel.Text = "Ouvido: " + args.Text +
                              " (" + args.Confidence + ")";

            bool acceptsAny = session.Document != null &&
                              session.Document.rules != null &&
                              session.Document.rules.acceptAnyAnswer;
            if (!acceptsAny &&
                (args.Confidence == RecognitionConfidence.Low ||
                 args.Confidence == RecognitionConfidence.Rejected))
            {
                await RetryCurrentItemAsync(
                    "Fala incerta: " + args.Text + ". Tente novamente.");
                return;
            }

            if (!session.TryConfirm(args.Text))
            {
                await RetryCurrentItemAsync(
                    "Resposta nao confirmada. Tente novamente.");
                return;
            }

            SetState("CONFIRMADO", success);
            RefreshChecklistItems();
            await Task.Delay(300);
            processingResponse = false;
            await PresentCurrentItemAsync();
        }

        private async Task RetryCurrentItemAsync(string message)
        {
            SetState("NAO CONFIRMADO", danger);
            heardLabel.Text = message;
            await speechSynthesis.SpeakAsync("Nao confirmado");
            processingResponse = false;
            await PresentCurrentItemAsync();
        }

        private async Task ForceCurrentItemAsync()
        {
            if (!checklistRunning ||
                processingResponse ||
                !awaitingResponse ||
                !session.ForceConfirm())
                return;

            processingResponse = true;
            awaitingResponse = false;
            forceCheckButton.Enabled = false;
            listeningAnimationTimer.Stop();
            SetState("CHECK MANUAL", success);
            heardLabel.Text = "Item confirmado manualmente.";
            RefreshChecklistItems();
            await Task.Delay(200);
            processingResponse = false;
            await PresentCurrentItemAsync();
        }

        private async Task FinishChecklistAsync()
        {
            if (!checklistRunning) return;

            checklistRunning = false;
            awaitingResponse = false;
            processingResponse = false;
            forceCheckButton.Enabled = false;
            listeningAnimationTimer.Stop();
            speechSynthesis.Cancel();

            try
            {
                await StopRecognitionAsync();
            }
            catch (Exception error)
            {
                speechStatus =
                    "Falha ao encerrar microfone: " + DescribeSpeechError(error);
            }

            session.End();
            SetRunControls(true);
            UpdateReadyChecklist();
            heardLabel.Text = "Checklist encerrada manualmente. Microfone desligado.";
            SetState("ENCERRADA", warning);
            RefreshStatus();
        }

        private async Task CompleteCurrentChecklistAsync()
        {
            processingResponse = true;
            awaitingResponse = false;
            listeningAnimationTimer.Stop();
            await StopRecognitionAsync();

            ChecklistDefinition completedChecklist = session.Checklist;
            ChecklistDefinition nextChecklist = FindNextChecklist();
            string completedCallout = string.IsNullOrWhiteSpace(
                completedChecklist.completedCallout)
                ? completedChecklist.name + " checklist complete"
                : completedChecklist.completedCallout;
            string announcement = nextChecklist == null
                ? completedCallout
                : completedCallout + ". Next checklist, " + nextChecklist.name;

            checklistNameLabel.Text =
                "CURRENT CHECKLIST: " + completedChecklist.name.ToUpperInvariant();
            challengeLabel.Text = completedChecklist.name + " completa";
            expectedLabel.Text = nextChecklist == null
                ? "Nao ha proxima checklist configurada"
                : "Proxima checklist: " + nextChecklist.name;
            progressLabel.Text = session.ItemCount + " de " + session.ItemCount;
            heardLabel.Text = "Microfone desligado";
            SetState("COMPLETA", success);
            RefreshChecklistItems();

            await speechSynthesis.SpeakAsync(announcement);
            checklistRunning = false;
            processingResponse = false;
            SetRunControls(true);

            if (nextChecklist != null)
            {
                checklistBox.SelectedItem = nextChecklist.name;
                checklistNameLabel.Text =
                    "CURRENT CHECKLIST: " + nextChecklist.name.ToUpperInvariant();
                challengeLabel.Text = nextChecklist.name;
                expectedLabel.Text = "Pressione F9 para iniciar o ciclo completo";
                progressLabel.Text = nextChecklist.items.Count + " itens";
                heardLabel.Text =
                    completedChecklist.name + " complete. Microfone desligado.";
                SetState("PRONTO", success);
                RefreshPreviewItems(nextChecklist);
                checklistStatus = "Current checklist: " + nextChecklist.name + ".";
            }
            else
            {
                checklistStatus = completedChecklist.name +
                                  " completa. Fim das checklists.";
            }

            SetMicrophoneStatus(
                "MICROFONE: DESLIGADO - F9 INICIA",
                System.Drawing.Color.FromArgb(31, 43, 58),
                muted);
            RefreshStatus();
        }

        private ChecklistDefinition FindNextChecklist()
        {
            if (session.Document == null ||
                session.Checklist == null ||
                string.IsNullOrWhiteSpace(session.Checklist.next))
                return null;

            return session.Document.checklists.FirstOrDefault(
                item => string.Equals(
                    item.name,
                    session.Checklist.next,
                    StringComparison.OrdinalIgnoreCase));
        }

        private async Task StartRecognitionAsync()
        {
            if (recognitionStarted) return;
            try
            {
                await speechRecognition.StartAsync();
                recognitionStarted = true;
            }
            catch (Exception error)
            {
                HandleSpeechFailure("Microfone indisponivel: ", error);
                EndChecklistRun();
                throw;
            }
        }

        private async Task StopRecognitionAsync()
        {
            if (!recognitionStarted) return;
            recognitionStarted = false;
            await speechRecognition.StopAsync();
        }

        private void HandleSpeechFailure(string prefix, Exception error)
        {
            speechStatus = prefix + DescribeSpeechError(error);
            SetState("ERRO DE VOZ", danger);
            RefreshStatus();
        }

        private void EndChecklistRun()
        {
            checklistRunning = false;
            awaitingResponse = false;
            processingResponse = false;
            recognitionStarted = false;
            listeningAnimationTimer.Stop();
            SetRunControls(true);
            SetMicrophoneStatus(
                speechRecognition.IsReady
                    ? "MICROFONE: DESLIGADO - F9 INICIA"
                    : "MICROFONE: RECONHECIMENTO INDISPONIVEL",
                System.Drawing.Color.FromArgb(31, 43, 58),
                muted);
        }

        private void SetRunControls(bool enabled)
        {
            aircraftBox.Enabled = enabled;
            checklistBox.Enabled = enabled;
            startButton.Enabled = enabled && speechRecognition.IsReady;
            forceCheckButton.Enabled = !enabled && awaitingResponse;
            finishButton.Enabled = !enabled;
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

        private void AnimateListening()
        {
            if (!awaitingResponse) return;
            listeningAnimationStep = (listeningAnimationStep % 3) + 1;
            string dots = new string('.', listeningAnimationStep);
            SetMicrophoneStatus(
                "MICROFONE: OUVINDO" + dots + "  RESPONDA AO CALLOUT",
                success,
                System.Drawing.Color.White);
            stateLabel.Text = "OUVINDO" + dots;
            stateLabel.ForeColor = success;
        }

        private void SetMicrophoneStatus(
            string text,
            System.Drawing.Color backColor,
            System.Drawing.Color foreColor)
        {
            microphoneStatusLabel.Text = text;
            microphoneStatusLabel.BackColor = backColor;
            microphoneStatusLabel.ForeColor = foreColor;
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
