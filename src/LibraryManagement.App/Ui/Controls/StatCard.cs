using System.ComponentModel;

namespace LibraryManagement.App.Ui.Controls;

/// <summary>A headline number with a caption, e.g. "3 Overdue".</summary>
internal sealed class StatCard : Control
{
    private readonly string _caption;
    private readonly Color _accent;
    private string _value = "–";

    public StatCard(string caption, Color accent)
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        SetStyle(ControlStyles.Selectable, false);

        _caption = caption;
        _accent = accent;
        Height = 104;
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;
        AccessibleName = caption;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Value
    {
        get => _value;
        set
        {
            _value = value;
            AccessibleDescription = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Clear(Parent?.BackColor ?? Theme.Background);
        Drawing.Prepare(graphics);

        var bounds = Drawing.Face(this);
        Drawing.FillRounded(graphics, Theme.Surface, bounds, LogicalToDeviceUnits(Theme.CornerRadius + 2));

        var padding = LogicalToDeviceUnits(18);
        var dot = LogicalToDeviceUnits(8);
        var captionHeight = LogicalToDeviceUnits(20);

        using (var brush = new SolidBrush(_accent))
        {
            graphics.FillEllipse(brush, padding, padding + ((captionHeight - dot) / 2f), dot, dot);
        }

        var captionBounds = RectangleF.FromLTRB(padding + dot + LogicalToDeviceUnits(8), padding, Width - padding, padding + captionHeight);
        Drawing.DrawText(graphics, _caption, Theme.Small, Theme.TextMuted, captionBounds);

        var valueBounds = RectangleF.FromLTRB(padding - LogicalToDeviceUnits(2), padding + captionHeight, Width - padding, Height - LogicalToDeviceUnits(10));
        Drawing.DrawText(graphics, _value, Theme.StatValue, Theme.Text, valueBounds);
    }
}
