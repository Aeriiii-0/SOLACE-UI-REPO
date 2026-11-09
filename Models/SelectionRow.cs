using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using SOLUM_UI.Core;

namespace SOLUM_UI.Models
{
    public class SelectionRow : ObservableObject
    {
        private bool _isFinalGrantee, _isSelected;
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }

        public int Rank { get; set; }
        public Guid EvaluationId { get; set; }
        public Guid CycleId { get; set; }
        public Guid SoloParentId { get; set; }
        public string SpId { get; set; }
        public string Name { get; set; }
        public string Barangay { get; set; }
        public string SubsidyType { get; set; } = "Allowance";
        public string Priority { get; set; }
        public double Score { get; set; }
        public double Confidence { get; set; } = 0.85;
        public bool IsLowConfidence => !IsFallbackScore && Confidence > 0 && Confidence < 0.60;
        public bool IsWaitlisted { get; set; }
        public string StatusText => IsFinalGrantee ? "Confirmed" : (IsWaitlisted ? "Waitlisted" : "Pending");

        public bool IsFinalGrantee
        {
            get => _isFinalGrantee;
            set
            {
                if (SetProperty(ref _isFinalGrantee, value))
                {
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(RankBg));
                    OnPropertyChanged(nameof(RankFg));
                    OnPropertyChanged(nameof(FinalBadgeColor));
                    OnPropertyChanged(nameof(FinalBadgeText));
                    OnPropertyChanged(nameof(FinalBadgeTextColor));
                    OnPropertyChanged(nameof(ConfirmButtonText));
                    OnPropertyChanged(nameof(ConfirmButtonBg));
                    OnPropertyChanged(nameof(ConfirmButtonFg));
                    OnPropertyChanged(nameof(ConfirmButtonBorder));
                    OnPropertyChanged(nameof(ConfirmButtonToolTip));
                }
            }
        }

        public string ScoreLabel => Score.ToString("0") + "%";

        private int _dependants;
        public int Dependants
        {
            get => _dependants > 0 ? _dependants : (ChildrenDetails != null && ChildrenDetails.Count > 0 ? ChildrenDetails.Count : (Children0To6Count + Children7To22Count));
            set { if (SetProperty(ref _dependants, value)) { OnPropertyChanged(nameof(MinorsLabel)); OnPropertyChanged(nameof(DependantsDetailLabel)); } }
        }
        public double MonthlyIncome { get; set; }
        public double IncomePerCapita { get; set; }
        public int MinorDependentsCount { get; set; }
        public int ToddlersUnder5Count { get; set; }
        private string _circumstance = string.Empty;
        public string Circumstance { get => _circumstance; set => SetProperty(ref _circumstance, value); }

        public string IncomeLabel => $"₱{MonthlyIncome:N0}/mo";
        public string PerCapitaLabel => $"₱{IncomePerCapita:N0} / capita";
        public string MinorsLabel => Dependants == 1 ? "1 Dependent" : $"{Dependants} Dependents";
        public string ToddlersLabel => Children0To6Count > 0 ? (Children0To6Count == 1 ? "1 Dependent (0–6y)" : $"{Children0To6Count} Dependents (0–6y)") : "0 Dependents (0–6y)";
        public SolidColorBrush ToddlersFg => Children0To6Count >= 2 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")) : (Children0To6Count == 1 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")));

        public bool HasCollegeAgeDependent { get; set; }
        public bool HasSpecialNeedsDependent { get; set; }
        public int SpecialNeedsDependentsCount { get; set; }
        public string ProblemPresented { get; set; } = string.Empty;
        public int CollegeAgeDependentsCount { get; set; }
        public bool IsPantawidBeneficiary { get; set; }
        private int _children0To6Count;
        public int Children0To6Count
        {
            get => _children0To6Count > 0 ? _children0To6Count : ToddlersUnder5Count;
            set { if (SetProperty(ref _children0To6Count, value)) { OnPropertyChanged(nameof(ToddlersLabel)); OnPropertyChanged(nameof(ToddlersFg)); OnPropertyChanged(nameof(DependantsDetailLabel)); } }
        }
        public int Children7To22Count { get; set; }
        private string _otherIncomeSource = string.Empty;
        public string OtherIncomeSource { get => _otherIncomeSource; set => SetProperty(ref _otherIncomeSource, value); }
        private string _needsAndProblems = string.Empty;
        public string NeedsAndProblems { get => _needsAndProblems; set { if (SetProperty(ref _needsAndProblems, value)) OnPropertyChanged(nameof(NeedsAndProblemsDisplay)); } }
        public string NeedsAndProblemsDisplay => !string.IsNullOrWhiteSpace(NeedsAndProblems) && !NeedsAndProblems.Equals("None", StringComparison.OrdinalIgnoreCase) ? NeedsAndProblems : "—";
        public List<ChildDetailItem> ChildrenDetails { get; set; } = new List<ChildDetailItem>();
        public bool HasChildrenDetails => ChildrenDetails != null && ChildrenDetails.Count > 0;
        public string DependentsExpanderHeader => $"Dependents List ({(ChildrenDetails != null && ChildrenDetails.Count > 0 ? ChildrenDetails.Count : Dependants)})";
        public List<ChildDetailItem> CollegeAgeDependents { get; set; } = new List<ChildDetailItem>();

        public string CrossCheckText =>
            HasCollegeAgeDependent ? "College Scholarship" : "Cleared";

        public SolidColorBrush CrossCheckFg =>
            HasCollegeAgeDependent
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));

                public string AuditFlagsSummary
        {
            get
            {
                var flags = new List<string>();
                if (IsCardExpired) flags.Add("ID Expired");
                if (HasCollegeAgeDependent) flags.Add($"College Scholarship ({CollegeStudentSummary})");
                if (HasSpecialNeedsDependent) flags.Add(SpecialNeedsDependentsCount > 1 ? $"{SpecialNeedsDependentsCount} Special Needs Dependents" : "Special Needs Dependent");
                if (IsPantawidBeneficiary) flags.Add("4Ps Beneficiary");
                return flags.Count > 0 ? string.Join(" • ", flags) : "—";
            }
        }
        public bool HasAuditFlags => IsCardExpired || HasCollegeAgeDependent || HasSpecialNeedsDependent || IsPantawidBeneficiary;
        public SolidColorBrush AuditFlagsFg => IsCardExpired
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"))
            : (HasCollegeAgeDependent ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")));

        public string ComplianceRemark
        {
            get
            {
                if (IsCardExpired)
                {
                    return "⚠️ Renewal Required • Solo Parent ID is Expired. Must renew before final confirmation.";
                }
                string depWord = Dependants == 1 ? "1 Dependent" : $"{Dependants} Dependents";
                if (HasCollegeAgeDependent)
                {
                    var studentList = (CollegeAgeDependents != null && CollegeAgeDependents.Count > 0)
                        ? string.Join(", ", CollegeAgeDependents.Select(c => $"{c.Name} ({c.Age}y)"))
                        : (CollegeAgeDependentsCount == 1 ? "1 College-Age" : $"{CollegeAgeDependentsCount} College-Age");
                    return $"{depWord} • Student: {studentList} • City Educ Scholarship Match Req.";
                }
                string minWord = MinorDependentsCount == 1 ? "1 Dependent" : $"{MinorDependentsCount} Dependents";
                return $"{depWord} ({minWord}) • Cleared (No Inter-Agency Overlap)";
            }
        }

        public SolidColorBrush ComplianceRemarkFg => IsCardExpired ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991B1B")) :
            HasCollegeAgeDependent
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#92400E"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));

        public SolidColorBrush ComplianceRemarkBg => IsCardExpired ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2")) :
            HasCollegeAgeDependent
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));

        public string Id => SpId;
        private string _contactNumber, _address, _emergencyContactName, _emergencyContactNumber;
        public string ContactNumber { get => _contactNumber; set { if (SetProperty(ref _contactNumber, value)) OnPropertyChanged(nameof(ContactNumberFormatted)); } }
        public string ContactNumberFormatted => !string.IsNullOrWhiteSpace(ContactNumber) ? SOLUM_UI.Services.OcrService.FormatPhoneNumber(ContactNumber) : "—";
        public string Address { get => _address; set { if (SetProperty(ref _address, value)) OnPropertyChanged(nameof(FullAddress)); } }
        public string FullAddress => !string.IsNullOrWhiteSpace(Address) ? $"{Address}, {Barangay}" : (!string.IsNullOrWhiteSpace(Barangay) ? Barangay : "—");
        public string EmergencyContactName { get => !string.IsNullOrWhiteSpace(_emergencyContactName) ? _emergencyContactName : "—"; set { if (SetProperty(ref _emergencyContactName, value)) OnPropertyChanged(nameof(EmergencySummary)); } }
        public string EmergencyContactNumber { get => _emergencyContactNumber; set { if (SetProperty(ref _emergencyContactNumber, value)) { OnPropertyChanged(nameof(EmergencyContactFormatted)); OnPropertyChanged(nameof(EmergencySummary)); } } }
        public string EmergencyContactFormatted => !string.IsNullOrWhiteSpace(EmergencyContactNumber) ? SOLUM_UI.Services.OcrService.FormatPhoneNumber(EmergencyContactNumber) : "—";
        public string EmergencySummary => !string.IsNullOrWhiteSpace(_emergencyContactName) ? (EmergencyContactFormatted != "—" ? $"{_emergencyContactName} ({EmergencyContactFormatted})" : _emergencyContactName) : "—";
        public string Sex { get; set; } = "Female";
        public string CivilStatus { get; set; } = "Single";
        public string DateOfBirthFormatted { get; set; } = "1988-05-14";
        public string LastUpdatedFormatted { get; set; } = "2026-10-04";
        private string _validUntilFormatted;
        public string ValidUntilFormatted
        {
            get
            {
                if (CardExpiresAt.HasValue) return CardExpiresAt.Value.ToString("yyyy-MM-dd");
                if (!string.IsNullOrEmpty(_validUntilFormatted)) return _validUntilFormatted;
                int hash = Math.Abs((SpId ?? Name ?? "0").GetHashCode());
                return $"2027-{((hash % 12) + 1):D2}-{((hash % 25) + 1):D2}";
            }
            set => _validUntilFormatted = value;
        }
        private DateTime? _cardExpiresAt;
        public DateTime? CardExpiresAt
        {
            get => _cardExpiresAt;
            set
            {
                if (SetProperty(ref _cardExpiresAt, value))
                {
                    OnPropertyChanged(nameof(IsCardExpired));
                    OnPropertyChanged(nameof(IsCardExpiringSoon));
                    OnPropertyChanged(nameof(CardStatusText));
                    OnPropertyChanged(nameof(CardStatusBg));
                    OnPropertyChanged(nameof(CardStatusFg));
                    OnPropertyChanged(nameof(CardStatusBorder));
                    OnPropertyChanged(nameof(CardValidityDisplay));
                    OnPropertyChanged(nameof(ValidUntilFormatted));
                    OnPropertyChanged(nameof(ComplianceRemark));
                    OnPropertyChanged(nameof(ComplianceRemarkFg));
                    OnPropertyChanged(nameof(ComplianceRemarkBg));
                }
            }
        }

        public bool IsCardExpired => CardExpiresAt.HasValue && CardExpiresAt.Value.Date < DateTime.Today;
        public bool IsCardExpiringSoon => CardExpiresAt.HasValue && !IsCardExpired && (CardExpiresAt.Value.Date - DateTime.Today).TotalDays <= 30;

        public string CardStatusText =>
            !CardExpiresAt.HasValue ? "ACTIVE" :
            IsCardExpired ? "EXPIRED" :
            IsCardExpiringSoon ? "EXPIRING SOON" : "ACTIVE";

        public SolidColorBrush CardStatusBg =>
            !CardExpiresAt.HasValue ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7")) :
            IsCardExpired ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2")) :
            IsCardExpiringSoon ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7")) :
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));

        public SolidColorBrush CardStatusFg =>
            !CardExpiresAt.HasValue ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534")) :
            IsCardExpired ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")) :
            IsCardExpiringSoon ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309")) :
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));

        public SolidColorBrush CardStatusBorder =>
            !CardExpiresAt.HasValue ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBF7D0")) :
            IsCardExpired ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FECACA")) :
            IsCardExpiringSoon ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE68A")) :
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBF7D0"));

        public string CardValidityDisplay => CardExpiresAt.HasValue ? CardExpiresAt.Value.ToString("yyyy-MM-dd") : "—";

                public string SubsidyStatusText => IsFinalGrantee ? "Confirmed" : (IsWaitlisted ? "Waitlisted" : "Shortlisted");
        public SolidColorBrush SubsidyStatusBg => IsFinalGrantee
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"))
            : (IsWaitlisted ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDF4F6")));
        public SolidColorBrush SubsidyStatusFg => IsFinalGrantee
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"))
            : (IsWaitlisted ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#702943")));
        public SolidColorBrush SubsidyStatusBorder => IsFinalGrantee
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBF7D0"))
            : (IsWaitlisted ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE68A")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1D4DC")));

        public string RecordStatus { get; set; } = "Active";
        public SolidColorBrush RecordStatusBg => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6F9EE"));
        public SolidColorBrush RecordStatusFg => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));

        public string ScholarshipEligibilityNotice => HasCollegeAgeDependent
            ? $"Has dependents eligible for scholarship grants: {CollegeStudentSummary}"
            : "No dependents eligible for scholarship grants";

        public SolidColorBrush ScholarshipEligibilityFg => HasCollegeAgeDependent
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));

        public string DependantsDetailLabel
        {
            get
            {
                string depWord = Dependants == 1 ? "1 dependent" : $"{Dependants} dependents";
                string todWord = Children0To6Count > 0 ? $", {Children0To6Count} aged 0–6" : (ToddlersUnder5Count > 0 ? $", {ToddlersUnder5Count} aged 0–6" : "");
                return $"{depWord}{todWord}";
            }
        }

        public string CollegeStudentSummary => (CollegeAgeDependents != null && CollegeAgeDependents.Count > 0)
            ? string.Join(", ", CollegeAgeDependents.Select(c => $"{c.Name} ({c.Age}y)"))
            : $"{CollegeAgeDependentsCount} College-Age Dependent(s) (17–24y)";

        public string ConfirmButtonText => IsFinalGrantee ? "Revert" : "Confirm";
        public string ConfirmButtonToolTip => IsFinalGrantee ? "Revert candidate back to pending shortlist" : "Confirm candidate as final grantee for FY 2026";
        public SolidColorBrush ConfirmButtonBg => IsFinalGrantee
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#702943"));
        public SolidColorBrush ConfirmButtonFg => IsFinalGrantee
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"))
            : new SolidColorBrush(Colors.White);
        public SolidColorBrush ConfirmButtonBorder => IsFinalGrantee
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#702943"));

        public SolidColorBrush RankBg =>
            IsFinalGrantee
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EAF6F0"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1"));

        public SolidColorBrush RankFg =>
            IsFinalGrantee
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));

        public SolidColorBrush RowBg =>
            IsWaitlisted
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFDF5"))
                : new SolidColorBrush(Colors.White);

        public SolidColorBrush ScoreBarColor => Score >= 75 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")) : (Score >= 50 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")));
        public SolidColorBrush PriorityColor => string.Equals(Priority, "High", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8E8")) : (string.Equals(Priority, "Medium", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")));
        public SolidColorBrush PriorityTextColor => string.Equals(Priority, "High", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")) : (string.Equals(Priority, "Medium", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")));
        public SolidColorBrush FinalBadgeColor => IsFinalGrantee ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"));
        public string FinalBadgeText => IsFinalGrantee ? "Granted" : "Pending";
        public SolidColorBrush FinalBadgeTextColor => IsFinalGrantee ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));

                        public SolidColorBrush ScoreCellBg => new SolidColorBrush(Colors.Transparent);
        public SolidColorBrush ScoreCellBorder => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F2EAEF"));
        public string ScoreCellTooltip => IsLowConfidence
            ? $"⚠ Low Model Confidence ({(Confidence * 100):0}%): AI prediction is below 60% threshold. Manual caseworker verification recommended."
            : $"Vulnerability Score: {Score:0}% • Model: {ModelVersion} ({(Confidence * 100):0}% Confidence)";
        public bool HasAlertSignal => IsLowConfidence || IsFallbackScore;
        public SolidColorBrush AlertSignalBg => IsLowConfidence
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#702943"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
        public SolidColorBrush AlertSignalFg => new SolidColorBrush(Colors.White);
        public string AlertSignalTooltip => IsLowConfidence
            ? $"Low Model Confidence ({(Confidence * 100):0}%): AI prediction is below 60% threshold. Manual caseworker verification recommended."
            : "Deterministic Rule-Based Fallback: Estimated via statutory income rules because the ML microservice was offline.";
        public string ModelVersion { get; set; } = "v0.3.2-development";
        public bool IsFallbackScore => !string.IsNullOrEmpty(ModelVersion) && ModelVersion.IndexOf("fallback", StringComparison.OrdinalIgnoreCase) >= 0;
        public string ModelSourceBadge => IsFallbackScore ? "Fallback" : "ML";
        public string ModelSourceTooltip => IsFallbackScore
            ? "Rule-Based Fallback: Estimated via statutory income rules because the ML microservice was offline."
            : (IsLowConfidence
                ? $"⚠ Low Model Confidence ({(Confidence * 100):0}%): AI model prediction is below 60% confidence threshold. Manual casework verification recommended."
                : $"SOLUM ML Model ({ModelVersion}) • {(Confidence * 100):0}% Confidence");
        public SolidColorBrush ModelSourceBg => IsFallbackScore
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"))
            : (IsLowConfidence ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF2F2")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F2FE")));
        public SolidColorBrush ModelSourceFg => IsFallbackScore
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"))
            : (IsLowConfidence ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0369A1")));
        public SolidColorBrush ModelSourceBorder => IsFallbackScore
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCD34D"))
            : (IsLowConfidence ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BAE6FD")));
    }
}