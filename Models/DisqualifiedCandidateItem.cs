using System;
using System.Windows.Media;
using SOLUM_UI.Core;

namespace SOLUM_UI.Models
{
    public class DisqualifiedCandidateItem : ObservableObject
    {
        private string _fullName;
        private string _spId;
        private string _barangay;
        private double _monthlyIncome;
        private int _dependents;
        private double _priorityScore;
        private string _priority = "Medium";
        private string _rejectionReason = "Ineligible Income / Undisclosed Assets";
        private string _rejectionNotes = string.Empty;
        private string _disqualifiedBy = "Administrator";
        private DateTime? _disqualifiedAt = DateTime.Now;

        public Guid EvaluationId { get; set; }
        public Guid CycleId { get; set; }
        public Guid SoloParentId { get; set; }

        public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }
        public string SpId { get => _spId; set => SetProperty(ref _spId, value); }
        public string Barangay { get => _barangay; set => SetProperty(ref _barangay, value); }

        public double MonthlyIncome
        {
            get => _monthlyIncome;
            set
            {
                if (SetProperty(ref _monthlyIncome, value))
                {
                    OnPropertyChanged(nameof(FormattedIncome));
                }
            }
        }

        public string FormattedIncome { get => $"₱{MonthlyIncome:N2}"; set { } }

        public int Dependents { get => _dependents; set => SetProperty(ref _dependents, value); }

        public double PriorityScore { get => _priorityScore; set => SetProperty(ref _priorityScore, value); }

        public string Priority
        {
            get => _priority;
            set
            {
                if (SetProperty(ref _priority, value))
                {
                    OnPropertyChanged(nameof(PriorityBadgeBg));
                    OnPropertyChanged(nameof(PriorityBadgeFg));
                }
            }
        }

        public string RejectionReason
        {
            get => _rejectionReason;
            set
            {
                if (SetProperty(ref _rejectionReason, value))
                {
                    OnPropertyChanged(nameof(ReasonBadgeBg));
                    OnPropertyChanged(nameof(ReasonBadgeFg));
                    OnPropertyChanged(nameof(ReasonBadgeBorder));
                }
            }
        }

        public string RejectionNotes
        {
            get => _rejectionNotes;
            set
            {
                if (SetProperty(ref _rejectionNotes, value))
                {
                    OnPropertyChanged(nameof(DisplayNotes));
                }
            }
        }

        public string DisplayNotes
        {
            get => !string.IsNullOrWhiteSpace(RejectionNotes)
                ? RejectionNotes
                : "No additional justification notes recorded.";
            set { }
        }

        public string DisqualifiedBy { get => _disqualifiedBy; set => SetProperty(ref _disqualifiedBy, value); }

        public DateTime? DisqualifiedAt
        {
            get => _disqualifiedAt;
            set
            {
                if (SetProperty(ref _disqualifiedAt, value))
                {
                    OnPropertyChanged(nameof(FormattedDisqualifiedAt));
                }
            }
        }

        public string FormattedDisqualifiedAt
        {
            get => DisqualifiedAt.HasValue
                ? DisqualifiedAt.Value.ToString("MMM dd, yyyy h:mm tt")
                : "—";
            set { }
        }
        // Badges for Reasons
        public SolidColorBrush ReasonBadgeBg
        {
            get
            {
                string r = RejectionReason ?? string.Empty;
                if (r.IndexOf("Income", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
                if (r.IndexOf("Scholarship", StringComparison.OrdinalIgnoreCase) >= 0 || r.IndexOf("Educ", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEF2FF"));
                if (r.IndexOf("Duplicate", StringComparison.OrdinalIgnoreCase) >= 0 || r.IndexOf("Falsified", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
                if (r.IndexOf("Age", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                if (r.IndexOf("Voluntary", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3E8FF"));
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F4F6"));
            }
        }

        public SolidColorBrush ReasonBadgeFg
        {
            get
            {
                string r = RejectionReason ?? string.Empty;
                if (r.IndexOf("Income", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"));
                if (r.IndexOf("Scholarship", StringComparison.OrdinalIgnoreCase) >= 0 || r.IndexOf("Educ", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4338CA"));
                if (r.IndexOf("Duplicate", StringComparison.OrdinalIgnoreCase) >= 0 || r.IndexOf("Falsified", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
                if (r.IndexOf("Age", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
                if (r.IndexOf("Voluntary", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B21A8"));
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151"));
            }
        }

        public SolidColorBrush ReasonBadgeBorder
        {
            get
            {
                string r = RejectionReason ?? string.Empty;
                if (r.IndexOf("Income", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE68A"));
                if (r.IndexOf("Scholarship", StringComparison.OrdinalIgnoreCase) >= 0 || r.IndexOf("Educ", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C7D2FE"));
                if (r.IndexOf("Duplicate", StringComparison.OrdinalIgnoreCase) >= 0 || r.IndexOf("Falsified", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FECACA"));
                if (r.IndexOf("Age", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
                if (r.IndexOf("Voluntary", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9D5FF"));
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));
            }
        }
        // Priority Badges
        public SolidColorBrush PriorityBadgeBg
        {
            get
            {
                if (string.Equals(Priority, "High", StringComparison.OrdinalIgnoreCase))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
                if (string.Equals(Priority, "Medium", StringComparison.OrdinalIgnoreCase))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F2FE"));
            }
        }

        public SolidColorBrush PriorityBadgeFg
        {
            get
            {
                if (string.Equals(Priority, "High", StringComparison.OrdinalIgnoreCase))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
                if (string.Equals(Priority, "Medium", StringComparison.OrdinalIgnoreCase))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"));
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0369A1"));
            }
        }
    }
}