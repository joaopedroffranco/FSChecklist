using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FSChecklist.Domain.Checklists;
using FSChecklist.Domain.Settings;
using FSChecklist.Features.AudioInput;
using FSChecklist.Features.Checklist;
using FSChecklist.Features.Input;
using FSChecklist.Features.Localization;
using FSChecklist.Features.Repository;
using FSChecklist.Features.Settings;
using FSChecklist.Features.SpeechRecognition;

namespace FSChecklist.Features.Main
{
    internal sealed partial class MainForm : Form
    {
        private readonly IChecklistRepository repository;
        private readonly ISpeechRecognitionService speechRecognition;
        private readonly ISpeechSynthesisService speechSynthesis;
        private readonly IGlobalPushToTalk globalPushToTalk;
        private readonly IAppSettingsRepository settingsRepository;
        private readonly IAppLocalizer localizer;
        private readonly IAudioInputDeviceService audioInput;
        private readonly string hotkeyError;
        private readonly ChecklistSession session = new ChecklistSession();
        private readonly List<ChecklistDocument> documents = new List<ChecklistDocument>();
        private readonly List<string> startupErrors = new List<string>();
        private AppSettings settings;

        private bool checklistRunning;
        private bool awaitingResponse;
        private bool processingResponse;
        private bool recognitionStarted;
        private string checklistStatus = string.Empty;
        private string speechStatus;
        private string hotkeyStatus;

        public MainForm(
            IChecklistRepository repository,
            ISpeechRecognitionService speechRecognition,
            ISpeechSynthesisService speechSynthesis,
            IGlobalPushToTalk globalPushToTalk,
            string hotkeyError,
            IAppSettingsRepository settingsRepository,
            AppSettings settings,
            IAppLocalizer localizer,
            IAudioInputDeviceService audioInput)
        {
            this.repository = repository;
            this.speechRecognition = speechRecognition;
            this.speechSynthesis = speechSynthesis;
            this.globalPushToTalk = globalPushToTalk;
            this.hotkeyError = hotkeyError;
            this.settingsRepository = settingsRepository;
            this.settings = settings;
            this.localizer = localizer;
            this.audioInput = audioInput;
            speechStatus = localizer.Get("SpeechInitializing");
            UpdateHotkeyStatus();

            BuildInterface();
            WireEvents();
            LoadChecklists();

            Shown += async delegate
            {
                await speechRecognition.InitializeAsync();
                speechStatus = speechRecognition.Status;
                startButton.Enabled = speechRecognition.IsReady;
                if (!speechRecognition.IsReady)
                {
                    microphoneStatusLabel.Text =
                        localizer.Get("SpeechUnavailable");
                    startupErrors.Add(speechRecognition.Status);
                }
                if (globalPushToTalk == null &&
                    !string.IsNullOrWhiteSpace(hotkeyError))
                    startupErrors.Add(hotkeyStatus);
                UpdateReadyChecklist();
                RefreshStatus();
                foreach (string startupError in startupErrors.Distinct())
                    ShowError(startupError);
                startupErrors.Clear();
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
            settingsButton.Click +=
                async delegate { await OpenSettingsAsync(); };

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
                    SetState(localizer.Get("MicrophoneStopped"), danger);
                    heardLabel.Text =
                        localizer.Format(
                            "ListeningEnded",
                            CurrentHotkeyText());
                    EndChecklistRun();
                    ShowError(heardLabel.Text);
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
                    if (HotkeyFormatter.Matches(
                            settings.Hotkey,
                            args.KeyCode,
                            args.Modifiers) &&
                        !args.Handled)
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
                    checklistStatus = localizer.Get("NoChecklistFound");
                }
            }
            catch (Exception error)
            {
                checklistStatus = error.Message;
                startupErrors.Add(error.GetBaseException().Message);
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
            expectedLabel.Text = localizer.Format(
                "PressToStart",
                CurrentHotkeyText());
            progressLabel.Text = localizer.Format(
                "ItemCount",
                checklist.items.Count);
            heardLabel.Text = string.Empty;
            SetState(localizer.Get("Ready"), success);
            SetMicrophoneStatus(
                speechRecognition.IsReady
                    ? localizer.Get("MicrophoneOff")
                    : localizer.Get("SpeechUnavailable"),
                panelColor,
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
                heardLabel.Text = localizer.Get("StartingChecklist");
                SetState(localizer.Get("Starting"), primary);
                await speechSynthesis.SpeakAsync(checklist.name + " checklist");
                processingResponse = false;
                await PresentCurrentItemAsync();
            }
            catch (Exception error)
            {
                HandleSpeechFailure(localizer.Get("StartFailure"), error);
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
                localizer.Get("CopilotSpeaking"),
                success,
                textPrimary);
            SetState(localizer.Get("Callout"), primary);

            speechRecognition.SetAcceptedResponses(
                session.AcceptedResponses);
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
                await ListenForResponseAsync(recognitionTask);
            }
            catch (OperationCanceledException)
            {
                // A manual check or finish command intentionally cancels
                // the current one-shot recognition operation.
            }
            catch (Exception error)
            {
                HandleSpeechFailure(
                    localizer.Get("RecognitionFailure"),
                    error);
                EndChecklistRun();
            }
        }

