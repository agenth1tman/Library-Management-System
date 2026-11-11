using System.ComponentModel;

namespace LibraryManagement.App.Ui.Controls;

/// <summary>A thin strip along the top of the page that shows the app is talking to the database.</summary>
internal sealed class BusyBar : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 30 };
    private bool _busy;
    private float _position;

    public BusyBar()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        SetStyle(ControlStyles.Selectable, false);

        Height = 3;
        Dock = DockStyle.Top;
        BackColor = Theme.Page;
        AccessibleRole = AccessibleRole.ProgressBar;
        AccessibleName = "Working";

        _timer.Tick += (_, _) =>
        {
            _position = (_position + 0.02f) % 1.4f;
            Invalidate();
        };
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Busy
    {
        get => _busy;
        set
        {
            _busy = value;
            _position = 0;

            // With Windows' "Show animations" turned off the bar is simply shown, not moved.
            _timer.Enabled = value && Drawing.AnimationsEnabled;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        if (!_busy)
        {
            return;
        }

        using var brush = new SolidBrush(Theme.Navy);
        if (!_timer.Enabled)
        {
            e.Graphics.FillRectangle(brush, ClientRectangle);
            return;
        }

        var segment = Width * 0.4f;
        e.Graphics.FillRectangle(brush, (_position * Width) - segment, 0, segment, Height);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
        }

        base.Dispose(disposing);
    }
}
