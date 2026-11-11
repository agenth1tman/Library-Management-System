using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace LibraryManagement.App.Ui.Controls;

/// <summary>A single-line text field. The border darkens on focus and turns red on an error.</summary>
internal sealed class InputBox : Control
{
    private const int HorizontalPadding = 9;

    private readonly TextBox _textBox;
    private bool _hasError;

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
            BackColor = Theme.Field,
            ForeColor = Theme.Ink,
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
        Size = new Size(240, Theme.ControlHeight);
        Margin = Padding.Empty;
        Cursor = Cursors.IBeam;
    }

    /// <summary>Raised when Enter is pressed. Barcode scanners "press" Enter after each scan.</summary>
    public event EventHandler? EnterPressed;

    /// <summary>Raised when Esc is pressed.</summary>
    public event EventHandler? EscapePressed;

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

    /// <summary>What a screen reader calls this field (its visible caption).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Caption
    {
        get => _textBox.AccessibleName ?? string.Empty;
        set => _textBox.AccessibleName = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool HasError
    {
        get => _hasError;
        set
        {
            _hasError = value;
            Invalidate();
        }
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

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);

        // DPI scaling resizes this control with layout switched off, so re-centre the
        // text box when the screen it is on is actually shown.
        if (Visible)
        {
            PerformLayout();
        }
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
        graphics.Clear(Parent?.BackColor ?? Theme.Page);
        Drawing.Prepare(graphics);

        var bounds = Drawing.Face(this);
        var radius = LogicalToDeviceUnits(Theme.CornerRadius);
        Drawing.FillRounded(graphics, _textBox.BackColor, bounds, radius);

        var focused = _textBox.Focused;
        var border = _hasError ? Theme.Overdue : focused ? Theme.Navy : Theme.RuleStrong;
        Drawing.StrokeRounded(graphics, border, bounds, radius, LogicalToDeviceUnits(focused || _hasError ? 2 : 1));
    }

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.Handled = true;
            e.SuppressKeyPress = true; // no "ding"
            EnterPressed?.Invoke(this, EventArgs.Empty);
        }
        else if (e.KeyCode == Keys.Escape && EscapePressed is not null)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            EscapePressed.Invoke(this, EventArgs.Empty);
        }
    }
}
