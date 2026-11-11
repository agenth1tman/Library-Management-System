using LibraryManagement.App.Ui;
using LibraryManagement.App.Ui.Controls;
using LibraryManagement.Models;

namespace LibraryManagement.App.Views;

/// <summary>Every book in the library, with search-as-you-type and status filters.</summary>
internal sealed class LibraryView : ViewBase
{
    private readonly Label _count = Make.Subtitle();
    private readonly InputBox _search = new("Search title, author, barcode, number or borrower") { Caption = "Search the library" };
    private readonly AppButton _open = new("Open book", ButtonKind.Primary);
    private readonly Label _noMatches = Make.Text(string.Empty, Theme.Body, Theme.InkMuted);
    private readonly BookGrid _grid = new(
        "Books",
        BookColumn.Id, BookColumn.Title, BookColumn.Author, BookColumn.Barcode, BookColumn.CatalogId,
        BookColumn.BorrowerName, BookColumn.Due, BookColumn.Status);

    private readonly List<Filter> _filters = [];
    private Filter _current;

    public LibraryView(IShell shell)
        : base(shell)
    {
        var refresh = new AppButton("Refresh");
        refresh.Click += async (_, _) => await Shell.RunAsync(() => Store.RefreshAsync());
        _open.Click += (_, _) => OpenSelected();

        _current = AddFilter("All", _ => true);
        AddFilter("On the shelf", status => status == BookStatus.Available);
        AddFilter("On loan", status => status != BookStatus.Available);
        AddFilter("Overdue", status => status == BookStatus.Overdue);

        var tabs = Make.Flow(2, _filters.Select(filter => (Control)filter.Tab).ToArray());
        tabs.Anchor = AnchorStyles.Right;
        tabs.Margin = new Padding(Theme.Gap, 0, 0, 0);

        var toolbar = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Height = Theme.ControlHeight, Margin = new Padding(0, 0, 0, 10) };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _search.Dock = DockStyle.Fill;
        toolbar.Controls.Add(_search, 0, 0);
        toolbar.Controls.Add(tabs, 1, 0);

        Compose(
            (Make.Header("Library", _count, refresh, _open), false),
            (toolbar, false),
            (Make.GridWithEmptyState(_grid, _noMatches), true));

        _search.TextChanged += (_, _) => Render();
        _search.EnterPressed += (_, _) => _grid.Focus();
        _search.EscapePressed += (_, _) => _search.Text = string.Empty;
        _grid.BookActivated += (_, book) => Shell.OpenBook(book);
        Store.Changed += (_, _) => Render();
        Render();
    }

    public override void OnNavigatedTo() => _search.FocusInput();

    private Filter AddFilter(string name, Func<BookStatus, bool> matches)
    {
        var filter = new Filter(name, matches, new AppButton(name, ButtonKind.Tab));
        filter.Tab.Click += (_, _) =>
        {
            _current = filter;
            Render();
        };
        _filters.Add(filter);
        return filter;
    }

    private void OpenSelected()
    {
        if (_grid.SelectedBook is { } book)
        {
            Shell.OpenBook(book);
        }
    }

    private void Render()
    {
        var today = Store.Today;
        var statuses = Store.Books.Select(book => (Book: book, Status: book.StatusOn(today))).ToList();

        // Each filter shows how many books it holds, so the tabs double as a summary.
        foreach (var filter in _filters)
        {
            filter.Tab.SetText($"{filter.Name} {statuses.Count(entry => filter.Matches(entry.Status)):N0}");
            filter.Tab.Selected = filter == _current;
        }

        var shown = statuses
            .Where(entry => _current.Matches(entry.Status))
            .Select(entry => entry.Book)
            .Where(book => BookSearch.Matches(book, _search.Text))
            .ToList();

        _grid.SetBooks(shown, today);
        _grid.Visible = shown.Count > 0;
        _open.Enabled = shown.Count > 0;

        _noMatches.Visible = shown.Count == 0;
        _noMatches.Text = Store.Books.Count == 0
            ? "The library has no books yet. Add the first one on the Register page."
            : "No books match. Clear the search or choose another filter.";

        var total = Store.Books.Count;
        _count.Text = shown.Count == total
            ? $"{total:N0} {(total == 1 ? "book" : "books")}"
            : $"{shown.Count:N0} of {total:N0} books shown";
    }

    private sealed record Filter(string Name, Func<BookStatus, bool> Matches, AppButton Tab);
}
