using LibraryManagement.Models;

namespace LibraryManagement.Tests;

internal static class TestData
{
    public static readonly DateOnly Today = new(2026, 3, 10);

    public static readonly Member Student = new("BTS-10-214", "Ayesha Khan", MemberRole.Student, "10-B");

    public static readonly Member Teacher = new("BTS-T1-007", "Sana Iqbal", MemberRole.Teacher, "Physics");

    public static Book Book(string id, string title = "A Book", Loan? loan = null) => new()
    {
        Id = id,
        Title = title,
        Author = "An Author",
        Barcode = "978" + id.PadLeft(10, '0'),
        CatalogId = "BTS-0000-" + id.PadLeft(5, '0'),
        RegisteredOn = Today.AddDays(-100),
        Loan = loan,
    };

    public static Loan LoanDue(DateOnly dueOn, Member? member = null)
    {
        member ??= Student;
        return new Loan
        {
            MemberId = member.Id,
            MemberName = member.Name,
            Role = member.Role,
            Section = member.Section,
            IssuedOn = dueOn.AddMonths(-1),
            DueOn = dueOn,
        };
    }
}
