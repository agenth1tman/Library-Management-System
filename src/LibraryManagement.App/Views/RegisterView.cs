using LibraryManagement.App.Ui;
using LibraryManagement.App.Ui.Controls;
using LibraryManagement.Models;

namespace LibraryManagement.App.Views;

/// <summary>
/// Adds new copies to the catalogue. Books registered today are listed alongside with
/// their numbers and catalogue IDs, because the next job is writing those on the labels.
/// </summary>
internal sealed class RegisterView : ViewBase
{
    private readonly Label _nextNumber = Make.Subtitle();
    private readonly FormField _title = new("Title", new InputBox { MaxLength = 200 });
    private readonly FormField _author = new("Author", new InputBox { MaxLength = 200 });
    private readonly FormField _barcode = new("Barcode", new InputBox("Scan it, or type the number") { MaxLength = 64 });
    private readonly Label _result = Make.Text(string.Empty, Theme.Body, Theme.Available, wrap: true);
    private readonly Label _noneToday = Make.Text("Books you register today are listed here, ready for labelling.", Theme.Body, Theme.InkMuted);
    private readonly BookGrid _today = new(
        "Registered today",
        BookColumn.Id, BookColumn.CatalogId, BookColumn.Title, BookColumn.Author);

    public RegisterView(IShell shell)
        : base(shell)
    {
        var register = new AppButton("Register book", ButtonKind.Primary);
        register.Click += async (_, _) => await RegisterAsync();

        var clear = new AppButton("Clear");
        clear.Click += (_, _) => ClearForm();

        var buttons = Make.Flow(8, register, clear);
        buttons.Margin = new Padding(0, 4, 0, 12);

        var form = Make.Stack(_title, _author, _barcode, buttons, _result);
        var registeredToday = Make.Section("Registered today", Make.GridWithEmptyState(_today, _noneToday), fill: true);

        Compose(
            (Make.Header("Register a book", _nextNumber), false),
            (Make.Columns(40, (form, 36), (registeredToday, 64)), true));

        // Enter moves down the form; on the last field it submits (scanners end with Enter).
        _title.Input.EnterPressed += (_, _) => _author.Input.FocusInput();
        _author.Input.EnterPressed += (_, _) => _barcode.Input.FocusInput();
        _barcode.Input.EnterPressed += async (_, _) => await RegisterAsync();
        _today.BookActivated += (_, book) => Shell.OpenBook(book);

        Store.Changed += (_, _) => Render();
        Render();
    }

    public override void OnNavigatedTo() => _title.Input.FocusInput();

    private async Task RegisterAsync()
    {
        _result.Text = string.Empty;

        // Check every field so all problems show at once, then put the cursor in the first.
        FormField[] fields = [_title, _author, _barcode];
        string[] messages = ["Enter the book’s title.", "Enter the author’s name.", "Scan or type the book’s barcode."];
        var invalid = fields.Where((field, index) => !field.Require(messages[index])).ToList();
        if (invalid.Count > 0)
        {
            invalid[0].Input.FocusInput();
            return;
        }

        Book? registered = null;
        var succeeded = await Shell.RunAsync(async () =>
            registered = await Store.RegisterBookAsync(_title.Input.Text, _author.Input.Text, _barcode.Input.Text));

        if (succeeded && registered is not null)
        {
            ClearForm();
            _result.Text = $"Registered “{registered.Title}” as book {registered.Id}, catalogue ID {registered.CatalogId}.";
        }
    }

    private void ClearForm()
    {
        foreach (var field in new[] { _title, _author, _barcode })
        {
            field.Input.Text = string.Empty;
            field.Error = null;
        }

        _result.Text = string.Empty;
        _title.Input.FocusInput();
    }

    private void Render()
    {
        _nextNumber.Text = $"The next book will be number {Store.NextBookId}.";

        var today = Store.Today;
        var registeredToday = Store.Books
            .Where(book => book.RegisteredOn == today)
            .OrderByDescending(book => book.NumericId ?? int.MinValue)
            .ToList();

        _today.SetBooks(registeredToday, today);
        _today.Visible = registeredToday.Count > 0;
        _noneToday.Visible = registeredToday.Count == 0;
    }
}
