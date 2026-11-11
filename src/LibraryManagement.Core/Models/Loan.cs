namespace LibraryManagement.Models;

/// <summary>Who currently has a book and when it is due back.</summary>
public sealed record Loan
{
    public required string MemberId { get; init; }

    public required string MemberName { get; init; }

    public MemberRole Role { get; init; }

    public string Section { get; init; } = string.Empty;

    public DateOnly? IssuedOn { get; init; }

    public DateOnly? DueOn { get; init; }

    public string RoleDescription => Member.DescribeRole(Role, Section);
}
