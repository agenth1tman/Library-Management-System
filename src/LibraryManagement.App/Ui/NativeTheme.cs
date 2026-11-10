using System.Runtime.InteropServices;

namespace LibraryManagement.App.Ui;

/// <summary>
/// Asks Windows to draw the parts the app does not paint itself (title bar, scroll bars)
/// in their dark variants. These are cosmetic extras: on a Windows version that does not
/// support them the calls simply do nothing.
/// </summary>
internal static class NativeTheme
{
    // DWMWA_USE_IMMERSIVE_DARK_MODE, available from Windows 10 version 2004.
    private const int DarkTitleBarAttribute = 20;

    public static void UseDarkTitleBar(Form form)
    {
        try
        {
            var enabled = 1;
            _ = DwmSetWindowAttribute(form.Handle, DarkTitleBarAttribute, ref enabled, sizeof(int));
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            // Older Windows: keep the standard title bar.
        }
    }

    public static void UseDarkScrollBars(Control control)
    {
        try
        {
            _ = SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            // Older Windows: keep the standard scroll bars.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int valueSize);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr window, string? appName, string? idList);
}
