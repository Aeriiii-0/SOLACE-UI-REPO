using System;
using System.Collections.Generic;
using System.Linq;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SOLUM_UI.Models;

namespace SOLUM_UI.Services
{
    /// <summary>
    /// Service for generating official, printable PDF rosters for the Solo Parent Subsidy Program.
    /// Formatted on Philippine Government Standard Long Bond Paper (8.5" x 13" Folio / Legal) in Landscape.
    /// </summary>
    public static class SubsidyPdfExportService
    {
        // Long Bond Paper (8.5" x 13" Folio) Landscape Dimensions in points (72 pt / inch)
        public const double PageWidth = 936.0;   // 13.0 inches * 72 pt/in
        public const double PageHeight = 612.0;  // 8.5 inches * 72 pt/in

        private const double MarginLeft = 36.0;
        private const double MarginRight = 36.0;
        private const double MarginTop = 32.0;
        private const double MarginBottom = 34.0;
        private const double UsableWidth = PageWidth - MarginLeft - MarginRight; // 864 pt

        // Brushes & Pens
        private static readonly XColor MaroonColor = XColor.FromArgb(107, 41, 67);       // #6B2943
        private static readonly XColor SubtitleGray = XColor.FromArgb(75, 85, 99);       // #4B5563
        private static readonly XColor TextDark = XColor.FromArgb(17, 24, 39);           // #111827

        private static readonly XSolidBrush MaroonBrush = new XSolidBrush(MaroonColor);
        private static readonly XSolidBrush WhiteBrush = new XSolidBrush(XColors.White);
        private static readonly XSolidBrush TextDarkBrush = new XSolidBrush(TextDark);
        private static readonly XSolidBrush SubtitleBrush = new XSolidBrush(SubtitleGray);
        private static readonly XSolidBrush TableHeaderBgBrush = new XSolidBrush(MaroonColor);

        private static readonly XPen BlackBorderPen = new XPen(XColors.Black, 0.6);
        private static readonly XPen HeaderRulePen = new XPen(XColors.Black, 1.0);

        // Fonts
        private static readonly XFont HeaderLguFont = new XFont("Arial", 9.5, XFontStyleEx.Bold);
        private static readonly XFont TitleFont = new XFont("Arial", 12.5, XFontStyleEx.Bold);
        private static readonly XFont MetaFont = new XFont("Arial", 8.5, XFontStyleEx.Regular);
        private static readonly XFont MetaBoldFont = new XFont("Arial", 8.5, XFontStyleEx.Bold);
        private static readonly XFont TableHeaderFont = new XFont("Arial", 7.5, XFontStyleEx.Bold);
        private static readonly XFont TableRowFont = new XFont("Arial", 7.5, XFontStyleEx.Regular);
        private static readonly XFont FooterFont = new XFont("Arial", 8, XFontStyleEx.Regular);

        #region 1. Final Grantee Disbursement Ledger

