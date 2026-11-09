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
        private const string FontName = "Arial";

        private class CategoryBlock
        {
            public string CategoryName { get; set; }
            public List<(string Label, int? Count)> Items { get; set; } = new List<(string Label, int? Count)>();
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

            public int? SpicNew { get; set; }
            public int? SpicRenewed { get; set; }
            public int? SpicTerminated { get; set; }

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

            // Solo Parent Identification Card (Out of scope - left empty)
            sb.AppendLine("Solo Parent Identification Card,Newly issued SPIC,");
            sb.AppendLine(",Renewed SPIC,");
            sb.AppendLine(",Terminated SPIC,");

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
            ws.Cells.Style.Font.Size = 12;
            ws.View.ShowGridLines = true;

            // Row 1: Document Header Title (Merged A1:C1)
            ws.Row(1).Height = 15.0;
            ws.Cells[1, 1, 1, 3].Merge = true;
            ws.Cells[1, 1].Value = "LGU SUMMARY OF SOLO PARENTS";
            ws.Cells[1, 1].Style.Font.Name = FontName;
            ws.Cells[1, 1].Style.Font.Size = 12;
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[1, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            // Row 2: Subtitle Period (Merged A2:C2)
            ws.Row(2).Height = 15.0;
            ws.Cells[2, 1, 2, 3].Merge = true;
            ws.Cells[2, 1].Value = "As of " + periodLabel;
            ws.Cells[2, 1].Style.Font.Name = FontName;
            ws.Cells[2, 1].Style.Font.Size = 12;
            ws.Cells[2, 1].Style.Font.Bold = true;
            ws.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[2, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            // Row 3: Blank
            ws.Row(3).Height = 11.25;

            // Row 4: Municipality Location
            ws.Row(4).Height = 18.75;
            ws.Cells[4, 1].Value = "City / Municipality / Province";
            ws.Cells[4, 1].Style.Font.Name = FontName;
            ws.Cells[4, 1].Style.Font.Size = 12;
            ws.Cells[4, 1].Style.Font.Bold = true;
            ws.Cells[4, 2].Value = municipality;
            ws.Cells[4, 2].Style.Font.Name = FontName;
            ws.Cells[4, 2].Style.Font.Size = 12;
            ws.Cells[4, 2].Style.Font.Bold = false;
            ws.Cells[4, 2].Style.WrapText = true;

            // Row 5: Region
            ws.Row(5).Height = 21.75;
            ws.Cells[5, 1].Value = "Region";
            ws.Cells[5, 1].Style.Font.Name = FontName;
            ws.Cells[5, 1].Style.Font.Size = 12;
            ws.Cells[5, 1].Style.Font.Bold = true;
            ws.Cells[5, 2].Value = region;
            ws.Cells[5, 2].Style.Font.Name = FontName;
            ws.Cells[5, 2].Style.Font.Size = 12;
            ws.Cells[5, 2].Style.Font.Bold = false;
            ws.Cells[5, 2].Style.WrapText = true;

            // Row 6: Blank
            ws.Row(6).Height = 12.75;

            // Row 7: Total Served Header (Merged A7:B7 with C7 count)
            ws.Row(7).Height = 17.25;
            ws.Cells[7, 1, 7, 2].Merge = true;
            ws.Cells[7, 1].Value = $"Number of Solo Parents served for {year}";
            ws.Cells[7, 1].Style.Font.Name = FontName;
            ws.Cells[7, 1].Style.Font.Size = 12;
            ws.Cells[7, 1].Style.Font.Bold = true;
            ws.Cells[7, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ws.Cells[7, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            ws.Cells[7, 1].Style.WrapText = true;
            ApplyBoxBorder(ws, 7, 1, 7, 2);

            ws.Cells[7, 3].Value = m.TotalServed;
            ws.Cells[7, 3].Style.Font.Name = FontName;
            ws.Cells[7, 3].Style.Font.Size = 12;
            ws.Cells[7, 3].Style.Font.Bold = false;
            ws.Cells[7, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[7, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells[7, 3].Style.Numberformat.Format = "#,##0";
            ApplyCellBorder(ws.Cells[7, 3]);

            // Rows 8-47: Exact 11 category blocks with vertically merged Column A
            int currentRow = 8;
            var blocks = GetCategoryBlocks(m);
            foreach (var block in blocks)
            {
                int startRow = currentRow;
                int endRow = startRow + block.Items.Count - 1;

                // Vertically merged Category cell in Column A
                var catRange = ws.Cells[startRow, 1, endRow, 1];
                catRange.Merge = true;
                catRange.Value = block.CategoryName;
                catRange.Style.Font.Name = FontName;
                catRange.Style.Font.Size = 12;
                catRange.Style.Font.Bold = true;
                catRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                catRange.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                catRange.Style.WrapText = true;
                ApplyBoxBorder(ws, startRow, 1, endRow, 1);

                // Subcategories and value rows
                for (int i = 0; i < block.Items.Count; i++)
                {
                    int r = startRow + i;
                    ws.Row(r).Height = 15.0;

                    var item = block.Items[i];

                    // Column B: Subcategory label
                    var bCell = ws.Cells[r, 2];
                    bCell.Value = item.Label;
                    bCell.Style.Font.Name = FontName;
                    bCell.Style.Font.Size = 12;
                    bCell.Style.Font.Bold = true;
                    bCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    bCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    bCell.Style.WrapText = true;
                    ApplyCellBorder(bCell);

                    // Column C: Count
                    var cCell = ws.Cells[r, 3];
                    if (item.Count.HasValue)
                    {
                        cCell.Value = item.Count.Value;
                        cCell.Style.Numberformat.Format = "#,##0";
                    }
                    else
                    {
                        cCell.Value = null;
                    }
                    cCell.Style.Font.Name = FontName;
                    cCell.Style.Font.Size = 12;
                    cCell.Style.Font.Bold = false;
                    cCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    cCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    ApplyCellBorder(cCell);
                }

                currentRow = endRow + 1;
            }

            // Footer / Signatures
            ws.Row(48).Height = 12.75; // Blank row 48

            ws.Row(49).Height = 12.75;
            ws.Cells[49, 1].Value = "Submitted by:";
            ws.Cells[49, 1].Style.Font.Name = FontName;
            ws.Cells[49, 1].Style.Font.Size = 12;
            ws.Cells[49, 1].Style.Font.Bold = true;

            ws.Row(50).Height = 12.75; // Blank row 50

            ws.Row(51).Height = 19.5;
            ws.Cells[51, 1, 51, 2].Merge = true;
            ws.Cells[51, 1].Value = focalPersonName;
            ws.Cells[51, 1].Style.Font.Name = FontName;
            ws.Cells[51, 1].Style.Font.Size = 12;
            ws.Cells[51, 1].Style.Font.Bold = true;
            ws.Cells[51, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

            ws.Row(52).Height = 18.0;
            ws.Cells[52, 1, 52, 2].Merge = true;
            ws.Cells[52, 1].Value = "Name of Solo Parent Focal Person:";
            ws.Cells[52, 1].Style.Font.Name = FontName;
            ws.Cells[52, 1].Style.Font.Size = 12;
            ws.Cells[52, 1].Style.Font.Bold = false;
            ws.Cells[52, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

            ws.Row(53).Height = 21.75;
            ws.Cells[53, 1, 53, 2].Merge = true;
            ws.Cells[53, 1].Value = $"Date of Submission: {DateTime.Today:MMMM d, yyyy}";
            ws.Cells[53, 1].Style.Font.Name = FontName;
            ws.Cells[53, 1].Style.Font.Size = 12;
            ws.Cells[53, 1].Style.Font.Bold = false;
            ws.Cells[53, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

            // Column Widths matching official template
            ws.Column(1).Width = 36.45;
            ws.Column(2).Width = 33.18;
            ws.Column(3).Width = 18.54;
            ws.Column(4).Width = 9.18;
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
            var ws = pkg.Workbook.Worksheets.Add("ProvinceRegional Summary");
            ws.Cells.Style.Font.Name = FontName;
            ws.Cells.Style.Font.Size = 10;
            ws.View.ShowGridLines = true;

            string cityName = municipality;
            string provinceName = "LAGUNA";
            if (!string.IsNullOrWhiteSpace(municipality))
            {
                var parts = municipality.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    cityName = parts[0].Trim().ToUpperInvariant();
                    provinceName = parts[1].Trim().ToUpperInvariant();
                }
                else
                {
                    cityName = municipality.Trim().ToUpperInvariant();
                }
            }

            // Row 1: Document Header Title (Merged A1:C1)
            ws.Row(1).Height = 12.75;
            ws.Cells[1, 1, 1, 3].Merge = true;
            ws.Cells[1, 1].Value = "REGIONAL / PROVINCIAL SUMMARY OF SOLO PARENTS";
            ws.Cells[1, 1].Style.Font.Name = FontName;
            ws.Cells[1, 1].Style.Font.Size = 11;
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[1, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            // Row 2: Subtitle Period (Merged A2:C2)
            ws.Row(2).Height = 12.75;
            ws.Cells[2, 1, 2, 3].Merge = true;
            ws.Cells[2, 1].Value = "As of " + periodLabel;
            ws.Cells[2, 1].Style.Font.Name = FontName;
            ws.Cells[2, 1].Style.Font.Size = 11;
            ws.Cells[2, 1].Style.Font.Bold = true;
            ws.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[2, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            // Row 3: Province
            ws.Row(3).Height = 11.25;
            ws.Cells[3, 5].Value = provinceName;
            ws.Cells[3, 5].Style.Font.Name = FontName;
            ws.Cells[3, 5].Style.Font.Size = 10;
            ws.Cells[3, 5].Style.Font.Bold = false;
            ws.Cells[3, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Rows 4-6: Header with rotated LGU names
            ws.Row(4).Height = 15.0;
            ws.Row(5).Height = 15.0;
            ws.Row(6).Height = 15.0;

            // Row 4: Region/Province
            ws.Cells[4, 1, 4, 3].Merge = true;
            ws.Cells[4, 1].Value = "Region/Province";
            ws.Cells[4, 1].Style.Font.Name = FontName;
            ws.Cells[4, 1].Style.Font.Size = 10;
            ws.Cells[4, 1].Style.Font.Bold = false;
            ApplyBoxBorder(ws, 4, 1, 4, 3);

            // Columns 5+: LGU headers rotated 90 degrees
            ws.Cells[4, 5, 6, 5].Merge = true;
            ws.Cells[4, 5].Value = cityName;
            ws.Cells[4, 5].Style.Font.Name = FontName;
            ws.Cells[4, 5].Style.Font.Size = 8;
            ws.Cells[4, 5].Style.Font.Bold = false;
            ws.Cells[4, 5].Style.TextRotation = 90;
            ws.Cells[4, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[4, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ApplyBoxBorder(ws, 4, 5, 6, 5);

            ws.Cells[4, 6, 6, 6].Merge = true;
            ws.Cells[4, 6].Value = "LGU 2";
            ws.Cells[4, 6].Style.Font.Name = FontName;
            ws.Cells[4, 6].Style.Font.Size = 8;
            ws.Cells[4, 6].Style.Font.Bold = false;
            ws.Cells[4, 6].Style.TextRotation = 90;
            ws.Cells[4, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[4, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ApplyBoxBorder(ws, 4, 6, 6, 6);

            ws.Cells[4, 7, 6, 7].Merge = true;
            ws.Cells[4, 7].Value = "LGU 3";
            ws.Cells[4, 7].Style.Font.Name = FontName;
            ws.Cells[4, 7].Style.Font.Size = 8;
            ws.Cells[4, 7].Style.Font.Bold = false;
            ws.Cells[4, 7].Style.TextRotation = 90;
            ws.Cells[4, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[4, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ApplyBoxBorder(ws, 4, 7, 6, 7);

            // Row 5: Number of LGUs submitted
            ws.Cells[5, 1, 5, 3].Merge = true;
            ws.Cells[5, 1].Value = "Number of LGUs submitted Summary";
            ws.Cells[5, 1].Style.Font.Name = FontName;
            ws.Cells[5, 1].Style.Font.Size = 10;
            ws.Cells[5, 1].Style.Font.Bold = false;
            ApplyBoxBorder(ws, 5, 1, 5, 3);

            // Row 6: Total LGUs
            ws.Cells[6, 1, 6, 3].Merge = true;
            ws.Cells[6, 1].Value = "Total Number of LGUs in the Region/Province";
            ws.Cells[6, 1].Style.Font.Name = FontName;
            ws.Cells[6, 1].Style.Font.Size = 10;
            ws.Cells[6, 1].Style.Font.Bold = false;
            ApplyBoxBorder(ws, 6, 1, 6, 3);

            // Row 7: Blank
            ws.Row(7).Height = 12.75;

            // Row 8: Number of Solo Parents served for year
            ws.Row(8).Height = 14.25;
            ws.Cells[8, 1, 8, 2].Merge = true;
            ws.Cells[8, 1].Value = $"Number of Solo Parents served for {year}";
            ws.Cells[8, 1].Style.Font.Name = FontName;
            ws.Cells[8, 1].Style.Font.Size = 11;
            ws.Cells[8, 1].Style.Font.Bold = true;
            ws.Cells[8, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            ApplyBoxBorder(ws, 8, 1, 8, 2);

            ws.Cells[8, 3].Value = m.TotalServed;
            ws.Cells[8, 3].Style.Font.Name = FontName;
            ws.Cells[8, 3].Style.Font.Size = 11;
            ws.Cells[8, 3].Style.Font.Bold = false;
            ws.Cells[8, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[8, 3].Style.Numberformat.Format = "#,##0";
            ApplyCellBorder(ws.Cells[8, 3]);

            ws.Cells[8, 5].Value = m.TotalServed;
            ws.Cells[8, 5].Style.Font.Name = FontName;
            ws.Cells[8, 5].Style.Font.Size = 11;
            ws.Cells[8, 5].Style.Font.Bold = false;
            ws.Cells[8, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[8, 5].Style.Numberformat.Format = "#,##0";
            ApplyCellBorder(ws.Cells[8, 5]);

            // Rows 9-48: 11 category blocks matching template
            int currentRow = 9;
            var blocks = GetCategoryBlocks(m);
            foreach (var block in blocks)
            {
                int startRow = currentRow;
                int endRow = startRow + block.Items.Count - 1;

                bool isBoldCategory = (block.CategoryName == "Employment Status" ||
                                       block.CategoryName == "Monthly Income" ||
                                       block.CategoryName == "Category" ||
                                       block.CategoryName == "Solo Parent Identification Card");

                // Merged Category column A
                var catRange = ws.Cells[startRow, 1, endRow, 1];
                catRange.Merge = true;
                catRange.Value = block.CategoryName;
                catRange.Style.Font.Name = FontName;
                catRange.Style.Font.Size = 8;
                catRange.Style.Font.Bold = isBoldCategory;
                catRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                catRange.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                catRange.Style.WrapText = true;
                ApplyBoxBorder(ws, startRow, 1, endRow, 1);

                for (int i = 0; i < block.Items.Count; i++)
                {
                    int r = startRow + i;
                    ws.Row(r).Height = 15.0;

                    var item = block.Items[i];

                    // Column B: Subcategory
                    var bCell = ws.Cells[r, 2];
                    bCell.Value = item.Label;
                    bCell.Style.Font.Name = FontName;
                    bCell.Style.Font.Size = 8;
                    bCell.Style.Font.Bold = isBoldCategory;
                    bCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    bCell.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    bCell.Style.WrapText = true;
                    ApplyCellBorder(bCell);

                    // Column C: Total
                    var cCell = ws.Cells[r, 3];
                    if (item.Count.HasValue)
                    {
                        cCell.Value = item.Count.Value;
                        cCell.Style.Numberformat.Format = "#,##0";
                    }
                    else
                    {
                        cCell.Value = null;
                    }
                    cCell.Style.Font.Name = FontName;
                    cCell.Style.Font.Size = 11;
                    cCell.Style.Font.Bold = false;
                    cCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    cCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    ApplyCellBorder(cCell);

                    // Column E: City count
                    var eCell = ws.Cells[r, 5];
                    if (item.Count.HasValue)
                    {
                        eCell.Value = item.Count.Value;
                        eCell.Style.Numberformat.Format = "#,##0";
                    }
                    else
                    {
                        eCell.Value = null;
                    }
                    eCell.Style.Font.Name = FontName;
                    eCell.Style.Font.Size = 11;
                    eCell.Style.Font.Bold = false;
                    eCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    eCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    ApplyCellBorder(eCell);
                }

                currentRow = endRow + 1;
            }

            // Footer
            ws.Row(49).Height = 12.75; // Blank

            ws.Row(50).Height = 12.75;
            ws.Cells[50, 1].Value = "Submitted by:";
            ws.Cells[50, 1].Style.Font.Name = FontName;
            ws.Cells[50, 1].Style.Font.Size = 10;
            ws.Cells[50, 1].Style.Font.Bold = false;

            ws.Row(51).Height = 12.75; // Blank
            ws.Row(52).Height = 12.75; // Blank

            ws.Row(53).Height = 12.75;
            ws.Cells[53, 1, 53, 2].Merge = true;
            ws.Cells[53, 1].Value = focalPersonName;
            ws.Cells[53, 1].Style.Font.Name = FontName;
            ws.Cells[53, 1].Style.Font.Size = 10;
            ws.Cells[53, 1].Style.Font.Bold = true;
            ws.Cells[53, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

            ws.Row(54).Height = 12.75;
            ws.Cells[54, 1, 54, 2].Merge = true;
            ws.Cells[54, 1].Value = "Name of Solo Parent Focal Person:";
            ws.Cells[54, 1].Style.Font.Name = FontName;
            ws.Cells[54, 1].Style.Font.Size = 10;
            ws.Cells[54, 1].Style.Font.Bold = false;
            ws.Cells[54, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

            ws.Row(55).Height = 12.75;
            ws.Cells[55, 1, 55, 2].Merge = true;
            ws.Cells[55, 1].Value = $"Date of Submission: {DateTime.Today:MMMM d, yyyy}";
            ws.Cells[55, 1].Style.Font.Name = FontName;
            ws.Cells[55, 1].Style.Font.Size = 10;
            ws.Cells[55, 1].Style.Font.Bold = false;
            ws.Cells[55, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

            // Column Widths
            ws.Column(1).Width = 22.73;
            ws.Column(2).Width = 38.45;
            ws.Column(3).Width = 20.73;
            ws.Column(4).Width = 9.18;
            ws.Column(5).Width = 10.0;
            ws.Column(6).Width = 8.5;
            ws.Column(7).Width = 8.5;
        }

        private static void ApplyCellBorder(ExcelRange cell)
        {
            cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            cell.Style.Border.Top.Color.SetColor(Color.Black);
            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            cell.Style.Border.Bottom.Color.SetColor(Color.Black);
            cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            cell.Style.Border.Left.Color.SetColor(Color.Black);
            cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            cell.Style.Border.Right.Color.SetColor(Color.Black);
        }

        private static void ApplyBoxBorder(ExcelWorksheet ws, int fromRow, int fromCol, int toRow, int toCol)
        {
            for (int r = fromRow; r <= toRow; r++)
            {
                for (int c = fromCol; c <= toCol; c++)
                {
                    var cell = ws.Cells[r, c];
                    if (r == fromRow)
                    {
                        cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Top.Color.SetColor(Color.Black);
                    }
                    if (r == toRow)
                    {
                        cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Bottom.Color.SetColor(Color.Black);
                    }
                    if (c == fromCol)
                    {
                        cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Left.Color.SetColor(Color.Black);
                    }
                    if (c == toCol)
                    {
                        cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Right.Color.SetColor(Color.Black);
                    }
                }
            }
        }

        private static List<CategoryBlock> GetCategoryBlocks(ReportMetrics m)
        {
            return new List<CategoryBlock>
            {
                new CategoryBlock
                {
                    CategoryName = "Age",
                    Items = new List<(string, int?)>
                    {
                        ("19 years old and below", m.Age19Below),
                        ("20-39 years old", m.Age20To39),
                        ("40-59 years old", m.Age40To59),
                        ("60 and above", m.Age60Above)
                    }
                },
                new CategoryBlock
                {
                    CategoryName = "Sex",
                    Items = new List<(string, int?)>
                    {
                        ("Male", m.MaleCount),
                        ("Female", m.FemaleCount)
                    }
                },
                new CategoryBlock
                {
                    CategoryName = "Civil Status",
                    Items = new List<(string, int?)>
                    {
                        ("Single", m.CivilSingle),
                        ("Married", m.CivilMarried),
                        ("Widowed", m.CivilWidowed),
                        ("Legally Separated / Annulled", m.CivilSeparatedAnnulled)
                    }
                },
                new CategoryBlock
                {
                    CategoryName = "Employment Status",
                    Items = new List<(string, int?)>
                    {
                        ("Employed (public & private)", m.EmpEmployed),
                        ("Self employed", m.EmpSelfEmployed),
                        ("Not employed", m.EmpNotEmployed)
                    }
                },
                new CategoryBlock
                {
                    CategoryName = "Monthly Income",
                    Items = new List<(string, int?)>
                    {
                        ("below minimum wage", m.IncomeBelowMin),
                        ("Minimum wage +1 to Php 20833", m.IncomeMinPlus),
                        ("Php 20834 and above", m.IncomeAbove20833)
                    }
                },
                new CategoryBlock
                {
                    CategoryName = "No. of Children / Dependent",
                    Items = new List<(string, int?)>
                    {
                        ("6 years old and below", m.DepBelow6),
                        ("7 -22 years old", m.Dep7To22),
                        ("22 years old and above", m.DepAbove22)
                    }
                },
                new CategoryBlock
                {
                    CategoryName = "Category",
                    Items = new List<(string, int?)>
                    {
                        ("a1. Consequence of rape", m.CatA1),
                        ("a2. Widow/widower", m.CatA2),
                        ("a3. Spouse of PDL", m.CatA3),
                        ("a4. Spouse of PWD", m.CatA4),
                        ("a5. Separated or de facto separated", m.CatA5),
                        ("a6. Annulled ", m.CatA6),
                        ("a7. Abandoned", m.CatA7),
                        ("b. Spouse/Relative of OFW", m.CatB),
                        ("c. Unmarried person", m.CatC),
                        ("d. Legal Guardian, Adoptive or Foster Parent", m.CatD),
                        ("e. Relative", m.CatE),
                        ("f. Pregnant woman", m.CatF)
                    }
                },
                new CategoryBlock
                {
                    CategoryName = "Solo Parent Identification Card",
                    Items = new List<(string, int?)>
                    {
                        ("Newly issued SPIC", null),
                        ("Renewed SPIC", null),
                        ("Terminated SPIC", null)
                    }
                },
                new CategoryBlock
                {
                    CategoryName = "Pantawid Beneficiary",
                    Items = new List<(string, int?)>
                    {
                        ("Yes", m.PantawidYes),
                        ("No", m.PantawidNo)
                    }
                },
                new CategoryBlock
                {
                    CategoryName = "Indigenous Person",
                    Items = new List<(string, int?)>
                    {
                        ("Yes", m.IpYes),
                        ("No", m.IpNo)
                    }
                },
                new CategoryBlock
                {
                    CategoryName = "LGBTQ+",
                    Items = new List<(string, int?)>
                    {
                        ("Yes", m.LgbtYes),
                        ("No", m.LgbtNo)
                    }
                }
            };
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

            // SPIC (Out of scope - left empty)
            m.SpicNew = null;
            m.SpicRenewed = null;
            m.SpicTerminated = null;

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
