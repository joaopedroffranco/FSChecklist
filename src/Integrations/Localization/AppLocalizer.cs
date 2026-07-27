using System;
using System.Collections.Generic;
using System.Globalization;
using FSChecklist.Features.Localization;

namespace FSChecklist.Integrations.Localization
{
    internal sealed class AppLocalizer : IAppLocalizer
    {
        private static readonly Dictionary<string, string> Portuguese =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Aircraft"] = "Aeronave",
                ["Checklist"] = "Checklist",
                ["Settings"] = "Configurações",
                ["StartButton"] = "INICIAR OU {0}",
                ["NoChecklistStarted"] = "Nenhuma checklist iniciada",
                ["Ready"] = "PRONTO",
                ["SelectChecklist"] = "Selecione uma aeronave e uma checklist",
                ["WaitingStart"] = "Aguardando início",
                ["MicrophoneOff"] = "Microfone desligado",
                ["LoadingChecklists"] = "Carregando checklists...",
                ["ForceCheckTip"] = "Confirmar manualmente o item atual",
                ["FinishTip"] = "Encerrar a checklist atual",
                ["SpeechInitializing"] = "Reconhecimento: inicializando...",
                ["SpeechUnavailable"] = "Reconhecimento de voz indisponível",
                ["MicrophoneStopped"] = "MICROFONE ENCERRADO",
                ["RecognitionReady"] =
                    "Reconhecimento {0} pronto. Microfone: {1}.",
                ["RecognitionUnavailable"] = "Voz indisponível: {0}",
                ["RecognitionLanguageUnavailable"] =
                    "O Windows não oferece reconhecimento para {0}.",
                ["RecognitionPrepareFailure"] =
                    "Falha ao preparar reconhecimento: {0}",
                ["NoDefaultMicrophone"] = "nenhum dispositivo padrão",
                ["GenericDefaultMicrophone"] = "dispositivo padrão",
                ["HotkeyUnavailable"] = "{0} global indisponível: {1}",
                ["HotkeyActive"] = "{0} global ativo.",
                ["ListeningEnded"] =
                    "A escuta foi encerrada pelo Windows. Pressione {0} para reiniciar.",
                ["NoChecklistFound"] = "Nenhuma checklist encontrada.",
                ["PressToStart"] = "Pressione {0} para iniciar o ciclo completo",
                ["ItemCount"] = "{0} itens",
                ["StartingChecklist"] = "Iniciando checklist...",
                ["Starting"] = "INICIANDO",
                ["StartFailure"] = "Falha ao iniciar checklist: ",
                ["CopilotSpeaking"] = "Microfone aberto - copiloto falando",
                ["Callout"] = "CALLOUT",
                ["RecognitionFailure"] = "Falha ao reconhecer resposta: ",
                ["AnyResponse"] =
                    "Responda normalmente; qualquer resposta reconhecida confirma",
                ["NoValidResponse"] = "Nenhuma resposta válida configurada",
                ["ExpectedResponse"] = "Resposta esperada: {0}",
                ["ItemProgress"] = "Item {0} de {1}",
                ["WaitingReadback"] = "Aguardando seu readback...",
                ["Checked"] = "CHECKED",
                ["Readback"] = "READBACK",
                ["SpeechDetected"] = "FALA DETECTADA",
                ["Detected"] = "Detectado: {0}",
                ["SoundDetected"] = "SOM DETECTADO",
                ["ContinueSpeaking"] =
                    "O microfone detectou áudio; continue falando.",
                ["SoundDetectedContinue"] = "Som detectado - continue falando",
                ["ProcessingSpeech"] = "PROCESSANDO FALA",
                ["ConvertingSpeech"] =
                    "Áudio recebido; convertendo para texto...",
                ["Processing"] = "Processando fala...",
                ["NoSpeech"] = "Nenhuma fala reconhecida. Tente novamente.",
                ["Heard"] = "Ouvido: {0} ({1})",
                ["NotConfirmedText"] =
                    "Não confirmado: \"{0}\". Tente novamente.",
                ["Confirmed"] = "CONFIRMADO",
                ["NotConfirmed"] = "NÃO CONFIRMADO",
                ["ManualCheck"] = "CHECK MANUAL",
                ["ManuallyConfirmed"] = "Item confirmado manualmente.",
                ["NoNextChecklist"] = "Não há próxima checklist configurada",
                ["NextChecklist"] = "Próxima checklist: {0}",
                ["ChecklistManuallyEnded"] =
                    "Checklist encerrada manualmente.",
                ["ChecklistCompleted"] = "Checklist concluída.",
                ["ChecklistCompletedTitle"] = "{0} completa",
                ["Ended"] = "ENCERRADA",
                ["Complete"] = "COMPLETA",
                ["CurrentChecklist"] = "Checklist atual: {0}.",
                ["AllComplete"] = "{0} completa. Fim das checklists.",
                ["MicrophoneUnavailable"] = "Microfone indisponível: ",
                ["VoiceError"] = "ERRO DE VOZ",
                ["EnableOnlineSpeech"] =
                    "ative o Reconhecimento de fala online em Configurações > " +
                    "Privacidade e segurança > Fala. ({0})",
                ["MicrophoneAccessDenied"] =
                    "acesso negado. Libere o FSChecklist em Configurações > " +
                    "Privacidade e segurança > Microfone. ({0})",
                ["Listening"] = "OUVINDO...",
                ["MicrophoneListening"] = "Microfone ouvindo...",
                ["SettingsTitle"] = "Configurações",
                ["InterfaceLanguage"] = "Idioma da interface",
                ["Portuguese"] = "Português",
                ["English"] = "Inglês",
                ["InputMicrophone"] = "Microfone de entrada",
                ["LoadingMicrophones"] = "Carregando microfones...",
                ["DefaultMicrophoneNote"] =
                    "A seleção altera o microfone padrão de entrada do Windows.",
                ["Shortcut"] = "Atalho para iniciar checklist",
                ["ChangeShortcut"] = "ALTERAR ATALHO",
                ["Save"] = "SALVAR",
                ["Cancel"] = "CANCELAR",
                ["SettingsSaved"] = "Configurações salvas.",
                ["SettingsFailure"] = "Não foi possível aplicar as configurações: {0}",
                ["ErrorTitle"] = "Ocorreu um erro",
                ["Understood"] = "Entendido",
                ["CaptureShortcutTitle"] = "Novo atalho",
                ["CaptureShortcutInstruction"] =
                    "Pressione a tecla ou combinação desejada.",
                ["CaptureShortcutWaiting"] = "Aguardando atalho...",
                ["UseShortcut"] = "USAR ESTE ATALHO",
                ["InvalidShortcut"] = "Escolha uma tecla diferente de Ctrl, Alt ou Shift.",
                ["DefaultDevice"] = "{0} (padrão)"
            };

