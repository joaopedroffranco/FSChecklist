using System.Drawing;
using System.Windows.Forms;
using FSChecklist.Features.Localization;

namespace FSChecklist.Features.Errors
{
    internal static class ErrorDialog
    {
        public static void Show(
            IWin32Window owner,
            string message,
            IAppLocalizer localizer)
        {
            using (var dialog = new Form())
            {
                dialog.Text = localizer.Get("ErrorTitle");
                dialog.ClientSize = new Size(500, 235);
                dialog.MinimumSize = dialog.MaximumSize = dialog.Size;
                dialog.StartPosition = owner == null
                    ? FormStartPosition.CenterScreen
                    : FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.BackColor = Color.FromArgb(16, 20, 27);
                dialog.ForeColor = Color.White;
                dialog.Font = new Font("Segoe UI", 10F);

                var title = new Label
                {
                    Text = localizer.Get("ErrorTitle"),
                    Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(216, 75, 85),
                    BackColor = Color.Transparent
                };
                title.SetBounds(25, 20, 450, 38);

                var text = new TextBox
                {
                    Text = message,
                    ReadOnly = true,
                    Multiline = true,
                    BorderStyle = BorderStyle.None,
                    BackColor = Color.FromArgb(24, 32, 43),
                    ForeColor = Color.White,
                    ScrollBars = ScrollBars.Vertical,
                    TabStop = false
                };
                text.SetBounds(25, 67, 450, 88);

                var understoodButton = new Button
                {
                    Text = localizer.Get("Understood"),
                    BackColor = Color.FromArgb(35, 116, 225),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    DialogResult = DialogResult.OK
                };
                understoodButton.FlatAppearance.BorderSize = 0;
                understoodButton.SetBounds(295, 174, 180, 40);

                dialog.AcceptButton = understoodButton;
                dialog.CancelButton = understoodButton;
                dialog.Controls.AddRange(new Control[]
                {
                    title, text, understoodButton
                });
                dialog.ShowDialog(owner);
            }
        }
    }
}
