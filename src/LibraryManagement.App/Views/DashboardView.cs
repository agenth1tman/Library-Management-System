using System.Globalization;
using LibraryManagement.App.Ui;
using LibraryManagement.App.Ui.Controls;
using LibraryManagement.Models;

namespace LibraryManagement.App.Views;

/// <summary>The landing page: headline numbers, loans that need chasing and shortcuts.</summary>
internal sealed class DashboardView : ViewBase
{
    private readonly Label _subtitle = Make.Subtitle();
    private readonly StatCard _total = new("Total books", Theme.Info);
    private readonly StatCard _available = new("Available", Theme.Success);
    private readonly StatCard _onLoan = new("On loan", Theme.Accent);
    private readonly StatCard _dueSoon = new("Due soon", Theme.Warning);
    private readonly StatCard _overdue = new("Overdue", Theme.Danger);
    private readonly BookGrid _attention = new(BookColumn.Status, BookColumn.Title, BookColumn.Borrower, BookColumn.Due);
    private readonly Label _nothingToChase;
    private readonly Label _loanSplit = Make.Caption(string.Empty);

    public DashboardView(IShell shell)
        : base(shell)
    {
        var stats = Make.EqualColumns(Theme.Gap, _total, _available, _onLoan, _dueSoon, _overdue);
        stats.Height = 104;
        stats.Margin = new Padding(0, 0, 0, Theme.Gap);

        _nothingToChase = Make.Text("Nothing is overdue or due soon. All caught up.", Theme.Body, Theme.TextMuted);
        _nothingToChase.Margin = new Padding(1, 6, 0, 0);
        _attention.BookActivated += (_, book) => Shell.OpenBook(book);

        var attentionBody = new Panel { Margin = Padding.Empty };
        attentionBody.Controls.Add(_attention);
        attentionBody.Controls.Add(_nothingToChase);

        var attentionCard = Make.CardWith(
            "Needs attention",
            $"Overdue, or due within {LoanPolicy.DueSoonWindowDays} days. Double-click a book to open it.",
            attentionBody);

        var bottom = Make.Columns(Theme.Gap, (attentionCard, 66), (BuildQuickActions(), 34));

        Compose((Make.Header("Dashboard", _subtitle), false), (stats, false), (bottom, true));

        Store.Changed += (_, _) => Render();
        Render();
    }

    private Card BuildQuickActions()
    {
        AppButton Shortcut(string text, Page page, ButtonKind kind = ButtonKind.Secondary)
        {
            var button = new AppButton(text, kind) { Margin = new Padding(0, 0, 0, 10) };
            button.Click += (_, _) => Shell.Navigate(page);
            return button;
        }

        _loanSplit.Margin = new Padding(1, 8, 0, 0);

        var actions = Make.Stack(
            Shortcut("Issue or return a book", Page.Manage, ButtonKind.Primary),
            Shortcut("Register a new book", Page.Register),
            Shortcut("Search the library", Page.Library),
            Shortcut("Open the daily report", Page.Reports),
            _loanSplit);

        return Make.CardWith("Quick actions", null, actions);
    }

    private void Render()
    {
        var stats = Store.Stats;
        var today = Store.Today;

        _subtitle.Text = today.ToString("dddd, d MMMM yyyy", CultureInfo.CurrentCulture);
        _total.Value = stats.Total.ToString("N0", CultureInfo.CurrentCulture);
        _available.Value = stats.Available.ToString("N0", CultureInfo.CurrentCulture);
        _onLoan.Value = stats.OnLoan.ToString("N0", CultureInfo.CurrentCulture);
        _dueSoon.Value = stats.DueSoon.ToString("N0", CultureInfo.CurrentCulture);
        _overdue.Value = stats.Overdue.ToString("N0", CultureInfo.CurrentCulture);
        _loanSplit.Text = $"On loan: {stats.OnLoanToStudents} with students, {stats.OnLoanToTeachers} with teachers.";

        var needsAttention = Store.CreateDailyReport().NeedsAttention.ToList();
        _attention.SetBooks(needsAttention, today);
        _attention.Visible = needsAttention.Count > 0;
        _nothingToChase.Visible = needsAttention.Count == 0;
    }
}