        private static readonly Dictionary<string, string> English =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Aircraft"] = "Aircraft",
                ["Checklist"] = "Checklist",
                ["Settings"] = "Settings",
                ["StartButton"] = "START OR {0}",
                ["NoChecklistStarted"] = "No checklist started",
                ["Ready"] = "READY",
                ["SelectChecklist"] = "Select an aircraft and a checklist",
                ["WaitingStart"] = "Waiting to start",
                ["MicrophoneOff"] = "Microphone off",
                ["LoadingChecklists"] = "Loading checklists...",
                ["ForceCheckTip"] = "Manually confirm the current item",
                ["FinishTip"] = "End the current checklist",
                ["SpeechInitializing"] = "Recognition: initializing...",
                ["SpeechUnavailable"] = "Speech recognition unavailable",
                ["MicrophoneStopped"] = "MICROPHONE STOPPED",
                ["RecognitionReady"] =
                    "{0} recognition ready. Microphone: {1}.",
                ["RecognitionUnavailable"] = "Voice unavailable: {0}",
                ["RecognitionLanguageUnavailable"] =
                    "Windows does not provide recognition for {0}.",
                ["RecognitionPrepareFailure"] =
                    "Could not prepare recognition: {0}",
                ["NoDefaultMicrophone"] = "no default device",
                ["GenericDefaultMicrophone"] = "default device",
                ["HotkeyUnavailable"] = "Global {0} unavailable: {1}",
                ["HotkeyActive"] = "Global {0} active.",
                ["ListeningEnded"] =
                    "Windows stopped listening. Press {0} to restart.",
                ["NoChecklistFound"] = "No checklist found.",
                ["PressToStart"] = "Press {0} to start the complete cycle",
                ["ItemCount"] = "{0} items",
                ["StartingChecklist"] = "Starting checklist...",
                ["Starting"] = "STARTING",
                ["StartFailure"] = "Could not start checklist: ",
                ["CopilotSpeaking"] = "Microphone open - copilot speaking",
                ["Callout"] = "CALLOUT",
                ["RecognitionFailure"] = "Could not recognize response: ",
                ["AnyResponse"] = "Any recognized response confirms the item",
                ["NoValidResponse"] = "No valid response configured",
                ["ExpectedResponse"] = "Expected response: {0}",
                ["ItemProgress"] = "Item {0} of {1}",
                ["WaitingReadback"] = "Waiting for your readback...",
                ["Checked"] = "CHECKED",
                ["Readback"] = "READBACK",
                ["SpeechDetected"] = "SPEECH DETECTED",
                ["Detected"] = "Detected: {0}",
                ["SoundDetected"] = "SOUND DETECTED",
                ["ContinueSpeaking"] =
                    "The microphone detected audio; keep speaking.",
                ["SoundDetectedContinue"] = "Sound detected - keep speaking",
                ["ProcessingSpeech"] = "PROCESSING SPEECH",
                ["ConvertingSpeech"] = "Audio received; converting to text...",
                ["Processing"] = "Processing speech...",
                ["NoSpeech"] = "No speech recognized. Try again.",
                ["Heard"] = "Heard: {0} ({1})",
                ["NotConfirmedText"] = "Not confirmed: \"{0}\". Try again.",
                ["Confirmed"] = "CONFIRMED",
                ["NotConfirmed"] = "NOT CONFIRMED",
                ["ManualCheck"] = "MANUAL CHECK",
                ["ManuallyConfirmed"] = "Item manually confirmed.",
                ["NoNextChecklist"] = "No next checklist configured",
                ["NextChecklist"] = "Next checklist: {0}",
                ["ChecklistManuallyEnded"] = "Checklist manually ended.",
                ["ChecklistCompleted"] = "Checklist completed.",
                ["ChecklistCompletedTitle"] = "{0} complete",
                ["Ended"] = "ENDED",
                ["Complete"] = "COMPLETE",
                ["CurrentChecklist"] = "Current checklist: {0}.",
                ["AllComplete"] = "{0} complete. End of checklists.",
                ["MicrophoneUnavailable"] = "Microphone unavailable: ",
                ["VoiceError"] = "VOICE ERROR",
                ["EnableOnlineSpeech"] =
                    "enable online speech recognition in Settings > Privacy & " +
                    "security > Speech. ({0})",
                ["MicrophoneAccessDenied"] =
                    "access denied. Allow FSChecklist in Settings > Privacy & " +
                    "security > Microphone. ({0})",
                ["Listening"] = "LISTENING...",
                ["MicrophoneListening"] = "Microphone listening...",
                ["SettingsTitle"] = "Settings",
                ["InterfaceLanguage"] = "Interface language",
                ["Portuguese"] = "Portuguese",
                ["English"] = "English",
                ["InputMicrophone"] = "Input microphone",
                ["LoadingMicrophones"] = "Loading microphones...",
                ["DefaultMicrophoneNote"] =
                    "This selection changes the default Windows input microphone.",
                ["Shortcut"] = "Checklist start shortcut",
                ["ChangeShortcut"] = "CHANGE SHORTCUT",
                ["Save"] = "SAVE",
                ["Cancel"] = "CANCEL",
                ["SettingsSaved"] = "Settings saved.",
                ["SettingsFailure"] = "Could not apply settings: {0}",
                ["ErrorTitle"] = "An error occurred",
                ["Understood"] = "Understood",
                ["CaptureShortcutTitle"] = "New shortcut",
                ["CaptureShortcutInstruction"] =
                    "Press the desired key or key combination.",
                ["CaptureShortcutWaiting"] = "Waiting for shortcut...",
                ["UseShortcut"] = "USE THIS SHORTCUT",
                ["InvalidShortcut"] =
                    "Choose a key other than Ctrl, Alt, or Shift.",
                ["DefaultDevice"] = "{0} (default)"
            };

        public string Language { get; private set; }

        public AppLocalizer(string language)
        {
            SetLanguage(language);
        }

        public void SetLanguage(string language)
        {
            Language = string.Equals(
                language,
                "en-US",
                StringComparison.OrdinalIgnoreCase)
                ? "en-US"
                : "pt-BR";
        }

        public string Get(string key)
        {
            Dictionary<string, string> values =
                Language == "en-US" ? English : Portuguese;
            string value;
            return values.TryGetValue(key, out value) ? value : key;
        }

        public string Format(string key, params object[] arguments)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                Get(key),
                arguments);
        }
    }
}
