namespace LibraryManagement.App.Ui.Controls;

internal enum NoticeKind
{
    Success,
    Info,
    Error,
}

/// <summary>
/// A short message that slides in over the bottom of the window and goes away by itself,
/// so routine confirmations don't need a dialog box and a click.
/// </summary>
internal sealed class Toast : Control
{
    private const int MaxWidth = 720;

    private readonly System.Windows.Forms.Timer _timer = new();
    private NoticeKind _kind;

    public Toast()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        SetStyle(ControlStyles.Selectable, false);

        Visible = false;
        Font = Theme.Body;
        Cursor = Cursors.Hand;
        _timer.Tick += (_, _) => Dismiss();
        Click += (_, _) => Dismiss();
    }

    public void Show(string message, NoticeKind kind)
    {
        _kind = kind;
        Text = message.ReplaceLineEndings(" ");
        AccessibleName = Text;

        // Errors stay longer: they usually need reading twice.
        _timer.Stop();
        _timer.Interval = kind == NoticeKind.Error ? 9000 : 4500;
        _timer.Start();

        Reposition();
        Visible = true;
        BringToFront();
        Invalidate();
    }

    /// <summary>Keeps the toast centred along the bottom edge of its parent.</summary>
    public void Reposition()
    {
        if (Parent is not { } parent)
        {
            return;
        }

        var margin = LogicalToDeviceUnits(24);
        var textWidth = Drawing.MeasureWidth(Text, Font) + LogicalToDeviceUnits(56);
        var width = Math.Min(Math.Min(textWidth, LogicalToDeviceUnits(MaxWidth)), parent.ClientSize.Width - (2 * margin));
        var height = LogicalToDeviceUnits(46);
        SetBounds((parent.ClientSize.Width - width) / 2, parent.ClientSize.Height - height - margin, Math.Max(0, width), height);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Clear(Parent?.BackColor ?? Theme.Background);
        Drawing.Prepare(graphics);

        var accent = _kind switch
        {
            NoticeKind.Error => Theme.Danger,
            NoticeKind.Info => Theme.Info,
            _ => Theme.Success,
        };

        var bounds = Drawing.Face(this);
        var radius = LogicalToDeviceUnits(Theme.CornerRadius);
        Drawing.FillRounded(graphics, Theme.SurfaceRaised, bounds, radius);
        Drawing.StrokeRounded(graphics, accent, bounds, radius, LogicalToDeviceUnits(1));

        var dot = LogicalToDeviceUnits(10);
        using (var brush = new SolidBrush(accent))
        {
            graphics.FillEllipse(brush, LogicalToDeviceUnits(18), (Height - dot) / 2f, dot, dot);
        }

        var textBounds = RectangleF.FromLTRB(LogicalToDeviceUnits(40), 0, Width - LogicalToDeviceUnits(16), Height);
        Drawing.DrawText(graphics, Text, Font, Theme.Text, textBounds);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Dismiss()
    {
        _timer.Stop();
        Visible = false;
    }
}
