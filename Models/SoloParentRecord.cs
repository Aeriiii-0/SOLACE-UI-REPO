using System;
using System.Collections.Generic;

namespace SOLUM_UI.Models
{
    // Family composition row — also carries OCR confidence scores for the review grid
    public class FamilyMember
    {
        public string MemberName          { get; set; }
        public string Sex                 { get; set; }
        public string Age                 { get; set; }
        public string Birthdate           { get; set; }
        public string CivilStatus         { get; set; }
        public string Relationship        { get; set; }
        public string EducationEmployment { get; set; }
        public string Income              { get; set; }

        // OCR confidence (0–1) per field; drives the cell background colour in OcrScanDialog
        public double NameConf { get; set; } = 0.90;
        public double SexConf  { get; set; } = 0.90;
        public double AgeConf  { get; set; } = 0.90;
        public double DobConf  { get; set; } = 0.90;
        public double CivConf  { get; set; } = 0.90;
        public double RelConf  { get; set; } = 0.90;
        public double EduConf  { get; set; } = 0.90;
        public double IncConf  { get; set; } = 0.90;

        public string NameBg => Bg(NameConf);
        public string SexBg  => Bg(SexConf);
        public string AgeBg  => Bg(AgeConf);
        public string DobBg  => Bg(DobConf);
        public string CivBg  => Bg(CivConf);
        public string RelBg  => Bg(RelConf);
        public string EduBg  => Bg(EduConf);
        public string IncBg  => Bg(IncConf);

        private static string Bg(double c)
            => c >= 0.80 ? "#D5F5E3" : c >= 0.50 ? "#FCF3CF" : "#FADBD8";
    }

    public class SoloParentRecord
    {
        // id
        public string   Id          { get; set; }
        public string   CreatedBy   { get; set; }
        public DateTime LastUpdated { get; set; }
        public string   Status      { get; set; }

        // applicant type
        public bool     IsNewApplicant    { get; set; }
        public bool     IsRenewal         { get; set; }
        public DateTime DateOfApplication { get; set; }

        // name
        public string Name          { get; set; }
        public string Surname       { get; set; }
        public string FirstName     { get; set; }
        public string MiddleName    { get; set; }
        public string ExtensionName { get; set; }

        // personal info
        public DateTime DateOfBirth  { get; set; }
        public string   PlaceOfBirth { get; set; }
        public string   Sex          { get; set; }
        public string   CivilStatus  { get; set; }
        public string   Citizenship  { get; set; }
        public string   BloodType    { get; set; }
        public string   Height       { get; set; }
        public string   Weight       { get; set; }

        // bg
        public string EducationalAttainment { get; set; }
        public string PhilSysNumber         { get; set; }
        public string Religion              { get; set; }
        public string Occupation            { get; set; }
        public string MonthlyIncome         { get; set; }
        public bool   IsEmployed            { get; set; }
        public bool   IsSelfEmployed        { get; set; }
        public bool   IsNotEmployed         { get; set; }

        // address n contact
        public string Address       { get; set; }
        public string Barangay      { get; set; }
        public string ContactNumber { get; set; }

        //  emergency contact 
        public string EmergencyContactName   { get; set; }
        public string EmergencyRelationship  { get; set; }
        public string EmergencyAddress       { get; set; }
        public string EmergencyContactNumber { get; set; }

        //  children 
        public int                Children      { get; set; }
        public List<FamilyMember> FamilyMembers { get; set; } = new List<FamilyMember>();

