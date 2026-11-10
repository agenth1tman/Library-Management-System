using System.ComponentModel;

namespace LibraryManagement.App.Ui.Controls;

/// <summary>A thin strip along the top of the window that animates while the app is talking to the database.</summary>
internal sealed class BusyBar : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 30 };
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
        BackColor = Theme.Background;

        _timer.Tick += (_, _) =>
        {
            _position = (_position + 0.02f) % 1.4f;
            Invalidate();
        };
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Busy
    {
        get => _timer.Enabled;
        set
        {
            _position = 0;
            _timer.Enabled = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        if (!Busy)
        {
            return;
        }

        var segment = Width * 0.4f;
        using var brush = new SolidBrush(Theme.Accent);
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
