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

        private bool checklistRunning;
        private bool awaitingResponse;
        private bool processingResponse;
        private bool recognitionStarted;
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
                        "Reconhecimento de voz indisponivel";
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

            speechRecognition.SpeechRecognized += OnSpeechRecognized;
            speechRecognition.SpeechHypothesized += OnSpeechHypothesized;
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
                checklist.name.ToUpperInvariant();
            challengeLabel.Text = checklist.name;
            expectedLabel.Text = "Pressione F9 para iniciar o ciclo completo";
            progressLabel.Text = checklist.items.Count + " itens";
            heardLabel.Text = string.Empty;
            SetState("PRONTO", success);
            SetMicrophoneStatus(
                speechRecognition.IsReady
                    ? "Microfone desligado"
                    : "Reconhecimento de voz indisponivel",
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
                await speechRecognition.CancelAsync();
                checklistNameLabel.Text =
                    checklist.name.ToUpperInvariant();
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
            SetCompactActionEnabled(forceCheckButton, false, primary);
            SetMicrophoneStatus(
                "Microfone aberto - copiloto falando",
                success,
                System.Drawing.Color.White);
            SetState("CALLOUT", primary);

            Task<SpeechRecognizedEventArgs> recognitionTask =
                speechRecognition.RecognizeOnceAsync();
            await speechSynthesis.SpeakAsync(item.Callout);
            if (!checklistRunning)
            {
                await speechRecognition.CancelAsync();
                return;
            }

            processingResponse = false;
            awaitingResponse = true;
            SetCompactActionEnabled(forceCheckButton, true, primary);
            ShowListeningStatus();

            try
            {
                SpeechRecognizedEventArgs response =
                    await recognitionTask;
                if (checklistRunning && awaitingResponse)
                    await HandleSpeechRecognizedAsync(response);
            }
            catch (OperationCanceledException)
            {
                // A manual check or finish command intentionally cancels
                // the current one-shot recognition operation.
            }
            catch (Exception error)
            {
                HandleSpeechFailure("Falha ao reconhecer resposta: ", error);
                EndChecklistRun();
            }
        }

        private void UpdateCurrentItemUi(ChecklistItem item)
        {
            checklistNameLabel.Text =
                session.Checklist.name.ToUpperInvariant();
            challengeLabel.Text = item.Callout;
            bool acceptsAny = session.Document.rules != null &&
                              session.Document.rules.acceptAnyAnswer;
            IReadOnlyList<string> acceptedResponses =
                session.AcceptedResponses;
            expectedLabel.Text = acceptsAny
                ? "Responda normalmente; qualquer resposta reconhecida confirma"
                : acceptedResponses.Count == 0
                    ? "Nenhuma resposta valida configurada"
                    : "Resposta esperada: " +
                      string.Join(" / ", acceptedResponses);
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
            checklistItemsPanel.AutoScrollPosition =
                new System.Drawing.Point(0, 0);
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
            if (session.ItemIndex == 0)
            {
                checklistItemsPanel.AutoScrollPosition =
                    new System.Drawing.Point(0, 0);
            }
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
                    ? System.Drawing.Color.FromArgb(32, 47, 64)
                    : panelColor,
                Height = 56,
                Width = Math.Max(100, checklistItemsPanel.ClientSize.Width - 4),
                Margin = new Padding(0)
            };

            var icon = new Label
            {
                Text = completed ? "✓" : current ? "›" : string.Empty,
                ForeColor = completed ? success : primary,
                Font = currentItemFont,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                BackColor = System.Drawing.Color.Transparent
            };
            icon.SetBounds(8, 0, 28, 55);

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
            text.SetBounds(42, 0, row.Width - 180, 55);
            text.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var itemStatus = new Label
            {
                Text = completed ? "CHECKED" : current ? "READBACK" : string.Empty,
                ForeColor = completed ? success : current ? warning : muted,
                Font = new System.Drawing.Font(
                    "Segoe UI",
                    8.5F,
                    System.Drawing.FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                BackColor = System.Drawing.Color.Transparent
            };
            itemStatus.SetBounds(row.Width - 132, 0, 116, 55);
            itemStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var separator = new Panel
            {
                BackColor = System.Drawing.Color.FromArgb(55, 67, 82)
            };
            separator.SetBounds(0, 55, row.Width, 1);
            separator.Anchor =
                AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            row.Controls.Add(icon);
            row.Controls.Add(text);
            row.Controls.Add(itemStatus);
            row.Controls.Add(separator);
            checklistItemsPanel.Controls.Add(row);
        }

        private void OnSpeechRecognized(
            object sender,
            SpeechRecognizedEventArgs args)
        {
            RunOnUi(async delegate { await HandleSpeechRecognizedAsync(args); });
        }

        private void OnSpeechHypothesized(
            object sender,
            SpeechRecognizedEventArgs args)
        {
            RunOnUi(delegate
            {
                if (!checklistRunning ||
                    !awaitingResponse ||
                    processingResponse ||
                    string.IsNullOrWhiteSpace(args.Text))
                    return;

                SetState("FALA DETECTADA", success);
                heardLabel.Text = "Detectado: " + args.Text;

            });
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
                        SetState("SOM DETECTADO", success);
                        heardLabel.Text =
                            "O microfone detectou audio; continue falando.";
                        SetMicrophoneStatus(
                            "Som detectado - continue falando",
                            success,
                            System.Drawing.Color.White);
                        break;
                    case SpeechListeningState.Processing:
                        SetState("PROCESSANDO FALA", warning);
                        heardLabel.Text =
                            "Audio recebido; convertendo para texto...";
                        SetMicrophoneStatus(
                            "Processando fala...",
                            warning,
                            background);
                        break;
                    case SpeechListeningState.Listening:
                        ShowListeningStatus();
                        break;
                }
            });
        }

        private async Task HandleSpeechRecognizedAsync(
            SpeechRecognizedEventArgs args)
        {
            if (!checklistRunning || !awaitingResponse || processingResponse)
                return;
            if (string.IsNullOrWhiteSpace(args.Text))
            {
                processingResponse = true;
                awaitingResponse = false;
                await RetryCurrentItemAsync(
                    "Nenhuma fala reconhecida. Tente novamente.");
                return;
            }

            processingResponse = true;
            awaitingResponse = false;
            SetCompactActionEnabled(forceCheckButton, false, primary);
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
            System.Media.SystemSounds.Hand.Play();
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
            SetCompactActionEnabled(forceCheckButton, false, primary);
            await speechRecognition.CancelAsync();
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

            processingResponse = true;
            speechSynthesis.Cancel();
            await CompleteCurrentChecklistAsync(true);
        }

        private async Task CompleteCurrentChecklistAsync(
            bool manuallyTerminated = false)
        {
            processingResponse = true;
            awaitingResponse = false;
            await speechRecognition.CancelAsync();
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
                completedChecklist.name.ToUpperInvariant();
            challengeLabel.Text = completedChecklist.name + " completa";
            expectedLabel.Text = nextChecklist == null
                ? "Nao ha proxima checklist configurada"
                : "Proxima checklist: " + nextChecklist.name;
            progressLabel.Text = session.ItemCount + " de " + session.ItemCount;
            heardLabel.Text = manuallyTerminated
                ? "Checklist encerrada manualmente."
                : "Checklist concluida.";
            SetState(manuallyTerminated ? "ENCERRADA" : "COMPLETA",
                manuallyTerminated ? warning : success);
            RefreshChecklistItems();

            if (nextChecklist != null)
            {
                int nextIndex = session.Document.checklists.IndexOf(nextChecklist);
                if (nextIndex >= 0) checklistBox.SelectedIndex = nextIndex;
                checklistNameLabel.Text =
                    nextChecklist.name.ToUpperInvariant();
            }

            await speechSynthesis.SpeakAsync(announcement);
            checklistRunning = false;
            processingResponse = false;
            SetRunControls(true);

            if (nextChecklist != null)
            {
                checklistNameLabel.Text =
                    nextChecklist.name.ToUpperInvariant();
                challengeLabel.Text = nextChecklist.name;
                expectedLabel.Text = "Pressione F9 para iniciar o ciclo completo";
                progressLabel.Text = nextChecklist.items.Count + " itens";
                heardLabel.Text = manuallyTerminated
                    ? completedChecklist.name + " encerrada manualmente."
                    : completedChecklist.name + " complete.";
                SetState("PRONTO", success);
                RefreshPreviewItems(nextChecklist);
                checklistStatus = "Checklist atual: " + nextChecklist.name + ".";
            }
            else
            {
                checklistStatus = completedChecklist.name +
                                  " completa. Fim das checklists.";
            }

            SetMicrophoneStatus(
                "Microfone desligado",
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
            SetRunControls(true);
            SetMicrophoneStatus(
                speechRecognition.IsReady
                    ? "Microfone desligado"
                    : "Reconhecimento de voz indisponivel",
                System.Drawing.Color.FromArgb(31, 43, 58),
                muted);
        }

        private void SetRunControls(bool enabled)
        {
            aircraftBox.Enabled = enabled;
            checklistBox.Enabled = enabled;
            startButton.Enabled = enabled && speechRecognition.IsReady;
            SetCompactActionEnabled(
                forceCheckButton,
                !enabled && awaitingResponse,
                primary);
            SetCompactActionEnabled(finishButton, !enabled, danger);
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

        private void ShowListeningStatus()
        {
            if (!awaitingResponse) return;
            SetMicrophoneStatus(
                "Microfone ouvindo...",
                success,
                System.Drawing.Color.White);
            stateLabel.Text = "OUVINDO...";
            stateLabel.ForeColor = success;
        }

        private void SetMicrophoneStatus(
            string text,
            System.Drawing.Color backColor,
            System.Drawing.Color foreColor)
        {
            microphoneStatusLabel.Text = text;
            microphoneStatusLabel.BackColor = System.Drawing.Color.Transparent;
            microphoneStatusLabel.ForeColor =
                backColor == System.Drawing.Color.FromArgb(31, 43, 58)
                    ? foreColor
                    : backColor;
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
