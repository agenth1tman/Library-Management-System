using LibraryManagement.App.Ui;
using LibraryManagement.App.Ui.Controls;
using LibraryManagement.Models;

namespace LibraryManagement.App.Views;

/// <summary>Every book in the library, with search-as-you-type and status filters.</summary>
internal sealed class LibraryView : ViewBase
{
    private readonly Label _subtitle = Make.Subtitle();
    private readonly InputBox _search = new("Search by title, author, barcode, ID or borrower");
    private readonly BookGrid _grid = new(
        BookColumn.Id, BookColumn.Title, BookColumn.Author, BookColumn.Barcode, BookColumn.CatalogId,
        BookColumn.Borrower, BookColumn.Due, BookColumn.Status);

    private readonly List<(AppButton Chip, Func<BookStatus, bool>? Filter)> _chips = [];
    private Func<BookStatus, bool>? _filter;

    public LibraryView(IShell shell)
        : base(shell)
    {
        var refresh = new AppButton("Refresh");
        refresh.Click += async (_, _) =>
        {
            if (await Shell.RunAsync(() => Store.RefreshAsync()))
            {
                Shell.Notify("Library refreshed.", NoticeKind.Info);
            }
        };

        var manage = new AppButton("Manage selected", ButtonKind.Primary);
        manage.Click += (_, _) =>
        {
            if (_grid.SelectedBook is { } book)
            {
                Shell.OpenBook(book);
            }
        };

        var chips = Make.Flow(
            8,
            AddChip("All", null),
            AddChip("Available", status => status == BookStatus.Available),
            AddChip("On loan", status => status != BookStatus.Available),
            AddChip("Overdue", status => status == BookStatus.Overdue));
        chips.Anchor = AnchorStyles.Right;
        chips.Margin = new Padding(Theme.Gap, 0, 0, 0);

        var toolbar = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Height = 40, Margin = new Padding(0, 0, 0, 14) };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _search.Dock = DockStyle.Fill;
        toolbar.Controls.Add(_search, 0, 0);
        toolbar.Controls.Add(chips, 1, 0);

        var gridCard = new Card { Padding = new Padding(12, 6, 12, 12) };
        gridCard.Controls.Add(_grid);

        Compose((Make.Header("Library", _subtitle, refresh, manage), false), (toolbar, false), (gridCard, true));

        _search.TextChanged += (_, _) => Render();
        _search.EnterPressed += (_, _) => _grid.Focus();
        _grid.BookActivated += (_, book) => Shell.OpenBook(book);
        Store.Changed += (_, _) => Render();
        SelectChip(_chips[0].Chip);
    }

    public override void OnNavigatedTo() => _search.FocusInput();

    private AppButton AddChip(string text, Func<BookStatus, bool>? filter)
    {
        var chip = new AppButton(text, ButtonKind.Chip);
        chip.Click += (_, _) => SelectChip(chip);
        _chips.Add((chip, filter));
        return chip;
    }

    private void SelectChip(AppButton selected)
    {
        foreach (var (chip, filter) in _chips)
        {
            chip.Selected = chip == selected;
            if (chip == selected)
            {
                _filter = filter;
            }
        }

        Render();
    }

    private void Render()
    {
        var today = Store.Today;
        var shown = Store.Books
            .Where(book => _filter is null || _filter(book.StatusOn(today)))
            .Where(book => BookSearch.Matches(book, _search.Text))
            .ToList();

        _grid.SetBooks(shown, today);

        var total = Store.Books.Count;
        _subtitle.Text = shown.Count == total
            ? $"{total:N0} {(total == 1 ? "book" : "books")}. Double-click one to manage it."
            : $"Showing {shown.Count:N0} of {total:N0} books.";
    }
}
