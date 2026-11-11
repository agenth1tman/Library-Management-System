using LibraryManagement.App.Ui.Controls;

namespace LibraryManagement.App.Ui;

/// <summary>
/// Building blocks for laying screens out in code. Everything is based on
/// <see cref="TableLayoutPanel"/> so the UI stretches with the window and scales with the
/// display's DPI instead of relying on fixed pixel positions.
/// </summary>
internal static class Make
{
    public static Label Text(string text, Font font, Color color, bool wrap = false)
    {
        // GDI+ text rendering draws the embedded title font straight from the app's
        // private font collection; everything else uses the crisper default renderer.
        var useGdiPlus = AppFonts.IsTitleFont(font);

        if (wrap)
        {
            return new WrapLabel { Text = text, Font = font, ForeColor = color, UseCompatibleTextRendering = useGdiPlus };
        }

        return new Label
        {
            Text = text,
            Font = font,
            ForeColor = color,
            AutoSize = true,
            Margin = Padding.Empty,
            UseMnemonic = false,
            UseCompatibleTextRendering = useGdiPlus,
        };
    }

    public static Label Title(string text) => Text(text, Theme.Title, Theme.Ink);

    public static Label Subtitle(string text = "") => Text(text, Theme.Body, Theme.InkMuted);

    public static Label Heading(string text) => Text(text, Theme.Heading, Theme.Ink);

    public static Label Caption(string text) => Text(text, Theme.Small, Theme.InkMuted);

    /// <summary>Stacks controls top to bottom, each as wide as the stack.</summary>
    public static TableLayoutPanel Stack(params Control[] rows)
    {
        var stack = new TableLayoutPanel
        {
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            Margin = Padding.Empty,
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        foreach (var row in rows)
        {
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            if (row is not (Label { AutoSize: true, Dock: DockStyle.None } or FlowLayoutPanel or CheckBox))
            {
                row.Dock = DockStyle.Fill;
            }

            stack.Controls.Add(row);
        }

        return stack;
    }

    /// <summary>Lays controls out left to right at their natural size.</summary>
    public static FlowLayoutPanel Flow(int gap, params Control[] items)
    {
        var flow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = Padding.Empty,
        };

        for (var index = 0; index < items.Length; index++)
        {
            var existing = items[index].Margin;
            items[index].Margin = new Padding(index == 0 ? 0 : gap, existing.Top, 0, existing.Bottom);
            flow.Controls.Add(items[index]);
        }

        return flow;
    }

    /// <summary>Lays controls out left to right, sharing the width by <c>Weight</c>.</summary>
    public static ColumnsPanel Columns(int gap, params (Control Control, float Weight)[] items) => new(gap, items);

    /// <summary>Page title (and optional subtitle) on the left, actions on the right.</summary>
    public static TableLayoutPanel Header(string title, Label? subtitle, params Control[] actions)
    {
        var header = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 18),
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var titleLabel = Title(title);
        if (subtitle is null)
        {
            header.Controls.Add(Stack(titleLabel), 0, 0);
        }
        else
        {
            subtitle.Margin = new Padding(2, 2, 0, 0);
            header.Controls.Add(Stack(titleLabel, subtitle), 0, 0);
        }

        if (actions.Length > 0)
        {
            var buttons = Flow(8, actions);
            buttons.Anchor = AnchorStyles.Right;
            header.Controls.Add(buttons, 1, 0);
        }

        return header;
    }

    /// <summary>
    /// A heading (with an optional action on its right) above some content. With
    /// <paramref name="fill"/> the content takes all remaining height; without it the
    /// section is only as tall as its content.
    /// </summary>
    public static TableLayoutPanel Section(string heading, Control content, Control? action = null, bool fill = false)
    {
        var headingRow = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8),
        };
        headingRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headingRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headingRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var headingLabel = Heading(heading);
        headingLabel.Anchor = AnchorStyles.Left;
        headingRow.Controls.Add(headingLabel, 0, 0);

        if (action is not null)
        {
            action.Anchor = AnchorStyles.Right;
            headingRow.Controls.Add(action, 1, 0);
        }

        var section = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = fill ? DockStyle.Fill : DockStyle.Top,
            AutoSize = !fill,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = Padding.Empty,
        };
        section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        section.RowStyles.Add(fill ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.AutoSize));

        content.Dock = DockStyle.Fill;
        section.Controls.Add(headingRow);
        section.Controls.Add(content);
        return section;
    }

    /// <summary>A table with a one-line message shown in its place when it has no rows.</summary>
    public static Panel GridWithEmptyState(BookGrid grid, Label emptyState)
    {
        emptyState.Margin = Padding.Empty;
        emptyState.Location = new Point(1, 6);
        emptyState.Visible = false;

        var host = new Panel { Margin = Padding.Empty };
        host.Controls.Add(grid);
        host.Controls.Add(emptyState);
        return host;
    }
}
