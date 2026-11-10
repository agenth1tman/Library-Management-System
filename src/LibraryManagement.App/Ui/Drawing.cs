using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace LibraryManagement.App.Ui;

/// <summary>Small GDI+ helpers shared by the custom-drawn controls.</summary>
internal static class Drawing
{
    public static void Prepare(Graphics graphics)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        // Every custom control paints text on an opaque background, which ClearType needs.
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
    }

    /// <summary>
    /// The control's full face as a drawable rectangle. One pixel smaller than its size,
    /// because shapes are measured between pixel centres; otherwise the right and bottom
    /// edges would be cut off.
    /// </summary>
    public static RectangleF Face(Control control) => new(0, 0, control.Width - 1, control.Height - 1);

    public static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));

        if (diameter <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void FillRounded(Graphics graphics, Color color, RectangleF bounds, float radius)
    {
        using var path = RoundedRectangle(bounds, radius);
        using var brush = new SolidBrush(color);
        using var pen = new Pen(color, 1f);
        graphics.FillPath(brush, path);

        // Outlining in the same colour gives the curve a properly smoothed edge.
        graphics.DrawPath(pen, path);
    }

    public static void StrokeRounded(Graphics graphics, Color color, RectangleF bounds, float radius, float width = 1f)
    {
        // Keep thicker strokes inside the bounds so they are not clipped at the control's edge.
        var inset = (width - 1) / 2f;
        bounds.Inflate(-inset, -inset);
        using var path = RoundedRectangle(bounds, radius);
        using var pen = new Pen(color, width);
        graphics.DrawPath(pen, path);
    }

    public static void DrawText(
        Graphics graphics,
        string? text,
        Font font,
        Color color,
        RectangleF bounds,
        StringAlignment horizontal = StringAlignment.Near,
        StringAlignment vertical = StringAlignment.Center)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        using var format = new StringFormat(StringFormatFlags.NoWrap)
        {
            Alignment = horizontal,
            LineAlignment = vertical,
            Trimming = StringTrimming.EllipsisCharacter,
        };
        using var brush = new SolidBrush(color);
        graphics.DrawString(text, font, brush, bounds, format);
    }

    /// <summary>Width of <paramref name="text"/> in pixels at the current screen DPI.</summary>
    public static int MeasureWidth(string text, Font font)
    {
        using var bitmap = new Bitmap(1, 1);
        using var graphics = Graphics.FromImage(bitmap);
        return (int)Math.Ceiling(graphics.MeasureString(text, font).Width);
    }

    public static Color Blend(Color from, Color to, float amount) => Color.FromArgb(
        (int)(from.R + ((to.R - from.R) * amount)),
        (int)(from.G + ((to.G - from.G) * amount)),
        (int)(from.B + ((to.B - from.B) * amount)));
}