        /// <summary>
        /// Generates the Final Grantee Disbursement Ledger in PDF (8.5" x 13" Landscape).
        /// </summary>
        public static void ExportFinalLedgerToPdf(
            string filePath,
            List<SelectionRow> items,
            int fiscalYear,
            decimal grantAmount)
        {
            if (items == null) items = new List<SelectionRow>();

            using (var doc = new PdfDocument())
            {
                doc.Info.Title = $"Final Grantee Disbursement Ledger - FY {fiscalYear}";
                doc.Info.Author = "City Social Welfare and Development Office";
                doc.Info.Subject = "Solo Parent Social Pension Disbursement Payroll";

                // Column Definitions (Total: 864 pt)
                var columns = new List<TableColumn>
                {
                    new TableColumn("No.", 26, XStringAlignment.Center),
                    new TableColumn("PhilSys / Valid ID", 78, XStringAlignment.Near),
                    new TableColumn("Grantee Full Name", 126, XStringAlignment.Near),
                    new TableColumn("Barangay", 80, XStringAlignment.Near),
                    new TableColumn("Complete Address", 136, XStringAlignment.Near),
                    new TableColumn("Contact No.", 72, XStringAlignment.Near),
                    new TableColumn("Birthdate", 54, XStringAlignment.Center),
                    new TableColumn("Grant (₱)", 52, XStringAlignment.Far),
                    new TableColumn("Date Received", 64, XStringAlignment.Center),
                    new TableColumn("Signature", 100, XStringAlignment.Center),
                    new TableColumn("Remarks", 76, XStringAlignment.Near)
                };

                double currentY = MarginTop;
                var page = AddLandscapeFolioPage(doc);
                XGraphics gfx = XGraphics.FromPdfPage(page);

                string rightMeta = $"Grant: ₱{grantAmount:N2}  •  Total Grantees: {items.Count:N0}";
                currentY = DrawMinimalHeader(
                    gfx,
                    "CITY SOCIAL WELFARE AND DEVELOPMENT OFFICE",
                    $"FINAL GRANTEE DISBURSEMENT LEDGER - FY {fiscalYear}",
                    rightMeta,
                    currentY);
                currentY = DrawTableHeader(gfx, columns, currentY);

                for (int i = 0; i < items.Count; i++)
                {
                    var r = items[i];
                    string philsys = !string.IsNullOrWhiteSpace(r.PhilSysCardNumber) && r.PhilSysCardNumber != "—" ? r.PhilSysCardNumber : "—";
                    string name = r.Name ?? string.Empty;
                    string brgy = r.Barangay ?? string.Empty;
                    string addr = !string.IsNullOrWhiteSpace(r.Address) ? r.Address.Trim() : (!string.IsNullOrWhiteSpace(r.Barangay) ? r.Barangay : "—");
                    string contact = !string.IsNullOrWhiteSpace(r.ContactNumberFormatted) && r.ContactNumberFormatted != "—" ? r.ContactNumberFormatted : (!string.IsNullOrWhiteSpace(r.ContactNumber) ? r.ContactNumber : "—");
                    string bday = !string.IsNullOrWhiteSpace(r.DateOfBirthFormatted) ? r.DateOfBirthFormatted : "—";
                    string amount = $"₱{grantAmount:N2}";

                    var values = new List<string>
                    {
                        (i + 1).ToString(),
                        philsys,
                        name,
                        brgy,
                        addr,
                        contact,
                        bday,
                        amount,
                        "", // Blank for physical date received
                        "", // Blank for physical signature
                        ""  // Blank for remarks
                    };

                    double neededHeight = MeasureAndDrawWrappedRow(gfx, columns, values, currentY, dryRun: true);
                    if (currentY + neededHeight > PageHeight - MarginBottom)
                    {
                        gfx.Dispose();
                        page = AddLandscapeFolioPage(doc);
                        gfx = XGraphics.FromPdfPage(page);
                        currentY = MarginTop;
                        currentY = DrawTableHeader(gfx, columns, currentY);
                    }

                    double drawnHeight = MeasureAndDrawWrappedRow(gfx, columns, values, currentY, dryRun: false);
                    currentY += drawnHeight;
                }

                gfx.Dispose();
                StampPageCounterFooter(doc);
                doc.Save(filePath);
            }
        }

        #endregion

        #region 2. City Education Scholarship Cross-Check Roster

