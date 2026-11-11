using System.Diagnostics;
using System.Globalization;
using LibraryManagement.App.Infrastructure;
using LibraryManagement.App.Ui;
using LibraryManagement.App.Ui.Controls;
using LibraryManagement.Models;
using LibraryManagement.Reporting;

namespace LibraryManagement.App.Views;

/// <summary>
/// Today's figures and the loans to chase, laid out the way the Excel export is:
/// a short column of numbers and the list of books.
/// </summary>
internal sealed class ReportsView : ViewBase
{
    private readonly Label _date = Make.Subtitle();
    private readonly Label _saved = Make.Text(string.Empty, Theme.Body, Theme.Available, wrap: true);
    private readonly Dictionary<string, Label> _figures = [];
    private readonly Label _nothingToChase = Make.Text("Nothing is overdue or due soon today.", Theme.Body, Theme.InkMuted);
    private readonly BookGrid _grid = new(
        "Books to chase",
        BookColumn.DueNote, BookColumn.Id, BookColumn.Title, BookColumn.BorrowerName,
        BookColumn.BorrowerRole, BookColumn.Issued, BookColumn.Due);

    public ReportsView(IShell shell)
        : base(shell)
    {
        var export = new AppButton("Export to Excel…", ButtonKind.Primary);
        export.Click += async (_, _) => await ExportAsync();

        var openFolder = new AppButton("Open reports folder");
        openFolder.Click += (_, _) => OpenReportsFolder();

        _saved.Margin = new Padding(1, 0, 0, 12);
        _saved.Visible = false;
        _grid.BookActivated += (_, book) => Shell.OpenBook(book);

        var figures = Make.Section("Figures", BuildFigures());
        var toChase = Make.Section("Books to chase", Make.GridWithEmptyState(_grid, _nothingToChase), fill: true);

        Compose(
            (Make.Header("Daily report", _date, openFolder, export), false),
            (_saved, false),
            (Make.Columns(40, (figures, 23), (toChase, 77)), true));

        Store.Changed += (_, _) => Render();
        Render();
    }

    /// <summary>A plain two-column list: what is counted on the left, the number on the right.</summary>
    private TableLayoutPanel BuildFigures()
    {
        var table = new TableLayoutPanel
        {
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            Margin = Padding.Empty,
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        void AddRow(string name, bool indented = false, bool ruleAbove = false)
        {
            if (ruleAbove)
            {
                var rule = new Divider { Margin = new Padding(0, 3, 0, 9) };
                table.Controls.Add(rule);
                table.SetColumnSpan(rule, 2);
            }

            var caption = Make.Text(name, Theme.Body, indented ? Theme.InkMuted : Theme.Ink);
            caption.Margin = new Padding(indented ? 14 : 1, 0, 0, 7);

            var value = Make.Text("0", Theme.Strong, Theme.Ink);
            value.Anchor = AnchorStyles.Right;
            value.Margin = new Padding(12, 0, 2, 7);

            table.Controls.Add(caption);
            table.Controls.Add(value);
            _figures[name] = value;
        }

        AddRow("Books in the library", ruleAbove: true);
        AddRow("On the shelf");
        AddRow("On loan");
        AddRow("with students", indented: true);
        AddRow("with teachers", indented: true);
        AddRow("Overdue", ruleAbove: true);
        AddRow($"Due within {LoanPolicy.DueSoonWindowDays} days");
        return table;
    }

    private void Render()
    {
        var report = Store.CreateDailyReport();
        var stats = report.Stats;

        _date.Text = report.Date.ToString("dddd, d MMMM yyyy", CultureInfo.CurrentCulture);

        void Set(string name, int number, Color? whenPositive = null)
        {
            var label = _figures[name];
            label.Text = number.ToString("N0", CultureInfo.CurrentCulture);
            label.ForeColor = number > 0 && whenPositive is { } color ? color : Theme.Ink;
        }

        Set("Books in the library", stats.Total);
        Set("On the shelf", stats.Available);
        Set("On loan", stats.OnLoan);
        Set("with students", stats.OnLoanToStudents);
        Set("with teachers", stats.OnLoanToTeachers);
        Set("Overdue", stats.Overdue, Theme.Overdue);
        Set($"Due within {LoanPolicy.DueSoonWindowDays} days", stats.DueSoon, Theme.DueSoon);

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
        _saved.Visible = false;
        if (await Shell.RunAsync(() => Task.Run(() => SaveReport(report, path))))
        {
            _saved.Text = $"Saved to {path}";
            _saved.Visible = true;
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
            throw new LibraryException($"Couldn’t save the report. If it is open in Excel, close it and try again. ({exception.Message})", exception);
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
            Shell.Notify($"Couldn’t open the reports folder: {exception.Message}", NoticeKind.Error);
        }
    }
}
