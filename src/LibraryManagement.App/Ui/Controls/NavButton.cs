using System.ComponentModel;

namespace LibraryManagement.App.Ui.Controls;

/// <summary>An entry in the sidebar. The current page gets a gold marker.</summary>
internal sealed class NavButton : Button
{
    private bool _hovered;
    private bool _selected;

    public NavButton(string text)
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);

        Text = text;
        Font = Theme.Nav;
        Height = 44;
        Dock = DockStyle.Fill;
        Margin = new Padding(0, 2, 0, 2);
        Cursor = Cursors.Hand;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            Invalidate();
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var graphics = pevent.Graphics;
        graphics.Clear(Theme.Sidebar);
        Drawing.Prepare(graphics);

        var bounds = Drawing.Face(this);
        var radius = LogicalToDeviceUnits(Theme.CornerRadius - 2);

        if (_selected)
        {
            Drawing.FillRounded(graphics, Theme.Surface, bounds, radius);

            var markerWidth = LogicalToDeviceUnits(4);
            var markerHeight = Height * 0.45f;
            var marker = new RectangleF(LogicalToDeviceUnits(8), (Height - markerHeight) / 2f, markerWidth, markerHeight);
            Drawing.FillRounded(graphics, Theme.Accent, marker, markerWidth / 2f);
        }
        else if (_hovered && Enabled)
        {
            Drawing.FillRounded(graphics, Drawing.Blend(Theme.Sidebar, Theme.Surface, 0.55f), bounds, radius);
        }

        if (Focused && ShowFocusCues)
        {
            Drawing.StrokeRounded(graphics, Theme.Border, bounds, radius, LogicalToDeviceUnits(2));
        }

        var color = !Enabled ? Theme.TextDisabled : _selected ? Theme.Accent : _hovered ? Theme.Text : Theme.TextMuted;
        var textBounds = RectangleF.FromLTRB(LogicalToDeviceUnits(24), 0, Width - LogicalToDeviceUnits(8), Height);
        Drawing.DrawText(graphics, Text, Font, color, textBounds);
    }
}
