using System.Globalization;
using LibraryManagement.App.Ui;
using LibraryManagement.App.Ui.Controls;
using LibraryManagement.Models;

namespace LibraryManagement.App.Views;

/// <summary>
/// The issue-and-return desk. Lending is the main job here, so it gets the wider column and
/// the screen's one primary button; editing the book's record sits quietly beside it.
/// </summary>
internal sealed class ManageView : ViewBase
{
    private const string DateFormat = "d MMM yyyy";
    private const string Hint = "Scan a book, or type its number, catalogue ID or barcode and press Enter.";

    private readonly InputBox _lookup = new("Book number, catalogue ID or barcode") { Caption = "Find a book" };
    private readonly Label _message = Make.Text(Hint, Theme.Body, Theme.InkMuted, wrap: true);
    private readonly ColumnsPanel _detail;

    // The book and its loan.
    private readonly Label _bookTitle = Make.Text(string.Empty, Theme.BookTitle, Theme.Ink, wrap: true);
    private readonly Label _bookFacts = Make.Text(string.Empty, Theme.Body, Theme.InkMuted, wrap: true);
    private readonly Label _state = Make.Text(string.Empty, Theme.Figure, Theme.Ink);
    private readonly TableLayoutPanel _issuePanel;
    private readonly TableLayoutPanel _returnPanel;
    private readonly FormField _member = new("Borrower’s member ID", new InputBox("e.g. BTS-10-214") { MaxLength = 32 });
    private readonly Label _memberPreview = Make.Text(string.Empty, Theme.Body, Theme.Ink, wrap: true);
    private readonly Label _borrower = Make.Text(string.Empty, Theme.Strong, Theme.Ink, wrap: true);
    private readonly Label _borrowerRole = Make.Text(string.Empty, Theme.Body, Theme.InkMuted, wrap: true);
    private readonly Label _dates = Make.Text(string.Empty, Theme.Body, Theme.InkMuted, wrap: true);

    // The book's record.
    private readonly FormField _title = new("Title", new InputBox { MaxLength = 200 });
    private readonly FormField _author = new("Author", new InputBox { MaxLength = 200 });
    private readonly FormField _barcode = new("Barcode", new InputBox { MaxLength = 64 });

    private string? _bookId;

    /// <summary>The record as last copied into the edit fields; edits are measured against it.</summary>
    private BookDetails? _shownRecord;

