namespace LibraryManagement.App.Ui;

/// <summary>
/// Every colour, font and measurement in one place.
/// <para>
/// The look is paper and ink: a light page, dark text and ruled tables, like a library
/// catalogue. The school's navy and gold appear in the sidebar; on the page, navy marks
/// the one main action of a screen and colour otherwise means status.
/// </para>
/// </summary>
internal static class Theme
{
    // Paper.
    public static readonly Color Page = Color.FromArgb(250, 248, 243);
    public static readonly Color Field = Color.White;
    public static readonly Color Hover = Color.FromArgb(240, 237, 229);
    public static readonly Color Selection = Color.FromArgb(226, 230, 246);
    public static readonly Color Rule = Color.FromArgb(226, 222, 211);
    public static readonly Color RuleStrong = Color.FromArgb(186, 181, 168);

    // Ink.
    public static readonly Color Ink = Color.FromArgb(27, 29, 51);
    public static readonly Color InkMuted = Color.FromArgb(96, 98, 118);
    public static readonly Color InkDisabled = Color.FromArgb(158, 158, 168);

    // The school's colours.
    public static readonly Color Navy = Color.FromArgb(41, 44, 86);
    public static readonly Color NavyHover = Color.FromArgb(57, 61, 114);
    public static readonly Color NavyPressed = Color.FromArgb(29, 31, 64);
    public static readonly Color Gold = Color.FromArgb(240, 197, 57);
    public static readonly Color OnNavy = Color.FromArgb(246, 244, 238);
    public static readonly Color OnNavyMuted = Color.FromArgb(176, 180, 214);

    // Status. Used on the status word itself, never as decoration.
    public static readonly Color Overdue = Color.FromArgb(176, 38, 38);
    public static readonly Color DueSoon = Color.FromArgb(150, 92, 0);
    public static readonly Color Available = Color.FromArgb(24, 118, 66);

    public const int CornerRadius = 4;
    public const int ControlHeight = 32;
    public const int Gap = 16;

    // Fonts are created once, after AppFonts.Load(), and live for the whole session.
    public static readonly Font Title = AppFonts.Title(20f);
    public static readonly Font Brand = AppFonts.Title(15f);
    public static readonly Font BookTitle = AppFonts.Title(15f);

    public static readonly Font Figure = AppFonts.Strong(15f);
    public static readonly Font Heading = AppFonts.Strong(11f);
    public static readonly Font Strong = AppFonts.Strong(10f);
    public static readonly Font Button = AppFonts.Strong(9.75f);
    public static readonly Font SmallStrong = AppFonts.Strong(9f);

    public static readonly Font Body = AppFonts.Body(10f);
    public static readonly Font Input = AppFonts.Body(10.5f);
    public static readonly Font Nav = AppFonts.Body(10.5f);
    public static readonly Font Small = AppFonts.Body(9f);
}
