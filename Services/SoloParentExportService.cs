using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SOLUM_UI.Models;

namespace SOLUM_UI.Services
{
    public static class SoloParentExportService
    {
        static SoloParentExportService()
        {
            try
            {
                PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;
            }
            catch { }
        }

        // New PDF template first, old name kept as a fallback.
        private static readonly string[] TemplateNames =
        {
            "CSWDO-066-2-Solo-Parent-Application-Form-REVISED.pdf",
            "CSWDO-066-Solo-Parent-Application-Form-REVISED.pdf",
            "CSWDO-066-2.pdf",
            "CSWDO-066.pdf",
            "SP-TEMPLATE.pdf",
            "SOLO-PARENTS-APPLICATION-FORM-TEMPLATE.pdf"
        };
        // ------------------------------------------------------------------
        // Reference page = the real template PDF page, measured in PDF points
        // (877.5 x 1311 pt, top-left origin). Every coordinate below is in
        // points. If the template page is ever a different size, everything is
        // scaled proportionally, so it still lands on the boxes.
        //
        // NOTE: SP-TEMPLATE.pdf has MediaBox = [0 8.04 877.5 1319.04], i.e. the
        // page does NOT start at y = 0. PdfSharp drew everything ~8 pt too low
        // because of that, so ExportToPdf shifts the drawing up by MediaBox.Y1.
        // ------------------------------------------------------------------
        private const double RefW = 877.5;
        private const double RefH = 1311.0;

        // ------------------------------------------------------------------
        // Section II detail fields. Each list is the property names the export
        // will look for on SoloParentRecord (first one that exists wins).
        // If your model uses a different name, add it to the matching list.
        // ------------------------------------------------------------------
        private static readonly string[] CauseOfDeathNames =
            { "CircumstanceA2Cause", "CauseOfDeath", "A2CauseOfDeath", "SpouseCauseOfDeath", "DeathCause" };
        private static readonly string[] DateOfDeathNames =
            { "CircumstanceA2Date", "DateOfDeath", "A2Date", "DeathDate", "SpouseDateOfDeath", "DateOfSpouseDeath" };
        private static readonly string[] DisabilityNames =
            { "CircumstanceA4Disability", "TypeOfDisability", "DisabilityType", "A4Disability", "SpouseDisability", "DisabilityOfSpouse" };
        private static readonly string[] SeparationNames =
            { "CircumstanceA5Period", "PeriodOfSeparation", "SeparationPeriod", "A5Period", "A5PeriodOfSeparation" };
        private static readonly string[] StayAbroadNames =
            { "CircumstanceBStayAbroad", "LengthOfStayAbroad", "LengthOfStay", "OfwLengthOfStay", "StayAbroad", "BLengthOfStay" };
        // A6 tick boxes (bool)
        private static readonly string[] NullityNames =
            { "CircumstanceA6Nullity", "DeclarationOfNullity", "A6Nullity", "IsNullity", "Nullity", "IsDeclarationOfNullity", "A6DeclarationOfNullity" };
        private static readonly string[] AnnulmentNames =
            { "CircumstanceA6Annulment", "AnnulmentOfMarriage", "A6Annulment", "IsAnnulment", "Annulment", "IsAnnulmentOfMarriage", "A6AnnulmentOfMarriage" };

        // A6 bullets: "• nullity   or   • annulment of marriage" (reference points).
        // Run the export with debugBoxes = true to see these and nudge if needed.
        private const double A6NullityX = 384.0;
        private const double A6AnnulmentX = 468.0;
        private const double A6BulletY = 479.5;

        /// <param name="debugBoxes">
        /// true = also draw a thin red outline of every field box so you can see
        /// exactly where text will land. Leave false for the real export.
        /// </param>
        public static void ExportToPdf(string outputPath, SoloParentRecord r, bool debugBoxes = false)
        {
            string templatePath = ResolveTemplatePath();
            using (var src = PdfReader.Open(templatePath, PdfDocumentOpenMode.Import))
            using (var doc = new PdfDocument())
            {
                doc.AddPage(src.Pages[0]);
                var page = doc.Pages[0];
                using (var gfx = XGraphics.FromPdfPage(page))
                {
                    bool isRevised = page.Width.Point < 700 ||
                                     Path.GetFileName(templatePath).IndexOf("CSWDO", StringComparison.OrdinalIgnoreCase) >= 0;
                    double refW = isRevised ? 612.0 : RefW;
                    double refH = isRevised ? 936.0 : RefH;

                    // Compensate for a MediaBox that doesn't start at y = 0 (legacy template: 8.04).
                    if (page.MediaBox.Y1 != 0)
                    {
                        gfx.TranslateTransform(0, -page.MediaBox.Y1);
                    }

                    var p = new Painter(gfx, page.Width.Point / refW, page.Height.Point / refH, debugBoxes, isRevised ? 4.0 : 15.0);
                    if (isRevised)
                        FillRevised(p, r);
                    else
                        Fill(p, r);
                }
                doc.Save(outputPath);
            }
        }
        // ------------------------------------------------------------------
        // Row geometry for Section I (points). For each row:
        //   Bottom      = the ruled line at the bottom of the row
        //   LabelBottom = lowest point of the printed field titles in that row
        // The value box of every cell is the free strip BELOW the title and
        // ABOVE the bottom line, so text never touches the title or the line.
        // ------------------------------------------------------------------
        private struct Row
        {
            public double Bottom, LabelBottom;
            public Row(double labelBottom, double bottom) { LabelBottom = labelBottom; Bottom = bottom; }
        }