    public ManageView(IShell shell)
        : base(shell)
    {
        var find = new AppButton("Find book");
        find.Click += (_, _) => LookUp(_lookup.Text);
        _lookup.EnterPressed += (_, _) => LookUp(_lookup.Text);

        var lookupRow = new TableLayoutPanel { ColumnCount = 3, RowCount = 1, Height = Theme.ControlHeight, Margin = new Padding(0, 0, 0, 8) };
        lookupRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380));
        lookupRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        lookupRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        lookupRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _lookup.Dock = DockStyle.Fill;
        find.Margin = new Padding(8, 0, 0, 0);
        lookupRow.Controls.Add(_lookup, 0, 0);
        lookupRow.Controls.Add(find, 1, 0);

        _message.Margin = new Padding(1, 0, 0, 18);

        _issuePanel = BuildIssuePanel();
        _returnPanel = BuildReturnPanel();

        _bookFacts.Margin = new Padding(1, 2, 0, 0);
        _state.Margin = new Padding(0, 0, 0, 10);
        var loan = Make.Stack(_bookTitle, _bookFacts, new Divider { Margin = new Padding(0, 14, 0, 14) }, _state, _issuePanel, _returnPanel);

        _detail = Make.Columns(48, (loan, 56), (BuildRecordSection(), 44));
        _detail.AutoSize = true;
        _detail.Visible = false;

        Compose(
            (Make.Header("Issue & return", subtitle: null), false),
            (lookupRow, false),
            (_message, false),
            (_detail, false),
            (new Panel(), true));

        Store.Changed += (_, _) => Render();
    }

    public override void OnNavigatedTo()
    {
        if (_bookId is not null && Store.GetBook(_bookId) is { IsAvailable: true })
        {
            _member.Input.FocusInput();
        }
        else
        {
            _lookup.FocusInput();
        }
    }

    /// <summary>Shows <paramref name="book"/> as if its number had been typed into the lookup box.</summary>
    public void Open(Book book)
    {
        if (book.Id != _bookId && !ConfirmDiscardEdits())
        {
            return;
        }

        _lookup.Text = book.Id;
        Show(book);
    }

    /// <summary>Finds a book from whatever was typed or scanned and shows it.</summary>
    public void LookUp(string? typed)
    {
        var query = typed?.Trim() ?? string.Empty;
        if (query.Length == 0)
        {
            return;
        }

        _lookup.Text = query;
        if (Store.FindBook(query) is not { } book)
        {
            Say($"No book has the number, catalogue ID or barcode “{query}”. Check it and try again.", Theme.Overdue);
            _lookup.FocusInput();
            return;
        }

        if (book.Id != _bookId && !ConfirmDiscardEdits())
        {
            return;
        }

        Show(book);
        if (book.IsAvailable)
        {
            _member.Input.FocusInput();
        }
    }

    /// <summary>
    /// Returns true when it is safe to leave the current book: nothing was edited, or the
    /// user agreed to throw the edits away.
    /// </summary>
    public bool ConfirmDiscardEdits()
    {
        if (_bookId is null || _shownRecord is null || EditedRecord() == _shownRecord)
        {
            return true;
        }

        if (!Dialogs.Confirm(FindForm()!, $"You changed the record of “{_shownRecord.Title}” but have not saved it.\n\nDiscard the changes?", "Unsaved changes"))
        {
            return false;
        }

        ShowRecord(_shownRecord);
        return true;
    }

    private TableLayoutPanel BuildRecordSection()
    {
        var save = new AppButton("Save changes");
        save.Click += async (_, _) => await SaveAsync();

        var delete = new AppButton("Delete book…", ButtonKind.Danger);
        delete.Click += async (_, _) => await DeleteAsync();

        var buttons = Make.Flow(8, save, delete);
        buttons.Margin = new Padding(0, 4, 0, 0);

        return Make.Section("Book record", Make.Stack(_title, _author, _barcode, buttons));
    }

    private TableLayoutPanel BuildIssuePanel()
    {
        var issue = new AppButton("Issue book", ButtonKind.Primary);
        issue.Click += async (_, _) => await IssueAsync();

        _memberPreview.Margin = new Padding(1, 0, 0, 12);
        _member.Margin = new Padding(0, 0, 0, 8);
        _member.Input.TextChanged += (_, _) => RenderMemberPreview();
        _member.Input.EnterPressed += async (_, _) => await IssueAsync();

        return Make.Stack(_member, _memberPreview, Make.Flow(0, issue));
    }

    private TableLayoutPanel BuildReturnPanel()
    {
        var returnBook = new AppButton("Mark as returned", ButtonKind.Primary);
        returnBook.Click += async (_, _) => await ReturnAsync();

        _borrowerRole.Margin = new Padding(0, 1, 0, 0);
        _dates.Margin = new Padding(0, 1, 0, 14);

        return Make.Stack(_borrower, _borrowerRole, _dates, Make.Flow(0, returnBook));
    }

    private void Show(Book book)
    {
        _bookId = book.Id;
        _shownRecord = null;
        _member.Input.Text = string.Empty;
        _member.Error = null;
        Say(string.Empty, Theme.InkMuted);
        Render();
    }

    private void Render()
    {
        var book = _bookId is null ? null : Store.GetBook(_bookId);
        if (book is null)
        {
            // Nothing loaded yet, or the book was deleted (possibly on another computer).
            _bookId = null;
            _shownRecord = null;
            _detail.Visible = false;
            if (_message.Text.Length == 0)
            {
                Say(Hint, Theme.InkMuted);
            }

            return;
        }

        var today = Store.Today;
        _bookTitle.Text = book.Title;
        _bookFacts.Text = $"{Fallback(book.Author)}   ·   Book {book.Id}   ·   {Fallback(book.CatalogId)}   ·   Registered {Format(book.RegisteredOn)}";

        // Copy the record into the edit fields only when it really changed, so a refresh
        // (for example after issuing the book) does not wipe out half-typed edits.
        var record = new BookDetails(book.Title, book.Author, book.Barcode);
        if (record != _shownRecord)
        {
            ShowRecord(record);
        }

        (_state.Text, _state.ForeColor) = DescribeState(book, today);
        _issuePanel.Visible = book.IsAvailable;
        _returnPanel.Visible = !book.IsAvailable;

        if (book.Loan is { } loan)
        {
            _borrower.Text = Fallback(loan.MemberName);
            _borrowerRole.Text = $"{loan.RoleDescription}   ·   {Fallback(loan.MemberId)}";
            _dates.Text = $"Issued {Format(loan.IssuedOn)}, due back {Format(loan.DueOn)}";
        }
        else
        {
            RenderMemberPreview();
        }

        _detail.Visible = true;
    }

    private void ShowRecord(BookDetails record)
    {
        _shownRecord = record;
        _title.Input.Text = record.Title;
        _author.Input.Text = record.Author;
        _barcode.Input.Text = record.Barcode;
        _title.Error = _author.Error = _barcode.Error = null;
    }

    private BookDetails EditedRecord() =>
        new(_title.Input.Text.Trim(), _author.Input.Text.Trim(), _barcode.Input.Text.Trim());

    private void RenderMemberPreview()
    {
        var member = Store.FindMember(_member.Input.Text);
        _memberPreview.Text = member is null
            ? string.Empty
            : $"{member.Name}, {member.RoleDescription}. Due back {Format(LoanPolicy.DueDateFor(member.Role, Store.Today))}.";
        _memberPreview.Visible = member is not null;
    }

    private static (string Text, Color Color) DescribeState(Book book, DateOnly today)
    {
        if (book.Loan is not { } loan)
        {
            return ("On the shelf", Theme.Available);
        }

        if (loan.DueOn is not { } due)
        {
            return ("On loan", Theme.Ink);
        }

        var days = due.DayNumber - today.DayNumber;
        return days switch
        {
            < -1 => ($"Overdue by {-days} days", Theme.Overdue),
            -1 => ("Overdue by 1 day", Theme.Overdue),
            0 => ("On loan, due back today", Theme.DueSoon),
            1 => ("On loan, due back tomorrow", Theme.DueSoon),
            <= LoanPolicy.DueSoonWindowDays => ($"On loan, due back in {days} days", Theme.DueSoon),
            _ => ("On loan", Theme.Ink),
        };
    }

    private async Task IssueAsync()
    {
        if (_bookId is not { } bookId)
        {
            return;
        }

        var typed = _member.Input.Text.Trim();
        if (!_member.Require("Enter the borrower’s member ID."))
        {
            _member.Input.FocusInput();
            return;
        }

        if (Store.FindMember(typed) is null)
        {
            _member.Error = "No member has this ID. Check it against the member’s card.";
            _member.Input.FocusInput();
            return;
        }

        Loan? loan = null;
        if (await Shell.RunAsync(async () => loan = await Store.IssueBookAsync(bookId, typed)) && loan is not null)
        {
            _member.Input.Text = string.Empty;
            Say($"Issued to {loan.MemberName}. Due back {Format(loan.DueOn)}.", Theme.Available);
            _lookup.FocusInput();
        }
    }

    private async Task ReturnAsync()
    {
        if (_bookId is not { } bookId || Store.GetBook(bookId) is not { } book)
        {
            return;
        }

        if (await Shell.RunAsync(() => Store.ReturnBookAsync(bookId)))
        {
            Say($"“{book.Title}” is back on the shelf.", Theme.Available);
            _lookup.FocusInput();
        }
    }

    private async Task SaveAsync()
    {
        if (_bookId is not { } bookId || _shownRecord is null)
        {
            return;
        }

        FormField[] fields = [_title, _author, _barcode];
        string[] messages = ["Enter the book’s title.", "Enter the author’s name.", "Scan or type the book’s barcode."];
        var invalid = fields.Where((field, index) => !field.Require(messages[index])).ToList();
        if (invalid.Count > 0)
        {
            invalid[0].Input.FocusInput();
            return;
        }

        var edited = EditedRecord();
        if (edited == _shownRecord)
        {
            Say("Nothing was changed, so there is nothing to save.", Theme.InkMuted);
            return;
        }

        if (await Shell.RunAsync(() => Store.UpdateBookAsync(bookId, edited)))
        {
            Say("Changes saved.", Theme.Available);
        }
    }

    private async Task DeleteAsync()
    {
        if (_bookId is not { } bookId || Store.GetBook(bookId) is not { } book)
        {
            return;
        }

        var question = book.Loan is { } loan
            ? $"“{book.Title}” is on loan to {loan.MemberName}.\n\nDelete it from the library anyway? This cannot be undone."
            : $"Delete “{book.Title}” (book {book.Id}) from the library?\n\nThis cannot be undone.";

        if (!Dialogs.Confirm(FindForm()!, question, "Delete book"))
        {
            return;
        }

        if (await Shell.RunAsync(() => Store.DeleteBookAsync(bookId)))
        {
            _lookup.Text = string.Empty;
            Say($"Deleted “{book.Title}”.", Theme.Available);
            _lookup.FocusInput();
        }
    }

    /// <summary>Shows one line under the lookup box: a hint, the result of the last action, or what went wrong.</summary>
    private void Say(string text, Color color)
    {
        _message.ForeColor = color;
        _message.Text = text;
        _message.AccessibleName = text;
    }

    private static string Format(DateOnly? date) =>
        date?.ToString(DateFormat, CultureInfo.CurrentCulture) ?? "not recorded";

    private static string Fallback(string text) => string.IsNullOrWhiteSpace(text) ? "not recorded" : text;
}
