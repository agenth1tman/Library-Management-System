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
        "Books and members are stored in your school's Firebase project. To connect, choose the " +
        "service-account key file (.json) downloaded from Firebase console → Project settings → Service accounts.";

    private readonly Label _title = Make.Text("Connect your library", Theme.Title, Theme.Text);
    private readonly Label _message;
    private readonly Label _error;
    private readonly AppButton _chooseKey = new("Choose key file…", ButtonKind.Primary);
    private readonly AppButton _useSample = new("Explore with sample data");

    public SetupView(IShell shell)
        : base(shell)
    {
        _message = Make.Text(Introduction, Theme.Body, Theme.TextMuted, wrap: true);
        _message.Margin = new Padding(1, 10, 0, 0);

        _error = Make.Text(string.Empty, Theme.BodyStrong, Theme.Danger, wrap: true);
        _error.Margin = new Padding(1, 14, 0, 0);
        _error.Visible = false;

        var buttons = Make.Flow(10, _chooseKey, _useSample);
        buttons.Margin = new Padding(0, 22, 0, 0);

        var tip = Make.Text(
            $"Tip: a key saved next to the app as {CredentialLocator.DefaultFileName}, or pointed to by the " +
            $"{CredentialLocator.EnvironmentVariable} environment variable, is picked up automatically. " +
            "Keep the key private and never commit it to Git.",
            Theme.Small,
            Theme.TextDisabled,
            wrap: true);
        tip.Margin = new Padding(1, 22, 0, 0);

        var crest = new PictureBox
        {
            Image = EmbeddedAssets.Crest,
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(72, 72),
            Margin = new Padding(0, 0, 0, 16),
            Anchor = AnchorStyles.Left,
        };

        var content = Make.Stack(_title, _message, _error, buttons, tip);
        var stack = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        stack.Controls.Add(crest);
        stack.Controls.Add(content);

        var card = new Card { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(32), Dock = DockStyle.Fill };
        card.Controls.Add(stack);

        // A 3×3 grid with the card in the middle cell keeps it centred at any window size.
        var centre = new TableLayoutPanel { ColumnCount = 3, RowCount = 3, Dock = DockStyle.Fill };
        centre.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        centre.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 600));
        centre.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        centre.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        centre.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        centre.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        centre.Controls.Add(card, 1, 1);
        Controls.Add(centre);

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
        _title.Text = error is null ? "Connect your library" : "Couldn't connect";
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
