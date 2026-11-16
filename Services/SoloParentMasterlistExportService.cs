using OfficeOpenXml;
using OfficeOpenXml.Style;
using SOLUM_UI.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace SOLUM_UI.Services
{
    /// <summary>
    /// Exports a complete masterlist of all solo parent records to an Excel (.xlsx) file,
    /// including all demographic, contact, family, and status fields.
    /// </summary>
    public static class SoloParentMasterlistExportService
    {
        private const string FontName = "Segoe UI";

        // Brand colors (used only for borders and header text, not cell fills)
        private static readonly Color Maroon      = Color.FromArgb(0x70, 0x29, 0x43);
        private static readonly Color MutedText   = Color.FromArgb(0x8B, 0x74, 0x80);
        private static readonly Color InkText      = Color.FromArgb(0x2B, 0x1A, 0x22);
        private static readonly Color BorderColor = Color.FromArgb(0xE0, 0xE0, 0xE0);

        /// <summary>
        /// Exports the given solo parent records to an Excel masterlist.
        /// </summary>
        /// <param name="filePath">Output file path (.xlsx)</param>
        /// <param name="records">List of SoloParentRecord objects to export</param>
        /// <param name="exportedBy">Name of the user performing the export (for the footer)</param>
        /// <param name="password">Optional password to protect the file</param>
        public static void ExportMasterlist(string filePath, List<SoloParentRecord> records, string exportedBy, string password = null)
        {
            ExcelPackage.License.SetNonCommercialOrganization("DSWD Solo Parents Program");

            using (var pkg = new ExcelPackage())
            {
                var ws = pkg.Workbook.Worksheets.Add("Solo Parent Masterlist");

                // ── Title rows ──────────────────────────────────────────────
                ws.Cells["A1"].Value = "SOLO PARENT MASTERLIST";
                ws.Cells["A1"].Style.Font.Size = 16;
                ws.Cells["A1"].Style.Font.Bold = true;
                ws.Cells["A1"].Style.Font.Name = FontName;
                ws.Cells["A1"].Style.Font.Color.SetColor(Maroon);
                ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells["A2"].Value = "Department of Social Welfare and Development - Solo Parents Program";
                ws.Cells["A2"].Style.Font.Size = 11;
                ws.Cells["A2"].Style.Font.Name = FontName;
                ws.Cells["A2"].Style.Font.Color.SetColor(MutedText);
                ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells["A3"].Value = $"Exported: {DateTime.Now:MMMM d, yyyy h:mm:ss tt}  |  Exported by: {exportedBy ?? "Administrator"}  |  Total Records: {records.Count}";
                ws.Cells["A3"].Style.Font.Size = 10;
                ws.Cells["A3"].Style.Font.Name = FontName;
                ws.Cells["A3"].Style.Font.Color.SetColor(MutedText);
                ws.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // ── Column headers ─────────────────────────────────────────
                int headerRow = 5;
                string[] headers = new string[]
                {
                    "#", "Solo Parent ID", "Surname", "First Name", "Middle Name", "Extension",
                    "Full Name", "PhilSys Number", "Sex", "Civil Status", "Date of Birth",
                    "Place of Birth", "Citizenship", "Educational Attainment", "Occupation",
                    "Monthly Income", "Employment Status", "Barangay", "Address", "Contact Number",
                    "No. of Children", "Emergency Contact Name", "Emergency Contact Number",
                    "Emergency Relationship", "Date of Application", "Application Type",
                    "Status", "Valid Until", "Created By", "Last Updated", "Circumstances"
                };

                for (int c = 0; c < headers.Length; c++)
                {
                    var cell = ws.Cells[headerRow, c + 1];
                    cell.Value = headers[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Size = 10.5f;
                    cell.Style.Font.Name = FontName;
                    cell.Style.Font.Color.SetColor(Maroon);
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    cell.Style.WrapText = true;
                    cell.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    cell.Style.Border.Bottom.Color.SetColor(Maroon);
                    cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Top.Color.SetColor(BorderColor);
                    cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Left.Color.SetColor(BorderColor);
                    cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Right.Color.SetColor(BorderColor);
                }

                ws.Row(headerRow).Height = 36f;

                // ── Data rows ──────────────────────────────────────────────
                for (int i = 0; i < records.Count; i++)
                {
                    var r = records[i];
                    int row = headerRow + 1 + i;

                    string[] values = new string[]
                    {
                        (i + 1).ToString(),
                        r.Id ?? "—",
                        r.Surname ?? "—",
                        r.FirstName ?? "—",
                        r.MiddleName ?? "—",
                        r.ExtensionName ?? "—",
                        r.FullName ?? "—",
                        r.PhilSysNumber ?? "—",
                        r.Sex ?? "—",
                        r.CivilStatus ?? "—",
                        r.DateOfBirthFormatted,
                        r.PlaceOfBirth ?? "—",
                        r.Citizenship ?? "—",
                        r.EducationalAttainment ?? "—",
                        r.Occupation ?? "—",
                        r.MonthlyIncome ?? "—",
                        r.EmploymentStatusDisplay,
                        r.Barangay ?? "—",
                        r.Address ?? "—",
                        r.ContactNumber ?? "—",
                        r.Children > 0 ? r.Children.ToString() : "0",
                        r.EmergencyContactName ?? "—",
                        r.EmergencyContactNumber ?? "—",
                        r.EmergencyRelationship ?? "—",
                        r.ApplicationDateFormatted,
                        r.ApplicationType,
                        r.Status ?? "—",
                        r.CurrentTermExpiresAt.HasValue ? r.CurrentTermExpiresAt.Value.ToString("yyyy-MM-dd") : "—",
                        r.CreatedBy ?? "—",
                        r.LastUpdatedFormatted,
                        r.CircumstancesDisplay ?? "—"
                    };

                    for (int c = 0; c < values.Length; c++)
                    {
                        var cell = ws.Cells[row, c + 1];
                        cell.Value = values[c];
                        cell.Style.Font.Size = 10f;
                        cell.Style.Font.Name = FontName;
                        cell.Style.Font.Color.SetColor(InkText);
                        cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        cell.Style.HorizontalAlignment = c == 0 ? ExcelHorizontalAlignment.Center : ExcelHorizontalAlignment.Left;
                        cell.Style.WrapText = (c == 18 || c == 30); // Address and Circumstances
                        cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Left.Color.SetColor(BorderColor);
                        cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Right.Color.SetColor(BorderColor);
                        cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Bottom.Color.SetColor(BorderColor);
                    }
                }

                // ── Column widths ──────────────────────────────────────────
                double[] widths = { 5, 18, 18, 18, 18, 10, 30, 22, 8, 14, 14, 22, 12, 22, 18,
                                    16, 16, 16, 28, 16, 10, 22, 18, 16, 16, 14, 14, 14, 18, 14, 40 };
                for (int c = 0; c < widths.Length; c++)
                    ws.Column(c + 1).Width = widths[c];

                // Merge title rows across all columns
                ws.Cells[1, 1, 1, headers.Length].Merge = true;
                ws.Cells[2, 1, 2, headers.Length].Merge = true;
                ws.Cells[3, 1, 3, headers.Length].Merge = true;

                // Freeze panes below header
                ws.View.FreezePanes(headerRow + 1, 1);

                // Auto-filter on the header row
                ws.Cells[headerRow, 1, headerRow, headers.Length].AutoFilter = true;

                // ── Save ────────────────────────────────────────────────────
                var fileInfo = new FileInfo(filePath);
                if (!string.IsNullOrEmpty(password))
                    pkg.SaveAs(fileInfo, password);
                else
                    pkg.SaveAs(fileInfo);
            }
        }
    }
}
