namespace LibraryManagement;

/// <summary>
/// An expected failure whose <see cref="Exception.Message"/> is written for the librarian
/// and is safe to show in the UI as-is (validation errors, "book not found", "no connection").
/// </summary>
public class LibraryException : Exception
{
    public LibraryException(string message)
        : base(message)
    {
    }

    public LibraryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Thrown by a repository when a new book's ID is already used by another book.</summary>
public sealed class BookIdTakenException : LibraryException
{
    public BookIdTakenException(string bookId)
        : base(MessageFor(bookId))
    {
        BookId = bookId;
    }

    public BookIdTakenException(string bookId, Exception innerException)
        : base(MessageFor(bookId), innerException)
    {
        BookId = bookId;
    }

    public string BookId { get; }

    private static string MessageFor(string bookId) => $"Book ID {bookId} is already in use.";
}
