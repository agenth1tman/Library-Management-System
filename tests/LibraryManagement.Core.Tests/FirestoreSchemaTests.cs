using Google.Cloud.Firestore;
using LibraryManagement.Data;
using LibraryManagement.Models;

namespace LibraryManagement.Tests;

public class FirestoreSchemaTests
{
    [Fact]
    public void Reads_an_available_book_written_by_the_original_app()
    {
        var fields = new Dictionary<string, object>
        {
            ["Name"] = "The Hobbit",
            ["Author"] = "J.R.R. Tolkien",
            ["Barcode"] = "9780547928227",
            ["BTSID"] = "BTS-0421-55521",
            ["Available"] = true,
            ["RegistrationDate"] = "25-09-2024",
            ["IssueDate"] = "null",
            ["DueDate"] = "null",
            ["IssuedBy"] = "null",
            ["IssuedBy_BTSCID"] = "null",
            ["IssuedBy_Level"] = "null",
            ["IssuedBy_Section"] = "null",
        };

        var book = FirestoreSchema.ToBook("12", fields);

        Assert.Equal("12", book.Id);
        Assert.Equal("The Hobbit", book.Title);
        Assert.Equal("BTS-0421-55521", book.CatalogId);
        Assert.Equal(new DateOnly(2024, 9, 25), book.RegisteredOn);
        Assert.True(book.IsAvailable);
    }

    [Fact]
    public void Reads_an_issued_book_written_by_the_original_app()
    {
        var fields = new Dictionary<string, object>
        {
            ["Name"] = "The Hobbit",
            ["Available"] = false,
            ["IssueDate"] = "01-03-2026",
            ["DueDate"] = "01-04-2026",
            ["IssuedBy"] = "Ayesha Khan",
            ["IssuedBy_BTSCID"] = "BTS-10-214",
            ["IssuedBy_Level"] = "Student",
            ["IssuedBy_Section"] = "10-B",
        };

        var loan = FirestoreSchema.ToBook("3", fields).Loan;

        Assert.NotNull(loan);
        Assert.Equal("Ayesha Khan", loan.MemberName);
        Assert.Equal("BTS-10-214", loan.MemberId);
        Assert.Equal(MemberRole.Student, loan.Role);
        Assert.Equal("10-B", loan.Section);
        Assert.Equal(new DateOnly(2026, 3, 1), loan.IssuedOn);
        Assert.Equal(new DateOnly(2026, 4, 1), loan.DueOn);
    }

    [Fact]
    public void Tolerates_missing_fields_wrong_types_and_bad_dates()
    {
        // The old code called GetValue<string> on every field and crashed on startup
        // ("connection can not be established") if a single document was incomplete.
        var fields = new Dictionary<string, object>
        {
            ["Name"] = "Loose Document",
            ["Barcode"] = 9780547928227L,
            ["Available"] = false,
            ["IssueDate"] = Timestamp.FromDateTime(new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc)),
            ["DueDate"] = string.Empty,
            ["IssuedBy"] = null!,
        };

        var book = FirestoreSchema.ToBook("x", fields);

        Assert.Equal("9780547928227", book.Barcode);
        Assert.Equal(string.Empty, book.Author);
        Assert.Null(book.RegisteredOn);
        Assert.NotNull(book.Loan);
        Assert.Equal(string.Empty, book.Loan.MemberName);
        Assert.Equal(MemberRole.Unknown, book.Loan.Role);
        Assert.NotNull(book.Loan.IssuedOn);
        Assert.Null(book.Loan.DueOn);
    }

    [Fact]
    public void Availability_is_inferred_from_the_borrower_when_the_flag_is_missing()
    {
        Assert.True(FirestoreSchema.ToBook("1", new Dictionary<string, object> { ["IssuedBy"] = "null" }).IsAvailable);
        Assert.False(FirestoreSchema.ToBook("1", new Dictionary<string, object> { ["IssuedBy"] = "Ayesha" }).IsAvailable);
    }

    [Fact]
    public void Writes_new_books_in_the_original_format()
    {
        var book = TestData.Book("5", "Cosmos") with { RegisteredOn = new DateOnly(2026, 3, 9) };

        var fields = FirestoreSchema.NewBookFields(book);

        Assert.Equal("Cosmos", fields["Name"]);
        Assert.Equal("09-03-2026", fields["RegistrationDate"]);
        Assert.Equal(true, fields["Available"]);
        Assert.Equal("null", fields["IssuedBy"]);
        Assert.Equal("null", fields["DueDate"]);
        Assert.Equal(12, fields.Count);
    }

    [Fact]
    public void Loan_fields_round_trip()
    {
        var loan = TestData.LoanDue(new DateOnly(2026, 6, 25), TestData.Teacher);

        var fields = FirestoreSchema.LoanFields(loan);
        var book = FirestoreSchema.ToBook("1", fields);

        Assert.Equal(false, fields["Available"]);
        Assert.Equal("Teacher", fields["IssuedBy_Level"]);
        Assert.Equal("25-06-2026", fields["DueDate"]);
        Assert.Equal(loan, book.Loan);
    }

    [Fact]
    public void Returning_a_book_clears_every_loan_field()
    {
        var fields = FirestoreSchema.LoanFields(null);

        Assert.Equal(true, fields["Available"]);
        Assert.All(fields.Where(field => field.Key != "Available"), field => Assert.Equal("null", field.Value));
        Assert.True(FirestoreSchema.ToBook("1", fields).IsAvailable);
    }

    [Fact]
    public void Reads_members()
    {
        var member = FirestoreSchema.ToMember("doc-1", new Dictionary<string, object>
        {
            ["BTSCID"] = "BTS-T1-007",
            ["Name"] = "Sana Iqbal",
            ["Level"] = "Teacher",
            ["Section"] = "Physics",
        });

        Assert.Equal(TestData.Teacher, member);
    }

    [Fact]
    public void A_member_without_an_id_field_falls_back_to_the_document_id()
    {
        var member = FirestoreSchema.ToMember("doc-1", new Dictionary<string, object> { ["Name"] = "No Id" });

        Assert.Equal("doc-1", member.Id);
        Assert.Equal(MemberRole.Unknown, member.Role);
    }
}
