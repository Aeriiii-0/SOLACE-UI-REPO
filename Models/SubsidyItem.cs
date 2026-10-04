using System;
using System.Collections.Generic;
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
        public string SpId { get; set; }
        public string Name { get; set; }
        public string Barangay { get; set; }
        public string SubsidyType { get; set; } = "Allowance";
        public string Priority { get; set; }
        public int Dependants { get; set; }
        public string CivilStatus { get; set; }
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
        public string Circumstance { get; set; } = "Abandonment (A2)";
        public string OtherIncomeSource { get; set; } = "None";
        public string NeedsAndProblems { get; set; } = "Financial & Livelihood Aid";

        public double Confidence { get; set; } = 0.85;
        public string ConfidencePercentLabel => $"{(Confidence * 100):0}% Confidence";

        // Triage Driver Labels & Brushes
        public string IncomeLabel => $"₱{MonthlyIncome:N0}/mo";
        public string PerCapitaLabel => $"₱{IncomePerCapita:N0} / capita";
        public SolidColorBrush PerCapitaFg => IncomePerCapita < 1500 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")) : (IncomePerCapita < 2500 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B5563")));
        public string MinorsLabel => $"{MinorDependentsCount} minor" + (MinorDependentsCount == 1 ? "" : "s");
        public string ToddlersLabel => ToddlersUnder5Count > 0 ? $"{ToddlersUnder5Count} toddler" + (ToddlersUnder5Count == 1 ? "" : "s") : "0 toddlers";
        public SolidColorBrush ToddlersFg => ToddlersUnder5Count >= 2 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")) : (ToddlersUnder5Count == 1 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")));

        // Prominent Metrics for the 4 Dimensions
        public string EconomicStrainPercentLabel => $"{EconomicStrainScore * 100:0}%";
        public string DependencyBurdenPercentLabel => $"{DependencyBurdenScore * 100:0}%";
        public string CareBurdenPercentLabel => $"{CareBurdenScore * 100:0}%";
        public string ResourceAdequacyPercentLabel => $"{ResourceAdequacyScore * 100:0}%";

        public string EcoPrimaryMetric => $"₱{MonthlyIncome:N0} / month";
        public string EcoSecondaryMetric => $"₱{IncomePerCapita:N0} per capita • {EmploymentStatus}";
        public string DepPrimaryMetric => $"{MinorDependentsCount} Minor Children";
        public string DepSecondaryMetric => $"Sole earner supporting {Dependants} total • {CivilStatus}";
        public string CarePrimaryMetric => ToddlersUnder5Count > 0 ? $"{ToddlersUnder5Count} Toddler" + (ToddlersUnder5Count == 1 ? " under 5 yrs" : "s under 5 yrs") : "No Toddlers (<5 yrs)";
        public string CareSecondaryMetric => ToddlersUnder5Count > 0 ? "Sole primary caregiver • Work constrained" : "School-age dependents only";
        public string ResPrimaryMetric => string.IsNullOrWhiteSpace(OtherIncomeSource) || OtherIncomeSource.Equals("None", StringComparison.OrdinalIgnoreCase) ? "Other Income: None Reported" : $"Other Income: {OtherIncomeSource}";
        public string ResSecondaryMetric => $"Needs: {(string.IsNullOrWhiteSpace(NeedsAndProblems) ? "Financial Aid" : NeedsAndProblems)} • {(IsPantawidBeneficiary ? "4Ps Beneficiary" : "Non-4Ps")}";

        // Specific Children List for Dependency Dimension Vertical List
        public List<ChildDetailItem> ChildrenDetails { get; set; } = new List<ChildDetailItem>();
        public bool HasChildrenDetails => ChildrenDetails != null && ChildrenDetails.Count > 0;

        // Cross-Check Overlap Flags (1-3 words, clean text, no emojis)
        public bool HasCollegeAgeDependent => CollegeAgeDependentsCount > 0;
        public bool IsPantawidBeneficiary { get; set; }
        public string CrossCheckText => HasCollegeAgeDependent ? "City Educ Check" : (IsPantawidBeneficiary ? "Pantawid 4Ps" : "None");
        public SolidColorBrush CrossCheckFg => HasCollegeAgeDependent ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309")) : (IsPantawidBeneficiary ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D4ED8")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")));
        public string AuditBadgeText => CrossCheckText;
        public SolidColorBrush AuditBadgeBg => HasCollegeAgeDependent ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFBEB")) : (IsPantawidBeneficiary ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4")));
        public SolidColorBrush AuditBadgeFg => CrossCheckFg;
        public SolidColorBrush AuditBadgeBorder => HasCollegeAgeDependent ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE68A")) : (IsPantawidBeneficiary ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BFDBFE")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBF7D0")));

        public bool IsDisqualified { get; set; }
        public string DisqualificationReason { get; set; }
        public bool HasAuditNotice => HasCollegeAgeDependent || IsPantawidBeneficiary;
        public string AuditBannerTitle => HasCollegeAgeDependent ? "⚠️ Departmental Cross-Check Required" : "ℹ️ Social Welfare Benefit Overlap Check";
        public string AuditBannerDesc => HasCollegeAgeDependent ? "Applicant has college-age dependent(s). Cross-reference with City Education Dept scholarship roster." : "Applicant tagged as Pantawid Pamilya (4Ps) beneficiary. Ensure subsidy deduplication.";

        public string Status { get => _status; set { if (SetProperty(ref _status, value)) { OnPropertyChanged(nameof(StatusColor)); OnPropertyChanged(nameof(StatusTextColor)); } } }
        public SolidColorBrush PriorityColor => Priority == "High" ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8E8")) : (Priority == "Medium" ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")));
        public SolidColorBrush PriorityTextColor => Priority == "High" ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")) : (Priority == "Medium" ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")));
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
