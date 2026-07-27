using System;
using System.Windows.Forms;
using FSChecklist.Features.Input;
using FSChecklist.Features.Main;
using FSChecklist.Features.Repository;
using FSChecklist.Features.SpeechRecognition;
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

            IGlobalPushToTalk pushToTalk = null;
            string hotkeyError = string.Empty;
            try
            {
                pushToTalk = new WindowsGlobalPushToTalk();
            }
            catch (Exception error)
            {
                hotkeyError = error.Message;
            }

            IChecklistRepository repository = new JsonChecklistRepository();
            ISpeechRecognitionService recognition =
                new WindowsSpeechRecognitionService("pt-BR");
            ISpeechSynthesisService synthesis = new WindowsSpeechSynthesisService();

            Application.Run(new MainForm(
                repository, recognition, synthesis, pushToTalk, hotkeyError));
        }
    }
}
