using System.ComponentModel;

namespace LibraryManagement.App.Ui.Controls;

/// <summary>The school crest and the app's name at the top of the sidebar.</summary>
internal sealed class BrandHeader : Control
{
    public BrandHeader()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        SetStyle(ControlStyles.Selectable, false);

        Height = 92;
        Dock = DockStyle.Top;
        AccessibleRole = AccessibleRole.Graphic;
        AccessibleName = "Library Management System";
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Clear(Theme.Navy);
        Drawing.Prepare(graphics);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        var size = LogicalToDeviceUnits(44);
        var left = LogicalToDeviceUnits(18);
        var top = LogicalToDeviceUnits(22);

        if (EmbeddedAssets.Crest is { } crest)
        {
            graphics.DrawImage(crest, new Rectangle(left, top, size, size));
        }

        var textLeft = left + size + LogicalToDeviceUnits(10);
        var middle = top + (size / 2f);
        Drawing.DrawText(graphics, "Library", Theme.Brand, Theme.OnNavy, RectangleF.FromLTRB(textLeft, top - 4, Width, middle + 4), vertical: StringAlignment.Far);
        Drawing.DrawText(graphics, "Management System", Theme.Small, Theme.OnNavyMuted, RectangleF.FromLTRB(textLeft + 1, middle + 3, Width, top + size + 6), vertical: StringAlignment.Near);
    }
}

/// <summary>Two lines at the bottom of the sidebar saying where the data comes from.</summary>
internal sealed class ConnectionStatus : Control
{
    private string _title = "Not connected";
    private string _detail = string.Empty;
    private bool _warning;

    public ConnectionStatus()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        SetStyle(ControlStyles.Selectable, false);

        Height = 58;
        Dock = DockStyle.Bottom;
        AccessibleRole = AccessibleRole.StaticText;
        AccessibleName = _title;
    }

    /// <summary><c>Warning</c> draws the title in gold: the data on screen is not the real library.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public (string Title, string Detail, bool Warning) State
    {
        get => (_title, _detail, _warning);
        set
        {
            (_title, _detail, _warning) = value;
            AccessibleName = $"{_title}. {_detail}";
            AccessibilityNotifyClients(AccessibleEvents.NameChange, -1);
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Clear(Theme.Navy);
        Drawing.Prepare(graphics);

        var left = LogicalToDeviceUnits(20);
        var right = Width - LogicalToDeviceUnits(10);

        using (var pen = new Pen(Theme.NavyHover))
        {
            graphics.DrawLine(pen, left, 0, right, 0);
        }

        var top = LogicalToDeviceUnits(10);
        var line = LogicalToDeviceUnits(18);
        Drawing.DrawText(graphics, _title, Theme.SmallStrong, _warning ? Theme.Gold : Theme.OnNavy, RectangleF.FromLTRB(left, top, right, top + line));
        Drawing.DrawText(graphics, _detail, Theme.Small, Theme.OnNavyMuted, RectangleF.FromLTRB(left, top + line, right, top + (2 * line)));
    }
}
