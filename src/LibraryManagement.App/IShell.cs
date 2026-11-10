using LibraryManagement.App.Ui.Controls;
using LibraryManagement.Configuration;
using LibraryManagement.Models;
using LibraryManagement.Services;

namespace LibraryManagement.App;

internal enum Page
{
    Dashboard,
    Library,
    Manage,
    Register,
    Reports,
    Settings,
}

/// <summary>What a screen may ask of the main window. Keeps the screens independent of each other.</summary>
internal interface IShell
{
    LibraryStore Store { get; }

    AppSettings Settings { get; }

    /// <summary>The folder reports are saved to (the user's choice, or Documents by default).</summary>
    string ReportsFolder { get; }

    /// <summary>The Firebase key file in use, or <c>null</c> when showing sample data.</summary>
    string? KeyFilePath { get; }

    void SaveSettings();

    void Navigate(Page page);

    /// <summary>Opens the Manage screen with <paramref name="book"/> loaded.</summary>
    void OpenBook(Book book);

    void Notify(string message, NoticeKind kind = NoticeKind.Success);

    /// <summary>
    /// Runs a database operation with the busy indicator on and errors reported to the user.
    /// Returns false when it failed (or another operation was already running).
    /// </summary>
    Task<bool> RunAsync(Func<Task> operation);

    /// <summary>Asks for a Firebase key file and connects with it.</summary>
    Task ChooseKeyFileAsync();

    Task UseSampleDataAsync();
}
