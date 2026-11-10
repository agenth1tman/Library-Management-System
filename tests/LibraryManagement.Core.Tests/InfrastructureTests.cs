using Grpc.Core;
using LibraryManagement.Configuration;
using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Reporting;
using OfficeOpenXml;

namespace LibraryManagement.Tests;

public sealed class InfrastructureTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "lms-tests-" + Guid.NewGuid().ToString("N"));

    public InfrastructureTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void Settings_round_trip_through_the_file()
    {
        var store = new SettingsStore(Path.Combine(_folder, "nested", "settings.json"));
        var settings = new AppSettings
        {
            ShowDailyReportOnStartup = true,
            AutoSaveDailyReport = true,
            CredentialsPath = @"C:\keys\library.json",
            ReportsFolder = @"D:\Reports",
            LastReportSavedOn = new DateOnly(2026, 3, 10),
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.True(loaded.ShowDailyReportOnStartup);
        Assert.True(loaded.AutoSaveDailyReport);
        Assert.Equal(settings.CredentialsPath, loaded.CredentialsPath);
        Assert.Equal(settings.ReportsFolder, loaded.ReportsFolder);
        Assert.Equal(settings.LastReportSavedOn, loaded.LastReportSavedOn);
        Assert.Null(loaded.LastReportShownOn);
    }

    [Fact]
    public void Missing_or_corrupt_settings_fall_back_to_defaults()
    {
        var path = Path.Combine(_folder, "settings.json");
        var store = new SettingsStore(path);

        Assert.False(store.Load().AutoSaveDailyReport);

        File.WriteAllText(path, "{ this is not json");

        Assert.False(store.Load().AutoSaveDailyReport);
    }

    [Fact]
    public void Credential_lookup_prefers_the_configured_path_then_the_app_folder()
    {
        var configured = Path.Combine(_folder, "chosen.json");
        var besideApp = Path.Combine(_folder, CredentialLocator.DefaultFileName);

        Assert.Null(CredentialLocator.Find(configuredPath: null, _folder));
        Assert.Null(CredentialLocator.Find(configured, _folder));

        File.WriteAllText(besideApp, "{}");
        Assert.Equal(besideApp, CredentialLocator.Find(configured, _folder));

        File.WriteAllText(configured, "{}");
        Assert.Equal(configured, CredentialLocator.Find(configured, _folder));
    }

    [Fact]
    public async Task A_missing_key_file_is_reported_in_plain_words()
    {
        var error = await Assert.ThrowsAsync<LibraryException>(
            () => FirestoreLibraryRepository.CreateAsync(Path.Combine(_folder, "nope.json")));

        Assert.Contains("not found", error.Message);
    }

    [Fact]
    public async Task A_file_that_is_not_a_service_account_key_is_rejected()
    {
        var path = Path.Combine(_folder, "random.json");
        File.WriteAllText(path, """{ "hello": "world" }""");

        var error = await Assert.ThrowsAsync<LibraryException>(() => FirestoreLibraryRepository.CreateAsync(path));

        Assert.Contains("not a valid Firebase service-account key", error.Message);
    }

    [Fact]
    public void Network_failures_are_explained_without_technical_jargon()
    {
        var unreachable = new RpcException(new Status(StatusCode.Unavailable, "failed to connect to all addresses"));
        var cannotStart = new RpcException(new Status(
            StatusCode.Internal,
            "Error starting gRPC call.",
            new HttpRequestException("Unable to get subchannel from HttpRequestMessage.")));
        var denied = new RpcException(new Status(StatusCode.PermissionDenied, "Missing or insufficient permissions."));
        var odd = new RpcException(new Status(StatusCode.ResourceExhausted, "Quota exceeded for quota metric 'x'."));

        Assert.StartsWith("Can't reach the library database", FirestoreLibraryRepository.Explain(unreachable));
        Assert.StartsWith("Can't reach the library database", FirestoreLibraryRepository.Explain(cannotStart));
        Assert.StartsWith("Firebase rejected the key file", FirestoreLibraryRepository.Explain(denied));
        Assert.Contains("took too long", FirestoreLibraryRepository.Explain(new TaskCanceledException()));
        Assert.DoesNotContain("quota metric", FirestoreLibraryRepository.Explain(odd));
        Assert.Contains("ResourceExhausted", FirestoreLibraryRepository.Explain(odd));
    }

    [Fact]
    public void Export_writes_a_workbook_with_the_summary_and_the_books_to_chase()
    {
        var today = TestData.Today;
        Book[] books =
        [
            TestData.Book("0"),
            TestData.Book("1", "Late Book", TestData.LoanDue(today.AddDays(-4))),
            TestData.Book("2", "Soon Book", TestData.LoanDue(today.AddDays(1), TestData.Teacher)),
        ];
        var path = Path.Combine(_folder, "reports", DailyReportExporter.DefaultFileName(today));

        DailyReportExporter.Export(DailyReport.Create(books, today), path);

        Assert.EndsWith("DailyReport-2026-03-10.xlsx", path);
        using var package = new ExcelPackage(new FileInfo(path));
        var summary = package.Workbook.Worksheets["Summary"];
        Assert.Equal("Total books", summary.Cells[4, 1].Text);
        Assert.Equal(3, summary.Cells[4, 2].GetValue<int>());
        Assert.Equal("Overdue", summary.Cells[9, 1].Text);
        Assert.Equal(1, summary.Cells[9, 2].GetValue<int>());

        var sheet = package.Workbook.Worksheets["Needs attention"];
        Assert.Equal("Overdue", sheet.Cells[2, 1].Text);
        Assert.Equal("Late Book", sheet.Cells[2, 3].Text);
        Assert.Equal(4, sheet.Cells[2, 12].GetValue<int>());
        Assert.Equal("Due soon", sheet.Cells[3, 1].Text);
        Assert.Equal("Physics Teacher", sheet.Cells[3, 9].Text);
        Assert.Equal(3, sheet.Dimension.End.Row);
    }

    [Fact]
    public void Export_of_a_quiet_day_says_so_instead_of_leaving_an_empty_sheet()
    {
        var path = Path.Combine(_folder, "quiet.xlsx");

        DailyReportExporter.Export(DailyReport.Create([TestData.Book("0")], TestData.Today), path);

        using var package = new ExcelPackage(new FileInfo(path));
        Assert.Contains("Nothing is overdue", package.Workbook.Worksheets["Needs attention"].Cells[2, 1].Text);
    }
}
