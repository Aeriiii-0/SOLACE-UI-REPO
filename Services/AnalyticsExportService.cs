using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SOLUM_UI.Models;

namespace SOLUM_UI.Services
{
    public static class AnalyticsExportService
    {
        private static int age(SoloParentRecord r) =>
            r.DateOfBirth != DateTime.MinValue
                ? (int)((DateTime.Today - r.DateOfBirth).TotalDays / 365.25)
                : 0;

        public static void Export(string filePath, List<SoloParentRecord> records, string period, string password = null)
        {
            using (var pkg = string.IsNullOrEmpty(password)
                ? new ExcelPackage()
                : new ExcelPackage())
            {
                buildSummarySheet(pkg, records, period);
                buildRecordsSheet(pkg, records);

                if (!string.IsNullOrEmpty(password))
                    pkg.SaveAs(new System.IO.FileInfo(filePath), password);
                else
                    pkg.SaveAs(new System.IO.FileInfo(filePath));
            }
        }

        private static void buildSummarySheet(ExcelPackage pkg, List<SoloParentRecord> d, string period)
        {
            var ws = pkg.Workbook.Worksheets.Add("LGU Summary");

            var accent = Color.FromArgb(0x70, 0x29, 0x43);
            var light  = Color.FromArgb(0xF0, 0xE8, 0xEC);
            var grey   = Color.FromArgb(0x77, 0x77, 0x77);

            int row = 1;

            ws.Cells[row, 1].Value = "LGU SUMMARY OF SOLO PARENTS";
            headerStyle(ws.Cells[row, 1, row, 3], accent, Color.White, 13);
            ws.Row(row).Height = 22;
            row++;

            ws.Cells[row, 1].Value = "As of " + period;
            labelStyle(ws.Cells[row, 1], grey);
            row += 2;

            infoRow(ws, ref row, "City / Municipality / Province", "BINAN, LAGUNA");
            infoRow(ws, ref row, "Region", "IV-A");
            row++;

            infoRow(ws, ref row, "Number of Solo Parents served", d.Count.ToString());
            row++;

            sectionHeader(ws, ref row, "Age", accent, light);
            dataRow(ws, ref row, "19 years old and below",        d.Count(r => age(r) <= 19));
            dataRow(ws, ref row, "20-39 years old",               d.Count(r => age(r) >= 20 && age(r) <= 39));
            dataRow(ws, ref row, "40-59 years old",               d.Count(r => age(r) >= 40 && age(r) <= 59));
            dataRow(ws, ref row, "60 and above",                  d.Count(r => age(r) >= 60));
            row++;

            sectionHeader(ws, ref row, "Sex", accent, light);
            dataRow(ws, ref row, "Male",   d.Count(r => r.Sex == "Male"));
            dataRow(ws, ref row, "Female", d.Count(r => r.Sex == "Female"));
            row++;

            sectionHeader(ws, ref row, "Civil Status", accent, light);
            dataRow(ws, ref row, "Single",                       d.Count(r => r.CivilStatus == "Single"));
            dataRow(ws, ref row, "Married",                      d.Count(r => r.CivilStatus == "Married"));
            dataRow(ws, ref row, "Widowed",                      d.Count(r => r.CivilStatus == "Widowed"));
            dataRow(ws, ref row, "Legally Separated / Annulled", d.Count(r => r.CivilStatus == "Separated" || r.CivilStatus == "Annulled"));
            row++;

            sectionHeader(ws, ref row, "Employment Status", accent, light);
            dataRow(ws, ref row, "Employed (public & private)", d.Count(r => r.IsEmployed));
            dataRow(ws, ref row, "Self employed",               d.Count(r => r.IsSelfEmployed));
            dataRow(ws, ref row, "Not employed",                d.Count(r => r.IsNotEmployed));
            row++;

            sectionHeader(ws, ref row, "Monthly Income", accent, light);
            dataRow(ws, ref row, "Below minimum wage",           d.Count(r => r.MonthlyIncome == "below minimum wage"));
            dataRow(ws, ref row, "Minimum wage +1 to Php 20833", d.Count(r => r.MonthlyIncome == "Minimum wage +1 to Php 20833"));
            dataRow(ws, ref row, "Php 20834 and above",          d.Count(r => r.MonthlyIncome == "Php 20834 and above"));
            row++;

            int depBelow6  = d.Sum(r => r.FamilyMembers.Count(m => { int a = 0; int.TryParse(m.Age, out a); return a <= 6; }));
            int dep7to22   = d.Sum(r => r.FamilyMembers.Count(m => { int a = 0; int.TryParse(m.Age, out a); return a >= 7 && a <= 22; }));
            int depAbove22 = d.Sum(r => r.FamilyMembers.Count(m => { int a = 0; int.TryParse(m.Age, out a); return a > 22; }));
            sectionHeader(ws, ref row, "No. of Children / Dependent", accent, light);
            dataRow(ws, ref row, "6 years old and below",  depBelow6);
            dataRow(ws, ref row, "7-22 years old",         dep7to22);
            dataRow(ws, ref row, "22 years old and above", depAbove22);
            row++;

            sectionHeader(ws, ref row, "Category", accent, light);
            dataRow(ws, ref row, "a1. Consequence of rape",               d.Count(r => r.CircumstanceA1));
            dataRow(ws, ref row, "a2. Widow/widower",                     d.Count(r => r.CircumstanceA2));
            dataRow(ws, ref row, "a3. Spouse of PDL",                     d.Count(r => r.CircumstanceA3));
            dataRow(ws, ref row, "a4. Spouse of PWD",                     d.Count(r => r.CircumstanceA4));
            dataRow(ws, ref row, "a5. Separated or de facto separated",   d.Count(r => r.CircumstanceA5));
            dataRow(ws, ref row, "a6. Annulled",                          d.Count(r => r.CircumstanceA6));
            dataRow(ws, ref row, "a7. Abandoned",                         d.Count(r => r.CircumstanceA7));
            dataRow(ws, ref row, "b. Spouse/Relative of OFW",             d.Count(r => r.CircumstanceB));
            dataRow(ws, ref row, "c. Unmarried person",                   d.Count(r => r.CircumstanceC));
            dataRow(ws, ref row, "d. Legal Guardian, Adoptive or Foster", d.Count(r => r.CircumstanceD));
            dataRow(ws, ref row, "e. Relative",                           d.Count(r => r.CircumstanceE));
            dataRow(ws, ref row, "f. Pregnant woman",                     d.Count(r => r.CircumstanceF));
            row++;

            sectionHeader(ws, ref row, "Solo Parent Identification Card", accent, light);
            dataRow(ws, ref row, "Newly issued SPIC", d.Count(r => r.IsNewApplicant));
            dataRow(ws, ref row, "Renewed SPIC",      d.Count(r => r.IsRenewal));
            dataRow(ws, ref row, "Terminated SPIC",   0);

            ws.Column(1).Width = 36;
            ws.Column(2).Width = 48;
            ws.Column(3).Width = 14;
        }

