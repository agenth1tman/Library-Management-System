using System.Drawing;
using System.Globalization;
using LibraryManagement.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace LibraryManagement.Reporting;

/// <summary>Writes a <see cref="DailyReport"/> to an Excel workbook.</summary>
public static class DailyReportExporter
{
    private const string DateFormat = "dd MMM yyyy";

    private static readonly Color TitleFill = Color.FromArgb(41, 44, 86);
    private static readonly Color TitleText = Color.FromArgb(240, 197, 57);
    private static readonly Color HeaderFill = Color.FromArgb(226, 228, 245);
    private static readonly Color OverdueFill = Color.FromArgb(253, 226, 226);
    private static readonly Color DueSoonFill = Color.FromArgb(255, 241, 214);

    static DailyReportExporter()
    {
        // EPPlus is free for non-commercial use (such as a school library) and requires
        // that choice to be stated before the first workbook is created.
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public static string DefaultFileName(DateOnly date) =>
        $"DailyReport-{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.xlsx";

    /// <summary>Creates (or overwrites) <paramref name="filePath"/>, creating its folder if needed.</summary>
    public static void Export(DailyReport report, string filePath)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var folder = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        using var package = new ExcelPackage();
        WriteSummary(package.Workbook.Worksheets.Add("Summary"), report);
        WriteBooks(package.Workbook.Worksheets.Add("Needs attention"), report);
        package.SaveAs(new FileInfo(filePath));
    }

    private static void WriteSummary(ExcelWorksheet sheet, DailyReport report)
    {
        sheet.Cells[1, 1].Value = "Library daily report";
        using (var title = sheet.Cells[1, 1, 1, 2])
        {
            title.Merge = true;
            title.Style.Font.Size = 18;
            title.Style.Font.Bold = true;
            title.Style.Font.Color.SetColor(TitleText);
            title.Style.Fill.PatternType = ExcelFillStyle.Solid;
            title.Style.Fill.BackgroundColor.SetColor(TitleFill);
            title.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }

        sheet.Row(1).Height = 32;

        var stats = report.Stats;
        (string Label, object Value)[] rows =
        [
            ("Date", report.Date.ToString(DateFormat, CultureInfo.CurrentCulture)),
            ("Total books", stats.Total),
            ("Available", stats.Available),
            ("On loan", stats.OnLoan),
            ("On loan to students", stats.OnLoanToStudents),
            ("On loan to teachers", stats.OnLoanToTeachers),
            ("Overdue", stats.Overdue),
            ($"Due within {LoanPolicy.DueSoonWindowDays} days", stats.DueSoon),
        ];

        for (var index = 0; index < rows.Length; index++)
        {
            var row = index + 3;
            sheet.Cells[row, 1].Value = rows[index].Label;
            sheet.Cells[row, 1].Style.Font.Bold = true;
            sheet.Cells[row, 2].Value = rows[index].Value;
            sheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
        }

        using (var table = sheet.Cells[3, 1, rows.Length + 2, 2])
        {
            table.Style.Font.Size = 12;
            ApplyThinBorders(table);
        }

        sheet.Column(1).Width = 30;
        sheet.Column(2).Width = 24;
    }

    private static void WriteBooks(ExcelWorksheet sheet, DailyReport report)
    {
        string[] headers =
        [
            "Status", "Book ID", "Title", "Author", "Barcode", "Catalogue ID",
            "Borrower", "Member ID", "Role", "Issued", "Due", "Days overdue",
        ];

        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cells[1, column + 1].Value = headers[column];
        }

        using (var header = sheet.Cells[1, 1, 1, headers.Length])
        {
            header.Style.Font.Bold = true;
            header.Style.Fill.PatternType = ExcelFillStyle.Solid;
            header.Style.Fill.BackgroundColor.SetColor(HeaderFill);
            header.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        var row = 2;
        foreach (var book in report.NeedsAttention)
        {
            var loan = book.Loan!;
            var overdue = book.StatusOn(report.Date) == BookStatus.Overdue;

            object?[] values =
            [
                overdue ? "Overdue" : "Due soon",
                book.NumericId is { } number ? number : book.Id,
                book.Title,
                book.Author,
                book.Barcode,
                book.CatalogId,
                loan.MemberName,
                loan.MemberId,
                loan.RoleDescription,
                FormatDate(loan.IssuedOn),
                FormatDate(loan.DueOn),
                overdue ? book.DaysOverdueOn(report.Date) : null,
            ];

            for (var column = 0; column < values.Length; column++)
            {
                sheet.Cells[row, column + 1].Value = values[column];
            }

            // Barcodes are long digit strings; keep Excel from showing them as 9.78E+12.
            sheet.Cells[row, 5].Style.Numberformat.Format = "@";

            var status = sheet.Cells[row, 1];
            status.Style.Font.Bold = true;
            status.Style.Fill.PatternType = ExcelFillStyle.Solid;
            status.Style.Fill.BackgroundColor.SetColor(overdue ? OverdueFill : DueSoonFill);
            row++;
        }

        if (row == 2)
        {
            sheet.Cells[2, 1].Value = "Nothing is overdue or due soon.";
            sheet.Cells[2, 1, 2, headers.Length].Merge = true;
            row++;
        }

        using (var table = sheet.Cells[1, 1, row - 1, headers.Length])
        {
            table.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ApplyThinBorders(table);
            table.AutoFitColumns(10, 60);
        }

        sheet.View.FreezePanes(2, 1);
    }

    private static string FormatDate(DateOnly? date) =>
        date?.ToString(DateFormat, CultureInfo.CurrentCulture) ?? string.Empty;

    private static void ApplyThinBorders(ExcelRange range)
    {
        var border = range.Style.Border;
        border.Top.Style = ExcelBorderStyle.Thin;
        border.Bottom.Style = ExcelBorderStyle.Thin;
        border.Left.Style = ExcelBorderStyle.Thin;
        border.Right.Style = ExcelBorderStyle.Thin;
    }
}
