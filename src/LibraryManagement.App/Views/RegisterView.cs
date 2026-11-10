using LibraryManagement.App.Ui;
using LibraryManagement.App.Ui.Controls;
using LibraryManagement.Models;

namespace LibraryManagement.App.Views;

/// <summary>Adds a new copy to the catalogue.</summary>
internal sealed class RegisterView : ViewBase
{
    private readonly InputBox _title = new("e.g. To Kill a Mockingbird") { MaxLength = 200 };
    private readonly InputBox _author = new("e.g. Harper Lee") { MaxLength = 200 };
    private readonly InputBox _barcode = new("Scan the barcode or type it") { MaxLength = 64 };

    public RegisterView(IShell shell)
        : base(shell)
    {
        var register = new AppButton("Register book", ButtonKind.Primary);
        register.Click += async (_, _) => await RegisterAsync();

        var clear = new AppButton("Clear");
        clear.Click += (_, _) => ClearForm();

        var note = Make.Text(
            "The book ID and catalogue ID are generated automatically. A scanner can be used for the barcode.",
            Theme.Small,
            Theme.TextMuted,
            wrap: true);
        note.Margin = new Padding(1, 0, 0, Theme.Gap + 2);

        var form = Make.Stack(
            Make.Field("Title", _title),
            Make.Field("Author", _author),
            Make.Field("Barcode", _barcode),
            note,
            Make.Flow(10, register, clear));

        var card = new Card { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(24) };
        card.Controls.Add(form);

        var row = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 600));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.Dock = DockStyle.Fill;
        row.Controls.Add(card, 0, 0);

        Compose(
            (Make.Header("Register a book", Make.Subtitle("Add a new copy to the catalogue.")), false),
            (row, false),
            (new Panel(), true));

        // Enter moves down the form; on the last field it submits (scanners end with Enter).
        _title.EnterPressed += (_, _) => _author.FocusInput();
        _author.EnterPressed += (_, _) => _barcode.FocusInput();
        _barcode.EnterPressed += async (_, _) => await RegisterAsync();
    }

    public override void OnNavigatedTo() => _title.FocusInput();

    private async Task RegisterAsync()
    {
        Book? registered = null;
        var succeeded = await Shell.RunAsync(
            async () => registered = await Store.RegisterBookAsync(_title.Text, _author.Text, _barcode.Text));

        if (succeeded && registered is not null)
        {
            Shell.Notify($"Registered “{registered.Title}” as book {registered.Id} ({registered.CatalogId}).");
            ClearForm();
        }
        else
        {
            // Put the cursor where the problem most likely is.
            InputBox[] fields = [_title, _author, _barcode];
            (fields.FirstOrDefault(field => string.IsNullOrWhiteSpace(field.Text)) ?? _title).FocusInput();
        }
    }

    private void ClearForm()
    {
        _title.Text = string.Empty;
        _author.Text = string.Empty;
        _barcode.Text = string.Empty;
        _title.FocusInput();
    }
}