        private static readonly Row R1 = new Row(229.5, 251.5);
        private static readonly Row R2 = new Row(269.0, 284.2);
        private static readonly Row R3 = new Row(301.2, 319.5);
        private static readonly Row R4 = new Row(337.0, 353.0);
        private static readonly Row R5 = new Row(370.0, 389.5);
        /// <summary>Value box for a cell between two vertical dividers.</summary>
        private static void Cell(Painter p, string text, double x0, double x1, Row row, double maxPt = 10, Align align = Align.Left)
        {
            double y = row.LabelBottom + 1.0;
            double h = (row.Bottom - 1.5) - y;
            p.Text(text, x0, y, x1 - x0, h, maxPt, align);
        }
        // ------------------------------------------------------------------
        // Field placement
        // ------------------------------------------------------------------
        private static void Fill(Painter p, SoloParentRecord r)
        {
            // ---- header -------------------------------------------------
            // Type of application is written in the blank after "FOR RENEWAL"
            // (blank = x 294 - 448.5, line at y = 145).
            string appType = string.Join(" / ", new[]
            {
                r.IsNewApplicant ? "NEW APPLICANT" : null,
                r.IsRenewal ? "FOR RENEWAL" : null
            }.Where(s => s != null));
            p.OnLine(appType, 294.0, 448.5, 145.0, 10, Align.Center);
            // date goes on the blank after "DATE OF THIS APPLICATION:" (x 667.5 - 830.5, line y = 145)
            p.OnLine(Date(r.DateOfApplication, "MM/dd/yyyy"), 667.5, 830.5, 145.0, 10, Align.Center);
            // ---- I. Identifying information, row 1 ----------------------
            Cell(p, r.Surname, 31.2, 224.8, R1, 11);
            Cell(p, r.FirstName, 224.8, 422.8, R1, 11);
            Cell(p, r.MiddleName, 422.8, 594.8, R1, 11);
            Cell(p, r.ExtensionName, 594.8, 675.2, R1, 10);
            Cell(p, r.CivilStatus, 675.2, 762.8, R1, 10);
            Cell(p, r.Sex, 762.8, 849.0, R1, 10);
            // ---- row 2 --------------------------------------------------
            Cell(p, Date(r.DateOfBirth, "MM/dd/yyyy"), 31.2, 189.2, R2, 9);
            Cell(p, r.PlaceOfBirth, 189.2, 364.8, R2, 9);
            Cell(p, Age(r.DateOfBirth), 364.8, 443.2, R2, 9);
            Cell(p, r.EducationalAttainment, 443.2, 646.2, R2, 9);
            Cell(p, r.PhilSysNumber, 646.2, 846.0, R2, 9);
            // ---- row 3 --------------------------------------------------
            Cell(p, r.Religion, 31.2, 189.2, R3, 10);
            Cell(p, r.Occupation, 189.2, 402.2, R3, 10);
            Cell(p, r.MonthlyIncome, 402.2, 534.8, R3, 10);
            // bullets "• Employed  • Self-Employed  • Not Employed"
            if (r.IsEmployed) p.Check(544.0, 310.9);
            if (r.IsSelfEmployed) p.Check(623.2, 310.9);
            if (r.IsNotEmployed) p.Check(727.4, 310.9);
            // ---- row 4 --------------------------------------------------
            Cell(p, r.Address, 31.2, 372.2, R4, 9);
            Cell(p, r.Barangay, 372.2, 594.8, R4, 9);
            Cell(p, r.ContactNumber, 594.8, 846.0, R4, 9);
            // ---- row 5 --------------------------------------------------
            Cell(p, r.EmergencyContactName, 31.2, 295.8, R5, 10);
            Cell(p, r.EmergencyRelationship, 295.8, 443.2, R5, 10);
            Cell(p, r.EmergencyAddress, 443.2, 666.8, R5, 10);
            Cell(p, r.EmergencyContactNumber, 666.8, 846.0, R5, 10);
            // ---- II. Circumstance code(s): draw a circle around them ----
            // (includes codes inferred from filled-in details, see BuildCircumstanceCodes)
            foreach (var code in SplitCodes(BuildCircumstanceCodes(r)))
                if (_codeCenters.TryGetValue(code, out var c))
                    p.Circle(c.X, c.Y, code.Length > 1 ? 13.0 : 11.0);   // true circle (rx = ry)
            // ---- II. "Specify" blanks (text sits on each underline) ------
            // A2  Cause of death ____ (x 168-226.5)   Date ____ (x 269.5-311)   line y = 484
            p.OnLineWrapped(Prop(r, CauseOfDeathNames), 168.0, 226.5, 484.0, 8);   // wraps to 2 lines if long
            p.OnLine(Prop(r, DateOfDeathNames), 269.5, 311.0, 484.0, 8, Align.Center);
            // A4  Type of disability ____ (x 171-253.5, line y = 551.5)
            p.OnLine(Prop(r, DisabilityNames), 171.0, 253.5, 551.5, 8, Align.Center);
            // A5  Period of separation ____ (x 488-576, line y = 451)
            p.OnLine(Prop(r, SeparationNames), 488.0, 576.0, 451.0, 8, Align.Center);
            // B   Length of stay abroad ____ (x 496-579, line y = 551.5)
            p.OnLine(Prop(r, StayAbroadNames), 496.0, 579.0, 551.5, 8, Align.Center);
            // A6  "• nullity   or   • annulment of marriage"
            if (Flag(r, NullityNames)) p.Check(A6NullityX, A6BulletY, 3.5);
            if (Flag(r, AnnulmentNames)) p.Check(A6AnnulmentX, A6BulletY, 3.5);
            // ---- III. Family composition (5 rows) ------------------------
            // row lines: 615.8 | 636.0 | 656.8 | 677.5 | 697.5 | 718.2
            double[] rowLines = { 615.8, 636.0, 656.8, 677.5, 697.5, 718.2 };
            // column lines: 31.2 | 261.2 | 305.5 | 348.8 | 440.0 | 520.5 | 616.2 | 765.8 | 845.5
            var members = r.FamilyMembers ?? new List<FamilyMember>();
            for (int i = 0; i < 5 && i < members.Count; i++)
            {
                var m = members[i];
                double y = rowLines[i] + 1.0;
                double h = rowLines[i + 1] - rowLines[i] - 2.0;
                p.Text(m.MemberName, 31.2, y, 261.2 - 31.2, h, 10);
                p.Text(FormatSex(m.Sex), 261.2, y, 305.5 - 261.2, h, 10, Align.Center);
                p.Text(FamilyAge(m), 305.5, y, 348.8 - 305.5, h, 10, Align.Center);
                p.Text(m.Birthdate, 348.8, y, 440.0 - 348.8, h, 9, Align.Center);
                p.Text(m.CivilStatus, 440.0, y, 520.5 - 440.0, h, 9, Align.Center);
                p.Text(m.Relationship, 520.5, y, 616.2 - 520.5, h, 9, Align.Center);
                p.Text(m.EducationEmployment, 616.2, y, 765.8 - 616.2, h, 9);
                p.Text(m.Income, 765.8, y, 845.5 - 765.8, h, 10, Align.Center);
            }
            // ---- IV / V. Free-text boxes (wrap + shrink to fit) ----------
            // IV box: y 744.5 - 782.5      V box: y 810.0 - 848.5
            p.Wrapped(r.NeedsAndProblems, 31.2, 746.0, 814.0, 35.5, 10);
            p.Wrapped(r.OtherIncomeSource, 31.2, 811.5, 814.0, 35.5, 10);
        }

