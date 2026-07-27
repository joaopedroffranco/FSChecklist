using FSChecklist.Features.Errors;

namespace FSChecklist.Features.Main
{
    internal sealed partial class MainForm
    {
        private void ShowError(string message)
        {
            ErrorDialog.Show(this, message, localizer);
        }
    }
}
