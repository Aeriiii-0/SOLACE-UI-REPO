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
        public bool IsWaitlisted { get; set; }
        public string StatusText => IsFinalGrantee ? "Confirmed" : "Waitlisted";

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

        public int Dependants { get; set; } = 3;
        public double MonthlyIncome { get; set; }
        public double IncomePerCapita { get; set; }
        public int MinorDependentsCount { get; set; }
        public int ToddlersUnder5Count { get; set; }
        public string Circumstance { get; set; } = "Abandonment (A2)";

        public string IncomeLabel => $"₱{MonthlyIncome:N0}/mo";
        public string PerCapitaLabel => $"₱{IncomePerCapita:N0} / capita";
        public SolidColorBrush PerCapitaFg => IncomePerCapita < 1500 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")) : (IncomePerCapita < 2500 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B5563")));
        public string MinorsLabel => $"{MinorDependentsCount} minor" + (MinorDependentsCount == 1 ? "" : "s");
        public string ToddlersLabel => ToddlersUnder5Count > 0 ? $"{ToddlersUnder5Count} toddler" + (ToddlersUnder5Count == 1 ? "" : "s") : "0 toddlers";
        public SolidColorBrush ToddlersFg => ToddlersUnder5Count >= 2 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")) : (ToddlersUnder5Count == 1 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")));

        public bool HasCollegeAgeDependent { get; set; }
        public int CollegeAgeDependentsCount { get; set; }
        public bool IsPantawidBeneficiary { get; set; }
        public List<ChildDetailItem> CollegeAgeDependents { get; set; } = new List<ChildDetailItem>();

        public string CrossCheckText =>
            HasCollegeAgeDependent ? "City Educ Check" : "Cleared";

        public SolidColorBrush CrossCheckFg =>
            HasCollegeAgeDependent
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));

        public string ComplianceRemark
        {
            get
            {
                string depWord = Dependants == 1 ? "1 Dependent" : $"{Dependants} Dependents";
                if (HasCollegeAgeDependent)
                {
                    var studentList = (CollegeAgeDependents != null && CollegeAgeDependents.Count > 0)
                        ? string.Join(", ", CollegeAgeDependents.Select(c => $"{c.Name} ({c.Age}y)"))
                        : (CollegeAgeDependentsCount == 1 ? "1 College-Age" : $"{CollegeAgeDependentsCount} College-Age");
                    return $"{depWord} • Student: {studentList} • City Educ Scholarship Match Req.";
                }
                string minWord = MinorDependentsCount == 1 ? "1 Minor" : $"{MinorDependentsCount} Minors";
                return $"{depWord} ({minWord}) • Cleared (No Inter-Agency Overlap)";
            }
        }

        public SolidColorBrush ComplianceRemarkFg =>
            HasCollegeAgeDependent
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#92400E"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));

        public SolidColorBrush ComplianceRemarkBg =>
            HasCollegeAgeDependent
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));

        public string Id => SpId;
        public string Sex { get; set; } = "Female";
        public string CivilStatus { get; set; } = "Single";
        public string DateOfBirthFormatted { get; set; } = "1988-05-14";
        public string LastUpdatedFormatted { get; set; } = "2026-10-04";
        private string _validUntilFormatted;
        public string ValidUntilFormatted
        {
            get
            {
                if (!string.IsNullOrEmpty(_validUntilFormatted)) return _validUntilFormatted;
                int hash = Math.Abs((SpId ?? Name ?? "0").GetHashCode());
                return $"2027-{((hash % 12) + 1):D2}-{((hash % 25) + 1):D2}";
            }
            set => _validUntilFormatted = value;
        }
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
                string minWord = MinorDependentsCount == 1 ? "1 minor" : $"{MinorDependentsCount} minors";
                string todWord = ToddlersUnder5Count > 0 ? $", {ToddlersUnder5Count} toddler" + (ToddlersUnder5Count == 1 ? "" : "s") : "";
                return $"{depWord} ({minWord}{todWord})";
            }
        }

        public string CollegeStudentSummary => (CollegeAgeDependents != null && CollegeAgeDependents.Count > 0)
            ? string.Join(", ", CollegeAgeDependents.Select(c => $"{c.Name} ({c.Age}y)"))
            : $"{CollegeAgeDependentsCount} College-Age Dependent(s) (17–24y)";

        public string ConfirmButtonText => IsFinalGrantee ? "✓ Confirmed" : "Confirm";
        public string ConfirmButtonToolTip => IsFinalGrantee ? "Click to remove from final grantees" : "Confirm as final grantee";
        public SolidColorBrush ConfirmButtonBg => IsFinalGrantee
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
        public SolidColorBrush ConfirmButtonFg => IsFinalGrantee
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"))
            : new SolidColorBrush(Colors.White);
        public SolidColorBrush ConfirmButtonBorder => IsFinalGrantee
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));

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

        public SolidColorBrush ScoreBarColor => Score >= 75 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")) : (Score >= 50 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")));
        public SolidColorBrush PriorityColor => string.Equals(Priority, "High", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8E8")) : (string.Equals(Priority, "Medium", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")));
        public SolidColorBrush PriorityTextColor => string.Equals(Priority, "High", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")) : (string.Equals(Priority, "Medium", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")));
        public SolidColorBrush FinalBadgeColor => IsFinalGrantee ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"));
        public string FinalBadgeText => IsFinalGrantee ? "✓ Granted" : "Pending";
        public SolidColorBrush FinalBadgeTextColor => IsFinalGrantee ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));

        public string ModelVersion { get; set; } = "v0.3.2-development";
        public bool IsFallbackScore => !string.IsNullOrEmpty(ModelVersion) && ModelVersion.IndexOf("fallback", StringComparison.OrdinalIgnoreCase) >= 0;
        public string ModelSourceBadge => IsFallbackScore ? "⚠ Fallback" : "ML";
        public string ModelSourceTooltip => IsFallbackScore ? "Rule-Based Fallback: Estimated via statutory income rules because the ML microservice was offline." : $"SOLUM ML Model ({ModelVersion})";
        public SolidColorBrush ModelSourceBg => IsFallbackScore ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F2FE"));
        public SolidColorBrush ModelSourceFg => IsFallbackScore ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0369A1"));
        public SolidColorBrush ModelSourceBorder => IsFallbackScore ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCD34D")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BAE6FD"));
    }
}
