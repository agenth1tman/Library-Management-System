using LibraryManagement.App.Ui;
using LibraryManagement.Services;

namespace LibraryManagement.App.Views;

/// <summary>Common set-up for a full-page screen.</summary>
internal abstract class ViewBase : UserControl
{
    protected ViewBase(IShell shell)
    {
        Shell = shell;
        Dock = DockStyle.Fill;
        AutoScaleMode = AutoScaleMode.Inherit;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = Theme.Body;
        Padding = new Padding(32, 26, 32, 24);
        Visible = false;
    }

    protected IShell Shell { get; }

    protected LibraryStore Store => Shell.Store;

    /// <summary>Called each time the screen is shown; a good moment to focus the first field.</summary>
    public virtual void OnNavigatedTo()
    {
    }

    /// <summary>
    /// Arranges the screen as rows. Rows marked <c>Fill</c> share the leftover height;
    /// the others are as tall as their content.
    /// </summary>
    protected void Compose(params (Control Control, bool Fill)[] rows)
    {
        var root = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, Margin = Padding.Empty };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        foreach (var (control, fill) in rows)
        {
            root.RowStyles.Add(fill ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.AutoSize));
            control.Dock = DockStyle.Fill;
            root.Controls.Add(control);
        }

        Controls.Add(root);
    }
}
