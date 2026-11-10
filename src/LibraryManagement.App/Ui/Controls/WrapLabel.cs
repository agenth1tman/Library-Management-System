namespace LibraryManagement.App.Ui.Controls;

/// <summary>
/// A label that wraps to the width it is given and is exactly as tall as its text needs.
/// <para>
/// A plain auto-sized <see cref="Label"/> only wraps when its parent tells it how wide it
/// may be. Some containers ask "how big do you want to be?" without saying, and get the
/// height of one long line back, which clips the text. This label answers that question
/// using its current width instead.
/// </para>
/// </summary>
internal sealed class WrapLabel : Label
{
    public WrapLabel()
    {
        AutoSize = true;
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;
        UseMnemonic = false;
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var widthIsKnown = proposedSize.Width > 1 && proposedSize.Width < short.MaxValue;
        var width = widthIsKnown ? proposedSize.Width : Width;

        return width > 1
            ? base.GetPreferredSize(new Size(width, 0))
            : base.GetPreferredSize(proposedSize);
    }
}
