namespace LibraryManagement.App.Ui.Controls;

/// <summary>A rounded surface that groups related content.</summary>
internal class Card : Panel
{
    public Card()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);

        BackColor = Theme.Surface;
        ForeColor = Theme.Text;
        Padding = new Padding(20);
        Margin = Padding.Empty;
    }

    /// <summary>
    /// When the card sizes itself to its content, pass the available width down so wrapped
    /// text inside is measured at the width it will really get.
    /// </summary>
    public override Size GetPreferredSize(Size proposedSize)
    {
        if (!AutoSize || Controls.Count != 1)
        {
            return base.GetPreferredSize(proposedSize);
        }

        var widthIsKnown = proposedSize.Width > 1 && proposedSize.Width < short.MaxValue;
        var width = Math.Max(1, (widthIsKnown ? proposedSize.Width : Width) - Padding.Horizontal);
        var content = Controls[0].GetPreferredSize(new Size(width, 0));
        return new Size(content.Width + Padding.Horizontal, content.Height + Padding.Vertical);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // The corners show whatever is behind the card.
        e.Graphics.Clear(Parent?.BackColor ?? Theme.Background);
        Drawing.Prepare(e.Graphics);
        Drawing.FillRounded(e.Graphics, BackColor, Drawing.Face(this), LogicalToDeviceUnits(Theme.CornerRadius + 2));
    }
}
