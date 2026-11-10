namespace LibraryManagement.App.Ui;

/// <summary>Every colour, font and measurement in one place: navy and gold, after the school crest.</summary>
internal static class Theme
{
    // Surfaces, darkest to lightest.
    public static readonly Color Sidebar = Color.FromArgb(19, 21, 46);
    public static readonly Color Background = Color.FromArgb(26, 29, 60);
    public static readonly Color Surface = Color.FromArgb(36, 40, 80);
    public static readonly Color SurfaceRaised = Color.FromArgb(46, 51, 100);
    public static readonly Color Border = Color.FromArgb(64, 70, 130);

    // Text.
    public static readonly Color Text = Color.FromArgb(240, 241, 250);
    public static readonly Color TextMuted = Color.FromArgb(166, 171, 210);
    public static readonly Color TextDisabled = Color.FromArgb(110, 115, 160);

    // Accent and status colours.
    public static readonly Color Accent = Color.FromArgb(240, 197, 57);
    public static readonly Color AccentHover = Color.FromArgb(250, 214, 96);
    public static readonly Color AccentPressed = Color.FromArgb(214, 173, 40);
    public static readonly Color OnAccent = Color.FromArgb(26, 29, 60);
    public static readonly Color Success = Color.FromArgb(74, 214, 152);
    public static readonly Color Warning = Color.FromArgb(255, 178, 77);
    public static readonly Color Danger = Color.FromArgb(255, 112, 112);
    public static readonly Color Info = Color.FromArgb(125, 170, 255);

    public const int CornerRadius = 10;
    public const int Gap = 16;

    // Fonts are created once, after AppFonts.Load(), and live for the whole session.
    public static readonly Font Title = AppFonts.Display(21f);
    public static readonly Font Heading = AppFonts.Display(12.5f);
    public static readonly Font Nav = AppFonts.Display(11f);
    public static readonly Font Button = AppFonts.Display(10f);
    public static readonly Font StatValue = AppFonts.Display(24f);
    public static readonly Font Brand = AppFonts.Display(15f);

    public static readonly Font Body = AppFonts.Body(10f);
    public static readonly Font BodyStrong = AppFonts.Body(10f, FontStyle.Bold);
    public static readonly Font Small = AppFonts.Body(9f);
    public static readonly Font SmallStrong = AppFonts.Body(9f, FontStyle.Bold);
    public static readonly Font Input = AppFonts.Body(11f);
    public static readonly Font Large = AppFonts.Body(13f);
}
