using OfficeOpenXml;
using OfficeOpenXml.Style;
using SOLUM_UI.Models.Api;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace SOLUM_UI.Services
{
    /// <summary>
    /// Generates official DSWD LGU Summary of Solo Parents and Regional/Provincial Summary reports
    /// matching the exact government formatting in both Excel (.xlsx) and CSV (.csv) formats.
    /// </summary>
    public static class AnalyticsExcelExportService
    {
        private const string FontName = "Segoe UI";

        private static class ReportColors
        {
            public static readonly Color MaroonPrimary = Color.FromArgb(0x70, 0x29, 0x43);
            public static readonly Color TextDark = Color.FromArgb(0x1F, 0x1F, 0x1F);
            public static readonly Color TextMuted = Color.FromArgb(0x55, 0x55, 0x55);
            public static readonly Color BorderSubtle = Color.FromArgb(0xD4, 0xD4, 0xD4);
            public static readonly Color HeaderSoft = Color.FromArgb(0xF8, 0xF2, 0xF4);
        }

        private class ReportMetrics
        {
            public int TotalServed { get; set; }

            public int Age19Below { get; set; }
            public int Age20To39 { get; set; }
            public int Age40To59 { get; set; }
            public int Age60Above { get; set; }

            public int MaleCount { get; set; }
            public int FemaleCount { get; set; }

            public int CivilSingle { get; set; }
            public int CivilMarried { get; set; }
            public int CivilWidowed { get; set; }
            public int CivilSeparatedAnnulled { get; set; }

            public int EmpEmployed { get; set; }
            public int EmpSelfEmployed { get; set; }
            public int EmpNotEmployed { get; set; }

            public int IncomeBelowMin { get; set; }
            public int IncomeMinPlus { get; set; }
            public int IncomeAbove20833 { get; set; }

            public int DepBelow6 { get; set; }
            public int Dep7To22 { get; set; }
            public int DepAbove22 { get; set; }

            public int CatA1 { get; set; }
            public int CatA2 { get; set; }
            public int CatA3 { get; set; }
            public int CatA4 { get; set; }
            public int CatA5 { get; set; }
            public int CatA6 { get; set; }
            public int CatA7 { get; set; }
            public int CatB { get; set; }
            public int CatC { get; set; }
            public int CatD { get; set; }
            public int CatE { get; set; }
            public int CatF { get; set; }

            public int SpicNew { get; set; }
            public int SpicRenewed { get; set; }
            public int SpicTerminated { get; set; }

            public int PantawidYes { get; set; }
            public int PantawidNo { get; set; }

            public int IpYes { get; set; }
            public int IpNo { get; set; }

            public int LgbtYes { get; set; }
            public int LgbtNo { get; set; }
        }

        /// <summary>
        /// Main entry point to export dynamic analytics data into the official DSWD report format.
        /// Automatically detects whether to export as Excel (.xlsx) or CSV (.csv) from the file extension.
        /// </summary>
        public static void ExportAnalyticsReport(
            string filePath,
            MonthlyAnalyticsDto analyticsData,
            string municipality = "BINAN, LAGUNA",
            string region = "IV-A",
            string focalPersonName = "CHRISTINE R. RIZARRE, RSW",
            string password = null,
            string periodLabel = null)
        {
            if (string.IsNullOrWhiteSpace(focalPersonName))
                focalPersonName = "CHRISTINE R. RIZARRE, RSW";

            if (string.IsNullOrWhiteSpace(municipality))
                municipality = "BINAN, LAGUNA";

            if (string.IsNullOrWhiteSpace(region))
                region = "IV-A";

            int year = analyticsData?.Year > 0 ? analyticsData.Year : DateTime.Today.Year;

            if (string.IsNullOrWhiteSpace(periodLabel))
            {
                if (analyticsData != null && analyticsData.Month >= 1 && analyticsData.Month <= 12)
                {
                    string mName = new DateTime(year, analyticsData.Month, 1).ToString("MMMM").ToUpperInvariant();
                    periodLabel = $"{mName} {year}";
                }
                else
                {
                    periodLabel = $"{DateTime.Today:MMMM yyyy}".ToUpperInvariant();
                }
            }

            var metrics = ExtractReportMetrics(analyticsData, year);

            if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                ExportToCsv(filePath, metrics, municipality, region, focalPersonName, periodLabel, year);
            }
            else
            {
                ExportToExcel(filePath, metrics, municipality, region, focalPersonName, periodLabel, year, password);
            }
        }

        /// <summary>
        /// Generates the exact CSV format requested by DSWD.
        /// </summary>
        private static void ExportToCsv(
            string filePath,
            ReportMetrics m,
            string municipality,
            string region,
            string focalPersonName,
            string periodLabel,
            int year)
        {
            var sb = new StringBuilder();

            sb.AppendLine("LGU SUMMARY OF SOLO PARENTS,,");
            sb.AppendLine($"As of {periodLabel},,");
            sb.AppendLine(",,");
            sb.AppendLine($"City / Municipality / Province,\"{EscapeCsv(municipality)}\",");
            sb.AppendLine($"Region,{EscapeCsv(region)},");
            sb.AppendLine(",,");
            sb.AppendLine($"Number of Solo Parents served for {year},,{m.TotalServed}");

            // Age
            sb.AppendLine($"Age,19 years old and below,{m.Age19Below}");
            sb.AppendLine($",20-39 years old,{m.Age20To39}");
            sb.AppendLine($",40-59 years old,{m.Age40To59}");
            sb.AppendLine($",60 and above,{m.Age60Above}");

            // Sex
            sb.AppendLine($"Sex,Male,{m.MaleCount}");
            sb.AppendLine($",Female,{m.FemaleCount}");

            // Civil Status
            sb.AppendLine($"Civil Status,Single,{m.CivilSingle}");
            sb.AppendLine($",Married,{m.CivilMarried}");
            sb.AppendLine($",Widowed,{m.CivilWidowed}");
            sb.AppendLine($",Legally Separated / Annulled,{m.CivilSeparatedAnnulled}");

            // Employment Status
            sb.AppendLine($"Employment Status,Employed (public & private),{m.EmpEmployed}");
            sb.AppendLine($",Self employed,{m.EmpSelfEmployed}");
            sb.AppendLine($",Not employed,{m.EmpNotEmployed}");

            // Monthly Income
            sb.AppendLine($"Monthly Income,below minimum wage,{m.IncomeBelowMin}");
            sb.AppendLine($",Minimum wage +1 to Php 20833,{m.IncomeMinPlus}");
            sb.AppendLine($",Php 20834 and above,{m.IncomeAbove20833}");

            // No. of Children / Dependent
            sb.AppendLine($"No. of Children / Dependent,6 years old and below,{m.DepBelow6}");
            sb.AppendLine($",7 -22 years old,{m.Dep7To22}");
            sb.AppendLine($",22 years old and above,{m.DepAbove22}");

            // Category
            sb.AppendLine($"Category,a1. Consequence of rape,{m.CatA1}");
            sb.AppendLine($",a2. Widow/widower,{m.CatA2}");
            sb.AppendLine($",a3. Spouse of PDL,{m.CatA3}");
            sb.AppendLine($",a4. Spouse of PWD,{m.CatA4}");
            sb.AppendLine($",a5. Separated or de facto separated,{m.CatA5}");
            sb.AppendLine($",a6. Annulled ,{m.CatA6}");
            sb.AppendLine($",a7. Abandoned,{m.CatA7}");
            sb.AppendLine($",b. Spouse/Relative of OFW,{m.CatB}");
            sb.AppendLine($",c. Unmarried person,{m.CatC}");
            sb.AppendLine($",\"d. Legal Guardian, Adoptive or Foster Parent\",{m.CatD}");
            sb.AppendLine($",e. Relative,{m.CatE}");
            sb.AppendLine($",f. Pregnant woman,{m.CatF}");

            // Solo Parent Identification Card
            sb.AppendLine($"Solo Parent Identification Card,Newly issued SPIC,{m.SpicNew}");
            sb.AppendLine($",Renewed SPIC,{m.SpicRenewed}");
            sb.AppendLine($",Terminated SPIC,{m.SpicTerminated}");

            // Pantawid Beneficiary
            sb.AppendLine($"Pantawid Beneficiary,Yes,{m.PantawidYes}");
            sb.AppendLine($",No,{m.PantawidNo}");

            // Indigenous Person
            sb.AppendLine($"Indigenous Person,Yes,{m.IpYes}");
            sb.AppendLine($",No,{m.IpNo}");

            // LGBTQ+
            sb.AppendLine($"LGBTQ+,Yes,{m.LgbtYes}");
            sb.AppendLine($",No,{m.LgbtNo}");

            // Signature block
            sb.AppendLine(",,");
            sb.AppendLine("Submitted by:,,");
            sb.AppendLine(",,");
            sb.AppendLine($"\"{EscapeCsv(focalPersonName)}\",,");
            sb.AppendLine("Name of Solo Parent Focal Person:,,");
            sb.AppendLine($"\"Date of Submission: {DateTime.Today:MMMM d, yyyy}\",,");

            File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));
        }

        /// <summary>
        /// Generates the Excel workbook (.xlsx) containing both LGU Summary and Regional/Provincial Summary sheets.
        /// </summary>
        private static void ExportToExcel(
            string filePath,
            ReportMetrics m,
            string municipality,
            string region,
            string focalPersonName,
            string periodLabel,
            int year,
            string password)
        {
            ExcelPackage.License.SetNonCommercialOrganization("DSWD Solo Parents Program");
            using (var pkg = new ExcelPackage())
            {
                // Sheet 1: LGU Summary
                BuildLguSummarySheet(pkg, m, municipality, region, focalPersonName, periodLabel, year);

                // Sheet 2: Regional / Provincial Summary
                BuildRegionalProvincialSheet(pkg, m, municipality, region, focalPersonName, periodLabel, year);

                var fileInfo = new FileInfo(filePath);
                if (!string.IsNullOrEmpty(password))
                    pkg.SaveAs(fileInfo, password);
                else
                    pkg.SaveAs(fileInfo);
            }
        }

        private static void BuildLguSummarySheet(
            ExcelPackage pkg,
            ReportMetrics m,
            string municipality,
            string region,
            string focalPersonName,
            string periodLabel,
            int year)
        {
            var ws = pkg.Workbook.Worksheets.Add("LGU Summary");
            ws.Cells.Style.Font.Name = FontName;
            ws.Cells.Style.Font.Size = 10;
            ws.View.ShowGridLines = true;

            int row = 1;

            // Title
            ws.Cells[row, 1].Value = "LGU SUMMARY OF SOLO PARENTS";
            ws.Cells[row, 1].Style.Font.Size = 12;
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Color.SetColor(ReportColors.MaroonPrimary);
            row++;

            // Period
            ws.Cells[row, 1].Value = "As of " + periodLabel;
            ws.Cells[row, 1].Style.Font.Size = 10;
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Color.SetColor(ReportColors.TextMuted);
            row += 2; // Row 3 empty

            // Location
            ws.Cells[row, 1].Value = "City / Municipality / Province";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 2].Value = municipality;
            ws.Cells[row, 2].Style.Font.Bold = true;
            row++;

            ws.Cells[row, 1].Value = "Region";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 2].Value = region;
            ws.Cells[row, 2].Style.Font.Bold = true;
            row += 2; // Row 6 empty

            // Total Served
            int dataStartRow = row;
            ws.Cells[row, 1].Value = $"Number of Solo Parents served for {year}";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 3].Value = m.TotalServed;
            ws.Cells[row, 3].Style.Font.Bold = true;
            ws.Cells[row, 3].Style.Numberformat.Format = "#,##0";
            row++;

            // Age
            AddDataRow(ws, ref row, "Age", "19 years old and below", m.Age19Below);
            AddDataRow(ws, ref row, "", "20-39 years old", m.Age20To39);
            AddDataRow(ws, ref row, "", "40-59 years old", m.Age40To59);
            AddDataRow(ws, ref row, "", "60 and above", m.Age60Above);

            // Sex
            AddDataRow(ws, ref row, "Sex", "Male", m.MaleCount);
            AddDataRow(ws, ref row, "", "Female", m.FemaleCount);

            // Civil Status
            AddDataRow(ws, ref row, "Civil Status", "Single", m.CivilSingle);
            AddDataRow(ws, ref row, "", "Married", m.CivilMarried);
            AddDataRow(ws, ref row, "", "Widowed", m.CivilWidowed);
            AddDataRow(ws, ref row, "", "Legally Separated / Annulled", m.CivilSeparatedAnnulled);

            // Employment Status
            AddDataRow(ws, ref row, "Employment Status", "Employed (public & private)", m.EmpEmployed);
            AddDataRow(ws, ref row, "", "Self employed", m.EmpSelfEmployed);
            AddDataRow(ws, ref row, "", "Not employed", m.EmpNotEmployed);

            // Monthly Income
            AddDataRow(ws, ref row, "Monthly Income", "below minimum wage", m.IncomeBelowMin);
            AddDataRow(ws, ref row, "", "Minimum wage +1 to Php 20833", m.IncomeMinPlus);
            AddDataRow(ws, ref row, "", "Php 20834 and above", m.IncomeAbove20833);

            // Dependents
            AddDataRow(ws, ref row, "No. of Children / Dependent", "6 years old and below", m.DepBelow6);
            AddDataRow(ws, ref row, "", "7 -22 years old", m.Dep7To22);
            AddDataRow(ws, ref row, "", "22 years old and above", m.DepAbove22);

            // Category
            AddDataRow(ws, ref row, "Category", "a1. Consequence of rape", m.CatA1);
            AddDataRow(ws, ref row, "", "a2. Widow/widower", m.CatA2);
            AddDataRow(ws, ref row, "", "a3. Spouse of PDL", m.CatA3);
            AddDataRow(ws, ref row, "", "a4. Spouse of PWD", m.CatA4);
            AddDataRow(ws, ref row, "", "a5. Separated or de facto separated", m.CatA5);
            AddDataRow(ws, ref row, "", "a6. Annulled ", m.CatA6);
            AddDataRow(ws, ref row, "", "a7. Abandoned", m.CatA7);
            AddDataRow(ws, ref row, "", "b. Spouse/Relative of OFW", m.CatB);
            AddDataRow(ws, ref row, "", "c. Unmarried person", m.CatC);
            AddDataRow(ws, ref row, "", "d. Legal Guardian, Adoptive or Foster Parent", m.CatD);
            AddDataRow(ws, ref row, "", "e. Relative", m.CatE);
            AddDataRow(ws, ref row, "", "f. Pregnant woman", m.CatF);

            // SPIC
            AddDataRow(ws, ref row, "Solo Parent Identification Card", "Newly issued SPIC", m.SpicNew);
            AddDataRow(ws, ref row, "", "Renewed SPIC", m.SpicRenewed);
            AddDataRow(ws, ref row, "", "Terminated SPIC", m.SpicTerminated);

            // Pantawid
            AddDataRow(ws, ref row, "Pantawid Beneficiary", "Yes", m.PantawidYes);
            AddDataRow(ws, ref row, "", "No", m.PantawidNo);

            // Indigenous Person
            AddDataRow(ws, ref row, "Indigenous Person", "Yes", m.IpYes);
            AddDataRow(ws, ref row, "", "No", m.IpNo);

            // LGBTQ+
            AddDataRow(ws, ref row, "LGBTQ+", "Yes", m.LgbtYes);
            AddDataRow(ws, ref row, "", "No", m.LgbtNo);

            int dataEndRow = row - 1;

            // Apply light borders to table region
            for (int r = dataStartRow; r <= dataEndRow; r++)
            {
                for (int c = 1; c <= 3; c++)
                {
                    var cell = ws.Cells[r, c];
                    cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Top.Color.SetColor(ReportColors.BorderSubtle);
                    cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Bottom.Color.SetColor(ReportColors.BorderSubtle);
                    cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Left.Color.SetColor(ReportColors.BorderSubtle);
                    cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Right.Color.SetColor(ReportColors.BorderSubtle);
                }
            }

            // Footer / Signature Section
            row++; // Blank row
            ws.Cells[row, 1].Value = "Submitted by:";
            ws.Cells[row, 1].Style.Font.Italic = true;
            row += 2;

            ws.Cells[row, 1].Value = focalPersonName;
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Size = 11;
            row++;

            ws.Cells[row, 1].Value = "Name of Solo Parent Focal Person:";
            ws.Cells[row, 1].Style.Font.Color.SetColor(ReportColors.TextMuted);
            ws.Cells[row, 1].Style.Font.Size = 9.5f;
            row++;

            ws.Cells[row, 1].Value = $"Date of Submission: {DateTime.Today:MMMM d, yyyy}";
            ws.Cells[row, 1].Style.Font.Color.SetColor(ReportColors.TextMuted);
            ws.Cells[row, 1].Style.Font.Size = 9.5f;

            // Column Widths
            ws.Column(1).Width = 34;
            ws.Column(2).Width = 46;
            ws.Column(3).Width = 16;
        }

        private static void BuildRegionalProvincialSheet(
            ExcelPackage pkg,
            ReportMetrics m,
            string municipality,
            string region,
            string focalPersonName,
            string periodLabel,
            int year)
        {
            var ws = pkg.Workbook.Worksheets.Add("Regional-Provincial Summary");
            ws.Cells.Style.Font.Name = FontName;
            ws.Cells.Style.Font.Size = 10;
            ws.View.ShowGridLines = true;

            int row = 1;

            // Title
            ws.Cells[row, 1].Value = "REGIONAL / PROVINCIAL SUMMARY OF SOLO PARENTS";
            ws.Cells[row, 1].Style.Font.Size = 12;
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Color.SetColor(ReportColors.MaroonPrimary);
            row++;

            // Period
            ws.Cells[row, 1].Value = "As of " + periodLabel;
            ws.Cells[row, 1].Style.Font.Size = 10;
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Color.SetColor(ReportColors.TextMuted);
            row++;

            // Province
            ws.Cells[row, 5].Value = "LAGUNA";
            ws.Cells[row, 5].Style.Font.Bold = true;
            ws.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            row++;

            // Region/Province Header
            ws.Cells[row, 1].Value = "Region/Province";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 5].Value = "BINAN";
            ws.Cells[row, 5].Style.Font.Bold = true;
            ws.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[row, 6].Value = "LGU 2";
            ws.Cells[row, 6].Style.Font.Color.SetColor(ReportColors.TextMuted);
            ws.Cells[row, 7].Value = "LGU 3";
            ws.Cells[row, 7].Style.Font.Color.SetColor(ReportColors.TextMuted);
            row++;

            ws.Cells[row, 1].Value = "Number of LGUs submitted Summary";
            ws.Cells[row, 5].Value = 1;
            ws.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            row++;

            ws.Cells[row, 1].Value = "Total Number of LGUs in the Region/Province";
            ws.Cells[row, 5].Value = 1;
            ws.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            row += 2; // Row 7 empty

            // Total Served
            int dataStartRow = row;
            ws.Cells[row, 1].Value = $"Number of Solo Parents served for {year}";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 3].Value = m.TotalServed;
            ws.Cells[row, 3].Style.Font.Bold = true;
            ws.Cells[row, 3].Style.Numberformat.Format = "#,##0";
            ws.Cells[row, 5].Value = m.TotalServed;
            ws.Cells[row, 5].Style.Font.Bold = true;
            ws.Cells[row, 5].Style.Numberformat.Format = "#,##0";
            ws.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            row++;

            // Age
            AddRegionalDataRow(ws, ref row, "Age", "19 years old and below", m.Age19Below);
            AddRegionalDataRow(ws, ref row, "", "20-39 years old", m.Age20To39);
            AddRegionalDataRow(ws, ref row, "", "40-59 years old", m.Age40To59);
            AddRegionalDataRow(ws, ref row, "", "60 and above", m.Age60Above);

            // Sex
            AddRegionalDataRow(ws, ref row, "Sex", "Male", m.MaleCount);
            AddRegionalDataRow(ws, ref row, "", "Female", m.FemaleCount);

            // Civil Status
            AddRegionalDataRow(ws, ref row, "Civil Status", "Single", m.CivilSingle);
            AddRegionalDataRow(ws, ref row, "", "Married", m.CivilMarried);
            AddRegionalDataRow(ws, ref row, "", "Widowed", m.CivilWidowed);
            AddRegionalDataRow(ws, ref row, "", "Legally Separated / Annulled", m.CivilSeparatedAnnulled);

            // Employment Status
            AddRegionalDataRow(ws, ref row, "Employment Status", "Employed (public & private)", m.EmpEmployed);
            AddRegionalDataRow(ws, ref row, "", "Self employed", m.EmpSelfEmployed);
            AddRegionalDataRow(ws, ref row, "", "Not employed", m.EmpNotEmployed);

            // Monthly Income
            AddRegionalDataRow(ws, ref row, "Monthly Income", "below minimum wage", m.IncomeBelowMin);
            AddRegionalDataRow(ws, ref row, "", "Minimum wage +1 to Php 20833", m.IncomeMinPlus);
            AddRegionalDataRow(ws, ref row, "", "Php 20834 and above", m.IncomeAbove20833);

            // Dependents
            AddRegionalDataRow(ws, ref row, "No. of Children / Dependent", "6 years old and below", m.DepBelow6);
            AddRegionalDataRow(ws, ref row, "", "7 -22 years old", m.Dep7To22);
            AddRegionalDataRow(ws, ref row, "", "22 years old and above", m.DepAbove22);

            // Category
            AddRegionalDataRow(ws, ref row, "Category", "a1. Consequence of rape", m.CatA1);
            AddRegionalDataRow(ws, ref row, "", "a2. Widow/widower", m.CatA2);
            AddRegionalDataRow(ws, ref row, "", "a3. Spouse of PDL", m.CatA3);
            AddRegionalDataRow(ws, ref row, "", "a4. Spouse of PWD", m.CatA4);
            AddRegionalDataRow(ws, ref row, "", "a5. Separated or de facto separated", m.CatA5);
            AddRegionalDataRow(ws, ref row, "", "a6. Annulled ", m.CatA6);
            AddRegionalDataRow(ws, ref row, "", "a7. Abandoned", m.CatA7);
            AddRegionalDataRow(ws, ref row, "", "b. Spouse/Relative of OFW", m.CatB);
            AddRegionalDataRow(ws, ref row, "", "c. Unmarried person", m.CatC);
            AddRegionalDataRow(ws, ref row, "", "d. Legal Guardian, Adoptive or Foster Parent", m.CatD);
            AddRegionalDataRow(ws, ref row, "", "e. Relative", m.CatE);
            AddRegionalDataRow(ws, ref row, "", "f. Pregnant woman", m.CatF);

            // SPIC
            AddRegionalDataRow(ws, ref row, "Solo Parent Identification Card", "Newly issued SPIC", m.SpicNew);
            AddRegionalDataRow(ws, ref row, "", "Renewed SPIC", m.SpicRenewed);
            AddRegionalDataRow(ws, ref row, "", "Terminated SPIC", m.SpicTerminated);

            // Pantawid
            AddRegionalDataRow(ws, ref row, "Pantawid Beneficiary", "Yes", m.PantawidYes);
            AddRegionalDataRow(ws, ref row, "", "No", m.PantawidNo);

            // Indigenous Person
            AddRegionalDataRow(ws, ref row, "Indigenous Person", "Yes", m.IpYes);
            AddRegionalDataRow(ws, ref row, "", "No", m.IpNo);

            // LGBTQ+
            AddRegionalDataRow(ws, ref row, "LGBTQ+", "Yes", m.LgbtYes);
            AddRegionalDataRow(ws, ref row, "", "No", m.LgbtNo);

            int dataEndRow = row - 1;

            // Apply light borders to table region
            for (int r = dataStartRow; r <= dataEndRow; r++)
            {
                for (int c = 1; c <= 5; c++)
                {
                    var cell = ws.Cells[r, c];
                    cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Top.Color.SetColor(ReportColors.BorderSubtle);
                    cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Bottom.Color.SetColor(ReportColors.BorderSubtle);
                    cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Left.Color.SetColor(ReportColors.BorderSubtle);
                    cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Right.Color.SetColor(ReportColors.BorderSubtle);
                }
            }

            // Footer / Signature Section
            row++;
            ws.Cells[row, 1].Value = "Submitted by:";
            ws.Cells[row, 1].Style.Font.Italic = true;
            row += 2;

            ws.Cells[row, 1].Value = focalPersonName;
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Size = 11;
            row++;

            ws.Cells[row, 1].Value = "Name of Solo Parent Focal Person:";
            ws.Cells[row, 1].Style.Font.Color.SetColor(ReportColors.TextMuted);
            ws.Cells[row, 1].Style.Font.Size = 9.5f;
            row++;

            ws.Cells[row, 1].Value = $"Date of Submission: {DateTime.Today:MMMM d, yyyy}";
            ws.Cells[row, 1].Style.Font.Color.SetColor(ReportColors.TextMuted);
            ws.Cells[row, 1].Style.Font.Size = 9.5f;

            // Column Widths
            ws.Column(1).Width = 34;
            ws.Column(2).Width = 46;
            ws.Column(3).Width = 14;
            ws.Column(4).Width = 10;
            ws.Column(5).Width = 16;
            ws.Column(6).Width = 14;
            ws.Column(7).Width = 14;
        }

        private static void AddDataRow(ExcelWorksheet ws, ref int row, string category, string subCategory, int count)
        {
            if (!string.IsNullOrWhiteSpace(category))
            {
                ws.Cells[row, 1].Value = category;
                ws.Cells[row, 1].Style.Font.Bold = true;
            }

            ws.Cells[row, 2].Value = subCategory;

            var valCell = ws.Cells[row, 3];
            valCell.Value = count;
            valCell.Style.Numberformat.Format = "#,##0";
            valCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            row++;
        }

        private static void AddRegionalDataRow(ExcelWorksheet ws, ref int row, string category, string subCategory, int count)
        {
            if (!string.IsNullOrWhiteSpace(category))
            {
                ws.Cells[row, 1].Value = category;
                ws.Cells[row, 1].Style.Font.Bold = true;
            }

            ws.Cells[row, 2].Value = subCategory;

            var valCell = ws.Cells[row, 5];
            valCell.Value = count;
            valCell.Style.Numberformat.Format = "#,##0";
            valCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            row++;
        }

        private static ReportMetrics ExtractReportMetrics(MonthlyAnalyticsDto data, int year)
        {
            var m = new ReportMetrics();
            if (data == null) return m;

            // Age
            m.Age19Below = GetCount(data.AgeBrackets, "19_AND_BELOW", "19_and_below", "<=19", "19 years old and below", "19 and below");
            m.Age20To39 = GetCount(data.AgeBrackets, "20_39", "20-39", "20-39 years old", "20_to_39");
            m.Age40To59 = GetCount(data.AgeBrackets, "40_59", "40-59", "40-59 years old", "40_to_59");
            m.Age60Above = GetCount(data.AgeBrackets, "60_AND_ABOVE", "60_and_above", ">=60", "60 and above");

            // Sex
            m.MaleCount = GetCount(data.Sex, "Male", "M", "male");
            m.FemaleCount = GetCount(data.Sex, "Female", "F", "female");

            // Total Served
            int computedTotal = m.MaleCount + m.FemaleCount;
            if (computedTotal == 0)
                computedTotal = m.Age19Below + m.Age20To39 + m.Age40To59 + m.Age60Above;
            m.TotalServed = data.TotalSoloParents > 0 ? data.TotalSoloParents : computedTotal;

            // Civil Status
            m.CivilSingle = GetCount(data.CivilStatus, "Single", "single");
            m.CivilMarried = GetCount(data.CivilStatus, "Married", "married");
            m.CivilWidowed = GetCount(data.CivilStatus, "Widowed", "widowed");
            int sep = GetCount(data.CivilStatus, "Separated", "separated", "Legally Separated");
            int ann = GetCount(data.CivilStatus, "Annulled", "annulled");
            int sepAnn = GetCount(data.CivilStatus, "Legally Separated / Annulled", "Separated / Annulled");
            m.CivilSeparatedAnnulled = sepAnn > 0 ? sepAnn : (sep + ann);

            // Employment Status
            m.EmpEmployed = GetCount(data.EmploymentStatus, "employed", "Employed", "Employed (public & private)");
            m.EmpSelfEmployed = GetCount(data.EmploymentStatus, "self_employed", "Self-Employed", "Self Employed", "self employed");
            m.EmpNotEmployed = GetCount(data.EmploymentStatus, "not_employed", "Not-Employed", "Not Employed", "not employed", "Unemployed", "unemployed");

            // Monthly Income
            m.IncomeBelowMin = GetCount(data.MonthlyIncomeBrackets, "BELOW_MINIMUM_WAGE", "below minimum wage", "below_minimum_wage", "<min");
            m.IncomeMinPlus = GetCount(data.MonthlyIncomeBrackets, "MIN_WAGE_PLUS1_TO_20833", "MINIMUM_WAGE_PLUS1_TO_20833", "Minimum wage +1 to Php 20833", "min_to_20833");
            m.IncomeAbove20833 = GetCount(data.MonthlyIncomeBrackets, "20834_AND_ABOVE", "Php 20834 and above", "20834_and_above", ">20833");

            // Dependents
            m.DepBelow6 = GetCount(data.DependentsAgeBrackets, "6_AND_BELOW", "6_and_below", "6 years old and below", "<=6");
            m.Dep7To22 = GetCount(data.DependentsAgeBrackets, "7_TO_22", "7_22", "7-22", "7 -22 years old", "7-22 years old");
            m.DepAbove22 = GetCount(data.DependentsAgeBrackets, "22_AND_ABOVE", "22_and_above", "22 years old and above", ">22");

            // Categories
            m.CatA1 = GetCount(data.Categories, "A1", "a1", "a1. Consequence of rape");
            m.CatA2 = GetCount(data.Categories, "A2", "a2", "a2. Widow/widower");
            m.CatA3 = GetCount(data.Categories, "A3", "a3", "a3. Spouse of PDL");
            m.CatA4 = GetCount(data.Categories, "A4", "a4", "a4. Spouse of PWD");
            m.CatA5 = GetCount(data.Categories, "A5", "a5", "a5. Separated or de facto separated");
            m.CatA6 = GetCount(data.Categories, "A6", "a6", "a6. Annulled ", "a6. Annulled");
            m.CatA7 = GetCount(data.Categories, "A7", "a7", "a7. Abandoned");
            m.CatB = GetCount(data.Categories, "B", "b", "b. Spouse/Relative of OFW");
            m.CatC = GetCount(data.Categories, "C", "c", "c. Unmarried person");
            m.CatD = GetCount(data.Categories, "D", "d", "d. Legal Guardian, Adoptive or Foster Parent");
            m.CatE = GetCount(data.Categories, "E", "e", "e. Relative");
            m.CatF = GetCount(data.Categories, "F", "f", "f. Pregnant woman");

            // SPIC
            m.SpicNew = data.NewRegistrations;
            m.SpicRenewed = data.Renewals;
            m.SpicTerminated = data.InactiveSoloParents;

            // Pantawid
            m.PantawidYes = GetCount(data.PantawidBeneficiary, "Yes", "Beneficiary", "yes", "true", "4Ps");
            m.PantawidNo = GetCount(data.PantawidBeneficiary, "No", "Non-Beneficiary", "no", "false");
            if (m.PantawidNo == 0 && m.TotalServed > m.PantawidYes)
                m.PantawidNo = m.TotalServed - m.PantawidYes;

            // Indigenous
            m.IpYes = GetCount(data.Indigenous, "Yes", "Indigenous", "yes", "true", "IP");
            m.IpNo = GetCount(data.Indigenous, "No", "Non-Indigenous", "no", "false");
            if (m.IpNo == 0 && m.TotalServed > m.IpYes)
                m.IpNo = m.TotalServed - m.IpYes;

            // LGBT
            m.LgbtYes = GetCount(data.Lgbt, "Yes", "LGBT", "yes", "true", "LGBTQ+");
            m.LgbtNo = GetCount(data.Lgbt, "No", "Non-LGBT", "no", "false");
            if (m.LgbtNo == 0 && m.TotalServed > m.LgbtYes)
                m.LgbtNo = m.TotalServed - m.LgbtYes;

            return m;
        }

        private static int GetCount(Dictionary<string, int> dict, params string[] candidateKeys)
        {
            if (dict == null || dict.Count == 0) return 0;
            foreach (var key in candidateKeys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (dict.TryGetValue(key, out int val)) return val;

                var match = dict.FirstOrDefault(kvp => string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(match.Key)) return match.Value;
            }
            return 0;
        }

        private static string EscapeCsv(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\"", "\"\"");
        }
    }
}
