using System.Globalization;

namespace LibraryManagement.Models;

public enum BookStatus
{
    Available,
    OnLoan,
    DueSoon,
    Overdue,
}

/// <summary>A single physical copy in the library (a document in the <c>Books</c> collection).</summary>
public sealed record Book
{
    /// <summary>The Firestore document ID. Historically a running number ("0", "1", ...).</summary>
    public required string Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Author { get; init; } = string.Empty;

    /// <summary>The publisher's barcode (usually the ISBN). Copies of the same title share it.</summary>
    public string Barcode { get; init; } = string.Empty;

    /// <summary>The library's own label for this copy, e.g. <c>BTS-0421-55521</c>.</summary>
    public string CatalogId { get; init; } = string.Empty;

    public DateOnly? RegisteredOn { get; init; }

    /// <summary>The current loan, or <c>null</c> when the book is on the shelf.</summary>
    public Loan? Loan { get; init; }

    public bool IsAvailable => Loan is null;

    /// <summary>Numeric value of <see cref="Id"/> when it is a number; used for ordering and new IDs.</summary>
    public int? NumericId =>
        int.TryParse(Id, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : null;

    public BookStatus StatusOn(DateOnly today)
    {
        if (Loan is null)
        {
            return BookStatus.Available;
        }

        if (Loan.DueOn is not { } due)
        {
            return BookStatus.OnLoan;
        }

        if (today > due)
        {
            return BookStatus.Overdue;
        }

        return today >= due.AddDays(-LoanPolicy.DueSoonWindowDays) ? BookStatus.DueSoon : BookStatus.OnLoan;
    }

    /// <summary>Whole days past the due date (0 when not overdue).</summary>
    public int DaysOverdueOn(DateOnly today) =>
        Loan?.DueOn is { } due && today > due ? today.DayNumber - due.DayNumber : 0;
}

/// <summary>The fields a librarian can edit on an existing book.</summary>
public sealed record BookDetails(string Title, string Author, string Barcode);
