using OfficeOpenXml;
using OfficeOpenXml.Style;
using SOLUM_UI.Models.Api;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SOLUM_UI.Services
{
    /// <summary>
    /// Comprehensive analytics export service that generates professional Excel reports
    /// with soft maroon-pink theme styling and dynamic data from backend API.
    /// </summary>
    public static class AnalyticsExcelExportService
    {
        // Color Palette - Soft Maroon-Pink Theme
        private static class ColorPalette
        {
            public static Color HeaderBackground = Color.FromArgb(0x6B, 0x1D, 0x2F);  // Deep Soft Maroon
            public static Color SectionHeader = Color.FromArgb(0xD8, 0xA7, 0xB1);     // Muted Rose
            public static Color AlternateRowLight = Color.FromArgb(0xFD, 0xF5, 0xF7); // Light Pink Tint
            public static Color BorderColor = Color.FromArgb(0xE0, 0xD0, 0xD5);       // Soft Rose Border
            public static Color TextDark = Color.FromArgb(0x22, 0x22, 0x22);          // Dark Gray
            public static Color TextWhite = Color.White;
        }

        // Font Settings
        private const string FontName = "Segoe UI";
        private const float HeaderFontSize = 14f;
        private const float SectionHeaderFontSize = 11f;
        private const float DataFontSize = 10f;

        /// <summary>
        /// Export analytics data as a professional Excel report with dynamic backend data.
        /// </summary>
        public static void ExportAnalyticsReport(
            string filePath,
            MonthlyAnalyticsDto analyticsData,
            string municipality = "Biñan City",
            string region = "CALABARZON (IV-A)",
            string focalPersonName = "",
            string password = null)
        {
            using (var pkg = new ExcelPackage())
            {
                BuildAnalyticsSheet(pkg, analyticsData, municipality, region, focalPersonName);

                if (!string.IsNullOrEmpty(password))
                    pkg.SaveAs(new System.IO.FileInfo(filePath), password);
                else
                    pkg.SaveAs(new System.IO.FileInfo(filePath));
            }
        }

        /// <summary>
        /// Build the main analytics data sheet with all breakdowns.
        /// </summary>
        private static void BuildAnalyticsSheet(
            ExcelPackage pkg,
            MonthlyAnalyticsDto data,
            string municipality,
            string region,
            string focalPersonName)
        {
            var ws = pkg.Workbook.Worksheets.Add("Solo Parents Analytics");

            // Set default font and column width
            ws.DefaultColWidth = 20;
            ws.Cells.Style.Font.Name = FontName;
            ws.Cells.Style.Font.Size = DataFontSize;

            int row = 1;

            // ===== HEADER SECTION =====
            BuildHeader(ws, ref row, municipality, region, data.Year, data.Month, focalPersonName);

            row += 1;

            // ===== MAIN DATA STRUCTURE =====
            // Three columns: Category, Sub-Category, Total Count

            BuildColumnHeaders(ws, ref row);

            row += 1;

            // ===== DATA ROWS - DYNAMIC FROM BACKEND =====

            // Age Group Breakdown
            BuildBreakdownSection(ws, ref row, "Age Group", data.AgeBrackets);

            // Sex Breakdown
            BuildBreakdownSection(ws, ref row, "Sex", data.Sex);

            // Civil Status Breakdown
            BuildBreakdownSection(ws, ref row, "Civil Status", data.CivilStatus);

            // Employment Status Breakdown
            BuildBreakdownSection(ws, ref row, "Employment Status", data.EmploymentStatus);

            // Monthly Income Breakdown
            BuildBreakdownSection(ws, ref row, "Monthly Income", data.MonthlyIncomeBrackets);

            // Children/Dependents Age Range Breakdown
            BuildBreakdownSection(ws, ref row, "Children/Dependents Age Range", data.DependentsAgeBrackets);

            // Solo Parent Classification/Category Breakdown
            BuildBreakdownSection(ws, ref row, "Solo Parent Classification", data.Categories);

            // Educational Attainment Breakdown
            if (data.EducationalAttainment != null && data.EducationalAttainment.Count > 0)
                BuildBreakdownSection(ws, ref row, "Educational Attainment", data.EducationalAttainment);

            // Program Flags - Pantawid/4Ps
            BuildBreakdownSection(ws, ref row, "Pantawid / 4Ps Beneficiary", data.PantawidBeneficiary);

            // Program Flags - Indigenous People (IP)
            if (data.Indigenous != null && data.Indigenous.Count > 0)
                BuildBreakdownSection(ws, ref row, "Indigenous People (IP)", data.Indigenous);

            // Program Flags - LGBTQ+
            BuildBreakdownSection(ws, ref row, "LGBTQ+", data.Lgbt);

            // Summary Statistics
            row += 1;
            BuildSummaryStatistics(ws, ref row, data);

            // Adjust column widths dynamically
            AutoFitColumns(ws, 3);
        }

        /// <summary>
        /// Build the report header with metadata.
        /// </summary>
        private static void BuildHeader(
            ExcelWorksheet ws,
            ref int row,
            string municipality,
            string region,
            int year,
            int month,
            string focalPersonName)
        {
            // Title
            var titleCell = ws.Cells[row, 1];
            titleCell.Value = "SOLO PARENTS ANALYTICS REPORT";
            ws.Cells[row, 1, row, 3].Merge = true;
            ApplyHeaderStyle(ws.Cells[row, 1, row, 3]);
            ws.Row(row).Height = 24;
            row++;

            // Subtitle with location and period
            string monthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
            var subtitleCell = ws.Cells[row, 1];
            subtitleCell.Value = $"{municipality} | {monthName} {year}";
            ws.Cells[row, 1, row, 3].Merge = true;
            ApplySectionHeaderStyle(ws.Cells[row, 1, row, 3]);
            ws.Row(row).Height = 18;
            row += 2;

            // Metadata
            ApplyMetadataRow(ws, ref row, "Municipality/City:", municipality);
            ApplyMetadataRow(ws, ref row, "Region:", region);
            ApplyMetadataRow(ws, ref row, "Reporting Period:", $"{monthName} {year}");
            ApplyMetadataRow(ws, ref row, "Focal Person:", string.IsNullOrWhiteSpace(focalPersonName) ? "—" : focalPersonName);
            ApplyMetadataRow(ws, ref row, "Date of Submission:", DateTime.Today.ToString("MMMM d, yyyy"));
        }

        /// <summary>
        /// Build the column headers for the data table.
        /// </summary>
        private static void BuildColumnHeaders(ExcelWorksheet ws, ref int row)
        {
            ws.Cells[row, 1].Value = "Category";
            ws.Cells[row, 2].Value = "Sub-Category / Breakdown";
            ws.Cells[row, 3].Value = "Total Count";

            for (int col = 1; col <= 3; col++)
            {
                var cell = ws.Cells[row, col];
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(ColorPalette.HeaderBackground);
                cell.Style.Font.Color.SetColor(ColorPalette.TextWhite);
                cell.Style.Font.Bold = true;
                cell.Style.Font.Size = SectionHeaderFontSize;
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ApplyBorder(cell);
            }

            ws.Row(row).Height = 20;
            row++;
        }

        /// <summary>
        /// Build a breakdown section with dynamic data.
        /// </summary>
        private static void BuildBreakdownSection(
            ExcelWorksheet ws,
            ref int row,
            string categoryName,
            Dictionary<string, int> breakdownData)
        {
            if (breakdownData == null || breakdownData.Count == 0)
                return;

            // Section header
            var categoryCell = ws.Cells[row, 1, row, 3];
            categoryCell.Merge = true;
            categoryCell.Value = categoryName;
            ApplyCategoryHeaderStyle(categoryCell);
            ws.Row(row).Height = 18;
            row++;

            // Data rows
            int total = 0;
            bool isAlternate = false;

            foreach (var kvp in breakdownData.OrderByDescending(x => x.Value))
            {
                string subCategory = FormatSubCategoryLabel(kvp.Key);
                int count = kvp.Value;
                total += count;

                ApplyDataRow(ws, row, "", subCategory, count, isAlternate);
                isAlternate = !isAlternate;
                row++;
            }

            // Subtotal row
            ApplySubtotalRow(ws, row, categoryName, total);
            row += 2;
        }

        /// <summary>
        /// Build summary statistics section.
        /// </summary>
        private static void BuildSummaryStatistics(ExcelWorksheet ws, ref int row, MonthlyAnalyticsDto data)
        {
            // Summary header
            var headerCell = ws.Cells[row, 1, row, 3];
            headerCell.Merge = true;
            headerCell.Value = "SUMMARY STATISTICS";
            ApplyCategoryHeaderStyle(headerCell);
            ws.Row(row).Height = 18;
            row++;

            bool isAlternate = false;

            // Key metrics
            ApplyDataRow(ws, row, "", "Total Solo Parents", data.TotalSoloParents, isAlternate);
            isAlternate = !isAlternate;
            row++;

            int activeSoloParents = data.ActiveSoloParents > 0 ? data.ActiveSoloParents : (data.TotalSoloParents > 0 ? data.TotalSoloParents : 0);
            ApplyDataRow(ws, row, "", "Active Solo Parents", activeSoloParents, isAlternate);
            isAlternate = !isAlternate;
            row++;

            int inactiveSoloParents = data.InactiveSoloParents > 0 ? data.InactiveSoloParents : 0;
            ApplyDataRow(ws, row, "", "Inactive Solo Parents", inactiveSoloParents, isAlternate);
            isAlternate = !isAlternate;
            row++;

            ApplyDataRow(ws, row, "", "New Registrations", data.NewRegistrations, isAlternate);
            isAlternate = !isAlternate;
            row++;

            ApplyDataRow(ws, row, "", "Renewals", data.Renewals, isAlternate);
            row++;
        }

        /// <summary>
        /// Apply styling to a data row with alternating background colors.
        /// </summary>
        private static void ApplyDataRow(
            ExcelWorksheet ws,
            int row,
            string category,
            string subCategory,
            int count,
            bool isAlternate)
        {
            var bgColor = isAlternate ? ColorPalette.AlternateRowLight : Color.White;

            // Category cell
            var categoryCell = ws.Cells[row, 1];
            categoryCell.Value = category;
            ApplyDataCellStyle(categoryCell, bgColor, ExcelHorizontalAlignment.Left);

            // Sub-category cell (indented)
            var subCategoryCell = ws.Cells[row, 2];
            subCategoryCell.Value = "  " + subCategory;
            ApplyDataCellStyle(subCategoryCell, bgColor, ExcelHorizontalAlignment.Left);

            // Count cell (right-aligned, formatted)
            var countCell = ws.Cells[row, 3];
            countCell.Value = count;
            ApplyDataCellStyle(countCell, bgColor, ExcelHorizontalAlignment.Right);

            ws.Row(row).Height = 16;
        }

        /// <summary>
        /// Apply styling to a subtotal row.
        /// </summary>
        private static void ApplySubtotalRow(ExcelWorksheet ws, int row, string label, int total)
        {
            var labelCell = ws.Cells[row, 2];
            labelCell.Value = "TOTAL - " + label;
            labelCell.Style.Font.Bold = true;
            labelCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            labelCell.Style.Fill.BackgroundColor.SetColor(ColorPalette.SectionHeader);
            labelCell.Style.Font.Color.SetColor(ColorPalette.TextDark);
            labelCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ApplyBorder(labelCell);

            var countCell = ws.Cells[row, 3];
            countCell.Value = total;
            countCell.Style.Font.Bold = true;
            countCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            countCell.Style.Fill.BackgroundColor.SetColor(ColorPalette.SectionHeader);
            countCell.Style.Font.Color.SetColor(ColorPalette.TextDark);
            countCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ApplyBorder(countCell);

            ws.Row(row).Height = 16;
        }

        /// <summary>
        /// Apply metadata row styling.
        /// </summary>
        private static void ApplyMetadataRow(ExcelWorksheet ws, ref int row, string label, string value)
        {
            var labelCell = ws.Cells[row, 1];
            labelCell.Value = label;
            labelCell.Style.Font.Bold = true;
            labelCell.Style.Font.Color.SetColor(ColorPalette.TextDark);

            var valueCell = ws.Cells[row, 2];
            valueCell.Value = value;
            ws.Cells[row, 2, row, 3].Merge = true;
            valueCell.Style.Font.Color.SetColor(ColorPalette.TextDark);

            ws.Row(row).Height = 14;
            row++;
        }

        /// <summary>
        /// Apply header styling to title cells.
        /// </summary>
        private static void ApplyHeaderStyle(ExcelRange cells)
        {
            cells.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cells.Style.Fill.BackgroundColor.SetColor(ColorPalette.HeaderBackground);
            cells.Style.Font.Color.SetColor(ColorPalette.TextWhite);
            cells.Style.Font.Bold = true;
            cells.Style.Font.Size = HeaderFontSize;
            cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ApplyBorder(cells);
        }

        /// <summary>
        /// Apply section header styling.
        /// </summary>
        private static void ApplySectionHeaderStyle(ExcelRange cells)
        {
            cells.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cells.Style.Fill.BackgroundColor.SetColor(ColorPalette.SectionHeader);
            cells.Style.Font.Color.SetColor(ColorPalette.TextDark);
            cells.Style.Font.Bold = true;
            cells.Style.Font.Size = SectionHeaderFontSize;
            cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ApplyBorder(cells);
        }

        /// <summary>
        /// Apply category header styling.
        /// </summary>
        private static void ApplyCategoryHeaderStyle(ExcelRange cells)
        {
            cells.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cells.Style.Fill.BackgroundColor.SetColor(ColorPalette.SectionHeader);
            cells.Style.Font.Color.SetColor(ColorPalette.TextDark);
            cells.Style.Font.Bold = true;
            cells.Style.Font.Size = SectionHeaderFontSize;
            cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ApplyBorder(cells);
        }

        /// <summary>
        /// Apply data cell styling.
        /// </summary>
        private static void ApplyDataCellStyle(
            ExcelRange cell,
            Color backgroundColor,
            ExcelHorizontalAlignment alignment)
        {
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(backgroundColor);
            cell.Style.Font.Color.SetColor(ColorPalette.TextDark);
            cell.Style.Font.Size = DataFontSize;
            cell.Style.HorizontalAlignment = alignment;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ApplyBorder(cell);
        }

        /// <summary>
        /// Apply border styling.
        /// </summary>
        private static void ApplyBorder(ExcelRange cells)
        {
            cells.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            cells.Style.Border.Top.Color.SetColor(ColorPalette.BorderColor);
            cells.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            cells.Style.Border.Bottom.Color.SetColor(ColorPalette.BorderColor);
            cells.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            cells.Style.Border.Left.Color.SetColor(ColorPalette.BorderColor);
            cells.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            cells.Style.Border.Right.Color.SetColor(ColorPalette.BorderColor);
        }

        /// <summary>
        /// Auto-fit column widths based on content.
        /// </summary>
        private static void AutoFitColumns(ExcelWorksheet ws, int columnCount)
        {
            for (int col = 1; col <= columnCount; col++)
            {
                double maxLength = 0;

                for (int row = 1; row <= ws.Dimension?.Rows; row++)
                {
                    var cell = ws.Cells[row, col];
                    if (cell.Value != null)
                    {
                        string cellValue = cell.Value.ToString();
                        maxLength = Math.Max(maxLength, cellValue.Length);
                    }
                }

                // Set column width with padding
                ws.Column(col).Width = Math.Min(maxLength + 2, 50);
            }
        }

        /// <summary>
        /// Format sub-category labels for display (convert underscores, title case, etc.)
        /// </summary>
        private static string FormatSubCategoryLabel(string key)
        {
            if (string.IsNullOrEmpty(key))
                return "—";

            // Replace underscores with spaces
            string formatted = key.Replace("_", " ");

            // Title case
            formatted = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(formatted.ToLower());

            return formatted;
        }
    }
}
