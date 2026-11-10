using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace LibraryManagement.App.Ui.Controls;

/// <summary>A single-line text field with a rounded border that turns gold while focused.</summary>
internal sealed class InputBox : Control
{
    private const int HorizontalPadding = 12;

    private readonly TextBox _textBox;

    public InputBox(string placeholder = "")
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        SetStyle(ControlStyles.Selectable, false);

        _textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Theme.SurfaceRaised,
            ForeColor = Theme.Text,
            Font = Theme.Input,
            PlaceholderText = placeholder,
        };

        _textBox.TextChanged += (_, e) => OnTextChanged(e);
        _textBox.GotFocus += (_, _) => Invalidate();
        _textBox.LostFocus += (_, _) => Invalidate();
        _textBox.KeyDown += OnTextBoxKeyDown;

        // The text box picks its own height from the font; re-centre whenever that changes.
        _textBox.SizeChanged += (_, _) => PerformLayout();

        Controls.Add(_textBox);
        Size = new Size(240, 40);
        Margin = Padding.Empty;
        Cursor = Cursors.IBeam;
    }

    /// <summary>Raised when Enter is pressed. Barcode scanners "press" Enter after each scan.</summary>
    public event EventHandler? EnterPressed;

    [AllowNull]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public override string Text
    {
        get => _textBox.Text;
        set => _textBox.Text = value ?? string.Empty;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int MaxLength
    {
        get => _textBox.MaxLength;
        set => _textBox.MaxLength = value;
    }

    public void FocusInput()
    {
        _textBox.Focus();
        _textBox.SelectAll();
    }

    protected override void OnClick(EventArgs e)
    {
        _textBox.Focus();
        base.OnClick(e);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);

        var padding = LogicalToDeviceUnits(HorizontalPadding);
        _textBox.SetBounds(padding, (Height - _textBox.Height) / 2, Math.Max(0, Width - (2 * padding)), _textBox.Height);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Clear(Parent?.BackColor ?? Theme.Background);
        Drawing.Prepare(graphics);

        var bounds = Drawing.Face(this);
        var radius = LogicalToDeviceUnits(Theme.CornerRadius - 2);
        Drawing.FillRounded(graphics, _textBox.BackColor, bounds, radius);

        var focused = _textBox.Focused;
        Drawing.StrokeRounded(
            graphics,
            focused ? Theme.Accent : Theme.Border,
            bounds,
            radius,
            LogicalToDeviceUnits(focused ? 2 : 1));
    }

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.Handled = true;
            e.SuppressKeyPress = true; // no "ding"
            EnterPressed?.Invoke(this, EventArgs.Empty);
        }
    }
}
