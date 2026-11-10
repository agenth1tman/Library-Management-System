using LibraryManagement.App.Infrastructure;
using LibraryManagement.App.Ui;
using LibraryManagement.App.Ui.Controls;
using LibraryManagement.App.Views;
using LibraryManagement.Configuration;
using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Reporting;
using LibraryManagement.Services;

namespace LibraryManagement.App;

/// <summary>
/// The main window: a sidebar for navigation and a content area that shows one screen at
/// a time. It also owns the things every screen shares (busy indicator, notifications,
/// settings and the connection to the library).
/// </summary>
internal sealed class MainForm : Form, IShell
{
    private static readonly (Page Page, string Caption)[] NavigationItems =
    [
        (Page.Dashboard, "Dashboard"),
        (Page.Library, "Library"),
        (Page.Manage, "Manage"),
        (Page.Register, "Register"),
        (Page.Reports, "Daily report"),
        (Page.Settings, "Settings"),
    ];

    private readonly StartupOptions _options;
    private readonly SettingsStore _settingsStore = new(AppPaths.SettingsFile);

    private readonly Dictionary<Page, ViewBase> _views = [];
    private readonly Dictionary<Page, NavButton> _navigation = [];
    private readonly SetupView _setup;
    private readonly ManageView _manage;
    private readonly BusyBar _busyBar = new();
    private readonly Toast _toast = new();
    private readonly ConnectionBadge _connection = new();

    private bool _busy;

    public MainForm(StartupOptions options)
    {
        _options = options;
        Settings = _settingsStore.Load();

        SuspendLayout();

        // Sizes below are written for 96 DPI; WinForms scales them to the actual display.
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "Library Management System";
        Icon = EmbeddedAssets.LoadAppIcon();
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = Theme.Body;
        ClientSize = new Size(1280, 780);
        MinimumSize = new Size(1120, 720);
        StartPosition = FormStartPosition.CenterScreen;

        _manage = new ManageView(this);
        _views[Page.Dashboard] = new DashboardView(this);
        _views[Page.Library] = new LibraryView(this);
        _views[Page.Manage] = _manage;
        _views[Page.Register] = new RegisterView(this);
        _views[Page.Reports] = new ReportsView(this);
        _views[Page.Settings] = new SettingsView(this);
        _setup = new SetupView(this);

        var content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
        content.Controls.Add(_toast);
        content.Controls.Add(_setup);
        foreach (var view in _views.Values)
        {
            content.Controls.Add(view);
        }

        // Docked edges are laid out from the last control added to the first.
        content.Controls.Add(_busyBar);
        content.Resize += (_, _) => _toast.Reposition();

        Controls.Add(content);
        Controls.Add(BuildSidebar());

        ResumeLayout(performLayout: true);

        SetNavigationEnabled(false);
        _setup.ShowConnecting();
        Show(_setup);
    }

    public LibraryStore Store { get; } = new();

    public AppSettings Settings { get; }

    public string ReportsFolder =>
        string.IsNullOrWhiteSpace(Settings.ReportsFolder) ? AppPaths.DefaultReportsFolder : Settings.ReportsFolder;

    public string? KeyFilePath { get; private set; }

    public void SaveSettings()
    {
        try
        {
            _settingsStore.Save(Settings);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Log.Error(exception, "Saving settings failed.");
            Notify($"Couldn't save your settings: {exception.Message}", NoticeKind.Error);
        }
    }

    public void Navigate(Page page)
    {
        if (!Store.IsConnected)
        {
            return;
        }

        foreach (var (candidate, button) in _navigation)
        {
            button.Selected = candidate == page;
        }

        var view = _views[page];
        Show(view);
        view.OnNavigatedTo();
    }

    public void OpenBook(Book book)
    {
        _manage.Open(book);
        Navigate(Page.Manage);
    }

    public void Notify(string message, NoticeKind kind = NoticeKind.Success) => _toast.Show(message, kind);

