namespace LibraryManagement.Configuration;

/// <summary>Per-user preferences, saved as JSON under the user's AppData folder.</summary>
public sealed class AppSettings
{
    /// <summary>Open the daily report (instead of the dashboard) the first time the app starts each day.</summary>
    public bool ShowDailyReportOnStartup { get; set; }

    /// <summary>Write the daily report to <see cref="ReportsFolder"/> once a day, without asking.</summary>
    public bool AutoSaveDailyReport { get; set; }

    /// <summary>Path of the Firebase service-account key chosen in the app, if any.</summary>
    public string? CredentialsPath { get; set; }

    /// <summary>Where reports are saved. <c>null</c> means "Documents\Library Management System".</summary>
    public string? ReportsFolder { get; set; }

    public DateOnly? LastReportShownOn { get; set; }

    public DateOnly? LastReportSavedOn { get; set; }

    /// <summary>Where the main window was when the app last closed, so it reopens the same way.</summary>
    public WindowPlacement? Window { get; set; }
}

/// <summary>The main window's position and size in screen pixels (its un-maximised bounds).</summary>
public sealed record WindowPlacement(int Left, int Top, int Width, int Height, bool Maximized);
