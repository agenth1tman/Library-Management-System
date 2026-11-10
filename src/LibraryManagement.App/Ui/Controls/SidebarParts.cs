using System.ComponentModel;

namespace LibraryManagement.App.Ui.Controls;

/// <summary>The crest and app name at the top of the sidebar.</summary>
internal sealed class BrandHeader : Control
{
    public BrandHeader()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        SetStyle(ControlStyles.Selectable, false);

        Height = 88;
        Dock = DockStyle.Top;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Clear(Theme.Sidebar);
        Drawing.Prepare(graphics);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        var size = LogicalToDeviceUnits(46);
        var left = LogicalToDeviceUnits(6);
        var top = LogicalToDeviceUnits(8);

        if (EmbeddedAssets.Crest is { } crest)
        {
            graphics.DrawImage(crest, new Rectangle(left, top, size, size));
        }

        var textLeft = left + size + LogicalToDeviceUnits(12);
        var middle = top + (size / 2f);
        Drawing.DrawText(graphics, "Library", Theme.Brand, Theme.Text, RectangleF.FromLTRB(textLeft, top - 2, Width, middle + 3), vertical: StringAlignment.Far);
        Drawing.DrawText(graphics, "Management System", Theme.Small, Theme.TextMuted, RectangleF.FromLTRB(textLeft + 1, middle + 3, Width, top + size + 4), vertical: StringAlignment.Near);
    }
}

/// <summary>Shows, at the bottom of the sidebar, where the data is coming from.</summary>
internal sealed class ConnectionBadge : Control
{
    private string _title = "Not connected";
    private string _detail = "Choose a data source";
    private Color _dot = Theme.TextDisabled;

    public ConnectionBadge()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        SetStyle(ControlStyles.Selectable, false);

        Height = 52;
        Dock = DockStyle.Bottom;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public (string Title, string Detail, Color Dot) State
    {
        get => (_title, _detail, _dot);
        set
        {
            (_title, _detail, _dot) = value;
            AccessibleName = $"{_title}. {_detail}";
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Clear(Theme.Sidebar);
        Drawing.Prepare(graphics);

        var bounds = Drawing.Face(this);
        Drawing.FillRounded(graphics, Drawing.Blend(Theme.Sidebar, Theme.Surface, 0.5f), bounds, LogicalToDeviceUnits(Theme.CornerRadius - 2));

        var dot = LogicalToDeviceUnits(9);
        var left = LogicalToDeviceUnits(14);
        using (var brush = new SolidBrush(_dot))
        {
            graphics.FillEllipse(brush, left, (Height - dot) / 2f, dot, dot);
        }

        var textLeft = left + dot + LogicalToDeviceUnits(10);
        var half = Height / 2f;
        Drawing.DrawText(graphics, _title, Theme.SmallStrong, Theme.Text, RectangleF.FromLTRB(textLeft, 0, Width - 6, half + 1), vertical: StringAlignment.Far);
        Drawing.DrawText(graphics, _detail, Theme.Small, Theme.TextMuted, RectangleF.FromLTRB(textLeft, half + 1, Width - 6, Height), vertical: StringAlignment.Near);
    }
}
