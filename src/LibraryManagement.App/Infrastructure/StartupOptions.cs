namespace LibraryManagement.App.Infrastructure;

/// <summary>Command-line switches.</summary>
internal sealed record StartupOptions(bool UseSampleData)
{
    /// <summary><c>--demo</c> starts with the built-in sample library instead of Firebase.</summary>
    public static StartupOptions Parse(string[] args) =>
        new(UseSampleData: args.Contains("--demo", StringComparer.OrdinalIgnoreCase));
}
