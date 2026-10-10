using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using SOLUM_UI.Core;

namespace SOLUM_UI.Models
{
    public class SubsidyItem : ObservableObject
    {
        private string _status = "Pending";
        private bool _isSelected;
        private bool _isWithinQuota = true;
        private int _rank;

        public int Rank { get => _rank; set => SetProperty(ref _rank, value); }
        public Guid EvaluationId { get; set; }
        public Guid CycleId { get; set; }
        public Guid SoloParentId { get; set; }
        public string SpId { get; set; }
        public string Name { get; set; }
        public string Barangay { get; set; }
        public string SubsidyType { get; set; } = "Allowance";
        public string Priority { get; set; }
        private int _dependants;
        public int Dependants { get => _dependants > 0 ? _dependants : (ChildrenDetails != null && ChildrenDetails.Count > 0 ? ChildrenDetails.Count : (_children0To6Count + _children7To22Count)); set { if (SetProperty(ref _dependants, value)) { OnPropertyChanged(nameof(DependantsLabel)); OnPropertyChanged(nameof(DepPrimaryMetric)); OnPropertyChanged(nameof(MinorsLabel)); } } }
        public string CivilStatus { get; set; }
        public string Sex { get; set; } = "Female";
        public double Score { get; set; }
        public string DependantsLabel => Dependants == 1 ? "1 Dependent" : $"{Dependants} Dependents";
        public string ScoreLabel => Score.ToString("0") + "%";

        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }

        public bool IsWithinQuota
        {
            get => _isWithinQuota;
            set
            {
                if (SetProperty(ref _isWithinQuota, value))
                {
                    OnPropertyChanged(nameof(QuotaBadgeBg));
                    OnPropertyChanged(nameof(QuotaBadgeFg));
                    OnPropertyChanged(nameof(QuotaBadgeText));
                }
            }
        }

