using LibraryManagement.Models;

namespace LibraryManagement.Tests;

public class LoanPolicyTests
{
    [Fact]
    public void Students_borrow_for_one_month()
    {
        Assert.Equal(new DateOnly(2026, 4, 10), LoanPolicy.DueDateFor(MemberRole.Student, new DateOnly(2026, 3, 10)));
    }

    [Fact]
    public void Month_end_is_clamped_rather_than_overflowing()
    {
        Assert.Equal(new DateOnly(2026, 2, 28), LoanPolicy.DueDateFor(MemberRole.Student, new DateOnly(2026, 1, 31)));
    }

    [Fact]
    public void Teachers_borrow_until_the_end_of_the_school_year()
    {
        Assert.Equal(new DateOnly(2026, 6, 25), LoanPolicy.DueDateFor(MemberRole.Teacher, new DateOnly(2026, 3, 10)));
    }

    [Theory]
    [InlineData(6, 25)]
    [InlineData(9, 1)]
    [InlineData(12, 31)]
    public void Teacher_loans_after_the_school_year_end_roll_over_to_next_year(int month, int day)
    {
        // The old code always used 25 June of the *current* year, so a book issued in
        // September was overdue the moment it left the desk.
        var issuedOn = new DateOnly(2026, month, day);

        var due = LoanPolicy.DueDateFor(MemberRole.Teacher, issuedOn);

        Assert.Equal(new DateOnly(2027, 6, 25), due);
        Assert.True(due > issuedOn);
    }

    [Fact]
    public void Members_without_a_known_role_get_the_student_period()
    {
        Assert.Equal(new DateOnly(2026, 4, 10), LoanPolicy.DueDateFor(MemberRole.Unknown, new DateOnly(2026, 3, 10)));
    }
}
