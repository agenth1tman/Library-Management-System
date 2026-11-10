using LibraryManagement.Models;

namespace LibraryManagement.Tests;

public class BookTests
{
    private static readonly DateOnly Today = TestData.Today;

    [Fact]
    public void A_book_without_a_loan_is_available()
    {
        var book = TestData.Book("1");

        Assert.True(book.IsAvailable);
        Assert.Equal(BookStatus.Available, book.StatusOn(Today));
    }

    [Theory]
    [InlineData(10, BookStatus.OnLoan)]
    [InlineData(4, BookStatus.OnLoan)]
    [InlineData(3, BookStatus.DueSoon)]
    [InlineData(1, BookStatus.DueSoon)]
    [InlineData(0, BookStatus.DueSoon)] // due today: the old code reported this as neither due soon nor overdue
    [InlineData(-1, BookStatus.Overdue)]
    [InlineData(-30, BookStatus.Overdue)]
    public void Status_depends_on_days_until_due(int dueInDays, BookStatus expected)
    {
        var book = TestData.Book("1", loan: TestData.LoanDue(Today.AddDays(dueInDays)));

        Assert.Equal(expected, book.StatusOn(Today));
    }

    [Fact]
    public void A_loan_without_a_due_date_is_simply_on_loan()
    {
        var loan = TestData.LoanDue(Today) with { DueOn = null };

        Assert.Equal(BookStatus.OnLoan, TestData.Book("1", loan: loan).StatusOn(Today));
    }

    [Fact]
    public void Days_overdue_counts_whole_days_past_the_due_date()
    {
        Assert.Equal(5, TestData.Book("1", loan: TestData.LoanDue(Today.AddDays(-5))).DaysOverdueOn(Today));
        Assert.Equal(0, TestData.Book("1", loan: TestData.LoanDue(Today.AddDays(2))).DaysOverdueOn(Today));
        Assert.Equal(0, TestData.Book("1").DaysOverdueOn(Today));
    }

    [Theory]
    [InlineData("12", 12)]
    [InlineData("0", 0)]
    [InlineData("abc", null)]
    [InlineData("-3", null)]
    public void Numeric_id_is_only_set_for_plain_numbers(string id, int? expected)
    {
        Assert.Equal(expected, TestData.Book(id).NumericId);
    }

    [Fact]
    public void Stats_count_each_group_once()
    {
        Book[] books =
        [
            TestData.Book("0"),
            TestData.Book("1"),
            TestData.Book("2", loan: TestData.LoanDue(Today.AddDays(20))),
            TestData.Book("3", loan: TestData.LoanDue(Today.AddDays(2))),
            TestData.Book("4", loan: TestData.LoanDue(Today.AddDays(-4))),
            TestData.Book("5", loan: TestData.LoanDue(Today.AddDays(90), TestData.Teacher)),
        ];

        var stats = LibraryStats.From(books, Today);

        Assert.Equal(new LibraryStats(Total: 6, Available: 2, OnLoan: 4, Overdue: 1, DueSoon: 1, OnLoanToStudents: 3, OnLoanToTeachers: 1), stats);
    }

    [Fact]
    public void Daily_report_lists_overdue_books_before_due_soon_ones_oldest_first()
    {
        Book[] books =
        [
            TestData.Book("0"),
            TestData.Book("1", loan: TestData.LoanDue(Today.AddDays(2))),
            TestData.Book("2", loan: TestData.LoanDue(Today.AddDays(-1))),
            TestData.Book("3", loan: TestData.LoanDue(Today.AddDays(-9))),
            TestData.Book("4", loan: TestData.LoanDue(Today.AddDays(30))),
        ];

        var report = DailyReport.Create(books, Today);

        Assert.Equal(["3", "2", "1"], report.NeedsAttention.Select(book => book.Id));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("  ", true)]
    [InlineData("mockingbird", true)]
    [InlineData("LEE mock", true)]
    [InlineData("ayesha", true)]
    [InlineData("bts-10-214", true)]
    [InlineData("9780061120084", true)]
    [InlineData("tolkien", false)]
    [InlineData("lee hobbit", false)]
    public void Search_matches_every_word_against_any_field(string query, bool expected)
    {
        var book = new Book
        {
            Id = "7",
            Title = "To Kill a Mockingbird",
            Author = "Harper Lee",
            Barcode = "9780061120084",
            CatalogId = "BTS-0421-55521",
            Loan = TestData.LoanDue(Today),
        };

        Assert.Equal(expected, BookSearch.Matches(book, query));
    }

    [Theory]
    [InlineData("BTS-10-214", "bts10214", true)]
    [InlineData("BTS-10-214", " 10-214 ", true)]
    [InlineData("BTS-10-214", "BTS 10 214", true)]
    [InlineData("BTS-10-214", "BTS-10-215", false)]
    [InlineData("BTS-10-214", "", false)]
    [InlineData("", "", false)]
    public void Member_ids_match_however_they_are_typed(string stored, string typed, bool expected)
    {
        Assert.Equal(expected, MemberId.Matches(stored, typed));
    }

    [Theory]
    [InlineData(MemberRole.Student, "10-B", "Student of 10-B")]
    [InlineData(MemberRole.Teacher, "Physics", "Physics Teacher")]
    [InlineData(MemberRole.Teacher, "", "Teacher")]
    [InlineData(MemberRole.Unknown, "", "Member")]
    public void Roles_are_described_in_plain_words(MemberRole role, string section, string expected)
    {
        Assert.Equal(expected, Member.DescribeRole(role, section));
    }
}
