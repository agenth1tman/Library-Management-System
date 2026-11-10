using System.Globalization;

namespace LibraryManagement.App.Infrastructure;

/// <summary>A minimal append-only error log, so problems on a librarian's PC can be diagnosed later.</summary>
internal static class Log
{
    private const long MaxFileBytes = 1024 * 1024;

    private static readonly object Gate = new();

    public static string FilePath => AppPaths.LogFile;

    public static void Error(Exception? exception, string? context = null)
    {
        if (exception is null)
        {
            return;
        }

        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

                if (File.Exists(FilePath) && new FileInfo(FilePath).Length > MaxFileBytes)
                {
                    File.Delete(FilePath);
                }

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                File.AppendAllText(FilePath, $"[{timestamp}] {context}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
            }
        }
        catch (Exception logFailure) when (logFailure is IOException or UnauthorizedAccessException)
        {
            // Logging is best-effort: failing to write the log must never break the app.
        }
    }
}
