using System.ComponentModel;
using System.Globalization;
using LibraryManagement.Models;

namespace LibraryManagement.App.Ui.Controls;

internal enum BookColumn
{
    Id,
    Status,
    DueNote,
    Title,
    Author,
    Barcode,
    CatalogId,
    Registered,
    Issued,
    Due,
    Borrower,
    BorrowerName,
    BorrowerRole,
}

/// <summary>
/// A read-only, ruled table of books that sits directly on the page.
/// Double-click or Enter "activates" a book.
/// </summary>
internal sealed class BookGrid : DataGridView
{
    private const string DateFormat = "d MMM yyyy";
    private const string Blank = "—";

    private static readonly IReadOnlyDictionary<BookColumn, ColumnDefinition> Definitions =
        new Dictionary<BookColumn, ColumnDefinition>
        {
            // Numbers, identifiers, dates and statuses are read whole or not at all, so they
            // get a fixed width. Only free text (title, author, borrower) shares what is left.
            [BookColumn.Id] = ColumnDefinition.Fixed("No.", 54, (book, _) => book.NumericId is { } number ? number : book.Id, rightAligned: true),
            [BookColumn.Status] = ColumnDefinition.Fixed("Status", 100, (book, today) => book.StatusOn(today)),
            [BookColumn.DueNote] = ColumnDefinition.Fixed("Status", 128, (book, today) => DueNote.For(book, today)),
            [BookColumn.Title] = ColumnDefinition.Flexible("Title", 200, 130, (book, _) => book.Title),
            [BookColumn.Author] = ColumnDefinition.Flexible("Author", 120, 96, (book, _) => book.Author),
            [BookColumn.Barcode] = ColumnDefinition.Fixed("Barcode", 124, (book, _) => book.Barcode),
            [BookColumn.CatalogId] = ColumnDefinition.Fixed("Catalogue ID", 128, (book, _) => book.CatalogId),
            [BookColumn.Registered] = ColumnDefinition.Fixed("Registered", 100, (book, _) => book.RegisteredOn),
            [BookColumn.Issued] = ColumnDefinition.Fixed("Issued", 100, (book, _) => book.Loan?.IssuedOn),
            [BookColumn.Due] = ColumnDefinition.Fixed("Due", 100, (book, _) => book.Loan?.DueOn),
            [BookColumn.Borrower] = ColumnDefinition.Flexible("Borrower", 150, 120, (book, _) => book.Loan is { } loan ? $"{loan.MemberName} ({loan.MemberId})" : null),
            [BookColumn.BorrowerName] = ColumnDefinition.Flexible("Borrower", 110, 100, (book, _) => book.Loan?.MemberName),
            [BookColumn.BorrowerRole] = ColumnDefinition.Fixed("Class", 78, (book, _) => ClassOf(book.Loan)),
        };

    private readonly BookColumn[] _columns;

    public BookGrid(string accessibleName, params BookColumn[] columns)
    {
        _columns = columns;
        DoubleBuffered = true;
        AccessibleName = accessibleName;

        // The grid's own measurements (row height, column widths) are not covered by
        // WinForms' automatic DPI scaling, so they are scaled here.
        var scale = DeviceDpi / 96f;
        int Scaled(int pixels) => (int)Math.Round(pixels * scale);

        Dock = DockStyle.Fill;
        Margin = Padding.Empty;
        BorderStyle = BorderStyle.None;
        BackgroundColor = Theme.Page;
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
        ColumnHeadersHeight = Scaled(30);
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Page,
            ForeColor = Theme.InkMuted,
            SelectionBackColor = Theme.Page,
            SelectionForeColor = Theme.InkMuted,
            Font = Theme.SmallStrong,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(4, 0, 4, 0),
        };

