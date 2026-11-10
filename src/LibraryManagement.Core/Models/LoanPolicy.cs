namespace LibraryManagement.Models;

/// <summary>The library's lending rules, kept in one place so they are easy to change.</summary>
public static class LoanPolicy
{
    /// <summary>A loan counts as "due soon" this many days before its due date.</summary>
    public const int DueSoonWindowDays = 3;

    /// <summary>Students borrow for one month.</summary>
    public const int StudentLoanMonths = 1;

    /// <summary>Teachers keep books until the end of the school year.</summary>
    public const int SchoolYearEndMonth = 6;

    public const int SchoolYearEndDay = 25;

    public static DateOnly DueDateFor(MemberRole role, DateOnly issuedOn) => role switch
    {
        MemberRole.Teacher => NextSchoolYearEnd(issuedOn),
        _ => issuedOn.AddMonths(StudentLoanMonths),
    };

    private static DateOnly NextSchoolYearEnd(DateOnly from)
    {
        var end = new DateOnly(from.Year, SchoolYearEndMonth, SchoolYearEndDay);
        return from < end ? end : end.AddYears(1);
    }
}
