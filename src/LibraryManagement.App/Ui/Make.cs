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
        if (wrap)
        {
            return new WrapLabel
            {
                Text = text,
                Font = font,
                ForeColor = color,
                UseCompatibleTextRendering = AppFonts.IsDisplayFont(font),
            };
        }

        return new Label
        {
            Text = text,
            Font = font,
            ForeColor = color,
            AutoSize = true,
            Margin = Padding.Empty,
            UseMnemonic = false,

            // GDI+ text rendering draws the embedded display font straight from the app's
            // private font collection; everything else uses the crisper default renderer.
            UseCompatibleTextRendering = AppFonts.IsDisplayFont(font),
        };
    }

    public static Label Title(string text) => Text(text, Theme.Title, Theme.Text);

    public static Label Subtitle(string text = "") => Text(text, Theme.Body, Theme.TextMuted);

    public static Label Heading(string text) => Text(text, Theme.Heading, Theme.Text);

    public static Label Caption(string text) => Text(text, Theme.Small, Theme.TextMuted);

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
            if (row is not Label { AutoSize: true, Dock: DockStyle.None })
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
            items[index].Margin = new Padding(index == 0 ? 0 : gap, 0, 0, 0);
            flow.Controls.Add(items[index]);
        }

        return flow;
    }

    /// <summary>Lays controls out left to right in equally wide columns.</summary>
    public static ColumnsPanel EqualColumns(int gap, params Control[] items) =>
        new(gap, items.Select(item => (item, 1f)).ToArray());

    /// <summary>Lays controls out left to right, sharing the width by <c>Weight</c>.</summary>
    public static ColumnsPanel Columns(int gap, params (Control Control, float Weight)[] items) => new(gap, items);

    /// <summary>A small caption above an input.</summary>
    public static TableLayoutPanel Field(string caption, Control input)
    {
        var label = Caption(caption);
        label.Margin = new Padding(1, 0, 0, 6);

        var field = Stack(label, input);
        field.Margin = new Padding(0, 0, 0, Theme.Gap);
        return field;
    }

    /// <summary>Page title and subtitle on the left, action buttons on the right.</summary>
    public static TableLayoutPanel Header(string title, Label subtitle, params Control[] actions)
    {
        subtitle.Margin = new Padding(2, 4, 0, 0);

        var header = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 22),
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        header.Controls.Add(Stack(Title(title), subtitle), 0, 0);

        if (actions.Length > 0)
        {
            var buttons = Flow(10, actions);
            buttons.Anchor = AnchorStyles.Right;
            header.Controls.Add(buttons, 1, 0);
        }

        return header;
    }

    /// <summary>
    /// A card with a heading above one control. With <paramref name="fill"/> the content
    /// stretches to the card's height; without it the card is only as tall as its content.
    /// </summary>
    public static Card CardWith(string heading, string? caption, Control content, bool fill = true)
    {
        var table = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = fill ? DockStyle.Fill : DockStyle.Top,
            AutoSize = !fill,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = Padding.Empty,
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var headingLabel = Heading(heading);
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(headingLabel);

        if (caption is not null)
        {
            var captionLabel = Caption(caption);
            captionLabel.Margin = new Padding(1, 4, 0, 0);
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(captionLabel);
        }

        content.Dock = DockStyle.Fill;
        content.Margin = new Padding(0, 14, 0, 0);
        table.RowStyles.Add(fill ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.AutoSize));
        table.Controls.Add(content);

        var card = new Card { AutoSize = !fill, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        card.Controls.Add(table);
        return card;
    }
}