        /// <summary>
        /// Field placement calibrated specifically for the official 8.5 x 13 inch (612 x 936 pt)
        /// CSWDO-066-2-Solo-Parent-Application-Form-REVISED.pdf document.
        /// </summary>
        private static void FillRevised(Painter p, SoloParentRecord r)
        {
            // ---- header -------------------------------------------------
            bool isNew = r.IsNewApplicant || (!r.IsRenewal && r.ApplicationType?.IndexOf("New", StringComparison.OrdinalIgnoreCase) >= 0);
            bool isRenew = r.IsRenewal || r.ApplicationType?.IndexOf("Renew", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isNew) p.Check(24.5, 99.0, 3.5);
            if (isRenew) p.Check(125.5, 99.0, 3.5);
            if (isRenew)
            {
                p.OnLine("RENEWAL", 208.0, 315.0, 102.5, 9.0, Align.Center);
            }
            DateTime appDate = r.DateOfApplication != DateTime.MinValue ? r.DateOfApplication : r.DateCreated;
            p.OnLine(Date(appDate, "MM/dd/yyyy"), 470.0, 585.0, 102.5, 9.0, Align.Center);

            // ---- I. Identifying information, row 1 ----------------------
            // Cell bounds: y 152.8 .. 177.6, label bottom ~ 162.3
            Row revR1 = new Row(162.3, 177.6);
            Cell(p, r.Surname, 21.8, 156.0, revR1, 9.5);
            Cell(p, r.FirstName, 156.6, 290.7, revR1, 9.5);
            Cell(p, r.MiddleName, 291.3, 410.8, revR1, 9.5);
            Cell(p, r.ExtensionName, 411.3, 466.9, revR1, 8.5, Align.Center);
            Cell(p, r.CivilStatus, 467.5, 527.7, revR1, 8.5, Align.Center);
            Cell(p, r.Sex, 528.2, 588.5, revR1, 8.5, Align.Center);

            // ---- row 2 --------------------------------------------------
            // Cell bounds: y 178.1 .. 200.1, label bottom ~ 187.7
            Row revR2 = new Row(187.7, 200.1);
            Cell(p, Date(r.DateOfBirth, "MM/dd/yyyy"), 21.8, 131.7, revR2, 8.5, Align.Center);
            Cell(p, r.PlaceOfBirth, 132.1, 253.5, revR2, 8.5);
            Cell(p, Age(r.DateOfBirth), 254.0, 304.9, revR2, 8.5, Align.Center);
            Cell(p, r.EducationalAttainment, 305.4, 446.7, revR2, 8.5);
            Cell(p, r.PhilSysNumber, 447.2, 588.5, revR2, 8.5, Align.Center);

            // ---- row 3 --------------------------------------------------
            // Cell bounds: y 200.5 .. 225.5, label bottom ~ 210.5
            Row revR3 = new Row(210.5, 225.5);
            Cell(p, r.Religion, 21.8, 131.7, revR3, 8.5);
            Cell(p, r.Occupation, 132.1, 276.5, revR3, 8.5);
            Cell(p, r.MonthlyIncome, 277.0, 368.7, revR3, 8.5, Align.Center);
            // Checkboxes: Employed, Self-Employed, Not Employed
            bool isEmp = r.IsEmployed || (!r.IsSelfEmployed && !r.IsNotEmployed && string.Equals(r.EmploymentStatusDisplay, "Employed", StringComparison.OrdinalIgnoreCase));
            bool isSelfEmp = r.IsSelfEmployed || (!r.IsEmployed && !r.IsNotEmployed && string.Equals(r.EmploymentStatusDisplay, "Self-Employed", StringComparison.OrdinalIgnoreCase));
            bool isNotEmp = r.IsNotEmployed || (!r.IsEmployed && !r.IsSelfEmployed && string.Equals(r.EmploymentStatusDisplay, "Not Employed", StringComparison.OrdinalIgnoreCase));
            if (isEmp) p.Check(376.5, 219.5, 3.0);
            if (isSelfEmp) p.Check(434.5, 219.5, 3.0);
            if (isNotEmp) p.Check(510.5, 219.5, 3.0);

            // ---- row 4 --------------------------------------------------
            // Cell bounds: y 226.1 .. 248.4, label bottom ~ 235.6
            Row revR4 = new Row(235.6, 248.4);
            Cell(p, r.Address, 21.8, 255.3, revR4, 8.0);
            Cell(p, r.Barangay, 255.8, 410.7, revR4, 8.5);
            Cell(p, r.ContactNumber, 411.3, 588.5, revR4, 8.5, Align.Center);

            // ---- row 5 --------------------------------------------------
            // Cell bounds: y 248.9 .. 273.6, label bottom ~ 258.5
            Row revR5 = new Row(258.5, 273.6);
            Cell(p, r.EmergencyContactName, 21.8, 205.7, revR5, 8.5);
            Cell(p, r.EmergencyRelationship, 206.2, 304.9, revR5, 8.5, Align.Center);
            Cell(p, r.EmergencyAddress, 305.4, 460.9, revR5, 8.0);
            Cell(p, r.EmergencyContactNumber, 461.4, 588.5, revR5, 8.5, Align.Center);

            // ---- II. Circumstance code(s): draw a circle around them ----
            foreach (var code in SplitCodes(BuildCircumstanceCodes(r)))
                if (_revisedCodeCenters.TryGetValue(code, out var c))
                    p.Circle(c.X, c.Y, code.Length > 1 ? 9.5 : 8.0);

            // ---- II. "Specify" blanks (text sits on each underline) ------
            p.OnLineWrapped(Prop(r, CauseOfDeathNames), 118.0, 160.0, 343.0, 6.5);
            p.OnLine(Prop(r, DateOfDeathNames), 188.0, 225.0, 343.0, 6.5, Align.Center);
            p.OnLine(Prop(r, DisabilityNames), 125.0, 225.0, 390.0, 6.5, Align.Center);
            p.OnLine(Prop(r, SeparationNames), 340.0, 422.0, 320.0, 6.5, Align.Center);
            p.OnLine(Prop(r, StayAbroadNames), 352.0, 422.0, 390.0, 6.5, Align.Center);

            if (Flag(r, NullityNames)) p.Check(263.5, 334.0, 2.5);
            if (Flag(r, AnnulmentNames)) p.Check(326.0, 334.0, 2.5);

            // ---- III. Family composition (5 rows) ------------------------
            double[] rowLines = { 430.5, 444.4, 458.4, 472.3, 486.2, 500.0 };
            var members = r.FamilyMembers ?? new List<FamilyMember>();
            for (int i = 0; i < 5 && i < members.Count; i++)
            {
                var m = members[i];
                double y = rowLines[i] + 1.0;
                double h = rowLines[i + 1] - rowLines[i] - 2.0;
                p.Text(m.MemberName, 21.8, y, 182.2 - 21.8, h, 8.5);
                p.Text(FormatSex(m.Sex), 182.8, y, 212.8 - 182.8, h, 8.5, Align.Center);
                p.Text(FamilyAge(m), 213.3, y, 242.7 - 213.3, h, 8.5, Align.Center);
                p.Text(m.Birthdate, 243.3, y, 306.4 - 243.3, h, 8.0, Align.Center);
                p.Text(m.CivilStatus, 306.9, y, 362.7 - 306.9, h, 8.0, Align.Center);
                p.Text(m.Relationship, 363.2, y, 429.7 - 363.2, h, 8.5, Align.Center);
                p.Text(m.EducationEmployment, 430.1, y, 534.1 - 430.1, h, 8.0);
                p.Text(m.Income, 534.6, y, 589.3 - 534.6, h, 8.5, Align.Center);
            }

            // ---- IV / V. Free-text boxes (wrap + shrink to fit) ----------
            p.Wrapped(r.NeedsAndProblems, 22.1, 518.5, 566.0, 25.0, 8.0);
            p.Wrapped(r.OtherIncomeSource, 22.1, 564.0, 566.0, 25.0, 8.0);
        }

