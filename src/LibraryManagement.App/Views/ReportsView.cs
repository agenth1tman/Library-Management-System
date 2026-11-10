using System.Diagnostics;
using System.Globalization;
using LibraryManagement.App.Infrastructure;
using LibraryManagement.App.Ui;
using LibraryManagement.App.Ui.Controls;
using LibraryManagement.Models;
using LibraryManagement.Reporting;

namespace LibraryManagement.App.Views;

/// <summary>Today's numbers and the loans to chase, with an Excel export.</summary>
internal sealed class ReportsView : ViewBase
{
    private readonly Label _subtitle = Make.Subtitle();
    private readonly StatCard _total = new("Total books", Theme.Info);
    private readonly StatCard _onLoan = new("On loan", Theme.Accent);
    private readonly StatCard _students = new("With students", Theme.Accent);
    private readonly StatCard _teachers = new("With teachers", Theme.Accent);
    private readonly StatCard _dueSoon = new("Due soon", Theme.Warning);
    private readonly StatCard _overdue = new("Overdue", Theme.Danger);
    private readonly BookGrid _grid = new(
        BookColumn.Status, BookColumn.Id, BookColumn.Title, BookColumn.Author,
        BookColumn.Borrower, BookColumn.Issued, BookColumn.Due);

    private readonly Label _nothingToChase;

    public ReportsView(IShell shell)
        : base(shell)
    {
        var export = new AppButton("Export to Excel", ButtonKind.Primary);
        export.Click += async (_, _) => await ExportAsync();

        var openFolder = new AppButton("Open reports folder");
        openFolder.Click += (_, _) => OpenReportsFolder();

        var stats = Make.EqualColumns(Theme.Gap, _total, _onLoan, _students, _teachers, _dueSoon, _overdue);
        stats.Height = 104;
        stats.Margin = new Padding(0, 0, 0, Theme.Gap);

        _nothingToChase = Make.Text("Nothing is overdue or due soon today.", Theme.Body, Theme.TextMuted);
        _nothingToChase.Margin = new Padding(1, 6, 0, 0);
        _grid.BookActivated += (_, book) => Shell.OpenBook(book);

        var body = new Panel { Margin = Padding.Empty };
        body.Controls.Add(_grid);
        body.Controls.Add(_nothingToChase);

        var card = Make.CardWith(
            "Overdue and upcoming returns",
            $"Loans past their due date, or due within {LoanPolicy.DueSoonWindowDays} days.",
            body);

        Compose((Make.Header("Daily report", _subtitle, openFolder, export), false), (stats, false), (card, true));

        Store.Changed += (_, _) => Render();
        Render();
    }

    private void Render()
    {
        var report = Store.CreateDailyReport();
        var stats = report.Stats;

        _subtitle.Text = report.Date.ToString("dddd, d MMMM yyyy", CultureInfo.CurrentCulture);
        _total.Value = stats.Total.ToString("N0", CultureInfo.CurrentCulture);
        _onLoan.Value = stats.OnLoan.ToString("N0", CultureInfo.CurrentCulture);
        _students.Value = stats.OnLoanToStudents.ToString("N0", CultureInfo.CurrentCulture);
        _teachers.Value = stats.OnLoanToTeachers.ToString("N0", CultureInfo.CurrentCulture);
        _dueSoon.Value = stats.DueSoon.ToString("N0", CultureInfo.CurrentCulture);
        _overdue.Value = stats.Overdue.ToString("N0", CultureInfo.CurrentCulture);

        var books = report.NeedsAttention.ToList();
        _grid.SetBooks(books, report.Date);
        _grid.Visible = books.Count > 0;
        _nothingToChase.Visible = books.Count == 0;
    }

    private async Task ExportAsync()
    {
        var report = Store.CreateDailyReport();

        using var dialog = new SaveFileDialog
        {
            Title = "Save daily report",
            Filter = "Excel workbook (*.xlsx)|*.xlsx",
            DefaultExt = "xlsx",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = DailyReportExporter.DefaultFileName(report.Date),
        };

        var folder = Shell.ReportsFolder;
        try
        {
            Directory.CreateDirectory(folder);
            dialog.InitialDirectory = folder;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The dialog simply opens wherever Windows last used; the user can still pick a place.
        }

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        var path = dialog.FileName;
        if (await Shell.RunAsync(() => Task.Run(() => SaveReport(report, path))))
        {
            Shell.Notify($"Report saved to {path}");
        }
    }

    private static void SaveReport(DailyReport report, string path)
    {
        try
        {
            DailyReportExporter.Export(report, path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Most often the file is still open in Excel.
            throw new LibraryException($"Couldn't save the report: {exception.Message}", exception);
        }
    }

    private void OpenReportsFolder()
    {
        try
        {
            var folder = Shell.ReportsFolder;
            Directory.CreateDirectory(folder);
            using var process = Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            Log.Error(exception, "Opening the reports folder failed.");
            Shell.Notify($"Couldn't open the reports folder: {exception.Message}", NoticeKind.Error);
        }
    }
}
