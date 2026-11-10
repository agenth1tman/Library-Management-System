namespace LibraryManagement.App.Ui.Controls;

/// <summary>An on/off setting with a title and a one-line explanation.</summary>
internal sealed class ToggleSwitch : CheckBox
{
    private readonly string _description;

    public ToggleSwitch(string title, string description)
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);

        Text = title;
        _description = description;
        AutoSize = false;
        Height = 52;
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;
        Cursor = Cursors.Hand;
        AccessibleDescription = description;
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var graphics = pevent.Graphics;
        graphics.Clear(Parent?.BackColor ?? Theme.Surface);
        Drawing.Prepare(graphics);

        var trackWidth = LogicalToDeviceUnits(42);
        var trackHeight = LogicalToDeviceUnits(22);
        var track = new RectangleF(1, (Height - trackHeight) / 2f, trackWidth, trackHeight);
        Drawing.FillRounded(graphics, Checked ? Theme.Accent : Theme.Border, track, trackHeight / 2f);

        var knob = trackHeight - LogicalToDeviceUnits(6);
        var knobLeft = Checked ? track.Right - knob - LogicalToDeviceUnits(3) : track.Left + LogicalToDeviceUnits(3);
        using (var brush = new SolidBrush(Checked ? Theme.OnAccent : Theme.Text))
        {
            graphics.FillEllipse(brush, knobLeft, track.Top + LogicalToDeviceUnits(3), knob, knob);
        }

        if (Focused && ShowFocusCues)
        {
            var ring = RectangleF.Inflate(track, 1, 1);
            Drawing.StrokeRounded(graphics, Theme.Text, ring, ring.Height / 2f, LogicalToDeviceUnits(1));
        }

        var textLeft = track.Right + LogicalToDeviceUnits(14);
        var half = Height / 2f;
        Drawing.DrawText(graphics, Text, Theme.BodyStrong, Theme.Text, RectangleF.FromLTRB(textLeft, 0, Width, half + 1), vertical: StringAlignment.Far);
        Drawing.DrawText(graphics, _description, Theme.Small, Theme.TextMuted, RectangleF.FromLTRB(textLeft, half + 1, Width, Height), vertical: StringAlignment.Near);
    }
}