        // Centre of each code in Section II for Revised Template (612 x 936 pt)
        private static readonly Dictionary<string, XPoint> _revisedCodeCenters =
            new Dictionary<string, XPoint>(StringComparer.OrdinalIgnoreCase)
        {
            { "A1", new XPoint(34.5,  305.5) }, { "A2", new XPoint(34.5,  328.5) },
            { "A3", new XPoint(34.5,  351.5) }, { "A4", new XPoint(34.5,  375.5) },
            { "A5", new XPoint(241.5, 305.5) }, { "A6", new XPoint(241.5, 328.5) },
            { "A7", new XPoint(241.5, 351.5) }, { "B",  new XPoint(241.5, 375.5) },
            { "C",  new XPoint(436.5, 305.5) }, { "D",  new XPoint(436.5, 328.5) },
            { "E",  new XPoint(436.5, 351.5) }, { "F",  new XPoint(436.5, 375.5) },
        };
        // Centre of each code in Section II (points)
        private static readonly Dictionary<string, XPoint> _codeCenters =
            new Dictionary<string, XPoint>(StringComparer.OrdinalIgnoreCase)
        {
            { "A1", new XPoint(49.2,  436.2) }, { "A2", new XPoint(49.2,  470.2) },
            { "A3", new XPoint(49.2,  503.3) }, { "A4", new XPoint(49.2,  537.5) },
            { "A5", new XPoint(346.8, 436.2) }, { "A6", new XPoint(346.8, 470.0) },
            { "A7", new XPoint(346.8, 503.3) }, { "B",  new XPoint(346.4, 537.8) },
            { "C",  new XPoint(627.2, 436.2) }, { "D",  new XPoint(627.4, 470.2) },
            { "E",  new XPoint(627.0, 503.6) }, { "F",  new XPoint(626.5, 537.8) },
        };

