using System.Globalization;
using LibraryManagement.Models;

namespace LibraryManagement.App.Ui.Controls;

internal enum BookColumn
{
    Id,
    Status,
    Title,
    Author,
    Barcode,
    CatalogId,
    Registered,
    Issued,
    Due,
    Borrower,
}

/// <summary>A read-only, themed table of books. Double-click or Enter "activates" a book.</summary>
internal sealed class BookGrid : DataGridView
{
    private const string DateFormat = "d MMM yyyy";
    private const string Blank = "—";

    private static readonly IReadOnlyDictionary<BookColumn, ColumnDefinition> Definitions =
        new Dictionary<BookColumn, ColumnDefinition>
        {
            [BookColumn.Id] = new("ID", 30, 46, (book, _) => book.NumericId is { } number ? number : book.Id),
            [BookColumn.Status] = new("Status", 62, 86, (book, today) => book.StatusOn(today)),
            [BookColumn.Title] = new("Title", 170, 130, (book, _) => book.Title),
            [BookColumn.Author] = new("Author", 110, 96, (book, _) => book.Author),
            [BookColumn.Barcode] = new("Barcode", 96, 104, (book, _) => book.Barcode),
            [BookColumn.CatalogId] = new("Catalogue ID", 100, 112, (book, _) => book.CatalogId),
            [BookColumn.Registered] = new("Registered", 78, 94, (book, _) => book.RegisteredOn),
            [BookColumn.Issued] = new("Issued", 78, 94, (book, _) => book.Loan?.IssuedOn),
            [BookColumn.Due] = new("Due", 78, 94, (book, _) => book.Loan?.DueOn),
            [BookColumn.Borrower] = new("Borrower", 150, 120, (book, _) => book.Loan is { } loan ? $"{loan.MemberName} ({loan.MemberId})" : null),
        };

    private readonly BookColumn[] _columns;

