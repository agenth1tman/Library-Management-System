using System;
using System.Formats.Asn1;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Google.Cloud.Firestore;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Library_Management_System
{
    public class ExportToExcel
    {
        //For all reports
        int TotalBooks, IssuedBooks, OverdueBooks, IssuedToTeachers, IssuedToStudents, UpcomingDeadlines;

        public string ReportType = "DailyReport";//DailyReport //WeeklyReport //MonthlyReport //Yearly Report
        public void Export(DataGridView dataGridView, string _filePath)
        {
            //License na add kero to maut ajati
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            try
            {
                //Anyways, file path to Excel Sheet
                string temp_filepath = GetFilePath();
                string filePath = Path.Combine(_filePath, "Data1.xlsx");
                if (temp_filepath != null) 
                {
                    filePath = temp_filepath;
                }

                using (ExcelPackage excelPackage = new ExcelPackage())
                {
                    //Other Info
                    ExcelWorksheet mainworksheet = excelPackage.Workbook.Worksheets.Add("Info");
                    // Title ong(and lil bit formatting)
                    mainworksheet.Cells[1, 1].Value = "Library Report"; // Tile name
                    mainworksheet.Cells[1, 1, 1, 8].Merge = true; // Merge cells for title
                    mainworksheet.Cells[1, 1].Style.Font.Size = 20; // Set title font size
                    mainworksheet.Cells[1, 1].Style.Font.Bold = true; // Make title bold
                    mainworksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Center align
                    mainworksheet.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid; // Set fill pattern
                    mainworksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 78, 120));
                    mainworksheet.Cells[1, 1].Style.Font.Color.SetColor(Color.White);
                    mainworksheet.Cells[1, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin); // Add a border around merged title cell


                    //Headers
                    //Add headers
                    int c = 1;
                    for (int i = 0; i < 2; i++)
                    {
                        //Text
                        if (c == 1)
                        {
                            mainworksheet.Cells[2, c].Value = "Report Type: " + ReportType;
                        }
                        else
                        {
                            mainworksheet.Cells[2, c].Value = "Date: " + DateTime.Now.ToString("dd-MMM-yy");
                        }
                        //Merging
                        mainworksheet.Cells[2, c, 2, c + 3].Merge = true; // Merge cells for title
                        //Formatting
                        using (var headerRange = mainworksheet.Cells[2, c, 2, c + 3])
                        {
                            headerRange.Style.Font.Bold = true; // Make the font bold
                            headerRange.Style.Font.Size = 14; // Set font size for headers
                            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid; // Set fill pattern
                            headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightBlue); // Set background color
                            headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Center align horizontally
                            headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Center align vertically
                            headerRange.Style.Border.BorderAround(ExcelBorderStyle.Thin); // Add a border around merged header cell
                        }

                        c += 4;
                    }
                    int ar = 3;
                    int index = 1;
                    int counter = 3;
                    if (ReportType != "DailyReport") { counter = 4; } else { counter = 3; }
                    //Add rows
                    for (int i = 0; i < counter; i++)
                    {
                        //Add Text Indeed
                        if (index == 1) 
                        {
                            mainworksheet.Cells[ar, 1].Value = "Total Books: " + TotalBooks;
                        }else if(index == 2) 
                        {
                            mainworksheet.Cells[ar, 1].Value = "Issued Books: " + IssuedBooks;
                        }else if(index == 3) 
                        {
                            mainworksheet.Cells[ar, 1].Value = "Overdue Books: " + OverdueBooks;
                        }

                        //Merging
                        mainworksheet.Cells[ar, 1, ar, 4].Merge = true; // Merge cells for title
                        //Formatting
                        using (var headerRange = mainworksheet.Cells[ar, 1, ar, 4])
                        {
                            headerRange.Style.Font.Size = 12; // Set font size for headers
                            headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Center align horizontally
                            headerRange.Style.Border.BorderAround(ExcelBorderStyle.Thin); // Add a border around merged header cell
                        }

                        ar += 1;
                        index += 1;
                    }
                    ar = 3;
                    for (int i = 0; i < counter; i++)
                    {
                        //Add Text Indeed
                        if (index == 4)
                        {
                            mainworksheet.Cells[ar, 5].Value = "Issued to Teachers: " + IssuedToTeachers;
                        }
                        else if (index == 5)
                        {
                            mainworksheet.Cells[ar, 5].Value = "Issued to Students: " + IssuedToStudents;
                        }
                        else if (index == 6)
                        {
                            mainworksheet.Cells[ar, 5].Value = "Upcoming Deadlines: " + UpcomingDeadlines;
                        }
                        //Merging
                        mainworksheet.Cells[ar, 5, ar, 8].Merge = true; // Merge cells for title
                        //Formatting
                        using (var headerRange = mainworksheet.Cells[ar, 5, ar, 8])
                        {
                            headerRange.Style.Font.Size = 12; // Set font size for headers
                            headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Center align horizontally
                            headerRange.Style.Border.BorderAround(ExcelBorderStyle.Thin); // Add a border around merged header cell
                        }

                        ar += 1;
                        index += 1;

                    }

                    //Database to Datatable
                    ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("DataTable");

                    // Add headers
                    for (int i = 0; i < dataGridView.Columns.Count - 1; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = dataGridView.Columns[i].HeaderText;
                        worksheet.Column(i + 1).AutoFit();
                    }

                    // Add rows
                    for (int i = 0; i < dataGridView.Rows.Count; i++)
                    {
                        for (int j = 0; j < dataGridView.Columns.Count - 1; j++)
                        {
                            worksheet.Cells[i + 2, j + 1].Value = dataGridView.Rows[i].Cells[j].Value?.ToString();
                            //Formatting
                            worksheet.Cells[i + 2, j + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        }
                    }

                    // Set header style
                    using (var headerRange = worksheet.Cells[1, 1, 1, dataGridView.Columns.Count - 1])
                    {
                        headerRange.Style.Font.Bold = true; // Make the font bold
                        headerRange.Style.Font.Size = 16; // Set the font size to 16 (adjust as needed)
                        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid; // Fill pattern for background
                        headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightBlue); // Background color for headers
                        headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Center align the text
                        headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center; // Center align vertically
                    }

                    //Add Borders
                    using (var dataRange = worksheet.Cells[1, 1, dataGridView.Rows.Count + 1, dataGridView.Columns.Count - 1])
                    {
                        foreach (var cell in dataRange)
                        {
                            cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        }
                    }

                    //Resize
                    for (int i = 0; i < dataGridView.Columns.Count - 1; i++)
                    {
                        worksheet.Column(i + 1).AutoFit();
                    }

                    // Save to file
                    File.WriteAllBytes(filePath, excelPackage.GetAsByteArray());

                    //Save Date
                    Properties.Settings.Default.PreviousSaveDate = DateTime.Now.ToString("dd-MM-yyyy");
                    Properties.Settings.Default.Save();
                    
                    MessageBox.Show("Data exported to Excel successfully!", "Export Complete!", MessageBoxButtons.OK);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error exporting data: " + ex.Message, "Error", MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }

        public string GetFilePath()
        {
            try
            {
                // Prompt the user to select a save location using SaveFileDialog
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Excel Files|*.xlsx";
                    saveFileDialog.Title = "Save Excel File";
                    saveFileDialog.FileName = ReportType + "[" + DateTime.Now.ToString("dd-MMM-yy") + "]" +".xlsx";

                    // Set the initial directory to a specific folder in Documents
                    string documentsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Library Management System");
                    if (!Directory.Exists(documentsFolder))
                    {
                        Directory.CreateDirectory(documentsFolder); // Create the folder if it doesn't exist
                    }
                    saveFileDialog.InitialDirectory = documentsFolder;

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // Get the selected file path
                        string filePath = saveFileDialog.FileName;
                        return filePath;
                    }
                    else {
                        return null;
                    }
                }
            }
            catch
            {
                return null;
            }

        }

        public void SetData(int _TotalBooks, int _IssuedBooks, int _OverdueBooks, int _IssuedToTeachers, int _IssuedToStudents, int _UpcomingDeadlines)
        {
            ///Set Report Type as well
            TotalBooks = _TotalBooks;
            IssuedBooks = _IssuedBooks;
            OverdueBooks = _OverdueBooks;
            IssuedToTeachers = _IssuedToTeachers;
            IssuedToStudents = _IssuedToStudents;
            UpcomingDeadlines = _UpcomingDeadlines;
        }
    }
}
