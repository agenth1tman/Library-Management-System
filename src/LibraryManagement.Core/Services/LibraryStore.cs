using System.Globalization;
using LibraryManagement.Data;
using LibraryManagement.Models;

namespace LibraryManagement.Services;

/// <summary>
/// The app's single source of truth: a cached copy of the library plus every operation a
/// librarian can perform. Screens read from it and listen to <see cref="Changed"/>.
/// <para>
/// Not thread-safe by design: call it from the UI thread. Awaits resume on the caller's
/// context, so <see cref="Changed"/> is always raised on that same thread.
/// </para>
/// </summary>
public sealed class LibraryStore
{
    private const int MaxIdAttempts = 3;

    private readonly Func<DateOnly> _today;
    private ILibraryRepository? _repository;

    public LibraryStore(Func<DateOnly>? today = null)
    {
        _today = today ?? (() => DateOnly.FromDateTime(DateTime.Today));
    }

    /// <summary>Raised after the cached books or members were replaced.</summary>
    public event EventHandler? Changed;

    public IReadOnlyList<Book> Books { get; private set; } = [];

    public IReadOnlyList<Member> Members { get; private set; } = [];

    public LibraryStats Stats { get; private set; } = LibraryStats.Empty;

    public DateOnly Today => _today();

    public bool IsConnected => _repository is not null;

    /// <summary>False while showing sample data that is not saved anywhere.</summary>
    public bool IsPersistent => _repository?.IsPersistent ?? false;

    public string SourceDescription => _repository?.Description ?? "Not connected";

    /// <summary>
    /// Switches to <paramref name="repository"/> after loading from it once. If loading
    /// fails the previous data source stays in place.
    /// </summary>
    public async Task ConnectAsync(ILibraryRepository repository, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);

