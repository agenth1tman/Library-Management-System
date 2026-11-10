using System.Globalization;
using Google.Cloud.Firestore;
using LibraryManagement.Models;

namespace LibraryManagement.Data;

/// <summary>
/// Maps between the domain model and the documents already stored in Firestore.
/// <para>
/// The original version of the app stored every date as a <c>dd-MM-yyyy</c> string and
/// used the literal text <c>"null"</c> for "no value". That format is kept on write so
/// existing data (and any older copies of the app) keep working, while reads accept
/// anything reasonable: missing fields, real nulls, timestamps and numbers.
/// </para>
/// </summary>
internal static class FirestoreSchema
{
    public const string BooksCollection = "Books";
    public const string MembersCollection = "Users";

    private const string NullText = "null";
    private const string DateFormat = "dd-MM-yyyy";

    private static class BookFields
    {
        public const string Title = "Name";
        public const string Author = "Author";
        public const string Barcode = "Barcode";
        public const string CatalogId = "BTSID";
        public const string Available = "Available";
        public const string RegisteredOn = "RegistrationDate";
        public const string IssuedOn = "IssueDate";
        public const string DueOn = "DueDate";
        public const string BorrowerName = "IssuedBy";
        public const string BorrowerId = "IssuedBy_BTSCID";
        public const string BorrowerRole = "IssuedBy_Level";
        public const string BorrowerSection = "IssuedBy_Section";
    }

    private static class MemberFields
    {
        public const string Id = "BTSCID";
        public const string Name = "Name";
        public const string Role = "Level";
        public const string Section = "Section";
    }

    public static Book ToBook(string documentId, IReadOnlyDictionary<string, object> fields)
    {
        var borrowerName = ReadText(fields, BookFields.BorrowerName);
        var isAvailable = fields.TryGetValue(BookFields.Available, out var flag) && flag is bool available
            ? available
            : borrowerName.Length == 0;

        return new Book
        {
            Id = documentId,
            Title = ReadText(fields, BookFields.Title),
            Author = ReadText(fields, BookFields.Author),
            Barcode = ReadText(fields, BookFields.Barcode),
            CatalogId = ReadText(fields, BookFields.CatalogId),
            RegisteredOn = ReadDate(fields, BookFields.RegisteredOn),
            Loan = isAvailable
                ? null
                : new Loan
                {
                    MemberName = borrowerName,
                    MemberId = ReadText(fields, BookFields.BorrowerId),
                    Role = ParseRole(ReadText(fields, BookFields.BorrowerRole)),
                    Section = ReadText(fields, BookFields.BorrowerSection),
                    IssuedOn = ReadDate(fields, BookFields.IssuedOn),
                    DueOn = ReadDate(fields, BookFields.DueOn),
                },
        };
    }

    public static Member ToMember(string documentId, IReadOnlyDictionary<string, object> fields)
    {
        var id = ReadText(fields, MemberFields.Id);
        return new Member(
            id.Length > 0 ? id : documentId,
            ReadText(fields, MemberFields.Name),
            ParseRole(ReadText(fields, MemberFields.Role)),
            ReadText(fields, MemberFields.Section));
    }

    public static Dictionary<string, object> NewBookFields(Book book)
    {
        var fields = DetailsFields(new BookDetails(book.Title, book.Author, book.Barcode));
        fields[BookFields.CatalogId] = book.CatalogId;
        fields[BookFields.RegisteredOn] = WriteDate(book.RegisteredOn);

        foreach (var (name, value) in LoanFields(book.Loan))
        {
            fields[name] = value;
        }

        return fields;
    }

    public static Dictionary<string, object> DetailsFields(BookDetails details) => new()
    {
        [BookFields.Title] = details.Title,
        [BookFields.Author] = details.Author,
        [BookFields.Barcode] = details.Barcode,
    };

    public static Dictionary<string, object> LoanFields(Loan? loan) => new()
    {
        [BookFields.Available] = loan is null,
        [BookFields.BorrowerName] = WriteText(loan?.MemberName),
        [BookFields.BorrowerId] = WriteText(loan?.MemberId),
        [BookFields.BorrowerRole] = loan is null or { Role: MemberRole.Unknown } ? NullText : loan.Role.ToString(),
        [BookFields.BorrowerSection] = WriteText(loan?.Section),
        [BookFields.IssuedOn] = WriteDate(loan?.IssuedOn),
        [BookFields.DueOn] = WriteDate(loan?.DueOn),
    };

    private static MemberRole ParseRole(string text) =>
        Enum.TryParse<MemberRole>(text, ignoreCase: true, out var role) ? role : MemberRole.Unknown;

    private static string ReadText(IReadOnlyDictionary<string, object> fields, string name)
    {
        if (!fields.TryGetValue(name, out var value) || value is null)
        {
            return string.Empty;
        }

        var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        return text.Equals(NullText, StringComparison.OrdinalIgnoreCase) ? string.Empty : text;
    }

    private static DateOnly? ReadDate(IReadOnlyDictionary<string, object> fields, string name)
    {
        if (!fields.TryGetValue(name, out var value))
        {
            return null;
        }

        switch (value)
        {
            case Timestamp timestamp:
                return DateOnly.FromDateTime(timestamp.ToDateTime().ToLocalTime());
            case DateTime dateTime:
                return DateOnly.FromDateTime(dateTime);
        }

        var text = ReadText(fields, name);
        if (DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
        {
            return exact;
        }

        // Hand-edited documents sometimes use other shapes (2024-09-25, 25/09/2024).
        return DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var loose)
            ? loose
            : null;
    }

    private static string WriteText(string? text) => string.IsNullOrWhiteSpace(text) ? NullText : text.Trim();

    private static string WriteDate(DateOnly? date) =>
        date?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? NullText;
}