        private static IEnumerable<string> SplitCodes(string s) =>
            (s ?? "").Split(new[] { ",", ";", " ", "/" }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(x => x.Trim().TrimEnd('.'));

        /// <summary>
        /// Codes to circle = the circumstances the user ticked PLUS any code whose
        /// detail was filled in (applicants often write the detail but forget to
        /// tick the circumstance), so the printed form never shows details with
        /// no circled code.
        /// </summary>
        private static string BuildCircumstanceCodes(SoloParentRecord r)
        {
            var codes = new List<string>();
            if (r.CircumstanceA1) codes.Add("A1");
            if (r.CircumstanceA2) codes.Add("A2");
            if (r.CircumstanceA3) codes.Add("A3");
            if (r.CircumstanceA4) codes.Add("A4");
            if (r.CircumstanceA5) codes.Add("A5");
            if (r.CircumstanceA6) codes.Add("A6");
            if (r.CircumstanceA7) codes.Add("A7");
            if (r.CircumstanceB) codes.Add("B");
            if (r.CircumstanceC) codes.Add("C");
            if (r.CircumstanceD) codes.Add("D");
            if (r.CircumstanceE) codes.Add("E");
            if (r.CircumstanceF) codes.Add("F");

            void Infer(string code, bool hasDetail)
            {
                if (hasDetail && !codes.Contains(code, StringComparer.OrdinalIgnoreCase))
                    codes.Add(code);
            }

            Infer("A2", Has(Prop(r, CauseOfDeathNames)) || Has(Prop(r, DateOfDeathNames)));
            Infer("A4", Has(Prop(r, DisabilityNames)));
            Infer("A5", Has(Prop(r, SeparationNames)));
            Infer("A6", Flag(r, NullityNames) || Flag(r, AnnulmentNames));
            Infer("B", Has(Prop(r, StayAbroadNames)));

            return string.Join(",", codes);
        }
        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
        private static bool Has(string s) => !string.IsNullOrWhiteSpace(s);

        private static string Date(DateTime d, string fmt) =>
            d == DateTime.MinValue ? "" : d.ToString(fmt);

        private static string Age(DateTime dob)
        {
            if (dob == DateTime.MinValue) return "";
            var today = DateTime.Today;
            int age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;
            return age.ToString();
        }

        private static string FamilyAge(FamilyMember m)
        {
            if (m == null) return "";
            if (!string.IsNullOrWhiteSpace(m.Age)) return m.Age;
            return FamilyAge(m.Birthdate);
        }

        private static string FamilyAge(string birthdate) =>
            DateTime.TryParse(birthdate, out var d) ? Age(d) : "";

        private static string FormatSex(string sex)
        {
            if (string.IsNullOrWhiteSpace(sex)) return "";
            string s = sex.Trim();
            if (s.StartsWith("F", StringComparison.OrdinalIgnoreCase)) return "F";
            if (s.StartsWith("M", StringComparison.OrdinalIgnoreCase)) return "M";
            return s.Length > 0 ? s.Substring(0, 1).ToUpperInvariant() : "";
        }
        /// <summary>
        /// Reads the first property of the record that exists under one of the given names
        /// (so this compiles even if your model uses a slightly different name).
        /// DateTime values are formatted MM/dd/yyyy; DateTime.MinValue / null = "".
        /// </summary>
        private static string Prop(SoloParentRecord r, params string[] names)
        {
            foreach (var n in names)
            {
                var pi = typeof(SoloParentRecord).GetProperty(n);
                if (pi == null) continue;
                var v = pi.GetValue(r);
                if (v == null) return "";
                if (v is DateTime dt) return dt == DateTime.MinValue ? "" : dt.ToString("MM/dd/yyyy");
                return v.ToString() ?? "";
            }
            return "";
        }

        /// <summary>
        /// Reads a tick-box property (bool, bool?, or text like "true"/"yes"/"1")
        /// under the first matching name. Missing property = false.
        /// </summary>
        private static bool Flag(SoloParentRecord r, params string[] names)
        {
            foreach (var n in names)
            {
                var pi = typeof(SoloParentRecord).GetProperty(n);
                if (pi == null) continue;
                var v = pi.GetValue(r);
                if (v == null) return false;
                if (v is bool b) return b;
                var s = v.ToString()?.Trim().ToLowerInvariant();
                return s == "true" || s == "yes" || s == "1" || s == "y" || s == "x";
            }
            return false;
        }

        private static string ResolveTemplatePath()
        {
            string exeDir = Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] subfolders = { "Templates", "Assets", "" };

            // Check each template by priority (CSWDO-066 revised template first).
            // Outer loop iterates through template names in order of preference so that
            // a higher-priority template in any location takes precedence over a fallback template.
            foreach (var n in TemplateNames)
            {
                // 1. Output directory
                foreach (var f in subfolders)
                {
                    string candidate = string.IsNullOrEmpty(f) ? Path.Combine(exeDir, n) : Path.Combine(exeDir, f, n);
                    if (File.Exists(candidate)) return candidate;
                }

                // 2. Base directory and parent directories (source / repo directory when running under VS/debugger)
                string probeDir = baseDir;
                for (int up = 0; up < 5 && !string.IsNullOrEmpty(probeDir); up++)
                {
                    foreach (var f in subfolders)
                    {
                        string probe = string.IsNullOrEmpty(f) ? Path.Combine(probeDir, n) : Path.Combine(probeDir, f, n);
                        if (File.Exists(probe)) return probe;
                    }
                    probeDir = Path.GetDirectoryName(probeDir.TrimEnd(Path.DirectorySeparatorChar));
                }
            }

            // 3. Fallback: match any file containing CSWDO and 066
            foreach (var folder in new[] { "Templates", "Assets" })
            {
                string probeDir = baseDir;
                for (int up = 0; up < 5 && !string.IsNullOrEmpty(probeDir); up++)
                {
                    string dir = Path.Combine(probeDir, folder);
                    if (Directory.Exists(dir))
                    {
                        var match = Directory.GetFiles(dir, "*CSWDO*066*.pdf", SearchOption.TopDirectoryOnly).FirstOrDefault();
                        if (match != null) return match;
                    }
                    probeDir = Path.GetDirectoryName(probeDir.TrimEnd(Path.DirectorySeparatorChar));
                }
            }

            throw new FileNotFoundException(
                $"Template not found: {TemplateNames[0]}. " +
                "Put it in Templates\\ or Assets\\ and set 'Copy to Output Directory' = 'Copy if newer'.");
        }

