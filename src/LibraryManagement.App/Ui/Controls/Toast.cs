namespace LibraryManagement.App.Ui.Controls;

internal enum NoticeKind
{
    Info,
    Error,
}

/// <summary>
/// A short message along the bottom of the window that goes away by itself. Reserved for
/// things that did not happen where the user is looking: a failed request, a report saved
/// in the background. Results of a button press are shown next to that button instead.
/// </summary>
internal sealed class Toast : Control
{
    private const int MaxWidth = 760;

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
        AccessibleRole = AccessibleRole.Alert;
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

        // Tells screen readers to announce the message (the web's aria-live).
        AccessibilityNotifyClients(AccessibleEvents.SystemAlert, -1);
    }

    /// <summary>Keeps the toast centred along the bottom edge of its parent.</summary>
    public void Reposition()
    {
        if (Parent is not { } parent)
        {
            return;
        }

        var margin = LogicalToDeviceUnits(20);
        var textWidth = Drawing.MeasureWidth(Text, Font) + LogicalToDeviceUnits(36);
        var width = Math.Min(Math.Min(textWidth, LogicalToDeviceUnits(MaxWidth)), parent.ClientSize.Width - (2 * margin));
        var height = LogicalToDeviceUnits(38);
        SetBounds((parent.ClientSize.Width - width) / 2, parent.ClientSize.Height - height - margin, Math.Max(0, width), height);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Clear(Parent?.BackColor ?? Theme.Page);
        Drawing.Prepare(graphics);

        var bounds = Drawing.Face(this);
        Drawing.FillRounded(graphics, _kind == NoticeKind.Error ? Theme.Overdue : Theme.Ink, bounds, LogicalToDeviceUnits(Theme.CornerRadius));

        var textBounds = RectangleF.FromLTRB(LogicalToDeviceUnits(16), 0, Width - LogicalToDeviceUnits(16), Height);
        Drawing.DrawText(graphics, Text, Font, Color.White, textBounds);
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
