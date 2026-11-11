using LibraryManagement.App.Infrastructure;
using LibraryManagement.App.Ui;

namespace LibraryManagement.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Anything that slips past the normal error handling is logged and shown,
        // instead of the app silently disappearing.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ReportUnexpected(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log.Error(e.ExceptionObject as Exception);

        ApplicationConfiguration.Initialize();
        AppFonts.Load();

        using var form = new MainForm(StartupOptions.Parse(args));
        Application.Run(form);
    }

    private static void ReportUnexpected(Exception exception)
    {
        Log.Error(exception);
        MessageBox.Show(
            $"Something unexpected went wrong:\n\n{exception.Message}\n\nDetails were saved to:\n{Log.FilePath}",
            "Library Management System",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
