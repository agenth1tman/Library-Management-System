using LibraryManagement.Models;

namespace LibraryManagement.Data;

/// <summary>
/// Where books and members are stored. Implementations translate their own failures into
/// <see cref="LibraryException"/> so callers never need to know about the backing service.
/// </summary>
public interface ILibraryRepository
{
    /// <summary>Short label for the UI, e.g. "Firestore · my-project".</summary>
    string Description { get; }

    /// <summary>False for the built-in sample data, where nothing outlives the session.</summary>
    bool IsPersistent { get; }

    Task<IReadOnlyList<Book>> GetBooksAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Member>> GetMembersAsync(CancellationToken cancellationToken = default);

    /// <exception cref="BookIdTakenException">A book with the same ID already exists.</exception>
    Task AddBookAsync(Book book, CancellationToken cancellationToken = default);

    Task UpdateBookDetailsAsync(string bookId, BookDetails details, CancellationToken cancellationToken = default);

    /// <summary>Records a loan, or clears it when <paramref name="loan"/> is <c>null</c> (book returned).</summary>
    Task SetLoanAsync(string bookId, Loan? loan, CancellationToken cancellationToken = default);

    Task DeleteBookAsync(string bookId, CancellationToken cancellationToken = default);
}