        private enum Align { Left, Center }
        // ------------------------------------------------------------------
        // Drawing helper: scales reference points -> actual page points,
        // fits text inside its box (shrinks, then trims with an ellipsis).
        // All text is placed by BASELINE (not by XStringFormats.Center) so the
        // vertical position is exact and the same on every PdfSharp version.
        // ------------------------------------------------------------------
        private sealed class Painter
        {
            private const double MinPt = 6.0;
            private readonly double PadLeft = 15.0;   // left-aligned text starts this far inside the cell
            private const double PadRight = 3.0;
            private const double CapHalf = 0.36;   // Arial cap-height / 2, as a fraction of font size
            private static readonly XColor InkColor = XColors.Black;
            private static readonly XBrush Ink = new XSolidBrush(InkColor);
            private readonly XGraphics _g;
            private readonly double _sx, _sy;
            private readonly bool _debug;
            private readonly XPen _markPen = new XPen(InkColor, 1.4);
            private readonly XPen _dbgPen = new XPen(XColors.Red, 0.3);

            public Painter(XGraphics g, double sx, double sy, bool debug, double padLeft = 15.0)
            { _g = g; _sx = sx; _sy = sy; _debug = debug; PadLeft = padLeft; }

            private XRect Rect(double x, double y, double w, double h) =>
                new XRect(x * _sx, y * _sy, w * _sx, h * _sy);

