using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SOLUM_UI.Models;

namespace SOLUM_UI.Services
{
    /// <summary>
    /// Generates clean, well-aligned Excel (.xlsx) workbooks for the Solo Parent Subsidy module:
    /// City Educ Cross-Check, DSWD 4Ps Cross-Check, Final Grantee Ledger, and Disqualified Audit.
    /// </summary>
    public static class SubsidyExcelExportService
    {
        private const string FontName = "Arial";
        private static readonly Color Maroon = Color.FromArgb(107, 41, 67);        // #6B2943
        private static readonly Color ZebraBg = Color.FromArgb(248, 249, 250);      // #F8F9FA
        private static readonly Color BorderColor = Color.FromArgb(209, 213, 219);  // #D1D5DB
        private static readonly Color TextDark = Color.FromArgb(17, 24, 39);        // #111827

        #region 1. City Educ Scholarship Cross-Check

        public static void ExportCityEducRosterToExcel(string filePath, List<SelectionRow> items, int fiscalYear)
        {
            if (items == null) items = new List<SelectionRow>();
            ExcelPackage.License.SetNonCommercialOrganization("DSWD Solo Parents Program");

            using (var pkg = new ExcelPackage())
            {
                var ws = pkg.Workbook.Worksheets.Add("City Educ Cross-Check");

                string[] headers = new[]
                {
                    "No.",
                    "Solo Parent Full Name",
                    "Barangay",
                    "Contact Number",
                    "Dependent Student Name",
                    "Relationship",
                    "Age",
                    "Date of Birth",
                    "Educational Attainment / Year Level",
                    "City Educ Verification Remarks"
                };

                StyleHeaderRow(ws, headers);

                int rowIdx = 2;
                int count = 0;

                foreach (var r in items.Where(i => i.HasCollegeAgeDependent))
                {
                    string contact = !string.IsNullOrWhiteSpace(r.ContactNumberFormatted) && r.ContactNumberFormatted != "—"
                        ? r.ContactNumberFormatted
                        : (!string.IsNullOrWhiteSpace(r.ContactNumber) ? r.ContactNumber : "—");

                    var deps = (r.CollegeAgeDependents != null && r.CollegeAgeDependents.Count > 0)
                        ? r.CollegeAgeDependents
                        : new List<ChildDetailItem>
                        {
                            new ChildDetailItem
                            {
                                Name = "Dependent (Age 17–24)",
                                Relationship = "Child",
                                Age = 18,
                                EducationalLevel = "College Level"
                            }
                        };

                    foreach (var child in deps)
                    {
                        count++;
                        string edu = !string.IsNullOrWhiteSpace(child.EducationalLevel) ? child.EducationalLevel : "—";
                        string rel = !string.IsNullOrWhiteSpace(child.Relationship) ? child.Relationship : "Child";
                        string bday = child.BirthdateFormatted;

                        object[] values = new object[]
                        {
                            count,
                            r.Name ?? string.Empty,
                            r.Barangay ?? string.Empty,
                            contact,
                            child.Name ?? string.Empty,
                            rel,
                            child.Age,
                            bday,
                            edu,
                            string.Empty
                        };

                        WriteDataRow(ws, rowIdx, values, count % 2 == 0);
                        ws.Cells[rowIdx, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        ws.Cells[rowIdx, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        ws.Cells[rowIdx, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        rowIdx++;
                    }
                }

                FinalizeSheet(ws, headers.Length, rowIdx - 1, new double[] { 7, 28, 18, 16, 28, 14, 8, 14, 24, 30 });
                pkg.SaveAs(new FileInfo(filePath));
            }
        }

        #endregion

        #region 2. DSWD Pantawid Pamilya (4Ps) Cross-Check

        public static void ExportPantawid4PsRosterToExcel(string filePath, List<SelectionRow> items, int fiscalYear)
        {
            if (items == null) items = new List<SelectionRow>();
            ExcelPackage.License.SetNonCommercialOrganization("DSWD Solo Parents Program");

            using (var pkg = new ExcelPackage())
            {
                var ws = pkg.Workbook.Worksheets.Add("DSWD 4Ps Cross-Check");

                string[] headers = new[]
                {
                    "No.",
                    "PhilSys Card Number",
                    "Solo Parent Full Name",
                    "Sex",
                    "Date of Birth",
                    "Barangay",
                    "Complete Address",
                    "Contact Number",
                    "Monthly Income",
                    "Minor Dependents (0–18 yrs)",
                    "Solo Parent Classification",
                    "4Ps Tag in SOLACE",
                    "DSWD Pantawid Verification Status"
                };

                StyleHeaderRow(ws, headers);

                int rowIdx = 2;
                int count = 0;

                foreach (var r in items.Where(i => i.IsPantawidBeneficiary))
                {
                    count++;
                    string philsys = !string.IsNullOrWhiteSpace(r.PhilSysCardNumber) && r.PhilSysCardNumber != "—" ? r.PhilSysCardNumber : "—";
                    string contact = !string.IsNullOrWhiteSpace(r.ContactNumberFormatted) && r.ContactNumberFormatted != "—"
                        ? r.ContactNumberFormatted
                        : (!string.IsNullOrWhiteSpace(r.ContactNumber) ? r.ContactNumber : "—");
                    string addr = !string.IsNullOrWhiteSpace(r.Address)
                        ? r.Address.Trim()
                        : (!string.IsNullOrWhiteSpace(r.Barangay) ? r.Barangay : "—");
                    string bday = !string.IsNullOrWhiteSpace(r.DateOfBirthFormatted) ? r.DateOfBirthFormatted : "—";
                    string sex = !string.IsNullOrWhiteSpace(r.Sex) ? r.Sex : "Female";
                    string circ = !string.IsNullOrWhiteSpace(r.Circumstance) ? r.Circumstance : "—";

                    object[] values = new object[]
                    {
                        count,
                        philsys,
                        r.Name ?? string.Empty,
                        sex,
                        bday,
                        r.Barangay ?? string.Empty,
                        addr,
                        contact,
                        r.MonthlyIncome,
                        r.MinorDependentsCount,
                        circ,
                        "Enrolled (DSWD 4Ps)",
                        string.Empty
                    };

                    WriteDataRow(ws, rowIdx, values, count % 2 == 0);
                    ws.Cells[rowIdx, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[rowIdx, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[rowIdx, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[rowIdx, 9].Style.Numberformat.Format = "₱#,##0.00";
                    ws.Cells[rowIdx, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    ws.Cells[rowIdx, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    rowIdx++;
                }

                FinalizeSheet(ws, headers.Length, rowIdx - 1, new double[] { 7, 22, 26, 10, 14, 18, 34, 16, 16, 16, 26, 20, 28 });
                pkg.SaveAs(new FileInfo(filePath));
            }
        }

        #endregion

        #region 3. Final Grantee Disbursement Ledger

        public static void ExportFinalLedgerToExcel(string filePath, List<SelectionRow> items, int fiscalYear, decimal grantAmount)
        {
            if (items == null) items = new List<SelectionRow>();
            ExcelPackage.License.SetNonCommercialOrganization("DSWD Solo Parents Program");

            using (var pkg = new ExcelPackage())
            {
                var ws = pkg.Workbook.Worksheets.Add($"Final Ledger FY {fiscalYear}");

                string[] headers = new[]
                {
                    "No.",
                    "PhilSys / Valid ID No.",
                    "Grantee Full Name",
                    "Barangay",
                    "Complete Address",
                    "Contact Number",
                    "Date of Birth",
                    "Grant Amount (₱)",
                    "Status",
                    "Date Received",
                    "Signature",
                    "Disbursing Officer Remarks"
                };

                StyleHeaderRow(ws, headers);

                int rowIdx = 2;
                int count = 0;

                foreach (var r in items)
                {
                    count++;
                    string philsys = !string.IsNullOrWhiteSpace(r.PhilSysCardNumber) && r.PhilSysCardNumber != "—" ? r.PhilSysCardNumber : "—";
                    string contact = !string.IsNullOrWhiteSpace(r.ContactNumberFormatted) && r.ContactNumberFormatted != "—"
                        ? r.ContactNumberFormatted
                        : (!string.IsNullOrWhiteSpace(r.ContactNumber) ? r.ContactNumber : "—");
                    string addr = !string.IsNullOrWhiteSpace(r.Address)
                        ? r.Address.Trim()
                        : (!string.IsNullOrWhiteSpace(r.Barangay) ? r.Barangay : "—");
                    string bday = !string.IsNullOrWhiteSpace(r.DateOfBirthFormatted) ? r.DateOfBirthFormatted : "—";

                    object[] values = new object[]
                    {
                        count,
                        philsys,
                        r.Name ?? string.Empty,
                        r.Barangay ?? string.Empty,
                        addr,
                        contact,
                        bday,
                        grantAmount,
                        "Confirmed Grantee",
                        string.Empty,
                        string.Empty,
                        string.Empty
                    };

                    WriteDataRow(ws, rowIdx, values, count % 2 == 0);
                    ws.Cells[rowIdx, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[rowIdx, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[rowIdx, 8].Style.Numberformat.Format = "₱#,##0.00";
                    ws.Cells[rowIdx, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    ws.Cells[rowIdx, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    rowIdx++;
                }

                FinalizeSheet(ws, headers.Length, rowIdx - 1, new double[] { 7, 22, 26, 18, 34, 16, 14, 16, 18, 16, 24, 28 });
                pkg.SaveAs(new FileInfo(filePath));
            }
        }

        #endregion

        #region 4. Disqualified Candidates Audit

        public static void ExportDisqualifiedToExcel(string filePath, List<DisqualifiedCandidateItem> items, int fiscalYear)
        {
            if (items == null) items = new List<DisqualifiedCandidateItem>();
            ExcelPackage.License.SetNonCommercialOrganization("DSWD Solo Parents Program");

            using (var pkg = new ExcelPackage())
            {
                var ws = pkg.Workbook.Worksheets.Add($"Disqualified FY {fiscalYear}");

                string[] headers = new[]
                {
                    "No.",
                    "Full Name",
                    "Barangay",
                    "Monthly Income",
                    "Vulnerability Score",
                    "Priority Level",
                    "Dependents",
                    "Disqualification Reason",
                    "Audit Justification Notes",
                    "Disqualified By",
                    "Timestamp"
                };

                StyleHeaderRow(ws, headers);

                int rowIdx = 2;
                int count = 0;

                foreach (var c in items)
                {
                    count++;
                    object[] values = new object[]
                    {
                        count,
                        c.FullName ?? string.Empty,
                        c.Barangay ?? string.Empty,
                        c.MonthlyIncome,
                        c.PriorityScore,
                        c.Priority ?? string.Empty,
                        c.Dependents,
                        c.RejectionReason ?? string.Empty,
                        c.RejectionNotes ?? string.Empty,
                        c.DisqualifiedBy ?? string.Empty,
                        c.FormattedDisqualifiedAt ?? string.Empty
                    };

                    WriteDataRow(ws, rowIdx, values, count % 2 == 0);
                    ws.Cells[rowIdx, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[rowIdx, 4].Style.Numberformat.Format = "₱#,##0.00";
                    ws.Cells[rowIdx, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    ws.Cells[rowIdx, 5].Style.Numberformat.Format = "0.00";
                    ws.Cells[rowIdx, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[rowIdx, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    rowIdx++;
                }

                FinalizeSheet(ws, headers.Length, rowIdx - 1, new double[] { 7, 26, 18, 16, 16, 14, 12, 26, 36, 20, 20 });
                pkg.SaveAs(new FileInfo(filePath));
            }
        }

        #endregion

        #region Formatting Helpers

        private static void StyleHeaderRow(ExcelWorksheet ws, string[] headers)
        {
            for (int c = 0; c < headers.Length; c++)
            {
                var cell = ws.Cells[1, c + 1];
                cell.Value = headers[c];
                cell.Style.Font.Name = FontName;
                cell.Style.Font.Size = 10f;
                cell.Style.Font.Bold = true;
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Maroon);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                cell.Style.WrapText = true;

                cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Top.Color.SetColor(Maroon);
                cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Bottom.Color.SetColor(Maroon);
                cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Left.Color.SetColor(Color.FromArgb(140, 70, 96));
                cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Right.Color.SetColor(Color.FromArgb(140, 70, 96));
            }
            ws.Row(1).Height = 28f;
        }

        private static void WriteDataRow(ExcelWorksheet ws, int rowIdx, object[] values, bool isZebra)
        {
            for (int c = 0; c < values.Length; c++)
            {
                var cell = ws.Cells[rowIdx, c + 1];
                cell.Value = values[c];
                cell.Style.Font.Name = FontName;
                cell.Style.Font.Size = 9.5f;
                cell.Style.Font.Color.SetColor(TextDark);
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                cell.Style.WrapText = true;

                if (isZebra)
                {
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(ZebraBg);
                }

                cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Top.Color.SetColor(BorderColor);
                cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Bottom.Color.SetColor(BorderColor);
                cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Left.Color.SetColor(BorderColor);
                cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Right.Color.SetColor(BorderColor);
            }
            ws.Row(rowIdx).Height = 22f;
        }

        private static void FinalizeSheet(ExcelWorksheet ws, int colCount, int lastRow, double[] widths)
        {
            for (int c = 0; c < colCount; c++)
            {
                double w = (widths != null && c < widths.Length) ? widths[c] : 18.0;
                ws.Column(c + 1).Width = w;
            }

            if (lastRow >= 1)
            {
                ws.Cells[1, 1, lastRow, colCount].AutoFilter = true;
            }
            ws.View.FreezePanes(2, 1);

            ws.PrinterSettings.Orientation = eOrientation.Landscape;
            ws.PrinterSettings.FitToPage = true;
            ws.PrinterSettings.FitToWidth = 1;
            ws.PrinterSettings.FitToHeight = 0;
        }

        #endregion
    }
}
