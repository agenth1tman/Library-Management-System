namespace LibraryManagement.Data;

/// <summary>
/// Finds the Firebase service-account key without any path being compiled into the app.
/// The key is a secret: it lives outside the repository and is never committed.
/// </summary>
public static class CredentialLocator
{
    /// <summary>Google's standard variable for pointing tools at a key file.</summary>
    public const string EnvironmentVariable = "GOOGLE_APPLICATION_CREDENTIALS";

    /// <summary>A key with this name next to the executable is picked up automatically.</summary>
    public const string DefaultFileName = "firebase-credentials.json";

    /// <summary>
    /// Returns the first existing key file among: the path chosen in the app, the
    /// <see cref="EnvironmentVariable"/>, and <see cref="DefaultFileName"/> in the app folder.
    /// </summary>
    public static string? Find(string? configuredPath, string appDirectory)
    {
        string?[] candidates =
        [
            configuredPath,
            Environment.GetEnvironmentVariable(EnvironmentVariable),
            Path.Combine(appDirectory, DefaultFileName),
        ];

        return candidates.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));
    }
}
