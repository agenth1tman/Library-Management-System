using System.Reflection;
using LibraryManagement.App.Ui;
using LibraryManagement.App.Ui.Controls;

namespace LibraryManagement.App.Views;

/// <summary>Preferences, the reports folder and the database connection.</summary>
internal sealed class SettingsView : ViewBase
{
    private readonly CheckBox _showReport = Option("Open the daily report when the app starts (once a day)");
    private readonly CheckBox _saveReport = Option("Save an Excel copy of the daily report to the reports folder (once a day)");
    private readonly Label _reportsFolder = Make.Text(string.Empty, Theme.Body, Theme.Ink, wrap: true);
    private readonly Label _source = Make.Text(string.Empty, Theme.Strong, Theme.Ink, wrap: true);
    private readonly Label _keyFile = Make.Text(string.Empty, Theme.Body, Theme.InkMuted, wrap: true);
    private bool _loading;

    public SettingsView(IShell shell)
        : base(shell)
    {
        var sections = Make.Stack(
            BuildReportSection(),
            Spaced(new Divider()),
            BuildFolderSection(),
            Spaced(new Divider()),
            BuildDatabaseSection(),
            Spaced(new Divider()),
            BuildAboutSection());

        var column = new TableLayoutPanel { ColumnCount = 2, RowCount = 1 };
        column.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 620));
        column.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        column.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        column.Controls.Add(sections, 0, 0);

        Compose(
            (Make.Header("Settings", Make.Subtitle("Kept for this Windows user on this computer.")), false),
            (column, true));

        _showReport.CheckedChanged += (_, _) => SaveOptions();
        _saveReport.CheckedChanged += (_, _) => SaveOptions();
        Store.Changed += (_, _) => Render();
        Render();
    }

    public override void OnNavigatedTo() => Render();

    private static CheckBox Option(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = Theme.Body,
        ForeColor = Theme.Ink,
        Margin = new Padding(0, 0, 0, 6),
        UseMnemonic = false,
    };

    private static Divider Spaced(Divider divider)
    {
        divider.Margin = new Padding(0, 16, 0, 16);
        return divider;
    }

    private TableLayoutPanel BuildReportSection() =>
        Make.Section("Daily report", Make.Stack(_showReport, _saveReport));

    private TableLayoutPanel BuildFolderSection()
    {
        var change = new AppButton("Change folder…");
        change.Click += (_, _) => ChooseReportsFolder();

        var reset = new AppButton("Use Documents");
        reset.Click += (_, _) =>
        {
            Shell.Settings.ReportsFolder = null;
            Shell.SaveSettings();
            Render();
        };

        var buttons = Make.Flow(8, change, reset);
        buttons.Margin = new Padding(0, 10, 0, 0);
        return Make.Section("Reports folder", Make.Stack(_reportsFolder, buttons));
    }

    private TableLayoutPanel BuildDatabaseSection()
    {
        _keyFile.Margin = new Padding(0, 4, 0, 0);

        var chooseKey = new AppButton("Choose key file…");
        chooseKey.Click += async (_, _) => await Shell.ChooseKeyFileAsync();

        var useSample = new AppButton("Use sample data");
        useSample.Click += async (_, _) => await Shell.UseSampleDataAsync();

        var buttons = Make.Flow(8, chooseKey, useSample);
        buttons.Margin = new Padding(0, 10, 0, 0);
        return Make.Section("Library database", Make.Stack(_source, _keyFile, buttons));
    }

    private static TableLayoutPanel BuildAboutSection()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
        var about = Make.Text(
            $"Library Management System {version}. Titles are set in Crimson Text (SIL Open Font License 1.1). " +
            "Data is kept in Google Cloud Firestore. Excel export uses EPPlus under its non-commercial licence.",
            Theme.Small,
            Theme.InkMuted,
            wrap: true);

        return Make.Section("About", Make.Stack(about));
    }

    private void Render()
    {
        _loading = true;
        _showReport.Checked = Shell.Settings.ShowDailyReportOnStartup;
        _saveReport.Checked = Shell.Settings.AutoSaveDailyReport;
        _loading = false;

        _reportsFolder.Text = Shell.ReportsFolder;
        _source.Text = Store.IsPersistent
            ? $"Connected to {Store.SourceDescription}"
            : "Showing sample data. Nothing you change is saved.";
        _keyFile.Text = Shell.KeyFilePath is { } path
            ? $"Key file: {path}"
            : "Choose your Firebase service-account key file to work with the real library.";
    }

    private void SaveOptions()
    {
        if (_loading)
        {
            return;
        }

        Shell.Settings.ShowDailyReportOnStartup = _showReport.Checked;
        Shell.Settings.AutoSaveDailyReport = _saveReport.Checked;
        Shell.SaveSettings();
    }

    private void ChooseReportsFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose where reports are saved",
            UseDescriptionForTitle = true,
            SelectedPath = Shell.ReportsFolder,
        };

        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
        {
            Shell.Settings.ReportsFolder = dialog.SelectedPath;
            Shell.SaveSettings();
            Render();
        }
    }
}
