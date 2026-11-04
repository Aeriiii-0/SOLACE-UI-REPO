using System;
using System.Collections.Generic;
using System.Linq;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SOLUM_UI.Models.Api;

namespace SOLUM_UI.Services
{
    public static class AnalyticsPdfExportService
    {
        private const int PageWidth = 612;
        private const int PageHeight = 792;
        private const int MarginTop = 40;
        private const int MarginLeft = 40;
        private const int MarginRight = 40;
        private const int MarginBottom = 40;

        public static void ExportAnalyticsReportToPdf(
            string filePath,
            MonthlyAnalyticsDto analyticsData,
            string municipality = "Biñan City",
            string region = "CALABARZON (IV-A)",
            string focalPersonName = "")
        {
            if (analyticsData == null)
                throw new ArgumentNullException(nameof(analyticsData), "Analytics data cannot be null");

            using (var document = new PdfDocument())
            {
                var page = document.AddPage(new PdfSharp.Pdf.PdfPage { Width = PageWidth, Height = PageHeight });
                using (var gfx = XGraphics.FromPdfPage(page))
                {
                    int yPosition = MarginTop;

                    // Draw header
                    yPosition = DrawHeader(gfx, municipality, region, focalPersonName, yPosition);

                    // Draw analytics sections
                    yPosition = DrawAnalyticsSection(gfx, "AGE BRACKET", analyticsData.AgeBrackets, yPosition);
                    yPosition = DrawAnalyticsSection(gfx, "CIVIL STATUS", analyticsData.CivilStatus, yPosition);
                    yPosition = DrawAnalyticsSection(gfx, "EMPLOYMENT STATUS", analyticsData.EmploymentStatus, yPosition);
                    yPosition = DrawAnalyticsSection(gfx, "MONTHLY INCOME", analyticsData.MonthlyIncomeBrackets, yPosition);
                    yPosition = DrawAnalyticsSection(gfx, "EDUCATIONAL ATTAINMENT", analyticsData.EducationalAttainment, yPosition);

                    // Summary statistics
                    if (yPosition + 80 > PageHeight - MarginBottom)
                    {
                        page = document.AddPage(new PdfSharp.Pdf.PdfPage { Width = PageWidth, Height = PageHeight });
                        gfx.Dispose();
                        var newGfx = XGraphics.FromPdfPage(page);
                        DrawSummaryStatistics(newGfx, analyticsData, MarginTop);
                    }
                    else
                    {
                        DrawSummaryStatistics(gfx, analyticsData, yPosition);
                    }
                }

                document.Save(filePath);
            }
        }

        private static int DrawHeader(XGraphics gfx, string municipality, string region, string focalPersonName, int yPosition)
        {
            var titleFont = new XFont("Segoe UI", 14, XFontStyle.Bold);
            var subtitleFont = new XFont("Segoe UI", 11, XFontStyle.Regular);
            var infoFont = new XFont("Segoe UI", 10, XFontStyle.Regular);
            var maroonBrush = new XSolidBrush(XColor.FromArgb(107, 41, 67)); // Maroon #6B2943
            var blackBrush = XBrushes.Black;

            // Title
            gfx.DrawString("SOLO PARENTS ANALYTICS REPORT", titleFont, maroonBrush, MarginLeft, yPosition);
            yPosition += 25;

            // Subtitle
            gfx.DrawString($"Municipality: {municipality}", subtitleFont, blackBrush, MarginLeft, yPosition);
            yPosition += 18;

            gfx.DrawString($"Region: {region}", infoFont, blackBrush, MarginLeft, yPosition);
            yPosition += 16;

            if (!string.IsNullOrEmpty(focalPersonName))
            {
                gfx.DrawString($"Focal Person: {focalPersonName}", infoFont, blackBrush, MarginLeft, yPosition);
                yPosition += 16;
            }

            gfx.DrawString($"Report Date: {DateTime.Today:MMMM dd, yyyy}", infoFont, blackBrush, MarginLeft, yPosition);
            yPosition += 20;

            // Horizontal line
            gfx.DrawLine(new XPen(maroonBrush), MarginLeft, yPosition, PageWidth - MarginRight, yPosition);
            yPosition += 15;

            return yPosition;
        }

        private static int DrawAnalyticsSection(XGraphics gfx, string sectionTitle, Dictionary<string, int> data, int yPosition)
        {
            if (data == null || data.Count == 0)
                return yPosition;

            var sectionFont = new XFont("Segoe UI", 11, XFontStyle.Bold);
            var rowFont = new XFont("Segoe UI", 10, XFontStyle.Regular);
            var maroonBrush = new XSolidBrush(XColor.FromArgb(107, 41, 67));
            var blackBrush = XBrushes.Black;

            // Check if we need a new page
            if (yPosition + 20 + (data.Count * 14) > PageHeight - MarginBottom)
            {
                return yPosition; // Would need new page - handled by caller
            }

            // Section title
            gfx.DrawString(sectionTitle, sectionFont, maroonBrush, MarginLeft, yPosition);
            yPosition += 16;

            // Data rows
            int total = 0;
            foreach (var kvp in data.OrderByDescending(x => x.Value))
            {
                total += kvp.Value;
                string line = $"  {kvp.Key}: {kvp.Value}";
                gfx.DrawString(line, rowFont, blackBrush, MarginLeft, yPosition);
                yPosition += 14;
            }

            // Subtotal
            var boldFont = new XFont("Segoe UI", 10, XFontStyle.Bold);
            gfx.DrawString($"  SUBTOTAL: {total}", boldFont, maroonBrush, MarginLeft, yPosition);
            yPosition += 16;

            return yPosition;
        }

        private static void DrawSummaryStatistics(XGraphics gfx, MonthlyAnalyticsDto data, int yPosition)
        {
            var titleFont = new XFont("Segoe UI", 12, XFontStyle.Bold);
            var rowFont = new XFont("Segoe UI", 10, XFontStyle.Regular);
            var maroonBrush = new XSolidBrush(XColor.FromArgb(107, 41, 67));
            var blackBrush = XBrushes.Black;

            gfx.DrawString("SUMMARY STATISTICS", titleFont, maroonBrush, MarginLeft, yPosition);
            yPosition += 18;

            int totalRecords = (data.AgeBrackets?.Sum(x => x.Value) ?? 0);
            int activeCount = data.ActiveSoloParents ?? 0;
            int inactiveCount = data.InactiveSoloParents ?? 0;

            var summaryLines = new List<(string label, int value)>
            {
                ("Total Solo Parents", totalRecords),
                ("Active Records", activeCount),
                ("Inactive Records", inactiveCount),
            };

            foreach (var (label, value) in summaryLines)
            {
                string line = $"  {label}: {value}";
                gfx.DrawString(line, rowFont, blackBrush, MarginLeft, yPosition);
                yPosition += 14;
            }

            // Footer
            yPosition += 20;
            var footerFont = new XFont("Segoe UI", 8, XFontStyle.Italic);
            var grayBrush = new XSolidBrush(XColor.FromArgb(128, 128, 128));
            gfx.DrawString($"Report generated on {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}", footerFont, grayBrush, MarginLeft, PageHeight - 30);
        }
    }
}
