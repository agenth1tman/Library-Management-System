using System.Drawing.Text;
using System.Runtime.InteropServices;
using LibraryManagement.App.Infrastructure;

namespace LibraryManagement.App.Ui;

/// <summary>
/// Loads the app's display font (Starz 2016) from inside the executable.
/// <para>
/// The font is an embedded resource, so it works on any computer whether or not it is
/// installed there. If loading ever fails, the app falls back to Segoe UI, which ships
/// with every supported version of Windows, rather than refusing to start.
/// </para>
/// </summary>
internal static class AppFonts
{
    private const string DisplayFontResource = "Assets.starz_2016.ttf";
    private const string BodyFontName = "Segoe UI";

    private static readonly PrivateFontCollection Collection = new();

    private static FontFamily? _displayFamily;

    // GDI+ keeps reading from this memory for as long as the collection lives, so it is
    // deliberately never freed. (Freeing it right after loading was a bug in the old code.)
    private static IntPtr _fontMemory;

    /// <summary>True when the bundled display font is in use (false means the fallback is).</summary>
    public static bool DisplayFontLoaded => _displayFamily is not null;

    public static void Load()
    {
        try
        {
            using var stream = typeof(AppFonts).Assembly.GetManifestResourceStream(DisplayFontResource)
                ?? throw new InvalidOperationException($"Embedded font '{DisplayFontResource}' is missing from the build.");

            var data = new byte[stream.Length];
            stream.ReadExactly(data);

            _fontMemory = Marshal.AllocCoTaskMem(data.Length);
            Marshal.Copy(data, 0, _fontMemory, data.Length);

            // For GDI+ (Graphics.DrawString).
            Collection.AddMemoryFont(_fontMemory, data.Length);

            // For GDI (TextRenderer and native controls). Private to this process: nothing is installed.
            uint installed = 0;
            AddFontMemResourceEx(_fontMemory, (uint)data.Length, IntPtr.Zero, ref installed);

            var family = Collection.Families.FirstOrDefault()
                ?? throw new InvalidOperationException("The embedded font contains no font family.");

            if (!family.IsStyleAvailable(FontStyle.Regular))
            {
                throw new InvalidOperationException("The embedded font has no regular style.");
            }

            _displayFamily = family;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Loading the bundled display font failed; falling back to Segoe UI.");
            _displayFamily = null;
        }
    }

    /// <summary>True when <paramref name="font"/> is the bundled display font.</summary>
    public static bool IsDisplayFont(Font font) =>
        _displayFamily is { } family && font.FontFamily.Name == family.Name;

    /// <summary>The brand font, used for headings, navigation, buttons and big numbers.</summary>
    public static Font Display(float size) => _displayFamily is { } family
        ? new Font(family, size, FontStyle.Regular, GraphicsUnit.Point)
        : new Font(BodyFontName, size, FontStyle.Bold, GraphicsUnit.Point);

    /// <summary>The reading font, used for data, forms and body text.</summary>
    public static Font Body(float size, FontStyle style = FontStyle.Regular) =>
        new(BodyFontName, size, style, GraphicsUnit.Point);

    [DllImport("gdi32.dll")]
    private static extern IntPtr AddFontMemResourceEx(IntPtr fontData, uint fontDataSize, IntPtr reserved, ref uint fontCount);
}
