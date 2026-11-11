using System.Text.Json;

namespace LibraryManagement.Configuration;

/// <summary>Loads and saves <see cref="AppSettings"/> as a JSON file.</summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public SettingsStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    /// <summary>Returns the saved settings, or defaults when the file is missing or unreadable.</summary>
    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_filePath), JsonOptions) ?? new AppSettings();
            }
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            // A damaged settings file must never stop the app from starting.
        }

        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_filePath))!);

        // Write to a temporary file first so a crash mid-write cannot corrupt the real one.
        var temporaryPath = _filePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporaryPath, _filePath, overwrite: true);
    }
}
