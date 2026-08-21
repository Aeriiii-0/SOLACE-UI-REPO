using System;
using System.Collections.Generic;

namespace SOLUM_UI.Models
{
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

        public double NameConf { get; set; } = 0.90;
        public double SexConf  { get; set; } = 0.90;
        public double AgeConf  { get; set; } = 0.90;
        public double DobConf  { get; set; } = 0.90;
        public double CivConf  { get; set; } = 0.90;
        public double RelConf  { get; set; } = 0.90;
        public double EduConf  { get; set; } = 0.90;
        public double IncConf  { get; set; } = 0.90;

        public string NameBg => GetConfBg(NameConf);
        public string SexBg  => GetConfBg(SexConf);
        public string AgeBg  => GetConfBg(AgeConf);
        public string DobBg  => GetConfBg(DobConf);
        public string CivBg  => GetConfBg(CivConf);
        public string RelBg  => GetConfBg(RelConf);
        public string EduBg  => GetConfBg(EduConf);
        public string IncBg  => GetConfBg(IncConf);

        private static string GetConfBg(double conf)
        {
            if (conf >= 0.80) return "#D5F5E3"; // Clear soft mint green (High confidence)
            if (conf >= 0.50) return "#FCF3CF"; // Clear visible warm amber/yellow (Moderate confidence)
            return "#FADBD8";                   // Clear visible coral red/pink (Low warning)
        }
    }

    public class SoloParentRecord
    {
        public string Id              { get; set; }
        public string Name            { get; set; }
        public string Surname         { get; set; }
        public string FirstName       { get; set; }
        public string MiddleName      { get; set; }
        public string ExtensionName   { get; set; }
        public DateTime DateOfBirth   { get; set; }
        public string PlaceOfBirth    { get; set; }
        public string Sex             { get; set; }
        public string CivilStatus     { get; set; }
        public string Citizenship     { get; set; }
        public string BloodType       { get; set; }
        public string Height          { get; set; }
        public string Weight          { get; set; }
        public string Address         { get; set; }
        public string ContactNumber   { get; set; }
        public string Barangay        { get; set; }
        public string SourceOfReferral { get; set; }
        public DateTime DateAdmitted  { get; set; }
        public string CaseNo          { get; set; }
        public string OffenseCommitted { get; set; }
        public string NatureOfReferral { get; set; }
        public int Children           { get; set; }
        public string Status          { get; set; }
        public string CreatedBy       { get; set; }
        public DateTime LastUpdated   { get; set; }

        public bool IsNewApplicant    { get; set; }
        public bool IsRenewal         { get; set; }
        public DateTime DateOfApplication { get; set; }

        public string EducationalAttainment { get; set; }
        public string PhilSysNumber   { get; set; }
        public string Religion        { get; set; }
        public string Occupation      { get; set; }
        public string MonthlyIncome   { get; set; }
        public bool IsEmployed        { get; set; }
        public bool IsSelfEmployed    { get; set; }
        public bool IsNotEmployed     { get; set; }

        public string EmergencyContactName    { get; set; }
        public string EmergencyRelationship   { get; set; }
        public string EmergencyAddress        { get; set; }
        public string EmergencyContactNumber  { get; set; }

        public bool CircumstanceA1            { get; set; }
        public bool CircumstanceA2            { get; set; }
        public string CircumstanceA2Cause     { get; set; }
        public DateTime CircumstanceA2Date    { get; set; }
        public bool CircumstanceA3            { get; set; }
        public bool CircumstanceA4            { get; set; }
        public string CircumstanceA4Disability { get; set; }
        public bool CircumstanceA5            { get; set; }
        public string CircumstanceA5Period    { get; set; }
        public bool CircumstanceA6            { get; set; }
        public bool CircumstanceA7            { get; set; }
        public bool CircumstanceB             { get; set; }
        public string CircumstanceBStayAbroad { get; set; }
        public bool CircumstanceC             { get; set; }
        public bool CircumstanceD             { get; set; }
        public bool CircumstanceE             { get; set; }
        public bool CircumstanceF             { get; set; }

        public List<FamilyMember> FamilyMembers { get; set; } = new List<FamilyMember>();

        public string NeedsAndProblems  { get; set; }
        public string OtherIncomeSource { get; set; }

        public string LastUpdatedFormatted  => LastUpdated != DateTime.MinValue    ? LastUpdated.ToString("yyyy-MM-dd") : "—";
        public string DateOfBirthFormatted  => DateOfBirth != DateTime.MinValue    ? DateOfBirth.ToString("yyyy-MM-dd") : "—";
        public string DateAdmittedFormatted => DateAdmitted != DateTime.MinValue   ? DateAdmitted.ToString("yyyy-MM-dd") : "—";
        public string FullName              => string.Format("{0}, {1} {2}", Surname, FirstName, MiddleName).Trim();

        public string EmploymentStatusDisplay
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>();
                if (IsEmployed)     parts.Add("Employed");
                if (IsSelfEmployed) parts.Add("Self-Employed");
                if (IsNotEmployed)  parts.Add("Not Employed");
                return parts.Count > 0 ? string.Join(", ", parts) : "—";
            }
        }

        public string CircumstancesDisplay
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>();
                if (CircumstanceA1) parts.Add("A1. Gives birth as a result of rape");
                if (CircumstanceA2) { var s = "A2. Death of Spouse"; if (!string.IsNullOrWhiteSpace(CircumstanceA2Cause)) s += " (" + CircumstanceA2Cause + ")"; parts.Add(s); }
                if (CircumstanceA3) parts.Add("A3. Detention of Spouse");
                if (CircumstanceA4) { var s = "A4. Physical & Mental Incapacity of Spouse"; if (!string.IsNullOrWhiteSpace(CircumstanceA4Disability)) s += " (" + CircumstanceA4Disability + ")"; parts.Add(s); }
                if (CircumstanceA5) { var s = "A5. Legal or de facto Separation"; if (!string.IsNullOrWhiteSpace(CircumstanceA5Period)) s += " (" + CircumstanceA5Period + ")"; parts.Add(s); }
                if (CircumstanceA6) parts.Add("A6. Nullity / annulment of marriage");
                if (CircumstanceA7) parts.Add("A7. Abandonment of spouse for at least 6 months");
                if (CircumstanceB)  { var s = "B. OFW-related"; if (!string.IsNullOrWhiteSpace(CircumstanceBStayAbroad)) s += " (" + CircumstanceBStayAbroad + ")"; parts.Add(s); }
                if (CircumstanceC)  parts.Add("C. Unmarried Mother or Father");
                if (CircumstanceD)  parts.Add("D. Legal Guardian / Adoptive / Foster Parent");
                if (CircumstanceE)  parts.Add("E. Relative within 4th civil degree");
                if (CircumstanceF)  parts.Add("F. Pregnant Woman");
                return parts.Count > 0 ? string.Join("\n", parts) : "—";
            }
        }
    }
}
