using LibraryManagement.Models;

namespace LibraryManagement.Data;

/// <summary>
/// A small in-memory library used to explore the app without a Firebase project
/// (and by the tests). Changes last until the app closes.
/// </summary>
public sealed class SampleLibraryRepository : ILibraryRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Book> _books;
    private readonly List<Member> _members;

    public SampleLibraryRepository(IEnumerable<Book> books, IEnumerable<Member> members)
    {
        _books = books.ToDictionary(book => book.Id);
        _members = members.ToList();
    }

    public string Description => "Sample data";

    public bool IsPersistent => false;

    /// <summary>A believable school library with loans in every state relative to <paramref name="today"/>.</summary>
    public static SampleLibraryRepository CreateDemo(DateOnly today)
    {
        var members = new List<Member>
        {
            new("BTS-10-214", "Ayesha Khan", MemberRole.Student, "10-B"),
            new("BTS-09-131", "Hamza Tariq", MemberRole.Student, "9-A"),
            new("BTS-11-087", "Zainab Raza", MemberRole.Student, "11-C"),
            new("BTS-08-302", "Bilal Ahmed", MemberRole.Student, "8-A"),
            new("BTS-12-045", "Fatima Malik", MemberRole.Student, "12-B"),
            new("BTS-T1-007", "Sana Iqbal", MemberRole.Teacher, "Physics"),
            new("BTS-T1-019", "Omar Farooq", MemberRole.Teacher, "English"),
        };

        var nextId = 0;
        var books = new List<Book>();

        void Add(string title, string author, string barcode, int registeredDaysAgo, Loan? loan = null) =>
            books.Add(new Book
            {
                Id = (nextId++).ToString(),
                Title = title,
                Author = author,
                Barcode = barcode,
                CatalogId = $"BTS-{1000 + (nextId * 37 % 9000):D4}-{10000 + (nextId * 7919 % 90000):D5}",
                RegisteredOn = today.AddDays(-registeredDaysAgo),
                Loan = loan,
            });

        Loan LoanTo(Member member, int issuedDaysAgo, int dueInDays) => new()
        {
            MemberId = member.Id,
            MemberName = member.Name,
            Role = member.Role,
            Section = member.Section,
            IssuedOn = today.AddDays(-issuedDaysAgo),
            DueOn = today.AddDays(dueInDays),
        };

        Add("To Kill a Mockingbird", "Harper Lee", "9780061120084", 410);
        Add("A Brief History of Time", "Stephen Hawking", "9780553380163", 398, LoanTo(members[5], 60, 120));
        Add("Pride and Prejudice", "Jane Austen", "9780141439518", 380, LoanTo(members[0], 38, -8));
        Add("The Kite Runner", "Khaled Hosseini", "9781594631931", 371);
        Add("Sapiens", "Yuval Noah Harari", "9780062316097", 350, LoanTo(members[2], 29, 1));
        Add("The Alchemist", "Paulo Coelho", "9780062315007", 322, LoanTo(members[1], 12, 18));
        Add("Animal Farm", "George Orwell", "9780451526342", 300);
        Add("The Reluctant Fundamentalist", "Mohsin Hamid", "9780156034029", 284, LoanTo(members[4], 33, -3));
        Add("Thinking, Fast and Slow", "Daniel Kahneman", "9780374533557", 251);
        Add("Things Fall Apart", "Chinua Achebe", "9780385474542", 230, LoanTo(members[6], 45, 200));
        Add("The Hobbit", "J.R.R. Tolkien", "9780547928227", 190, LoanTo(members[3], 28, 2));
        Add("Cosmos", "Carl Sagan", "9780345539434", 150);
        Add("Moth Smoke", "Mohsin Hamid", "9781594486609", 96);
        Add("Brave New World", "Aldous Huxley", "9780060850524", 41);
        Add("Animal Farm", "George Orwell", "9780451526342", 12);

        return new SampleLibraryRepository(books, members);
    }

    public Task<IReadOnlyList<Book>> GetBooksAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<Book>>(_books.Values.ToList());
        }
    }

    public Task<IReadOnlyList<Member>> GetMembersAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<Member>>(_members.ToList());
        }
    }

    public Task AddBookAsync(Book book, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_books.TryAdd(book.Id, book))
            {
                throw new BookIdTakenException(book.Id);
            }
        }

        return Task.CompletedTask;
    }

    public Task UpdateBookDetailsAsync(
        string bookId,
        BookDetails details,
        CancellationToken cancellationToken = default) =>
        Update(bookId, book => book with { Title = details.Title, Author = details.Author, Barcode = details.Barcode });

    public Task SetLoanAsync(string bookId, Loan? loan, CancellationToken cancellationToken = default) =>
        Update(bookId, book => book with { Loan = loan });

    public Task DeleteBookAsync(string bookId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _books.Remove(bookId);
        }

        return Task.CompletedTask;
    }

    private Task Update(string bookId, Func<Book, Book> change)
    {
        lock (_gate)
        {
            if (!_books.TryGetValue(bookId, out var book))
            {
                throw new LibraryException("That record no longer exists in the database. Refresh and try again.");
            }

            _books[bookId] = change(book);
        }

        return Task.CompletedTask;
    }
}
