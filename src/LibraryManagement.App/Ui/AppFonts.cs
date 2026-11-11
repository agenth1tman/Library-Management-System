using System.Drawing.Text;
using System.Runtime.InteropServices;
using LibraryManagement.App.Infrastructure;

namespace LibraryManagement.App.Ui;

/// <summary>
/// The app's two typefaces.
/// <para>
/// Titles use Crimson Text (SIL Open Font License 1.1, see Assets/Fonts/CrimsonText-OFL.txt),
/// loaded from inside the executable so it works on any computer without being installed.
/// Everything else uses Segoe UI, which ships with every supported version of Windows.
/// If the embedded font ever fails to load, titles fall back to Segoe UI rather than the
/// app refusing to start.
/// </para>
/// </summary>
internal static class AppFonts
{
    private const string TitleFontResource = "Assets.Fonts.CrimsonText-SemiBold.ttf";
    private const string BodyFamily = "Segoe UI";
    private const string StrongFamily = "Segoe UI Semibold";

    private static readonly PrivateFontCollection Collection = new();
    private static readonly Lazy<bool> HasStrongFamily = new(() => IsInstalled(StrongFamily));

    private static FontFamily? _titleFamily;

    // GDI+ keeps reading from this memory for as long as the collection lives, so it is
    // deliberately never freed.
    private static IntPtr _fontMemory;

    public static void Load()
    {
        try
        {
            using var stream = typeof(AppFonts).Assembly.GetManifestResourceStream(TitleFontResource)
                ?? throw new InvalidOperationException($"Embedded font '{TitleFontResource}' is missing from the build.");

            var data = new byte[stream.Length];
            stream.ReadExactly(data);

            _fontMemory = Marshal.AllocCoTaskMem(data.Length);
            Marshal.Copy(data, 0, _fontMemory, data.Length);

            // For GDI+ (Graphics.DrawString).
            Collection.AddMemoryFont(_fontMemory, data.Length);

            // For GDI (TextRenderer and native controls). Private to this process: nothing is installed.
            uint added = 0;
            AddFontMemResourceEx(_fontMemory, (uint)data.Length, IntPtr.Zero, ref added);

            var family = Collection.Families.FirstOrDefault()
                ?? throw new InvalidOperationException("The embedded font contains no font family.");

            if (!family.IsStyleAvailable(FontStyle.Regular))
            {
                throw new InvalidOperationException("The embedded font has no regular style.");
            }

            _titleFamily = family;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Loading the embedded title font failed; falling back to Segoe UI.");
            _titleFamily = null;
        }
    }

    /// <summary>True when <paramref name="font"/> is the embedded title typeface.</summary>
    public static bool IsTitleFont(Font font) =>
        _titleFamily is { } family && font.FontFamily.Name == family.Name;

    /// <summary>The title typeface: page titles and the brand, nothing else.</summary>
    public static Font Title(float size) => _titleFamily is { } family
        ? new Font(family, size, FontStyle.Regular, GraphicsUnit.Point)
        : Strong(size);

    /// <summary>The reading typeface: data, forms and body text.</summary>
    public static Font Body(float size) => new(BodyFamily, size, FontStyle.Regular, GraphicsUnit.Point);

    /// <summary>A heavier cut of the reading typeface for headings, buttons and emphasis.</summary>
    public static Font Strong(float size) => HasStrongFamily.Value
        ? new Font(StrongFamily, size, FontStyle.Regular, GraphicsUnit.Point)
        : new Font(BodyFamily, size, FontStyle.Bold, GraphicsUnit.Point);

    private static bool IsInstalled(string familyName)
    {
        // Asking for a missing family silently returns a substitute, so compare the names.
        using var probe = new Font(familyName, 10f);
        return string.Equals(probe.Name, familyName, StringComparison.OrdinalIgnoreCase);
    }

    [DllImport("gdi32.dll")]
    private static extern IntPtr AddFontMemResourceEx(IntPtr fontData, uint fontDataSize, IntPtr reserved, ref uint fontCount);
}
