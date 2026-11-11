using LibraryManagement.App.Ui;
using LibraryManagement.App.Ui.Controls;
using LibraryManagement.Data;

namespace LibraryManagement.App.Views;

/// <summary>
/// Shown when the app has no working connection to a library yet: on first run, or when
/// connecting failed. Replaces the old behaviour of showing an error box and quitting.
/// </summary>
internal sealed class SetupView : ViewBase
{
    private const string Introduction =
        "Books and members are kept in your school’s Firebase project. Choose the service-account key file " +
        "(.json) for it. An administrator can download one from Firebase console → Project settings → Service accounts.";

    private readonly Label _title = Make.Title("Connect the library database");
    private readonly Label _message = Make.Text(Introduction, Theme.Body, Theme.Ink, wrap: true);
    private readonly Label _error = Make.Text(string.Empty, Theme.Strong, Theme.Overdue, wrap: true);
    private readonly AppButton _chooseKey = new("Choose key file…", ButtonKind.Primary);
    private readonly AppButton _useSample = new("Use sample data");

    public SetupView(IShell shell)
        : base(shell)
    {
        _message.Margin = new Padding(1, 10, 0, 0);
        _error.Margin = new Padding(1, 12, 0, 0);
        _error.Visible = false;

        var buttons = Make.Flow(8, _chooseKey, _useSample);
        buttons.Margin = new Padding(0, 18, 0, 0);

        var tip = Make.Text(
            $"A key saved next to the app as {CredentialLocator.DefaultFileName}, or named by the " +
            $"{CredentialLocator.EnvironmentVariable} environment variable, is picked up without asking. " +
            "The key gives full access to the library’s data: keep it private.",
            Theme.Small,
            Theme.InkMuted,
            wrap: true);
        tip.Margin = new Padding(1, 22, 0, 0);

        // A fixed-width column keeps the lines at a comfortable reading length.
        var column = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Dock = DockStyle.Fill };
        column.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 560));
        column.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        column.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        column.Controls.Add(Make.Stack(_title, _message, _error, buttons, tip), 0, 0);
        Controls.Add(column);

        _chooseKey.Click += async (_, _) => await Shell.ChooseKeyFileAsync();
        _useSample.Click += async (_, _) => await Shell.UseSampleDataAsync();
    }

    public void ShowConnecting()
    {
        _title.Text = "Connecting…";
        _message.Text = "Loading books and members from the library database.";
        _error.Visible = false;
        SetButtonsEnabled(false);
    }

    public void ShowPrompt(string? error)
    {
        _title.Text = error is null ? "Connect the library database" : "Couldn’t connect";
        _message.Text = Introduction;
        _error.Text = error ?? string.Empty;
        _error.Visible = error is not null;
        SetButtonsEnabled(true);
    }

    private void SetButtonsEnabled(bool enabled)
    {
        _chooseKey.Enabled = enabled;
        _useSample.Enabled = enabled;
    }
}