        /// <summary>
        /// Generates the City Education Scholarship Cross-Check Roster in PDF (8.5" x 13" Landscape).
        /// </summary>
        public static void ExportCityEducRosterToPdf(
            string filePath,
            List<SelectionRow> items,
            int fiscalYear)
        {
            if (items == null) items = new List<SelectionRow>();

            using (var doc = new PdfDocument())
            {
                doc.Info.Title = $"Scholarship Cross-Check - FY {fiscalYear}";
                doc.Info.Author = "City Social Welfare and Development Office";
                doc.Info.Subject = "Solo Parent Dependent College Students Cross-Check List";

                // Column Definitions (Total: 864 pt)
                var columns = new List<TableColumn>
                {
                    new TableColumn("No.", 28, XStringAlignment.Center),
                    new TableColumn("Solo Parent Full Name", 140, XStringAlignment.Near),
                    new TableColumn("Barangay", 90, XStringAlignment.Near),
                    new TableColumn("Contact No.", 82, XStringAlignment.Near),
                    new TableColumn("Dependent Student Name", 140, XStringAlignment.Near),
                    new TableColumn("Relationship", 64, XStringAlignment.Near),
                    new TableColumn("Age", 34, XStringAlignment.Center),
                    new TableColumn("Date of Birth", 56, XStringAlignment.Center),
                    new TableColumn("Year Level / Education", 94, XStringAlignment.Near),
                    new TableColumn("City Educ Verification Remarks", 136, XStringAlignment.Near)
                };

                double currentY = MarginTop;
                var page = AddLandscapeFolioPage(doc);
                XGraphics gfx = XGraphics.FromPdfPage(page);

                currentY = DrawMinimalHeader(
                    gfx,
                    "CITY SOCIAL WELFARE AND DEVELOPMENT OFFICE",
                    "SCHOLARSHIP CROSS-CHECK",
                    null,
                    currentY);
                currentY = DrawTableHeader(gfx, columns, currentY);

                int count = 0;
                var filteredParents = items.Where(i => i.HasCollegeAgeDependent).ToList();
                foreach (var r in filteredParents)
                {
                    string contact = !string.IsNullOrWhiteSpace(r.ContactNumberFormatted) && r.ContactNumberFormatted != "—" ? r.ContactNumberFormatted : (!string.IsNullOrWhiteSpace(r.ContactNumber) ? r.ContactNumber : "—");
                    string brgy = r.Barangay ?? string.Empty;

                    var deps = (r.CollegeAgeDependents != null && r.CollegeAgeDependents.Count > 0)
                        ? r.CollegeAgeDependents
                        : new List<ChildDetailItem> { new ChildDetailItem { Name = "Dependent (Age 17–24)", Relationship = "Child", Age = 18, EducationalLevel = "College Level" } };

                    foreach (var child in deps)
                    {
                        count++;
                        string edu = !string.IsNullOrWhiteSpace(child.EducationalLevel) ? child.EducationalLevel : "—";
                        string rel = !string.IsNullOrWhiteSpace(child.Relationship) ? child.Relationship : "Child";
                        string bday = child.BirthdateFormatted;

                        var values = new List<string>
                        {
                            count.ToString(),
                            r.Name ?? string.Empty,
                            brgy,
                            contact,
                            child.Name ?? string.Empty,
                            rel,
                            child.Age.ToString(),
                            bday,
                            edu,
                            "" // Blank for City Educ remarks
                        };

                        double neededHeight = MeasureAndDrawWrappedRow(gfx, columns, values, currentY, dryRun: true);
                        if (currentY + neededHeight > PageHeight - MarginBottom)
                        {
                            gfx.Dispose();
                            page = AddLandscapeFolioPage(doc);
                            gfx = XGraphics.FromPdfPage(page);
                            currentY = MarginTop;
                            currentY = DrawTableHeader(gfx, columns, currentY);
                        }

                        double drawnHeight = MeasureAndDrawWrappedRow(gfx, columns, values, currentY, dryRun: false);
                        currentY += drawnHeight;
                    }
                }

                gfx.Dispose();
                StampPageCounterFooter(doc);
                doc.Save(filePath);
            }
        }

        #endregion

        #region 3. DSWD Pantawid Pamilya (4Ps) Cross-Check Roster