        public SolidColorBrush QuotaBadgeBg => IsWithinQuota ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EAF6F0")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1"));
        public SolidColorBrush QuotaBadgeFg => IsWithinQuota ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17"));
        public string QuotaBadgeText => IsWithinQuota ? "Within Quota" : "Waitlist";

        public double EconomicStrainScore { get; set; } = 0.50;
        public double DependencyBurdenScore { get; set; } = 0.50;
        public double CareBurdenScore { get; set; } = 0.50;
        public double ResourceAdequacyScore { get; set; } = 0.50;

        public double MonthlyIncome { get; set; }
        public double IncomePerCapita { get; set; }
        public string EmploymentStatus { get; set; } = "Unemployed";
        public int MinorDependentsCount { get; set; }
        public int ToddlersUnder5Count { get; set; }
        public int CollegeAgeDependentsCount { get; set; }
        private string _circumstance = string.Empty;
        public string Circumstance { get => _circumstance; set { if (SetProperty(ref _circumstance, value)) { OnPropertyChanged(nameof(DepSecondaryMetric)); OnPropertyChanged(nameof(CircumstanceDisplay)); } } }
        private string _otherIncomeSource = string.Empty;
        public string OtherIncomeSource { get => _otherIncomeSource; set { if (SetProperty(ref _otherIncomeSource, value)) OnPropertyChanged(nameof(OtherIncomeDisplay)); } }
        private string _needsAndProblems = string.Empty;
        public string NeedsAndProblems { get => _needsAndProblems; set { if (SetProperty(ref _needsAndProblems, value)) OnPropertyChanged(nameof(NeedsAndProblemsDisplay)); } }

        public double Confidence { get; set; } = 0.85;
        public string ConfidencePercentLabel => $"{(Confidence * 100):0}% Confidence";

        public string ModelVersion { get; set; } = "v0.3.2-development";
        public bool IsFallbackScore => !string.IsNullOrEmpty(ModelVersion) && ModelVersion.IndexOf("fallback", StringComparison.OrdinalIgnoreCase) >= 0;
        public bool ShowModelConfidence => !IsFallbackScore;
        public string ModelSourceBadge => IsFallbackScore ? "Fallback" : "ML";
        public string ModelSourceFullBadge => IsFallbackScore ? "Statutory Heuristic (Fallback)" : $"SOLUM ML Model ({ModelVersion})";
        public string ModelSourceTooltip => IsFallbackScore ? "Rule-Based Heuristic: Calculated via statutory income rules because the ML microservice was unreachable." : $"Live ML Inference: Evaluated by SOLUM Model ({ModelVersion}) with {ConfidencePercentLabel}.";
        public SolidColorBrush ModelSourceBg => IsFallbackScore ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F2FE"));
        public SolidColorBrush ModelSourceFg => IsFallbackScore ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0369A1"));
        public SolidColorBrush ModelSourceBorder => IsFallbackScore ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCD34D")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BAE6FD"));
        public string FallbackAlertTitle => "Rule-Based Heuristic Fallback";
        public string FallbackAlertDesc => "This score was computed using statutory income criteria because the Python ML microservice was offline during evaluation. Re-evaluating will score with the live ML model.";

        // Triage Driver Labels & Brushes
        public string IncomeLabel => $"₱{MonthlyIncome:N0}/mo";
        public string PerCapitaLabel => $"₱{IncomePerCapita:N0} / capita";
        public SolidColorBrush PerCapitaFg => IncomePerCapita < 1500 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")) : (IncomePerCapita < 2500 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B5563")));
        public string MinorsLabel => Dependants == 1 ? "1 Dependent" : $"{Dependants} Dependents";
        public string ToddlersLabel => Children0To6Count > 0 ? (Children0To6Count == 1 ? "1 Dependent (0–6y)" : $"{Children0To6Count} Dependents (0–6y)") : "0 Dependents (0–6y)";
        public SolidColorBrush ToddlersFg => Children0To6Count >= 2 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")) : (Children0To6Count == 1 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")));

        // Prominent Metrics for the 4 Dimensions
        public string EconomicStrainPercentLabel => $"{EconomicStrainScore * 100:0}%";
        public string DependencyBurdenPercentLabel => $"{DependencyBurdenScore * 100:0}%";
        public string CareBurdenPercentLabel => $"{CareBurdenScore * 100:0}%";
        public string ResourceAdequacyPercentLabel => $"{ResourceAdequacyScore * 100:0}%";

        public string EcoPrimaryMetric => $"₱{MonthlyIncome:N0} / month";
        public string EcoSecondaryMetric => string.IsNullOrWhiteSpace(EmploymentStatus) ? "Employment: Not Specified" : $"Employment: {EmploymentStatus}";
        public string DepPrimaryMetric => $"{Dependants} Total Dependent" + (Dependants == 1 ? "" : "s");
        public string DepSecondaryMetric => string.IsNullOrWhiteSpace(CivilStatus)
            ? (string.IsNullOrWhiteSpace(Circumstance) ? "Sole earner supporting family" : $"Sole earner supporting family • {Circumstance}")
            : $"Sole earner supporting family • {CivilStatus}";

        private int _children0To6Count, _children7To22Count;
        public int Children0To6Count
        {
            get => _children0To6Count > 0 ? _children0To6Count : (ChildrenDetails != null && ChildrenDetails.Count > 0 ? ChildrenDetails.Count(c => c.Age >= 0 && c.Age <= 6) : ToddlersUnder5Count);
            set { if (SetProperty(ref _children0To6Count, value)) { OnPropertyChanged(nameof(Children0To6Text)); OnPropertyChanged(nameof(ShowCareBurden)); OnPropertyChanged(nameof(CarePrimaryMetric)); OnPropertyChanged(nameof(ToddlersLabel)); OnPropertyChanged(nameof(ToddlersFg)); } }
        }
        public int Children7To22Count
        {
            get => _children7To22Count > 0 ? _children7To22Count : (ChildrenDetails != null && ChildrenDetails.Count > 0 ? ChildrenDetails.Count(c => c.Age >= 7 && c.Age <= 22) : Math.Max(0, Dependants - Children0To6Count));
            set { if (SetProperty(ref _children7To22Count, value)) { OnPropertyChanged(nameof(Children7To22Text)); OnPropertyChanged(nameof(ShowCareBurden)); } }
        }
        public string Children7To22Text => Children7To22Count == 1 ? "1 Dependent aged 7–22" : $"{Children7To22Count} Dependents aged 7–22";
        public string Children0To6Text => Children0To6Count == 1 ? "1 Dependent aged 0–6" : $"{Children0To6Count} Dependents aged 0–6";
        public bool HasChildren7To22 => Children7To22Count > 0;
        public bool HasChildren0To6 => Children0To6Count > 0;
        public bool ShowCareBurden => Children0To6Count > 0 && Children7To22Count == 0;

        public string CarePrimaryMetric => Children0To6Count > 0 ? (Children0To6Count == 1 ? "1 Dependent (0–6 yrs)" : $"{Children0To6Count} Dependents (0–6 yrs)") : "0 Dependents (0–6 yrs)";
        public string CareSecondaryMetric => "Sole primary caregiver • Work constrained";

        public string CircumstanceDisplay => !string.IsNullOrWhiteSpace(Circumstance) && !Circumstance.Equals("None", StringComparison.OrdinalIgnoreCase) ? Circumstance : "—";
        public string NeedsAndProblemsDisplay => !string.IsNullOrWhiteSpace(NeedsAndProblems) && !NeedsAndProblems.Equals("None", StringComparison.OrdinalIgnoreCase) ? NeedsAndProblems : "—";
        public string OtherIncomeDisplay => !string.IsNullOrWhiteSpace(OtherIncomeSource) && !OtherIncomeSource.Equals("None", StringComparison.OrdinalIgnoreCase) ? OtherIncomeSource : "—";

        // Specific Children List for Dependency Dimension Vertical List
        private List<ChildDetailItem> _childrenDetails = new List<ChildDetailItem>();
        public List<ChildDetailItem> ChildrenDetails
        {
            get => _childrenDetails;
            set
            {
                if (SetProperty(ref _childrenDetails, value))
                {
                    OnPropertyChanged(nameof(HasChildrenDetails));
                    OnPropertyChanged(nameof(Children0To6Count));
                    OnPropertyChanged(nameof(Children7To22Count));
                    OnPropertyChanged(nameof(Children7To22Text));
                    OnPropertyChanged(nameof(Children0To6Text));
                    OnPropertyChanged(nameof(ShowCareBurden));
                    OnPropertyChanged(nameof(CarePrimaryMetric));
                    OnPropertyChanged(nameof(DepPrimaryMetric));
                    OnPropertyChanged(nameof(Dependants));
                    OnPropertyChanged(nameof(DependantsLabel));
                    OnPropertyChanged(nameof(MinorsLabel));
                    OnPropertyChanged(nameof(ToddlersLabel));
                    OnPropertyChanged(nameof(ToddlersFg));
                }
            }
        }
        public bool HasChildrenDetails => ChildrenDetails != null && ChildrenDetails.Count > 0;

        // Cross-Check Overlap Flags (1-3 words, clean text, no emojis)
        public bool HasCollegeAgeDependent => CollegeAgeDependentsCount > 0;
        public bool IsPantawidBeneficiary { get; set; }
        public string CrossCheckText => HasCollegeAgeDependent ? "City Educ Check" : "None";
        public SolidColorBrush CrossCheckFg => HasCollegeAgeDependent ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF"));
        public string AuditBadgeText => CrossCheckText;
        public SolidColorBrush AuditBadgeBg => HasCollegeAgeDependent ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFBEB")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4"));
        public SolidColorBrush AuditBadgeFg => CrossCheckFg;
        public SolidColorBrush AuditBadgeBorder => HasCollegeAgeDependent ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE68A")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBF7D0"));

        public bool IsDisqualified { get; set; }
        public string DisqualificationReason { get; set; }
        public bool HasAuditNotice => HasCollegeAgeDependent;
        public string AuditBannerTitle => "Departmental Cross-Check Required";
        public string AuditBannerDesc => "Applicant has college-age dependent(s). Cross-reference with City Education Dept scholarship roster.";

        public string Status { get => _status; set { if (SetProperty(ref _status, value)) { OnPropertyChanged(nameof(StatusColor)); OnPropertyChanged(nameof(StatusTextColor)); } } }
        public SolidColorBrush PriorityColor => string.Equals(Priority, "High", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8E8")) : (string.Equals(Priority, "Medium", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")));
        public SolidColorBrush PriorityTextColor => string.Equals(Priority, "High", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")) : (string.Equals(Priority, "Medium", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")));
        public SolidColorBrush ScoreBarColor => Score >= 75 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")) : (Score >= 50 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")));
        public SolidColorBrush StatusColor => Status == "Approved" ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")) : (Status == "Rejected" ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8E8")) : (Status == "Waitlisted" ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF0F5"))));
        public SolidColorBrush StatusTextColor => Status == "Approved" ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")) : (Status == "Rejected" ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")) : (Status == "Waitlisted" ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#702943"))));
    }

    public class ChildDetailItem
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string AgeDisplay => $"{Age} yr" + (Age == 1 ? " old" : "s old");
        public SolidColorBrush BadgeBg => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F4F6"));
        public SolidColorBrush BadgeFg => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B5563"));
    }
}
