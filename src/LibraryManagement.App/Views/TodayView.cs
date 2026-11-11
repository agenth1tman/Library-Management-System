using System.Globalization;
using LibraryManagement.App.Ui;
using LibraryManagement.App.Ui.Controls;
using LibraryManagement.Models;

namespace LibraryManagement.App.Views;

/// <summary>
/// The start page: what needs chasing today. The two figures a librarian acts on
/// (overdue, due soon) are stated plainly; everything else is one line of text.
/// </summary>
internal sealed class TodayView : ViewBase
{
    private readonly Label _date = Make.Subtitle();
    private readonly Label _overdue = Make.Text(string.Empty, Theme.Figure, Theme.Overdue);
    private readonly Label _dueSoon = Make.Text(string.Empty, Theme.Figure, Theme.DueSoon);
    private readonly Label _totals = Make.Text(string.Empty, Theme.Body, Theme.InkMuted);
    private readonly InputBox _jump = new("Book number or barcode") { Width = 230, Caption = "Open a book" };
    private readonly AppButton _open = new("Open selected");
    private readonly BookGrid _grid = new(
        "Overdue and due soon",
        BookColumn.DueNote, BookColumn.Title, BookColumn.BorrowerName, BookColumn.BorrowerRole, BookColumn.Due);

    private readonly Label _allClear = Make.Text("Nothing is overdue or due in the next few days.", Theme.Body, Theme.InkMuted);

    public TodayView(IShell shell)
        : base(shell)
    {
        var find = new AppButton("Open book");
        find.Click += (_, _) => Jump();
        _jump.EnterPressed += (_, _) => Jump();

        var figures = Make.Flow(28, _overdue, _dueSoon);
        figures.Margin = new Padding(0, 0, 0, 4);
        _totals.Margin = new Padding(1, 0, 0, 22);

        _open.Click += (_, _) => OpenSelected();
        _grid.BookActivated += (_, book) => Shell.OpenBook(book);

        var list = Make.Section(
            "Overdue and due soon",
            Make.GridWithEmptyState(_grid, _allClear),
            _open,
            fill: true);

        Compose(
            (Make.Header("Today", _date, _jump, find), false),
            (figures, false),
            (_totals, false),
            (list, true));

        Store.Changed += (_, _) => Render();
        Render();
    }

    private void Jump()
    {
        var query = _jump.Text.Trim();
        if (query.Length > 0)
        {
            _jump.Text = string.Empty;
            Shell.LookUpBook(query);
        }
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
        var stats = Store.Stats;
        var today = Store.Today;

        _date.Text = today.ToString("dddd, d MMMM yyyy", CultureInfo.CurrentCulture);

        _overdue.Text = stats.Overdue == 0 ? "Nothing overdue" : $"{stats.Overdue:N0} overdue";
        _overdue.ForeColor = stats.Overdue == 0 ? Theme.Ink : Theme.Overdue;
        _dueSoon.Text = $"{stats.DueSoon:N0} due within {LoanPolicy.DueSoonWindowDays} days";
        _dueSoon.Visible = stats.DueSoon > 0;

        _totals.Text =
            $"{Count(stats.Total, "book")} in the library: {stats.Available:N0} on the shelf, {stats.OnLoan:N0} on loan " +
            $"({stats.OnLoanToStudents:N0} with students, {stats.OnLoanToTeachers:N0} with teachers).";

        var toChase = Store.CreateDailyReport().NeedsAttention.ToList();
        _grid.SetBooks(toChase, today);
        _grid.Visible = toChase.Count > 0;
        _allClear.Visible = toChase.Count == 0;
        _open.Enabled = toChase.Count > 0;
    }

    private static string Count(int number, string noun) => $"{number:N0} {noun}{(number == 1 ? string.Empty : "s")}";
}