        //  circumstances 
        public bool   CircumstanceA1           { get; set; }
        public bool   CircumstanceA2           { get; set; }
        public string CircumstanceA2Cause      { get; set; }
        public DateTime CircumstanceA2Date     { get; set; }
        public bool   CircumstanceA3           { get; set; }
        public bool   CircumstanceA4           { get; set; }
        public string CircumstanceA4Disability { get; set; }
        public bool   CircumstanceA5           { get; set; }
        public string CircumstanceA5Period     { get; set; }
        public bool   CircumstanceA6           { get; set; }
        public bool   CircumstanceA6Nullity    { get; set; }
        public bool   CircumstanceA6Annulment  { get; set; }
        public bool   CircumstanceA7           { get; set; }
        public bool   CircumstanceB            { get; set; }
        public string CircumstanceBStayAbroad  { get; set; }
        public bool   CircumstanceC            { get; set; }
        public bool   CircumstanceD            { get; set; }
        public bool   CircumstanceE            { get; set; }
        public bool   CircumstanceF            { get; set; }

        // needs & other income 
        public string NeedsAndProblems  { get; set; }
        public string OtherIncomeSource { get; set; }

        // helper
        public string LastUpdatedFormatted => LastUpdated != DateTime.MinValue ? LastUpdated.ToString("yyyy-MM-dd") : "—";
        public string DateOfBirthFormatted => DateOfBirth != DateTime.MinValue ? DateOfBirth.ToString("yyyy-MM-dd") : "—";
        public string FullName             => $"{Surname}, {FirstName} {MiddleName}".Trim();

        public string EmploymentStatusDisplay
        {
            get
            {
                var parts = new List<string>();
                if (IsEmployed)     parts.Add("Employed");
                if (IsSelfEmployed) parts.Add("Self-Employed");
                if (IsNotEmployed)  parts.Add("Not Employed");
                return parts.Count > 0 ? string.Join(", ", parts) : "—";
            }
        }

        // newline-separated summary of all ticked circumstances
        public string CircumstancesDisplay
        {
            get
            {
                var parts = new List<string>();
                if (CircumstanceA1) parts.Add("A1. Gives birth as a result of rape");
                if (CircumstanceA2)
                {
                    var s = "A2. Death of Spouse";
                    if (!string.IsNullOrWhiteSpace(CircumstanceA2Cause)) s += " — " + CircumstanceA2Cause;
                    if (CircumstanceA2Date != DateTime.MinValue) s += " (" + CircumstanceA2Date.ToString("MMM d, yyyy") + ")";
                    parts.Add(s);
                }
                if (CircumstanceA3) parts.Add("A3. Detention of Spouse");
                if (CircumstanceA4)
                {
                    var s = "A4. Physical & Mental Incapacity of Spouse";
                    if (!string.IsNullOrWhiteSpace(CircumstanceA4Disability)) s += " (" + CircumstanceA4Disability + ")";
                    parts.Add(s);
                }
                if (CircumstanceA5)
                {
                    var s = "A5. Legal or de facto Separation";
                    if (!string.IsNullOrWhiteSpace(CircumstanceA5Period)) s += " (" + CircumstanceA5Period + ")";
                    parts.Add(s);
                }
                if (CircumstanceA6)
                {
                    var sub = new List<string>();
                    if (CircumstanceA6Nullity)   sub.Add("Nullity of marriage");
                    if (CircumstanceA6Annulment) sub.Add("Annulment of marriage");
                    parts.Add("A6. Declaration of" + (sub.Count > 0 ? ": " + string.Join(", ", sub) : ""));
                }
                if (CircumstanceA7) parts.Add("A7. Abandonment of spouse for at least 6 months");
                if (CircumstanceB)
                {
                    var s = "B. OFW-related";
                    if (!string.IsNullOrWhiteSpace(CircumstanceBStayAbroad)) s += " (" + CircumstanceBStayAbroad + ")";
                    parts.Add(s);
                }
                if (CircumstanceC) parts.Add("C. Unmarried Mother or Father");
                if (CircumstanceD) parts.Add("D. Legal Guardian / Adoptive / Foster Parent");
                if (CircumstanceE) parts.Add("E. Relative within 4th civil degree");
                if (CircumstanceF) parts.Add("F. Pregnant Woman");
                return parts.Count > 0 ? string.Join("\n", parts) : "—";
            }
        }
    }
}