            private static XFont Font(double pt) => new XFont("Arial", pt, XFontStyleEx.Regular);
            /// <summary>Shrinks the font until the text fits, then trims with an ellipsis if still too wide.</summary>
            private XFont Fit(ref string text, double startPt, double avail)
            {
                double size = Math.Max(startPt, MinPt);
                XFont f = Font(size);
                while (size > MinPt && _g.MeasureString(text, f).Width > avail)
                {
                    size -= 0.25;
                    f = Font(size);
                }
                while (text.Length > 1 && _g.MeasureString(text, f).Width > avail)
                    text = text.Substring(0, text.Length - 2).TrimEnd() + "…";
                return f;
            }
            /// <summary>Single line, vertically centred in its box, shrinks to fit the box.</summary>
            public void Text(string text, double x, double y, double w, double h, double maxPt,
                             Align align = Align.Left)
            {
                var rect = Rect(x, y, w, h);
                if (_debug) _g.DrawRectangle(_dbgPen, rect);
                if (string.IsNullOrWhiteSpace(text)) return;
                text = text.Trim();
                double padL = align == Align.Left ? PadLeft : PadRight;
                double avail = rect.Width - padL - PadRight;
                // never let the font be taller than the box
                double start = Math.Min(maxPt, rect.Height / 1.2);
                XFont f = Fit(ref text, start, avail);

                double tw = _g.MeasureString(text, f).Width;
                double tx = align == Align.Center ? rect.X + (rect.Width - tw) / 2 : rect.X + padL;
                double baseline = rect.Y + rect.Height / 2 + f.Size * CapHalf;
                _g.DrawString(text, f, Ink, new XPoint(tx, baseline), XStringFormats.BaseLineLeft);
            }
            /// <summary>
            /// Text that sits ON a printed underline: x0..x1 = the blank, yLine = y of the line.
            /// The baseline is a few points above the line so it never touches or crosses it.
            /// </summary>
            public void OnLine(string text, double x0, double x1, double yLine, double maxPt,
                               Align align = Align.Left)
            {
                var rect = Rect(x0, yLine - 14.0, x1 - x0, 14.0);
                if (_debug) _g.DrawRectangle(_dbgPen, rect);
                if (string.IsNullOrWhiteSpace(text)) return;
                text = text.Trim();
                double avail = rect.Width - 2.0;
                XFont f = Fit(ref text, maxPt, avail);

                double tw = _g.MeasureString(text, f).Width;
                double tx = align == Align.Center ? rect.X + (rect.Width - tw) / 2 : rect.X + 1.0;
                double baseline = (yLine - 3.5) * _sy;   // 3.5 pt above the line
                _g.DrawString(text, f, Ink, new XPoint(tx, baseline), XStringFormats.BaseLineLeft);
            }

            /// <summary>
            /// Like OnLine, but if the text doesn't fit on one line (at >= minSinglePt) it wraps
            /// onto up to maxLines lines stacked upward from the underline. Only if it still
            /// doesn't fit at the smallest font is the last line trimmed with an ellipsis.
            /// </summary>
            public void OnLineWrapped(string text, double x0, double x1, double yLine, double maxPt,
                                      int maxLines = 3, double minSinglePt = 7.0)
            {
                var rect = Rect(x0, yLine - 14.0, x1 - x0, 14.0);
                if (_debug) _g.DrawRectangle(_dbgPen, rect);
                if (string.IsNullOrWhiteSpace(text)) return;
                text = text.Trim();
                double avail = rect.Width - 2.0;
                double bottomBase = (yLine - 3.5) * _sy;

                // 1) single line, if it fits without getting too small
                for (double size = maxPt; size >= minSinglePt; size -= 0.25)
                {
                    var f = Font(size);
                    double tw = _g.MeasureString(text, f).Width;
                    if (tw <= avail)
                    {
                        _g.DrawString(text, f, Ink, new XPoint(rect.X + (rect.Width - tw) / 2, bottomBase),
                                      XStringFormats.BaseLineLeft);
                        return;
                    }
                }

                // 2) expand one line at a time: try 2 lines, then 3 lines, ...
                //    Each step first tries the normal font (shrinking only a little)
                //    before moving on to the next number of lines.
                const double MinWrapPt = 6.5;
                for (int n = 2; n <= maxLines; n++)
                {
                    for (double size = maxPt; size >= MinWrapPt; size -= 0.25)
                    {
                        var f = Font(size);
                        var lines = WrapToWidth(text, f, avail);
                        if (lines.Count <= n)
                        {
                            DrawStacked(lines, f, rect, avail, bottomBase);
                            return;
                        }
                    }
                }

                // 3) still too long: use all maxLines at the smallest font, trim the last line with "…"
                {
                    var f = Font(MinWrapPt);
                    var lines = WrapToWidth(text, f, avail);
                    if (lines.Count > maxLines)
                    {
                        lines = lines.Take(maxLines).ToList();
                        string t = lines[maxLines - 1];
                        while (t.Length > 1 && _g.MeasureString(t + "…", f).Width > avail)
                            t = t.Substring(0, t.Length - 1);
                        lines[maxLines - 1] = t.TrimEnd() + "…";
                    }
                    DrawStacked(lines, f, rect, avail, bottomBase);
                }
            }

