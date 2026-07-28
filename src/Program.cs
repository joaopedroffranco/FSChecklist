using System;
using System.Windows.Forms;
using FSChecklist.Domain.Settings;
using FSChecklist.Features.AudioInput;
using FSChecklist.Features.Errors;
using FSChecklist.Features.FlightCallouts;
using FSChecklist.Features.Input;
using FSChecklist.Features.Localization;
using FSChecklist.Features.Main;
using FSChecklist.Features.Repository;
using FSChecklist.Features.Settings;
using FSChecklist.Features.Simulator;
using FSChecklist.Features.SpeechRecognition;
using FSChecklist.Integrations.Localization;
using FSChecklist.Integrations.SimConnect;
using FSChecklist.Integrations.Settings;
using FSChecklist.Integrations.WindowsAudio;
using FSChecklist.Integrations.WindowsInput;
using FSChecklist.Integrations.WindowsSpeech;

namespace FSChecklist
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            IAppSettingsRepository settingsRepository =
                new JsonAppSettingsRepository();
            AppSettings settings = settingsRepository.Load();
            IAppLocalizer localizer =
                new AppLocalizer(settings.UiLanguage);
            IAudioInputDeviceService audioInput =
                new WindowsAudioInputDeviceService();
            Application.SetUnhandledExceptionMode(
                UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs args)
            {
                ErrorDialog.Show(
                    null,
                    args.Exception.GetBaseException().Message,
                    localizer);
            };

            IGlobalPushToTalk pushToTalk = null;
            string hotkeyError = string.Empty;
            try
            {
                pushToTalk = new WindowsGlobalPushToTalk(settings.Hotkey);
            }
            catch (Exception error)
            {
                hotkeyError = error.Message;
            }

            IChecklistRepository repository = new JsonChecklistRepository();
            ISpeechRecognitionService recognition =
                new WindowsSpeechRecognitionService("en-US", localizer);
            ISpeechSynthesisService synthesis = new WindowsSpeechSynthesisService();
            ISimulatorConnection simulator = new SimConnectConnection();
            IFlightCalloutService flightCallouts =
                new FlightCalloutService(
                    simulator,
                    new WindowsSpeechSynthesisService());

            try
            {
                Application.Run(new MainForm(
                    repository,
                    recognition,
                    synthesis,
                    pushToTalk,
                    hotkeyError,
                    settingsRepository,
                    settings,
                    localizer,
                    audioInput,
                    simulator,
                    flightCallouts));
            }
            catch (Exception error)
            {
                ErrorDialog.Show(
                    null,
                    error.GetBaseException().Message,
                    localizer);
            }
        }
    }
}
