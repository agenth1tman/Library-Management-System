using System.Globalization;
using LibraryManagement.App.Ui;
using LibraryManagement.App.Ui.Controls;
using LibraryManagement.Models;

namespace LibraryManagement.App.Views;

/// <summary>Look a book up, then issue, return, edit or delete it.</summary>
internal sealed class ManageView : ViewBase
{
    private const string DateFormat = "d MMM yyyy";

    private readonly InputBox _lookup = new("Book ID, catalogue ID or barcode");
    private readonly Label _hint;
    private readonly ColumnsPanel _detail;

    // Book details card.
    private readonly Label _status = Make.Text(string.Empty, Theme.BodyStrong, Theme.Text);
    private readonly InputBox _title = new() { MaxLength = 200 };
    private readonly InputBox _author = new() { MaxLength = 200 };
    private readonly InputBox _barcode = new() { MaxLength = 64 };
    private readonly Label _facts = Make.Text(string.Empty, Theme.Small, Theme.TextMuted, wrap: true);

    // Loan card, "on the shelf" state.
    private readonly TableLayoutPanel _issuePanel;
    private readonly InputBox _memberId = new("e.g. BTS-10-214");
    private readonly Label _memberPreview = Make.Text(string.Empty, Theme.Body, Theme.TextMuted, wrap: true);
    private readonly AppButton _issue = new("Issue book", ButtonKind.Primary);

    // Loan card, "on loan" state.
    private readonly TableLayoutPanel _returnPanel;
    private readonly Label _borrower = Make.Text(string.Empty, Theme.Large, Theme.Text);
    private readonly Label _borrowerRole = Make.Text(string.Empty, Theme.Body, Theme.TextMuted);
    private readonly Label _issuedOn = Make.Text(string.Empty, Theme.Body, Theme.Text);
    private readonly Label _dueOn = Make.Text(string.Empty, Theme.Body, Theme.Text);
    private readonly Label _dueNote = Make.Text(string.Empty, Theme.BodyStrong, Theme.Text);

    private string? _bookId;

