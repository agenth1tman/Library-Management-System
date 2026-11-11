namespace LibraryManagement.Models;

public static class BookSearch
{
    /// <summary>
    /// True when every word of <paramref name="query"/> appears somewhere in the book's
    /// searchable text (title, author, IDs, barcode, borrower). Case-insensitive.
    /// </summary>
    public static bool Matches(Book book, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return words.All(word => SearchableFields(book).Any(
            field => field.Contains(word, StringComparison.OrdinalIgnoreCase)));
    }

    private static IEnumerable<string> SearchableFields(Book book)
    {
        yield return book.Id;
        yield return book.Title;
        yield return book.Author;
        yield return book.Barcode;
        yield return book.CatalogId;

        if (book.Loan is { } loan)
        {
            yield return loan.MemberName;
            yield return loan.MemberId;
        }
    }
}
