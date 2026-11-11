namespace LibraryManagement.App.Ui.Controls;

/// <summary>A one-pixel horizontal rule, like the ruled lines of a ledger.</summary>
internal sealed class Divider : Control
{
    public Divider(bool strong = false)
    {
        SetStyle(ControlStyles.Selectable, false);
        Height = 1;
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;
        BackColor = strong ? Theme.RuleStrong : Theme.Rule;
    }

    protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
    {
        // A hairline stays a hairline at every DPI.
        base.ScaleControl(factor, specified);
        Height = 1;
    }
}