        private async Task ListenForResponseAsync(
            Task<SpeechRecognizedEventArgs> recognitionTask)
        {
            Task<SpeechRecognizedEventArgs> pendingRecognition =
                recognitionTask;

            while (checklistRunning && awaitingResponse)
            {
                SpeechRecognizedEventArgs response =
                    await pendingRecognition;
                if (!checklistRunning || !awaitingResponse) return;

                bool weakUnmatchedResult =
                    !session.CanConfirm(response.Text) &&
                    (string.IsNullOrWhiteSpace(response.Text) ||
                     response.Confidence == RecognitionConfidence.Low ||
                     response.Confidence == RecognitionConfidence.Rejected);

                if (!weakUnmatchedResult)
                {
                    await HandleSpeechRecognizedAsync(response);
                    return;
                }

                heardLabel.Text = localizer.Get("WaitingReadback");
                ShowListeningStatus();
                pendingRecognition = speechRecognition.RecognizeOnceAsync();
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
                ? localizer.Get("AnyResponse")
                : acceptedResponses.Count == 0
                    ? localizer.Get("NoValidResponse")
                    : localizer.Format(
                        "ExpectedResponse",
                        string.Join(" / ", acceptedResponses));
            progressLabel.Text = localizer.Format(
                "ItemProgress",
                session.ItemIndex + 1,
                session.ItemCount);
            heardLabel.Text = localizer.Get("WaitingReadback");
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
                    ? currentItemBackground
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
                    ? muted
                    : current ? textPrimary : muted,
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
                Text = completed
                    ? localizer.Get("Checked")
                    : current
                        ? localizer.Get("Readback")
                        : string.Empty,
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
                BackColor = borderColor
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
                    string.IsNullOrWhiteSpace(args.Text) ||
                    !session.CanConfirm(args.Text))
                    return;

                SetState(localizer.Get("SpeechDetected"), success);
                heardLabel.Text = localizer.Format("Detected", args.Text);

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
                        SetState(localizer.Get("SoundDetected"), success);
                        heardLabel.Text =
                            localizer.Get("ContinueSpeaking");
                        SetMicrophoneStatus(
                            localizer.Get("SoundDetectedContinue"),
                            success,
                            textPrimary);
                        break;
                    case SpeechListeningState.Processing:
                        SetState(localizer.Get("ProcessingSpeech"), warning);
                        heardLabel.Text =
                            localizer.Get("ConvertingSpeech");
                        SetMicrophoneStatus(
                            localizer.Get("Processing"),
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
                    localizer.Get("NoSpeech"));
                return;
            }

            processingResponse = true;
            awaitingResponse = false;
            SetCompactActionEnabled(forceCheckButton, false, primary);
            heardLabel.Text = localizer.Format(
                "Heard",
                args.Text,
                args.Confidence);