        /// <summary>
        /// Generates the DSWD Pantawid Pamilya (4Ps) Cross-Check Roster in PDF (8.5" x 13" Landscape).
        /// </summary>
        public static void ExportPantawid4PsRosterToPdf(
            string filePath,
            List<SelectionRow> items,
            int fiscalYear)
        {
            if (items == null) items = new List<SelectionRow>();

            using (var doc = new PdfDocument())
            {
                doc.Info.Title = $"DSWD Pantawid Pamilya (4Ps) Cross-Check - FY {fiscalYear}";
                doc.Info.Author = "City Social Welfare and Development Office";
                doc.Info.Subject = "DSWD Pantawid Pamilyang Pilipino Program Beneficiaries Cross-Check List";

                // Column Definitions (Total: 864 pt)
                var columns = new List<TableColumn>
                {
                    new TableColumn("No.", 26, XStringAlignment.Center),
                    new TableColumn("PhilSys ID", 78, XStringAlignment.Near),
                    new TableColumn("Solo Parent Full Name", 120, XStringAlignment.Near),
                    new TableColumn("Sex", 36, XStringAlignment.Center),
                    new TableColumn("Date of Birth", 56, XStringAlignment.Center),
                    new TableColumn("Barangay", 76, XStringAlignment.Near),
                    new TableColumn("Complete Address", 120, XStringAlignment.Near),
                    new TableColumn("Contact No.", 72, XStringAlignment.Near),
                    new TableColumn("Monthly Income", 64, XStringAlignment.Far),
                    new TableColumn("Minors (0–18)", 48, XStringAlignment.Center),
                    new TableColumn("Classification", 82, XStringAlignment.Near),
                    new TableColumn("DSWD Verification Status", 86, XStringAlignment.Near)
                };

                double currentY = MarginTop;
                var page = AddLandscapeFolioPage(doc);
                XGraphics gfx = XGraphics.FromPdfPage(page);

                currentY = DrawMinimalHeader(
                    gfx,
                    "CITY SOCIAL WELFARE AND DEVELOPMENT OFFICE",
                    "DSWD PANTAWID PAMILYA (4Ps) CROSS-CHECK",
                    null,
                    currentY);
                currentY = DrawTableHeader(gfx, columns, currentY);

                int count = 0;
                var filteredBeneficiaries = items.Where(i => i.IsPantawidBeneficiary).ToList();
                for (int i = 0; i < filteredBeneficiaries.Count; i++)
                {
                    var r = filteredBeneficiaries[i];
                    count++;

                    string philsys = !string.IsNullOrWhiteSpace(r.PhilSysCardNumber) && r.PhilSysCardNumber != "—" ? r.PhilSysCardNumber : "—";
                    string sex = !string.IsNullOrWhiteSpace(r.Sex) ? r.Sex : "Female";
                    string bday = !string.IsNullOrWhiteSpace(r.DateOfBirthFormatted) ? r.DateOfBirthFormatted : "—";
                    string brgy = r.Barangay ?? string.Empty;
                    string addr = !string.IsNullOrWhiteSpace(r.Address) ? r.Address.Trim() : (!string.IsNullOrWhiteSpace(r.Barangay) ? r.Barangay : "—");
                    string contact = !string.IsNullOrWhiteSpace(r.ContactNumberFormatted) && r.ContactNumberFormatted != "—" ? r.ContactNumberFormatted : (!string.IsNullOrWhiteSpace(r.ContactNumber) ? r.ContactNumber : "—");
                    string inc = r.IncomeLabel ?? $"₱{r.MonthlyIncome:N0}";
                    string circ = !string.IsNullOrWhiteSpace(r.Circumstance) ? r.Circumstance : "—";

                    var values = new List<string>
                    {
                        count.ToString(),
                        philsys,
                        r.Name ?? string.Empty,
                        sex,
                        bday,
                        brgy,
                        addr,
                        contact,
                        inc,
                        r.MinorDependentsCount.ToString(),
                        circ,
                        "" // Blank for DSWD Verification Status
                    };

                    double neededHeight = MeasureAndDrawWrappedRow(gfx, columns, values, currentY, dryRun: true);
                    if (currentY + neededHeight > PageHeight - MarginBottom)
                    {
                        gfx.Dispose();
                        page = AddLandscapeFolioPage(doc);
                        gfx = XGraphics.FromPdfPage(page);
                        currentY = MarginTop;
                        currentY = DrawTableHeader(gfx, columns, currentY);
                    }

                    double drawnHeight = MeasureAndDrawWrappedRow(gfx, columns, values, currentY, dryRun: false);
                    currentY += drawnHeight;
                }

                gfx.Dispose();
                StampPageCounterFooter(doc);
                doc.Save(filePath);
            }
        }

        #endregion

