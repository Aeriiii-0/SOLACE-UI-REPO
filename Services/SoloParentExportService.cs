using System;
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
        private const string TemplateName = "SOLO-PARENTS-APPLICATION-FORM-TEMPLATE.pdf";

        public static void ExportToPdf(string outputPath, SoloParentRecord r)
        {
            string templatePath = ResolveTemplatePath();

            using (var src = PdfReader.Open(templatePath, PdfDocumentOpenMode.Import))
            using (var doc = new PdfDocument())
            {
                doc.AddPage(src.Pages[0]);

                var page     = doc.Pages[0];
                var gfx      = XGraphics.FromPdfPage(page);
                var font     = new XFont("Arial", 8,  XFontStyle.Regular);
                var fontName = new XFont("Arial", 13, XFontStyle.Regular); // names — prominent
                var fontSm   = new XFont("Arial", 7,  XFontStyle.Regular);
                var brush    = XBrushes.Black;

                int calcAge = r.DateOfBirth != DateTime.MinValue
                    ? (int)((DateTime.Today - r.DateOfBirth).TotalDays / 365.25)
                    : 0;

                string employment = string.Join(", ", new[]
                {
                    r.IsEmployed     ? "Employed"      : null,
                    r.IsSelfEmployed ? "Self-Employed"  : null,
                    r.IsNotEmployed  ? "Not Employed"   : null,
                }.Where(s => s != null));

                // meta
                Draw(gfx, font, brush, "TYPE",
                    r.IsNewApplicant ? "New Applicant" : r.IsRenewal ? "Renewal" : "");
                Draw(gfx, font, brush, "DATE_OF_APPLICATION",
                    r.DateOfApplication != DateTime.MinValue
                        ? r.DateOfApplication.ToString("MM/dd/yyyy") : "");
                Draw(gfx, font, brush, "CONTROL_NUMBER", r.Id ?? "");

                // Section I row 1 — Last Name | First Name | Middle Name
                Draw(gfx, fontName, brush, "LAST_NAME",   r.Surname    ?? "");
                Draw(gfx, fontName, brush, "FIRST_NAME",  r.FirstName  ?? "");
                Draw(gfx, fontName, brush, "MIDDLE_NAME", r.MiddleName ?? "");

                // Section I row 2 — Religion | Occupation | Monthly Income | Employment Status
                Draw(gfx, font, brush, "RELIGION",          r.Religion      ?? "");
                Draw(gfx, font, brush, "OCCUPATION",        r.Occupation    ?? "");
                Draw(gfx, font, brush, "INCOME",            r.MonthlyIncome ?? "");
                Draw(gfx, font, brush, "EMPLOYMENT_STATUS", employment);

                // Section I row 3 — Address | Barangay | Applicant Contact Number
                Draw(gfx, font, brush, "ADDRESS",       r.Address       ?? "");
                Draw(gfx, font, brush, "BARANGAY",      r.Barangay      ?? "");
                Draw(gfx, font, brush, "APPLICANTS_NO", r.ContactNumber ?? "");

                // Section I row 4 — Emergency Contact | Relationship | Address | Number
                Draw(gfx, font, brush, "EMERGENCY_CONTACT", r.EmergencyContactName   ?? "");
                Draw(gfx, font, brush, "RELATIONSHIP",      r.EmergencyRelationship  ?? "");
                Draw(gfx, font, brush, "EMERGENCY_ADDRESS", r.EmergencyAddress       ?? "");
                Draw(gfx, font, brush, "EMERGENCY_NO",      r.EmergencyContactNumber ?? "");

                // Section III — family composition
                DrawFamilyMembers(gfx, font, brush, r);

                Draw(gfx, fontSm, brush, "NEEDS_AND_PROBLEMS",      r.NeedsAndProblems  ?? "");
                Draw(gfx, fontSm, brush, "OTHER_SOURCES_OF_INCOME", r.OtherIncomeSource ?? "");

                gfx.Dispose();
                doc.Save(outputPath);
            }
        }

        // Section III — family composition
        private static void DrawFamilyMembers(XGraphics gfx, XFont font, XBrush brush, SoloParentRecord r)
        {
            var members = r.FamilyMembers ?? new System.Collections.Generic.List<FamilyMember>();

            string[] nameKeys  = { "MEMBER_NAME_1", "MEMBER_NAME_2", "MEMBER_NAME_3", "MEMBER_NAME_4", "MEMBER_NAME_5" };
            string[] bdateKeys = { "BDATE_1",        "BDATE_2",       "BDATE_3",       "BDATE_4",       "BDATE_5"       };
            string[] csKeys    = { "CS_1",           "CS_2",          "CS_3",          "CS_4",          "CS_5"          };
            string[] rsKeys    = { "RS_1",           "RS_2",          "RS_3",          "RS_4",          "RS_5"          };
            string[] eduKeys   = { "EDU_1",          "EDU_2",         "EDU_3",         "EDU_4",         "EDU_5"         };
            string[] icmKeys   = { "ICM_1",          "ICM_2",         "ICM_3",         "ICM_4",         "ICM_5"         };

            for (int i = 0; i < 5; i++)
            {
                if (i >= members.Count) break;
                var m = members[i];
                Draw(gfx, font, brush, nameKeys[i],  m.MemberName          ?? "");
                Draw(gfx, font, brush, bdateKeys[i], m.Birthdate           ?? "");
                Draw(gfx, font, brush, csKeys[i],    m.CivilStatus         ?? "");
                Draw(gfx, font, brush, rsKeys[i],    m.Relationship        ?? "");
                Draw(gfx, font, brush, eduKeys[i],   m.EducationEmployment ?? "");
                Draw(gfx, font, brush, icmKeys[i],   m.Income              ?? "");
            }
        }

        private static void Draw(XGraphics gfx, XFont font, XBrush brush, string field, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            if (!_coords.TryGetValue(field, out var coord)) return;
            gfx.DrawString(value, font, brush, coord.X, coord.Y);
        }

        private static string ResolveTemplatePath()
        {
            string exeDir    = Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
            string candidate = Path.Combine(exeDir, "Templates", TemplateName);
            if (File.Exists(candidate)) return candidate;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            for (int up = 0; up < 4; up++)
            {
                string probe = Path.Combine(baseDir, "Templates", TemplateName);
                if (File.Exists(probe)) return probe;
                baseDir = Path.GetDirectoryName(baseDir) ?? baseDir;
            }

            throw new FileNotFoundException(
                $"Template not found: {TemplateName}. " +
                "Ensure the file is in the Templates\\ folder and set to 'Copy to Output Directory'.");
        }

        // All coordinates in PDF points (612x1008pt page).
        // Derived from 1170x1748 canvas via sx=612/1170, sy=1008/1748,
        // then adjusted with per-field pixel deltas from spec.
        private static readonly System.Collections.Generic.Dictionary<string, XPoint> _coords =
            new System.Collections.Generic.Dictionary<string, XPoint>
        {
            { "TYPE",                new XPoint(238.4, 107.6) },
            { "DATE_OF_APPLICATION", new XPoint(473.4, 107.6) },
            { "CONTROL_NUMBER",      new XPoint(  8.7,  22.3) },

            // Section I row 1 — names (13pt, dy+25px, x offsets per column)
            { "LAST_NAME",           new XPoint( 28.8, 242.2) },
            { "FIRST_NAME",          new XPoint(232.8, 242.2) },
            { "MIDDLE_NAME",         new XPoint(387.1, 242.2) },

            // Section I row 2 — religion/occupation/income/employment (dy+70px)
            { "RELIGION",            new XPoint( 28.8, 305.7) },
            { "OCCUPATION",          new XPoint(227.6, 305.7) },
            { "INCOME",              new XPoint(366.1, 305.7) },
            { "EMPLOYMENT_STATUS",   new XPoint(324.3, 305.7) },

            // Section I row 3 — address/barangay/contact (dy+65px)
            { "ADDRESS",             new XPoint( 28.8, 340.2) },
            { "BARANGAY",            new XPoint(410.6, 340.2) },
            { "APPLICANTS_NO",       new XPoint(489.1, 340.2) },

            // Section I row 4 — emergency contact (dy+120px)
            { "EMERGENCY_CONTACT",   new XPoint( 28.8, 409.4) },
            { "RELATIONSHIP",        new XPoint(196.2, 409.4) },
            { "EMERGENCY_ADDRESS",   new XPoint(282.5, 409.4) },
            { "EMERGENCY_NO",        new XPoint(402.8, 409.4) },

            // Section III — family composition (Y base 634pt, 28pt min column gap)
            // Columns: Name=9.4  Bdate=206.6  CS=234.6  RS=262.6  EDU=290.6  ICM=318.6
            { "MEMBER_NAME_1",       new XPoint(  9.4, 634.3) },
            { "MEMBER_NAME_2",       new XPoint(  9.4, 652.8) },
            { "MEMBER_NAME_3",       new XPoint(  9.4, 671.2) },
            { "MEMBER_NAME_4",       new XPoint(  9.4, 689.7) },
            { "MEMBER_NAME_5",       new XPoint(  9.4, 708.1) },

            { "BDATE_1",             new XPoint(206.6, 634.3) },
            { "BDATE_2",             new XPoint(206.6, 652.8) },
            { "BDATE_3",             new XPoint(206.6, 671.2) },
            { "BDATE_4",             new XPoint(206.6, 689.7) },
            { "BDATE_5",             new XPoint(206.6, 708.1) },

            { "CS_1",                new XPoint(234.6, 634.3) },
            { "CS_2",                new XPoint(234.6, 652.8) },
            { "CS_3",                new XPoint(234.6, 671.2) },
            { "CS_4",                new XPoint(234.6, 689.7) },
            { "CS_5",                new XPoint(234.6, 708.1) },

            { "RS_1",                new XPoint(262.6, 634.3) },
            { "RS_2",                new XPoint(262.6, 652.8) },
            { "RS_3",                new XPoint(262.6, 671.2) },
            { "RS_4",                new XPoint(262.6, 689.7) },
            { "RS_5",                new XPoint(262.6, 708.1) },

            { "EDU_1",               new XPoint(290.6, 634.3) },
            { "EDU_2",               new XPoint(290.6, 652.8) },
            { "EDU_3",               new XPoint(290.6, 671.2) },
            { "EDU_4",               new XPoint(290.6, 689.7) },
            { "EDU_5",               new XPoint(290.6, 708.1) },

            { "ICM_1",               new XPoint(318.6, 634.3) },
            { "ICM_2",               new XPoint(318.6, 652.8) },
            { "ICM_3",               new XPoint(318.6, 671.2) },
            { "ICM_4",               new XPoint(318.6, 689.7) },
            { "ICM_5",               new XPoint(318.6, 708.1) },

            { "NEEDS_AND_PROBLEMS",      new XPoint( 18.3, 557.6) },
            { "OTHER_SOURCES_OF_INCOME", new XPoint( 17.1, 605.7) },
        };
    }
}