        RowTemplate.Height = Scaled(32);
        DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Page,
            ForeColor = Theme.Ink,
            SelectionBackColor = Theme.Selection,
            SelectionForeColor = Theme.Ink,
            Font = Theme.Body,
            Padding = new Padding(4, 0, 4, 0),
        };

        foreach (var column in columns)
        {
            var definition = Definitions[column];
            var gridColumn = new DataGridViewTextBoxColumn
            {
                Name = column.ToString(),
                HeaderText = definition.Header,
                SortMode = DataGridViewColumnSortMode.Automatic,
            };

            if (definition.FillWeight is { } weight)
            {
                gridColumn.FillWeight = weight;
                gridColumn.MinimumWidth = Scaled(definition.Width);
            }
            else
            {
                gridColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                gridColumn.Width = Scaled(definition.Width);
            }

            if (definition.RightAligned)
            {
                // Numbers line up on their last digit so they can be compared at a glance.
                gridColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                gridColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                gridColumn.DefaultCellStyle.Padding = new Padding(4, 0, 10, 0);
                gridColumn.HeaderCell.Style.Padding = new Padding(4, 0, 10, 0);
            }

            Columns.Add(gridColumn);
        }
    }

    /// <summary>Raised when the user double-clicks a book or presses Enter on it.</summary>
    public event EventHandler<Book>? BookActivated;

    public Book? SelectedBook => CurrentRow?.Tag as Book;

    /// <summary>The whole row is highlighted, so the dotted box around one cell is just noise.</summary>
    protected override bool ShowFocusCues => false;

    public static (string Text, Color Color) Describe(BookStatus status) => status switch
    {
        BookStatus.Available => ("On the shelf", Theme.Available),
        BookStatus.DueSoon => ("Due soon", Theme.DueSoon),
        BookStatus.Overdue => ("Overdue", Theme.Overdue),
        _ => ("On loan", Theme.InkMuted),
    };

    /// <summary>Replaces the rows, keeping the previously selected book selected when it is still listed.</summary>
    public void SetBooks(IEnumerable<Book> books, DateOnly today)
    {
        var previousId = SelectedBook?.Id;
        var sortedColumn = SortedColumn;
        var sortDirection = SortOrder == SortOrder.Descending ? ListSortDirection.Descending : ListSortDirection.Ascending;

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
        var reselect = Rows.Cast<DataGridViewRow>().FirstOrDefault(row => (row.Tag as Book)?.Id == previousId)
            ?? (Rows.Count > 0 ? Rows[0] : null);

        if (reselect is not null)
        {
            CurrentCell = reselect.Cells[0];
            reselect.Selected = true;
        }

        ResumeLayout();
    }

    protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
    {
        switch (e.Value)
        {
            case null:
                Apply(e, Blank, Theme.InkDisabled);
                break;

            case DateOnly date:
                e.Value = date.ToString(DateFormat, CultureInfo.CurrentCulture);
                e.FormattingApplied = true;
                break;

            case BookStatus status:
                var (text, color) = Describe(status);
                Apply(e, text, color, strong: status is BookStatus.Overdue or BookStatus.DueSoon);
                break;

            case DueNote note:
                Apply(e, note.Text, note.Color, strong: true);
                break;
        }

        base.OnCellFormatting(e);
    }

    protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
    {
        base.OnCellPainting(e);

        if (e.RowIndex != -1 || e.Graphics is null)
        {
            return;
        }

        // Column headers: plain text with one firm rule underneath, like a ledger.
        e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.Border);
        using var pen = new Pen(Theme.RuleStrong);
        e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
        e.Handled = true;
    }

    protected override void OnRowPostPaint(DataGridViewRowPostPaintEventArgs e)
    {
        base.OnRowPostPaint(e);

        // One hairline under each row, drawn across all columns in a single stroke.
        using var pen = new Pen(Theme.Rule);
        var y = e.RowBounds.Bottom - 1;
        e.Graphics.DrawLine(pen, e.RowBounds.Left, y, e.RowBounds.Right, y);
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
            (DueNote left, DueNote right) => left.DaysUntilDue.CompareTo(right.DaysUntilDue),
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

    /// <summary>A student's class ("10-B"), or "Teacher": short enough to scan down a column.</summary>
    private static string? ClassOf(Loan? loan) => loan switch
    {
        null => null,
        { Role: MemberRole.Teacher } => "Teacher",
        { Section.Length: > 0 } => loan.Section,
        { Role: MemberRole.Student } => "Student",
        _ => null,
    };

    private static void Apply(DataGridViewCellFormattingEventArgs e, string text, Color color, bool strong = false)
    {
        e.Value = text;
        e.CellStyle!.ForeColor = color;
        e.CellStyle.SelectionForeColor = color;
        if (strong)
        {
            e.CellStyle.Font = Theme.Strong;
        }

        e.FormattingApplied = true;
    }

    private void ActivateSelected()
    {
        if (SelectedBook is { } book)
        {
            BookActivated?.Invoke(this, book);
        }
    }

    /// <summary><c>FillWeight</c> is null for fixed-width columns; then <c>Width</c> is the width, otherwise the minimum.</summary>
    private sealed record ColumnDefinition(
        string Header,
        int Width,
        float? FillWeight,
        Func<Book, DateOnly, object?> Value,
        bool RightAligned)
    {
        public static ColumnDefinition Fixed(string header, int width, Func<Book, DateOnly, object?> value, bool rightAligned = false) =>
            new(header, width, null, value, rightAligned);

        public static ColumnDefinition Flexible(string header, float weight, int minimumWidth, Func<Book, DateOnly, object?> value) =>
            new(header, minimumWidth, weight, value, false);
    }
}

/// <summary>How urgent a loan is, in the librarian's words: "8 days overdue", "Due tomorrow".</summary>
internal sealed record DueNote(int DaysUntilDue)
{
    public string Text => DaysUntilDue switch
    {
        < -1 => $"{-DaysUntilDue} days overdue",
        -1 => "1 day overdue",
        0 => "Due today",
        1 => "Due tomorrow",
        _ => $"Due in {DaysUntilDue} days",
    };

    public Color Color => DaysUntilDue switch
    {
        < 0 => Theme.Overdue,
        <= LoanPolicy.DueSoonWindowDays => Theme.DueSoon,
        _ => Theme.InkMuted,
    };

    public static DueNote? For(Book book, DateOnly today) =>
        book.Loan?.DueOn is { } due ? new DueNote(due.DayNumber - today.DayNumber) : null;
}
