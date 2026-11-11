using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Services;

namespace LibraryManagement.Tests;

public class LibraryStoreTests
{
    private static readonly DateOnly Today = TestData.Today;

    private static async Task<LibraryStore> ConnectedStoreAsync(params Book[] books)
    {
        var store = new LibraryStore(() => Today);
        await store.ConnectAsync(new SampleLibraryRepository(books, [TestData.Student, TestData.Teacher]));
        return store;
    }

    [Fact]
    public async Task Books_are_ordered_by_number_not_by_text()
    {
        // Firestore hands documents back as "0", "1", "10", "2". The old code used the
        // list position as the book ID, so with more than ten books it opened the wrong one.
        var store = await ConnectedStoreAsync(
            TestData.Book("10"), TestData.Book("2"), TestData.Book("0"), TestData.Book("1"));

        Assert.Equal(["0", "1", "2", "10"], store.Books.Select(book => book.Id));
    }

    [Fact]
    public async Task Find_book_uses_the_id_not_the_position_in_the_list()
    {
        var store = await ConnectedStoreAsync(TestData.Book("0", "Zero"), TestData.Book("5", "Five"));

        Assert.Equal("Five", store.FindBook("5")?.Title);
        Assert.Null(store.FindBook("1"));
    }

    [Fact]
    public async Task Find_book_also_accepts_a_catalogue_id_or_barcode()
    {
        var book = TestData.Book("4", "Four");
        var store = await ConnectedStoreAsync(TestData.Book("0"), book);

        Assert.Equal(book, store.FindBook(book.CatalogId.ToLowerInvariant()));
        Assert.Equal(book, store.FindBook($" {book.Barcode} "));
        Assert.Null(store.FindBook(""));
    }

    [Fact]
    public async Task Registering_uses_the_next_free_number_and_never_reuses_a_gap()
    {
        var store = await ConnectedStoreAsync(TestData.Book("0"), TestData.Book("1"), TestData.Book("7"));

        Assert.Equal("8", store.NextBookId);

        var book = await store.RegisterBookAsync("  Dune ", " Frank Herbert", "9780441013593 ");

        Assert.Equal("8", book.Id);
        Assert.Equal("9", store.NextBookId);
        Assert.Equal("Dune", book.Title);
        Assert.Equal("Frank Herbert", book.Author);
        Assert.Equal(Today, book.RegisteredOn);
        Assert.Matches(@"^BTS-\d{4}-\d{5}$", book.CatalogId);
        Assert.Equal(book, store.GetBook("8"));
    }

    [Fact]
    public async Task The_first_book_in_an_empty_library_gets_id_zero()
    {
        var store = await ConnectedStoreAsync();

        Assert.Equal("0", store.NextBookId);
        Assert.Equal("0", (await store.RegisterBookAsync("Dune", "Frank Herbert", "1")).Id);
    }

    [Fact]
    public async Task Registering_retries_when_another_computer_took_the_id()
    {
        var repository = new SampleLibraryRepository([TestData.Book("0")], []);
        var store = new LibraryStore(() => Today);
        await store.ConnectAsync(repository);

        // Simulates a second librarian adding book 1 after this store last refreshed.
        await repository.AddBookAsync(TestData.Book("1"));

        var book = await store.RegisterBookAsync("Dune", "Frank Herbert", "1");

        Assert.Equal("2", book.Id);
        Assert.Equal(3, store.Books.Count);
    }

    [Theory]
    [InlineData("", "Author", "123", "title")]
    [InlineData("Title", "  ", "123", "author")]
    [InlineData("Title", "Author", "", "barcode")]
    public async Task Registering_requires_every_field(string title, string author, string barcode, string expectedWord)
    {
        var store = await ConnectedStoreAsync();

        var error = await Assert.ThrowsAsync<LibraryException>(() => store.RegisterBookAsync(title, author, barcode));

        Assert.Contains(expectedWord, error.Message);
        Assert.Empty(store.Books);
    }

    [Fact]
    public async Task Issuing_to_a_student_sets_the_loan_and_a_one_month_due_date()
    {
        var store = await ConnectedStoreAsync(TestData.Book("0"));

        await store.IssueBookAsync("0", "bts10214");

        var loan = store.GetBook("0")!.Loan;
        Assert.NotNull(loan);
        Assert.Equal(TestData.Student.Id, loan.MemberId);
        Assert.Equal(TestData.Student.Name, loan.MemberName);
        Assert.Equal(Today, loan.IssuedOn);
        Assert.Equal(Today.AddMonths(1), loan.DueOn);
        Assert.Equal(1, store.Stats.OnLoan);
    }

