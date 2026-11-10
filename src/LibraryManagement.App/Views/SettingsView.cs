using System.Reflection;
using LibraryManagement.App.Ui;
using LibraryManagement.App.Ui.Controls;

namespace LibraryManagement.App.Views;

/// <summary>Preferences, the reports folder and the database connection.</summary>
internal sealed class SettingsView : ViewBase
{
    private readonly ToggleSwitch _showReport = new(
        "Open the daily report at startup",
        "Once a day, the app opens on the report instead of the dashboard.");

    private readonly ToggleSwitch _saveReport = new(
        "Save the daily report automatically",
        "Once a day, an Excel copy is written to the reports folder.");

    private readonly Label _reportsFolder;
    private readonly Label _source;
    private readonly Label _keyFile;
    private bool _loading;

    public SettingsView(IShell shell)
        : base(shell)
    {
        _reportsFolder = Make.Text(string.Empty, Theme.Body, Theme.Text, wrap: true);
        _source = Make.Text(string.Empty, Theme.BodyStrong, Theme.Text, wrap: true);
        _keyFile = Make.Text(string.Empty, Theme.Small, Theme.TextMuted, wrap: true);

        var columns = Make.Columns(Theme.Gap, (BuildReportCard(), 54), (BuildRightColumn(), 46));
        columns.AutoSize = true;

        Compose(
            (Make.Header("Settings", Make.Subtitle("Saved for this Windows user on this computer.")), false),
            (columns, false),
            (new Panel(), true));

        _showReport.CheckedChanged += (_, _) => SaveToggles();
        _saveReport.CheckedChanged += (_, _) => SaveToggles();
        Store.Changed += (_, _) => Render();
        Render();
    }

    public override void OnNavigatedTo() => Render();

    private Card BuildReportCard()
    {
        _saveReport.Margin = new Padding(0, 6, 0, 0);

        var folderCaption = Make.Caption("Reports folder");
        folderCaption.Margin = new Padding(1, 22, 0, 4);

        var change = new AppButton("Change folder…");
        change.Click += (_, _) => ChooseReportsFolder();

        var reset = new AppButton("Use Documents");
        reset.Click += (_, _) =>
        {
            Shell.Settings.ReportsFolder = null;
            Shell.SaveSettings();
            Render();
        };

        var buttons = Make.Flow(10, change, reset);
        buttons.Margin = new Padding(0, 14, 0, 0);

        return Make.CardWith(
            "Daily report",
            null,
            Make.Stack(_showReport, _saveReport, folderCaption, _reportsFolder, buttons),
            fill: false);
    }

    private TableLayoutPanel BuildRightColumn()
    {
        _keyFile.Margin = new Padding(1, 6, 0, 0);

        var chooseKey = new AppButton("Choose key file…", ButtonKind.Primary);
        chooseKey.Click += async (_, _) => await Shell.ChooseKeyFileAsync();

        var useSample = new AppButton("Use sample data");
        useSample.Click += async (_, _) => await Shell.UseSampleDataAsync();

        var buttons = Make.Flow(10, chooseKey, useSample);
        buttons.Margin = new Padding(0, 16, 0, 0);

        var dataCard = Make.CardWith("Data source", null, Make.Stack(_source, _keyFile, buttons), fill: false);
        dataCard.Margin = new Padding(0, 0, 0, Theme.Gap);

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
        var about = Make.Text(
            $"Library Management System {version}\n" +
            "Data is stored with Google Cloud Firestore. Excel export uses EPPlus under its " +
            "non-commercial licence.",
            Theme.Small,
            Theme.TextMuted,
            wrap: true);

        var aboutCard = Make.CardWith("About", null, Make.Stack(about), fill: false);

        return Make.Stack(dataCard, aboutCard);
    }

    private void Render()
    {
        _loading = true;
        _showReport.Checked = Shell.Settings.ShowDailyReportOnStartup;
        _saveReport.Checked = Shell.Settings.AutoSaveDailyReport;
        _loading = false;

        _reportsFolder.Text = Shell.ReportsFolder;
        _source.Text = Store.IsPersistent
            ? $"Connected: {Store.SourceDescription}"
            : "Using built-in sample data. Changes are not saved.";
        _keyFile.Text = Shell.KeyFilePath is { } path
            ? $"Key file: {path}"
            : "Choose your Firebase service-account key file to work with the real library.";
    }

    private void SaveToggles()
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
