namespace LibraryManagement.Models;

/// <summary>A snapshot of the library for one day: headline numbers plus the loans that need chasing.</summary>
public sealed record DailyReport(
    DateOnly Date,
    LibraryStats Stats,
    IReadOnlyList<Book> Overdue,
    IReadOnlyList<Book> DueSoon)
{
    /// <summary>Overdue books first (longest overdue on top), then the ones due soonest.</summary>
    public IEnumerable<Book> NeedsAttention => Overdue.Concat(DueSoon);

    public static DailyReport Create(IReadOnlyList<Book> books, DateOnly date)
    {
        List<Book> WithStatus(BookStatus status) => books
            .Where(book => book.StatusOn(date) == status)
            .OrderBy(book => book.Loan!.DueOn)
            .ToList();

        return new DailyReport(
            date,
            LibraryStats.From(books, date),
            WithStatus(BookStatus.Overdue),
            WithStatus(BookStatus.DueSoon));
    }
}