    public BookGrid(params BookColumn[] columns)
    {
        _columns = columns;
        DoubleBuffered = true;

        // The grid's own measurements (row height, column widths) are not covered by
        // WinForms' automatic DPI scaling, so they are scaled here.
        var scale = DeviceDpi / 96f;
        int Scaled(int pixels) => (int)Math.Round(pixels * scale);

        Dock = DockStyle.Fill;
        Margin = Padding.Empty;
        BorderStyle = BorderStyle.None;
        BackgroundColor = Theme.Surface;
        CellBorderStyle = DataGridViewCellBorderStyle.None;

        ReadOnly = true;
        MultiSelect = false;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        AllowUserToResizeRows = false;
        RowHeadersVisible = false;
        AutoGenerateColumns = false;
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        StandardTab = true;

        EnableHeadersVisualStyles = false;
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        ColumnHeadersHeight = Scaled(40);
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Surface,
            ForeColor = Theme.TextMuted,
            SelectionBackColor = Theme.Surface,
            SelectionForeColor = Theme.TextMuted,
            Font = Theme.SmallStrong,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 6, 0),
        };

        RowTemplate.Height = Scaled(38);
        DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            SelectionBackColor = Theme.SurfaceRaised,
            SelectionForeColor = Theme.Text,
            Font = Theme.Body,
            Padding = new Padding(6, 0, 6, 0),
        };

        foreach (var column in columns)
        {
            var definition = Definitions[column];
            Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = column.ToString(),
                HeaderText = definition.Header,
                FillWeight = definition.Weight,
                MinimumWidth = Scaled(definition.MinimumWidth),
                SortMode = DataGridViewColumnSortMode.Automatic,
            });
        }
    }

    /// <summary>Raised when the user double-clicks a book or presses Enter on it.</summary>
    public event EventHandler<Book>? BookActivated;

    public Book? SelectedBook => CurrentRow?.Tag as Book;

    /// <summary>The whole row is highlighted, so the dotted box around one cell is just noise.</summary>
    protected override bool ShowFocusCues => false;

    /// <summary>Replaces the rows, keeping the previously selected book selected when it is still listed.</summary>
    public void SetBooks(IEnumerable<Book> books, DateOnly today)
    {
        var previousId = SelectedBook?.Id;
        var sortedColumn = SortedColumn;
        var sortDirection = SortOrder == SortOrder.Descending
            ? System.ComponentModel.ListSortDirection.Descending
            : System.ComponentModel.ListSortDirection.Ascending;

        var rows = books.Select(book =>
        {
            var row = new DataGridViewRow { Tag = book, Height = RowTemplate.Height };
            row.CreateCells(this, _columns.Select(column => Definitions[column].Value(book, today)).ToArray());
            return row;
        }).ToArray();

        SuspendLayout();
        Rows.Clear();
        Rows.AddRange(rows);

        if (sortedColumn is not null)
        {
            Sort(sortedColumn, sortDirection);
        }

        ClearSelection();
        var reselect = Rows.Cast<DataGridViewRow>().FirstOrDefault(row => (row.Tag as Book)?.Id == previousId);
        if (reselect is not null)
        {
            CurrentCell = reselect.Cells[0];
            reselect.Selected = true;
        }
        else if (Rows.Count > 0)
        {
            CurrentCell = Rows[0].Cells[0];
            Rows[0].Selected = true;
        }

        ResumeLayout();
    }

    protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
    {
        switch (e.Value)
        {
            case null:
                e.Value = Blank;
                e.CellStyle!.ForeColor = Theme.TextDisabled;
                e.CellStyle.SelectionForeColor = Theme.TextDisabled;
                e.FormattingApplied = true;
                break;

            case DateOnly date:
                e.Value = date.ToString(DateFormat, CultureInfo.CurrentCulture);
                e.FormattingApplied = true;
                break;

            case BookStatus status:
                var (text, color) = Describe(status);
                e.Value = text;
                e.CellStyle!.ForeColor = color;
                e.CellStyle.SelectionForeColor = color;
                e.CellStyle.Font = Theme.BodyStrong;
                e.FormattingApplied = true;
                break;
        }

        base.OnCellFormatting(e);
    }

    protected override void OnRowPostPaint(DataGridViewRowPostPaintEventArgs e)
    {
        base.OnRowPostPaint(e);

        // One hairline under each row, drawn across all columns in a single stroke.
        using var pen = new Pen(Drawing.Blend(Theme.Surface, Theme.Border, 0.55f));
        var y = e.RowBounds.Bottom - 1;
        e.Graphics.DrawLine(pen, e.RowBounds.Left, y, e.RowBounds.Right, y);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        NativeTheme.UseDarkScrollBars(VerticalScrollBar);
        NativeTheme.UseDarkScrollBars(HorizontalScrollBar);
    }

    protected override void OnSortCompare(DataGridViewSortCompareEventArgs e)
    {
        // Numbers sort as numbers and dates as dates; empty cells always go last when ascending.
        e.SortResult = (e.CellValue1, e.CellValue2) switch
        {
            (null, null) => 0,
            (null, _) => 1,
            (_, null) => -1,
            (int left, int right) => left.CompareTo(right),
            (DateOnly left, DateOnly right) => left.CompareTo(right),
            (BookStatus left, BookStatus right) => left.CompareTo(right),
            var (left, right) => string.Compare(
                Convert.ToString(left, CultureInfo.CurrentCulture),
                Convert.ToString(right, CultureInfo.CurrentCulture),
                StringComparison.CurrentCultureIgnoreCase),
        };
        e.Handled = true;

        base.OnSortCompare(e);
    }

    protected override void OnCellDoubleClick(DataGridViewCellEventArgs e)
    {
        base.OnCellDoubleClick(e);

        if (e.RowIndex >= 0)
        {
            ActivateSelected();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            ActivateSelected();
            return;
        }

        base.OnKeyDown(e);
    }

    public static (string Text, Color Color) Describe(BookStatus status) => status switch
    {
        BookStatus.Available => ("Available", Theme.Success),
        BookStatus.DueSoon => ("Due soon", Theme.Warning),
        BookStatus.Overdue => ("Overdue", Theme.Danger),
        _ => ("On loan", Theme.Info),
    };

    private void ActivateSelected()
    {
        if (SelectedBook is { } book)
        {
            BookActivated?.Invoke(this, book);
        }
    }

    private sealed record ColumnDefinition(string Header, float Weight, int MinimumWidth, Func<Book, DateOnly, object?> Value);
}
