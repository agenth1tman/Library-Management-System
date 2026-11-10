namespace LibraryManagement.App.Ui;

/// <summary>Images compiled into the executable.</summary>
internal static class EmbeddedAssets
{
    private static readonly Lazy<Image?> LazyCrest = new(() => Load("Assets.crest.png", stream => Image.FromStream(stream)));

    public static Image? Crest => LazyCrest.Value;

    public static Icon? LoadAppIcon() => Load("Assets.app.ico", stream => new Icon(stream));

    private static T? Load<T>(string resourceName, Func<Stream, T> read)
        where T : class
    {
        // The stream is copied because GDI+ images keep reading from their source stream.
        using var resource = typeof(EmbeddedAssets).Assembly.GetManifestResourceStream(resourceName);
        if (resource is null)
        {
            return null;
        }

        var copy = new MemoryStream();
        resource.CopyTo(copy);
        copy.Position = 0;
        return read(copy);
    }
}
