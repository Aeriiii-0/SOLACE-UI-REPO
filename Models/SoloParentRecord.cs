using System;
using System.Collections.Generic;

namespace SOLUM_UI.Models
{
    // Family composition row — also carries OCR confidence scores for the review grid
    public class FamilyMember
    {
        public Guid? Id                   { get; set; }
        public bool IsNew                 { get; set; } = true;
        private string _memberName;
        public string MemberName
        {
            get => _memberName;
            set => _memberName = value?.ToUpper() ?? string.Empty;
        }
        public string Sex                 { get; set; }
        public string Age                 { get; set; }
        public string Birthdate           { get; set; }
        public string CivilStatus         { get; set; }
        public string Relationship        { get; set; }
        private string _educationEmployment;
        public string EducationEmployment
        {
            get => _educationEmployment;
            set => _educationEmployment = value?.ToUpper() ?? string.Empty;
        }
        public string Income              { get; set; }

        // Special needs
        public bool HasSpecialNeeds       { get; set; } = false;
        public string SpecialNeedsCategory { get; set; }
        public string SpecialNeedsDescription { get; set; }

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
        public DateTime? CurrentTermExpiresAt { get; set; }
        public List<SOLUM_UI.Models.Api.RecordTermDto> RecordTerms { get; set; } = new List<SOLUM_UI.Models.Api.RecordTermDto>();

        /// <summary>
        /// Human-readable application type derived from <see cref="IsNewApplicant"/> and
        /// <see cref="IsRenewal"/>. Set automatically by the entry-point flow; never
        /// set from a UI checkbox.
        /// </summary>
        public string ApplicationType =>
            IsNewApplicant ? "New Record" :
            IsRenewal      ? "Renewed" : "—";

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
        public List<Guid> RemovedFamilyMemberIds { get; set; } = new List<Guid>();

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
        public string ProblemPresented { get; set; }
        public string FamilyComposition { get; set; }
        public string AdditionalClassifications { get; set; }

        public bool IsLGBT { get; set; }
        public bool IsPantawidBeneficiary { get; set; }
        public bool IsIndigenous { get; set; }

        // helper
        public string LastUpdatedFormatted => LastUpdated != DateTime.MinValue ? LastUpdated.ToString("yyyy-MM-dd") : "—";
        public string DateOfBirthFormatted => DateOfBirth != DateTime.MinValue ? DateOfBirth.ToString("yyyy-MM-dd") : "—";
        public string FullName             => $"{Surname}, {FirstName} {MiddleName}".Trim();

        public string EmploymentStatusDisplay
        {
            get
            {
                if (IsEmployed)     return "Employed";
                if (IsSelfEmployed) return "Self-Employed";
                if (IsNotEmployed)  return "Not Employed";
                return "—";
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
                if (parts.Count > 0) return string.Join("\n", parts);
                if (!string.IsNullOrWhiteSpace(ProblemPresented)) return ProblemPresented;
                return "—";
            }
        }

        /// <summary>
        /// Gets the start/issue date of the current active term.
        /// Checks the latest term in RecordTerms, then LastUpdated, then DateOfApplication, then CurrentTermExpiresAt - 1 year.
        /// </summary>
        public DateTime GetActiveTermStartDate()
        {
            DateTime latestStart = DateTime.MinValue;

            if (RecordTerms != null && RecordTerms.Count > 0)
            {
                foreach (var rt in RecordTerms)
                {
                    DateTime dt = rt.DateCreated ?? DateTime.MinValue;
                    if (DateTime.TryParse(rt.StartsAt, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var s) && s > dt)
                        dt = s;
                    else if (DateTime.TryParse(rt.StartsAt, out var s2) && s2 > dt)
                        dt = s2;

                    if (dt > latestStart)
                        latestStart = dt;
                }
            }

            if (latestStart != DateTime.MinValue)
                return latestStart;

            if (LastUpdated != DateTime.MinValue)
                return LastUpdated;

            if (DateOfApplication != DateTime.MinValue)
                return DateOfApplication;

            if (CurrentTermExpiresAt.HasValue && CurrentTermExpiresAt.Value != DateTime.MinValue)
                return CurrentTermExpiresAt.Value.AddYears(-1);

            return DateTime.MinValue;
        }

        /// <summary>
        /// Determines whether the record is eligible for renewal.
        /// Rule: Solo parents cannot renew if their record is active for less than 1 month since its issue/start date.
        /// Expired or inactive records can always be renewed.
        /// </summary>
        public bool CanRenew(out string failureReason, out DateTime? earliestRenewalDate)
        {
            failureReason = null;
            earliestRenewalDate = null;

            bool isExpired = CurrentTermExpiresAt.HasValue && CurrentTermExpiresAt.Value.Date < DateTime.Today;
            bool isStatusInactive = string.Equals(Status, "Inactive", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(Status, "Expired", StringComparison.OrdinalIgnoreCase);

            // Expired or inactive records are always eligible for renewal
            if (isExpired || isStatusInactive)
            {
                return true;
            }

            DateTime startDate = GetActiveTermStartDate();
            if (startDate != DateTime.MinValue)
            {
                DateTime minRenewal = startDate.Date.AddMonths(1);
                if (DateTime.Today < minRenewal)
                {
                    earliestRenewalDate = minRenewal;
                    int daysRemaining = Math.Max(1, (minRenewal - DateTime.Today).Days);
                    failureReason = $"This record has been active for less than a month (activated on {startDate:MMM dd, yyyy}). Renewal will be available starting {minRenewal:MMM dd, yyyy} ({daysRemaining} day{(daysRemaining == 1 ? "" : "s")} remaining).";
                    return false;
                }
            }

            return true;
        }

        public bool IsEligibleForRenewal => CanRenew(out _, out _);
    }
}