    public ManageView(IShell shell)
        : base(shell)
    {
        var find = new AppButton("Find book", ButtonKind.Primary);
        find.Click += (_, _) => LookUp();
        _lookup.EnterPressed += (_, _) => LookUp();

        var lookupRow = new TableLayoutPanel { ColumnCount = 3, RowCount = 1, Height = 40, Margin = new Padding(0, 0, 0, Theme.Gap) };
        lookupRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380));
        lookupRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        lookupRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        lookupRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _lookup.Dock = DockStyle.Fill;
        find.Margin = new Padding(10, 0, 0, 0);
        lookupRow.Controls.Add(_lookup, 0, 0);
        lookupRow.Controls.Add(find, 1, 0);

        _hint = Make.Text(
            "Type or scan a book's ID, catalogue label or barcode, then press Enter.",
            Theme.Body,
            Theme.TextMuted);
        _hint.Margin = new Padding(2, 4, 0, 0);

        _issuePanel = BuildIssuePanel();
        _returnPanel = BuildReturnPanel();

        _detail = Make.Columns(Theme.Gap, (BuildDetailsCard(), 54), (BuildLoanCard(), 46));
        _detail.AutoSize = true;
        _detail.Visible = false;

        Compose(
            (Make.Header("Manage a book", Make.Subtitle("Issue, return, edit or remove a single copy.")), false),
            (lookupRow, false),
            (_hint, false),
            (_detail, false),
            (new Panel(), true));

        Store.Changed += (_, _) => Render();
    }

    public override void OnNavigatedTo()
    {
        if (_bookId is not null && Store.GetBook(_bookId) is { IsAvailable: true })
        {
            _memberId.FocusInput();
        }
        else
        {
            _lookup.FocusInput();
        }
    }

    /// <summary>Loads <paramref name="book"/> as if its ID had been typed into the lookup box.</summary>
    public void Open(Book book)
    {
        _lookup.Text = book.Id;
        _bookId = book.Id;
        _memberId.Text = string.Empty;
        Render();
    }

    private Card BuildDetailsCard()
    {
        var heading = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 0, 16) };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        heading.Controls.Add(Make.Heading("Book details"), 0, 0);
        _status.Anchor = AnchorStyles.Right;
        heading.Controls.Add(_status, 1, 0);

        var save = new AppButton("Save changes");
        save.Click += async (_, _) => await SaveAsync();

        var delete = new AppButton("Delete book", ButtonKind.Danger);
        delete.Click += async (_, _) => await DeleteAsync();

        _facts.Margin = new Padding(1, 0, 0, Theme.Gap + 2);

        var card = new Card { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(24) };
        card.Controls.Add(Make.Stack(
            heading,
            Make.Field("Title", _title),
            Make.Field("Author", _author),
            Make.Field("Barcode", _barcode),
            _facts,
            Make.Flow(10, save, delete)));
        return card;
    }

    private Card BuildLoanCard()
    {
        var heading = Make.Heading("Loan");
        heading.Margin = new Padding(0, 0, 0, 16);

        var card = new Card { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(24) };
        card.Controls.Add(Make.Stack(heading, _issuePanel, _returnPanel));
        return card;
    }

    private TableLayoutPanel BuildIssuePanel()
    {
        var intro = Make.Text("This copy is on the shelf. Enter the borrower's member ID to issue it.", Theme.Body, Theme.TextMuted, wrap: true);
        intro.Margin = new Padding(1, 0, 0, Theme.Gap);

        _memberPreview.Margin = new Padding(1, 0, 0, Theme.Gap + 2);
        _memberId.TextChanged += (_, _) => RenderMemberPreview();
        _memberId.EnterPressed += async (_, _) => await IssueAsync();
        _issue.Click += async (_, _) => await IssueAsync();

        return Make.Stack(intro, Make.Field("Member ID", _memberId), _memberPreview, Make.Flow(0, _issue));
    }

    private TableLayoutPanel BuildReturnPanel()
    {
        _borrowerRole.Margin = new Padding(1, 2, 0, Theme.Gap);

        var dates = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 0, 10) };
        dates.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        dates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        void AddRow(string caption, Label value)
        {
            var label = Make.Text(caption, Theme.Body, Theme.TextMuted);
            label.Margin = new Padding(1, 0, 0, 8);
            value.Margin = new Padding(0, 0, 0, 8);
            dates.Controls.Add(label);
            dates.Controls.Add(value);
        }

        AddRow("Issued", _issuedOn);
        AddRow("Due back", _dueOn);

        _dueNote.Margin = new Padding(1, 0, 0, Theme.Gap + 2);

        var returnBook = new AppButton("Mark as returned", ButtonKind.Primary);
        returnBook.Click += async (_, _) => await ReturnAsync();

        return Make.Stack(_borrower, _borrowerRole, dates, _dueNote, Make.Flow(0, returnBook));
    }

    private void LookUp()
    {
        var query = _lookup.Text.Trim();
        if (query.Length == 0)
        {
            return;
        }

        if (Store.FindBook(query) is not { } book)
        {
            Shell.Notify($"No book has the ID, catalogue ID or barcode “{query}”.", NoticeKind.Error);
            _lookup.FocusInput();
            return;
        }

        _bookId = book.Id;
        _memberId.Text = string.Empty;
        Render();

        if (book.IsAvailable)
        {
            _memberId.FocusInput();
        }
    }

    private void Render()
    {
        var book = _bookId is null ? null : Store.GetBook(_bookId);
        if (book is null)
        {
            // Nothing loaded yet, or the book was deleted (possibly on another computer).
            _bookId = null;
            _detail.Visible = false;
            _hint.Visible = true;
            return;
        }

        var today = Store.Today;
        var (statusText, statusColor) = BookGrid.Describe(book.StatusOn(today));
        _status.Text = statusText;
        _status.ForeColor = statusColor;

        _title.Text = book.Title;
        _author.Text = book.Author;
        _barcode.Text = book.Barcode;
        _facts.Text =
            $"Book ID {book.Id}   ·   Catalogue ID {Fallback(book.CatalogId)}   ·   Registered {Format(book.RegisteredOn)}";

        _issuePanel.Visible = book.IsAvailable;
        _returnPanel.Visible = !book.IsAvailable;

        if (book.Loan is { } loan)
        {
            _borrower.Text = Fallback(loan.MemberName);
            _borrowerRole.Text = $"{loan.RoleDescription}   ·   {Fallback(loan.MemberId)}";
            _issuedOn.Text = Format(loan.IssuedOn);
            _dueOn.Text = Format(loan.DueOn);
            (_dueNote.Text, _dueNote.ForeColor) = DescribeDue(book, today);
        }
        else
        {
            RenderMemberPreview();
        }

        _hint.Visible = false;
        _detail.Visible = true;
    }

    private void RenderMemberPreview()
    {
        var typed = _memberId.Text.Trim();
        var member = Store.FindMember(typed);
        _issue.Enabled = member is not null;

        if (member is not null)
        {
            var due = LoanPolicy.DueDateFor(member.Role, Store.Today);
            _memberPreview.Text = $"{member.Name}, {member.RoleDescription}. Due back {Format(due)}.";
            _memberPreview.ForeColor = Theme.Success;
        }
        else
        {
            _memberPreview.Text = typed.Length == 0 ? "No member selected yet." : "No member has this ID.";
            _memberPreview.ForeColor = typed.Length == 0 ? Theme.TextMuted : Theme.Warning;
        }
    }

    private static (string Text, Color Color) DescribeDue(Book book, DateOnly today)
    {
        if (book.Loan?.DueOn is not { } due)
        {
            return ("No due date was recorded for this loan.", Theme.TextMuted);
        }

        var days = due.DayNumber - today.DayNumber;
        return days switch
        {
            < 0 => ($"Overdue by {-days} {(days == -1 ? "day" : "days")}.", Theme.Danger),
            0 => ("Due back today.", Theme.Warning),
            <= LoanPolicy.DueSoonWindowDays => ($"Due in {days} {(days == 1 ? "day" : "days")}.", Theme.Warning),
            _ => ($"Due in {days} days.", Theme.TextMuted),
        };
    }

    private async Task IssueAsync()
    {
        if (_bookId is not { } bookId)
        {
            return;
        }

        Loan? loan = null;
        if (await Shell.RunAsync(async () => loan = await Store.IssueBookAsync(bookId, _memberId.Text)) && loan is not null)
        {
            _memberId.Text = string.Empty;
            Shell.Notify($"Issued to {loan.MemberName}. Due back {Format(loan.DueOn)}.");
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
            Shell.Notify($"“{book.Title}” is back on the shelf.");
            _lookup.FocusInput();
        }
    }

    private async Task SaveAsync()
    {
        if (_bookId is not { } bookId || Store.GetBook(bookId) is not { } book)
        {
            return;
        }

        var details = new BookDetails(_title.Text.Trim(), _author.Text.Trim(), _barcode.Text.Trim());
        if (details == new BookDetails(book.Title, book.Author, book.Barcode))
        {
            Shell.Notify("There are no changes to save.", NoticeKind.Info);
            return;
        }

        if (await Shell.RunAsync(() => Store.UpdateBookAsync(bookId, details)))
        {
            Shell.Notify("Changes saved.");
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
            Shell.Notify($"Deleted “{book.Title}”.");
            _lookup.Text = string.Empty;
            _lookup.FocusInput();
        }
    }

    private static string Format(DateOnly? date) =>
        date?.ToString(DateFormat, CultureInfo.CurrentCulture) ?? "not recorded";

    private static string Fallback(string text) => string.IsNullOrWhiteSpace(text) ? "not recorded" : text;
}