        #region Header, Table & Footer Helpers

        private static PdfPage AddLandscapeFolioPage(PdfDocument doc)
        {
            var page = doc.AddPage();
            page.Width = XUnit.FromPoint(PageWidth);
            page.Height = XUnit.FromPoint(PageHeight);
            page.Orientation = PdfSharp.PageOrientation.Landscape;
            return page;
        }

        private static double DrawMinimalHeader(XGraphics gfx, string office, string title, string rightMeta, double y)
        {
            var centerFormat = new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Near };

            gfx.DrawString(office.ToUpperInvariant(), HeaderLguFont, MaroonBrush, new XRect(MarginLeft, y, UsableWidth, 14), centerFormat);
            y += 14;
            gfx.DrawString(title.ToUpperInvariant(), TitleFont, TextDarkBrush, new XRect(MarginLeft, y, UsableWidth, 16), centerFormat);
            y += 18;

            gfx.DrawLine(HeaderRulePen, MarginLeft, y, MarginLeft + UsableWidth, y);
            y += 6;

            var leftFmt = new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Center };
            gfx.DrawString($"Date Created: {DateTime.Now:MMMM dd, yyyy}", MetaBoldFont, SubtitleBrush, new XRect(MarginLeft, y, UsableWidth * 0.5, 14), leftFmt);

            if (!string.IsNullOrWhiteSpace(rightMeta))
            {
                var rightFmt = new XStringFormat { Alignment = XStringAlignment.Far, LineAlignment = XLineAlignment.Center };
                gfx.DrawString(rightMeta, MetaFont, TextDarkBrush, new XRect(MarginLeft + UsableWidth * 0.5, y, UsableWidth * 0.5, 14), rightFmt);
            }

