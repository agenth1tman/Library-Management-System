namespace LibraryManagement.App.Ui;

internal static class Dialogs
{
    private const string AppTitle = "Library Management System";

    public static bool Confirm(IWin32Window owner, string question, string confirmCaption = AppTitle) =>
        MessageBox.Show(
            owner,
            question,
            confirmCaption,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) == DialogResult.Yes;

    public static void Error(IWin32Window owner, string message) =>
        MessageBox.Show(owner, message, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
}