    [Fact]
    public async Task Issuing_to_an_unknown_member_is_refused()
    {
        var store = await ConnectedStoreAsync(TestData.Book("0"));

        await Assert.ThrowsAsync<LibraryException>(() => store.IssueBookAsync("0", "BTS-99-999"));

        Assert.True(store.GetBook("0")!.IsAvailable);
    }

    [Fact]
    public async Task A_book_cannot_be_issued_twice()
    {
        var store = await ConnectedStoreAsync(TestData.Book("0"));
        await store.IssueBookAsync("0", TestData.Student.Id);

        var error = await Assert.ThrowsAsync<LibraryException>(() => store.IssueBookAsync("0", TestData.Teacher.Id));

        Assert.Contains("already on loan", error.Message);
        Assert.Equal(TestData.Student.Id, store.GetBook("0")!.Loan!.MemberId);
    }

    [Fact]
    public async Task Returning_clears_the_loan()
    {
        var store = await ConnectedStoreAsync(TestData.Book("0", loan: TestData.LoanDue(Today)));

        await store.ReturnBookAsync("0");

        Assert.True(store.GetBook("0")!.IsAvailable);
        await Assert.ThrowsAsync<LibraryException>(() => store.ReturnBookAsync("0"));
    }

    [Fact]
    public async Task Editing_keeps_the_loan_untouched()
    {
        var loan = TestData.LoanDue(Today);
        var store = await ConnectedStoreAsync(TestData.Book("0", loan: loan));

        await store.UpdateBookAsync("0", new BookDetails(" New Title ", "New Author", "999"));

        var book = store.GetBook("0")!;
        Assert.Equal("New Title", book.Title);
        Assert.Equal("New Author", book.Author);
        Assert.Equal("999", book.Barcode);
        Assert.Equal(loan, book.Loan);
    }

    [Fact]
    public async Task Deleting_removes_only_that_book()
    {
        var store = await ConnectedStoreAsync(TestData.Book("0"), TestData.Book("1"), TestData.Book("2"));

        await store.DeleteBookAsync("1");

        Assert.Equal(["0", "2"], store.Books.Select(book => book.Id));
        await Assert.ThrowsAsync<LibraryException>(() => store.DeleteBookAsync("1"));
    }

    [Fact]
    public async Task Changed_is_raised_once_per_refresh_and_stats_do_not_accumulate()
    {
        // The old code never reset its counters, so every refresh inflated the totals.
        var store = await ConnectedStoreAsync(TestData.Book("0"), TestData.Book("1", loan: TestData.LoanDue(Today.AddDays(-2))));
        var raised = 0;
        store.Changed += (_, _) => raised++;

        await store.RefreshAsync();
        await store.RefreshAsync();

        Assert.Equal(2, raised);
        Assert.Equal(2, store.Stats.Total);
        Assert.Equal(1, store.Stats.Overdue);
        Assert.Single(store.CreateDailyReport().Overdue);
    }

    [Fact]
    public async Task Operations_before_connecting_fail_with_a_clear_message()
    {
        var store = new LibraryStore(() => Today);

        Assert.False(store.IsConnected);
        await Assert.ThrowsAsync<LibraryException>(() => store.RefreshAsync());
    }

    [Fact]
    public async Task A_failed_connection_keeps_the_previous_data_source()
    {
        var store = await ConnectedStoreAsync(TestData.Book("0"));

        await Assert.ThrowsAsync<LibraryException>(() => store.ConnectAsync(new FailingRepository()));

        Assert.Equal("Sample data", store.SourceDescription);
        Assert.Single(store.Books);
    }

    [Fact]
    public async Task The_demo_library_has_loans_in_every_state()
    {
        var store = new LibraryStore(() => Today);
        await store.ConnectAsync(SampleLibraryRepository.CreateDemo(Today));

        Assert.True(store.Stats.Available > 0);
        Assert.True(store.Stats.Overdue > 0);
        Assert.True(store.Stats.DueSoon > 0);
        Assert.True(store.Stats.OnLoanToTeachers > 0);
        Assert.Equal(store.Books.Count, store.Books.Select(book => book.CatalogId).Distinct().Count());
        Assert.All(store.Books.Where(book => !book.IsAvailable), book => Assert.NotNull(store.FindMember(book.Loan!.MemberId)));
    }

    private sealed class FailingRepository : ILibraryRepository
    {
        public string Description => "Broken";

        public bool IsPersistent => true;

        public Task<IReadOnlyList<Book>> GetBooksAsync(CancellationToken cancellationToken = default) =>
            throw new LibraryException("offline");

        public Task<IReadOnlyList<Member>> GetMembersAsync(CancellationToken cancellationToken = default) =>
            throw new LibraryException("offline");

        public Task AddBookAsync(Book book, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateBookDetailsAsync(string bookId, BookDetails details, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SetLoanAsync(string bookId, Loan? loan, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteBookAsync(string bookId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