    public async Task<bool> RunAsync(Func<Task> operation)
    {
        if (_busy)
        {
            // Ignore double-clicks and Enter being held down while a request is in flight.
            return false;
        }

        SetBusy(true);
        try
        {
            await operation();
            return true;
        }
        catch (LibraryException exception)
        {
            // Expected problems (validation, no connection) come with a message meant for the user.
            LogCause(exception, "A library operation failed.");
            Notify(exception.Message, NoticeKind.Error);
            return false;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "An operation failed unexpectedly.");
            Dialogs.Error(this, $"Something unexpected went wrong:\n\n{exception.Message}\n\nDetails were saved to:\n{Log.FilePath}");
            return false;
        }
        finally
        {
            SetBusy(false);
        }
    }

    public async Task ChooseKeyFileAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Choose your Firebase service-account key",
            Filter = "Service-account key (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var path = dialog.FileName;
        if (await ConnectAsync(async () => await FirestoreLibraryRepository.CreateAsync(path), keyFilePath: path))
        {
            Settings.CredentialsPath = path;
            SaveSettings();
        }
    }

    public Task UseSampleDataAsync() => ConnectAsync(
        () => Task.FromResult<ILibraryRepository>(SampleLibraryRepository.CreateDemo(Store.Today)),
        keyFilePath: null);

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (_options.UseSampleData)
        {
            await UseSampleDataAsync();
            return;
        }

        var keyFile = CredentialLocator.Find(Settings.CredentialsPath, AppContext.BaseDirectory);
        if (keyFile is null)
        {
            _setup.ShowPrompt(error: null);
            return;
        }

        await ConnectAsync(async () => await FirestoreLibraryRepository.CreateAsync(keyFile), keyFile);
    }

    private Panel BuildSidebar()
    {
        var navigation = new TableLayoutPanel
        {
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            BackColor = Theme.Sidebar,
        };
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        foreach (var (page, caption) in NavigationItems)
        {
            var button = new NavButton(caption);
            button.Click += (_, _) => Navigate(page);
            _navigation[page] = button;
            navigation.Controls.Add(button);
        }

        var sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 236,
            BackColor = Theme.Sidebar,
            Padding = new Padding(14, 16, 14, 16),
        };

        sidebar.Controls.Add(navigation);
        sidebar.Controls.Add(new BrandHeader());
        sidebar.Controls.Add(_connection);
        return sidebar;
    }

    /// <summary>
    /// Switches the app to a new data source. On failure the previous one (if any) stays
    /// active; if there was none, the setup screen explains what went wrong.
    /// </summary>
    private async Task<bool> ConnectAsync(Func<Task<ILibraryRepository>> open, string? keyFilePath)
    {
        if (_busy)
        {
            return false;
        }

        var firstConnection = !Store.IsConnected;
        if (firstConnection)
        {
            _setup.ShowConnecting();
        }

        SetBusy(true);
        try
        {
            await Store.ConnectAsync(await open());
        }
        catch (Exception exception)
        {
            var message = exception is LibraryException
                ? exception.Message
                : $"Something unexpected went wrong: {exception.Message}";

            LogCause(exception, "Connecting to the library failed.");

            if (firstConnection)
            {
                _setup.ShowPrompt(message);
            }
            else
            {
                Notify(message, NoticeKind.Error);
            }

            return false;
        }
        finally
        {
            SetBusy(false);
        }

        KeyFilePath = keyFilePath;
        _connection.State = Store.IsPersistent
            ? ("Connected", Store.SourceDescription, Theme.Success)
            : ("Sample data", "Changes are not saved", Theme.Warning);

        SetNavigationEnabled(true);

        if (firstConnection)
        {
            await OpenStartPageAsync();
        }
        else
        {
            Navigate(Page.Dashboard);
            Notify($"Now using: {Store.SourceDescription}", NoticeKind.Info);
        }

        return true;
    }

    private async Task OpenStartPageAsync()
    {
        var today = Store.Today;

        if (Settings.ShowDailyReportOnStartup && Settings.LastReportShownOn != today)
        {
            Settings.LastReportShownOn = today;
            SaveSettings();
            Navigate(Page.Reports);
        }
        else
        {
            Navigate(Page.Dashboard);
        }

        if (Settings.AutoSaveDailyReport && Settings.LastReportSavedOn != today && Store.IsPersistent)
        {
            await AutoSaveDailyReportAsync(today);
        }
    }

    private async Task AutoSaveDailyReportAsync(DateOnly today)
    {
        var report = Store.CreateDailyReport();
        var path = Path.Combine(ReportsFolder, DailyReportExporter.DefaultFileName(today));

        try
        {
            await Task.Run(() => DailyReportExporter.Export(report, path));
            Settings.LastReportSavedOn = today;
            SaveSettings();
            Notify($"Today's report was saved to {path}", NoticeKind.Info);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Log.Error(exception, "Auto-saving the daily report failed.");
            Notify($"Couldn't save today's report: {exception.Message}", NoticeKind.Error);
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        NativeTheme.UseDarkTitleBar(this);
    }

    private void Show(Control screen)
    {
        screen.Visible = true;
        screen.BringToFront();

        foreach (var other in _views.Values.Cast<Control>().Append(_setup))
        {
            if (other != screen)
            {
                other.Visible = false;
            }
        }

        if (_toast.Visible)
        {
            _toast.BringToFront();
        }
    }

    /// <summary>
    /// Validation messages ("enter a title") are not worth logging. Failures that wrap a
    /// technical cause, and anything unexpected, are: the user sees a plain sentence and
    /// the log keeps the details.
    /// </summary>
    private static void LogCause(Exception exception, string context)
    {
        if (exception is not LibraryException || exception.InnerException is not null)
        {
            Log.Error(exception, context);
        }
    }

    private void SetNavigationEnabled(bool enabled)
    {
        foreach (var button in _navigation.Values)
        {
            button.Enabled = enabled;
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        _busyBar.Busy = busy;
        UseWaitCursor = busy;
    }
}