            /// <summary>Draws centred lines stacked upward so the LAST line sits on the underline.</summary>
            private void DrawStacked(List<string> lines, XFont f, XRect rect, double avail, double bottomBase)
            {
                double lh = f.Size * 1.05;
                for (int i = 0; i < lines.Count; i++)
                {
                    double tw = _g.MeasureString(lines[i], f).Width;
                    double baseline = bottomBase - (lines.Count - 1 - i) * lh;
                    _g.DrawString(lines[i], f, Ink,
                        new XPoint(rect.X + (rect.Width - tw) / 2, baseline), XStringFormats.BaseLineLeft);
                }
            }

            /// <summary>Word-wraps to a width; a single word wider than the width is broken by characters.</summary>
            private List<string> WrapToWidth(string text, XFont f, double maxWidth)
            {
                var result = new List<string>();
                string line = "";
                foreach (var word in text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string test = line.Length == 0 ? word : line + " " + word;
                    if (_g.MeasureString(test, f).Width <= maxWidth) { line = test; continue; }

                    if (line.Length > 0) { result.Add(line); line = ""; }

                    string rest = word;
                    while (_g.MeasureString(rest, f).Width > maxWidth && rest.Length > 1)
                    {
                        int n = rest.Length;
                        while (n > 1 && _g.MeasureString(rest.Substring(0, n), f).Width > maxWidth) n--;
                        result.Add(rest.Substring(0, n));
                        rest = rest.Substring(n);
                    }
                    line = rest;
                }
                if (line.Length > 0) result.Add(line);
                return result;
            }

            /// <summary>Multi-line text that wraps and shrinks to fit the box height.</summary>
            public void Wrapped(string text, double x, double y, double w, double h, double maxPt)
            {
                var rect = Rect(x, y, w, h);
                if (_debug) _g.DrawRectangle(_dbgPen, rect);
                if (string.IsNullOrWhiteSpace(text)) return;

                double avail = rect.Width - PadLeft - PadRight;
                for (double size = maxPt; size >= MinPt; size -= 0.5)
                {
                    var f = Font(size);
                    var lines = Wrap(text, f, avail);
                    double lh = f.GetHeight();
                    int maxLines = Math.Max(1, (int)((rect.Height - 2) / lh));
                    if (lines.Count <= maxLines || size - 0.5 < MinPt)
                    {
                        for (int i = 0; i < lines.Count && i < maxLines; i++)
                        {
                            double baseline = rect.Y + 1 + i * lh + lh * 0.8;
                            _g.DrawString(lines[i], f, Ink,
                                new XPoint(rect.X + PadLeft, baseline), XStringFormats.BaseLineLeft);
                        }
                        return;
                    }
                }
            }

            private List<string> Wrap(string text, XFont f, double maxWidth)
            {
                var result = new List<string>();
                foreach (var para in text.Replace("\r", "").Split('\n'))
                {
                    string line = "";
                    foreach (var word in para.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string test = line.Length == 0 ? word : line + " " + word;
                        if (line.Length > 0 && _g.MeasureString(test, f).Width > maxWidth)
                        {
                            result.Add(line);
                            line = word;
                        }
                        else line = test;
                    }
                    result.Add(line);
                }
                return result;
            }
            /// <summary>Draws an "X" centred on a bullet/checkbox (centre in reference points).</summary>
            public void Check(double cx, double cy, double half = 4.5)
            {
                double x1 = (cx - half) * _sx, x2 = (cx + half) * _sx;
                double y1 = (cy - half) * _sy, y2 = (cy + half) * _sy;
                if (_debug) _g.DrawRectangle(_dbgPen, new XRect(x1, y1, x2 - x1, y2 - y1));
                _g.DrawLine(_markPen, x1, y1, x2, y2);
                _g.DrawLine(_markPen, x1, y2, x2, y1);
            }
            /// <summary>Draws a true circle around a code (centre + radius in reference points).</summary>
            public void Circle(double cx, double cy, double radius)
            {
                double d = radius * 2 * Math.Min(_sx, _sy);   // same width and height = perfect circle
                _g.DrawEllipse(_markPen, new XRect(cx * _sx - d / 2, cy * _sy - d / 2, d, d));
            }
        }
    }
}