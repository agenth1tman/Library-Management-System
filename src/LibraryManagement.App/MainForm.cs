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
    /// <summary>In sidebar order. Ctrl+1 to Ctrl+6 open them.</summary>
    private static readonly (Page Page, string Caption)[] NavigationItems =
    [
        (Page.Today, "Today"),
        (Page.Library, "Library"),
        (Page.Desk, "Issue & return"),
        (Page.Register, "Register"),
        (Page.Reports, "Daily report"),
        (Page.Settings, "Settings"),
    ];

    private readonly StartupOptions _options;
    private readonly SettingsStore _settingsStore = new(AppPaths.SettingsFile);

    private readonly Dictionary<Page, ViewBase> _views = [];
    private readonly Dictionary<Page, NavButton> _navigation = [];
    private readonly SetupView _setup;
    private readonly ManageView _desk;
    private readonly BusyBar _busyBar = new();
    private readonly Toast _toast = new();
    private readonly ConnectionStatus _connection = new();

    private Page _currentPage = Page.Today;
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
        BackColor = Theme.Page;
        ForeColor = Theme.Ink;
        Font = Theme.Body;
        ClientSize = new Size(1240, 760);
        MinimumSize = new Size(1120, 680);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        _desk = new ManageView(this);
        _views[Page.Today] = new TodayView(this);
        _views[Page.Library] = new LibraryView(this);
        _views[Page.Desk] = _desk;
        _views[Page.Register] = new RegisterView(this);
        _views[Page.Reports] = new ReportsView(this);
        _views[Page.Settings] = new SettingsView(this);
        _setup = new SetupView(this);

        var content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Page };
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
            Notify($"Couldn’t save your settings: {exception.Message}", NoticeKind.Error);
        }
    }

    public void Navigate(Page page)
    {
        if (!Store.IsConnected)
        {
            return;
        }

        // Leaving the desk with a half-edited record asks first.
        if (_currentPage == Page.Desk && page != Page.Desk && !_desk.ConfirmDiscardEdits())
        {
            return;
        }

        _currentPage = page;
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
        _desk.Open(book);
        Navigate(Page.Desk);
    }

    public void LookUpBook(string query)
    {
        Navigate(Page.Desk);
        _desk.LookUp(query);
    }

    public void Notify(string message, NoticeKind kind = NoticeKind.Info) => _toast.Show(message, kind);

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
            // Expected problems (no connection, a record changed elsewhere) come with a message meant for the user.
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

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        RestoreWindowPlacement();
    }

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

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing && !_desk.ConfirmDiscardEdits())
        {
            e.Cancel = true;
        }

        base.OnFormClosing(e);

        if (!e.Cancel)
        {
            RememberWindowPlacement();
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Ctrl+1 … Ctrl+6 open the screens in sidebar order; F5 reloads from the database.
        for (var index = 0; index < NavigationItems.Length; index++)
        {
            if (keyData == (Keys.Control | (Keys.D1 + index)) && Store.IsConnected)
            {
                Navigate(NavigationItems[index].Page);
                return true;
            }
        }

        if (keyData == Keys.F5 && Store.IsConnected)
        {
            _ = RunAsync(() => Store.RefreshAsync());
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private Panel BuildSidebar()
    {
        var navigation = new TableLayoutPanel
        {
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            BackColor = Theme.Navy,
        };
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        foreach (var (page, caption) in NavigationItems)
        {
            var button = new NavButton(caption);
            button.Click += (_, _) => Navigate(page);
            _navigation[page] = button;
            navigation.Controls.Add(button);
        }

        var sidebar = new Panel { Dock = DockStyle.Left, Width = 208, BackColor = Theme.Navy };
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
            ? ("Connected", Store.SourceDescription, false)
            : ("Sample data", "Nothing is saved", true);

        SetNavigationEnabled(true);

        if (firstConnection)
        {
            await OpenStartPageAsync();
        }
        else
        {
            Navigate(Page.Today);
            Notify($"Now using {Store.SourceDescription}.");
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
            Navigate(Page.Today);
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
            Notify($"Today’s report was saved to {path}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Log.Error(exception, "Auto-saving the daily report failed.");
            Notify($"Couldn’t save today’s report: {exception.Message}", NoticeKind.Error);
        }
    }

    /// <summary>Reopens the window where it was last time, unless that spot is no longer on any screen.</summary>
    private void RestoreWindowPlacement()
    {
        if (Settings.Window is not { } saved)
        {
            return;
        }

        var bounds = new Rectangle(saved.Left, saved.Top, saved.Width, saved.Height);
        var visibleSomewhere = Screen.AllScreens.Any(screen =>
        {
            var overlap = Rectangle.Intersect(screen.WorkingArea, bounds);
            return overlap.Width >= 200 && overlap.Height >= 100;
        });

        if (visibleSomewhere && bounds.Width >= MinimumSize.Width && bounds.Height >= MinimumSize.Height)
        {
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
        }

        if (saved.Maximized)
        {
            WindowState = FormWindowState.Maximized;
        }
    }

    private void RememberWindowPlacement()
    {
        var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        Settings.Window = new WindowPlacement(bounds.Left, bounds.Top, bounds.Width, bounds.Height, WindowState == FormWindowState.Maximized);
        SaveSettings();
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
    /// Failures that wrap a technical cause, and anything unexpected, are logged: the user
    /// sees a plain sentence and the log keeps the details.
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