            y += 18;
            return y;
        }

        private static double DrawTableHeader(XGraphics gfx, List<TableColumn> columns, double y)
        {
            const double padX = 4.0;
            const double padY = 4.5;
            const double lineHeight = 10.0;
            const double minHeaderHeight = 22.0;

            var wrappedHeaders = new List<List<string>>(columns.Count);
            int maxLines = 1;

            for (int i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                double maxTextW = Math.Max(4.0, col.Width - (padX * 2));
                var lines = WrapText(gfx, col.Header, TableHeaderFont, maxTextW);
                wrappedHeaders.Add(lines);
                if (lines.Count > maxLines) maxLines = lines.Count;
            }

            double headerHeight = Math.Max(minHeaderHeight, (maxLines * lineHeight) + (padY * 2));
            double currentX = MarginLeft;

            // Keep maroon background on table headers
            gfx.DrawRectangle(TableHeaderBgBrush, MarginLeft, y, UsableWidth, headerHeight);

            for (int i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                var cellRect = new XRect(currentX, y, col.Width, headerHeight);
                gfx.DrawRectangle(BlackBorderPen, cellRect);

                var lines = wrappedHeaders[i];
                double totalTextH = lines.Count * lineHeight;
                double startY = y + ((headerHeight - totalTextH) / 2.0);

                var fmt = new XStringFormat
                {
                    Alignment = col.Alignment,
                    LineAlignment = XLineAlignment.Center
                };

                for (int l = 0; l < lines.Count; l++)
                {
                    if (!string.IsNullOrEmpty(lines[l]))
                    {
                        var lineRect = new XRect(currentX + padX, startY + (l * lineHeight), col.Width - (padX * 2), lineHeight);
                        gfx.DrawString(lines[l], TableHeaderFont, WhiteBrush, lineRect, fmt);
                    }
                }

                currentX += col.Width;
            }

            return y + headerHeight;
        }

        private static double MeasureAndDrawWrappedRow(
            XGraphics gfx,
            List<TableColumn> columns,
            List<string> values,
            double y,
            bool dryRun = false)
        {
            const double padX = 4.5;
            const double padY = 5.0;
            const double lineHeight = 10.5;
            const double minRowHeight = 24.0;

            var wrappedColumns = new List<List<string>>(columns.Count);
            int maxLines = 1;

            for (int i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                string val = (i < values.Count) ? (values[i] ?? string.Empty) : string.Empty;
                double maxTextW = Math.Max(4.0, col.Width - (padX * 2));
                var lines = WrapText(gfx, val, TableRowFont, maxTextW);
                wrappedColumns.Add(lines);
                if (lines.Count > maxLines) maxLines = lines.Count;
            }

            double rowHeight = Math.Max(minRowHeight, (maxLines * lineHeight) + (padY * 2));
            if (dryRun) return rowHeight;

            // Transparent rows with crisp black borders
            double currentX = MarginLeft;
            for (int i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                var cellRect = new XRect(currentX, y, col.Width, rowHeight);
                gfx.DrawRectangle(BlackBorderPen, cellRect);

                var lines = wrappedColumns[i];
                double totalTextH = lines.Count * lineHeight;
                double startY = y + ((rowHeight - totalTextH) / 2.0);

                var fmt = new XStringFormat
                {
                    Alignment = col.Alignment,
                    LineAlignment = XLineAlignment.Center
                };

                for (int l = 0; l < lines.Count; l++)
                {
                    if (!string.IsNullOrEmpty(lines[l]))
                    {
                        var lineRect = new XRect(currentX + padX, startY + (l * lineHeight), col.Width - (padX * 2), lineHeight);
                        gfx.DrawString(lines[l], TableRowFont, TextDarkBrush, lineRect, fmt);
                    }
                }

                currentX += col.Width;
            }

            return rowHeight;
        }

        private static List<string> WrapText(XGraphics gfx, string text, XFont font, double maxWidth)
        {
            var lines = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
            {
                lines.Add(string.Empty);
                return lines;
            }

            string[] paragraphs = text.Replace("\r\n", "\n").Split('\n');
            foreach (string para in paragraphs)
            {
                string[] words = para.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0)
                {
                    lines.Add(string.Empty);
                    continue;
                }

                var currentLine = new System.Text.StringBuilder();
                foreach (string word in words)
                {
                    string testLine = currentLine.Length == 0 ? word : currentLine + " " + word;
                    if (gfx.MeasureString(testLine, font).Width <= maxWidth)
                    {
                        if (currentLine.Length > 0) currentLine.Append(" ");
                        currentLine.Append(word);
                    }
                    else
                    {
                        if (currentLine.Length > 0)
                        {
                            lines.Add(currentLine.ToString());
                            currentLine.Clear();
                        }

                        if (gfx.MeasureString(word, font).Width > maxWidth)
                        {
                            string remaining = word;
                            while (remaining.Length > 0)
                            {
                                int take = remaining.Length;
                                while (take > 1 && gfx.MeasureString(remaining.Substring(0, take), font).Width > maxWidth)
                                {
                                    take--;
                                }
                                lines.Add(remaining.Substring(0, take));
                                remaining = remaining.Substring(take);
                            }
                        }
                        else
                        {
                            currentLine.Append(word);
                        }
                    }
                }
                if (currentLine.Length > 0)
                {
                    lines.Add(currentLine.ToString());
                }
            }
            if (lines.Count == 0) lines.Add(string.Empty);
            return lines;
        }

        private static void StampPageCounterFooter(PdfDocument doc)
        {
            int total = doc.PageCount;
            var rightFmt = new XStringFormat { Alignment = XStringAlignment.Far, LineAlignment = XLineAlignment.Center };

            for (int i = 0; i < total; i++)
            {
                var p = doc.Pages[i];
                using (var gfx = XGraphics.FromPdfPage(p))
                {
                    double y = PageHeight - MarginBottom + 10;
                    string pageText = $"{i + 1} of {total}";
                    gfx.DrawString(pageText, FooterFont, SubtitleBrush, new XRect(MarginLeft, y, UsableWidth, 12), rightFmt);
                }
            }
        }

        private class TableColumn
        {
            public string Header { get; }
            public double Width { get; }
            public XStringAlignment Alignment { get; }

            public TableColumn(string header, double width, XStringAlignment alignment)
            {
                Header = header;
                Width = width;
                Alignment = alignment;
            }
        }

        #endregion
    }
}