        var (books, members) = await LoadAsync(repository, cancellationToken);
        _repository = repository;
        Publish(books, members);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var (books, members) = await LoadAsync(Repository, cancellationToken);
        Publish(books, members);
    }

    public DailyReport CreateDailyReport() => DailyReport.Create(Books, Today);

    public Book? GetBook(string bookId) => Books.FirstOrDefault(book => book.Id == bookId);

    /// <summary>
    /// Finds a book from whatever was typed or scanned: its ID first, then its catalogue
    /// label, then the publisher's barcode (first match, since copies share a barcode).
    /// </summary>
    public Book? FindBook(string? query)
    {
        var text = query?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        return Books.FirstOrDefault(book => book.Id.Equals(text, StringComparison.OrdinalIgnoreCase))
            ?? Books.FirstOrDefault(book => book.CatalogId.Equals(text, StringComparison.OrdinalIgnoreCase))
            ?? Books.FirstOrDefault(book => book.Barcode.Equals(text, StringComparison.OrdinalIgnoreCase));
    }

    public Member? FindMember(string? memberId) =>
        Members.FirstOrDefault(member => MemberId.Matches(member.Id, memberId));

    public async Task<Book> RegisterBookAsync(
        string title,
        string author,
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var details = Validate(new BookDetails(title, author, barcode));

        for (var attempt = 1; ; attempt++)
        {
            var book = new Book
            {
                Id = NextBookId(),
                Title = details.Title,
                Author = details.Author,
                Barcode = details.Barcode,
                CatalogId = NewCatalogId(),
                RegisteredOn = Today,
            };

            try
            {
                await Repository.AddBookAsync(book, cancellationToken);
                await RefreshAsync(cancellationToken);
                return book;
            }
            catch (BookIdTakenException) when (attempt < MaxIdAttempts)
            {
                // Another computer registered a book since our last refresh. Catch up and retry.
                await RefreshAsync(cancellationToken);
            }
        }
    }

    public async Task UpdateBookAsync(string bookId, BookDetails details, CancellationToken cancellationToken = default)
    {
        var book = RequireBook(bookId);
        await Repository.UpdateBookDetailsAsync(book.Id, Validate(details), cancellationToken);
        await RefreshAsync(cancellationToken);
    }

    public async Task<Loan> IssueBookAsync(string bookId, string memberId, CancellationToken cancellationToken = default)
    {
        var book = RequireBook(bookId);
        if (!book.IsAvailable)
        {
            throw new LibraryException($"“{book.Title}” is already on loan to {book.Loan!.MemberName}.");
        }

        var member = FindMember(memberId)
            ?? throw new LibraryException($"No member has the ID “{memberId.Trim()}”.");

        var loan = new Loan
        {
            MemberId = member.Id,
            MemberName = member.Name,
            Role = member.Role,
            Section = member.Section,
            IssuedOn = Today,
            DueOn = LoanPolicy.DueDateFor(member.Role, Today),
        };

        await Repository.SetLoanAsync(book.Id, loan, cancellationToken);
        await RefreshAsync(cancellationToken);
        return loan;
    }

    public async Task ReturnBookAsync(string bookId, CancellationToken cancellationToken = default)
    {
        var book = RequireBook(bookId);
        if (book.IsAvailable)
        {
            throw new LibraryException($"“{book.Title}” is not on loan.");
        }

        await Repository.SetLoanAsync(book.Id, loan: null, cancellationToken);
        await RefreshAsync(cancellationToken);
    }

    public async Task DeleteBookAsync(string bookId, CancellationToken cancellationToken = default)
    {
        var book = RequireBook(bookId);
        await Repository.DeleteBookAsync(book.Id, cancellationToken);
        await RefreshAsync(cancellationToken);
    }

    private ILibraryRepository Repository =>
        _repository ?? throw new LibraryException("The app is not connected to a library yet.");

    private static async Task<(IReadOnlyList<Book> Books, IReadOnlyList<Member> Members)> LoadAsync(
        ILibraryRepository repository,
        CancellationToken cancellationToken)
    {
        var booksTask = repository.GetBooksAsync(cancellationToken);
        var membersTask = repository.GetMembersAsync(cancellationToken);
        await Task.WhenAll(booksTask, membersTask);

        // Firestore returns document IDs in text order ("10" before "2"); sort them as numbers.
        var books = (await booksTask)
            .OrderBy(book => book.NumericId ?? int.MaxValue)
            .ThenBy(book => book.Id, StringComparer.Ordinal)
            .ToList();

        var members = (await membersTask)
            .OrderBy(member => member.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return (books, members);
    }

    private void Publish(IReadOnlyList<Book> books, IReadOnlyList<Member> members)
    {
        Books = books;
        Members = members;
        Stats = LibraryStats.From(books, Today);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private Book RequireBook(string bookId) =>
        GetBook(bookId) ?? throw new LibraryException($"Book {bookId} is no longer in the library. Refresh and try again.");

    private static BookDetails Validate(BookDetails details)
    {
        var clean = new BookDetails(details.Title.Trim(), details.Author.Trim(), details.Barcode.Trim());

        if (clean.Title.Length == 0)
        {
            throw new LibraryException("Enter the book's title.");
        }

        if (clean.Author.Length == 0)
        {
            throw new LibraryException("Enter the author's name.");
        }

        if (clean.Barcode.Length == 0)
        {
            throw new LibraryException("Enter or scan the book's barcode.");
        }

        return clean;
    }

    /// <summary>One more than the highest number in use, so the ID of a deleted book is never recycled.</summary>
    private string NextBookId()
    {
        var highest = Books.Max(book => book.NumericId) ?? -1;
        return (highest + 1).ToString(CultureInfo.InvariantCulture);
    }

    private string NewCatalogId()
    {
        var taken = Books.Select(book => book.CatalogId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            var candidate = $"BTS-{Random.Shared.Next(10_000):D4}-{Random.Shared.Next(100_000):D5}";
            if (!taken.Contains(candidate))
            {
                return candidate;
            }
        }
    }
}
