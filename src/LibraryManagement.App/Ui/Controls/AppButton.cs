using System.ComponentModel;

namespace LibraryManagement.App.Ui.Controls;

internal enum ButtonKind
{
    /// <summary>The one main action on a screen (navy).</summary>
    Primary,

    /// <summary>Supporting actions.</summary>
    Secondary,

    /// <summary>Destructive actions such as delete.</summary>
    Danger,

    /// <summary>A text-only filter that is underlined while <see cref="AppButton.Selected"/>.</summary>
    Tab,
}

/// <summary>A flat button drawn to match the theme, with hover, pressed, focus and disabled looks.</summary>
internal sealed class AppButton : Button
{
    private bool _hovered;
    private bool _pressed;
    private bool _selected;

    /// <summary>How much WinForms has scaled this button for the display's DPI so far.</summary>
    private float _dpiScale = 1f;

    public AppButton(string text, ButtonKind kind = ButtonKind.Secondary)
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);

        Kind = kind;
        Font = Theme.Button;
        Text = text;
        UseMnemonic = false;
        Cursor = Cursors.Hand;
        Margin = Padding.Empty;
        AccessibleRole = kind == ButtonKind.Tab ? AccessibleRole.PageTab : AccessibleRole.PushButton;
        FitToText();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ButtonKind Kind { get; }

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

    /// <summary>Changes the caption and resizes the button to fit it (used for tabs with counts).</summary>
    public void SetText(string text)
    {
        if (Text == text)
        {
            return;
        }

        Text = text;
        Width = (int)Math.Round(WidthFor(text) * _dpiScale);
        Invalidate();
    }

    protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
    {
        base.ScaleControl(factor, specified);
        _dpiScale *= factor.Width;
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
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        _pressed = mevent.Button == MouseButtons.Left;
        Invalidate();
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var graphics = pevent.Graphics;
        var surroundings = Parent?.BackColor ?? Theme.Page;
        graphics.Clear(surroundings);
        Drawing.Prepare(graphics);

        var bounds = Drawing.Face(this);
        var radius = LogicalToDeviceUnits(Theme.CornerRadius);

        if (Kind == ButtonKind.Tab)
        {
            PaintTab(graphics, bounds, radius);
            return;
        }

        var (fill, border, text) = Colors();
        Drawing.FillRounded(graphics, fill, bounds, radius);
        Drawing.StrokeRounded(graphics, border, bounds, radius, LogicalToDeviceUnits(1));

        if (Focused && ShowFocusCues)
        {
            // A second ring just inside the edge, visible on both light and navy buttons.
            var ring = RectangleF.Inflate(bounds, -LogicalToDeviceUnits(3), -LogicalToDeviceUnits(3));
            Drawing.StrokeRounded(graphics, text, ring, Math.Max(1, radius - 2), LogicalToDeviceUnits(1));
        }

        Drawing.DrawText(graphics, Text, Font, text, bounds, StringAlignment.Center);
    }

    private void PaintTab(Graphics graphics, RectangleF bounds, int radius)
    {
        if ((_hovered || _pressed) && Enabled && !_selected)
        {
            Drawing.FillRounded(graphics, Theme.Hover, bounds, radius);
        }

        var color = !Enabled ? Theme.InkDisabled : _selected ? Theme.Ink : Theme.InkMuted;
        Drawing.DrawText(graphics, Text, Font, color, bounds, StringAlignment.Center);

        if (_selected)
        {
            var thickness = LogicalToDeviceUnits(2);
            var inset = LogicalToDeviceUnits(8);
            using var brush = new SolidBrush(Theme.Navy);
            graphics.FillRectangle(brush, inset, Height - thickness, Width - (2 * inset), thickness);
        }

        if (Focused && ShowFocusCues)
        {
            Drawing.StrokeRounded(graphics, Theme.Navy, bounds, radius, LogicalToDeviceUnits(1));
        }
    }

    private (Color Fill, Color Border, Color Text) Colors()
    {
        if (!Enabled)
        {
            return (Theme.Hover, Theme.Rule, Theme.InkDisabled);
        }

        switch (Kind)
        {
            case ButtonKind.Primary:
                var navy = _pressed ? Theme.NavyPressed : _hovered ? Theme.NavyHover : Theme.Navy;
                return (navy, navy, Theme.OnNavy);

            case ButtonKind.Danger:
                return _hovered || _pressed
                    ? (Theme.Overdue, Theme.Overdue, Color.White)
                    : (Theme.Field, Theme.RuleStrong, Theme.Overdue);

            default:
                var fill = _pressed ? Theme.Rule : _hovered ? Theme.Hover : Theme.Field;
                return (fill, _hovered || _pressed ? Theme.InkMuted : Theme.RuleStrong, Theme.Ink);
        }
    }

    private void FitToText() => Size = new Size(WidthFor(Text), Theme.ControlHeight);

    /// <summary>The button's width for <paramref name="text"/>, in 96-DPI units.</summary>
    private int WidthFor(string text)
    {
        var padding = Kind == ButtonKind.Tab ? 10 : 16;
        return (Drawing.MeasureWidth(text, Font) * 96 / DeviceDpi) + (2 * padding);
    }
}
