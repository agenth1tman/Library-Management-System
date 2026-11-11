using System.ComponentModel;

namespace LibraryManagement.App.Ui.Controls;

/// <summary>An entry in the navy sidebar. The current page gets the school's gold marker.</summary>
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
        UseMnemonic = false;
        Font = Theme.Nav;
        Height = 36;
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.PageTab;
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
        Drawing.Prepare(graphics);

        var highlighted = Enabled && (_selected || _hovered);
        graphics.Clear(highlighted ? Theme.NavyHover : Theme.Navy);

        if (_selected)
        {
            using var gold = new SolidBrush(Theme.Gold);
            graphics.FillRectangle(gold, 0, 0, LogicalToDeviceUnits(3), Height);
        }

        if (Focused && ShowFocusCues)
        {
            using var pen = new Pen(Theme.OnNavyMuted, LogicalToDeviceUnits(1));
            graphics.DrawRectangle(pen, LogicalToDeviceUnits(5), LogicalToDeviceUnits(3), Width - LogicalToDeviceUnits(9), Height - LogicalToDeviceUnits(7));
        }

        var color = !Enabled ? Color.FromArgb(120, 124, 162) : _selected ? Theme.OnNavy : _hovered ? Theme.OnNavy : Theme.OnNavyMuted;
        var font = _selected ? Theme.Strong : Font;
        var textBounds = RectangleF.FromLTRB(LogicalToDeviceUnits(20), 0, Width - LogicalToDeviceUnits(8), Height);
        Drawing.DrawText(graphics, Text, font, color, textBounds);
    }
}
