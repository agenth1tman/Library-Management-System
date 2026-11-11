using System.ComponentModel;

namespace LibraryManagement.App.Ui.Controls;

/// <summary>
/// A caption, a text input and, when something is wrong, the reason right under the input.
/// Validation messages belong next to the field they are about, not in a pop-up.
/// </summary>
internal sealed class FormField : TableLayoutPanel
{
    private readonly Label _error;

    public FormField(string caption, InputBox input)
    {
        Input = input;
        Input.Caption = caption;
        Input.Dock = DockStyle.Fill;

        var captionLabel = Make.Caption(caption);
        captionLabel.Margin = new Padding(1, 0, 0, 4);

        _error = Make.Text(string.Empty, Theme.Small, Theme.Overdue, wrap: true);
        _error.Margin = new Padding(1, 4, 0, 0);
        _error.Visible = false;

        ColumnCount = 1;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Dock = DockStyle.Fill;
        Margin = new Padding(0, 0, 0, 14);
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        foreach (Control row in new Control[] { captionLabel, Input, _error })
        {
            RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(row);
        }

        // Typing in a field is the user's answer to its error.
        Input.TextChanged += (_, _) => Error = null;
    }

    public InputBox Input { get; }

    /// <summary>The problem with this field's value, or <c>null</c> when there is none.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? Error
    {
        get => _error.Visible ? _error.Text : null;
        set
        {
            var hasError = !string.IsNullOrEmpty(value);
            if (!hasError && !_error.Visible)
            {
                return;
            }

            _error.Text = value ?? string.Empty;
            _error.Visible = hasError;
            Input.HasError = hasError;
        }
    }

    /// <summary>Sets <see cref="Error"/> when the field is blank. Returns true when it has a value.</summary>
    public bool Require(string messageWhenEmpty)
    {
        var filled = !string.IsNullOrWhiteSpace(Input.Text);
        Error = filled ? null : messageWhenEmpty;
        return filled;
    }
}
