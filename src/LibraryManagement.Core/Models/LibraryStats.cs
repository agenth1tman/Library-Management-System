namespace LibraryManagement.Models;

/// <summary>Headline numbers for the dashboard and the daily report.</summary>
public sealed record LibraryStats(
    int Total,
    int Available,
    int OnLoan,
    int Overdue,
    int DueSoon,
    int OnLoanToStudents,
    int OnLoanToTeachers)
{
    public static LibraryStats Empty { get; } = new(0, 0, 0, 0, 0, 0, 0);

    public static LibraryStats From(IEnumerable<Book> books, DateOnly today)
    {
        int total = 0, onLoan = 0, overdue = 0, dueSoon = 0, students = 0, teachers = 0;

        foreach (var book in books)
        {
            total++;
            if (book.Loan is not { } loan)
            {
                continue;
            }

            onLoan++;
            switch (loan.Role)
            {
                case MemberRole.Student:
                    students++;
                    break;
                case MemberRole.Teacher:
                    teachers++;
                    break;
            }

            switch (book.StatusOn(today))
            {
                case BookStatus.Overdue:
                    overdue++;
                    break;
                case BookStatus.DueSoon:
                    dueSoon++;
                    break;
            }
        }

        return new LibraryStats(total, total - onLoan, onLoan, overdue, dueSoon, students, teachers);
    }
}
