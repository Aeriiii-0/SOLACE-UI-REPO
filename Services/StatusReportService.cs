using System;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;
using SOLUM_UI.Models;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Services
{
    /// <summary>
    /// Service for generating status-based and category-based reports
    /// </summary>
    public static class StatusReportService
    {
        /// <summary>
        /// Generates a comprehensive status report with all status categories
        /// </summary>
        public static void ExportComprehensiveStatusReport(string filePath, List<SoloParentRecordViewModel> records)
        {
            if (records == null || records.Count == 0)
                throw new ArgumentException("No records to export");

            using (var package = new ExcelPackage())
            {
                var summarySheet = package.Workbook.Worksheets.Add("Status Summary");
                int row = 1;

                // Summary Sheet Header
                summarySheet.Cells[row, 1].Value = "SOLO PARENTS STATUS REPORT";
                summarySheet.Cells[row, 1].Style.Font.Bold = true;
                summarySheet.Cells[row, 1].Style.Font.Size = 14;
                summarySheet.Cells[row, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                summarySheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(107, 41, 67));
                summarySheet.Cells[row, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                summarySheet.Merge(row, row, 1, 3);
                row += 2;

                // Status counts
                var statusCounts = records.GroupBy(r => r.Status ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count())
                    .OrderByDescending(x => x.Value);

                summarySheet.Cells[row, 1].Value = "Status";
                summarySheet.Cells[row, 2].Value = "Count";
                summarySheet.Cells[row, 3].Value = "Percentage";
                ApplyHeaderRowFormat(summarySheet, row);
                row++;

                int totalRecords = records.Count;
                foreach (var status in statusCounts)
                {
                    summarySheet.Cells[row, 1].Value = status.Key;
                    summarySheet.Cells[row, 2].Value = status.Value;
                    summarySheet.Cells[row, 2].Style.NumberFormat = "0";
                    summarySheet.Cells[row, 3].Value = (double)status.Value / totalRecords;
                    summarySheet.Cells[row, 3].Style.NumberFormat = "0.0%";
                    row++;
                }

                // Classification breakdown
                row += 2;
                summarySheet.Cells[row, 1].Value = "CLASSIFICATION BREAKDOWN";
                summarySheet.Cells[row, 1].Style.Font.Bold = true;
                summarySheet.Cells[row, 1].Style.Font.Size = 12;
                row += 1;

                var classificationCounts = records.GroupBy(r => r.Classification ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count())
                    .OrderByDescending(x => x.Value);

                summarySheet.Cells[row, 1].Value = "Classification";
                summarySheet.Cells[row, 2].Value = "Count";
                ApplyHeaderRowFormat(summarySheet, row);
                row++;

                foreach (var classification in classificationCounts)
                {
                    summarySheet.Cells[row, 1].Value = classification.Key;
                    summarySheet.Cells[row, 2].Value = classification.Value;
                    summarySheet.Cells[row, 2].Style.NumberFormat = "0";
                    row++;
                }

                // Barangay breakdown
                row += 2;
                summarySheet.Cells[row, 1].Value = "BARANGAY BREAKDOWN";
                summarySheet.Cells[row, 1].Style.Font.Bold = true;
                summarySheet.Cells[row, 1].Style.Font.Size = 12;
                row += 1;

                var barangayBreakdown = records.GroupBy(r => r.RawModel.Barangay ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count())
                    .OrderByDescending(x => x.Value);

                summarySheet.Cells[row, 1].Value = "Barangay";
                summarySheet.Cells[row, 2].Value = "Count";
                ApplyHeaderRowFormat(summarySheet, row);
                row++;

                foreach (var barangay in barangayBreakdown)
                {
                    summarySheet.Cells[row, 1].Value = barangay.Key;
                    summarySheet.Cells[row, 2].Value = barangay.Value;
                    summarySheet.Cells[row, 2].Style.NumberFormat = "0";
                    row++;
                }

                summarySheet.Column(1).Width = 25;
                summarySheet.Column(2).Width = 15;
                summarySheet.Column(3).Width = 15;

                // Create detailed sheets for each status
                var statuses = records.Select(r => r.Status).Distinct().Where(s => !string.IsNullOrEmpty(s));
                foreach (var status in statuses)
                {
                    var statusRecords = records.Where(r => r.Status == status).ToList();
                    CreateDetailedStatusSheet(package, status, statusRecords);
                }

                package.SaveAs(new System.IO.FileInfo(filePath));
            }
        }

        /// <summary>
        /// Export records by classification (A1, A2, B, C, D, etc.)
        /// </summary>
        public static void ExportByClassification(string filePath, List<SoloParentRecordViewModel> records)
        {
            if (records == null || records.Count == 0)
                throw new ArgumentException("No records to export");

            using (var package = new ExcelPackage())
            {
                var classifications = records.Select(r => r.Classification).Distinct().Where(c => !string.IsNullOrEmpty(c)).OrderBy(c => c);

                foreach (var classification in classifications)
                {
                    var classRecords = records.Where(r => r.Classification == classification).ToList();
                    CreateDetailedStatusSheet(package, classification, classRecords, "Classification");
                }

                package.SaveAs(new System.IO.FileInfo(filePath));
            }
        }

        /// <summary>
        /// Export records by barangay/location
        /// </summary>
        public static void ExportByBarangay(string filePath, List<SoloParentRecordViewModel> records)
        {
            if (records == null || records.Count == 0)
                throw new ArgumentException("No records to export");

            using (var package = new ExcelPackage())
            {
                var barangays = records.Select(r => r.RawModel.Barangay).Distinct().Where(b => !string.IsNullOrEmpty(b)).OrderBy(b => b);

                foreach (var barangay in barangays)
                {
                    var barangayRecords = records.Where(r => r.RawModel.Barangay == barangay).ToList();
                    CreateDetailedStatusSheet(package, barangay, barangayRecords, "Barangay");
                }

                package.SaveAs(new System.IO.FileInfo(filePath));
            }
        }

        /// <summary>
        /// Export records by encoder/staff member
        /// </summary>
        public static void ExportByEncoder(string filePath, List<SoloParentRecordViewModel> records)
        {
            if (records == null || records.Count == 0)
                throw new ArgumentException("No records to export");

            using (var package = new ExcelPackage())
            {
                var encoders = records.Select(r => r.RawModel.EncodedBy).Distinct().Where(e => !string.IsNullOrEmpty(e)).OrderBy(e => e);

                foreach (var encoder in encoders)
                {
                    var encoderRecords = records.Where(r => r.RawModel.EncodedBy == encoder).ToList();
                    CreateDetailedStatusSheet(package, encoder, encoderRecords, "Encoder");
                }

                package.SaveAs(new System.IO.FileInfo(filePath));
            }
        }

        /// <summary>
        /// Export records by sex/gender
        /// </summary>
        public static void ExportBySex(string filePath, List<SoloParentRecordViewModel> records)
        {
            if (records == null || records.Count == 0)
                throw new ArgumentException("No records to export");

            using (var package = new ExcelPackage())
            {
                var sexes = records.Select(r => r.RawModel.Sex).Distinct().Where(s => !string.IsNullOrEmpty(s)).OrderBy(s => s);

                foreach (var sex in sexes)
                {
                    var sexRecords = records.Where(r => r.RawModel.Sex == sex).ToList();
                    CreateDetailedStatusSheet(package, sex, sexRecords, "Sex");
                }

                package.SaveAs(new System.IO.FileInfo(filePath));
            }
        }

        /// <summary>
        /// Export records by civil status
        /// </summary>
        public static void ExportByCivilStatus(string filePath, List<SoloParentRecordViewModel> records)
        {
            if (records == null || records.Count == 0)
                throw new ArgumentException("No records to export");

            using (var package = new ExcelPackage())
            {
                var civilStatuses = records.Select(r => r.RawModel.CivilStatus).Distinct().Where(s => !string.IsNullOrEmpty(s)).OrderBy(s => s);

                foreach (var civilStatus in civilStatuses)
                {
                    var statusRecords = records.Where(r => r.RawModel.CivilStatus == civilStatus).ToList();
                    CreateDetailedStatusSheet(package, civilStatus, statusRecords, "Civil Status");
                }

                package.SaveAs(new System.IO.FileInfo(filePath));
            }
        }

        private static void CreateDetailedStatusSheet(ExcelPackage package, string sheetName, List<SoloParentRecordViewModel> records, string reportType = "Status")
        {
            // Sanitize sheet name (max 31 characters, no special chars)
            string safeName = sheetName.Replace("/", "-").Replace("\\", "-").Replace("?", "").Replace("*", "").Replace("[", "").Replace("]", "");
            if (safeName.Length > 31) safeName = safeName.Substring(0, 31);

            var sheet = package.Workbook.Worksheets.Add(safeName);

            // Set column widths
            sheet.Column(1).Width = 15;
            sheet.Column(2).Width = 20;
            sheet.Column(3).Width = 20;
            sheet.Column(4).Width = 15;
            sheet.Column(5).Width = 15;
            sheet.Column(6).Width = 15;
            sheet.Column(7).Width = 12;
            sheet.Column(8).Width = 18;
            sheet.Column(9).Width = 15;
            sheet.Column(10).Width = 15;

            // Header with report type
            int row = 1;
            sheet.Cells[row, 1].Value = $"{reportType.ToUpper()}: {sheetName}";
            sheet.Cells[row, 1].Style.Font.Bold = true;
            sheet.Cells[row, 1].Style.Font.Size = 12;
            sheet.Cells[row, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            sheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(107, 41, 67));
            sheet.Cells[row, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
            sheet.Merge(row, row, 1, 10);
            row += 2;

            // Column headers
            var headerCells = new[] { "ID", "Full Name", "Barangay", "Status", "Classification", "Age", "Sex", "Contact", "Monthly Income", "Date Registered" };
            for (int col = 1; col <= headerCells.Length; col++)
            {
                var cell = sheet.Cells[row, col];
                cell.Value = headerCells[col - 1];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(107, 41, 67));
                cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            row++;
            bool alternate = false;

            // Data rows
            foreach (var record in records.OrderBy(r => r.RawModel.Surname))
            {
                var rawModel = record.RawModel;
                int age = rawModel.DateOfBirth != DateTime.MinValue
                    ? (int)((DateTime.Today - rawModel.DateOfBirth).TotalDays / 365.25)
                    : 0;

                sheet.Cells[row, 1].Value = rawModel.Id ?? "";
                sheet.Cells[row, 2].Value = $"{rawModel.FirstName} {rawModel.MiddleName} {rawModel.Surname}".Trim();
                sheet.Cells[row, 3].Value = rawModel.Barangay ?? "";
                sheet.Cells[row, 4].Value = record.Status ?? "";
                sheet.Cells[row, 5].Value = record.Classification ?? "";
                sheet.Cells[row, 6].Value = age;
                sheet.Cells[row, 7].Value = rawModel.Sex ?? "";
                sheet.Cells[row, 8].Value = rawModel.ContactNumber ?? "";
                sheet.Cells[row, 9].Value = rawModel.MonthlyIncome ?? "";
                sheet.Cells[row, 10].Value = rawModel.DateOfApplication != DateTime.MinValue
                    ? rawModel.DateOfApplication.ToString("MM/dd/yyyy")
                    : "";

                // Apply alternating row colors
                if (alternate)
                {
                    var rowCells = sheet.Cells[row, 1, row, headerCells.Length];
                    rowCells.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    rowCells.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(253, 245, 247));
                }

                alternate = !alternate;
                row++;
            }

            // Add totals row
            row++;
            sheet.Cells[row, 1].Value = "TOTAL:";
            sheet.Cells[row, 1].Style.Font.Bold = true;
            sheet.Cells[row, 2].Value = records.Count;
            sheet.Cells[row, 2].Style.Font.Bold = true;

            sheet.View.FreezePanes(3, 1);
        }

        private static void ApplyHeaderRowFormat(ExcelWorksheet sheet, int row)
        {
            for (int col = 1; col <= 3; col++)
            {
                var cell = sheet.Cells[row, col];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(107, 41, 67));
                cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }
        }
    }
}