        private static void buildRecordsSheet(ExcelPackage pkg, List<SoloParentRecord> records)
        {
            var ws = pkg.Workbook.Worksheets.Add("Individual Records");
            var accent = Color.FromArgb(0x70, 0x29, 0x43);

            string[] headers = {
                "SP ID", "Surname", "First Name", "Middle Name",
                "Date of Birth", "Age", "Sex", "Civil Status",
                "Barangay", "Address", "Contact Number",
                "Employment", "Monthly Income", "Children", "Status",
                "Circumstance", "SPIC Type", "Last Updated"
            };

            for (int c = 0; c < headers.Length; c++)
            {
                var cell = ws.Cells[1, c + 1];
                cell.Value = headers[c];
                cell.Style.Font.Bold      = true;
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(accent);
                cell.Style.Font.Size      = 10;
            }
            ws.Row(1).Height = 20;

            int row = 2;
            foreach (var r in records.OrderBy(x => x.Surname).ThenBy(x => x.FirstName))
            {
                ws.Cells[row, 1].Value  = r.Id;
                ws.Cells[row, 2].Value  = r.Surname ?? "";
                ws.Cells[row, 3].Value  = r.FirstName ?? "";
                ws.Cells[row, 4].Value  = r.MiddleName ?? "";
                ws.Cells[row, 5].Value  = r.DateOfBirth != DateTime.MinValue ? r.DateOfBirth.ToString("yyyy-MM-dd") : "";
                ws.Cells[row, 6].Value  = age(r);
                ws.Cells[row, 7].Value  = r.Sex ?? "";
                ws.Cells[row, 8].Value  = r.CivilStatus ?? "";
                ws.Cells[row, 9].Value  = r.Barangay ?? "";
                ws.Cells[row, 10].Value = r.Address ?? "";
                ws.Cells[row, 11].Value = r.ContactNumber ?? "";
                ws.Cells[row, 12].Value = r.EmploymentStatusDisplay;
                ws.Cells[row, 13].Value = r.MonthlyIncome ?? "";
                ws.Cells[row, 14].Value = r.Children;
                ws.Cells[row, 15].Value = r.Status ?? "";
                ws.Cells[row, 16].Value = r.CircumstancesDisplay.Replace("\n", "; ");
                ws.Cells[row, 17].Value = r.IsNewApplicant ? "New" : r.IsRenewal ? "Renewal" : "";
                ws.Cells[row, 18].Value = r.LastUpdated != DateTime.MinValue ? r.LastUpdated.ToString("yyyy-MM-dd") : "";

                if (row % 2 == 0)
                {
                    ws.Cells[row, 1, row, 18].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, 1, row, 18].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0xFD, 0xF5, 0xF7));
                }
                row++;
            }

            double[] colWidths = { 10, 18, 18, 16, 14, 6, 8, 18, 18, 30, 16, 20, 24, 10, 10, 40, 10, 14 };
            for (int c = 0; c < colWidths.Length; c++)
                ws.Column(c + 1).Width = colWidths[c];

            ws.View.FreezePanes(2, 1);
        }

        private static void sectionHeader(ExcelWorksheet ws, ref int row, string label, Color accent, Color light)
        {
            var range = ws.Cells[row, 1, row, 3];
            range.Merge = true;
            range.Value = label.ToUpper();
            range.Style.Font.Bold = true;
            range.Style.Font.Size = 10;
            range.Style.Font.Color.SetColor(accent);
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(light);
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Color.SetColor(Color.FromArgb(0xD4, 0xA0, 0xB0));
            ws.Row(row).Height = 18;
            row++;
        }

        private static void dataRow(ExcelWorksheet ws, ref int row, string label, int count)
        {
            ws.Cells[row, 2].Value = label;
            ws.Cells[row, 2].Style.Font.Size = 10;
            ws.Cells[row, 2].Style.Indent = 1;
            ws.Cells[row, 3].Value = count;
            ws.Cells[row, 3].Style.Font.Bold = true;
            ws.Cells[row, 3].Style.Font.Size = 10;
            ws.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Row(row).Height = 16;
            row++;
        }

        private static void infoRow(ExcelWorksheet ws, ref int row, string label, string value)
        {
            ws.Cells[row, 1].Value = label;
            ws.Cells[row, 1].Style.Font.Color.SetColor(Color.FromArgb(0x77, 0x77, 0x77));
            ws.Cells[row, 1].Style.Font.Size = 10;
            ws.Cells[row, 2].Value = value;
            ws.Cells[row, 2].Style.Font.Bold = true;
            ws.Cells[row, 2].Style.Font.Size = 10;
            ws.Row(row).Height = 16;
            row++;
        }

        private static void headerStyle(ExcelRange range, Color bg, Color fg, int size)
        {
            range.Merge = true;
            range.Style.Font.Bold = true;
            range.Style.Font.Size = size;
            range.Style.Font.Color.SetColor(fg);
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(bg);
        }

        private static void labelStyle(ExcelRange range, Color color)
        {
            range.Style.Font.Color.SetColor(color);
            range.Style.Font.Size = 10;
        }

        public static AnalyticsSummaryDto BuildSummaryDto(List<SoloParentRecord> d, string period)
        {
            return new AnalyticsSummaryDto
            {
                Period           = period,
                GeneratedAt      = DateTime.UtcNow,
                TotalSoloParents = d.Count,
                ValidCount       = d.Count(r => r.Status == "Valid"),
                InactiveCount    = d.Count(r => r.Status == "Inactive"),
                MaleCount        = d.Count(r => r.Sex == "Male"),
                FemaleCount      = d.Count(r => r.Sex == "Female"),
                TotalDependants  = d.Sum(r => r.Children),
                NewSpicCount     = d.Count(r => r.IsNewApplicant),
                RenewedSpicCount = d.Count(r => r.IsRenewal),
                Age19Below       = d.Count(r => age(r) <= 19),
                Age20To39        = d.Count(r => age(r) >= 20 && age(r) <= 39),
                Age40To59        = d.Count(r => age(r) >= 40 && age(r) <= 59),
                Age60Above       = d.Count(r => age(r) >= 60),
                CivilSingle      = d.Count(r => r.CivilStatus == "Single"),
                CivilMarried     = d.Count(r => r.CivilStatus == "Married"),
                CivilWidowed     = d.Count(r => r.CivilStatus == "Widowed"),
                CivilSepAnnulled = d.Count(r => r.CivilStatus == "Separated" || r.CivilStatus == "Annulled"),
                EmpEmployed      = d.Count(r => r.IsEmployed),
                EmpSelfEmployed  = d.Count(r => r.IsSelfEmployed),
                EmpNotEmployed   = d.Count(r => r.IsNotEmployed),
                IncomeBelowMin   = d.Count(r => r.MonthlyIncome == "below minimum wage"),
                IncomeMinPlus    = d.Count(r => r.MonthlyIncome == "Minimum wage +1 to Php 20833"),
                IncomeAbove20833 = d.Count(r => r.MonthlyIncome == "Php 20834 and above"),
                DepBelow6        = d.Sum(r => r.FamilyMembers.Count(m => { int a = 0; int.TryParse(m.Age, out a); return a <= 6; })),
                Dep7To22         = d.Sum(r => r.FamilyMembers.Count(m => { int a = 0; int.TryParse(m.Age, out a); return a >= 7 && a <= 22; })),
                DepAbove22       = d.Sum(r => r.FamilyMembers.Count(m => { int a = 0; int.TryParse(m.Age, out a); return a > 22; })),
                CatA1 = d.Count(r => r.CircumstanceA1), CatA2 = d.Count(r => r.CircumstanceA2),
                CatA3 = d.Count(r => r.CircumstanceA3), CatA4 = d.Count(r => r.CircumstanceA4),
                CatA5 = d.Count(r => r.CircumstanceA5), CatA6 = d.Count(r => r.CircumstanceA6),
                CatA7 = d.Count(r => r.CircumstanceA7), CatB  = d.Count(r => r.CircumstanceB),
                CatC  = d.Count(r => r.CircumstanceC),  CatD  = d.Count(r => r.CircumstanceD),
                CatE  = d.Count(r => r.CircumstanceE),  CatF  = d.Count(r => r.CircumstanceF),
                ByBarangay = d.GroupBy(r => r.Barangay ?? "Unknown")
                              .OrderByDescending(g => g.Count())
                              .ToDictionary(g => g.Key, g => g.Count()),
            };
        }
    }

    public class AnalyticsSummaryDto
    {
        public string   Period           { get; set; }
        public DateTime GeneratedAt      { get; set; }
        public int TotalSoloParents      { get; set; }
        public int ValidCount            { get; set; }
        public int InactiveCount         { get; set; }
        public int MaleCount             { get; set; }
        public int FemaleCount           { get; set; }
        public int TotalDependants       { get; set; }
        public int NewSpicCount          { get; set; }
        public int RenewedSpicCount      { get; set; }
        public int Age19Below            { get; set; }
        public int Age20To39             { get; set; }
        public int Age40To59             { get; set; }
        public int Age60Above            { get; set; }
        public int CivilSingle           { get; set; }
        public int CivilMarried          { get; set; }
        public int CivilWidowed          { get; set; }
        public int CivilSepAnnulled      { get; set; }
        public int EmpEmployed           { get; set; }
        public int EmpSelfEmployed       { get; set; }
        public int EmpNotEmployed        { get; set; }
        public int IncomeBelowMin        { get; set; }
        public int IncomeMinPlus         { get; set; }
        public int IncomeAbove20833      { get; set; }
        public int DepBelow6             { get; set; }
        public int Dep7To22              { get; set; }
        public int DepAbove22            { get; set; }
        public int CatA1, CatA2, CatA3, CatA4, CatA5, CatA6, CatA7;
        public int CatB, CatC, CatD, CatE, CatF;
        public Dictionary<string, int> ByBarangay { get; set; }
    }
}