            if (!session.TryConfirm(args.Text))
            {
                await RetryCurrentItemAsync(
                    localizer.Format("NotConfirmedText", args.Text));
                return;
            }

            SetState(localizer.Get("Confirmed"), success);
            RefreshChecklistItems();
            await Task.Delay(300);
            processingResponse = false;
            await PresentCurrentItemAsync();
        }

        private async Task RetryCurrentItemAsync(string message)
        {
            SetState(localizer.Get("NotConfirmed"), danger);
            heardLabel.Text = message;
            System.Media.SystemSounds.Hand.Play();
            await speechSynthesis.SpeakAsync("Not confirmed");
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
            SetState(localizer.Get("ManualCheck"), success);
            heardLabel.Text = localizer.Get("ManuallyConfirmed");
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
            challengeLabel.Text = localizer.Format(
                "ChecklistCompletedTitle",
                completedChecklist.name);
            expectedLabel.Text = nextChecklist == null
                ? localizer.Get("NoNextChecklist")
                : localizer.Format("NextChecklist", nextChecklist.name);
            progressLabel.Text = localizer.Format(
                "ItemProgress",
                session.ItemCount,
                session.ItemCount);
            heardLabel.Text = manuallyTerminated
                ? localizer.Get("ChecklistManuallyEnded")
                : localizer.Get("ChecklistCompleted");
            SetState(
                manuallyTerminated
                    ? localizer.Get("Ended")
                    : localizer.Get("Complete"),
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
                expectedLabel.Text = localizer.Format(
                    "PressToStart",
                    CurrentHotkeyText());
                progressLabel.Text = localizer.Format(
                    "ItemCount",
                    nextChecklist.items.Count);
                heardLabel.Text = manuallyTerminated
                    ? completedChecklist.name + " - " +
                      localizer.Get("ChecklistManuallyEnded")
                    : completedChecklist.name + " complete.";
                SetState(localizer.Get("Ready"), success);
                RefreshPreviewItems(nextChecklist);
                checklistStatus = localizer.Format(
                    "CurrentChecklist",
                    nextChecklist.name);
            }
            else
            {
                checklistStatus = localizer.Format(
                    "AllComplete",
                    completedChecklist.name);
            }

            SetMicrophoneStatus(
                localizer.Get("MicrophoneOff"),
                panelColor,
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
                HandleSpeechFailure(
                    localizer.Get("MicrophoneUnavailable"),
                    error);
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
            SetState(localizer.Get("VoiceError"), danger);
            RefreshStatus();
            ShowError(speechStatus);
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
                    ? localizer.Get("MicrophoneOff")
                    : localizer.Get("SpeechUnavailable"),
                panelColor,
                muted);
        }

        private void SetRunControls(bool enabled)
        {
            aircraftBox.Enabled = enabled;
            checklistBox.Enabled = enabled;
            settingsButton.Enabled = enabled;
            startButton.Enabled = enabled && speechRecognition.IsReady;
            SetCompactActionEnabled(
                forceCheckButton,
                !enabled && awaitingResponse,
                primary);
            SetCompactActionEnabled(finishButton, !enabled, danger);
        }

        private string DescribeSpeechError(Exception error)
        {
            Exception rootError = error.GetBaseException();
            string errorCode = "0x" + rootError.HResult.ToString("X8");

            switch (unchecked((uint)rootError.HResult))
            {
                case 0x80045509:
                    return localizer.Format(
                        "EnableOnlineSpeech",
                        errorCode);
                case 0x80070005:
                    return localizer.Format(
                        "MicrophoneAccessDenied",
                        errorCode);
                default:
                    return rootError.Message + " (" + errorCode + ")";
            }
        }

        private void ShowListeningStatus()
        {
            if (!awaitingResponse) return;
            SetMicrophoneStatus(
                localizer.Get("MicrophoneListening"),
                success,
                textPrimary);
            stateLabel.Text = localizer.Get("Listening");
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
                backColor == panelColor
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
