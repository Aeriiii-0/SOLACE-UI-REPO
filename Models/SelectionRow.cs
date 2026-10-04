using System;
using System.Windows.Media;
using SOLUM_UI.Core;

namespace SOLUM_UI.Models
{
    public class SelectionRow : ObservableObject
    {
        private bool _isFinalGrantee;

        public int Rank { get; set; }
        public string SpId { get; set; }
        public string Name { get; set; }
        public string Barangay { get; set; }
        public string SubsidyType { get; set; } = "Allowance";
        public string Priority { get; set; }
        public double Score { get; set; }
        public bool IsWaitlisted { get; set; }
        public string StatusText => IsWaitlisted ? "Waitlisted" : "Shortlisted";

        public bool IsFinalGrantee
        {
            get => _isFinalGrantee;
            set
            {
                if (SetProperty(ref _isFinalGrantee, value))
                {
                    OnPropertyChanged(nameof(FinalBadgeColor));
                    OnPropertyChanged(nameof(FinalBadgeText));
                    OnPropertyChanged(nameof(FinalBadgeTextColor));
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

        public string CrossCheckText =>
            HasCollegeAgeDependent
                ? "City Educ Check"
                : (IsPantawidBeneficiary ? "Pantawid 4Ps" : "Cleared");

        public SolidColorBrush CrossCheckFg =>
            HasCollegeAgeDependent
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"))
                : (IsPantawidBeneficiary
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D4ED8"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A")));

        public string ComplianceRemark
        {
            get
            {
                string depWord = Dependants == 1 ? "1 Dependent" : $"{Dependants} Dependents";
                if (HasCollegeAgeDependent)
                {
                    string colWord = CollegeAgeDependentsCount == 1 ? "1 College-Age" : $"{CollegeAgeDependentsCount} College-Age";
                    return $"{depWord} ({colWord}: 17–24y) • City Educ Scholarship Match Req.";
                }
                if (IsPantawidBeneficiary)
                {
                    return $"{depWord} • DSWD Pantawid 4Ps Overlap (Deduplication Req.)";
                }
                string minWord = MinorDependentsCount == 1 ? "1 Minor" : $"{MinorDependentsCount} Minors";
                return $"{depWord} ({minWord}) • Cleared (No Inter-Agency Overlap)";
            }
        }

        public SolidColorBrush ComplianceRemarkFg =>
            HasCollegeAgeDependent
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#92400E"))
                : (IsPantawidBeneficiary
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E40AF"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534")));

        public SolidColorBrush ComplianceRemarkBg =>
            HasCollegeAgeDependent
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"))
                : (IsPantawidBeneficiary
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBEAFE"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7")));

        public string AuditBadgeText => CrossCheckText;

        public SolidColorBrush AuditBadgeBg =>
            HasCollegeAgeDependent
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFBEB"))
                : (IsPantawidBeneficiary
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4")));

        public SolidColorBrush AuditBadgeFg => CrossCheckFg;

        public SolidColorBrush AuditBadgeBorder =>
            HasCollegeAgeDependent
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE68A"))
                : (IsPantawidBeneficiary
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BFDBFE"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBF7D0")));

        public SolidColorBrush RankBg =>
            IsWaitlisted
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EAF6F0"));

        public SolidColorBrush RankFg =>
            IsWaitlisted
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));

        public SolidColorBrush RowBg =>
            IsWaitlisted
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFDF5"))
                : new SolidColorBrush(Colors.White);

        public SolidColorBrush ScoreBarColor
        {
            get
            {
                if (Score >= 75) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545"));
                if (Score >= 50) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17"));
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
            }
        }

        public SolidColorBrush PriorityColor
        {
            get
            {
                switch (Priority)
                {
                    case "High": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8E8"));
                    case "Medium": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1"));
                    default: return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
                }
            }
        }

        public SolidColorBrush PriorityTextColor
        {
            get
            {
                switch (Priority)
                {
                    case "High": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545"));
                    case "Medium": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17"));
                    default: return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                }
            }
        }

        public SolidColorBrush FinalBadgeColor =>
            IsFinalGrantee
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"));

        public string FinalBadgeText => IsFinalGrantee ? "✓ Granted" : "Pending";

        public SolidColorBrush FinalBadgeTextColor =>
            IsFinalGrantee
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
    }
}
