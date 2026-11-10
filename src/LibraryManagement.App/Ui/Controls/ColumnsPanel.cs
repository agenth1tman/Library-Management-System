namespace LibraryManagement.App.Ui.Controls;

/// <summary>
/// Lays its children out side by side, sharing the width by weight, all the same height.
/// <para>
/// With <see cref="Control.AutoSize"/> on, the panel is as tall as its tallest child
/// measured at the width that child will actually get, so wrapped text never throws the
/// height off. (A TableLayoutPanel with percentage columns cannot promise that.)
/// </para>
/// </summary>
internal sealed class ColumnsPanel : Panel
{
    private readonly (Control Control, float Weight)[] _items;
    private readonly int _gap;

    public ColumnsPanel(int gap, params (Control Control, float Weight)[] items)
    {
        _items = items;
        _gap = gap;
        Margin = Padding.Empty;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        foreach (var (control, _) in items)
        {
            control.Dock = DockStyle.None;
            control.Margin = Padding.Empty;
            Controls.Add(control);
        }
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var widthIsKnown = proposedSize.Width > 1 && proposedSize.Width < short.MaxValue;
        var width = widthIsKnown ? proposedSize.Width : Width;

        var height = 0;
        foreach (var (control, _, columnWidth) in Arrange(width))
        {
            var wanted = control.AutoSize ? control.GetPreferredSize(new Size(columnWidth, 0)).Height : control.Height;
            height = Math.Max(height, wanted);
        }

        return new Size(width, height);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);

        foreach (var (control, left, columnWidth) in Arrange(ClientSize.Width))
        {
            control.SetBounds(left, 0, columnWidth, ClientSize.Height);
        }
    }

    private IEnumerable<(Control Control, int Left, int Width)> Arrange(int totalWidth)
    {
        var gap = LogicalToDeviceUnits(_gap);
        var usable = Math.Max(0, totalWidth - (gap * (_items.Length - 1)));
        var totalWeight = _items.Sum(item => item.Weight);

        var left = 0f;
        foreach (var (control, weight) in _items)
        {
            var width = usable * weight / totalWeight;
            yield return (control, (int)Math.Round(left), (int)Math.Round(left + width) - (int)Math.Round(left));
            left += width + gap;
        }
    }
}
