namespace LibraryManagement.App.Infrastructure;

/// <summary>Every folder the app uses, resolved per user at run time. Nothing is hard-coded.</summary>
internal static class AppPaths
{
    private const string AppFolderName = "Library Management System";

    /// <summary><c>%AppData%\Library Management System</c>: settings and logs.</summary>
    public static string DataFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppFolderName);

    public static string SettingsFile { get; } = Path.Combine(DataFolder, "settings.json");

    public static string LogFile { get; } = Path.Combine(DataFolder, "logs", "app.log");

    /// <summary><c>Documents\Library Management System</c>: where reports go unless changed in Settings.</summary>
    public static string DefaultReportsFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        AppFolderName);
}
