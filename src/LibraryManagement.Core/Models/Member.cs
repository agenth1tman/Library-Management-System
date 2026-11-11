namespace LibraryManagement.Models;

public enum MemberRole
{
    Unknown,
    Student,
    Teacher,
}

/// <summary>Someone who can borrow books (a document in the <c>Users</c> collection).</summary>
public sealed record Member(string Id, string Name, MemberRole Role, string Section)
{
    /// <summary>Human readable role, e.g. "Student of 10-B" or "Physics Teacher".</summary>
    public string RoleDescription => DescribeRole(Role, Section);

    public static string DescribeRole(MemberRole role, string? section)
    {
        var hasSection = !string.IsNullOrWhiteSpace(section);
        return role switch
        {
            MemberRole.Student => hasSection ? $"Student of {section}" : "Student",
            MemberRole.Teacher => hasSection ? $"{section} Teacher" : "Teacher",
            _ => hasSection ? section! : "Member",
        };
    }
}
