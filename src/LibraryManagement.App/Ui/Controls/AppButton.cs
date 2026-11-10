using System.ComponentModel;

namespace LibraryManagement.App.Ui.Controls;

internal enum ButtonKind
{
    /// <summary>The one main action on a screen (gold).</summary>
    Primary,

    /// <summary>Supporting actions.</summary>
    Secondary,

    /// <summary>Destructive actions such as delete.</summary>
    Danger,

    /// <summary>A filter chip that stays highlighted while <see cref="AppButton.Selected"/>.</summary>
    Chip,
}

/// <summary>A flat, rounded button drawn to match the theme.</summary>
internal sealed class AppButton : Button
{
    private const int HorizontalPadding = 20;

    private bool _hovered;
    private bool _pressed;
    private bool _selected;

    public AppButton(string text, ButtonKind kind = ButtonKind.Secondary)
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);

        Kind = kind;
        Font = kind == ButtonKind.Chip ? Theme.SmallStrong : Theme.Button;
        Text = text;
        Cursor = Cursors.Hand;
        Margin = Padding.Empty;
        Size = new Size(
            Drawing.MeasureWidth(text, Font) * 96 / DeviceDpi + (2 * (kind == ButtonKind.Chip ? 14 : HorizontalPadding)),
            kind == ButtonKind.Chip ? 32 : 40);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ButtonKind Kind { get; set; }

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
        var surroundings = Parent?.BackColor ?? Theme.Background;
        graphics.Clear(surroundings);
        Drawing.Prepare(graphics);

        var (fill, border, text) = Colors(surroundings);
        var bounds = Drawing.Face(this);
        var radius = Kind == ButtonKind.Chip ? Height / 2f : LogicalToDeviceUnits(Theme.CornerRadius - 2);

        Drawing.FillRounded(graphics, fill, bounds, radius);
        if (border is { } borderColor)
        {
            Drawing.StrokeRounded(graphics, borderColor, bounds, radius, LogicalToDeviceUnits(1));
        }

        if (Focused && ShowFocusCues)
        {
            Drawing.StrokeRounded(graphics, Theme.Text, bounds, radius, LogicalToDeviceUnits(2));
        }

        Drawing.DrawText(graphics, Text, Font, text, bounds, StringAlignment.Center);
    }

    private (Color Fill, Color? Border, Color Text) Colors(Color surroundings)
    {
        if (!Enabled)
        {
            return (Drawing.Blend(surroundings, Theme.SurfaceRaised, 0.6f), null, Theme.TextDisabled);
        }

        switch (Kind)
        {
            case ButtonKind.Primary:
                return (_pressed ? Theme.AccentPressed : _hovered ? Theme.AccentHover : Theme.Accent, null, Theme.OnAccent);

            case ButtonKind.Danger:
                return _hovered || _pressed
                    ? (Drawing.Blend(Theme.Danger, Color.Black, _pressed ? 0.15f : 0f), null, Theme.OnAccent)
                    : (surroundings, Theme.Danger, Theme.Danger);

            case ButtonKind.Chip when _selected:
                return (Theme.Accent, null, Theme.OnAccent);

            default:
                var fill = _pressed ? Theme.Border : _hovered ? Drawing.Blend(Theme.SurfaceRaised, Theme.Border, 0.5f) : Theme.SurfaceRaised;
                return (fill, Theme.Border, Theme.Text);
        }
    }
}
