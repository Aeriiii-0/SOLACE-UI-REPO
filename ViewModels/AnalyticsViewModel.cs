using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using SOLUM_UI.Models.Api;

namespace SOLUM_UI.ViewModels
{
    public class AnalyticsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // KPI Properties
        private int _totalSoloParents;
        public int TotalSoloParents
        {
            get => _totalSoloParents;
            set
            {
                SetProperty(ref _totalSoloParents, value);
                OnPropertyChanged(nameof(DisplayTotalEnrolled));
            }
        }

        private int _femaleCount;
        public int FemaleCount
        {
            get => _femaleCount;
            set
            {
                SetProperty(ref _femaleCount, value);
                OnPropertyChanged(nameof(FemaleGridLength));
                OnPropertyChanged(nameof(MaleGridLength));
            }
        }

        private int _maleCount;
        public int MaleCount
        {
            get => _maleCount;
            set
            {
                SetProperty(ref _maleCount, value);
                OnPropertyChanged(nameof(FemaleGridLength));
                OnPropertyChanged(nameof(MaleGridLength));
            }
        }

        private int _totalDependents;
        public int TotalDependents
        {
            get => _totalDependents;
            set => SetProperty(ref _totalDependents, value);
        }

        private int _lgbtCount;
        public int LgbtCount
        {
            get => _lgbtCount;
            set => SetProperty(ref _lgbtCount, value);
        }

        private int _pantawidBeneficiaryCount;
        public int PantawidBeneficiaryCount
        {
            get => _pantawidBeneficiaryCount;
            set => SetProperty(ref _pantawidBeneficiaryCount, value);
        }

        private int _indigenousCount;
        public int IndigenousCount
        {
            get => _indigenousCount;
            set => SetProperty(ref _indigenousCount, value);
        }

        // Top Barangay Properties
        private string _topBarangayName = string.Empty;
        public string TopBarangayName
        {
            get => _topBarangayName;
            set => SetProperty(ref _topBarangayName, value);
        }

        private int _topBarangayCount;
        public int TopBarangayCount
        {
            get => _topBarangayCount;
            set => SetProperty(ref _topBarangayCount, value);
        }

        // Dashboard Operational KPI Properties (from GET /api/Analytics/dashboard)
        private int _totalRegistered;
        public int TotalRegistered
        {
            get => _totalRegistered;
            set
            {
                SetProperty(ref _totalRegistered, value);
                OnPropertyChanged(nameof(DisplayTotalEnrolled));
            }
        }

        public int DisplayTotalEnrolled
        {
            get => IsFilterActive ? _totalSoloParents : (_totalRegistered > 0 ? _totalRegistered : _totalSoloParents);
            set { }
        }

        private int _activeRecords;
        public int ActiveRecords
        {
            get => _activeRecords;
            set
            {
                SetProperty(ref _activeRecords, value);
                OnPropertyChanged(nameof(DisplayActiveRecords));
            }
        }

        private int _inactiveRecords;
        public int InactiveRecords
        {
            get => _inactiveRecords;
            set
            {
                SetProperty(ref _inactiveRecords, value);
                OnPropertyChanged(nameof(DisplayInactiveRecords));
            }
        }

        private int _filteredActiveRecords;
        public int FilteredActiveRecords
        {
            get => _filteredActiveRecords;
            set
            {
                SetProperty(ref _filteredActiveRecords, value);
                OnPropertyChanged(nameof(DisplayActiveRecords));
            }
        }

        public int DisplayActiveRecords
        {
            get => IsFilterActive ? _filteredActiveRecords : (_activeRecords > 0 ? _activeRecords : _filteredActiveRecords);
            set { }
        }

        private int _filteredInactiveRecords;
        public int FilteredInactiveRecords
        {
            get => _filteredInactiveRecords;
            set
            {
                SetProperty(ref _filteredInactiveRecords, value);
                OnPropertyChanged(nameof(DisplayInactiveRecords));
            }
        }

        public int DisplayInactiveRecords
        {
            get => IsFilterActive ? _filteredInactiveRecords : (_inactiveRecords > 0 ? _inactiveRecords : _filteredInactiveRecords);
            set { }
        }

        private int _activeChangeFromLastMonth;
        public int ActiveChangeFromLastMonth
        {
            get => _activeChangeFromLastMonth;
            set
            {
                SetProperty(ref _activeChangeFromLastMonth, value);
                OnPropertyChanged(nameof(ActiveChangeText));
                OnPropertyChanged(nameof(ActiveChangeIsPositive));
            }
        }

        public string ActiveChangeText
        {
            get
            {
                if (_activeChangeFromLastMonth > 0)
                    return $"+{_activeChangeFromLastMonth} this month";
                else if (_activeChangeFromLastMonth < 0)
                    return $"{_activeChangeFromLastMonth} this month";
                return "0 change";
            }
        }

        public bool ActiveChangeIsPositive => _activeChangeFromLastMonth >= 0;

        private int _renewalsDueThisMonth;
        public int RenewalsDueThisMonth
        {
            get => _renewalsDueThisMonth;
            set => SetProperty(ref _renewalsDueThisMonth, value);
        }

        private int _renewalsCompletedToday;
        public int RenewalsCompletedToday
        {
            get => _renewalsCompletedToday;
            set => SetProperty(ref _renewalsCompletedToday, value);
        }

        private int _renewalsThisMonth;
        public int RenewalsThisMonth
        {
            get => _renewalsThisMonth;
            set => SetProperty(ref _renewalsThisMonth, value);
        }

        private int _registeredThisMonth;
        public int RegisteredThisMonth
        {
            get => _registeredThisMonth;
            set
            {
                SetProperty(ref _registeredThisMonth, value);
                OnPropertyChanged(nameof(DisplayRegisteredCount));
            }
        }

        private int _filteredNewRegistrations;
        public int FilteredNewRegistrations
        {
            get => _filteredNewRegistrations;
            set
            {
                SetProperty(ref _filteredNewRegistrations, value);
                OnPropertyChanged(nameof(DisplayRegisteredCount));
            }
        }

        public int DisplayRegisteredCount
        {
            get
            {
                if (IsFilterActive)
                    return _filteredNewRegistrations;
                return _registeredThisMonth > 0 ? _registeredThisMonth : _filteredNewRegistrations;
            }
            set { }
        }

        public string DisplayRegisteredLabel
        {
            get
            {
                if (!IsFilterActive)
                    return "Enrolled This Month";

                switch (FilterType)
                {
                    case "Yearly":
                        return "Enrolled This Year";
                    case "Quarterly":
                        return "Enrolled This Quarter";
                    case "CustomRange":
                        return "Enrolled in Period";
                    case "Monthly":
                    default:
                        return "Enrolled in Month";
                }
            }
            set { }
        }

        private int _filteredRenewals;
        public int FilteredRenewals
        {
            get => _filteredRenewals;
            set => SetProperty(ref _filteredRenewals, value);
        }

        // Active Filter Summary Indicator
        private string _activeFilterSummary = "Citywide (All Barangays) • Current Month";
        public string ActiveFilterSummary
        {
            get => _activeFilterSummary;
            set => SetProperty(ref _activeFilterSummary, value);
        }

        private bool _isFilterActive;
        public bool IsFilterActive
        {
            get => _isFilterActive;
            set
            {
                SetProperty(ref _isFilterActive, value);
                OnPropertyChanged(nameof(DisplayTotalEnrolled));
                OnPropertyChanged(nameof(DisplayActiveRecords));
                OnPropertyChanged(nameof(DisplayInactiveRecords));
                OnPropertyChanged(nameof(DisplayRegisteredCount));
                OnPropertyChanged(nameof(DisplayRegisteredLabel));
            }
        }

        // Hierarchical Period Filter Properties
        private string _filterType = "Monthly";
        public string FilterType
        {
            get => _filterType;
            set
            {
                SetProperty(ref _filterType, value);
                OnPropertyChanged(nameof(DisplayRegisteredLabel));
            }
        }

        private int _selectedYear = DateTime.Today.Year;
        public int SelectedYear
        {
            get => _selectedYear;
            set => SetProperty(ref _selectedYear, value);
        }

        private int _selectedMonth = DateTime.Today.Month;
        public int SelectedMonth
        {
            get => _selectedMonth;
            set => SetProperty(ref _selectedMonth, value);
        }

        private int _selectedQuarter = (DateTime.Today.Month - 1) / 3 + 1;
        public int SelectedQuarter
        {
            get => _selectedQuarter;
            set => SetProperty(ref _selectedQuarter, value);
        }

        // Solo Parent Statutory Age Brackets (from MonthlyAnalyticsDto.AgeBrackets)
        private int _parentAge19Below;
        public int ParentAge19Below { get => _parentAge19Below; set => SetProperty(ref _parentAge19Below, value); }

        private int _parentAge20_39;
        public int ParentAge20_39 { get => _parentAge20_39; set => SetProperty(ref _parentAge20_39, value); }

        private int _parentAge40_59;
        public int ParentAge40_59 { get => _parentAge40_59; set => SetProperty(ref _parentAge40_59, value); }

        private int _parentAge60Above;
        public int ParentAge60Above { get => _parentAge60Above; set => SetProperty(ref _parentAge60Above, value); }

        private double _parentAge19BelowPercent;
        public double ParentAge19BelowPercent
        {
            get => _parentAge19BelowPercent;
            set
            {
                SetProperty(ref _parentAge19BelowPercent, value);
                OnPropertyChanged(nameof(ParentAge19BelowGridLength));
                OnPropertyChanged(nameof(ParentAge19BelowRemainingGridLength));
            }
        }

        private double _parentAge20_39Percent;
        public double ParentAge20_39Percent
        {
            get => _parentAge20_39Percent;
            set
            {
                SetProperty(ref _parentAge20_39Percent, value);
                OnPropertyChanged(nameof(ParentAge20_39GridLength));
                OnPropertyChanged(nameof(ParentAge20_39RemainingGridLength));
            }
        }

        private double _parentAge40_59Percent;
        public double ParentAge40_59Percent
        {
            get => _parentAge40_59Percent;
            set
            {
                SetProperty(ref _parentAge40_59Percent, value);
                OnPropertyChanged(nameof(ParentAge40_59GridLength));
                OnPropertyChanged(nameof(ParentAge40_59RemainingGridLength));
            }
        }

        private double _parentAge60AbovePercent;
        public double ParentAge60AbovePercent
        {
            get => _parentAge60AbovePercent;
            set
            {
                SetProperty(ref _parentAge60AbovePercent, value);
                OnPropertyChanged(nameof(ParentAge60AboveGridLength));
                OnPropertyChanged(nameof(ParentAge60AboveRemainingGridLength));
            }
        }

        // Proportional GridLengths for accurate bar widths
        public System.Windows.GridLength FemaleGridLength =>
            new System.Windows.GridLength(FemaleCount > 0 ? FemaleCount : (MaleCount > 0 ? 0 : 1), System.Windows.GridUnitType.Star);

        public System.Windows.GridLength MaleGridLength =>
            new System.Windows.GridLength(MaleCount > 0 ? MaleCount : (FemaleCount > 0 ? 0 : 1), System.Windows.GridUnitType.Star);

        public System.Windows.GridLength ParentAge19BelowGridLength =>
            new System.Windows.GridLength(Math.Max(ParentAge19BelowPercent, 0), System.Windows.GridUnitType.Star);
        public System.Windows.GridLength ParentAge19BelowRemainingGridLength =>
            new System.Windows.GridLength(Math.Max(100.0 - ParentAge19BelowPercent, 0.001), System.Windows.GridUnitType.Star);

        public System.Windows.GridLength ParentAge20_39GridLength =>
            new System.Windows.GridLength(Math.Max(ParentAge20_39Percent, 0), System.Windows.GridUnitType.Star);
        public System.Windows.GridLength ParentAge20_39RemainingGridLength =>
            new System.Windows.GridLength(Math.Max(100.0 - ParentAge20_39Percent, 0.001), System.Windows.GridUnitType.Star);

        public System.Windows.GridLength ParentAge40_59GridLength =>
            new System.Windows.GridLength(Math.Max(ParentAge40_59Percent, 0), System.Windows.GridUnitType.Star);
        public System.Windows.GridLength ParentAge40_59RemainingGridLength =>
            new System.Windows.GridLength(Math.Max(100.0 - ParentAge40_59Percent, 0.001), System.Windows.GridUnitType.Star);

        public System.Windows.GridLength ParentAge60AboveGridLength =>
            new System.Windows.GridLength(Math.Max(ParentAge60AbovePercent, 0), System.Windows.GridUnitType.Star);
        public System.Windows.GridLength ParentAge60AboveRemainingGridLength =>
            new System.Windows.GridLength(Math.Max(100.0 - ParentAge60AbovePercent, 0.001), System.Windows.GridUnitType.Star);

        public System.Windows.GridLength IncomeBelowMinGridLength =>
            new System.Windows.GridLength(IncomeBelowMinCount > 0 ? IncomeBelowMinCount : (IncomeBelowMinCount + IncomeMinWageCount + IncomeAboveMinCount > 0 ? 0 : 1), System.Windows.GridUnitType.Star);

        public System.Windows.GridLength IncomeMinWageGridLength =>
            new System.Windows.GridLength(IncomeMinWageCount > 0 ? IncomeMinWageCount : (IncomeBelowMinCount + IncomeMinWageCount + IncomeAboveMinCount > 0 ? 0 : 1), System.Windows.GridUnitType.Star);

        public System.Windows.GridLength IncomeAboveMinGridLength =>
            new System.Windows.GridLength(IncomeAboveMinCount > 0 ? IncomeAboveMinCount : (IncomeBelowMinCount + IncomeMinWageCount + IncomeAboveMinCount > 0 ? 0 : 1), System.Windows.GridUnitType.Star);

        private double _parentAge19BelowBarWidth;
        public double ParentAge19BelowBarWidth { get => _parentAge19BelowBarWidth; set => SetProperty(ref _parentAge19BelowBarWidth, value); }

        private double _parentAge20_39BarWidth;
        public double ParentAge20_39BarWidth { get => _parentAge20_39BarWidth; set => SetProperty(ref _parentAge20_39BarWidth, value); }

        private double _parentAge40_59BarWidth;
        public double ParentAge40_59BarWidth { get => _parentAge40_59BarWidth; set => SetProperty(ref _parentAge40_59BarWidth, value); }

        private double _parentAge60AboveBarWidth;
        public double ParentAge60AboveBarWidth { get => _parentAge60AboveBarWidth; set => SetProperty(ref _parentAge60AboveBarWidth, value); }

        // Age Group Data (for Donut Chart)
        private ObservableCollection<DonutSegment> _ageGroupData;
        public ObservableCollection<DonutSegment> AgeGroupData
        {
            get => _ageGroupData;
            set => SetProperty(ref _ageGroupData, value);
        }

        // Civil Status Data (for List)
        private ObservableCollection<ListItem> _civilStatusData;
        public ObservableCollection<ListItem> CivilStatusData
        {
            get => _civilStatusData;
            set => SetProperty(ref _civilStatusData, value);
        }

        // Employment Status Data (for Progress Rows)
        private ObservableCollection<ProgressItem> _employmentStatusData;
        public ObservableCollection<ProgressItem> EmploymentStatusData
        {
            get => _employmentStatusData;
            set => SetProperty(ref _employmentStatusData, value);
        }

        // Income Bracket Data (for Progress Rows)
        private ObservableCollection<ProgressItem> _incomeBracketData;
        public ObservableCollection<ProgressItem> IncomeBracketData
        {
            get => _incomeBracketData;
            set => SetProperty(ref _incomeBracketData, value);
        }

        // Dependents Age Data (for Column Chart)
        private ObservableCollection<ColumnChartData> _dependentsAgeData;
        public ObservableCollection<ColumnChartData> DependentsAgeData
        {
            get => _dependentsAgeData;
            set => SetProperty(ref _dependentsAgeData, value);
        }

        private ObservableCollection<AreaChartData> _barangayData;
        public ObservableCollection<AreaChartData> BarangayData
        {
            get => _barangayData;
            set => SetProperty(ref _barangayData, value);
        }

        private ObservableCollection<AreaChartData> _topCasesData;
        public ObservableCollection<AreaChartData> TopCasesData
        {
            get => _topCasesData;
            set => SetProperty(ref _topCasesData, value);
        }

        // New properties for simplified UI
        private int _singleCount;
        public int SingleCount
        {
            get => _singleCount;
            set => SetProperty(ref _singleCount, value);
        }

        private int _marriedCount;
        public int MarriedCount
        {
            get => _marriedCount;
            set => SetProperty(ref _marriedCount, value);
        }

        private int _widowedCount;
        public int WidowedCount
        {
            get => _widowedCount;
            set => SetProperty(ref _widowedCount, value);
        }

        private int _separatedCount;
        public int SeparatedCount
        {
            get => _separatedCount;
            set => SetProperty(ref _separatedCount, value);
        }

        private int _annulledCount;
        public int AnnulledCount
        {
            get => _annulledCount;
            set => SetProperty(ref _annulledCount, value);
        }

        // Employment percentages
        private double _employedPercent;
        public double EmployedPercent
        {
            get => _employedPercent;
            set => SetProperty(ref _employedPercent, value);
        }

        private double _selfEmployedPercent;
        public double SelfEmployedPercent
        {
            get => _selfEmployedPercent;
            set => SetProperty(ref _selfEmployedPercent, value);
        }

        private double _notEmployedPercent;
        public double NotEmployedPercent
        {
            get => _notEmployedPercent;
            set => SetProperty(ref _notEmployedPercent, value);
        }

        // Arc path properties for employment charts (dynamically calculated)
        private string _employedArcPath;
        public string EmployedArcPath
        {
            get => _employedArcPath;
            set => SetProperty(ref _employedArcPath, value);
        }

        private string _selfEmployedArcPath;
        public string SelfEmployedArcPath
        {
            get => _selfEmployedArcPath;
            set => SetProperty(ref _selfEmployedArcPath, value);
        }

        private string _notEmployedArcPath;
        public string NotEmployedArcPath
        {
            get => _notEmployedArcPath;
            set => SetProperty(ref _notEmployedArcPath, value);
        }

        private int _ageGroup0_5;
        public int AgeGroup0_5
        {
            get => _ageGroup0_5;
            set => SetProperty(ref _ageGroup0_5, value);
        }

        private int _ageGroup6_12;
        public int AgeGroup6_12
        {
            get => _ageGroup6_12;
            set => SetProperty(ref _ageGroup6_12, value);
        }

        private int _ageGroup13_17;
        public int AgeGroup13_17
        {
            get => _ageGroup13_17;
            set => SetProperty(ref _ageGroup13_17, value);
        }

        private string _employedText;
        public string EmployedText
        {
            get => _employedText;
            set => SetProperty(ref _employedText, value);
        }

        private string _selfEmployedText;
        public string SelfEmployedText
        {
            get => _selfEmployedText;
            set => SetProperty(ref _selfEmployedText, value);
        }

        private string _notEmployedText;
        public string NotEmployedText
        {
            get => _notEmployedText;
            set => SetProperty(ref _notEmployedText, value);
        }

        private string _femalePercentText;
        public string FemalePercentText
        {
            get => _femalePercentText;
            set => SetProperty(ref _femalePercentText, value);
        }

        private double _femalePercentWidth;
        public double FemalePercentWidth
        {
            get => _femalePercentWidth;
            set => SetProperty(ref _femalePercentWidth, value);
        }

        private DateTime? _startDate;
        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        private DateTime? _endDate;
        public DateTime? EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        private string _incomeBelowMinText;
        public string IncomeBelowMinText
        {
            get => _incomeBelowMinText;
            set => SetProperty(ref _incomeBelowMinText, value);
        }

        private string _incomeMinWageText;
        public string IncomeMinWageText
        {
            get => _incomeMinWageText;
            set => SetProperty(ref _incomeMinWageText, value);
        }

        private double _incomeBelowMinPercentWidth;
        public double IncomeBelowMinPercentWidth
        {
            get => _incomeBelowMinPercentWidth;
            set => SetProperty(ref _incomeBelowMinPercentWidth, value);
        }

        private double _ageGroup0_5Percent;
        public double AgeGroup0_5Percent
        {
            get => _ageGroup0_5Percent;
            set => SetProperty(ref _ageGroup0_5Percent, value);
        }

        private double _ageGroup6_12Percent;
        public double AgeGroup6_12Percent
        {
            get => _ageGroup6_12Percent;
            set => SetProperty(ref _ageGroup6_12Percent, value);
        }

        private double _ageGroup13_17Percent;
        public double AgeGroup13_17Percent
        {
            get => _ageGroup13_17Percent;
            set => SetProperty(ref _ageGroup13_17Percent, value);
        }

        private DateTime _lastRefreshTime;
        public DateTime LastRefreshTime
        {
            get => _lastRefreshTime;
            set => SetProperty(ref _lastRefreshTime, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>True when the current data set has at least one record.</summary>
        private bool _hasData;
        public bool HasData
        {
            get => _hasData;
            set
            {
                if (!Equals(_hasData, value))
                {
                    _hasData = value;
                    OnPropertyChanged(nameof(HasData));
                    OnPropertyChanged(nameof(IsNoData));
                }
            }
        }

        /// <summary>True when the filter returned zero results — drives the empty-state banner.</summary>
        public bool IsNoData => !_hasData;


        // Pagination properties
        private int _currentBarangayPage;
        public int CurrentBarangayPage
        {
            get => _currentBarangayPage;
            set => SetProperty(ref _currentBarangayPage, value);
        }

        private int _totalBarangayPages;
        public int TotalBarangayPages
        {
            get => _totalBarangayPages;
            set => SetProperty(ref _totalBarangayPages, value);
        }

        private bool _hasNextBarangayPage;
        public bool HasNextBarangayPage
        {
            get => _hasNextBarangayPage;
            set => SetProperty(ref _hasNextBarangayPage, value);
        }

        private bool _hasPreviousBarangayPage;
        public bool HasPreviousBarangayPage
        {
            get => _hasPreviousBarangayPage;
            set => SetProperty(ref _hasPreviousBarangayPage, value);
        }

        // All barangays list (for pagination)
        private ObservableCollection<AreaChartData> _allBarangayData;
        public ObservableCollection<AreaChartData> AllBarangayData
        {
            get => _allBarangayData;
            set => SetProperty(ref _allBarangayData, value);
        }

        // Income breakdown details
        private int _incomeBelowMinCount;
        public int IncomeBelowMinCount
        {
            get => _incomeBelowMinCount;
            set
            {
                SetProperty(ref _incomeBelowMinCount, value);
                OnPropertyChanged(nameof(IncomeBelowMinGridLength));
                OnPropertyChanged(nameof(IncomeMinWageGridLength));
                OnPropertyChanged(nameof(IncomeAboveMinGridLength));
            }
        }

        private int _incomeMinWageCount;
        public int IncomeMinWageCount
        {
            get => _incomeMinWageCount;
            set
            {
                SetProperty(ref _incomeMinWageCount, value);
                OnPropertyChanged(nameof(IncomeBelowMinGridLength));
                OnPropertyChanged(nameof(IncomeMinWageGridLength));
                OnPropertyChanged(nameof(IncomeAboveMinGridLength));
            }
        }

        private int _incomeAboveMinCount;
        public int IncomeAboveMinCount
        {
            get => _incomeAboveMinCount;
            set
            {
                SetProperty(ref _incomeAboveMinCount, value);
                OnPropertyChanged(nameof(IncomeBelowMinGridLength));
                OnPropertyChanged(nameof(IncomeMinWageGridLength));
                OnPropertyChanged(nameof(IncomeAboveMinGridLength));
            }
        }

        private double _incomeBelowMinPercent;
        public double IncomeBelowMinPercent
        {
            get => _incomeBelowMinPercent;
            set => SetProperty(ref _incomeBelowMinPercent, value);
        }

        private double _incomeMinWagePercent;
        public double IncomeMinWagePercent
        {
            get => _incomeMinWagePercent;
            set => SetProperty(ref _incomeMinWagePercent, value);
        }

        private double _incomeAboveMinPercent;
        public double IncomeAboveMinPercent
        {
            get => _incomeAboveMinPercent;
            set => SetProperty(ref _incomeAboveMinPercent, value);
        }

        private double _incomeMinWagePercentWidth;
        public double IncomeMinWagePercentWidth
        {
            get => _incomeMinWagePercentWidth;
            set => SetProperty(ref _incomeMinWagePercentWidth, value);
        }

        private double _incomeAboveMinPercentWidth;
        public double IncomeAboveMinPercentWidth
        {
            get => _incomeAboveMinPercentWidth;
            set => SetProperty(ref _incomeAboveMinPercentWidth, value);
        }

        private double _ageGroup0_5BarHeight;
        public double AgeGroup0_5BarHeight
        {
            get => _ageGroup0_5BarHeight;
            set => SetProperty(ref _ageGroup0_5BarHeight, value);
        }

        private double _ageGroup6_12BarHeight;
        public double AgeGroup6_12BarHeight
        {
            get => _ageGroup6_12BarHeight;
            set => SetProperty(ref _ageGroup6_12BarHeight, value);
        }

        private double _ageGroup13_17BarHeight;
        public double AgeGroup13_17BarHeight
        {
            get => _ageGroup13_17BarHeight;
            set => SetProperty(ref _ageGroup13_17BarHeight, value);
        }

        // Selected Barangay Filter
        private string _selectedBarangay = "ALL";
        public string SelectedBarangay
        {
            get => _selectedBarangay;
            set => SetProperty(ref _selectedBarangay, value);
        }

        // Analytics DTO for export purposes
        private MonthlyAnalyticsDto _analyticsData;
        public MonthlyAnalyticsDto AnalyticsData
        {
            get => _analyticsData;
            set => SetProperty(ref _analyticsData, value);
        }

        /// <summary>
        /// Constructor - Initializes collections and default values
        /// </summary>
        public AnalyticsViewModel()
        {
            AgeGroupData = new ObservableCollection<DonutSegment>();
            CivilStatusData = new ObservableCollection<ListItem>();
            EmploymentStatusData = new ObservableCollection<ProgressItem>();
            IncomeBracketData = new ObservableCollection<ProgressItem>();
            DependentsAgeData = new ObservableCollection<ColumnChartData>();
            BarangayData = new ObservableCollection<AreaChartData>();
            AllBarangayData = new ObservableCollection<AreaChartData>();
            TopCasesData = new ObservableCollection<AreaChartData>();
            LastRefreshTime = DateTime.Now;
            IsLoading = false;
            HasData = true; // Assume data exists until a load proves otherwise
            CurrentBarangayPage = 1;
            TotalBarangayPages = 1;
            HasNextBarangayPage = false;
            HasPreviousBarangayPage = false;
            
            // Initialize date range to current month
            StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            EndDate = DateTime.Now;
            SelectedBarangay = "ALL";
            UpdateActiveFilterSummary();
        }

        /// <summary>
        /// Updates the active filter indicator summary string and filter active flag
        /// </summary>
        public void UpdateActiveFilterSummary()
        {
            string bName = string.IsNullOrWhiteSpace(SelectedBarangay) || SelectedBarangay.Equals("ALL", StringComparison.OrdinalIgnoreCase)
                ? "Citywide (All Barangays)"
                : $"Barangay {SelectedBarangay}";

            string periodStr;
            bool isCustomDate = false;

            switch (FilterType)
            {
                case "Yearly":
                    periodStr = $"Year {SelectedYear}";
                    isCustomDate = (SelectedYear != DateTime.Today.Year);
                    break;

                case "Quarterly":
                    periodStr = $"Q{SelectedQuarter} {SelectedYear}";
                    int currentQuarter = (DateTime.Today.Month - 1) / 3 + 1;
                    isCustomDate = (SelectedYear != DateTime.Today.Year || SelectedQuarter != currentQuarter);
                    break;

                case "CustomRange":
                    if (StartDate.HasValue && EndDate.HasValue)
                        periodStr = $"{StartDate.Value:MMM dd, yyyy} – {EndDate.Value:MMM dd, yyyy}";
                    else if (StartDate.HasValue)
                        periodStr = $"From {StartDate.Value:MMM dd, yyyy}";
                    else
                        periodStr = "Custom Range";
                    isCustomDate = true;
                    break;

                case "Monthly":
                default:
                    int y = SelectedYear > 0 ? SelectedYear : DateTime.Today.Year;
                    int m = (SelectedMonth >= 1 && SelectedMonth <= 12) ? SelectedMonth : DateTime.Today.Month;
                    string monthName = new DateTime(y, m, 1).ToString("MMMM yyyy");
                    periodStr = monthName;
                    isCustomDate = (SelectedYear != DateTime.Today.Year || SelectedMonth != DateTime.Today.Month);
                    break;
            }

            ActiveFilterSummary = $"{bName}  •  {periodStr}";

            bool isCustomBarangay = !string.IsNullOrWhiteSpace(SelectedBarangay) && !SelectedBarangay.Equals("ALL", StringComparison.OrdinalIgnoreCase);
            bool isNotDefaultType = !string.Equals(FilterType, "Monthly", StringComparison.OrdinalIgnoreCase);

            IsFilterActive = isCustomBarangay || isCustomDate || isNotDefaultType;
        }

        /// <summary>
        /// Populate ViewModel from DashboardAnalyticsDto (operational metrics)
        /// </summary>
        public void LoadDashboardDto(DashboardAnalyticsDto dto)
        {
            if (dto == null) return;
            TotalRegistered = dto.TotalRegistered;
            ActiveRecords = dto.ActiveRecords;
            InactiveRecords = dto.InactiveRecords;
            ActiveChangeFromLastMonth = dto.ActiveChangeFromLastMonth;
            RenewalsDueThisMonth = dto.RenewalsDueThisMonth;
            RenewalsCompletedToday = dto.RenewalsCompletedToday;
            RenewalsThisMonth = dto.RenewalsThisMonth;
            RegisteredThisMonth = dto.RegisteredThisMonth;
        }

        /// <summary>
        /// Populate Barangay breakdown from BarangayAnalyticsResponseDto (24 canonical Biñan barangays)
        /// </summary>
        public void LoadBarangayBreakdownDto(BarangayAnalyticsResponseDto dto)
        {
            if (dto == null || dto.Barangays == null) return;

            if (IsFilterActive)
            {
                if (dto.TotalNewRegistrations > 0)
                    FilteredNewRegistrations = dto.TotalNewRegistrations;
                if (dto.TotalActive > 0)
                    FilteredActiveRecords = dto.TotalActive;
                if (dto.TotalInactive > 0)
                    FilteredInactiveRecords = dto.TotalInactive;
                if (dto.TotalRenewals > 0)
                    FilteredRenewals = dto.TotalRenewals;
            }

            AllBarangayData.Clear();

            int maxVal = dto.Barangays.Count > 0 ? System.Linq.Enumerable.Max(dto.Barangays, b => b.TotalSoloParents) : 1;
            if (maxVal <= 0) maxVal = 1;

            int rank = 1;
            foreach (var b in dto.Barangays)
            {
                double barWidth = (b.TotalSoloParents * 1.0 / maxVal) * 280;

                AllBarangayData.Add(new AreaChartData
                {
                    Label = b.BarangayName,
                    Value = b.TotalSoloParents,
                    XPosition = rank - 1,
                    Rank = rank++,
                    Color = "#B03058",
                    BarWidth = Math.Max(barWidth, b.TotalSoloParents > 0 ? 8 : 0),
                    MaxBarangayValue = maxVal,
                    PercentageShare = b.PercentageShare,
                    ActiveCount = b.ActiveCount,
                    InactiveCount = b.InactiveCount,
                    NewRegistrationsCount = b.NewRegistrationsCount,
                    RenewalsCount = b.RenewalsCount
                });
            }

            const int barangaysPerPage = 10;
            TotalBarangayPages = Math.Max(1, (int)Math.Ceiling(AllBarangayData.Count / (double)barangaysPerPage));
            CurrentBarangayPage = 1;
            UpdateBarangayPage();
        }


        /// <summary>
        /// Navigate to next barangay page
        /// </summary>
        public void NextBarangayPage()
        {
            if (HasNextBarangayPage)
            {
                CurrentBarangayPage++;
                UpdateBarangayPage();
            }
        }

        /// <summary>
        /// Navigate to previous barangay page
        /// </summary>
        public void PreviousBarangayPage()
        {
            if (HasPreviousBarangayPage)
            {
                CurrentBarangayPage--;
                UpdateBarangayPage();
            }
        }

        /// <summary>
        /// Update the displayed barangay data for current page
        /// </summary>
        private void UpdateBarangayPage()
        {
            const int itemsPerPage = 10;
            int startIndex = (CurrentBarangayPage - 1) * itemsPerPage;
            int endIndex = Math.Min(startIndex + itemsPerPage, AllBarangayData.Count);

            BarangayData.Clear();
            for (int i = startIndex; i < endIndex; i++)
            {
                BarangayData.Add(AllBarangayData[i]);
            }

            UpdateTopBarangay();
            
            // Update navigation buttons
            HasPreviousBarangayPage = CurrentBarangayPage > 1;
            HasNextBarangayPage = CurrentBarangayPage < TotalBarangayPages;
        }

        /// <summary>
        /// Update top barangay name and count from AllBarangayData
        /// </summary>
        private void UpdateTopBarangay()
        {
            if (AllBarangayData.Count > 0)
            {
                var topBarangay = AllBarangayData[0];
                TopBarangayName = topBarangay.Label;
                TopBarangayCount = topBarangay.Value;
            }
            else
            {
                TopBarangayName = "No Data";
                TopBarangayCount = 0;
            }
        }

        /// <summary>
        /// Safely retrieves an integer value from a dictionary using case-insensitive matching across multiple key aliases.
        /// </summary>
        public static int GetDictValue(System.Collections.Generic.IDictionary<string, int> dict, params string[] keys)
        {
            if (dict == null) return 0;
            foreach (var key in keys)
            {
                if (dict.TryGetValue(key, out int val)) return val;
                foreach (var kvp in dict)
                {
                    if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                        return kvp.Value;
                }
            }
            return 0;
        }

        /// <summary>
        /// Populate ViewModel from MonthlyAnalyticsDto
        /// </summary>
        public void LoadFromAnalyticsDto(MonthlyAnalyticsDto dto)
        {
            if (dto == null)
            {
                System.Diagnostics.Debug.WriteLine("✗ LoadFromAnalyticsDto: dto is null");
                return;
            }

            System.Diagnostics.Debug.WriteLine("→ LoadFromAnalyticsDto: Processing analytics data...");

            // Store the DTO for export purposes
            AnalyticsData = dto;

            // Calculate KPIs
            TotalSoloParents = dto.TotalSoloParents;
            FilteredActiveRecords = dto.ActiveSoloParents;
            FilteredInactiveRecords = dto.InactiveSoloParents;
            FilteredNewRegistrations = dto.NewRegistrations;
            FilteredRenewals = dto.Renewals;

            // If not in a custom filter, synchronize the real-time card numbers
            if (!IsFilterActive)
            {
                if (dto.NewRegistrations > 0 || RegisteredThisMonth == 0)
                    RegisteredThisMonth = dto.NewRegistrations;
                if (dto.ActiveSoloParents > 0)
                    ActiveRecords = dto.ActiveSoloParents;
                if (dto.InactiveSoloParents > 0)
                    InactiveRecords = dto.InactiveSoloParents;
            }

            System.Diagnostics.Debug.WriteLine($"  TotalSoloParents: {TotalSoloParents}, Active: {dto.ActiveSoloParents}, Inactive: {dto.InactiveSoloParents}, New: {dto.NewRegistrations}");
            
            // Female/Male from Sex dictionary (null-safe & case-insensitive)
            var sex = dto.Sex ?? new System.Collections.Generic.Dictionary<string, int>();
            int female = GetDictValue(sex, "Female", "female", "F");
            int male = GetDictValue(sex, "Male", "male", "M");
            FemaleCount = female;
            MaleCount = male;
            System.Diagnostics.Debug.WriteLine($"  Female: {female}, Male: {male}");

            // Calculate female/male percentages for progress bar and summary label
            int totalGender = female + male;
            int genderBase = TotalSoloParents > 0 ? TotalSoloParents : totalGender;
            if (genderBase > 0)
            {
                double femalePercent = (female * 100.0 / genderBase);
                double malePercent = (male * 100.0 / genderBase);
                FemalePercentText = $"{femalePercent:F1}% Female  ·  {malePercent:F1}% Male";
                FemalePercentWidth = femalePercent * 2.5;
            }
            else
            {
                FemalePercentText = "0% Female  ·  0% Male";
                FemalePercentWidth = 0;
            }

            // Total dependents sum (null-safe)
            var dependentsAge = dto.DependentsAgeBrackets ?? new System.Collections.Generic.Dictionary<string, int>();
            TotalDependents = 0;
            foreach (var kvp in dependentsAge)
            {
                TotalDependents += kvp.Value;
            }

            // Special Sectors (null-safe, case-insensitive, with sub-breakdown fallback)
            int lgbt = GetDictValue(dto.Sectors, "isLGBT", "lgbt", "LGBT");
            if (lgbt <= 0 && dto.Lgbt != null)
            {
                lgbt = GetDictValue(dto.Lgbt, "LGBT", "Yes", "true");
            }
            LgbtCount = lgbt;

            int pantawid = GetDictValue(dto.Sectors, "isPantawidBeneficiary", "pantawidBeneficiary", "pantawid");
            if (pantawid <= 0 && dto.PantawidBeneficiary != null)
            {
                pantawid = GetDictValue(dto.PantawidBeneficiary, "Beneficiary", "Yes", "true");
            }
            PantawidBeneficiaryCount = pantawid;

            int indigenous = GetDictValue(dto.Sectors, "isIndigenous", "indigenous", "IP");
            if (indigenous <= 0 && dto.Indigenous != null)
            {
                indigenous = GetDictValue(dto.Indigenous, "Indigenous", "Yes", "true");
            }
            IndigenousCount = indigenous;

            // Load civil status simple counts (null-safe & case-insensitive)
            var civilStatus = dto.CivilStatus ?? new System.Collections.Generic.Dictionary<string, int>();
            SingleCount = GetDictValue(civilStatus, "Single", "single");
            MarriedCount = GetDictValue(civilStatus, "Married", "married");
            WidowedCount = GetDictValue(civilStatus, "Widowed", "widowed", "Widow");
            SeparatedCount = GetDictValue(civilStatus, "Separated", "separated");
            AnnulledCount = GetDictValue(civilStatus, "Annulled", "annulled", "Divorced", "divorced");

            // Load employment status simple text counts (null-safe & case-insensitive)
            var employment = dto.EmploymentStatus ?? new System.Collections.Generic.Dictionary<string, int>();
            int employed = GetDictValue(employment, "Employed", "employed");
            int selfEmployed = GetDictValue(employment, "Self-Employed", "Self-employed", "self_employed", "Self Employed", "selfemployed");
            int notEmployed = GetDictValue(employment, "Unemployed", "unemployed", "Not Employed", "not_employed", "Not-Employed", "not employed");

            int totalEmpBase = (employed + selfEmployed + notEmployed > 0) ? (employed + selfEmployed + notEmployed) : TotalSoloParents;

            EmployedPercent = totalEmpBase > 0 ? (employed * 100.0 / totalEmpBase) : 0;
            SelfEmployedPercent = totalEmpBase > 0 ? (selfEmployed * 100.0 / totalEmpBase) : 0;
            NotEmployedPercent = totalEmpBase > 0 ? (notEmployed * 100.0 / totalEmpBase) : 0;

            EmployedText = $"{employed} ({EmployedPercent:F1}%)";
            SelfEmployedText = $"{selfEmployed} ({SelfEmployedPercent:F1}%)";
            NotEmployedText = $"{notEmployed} ({NotEmployedPercent:F1}%)";

            // Generate SVG arc paths for employment ring charts (64x64 canvas, radius 28, center at 32,32)
            EmployedArcPath = GenerateArcPath(EmployedPercent);
            SelfEmployedArcPath = GenerateArcPath(SelfEmployedPercent);
            NotEmployedArcPath = GenerateArcPath(NotEmployedPercent);

            // Load dependents age brackets (null-safe & case-insensitive)
            AgeGroup0_5 = GetDictValue(dependentsAge, "6_AND_BELOW", "0_6", "0-6", "6 and below");
            AgeGroup6_12 = GetDictValue(dependentsAge, "7_TO_22", "7_22", "7-22", "7_TO_21");
            AgeGroup13_17 = GetDictValue(dependentsAge, "22_AND_ABOVE", "22_ABOVE", "22 and above", "22+");

            int totalDependentsByAge = AgeGroup0_5 + AgeGroup6_12 + AgeGroup13_17;
            if (totalDependentsByAge > 0)
            {
                AgeGroup0_5Percent = (AgeGroup0_5 * 100.0 / totalDependentsByAge);
                AgeGroup6_12Percent = (AgeGroup6_12 * 100.0 / totalDependentsByAge);
                AgeGroup13_17Percent = (AgeGroup13_17 * 100.0 / totalDependentsByAge);
            }
            else
            {
                AgeGroup0_5Percent = 0;
                AgeGroup6_12Percent = 0;
                AgeGroup13_17Percent = 0;
            }

            // Calculate age group bar heights relative to the max bracket (scale to 0-115px for visual display)
            int maxDepCount = Math.Max(AgeGroup0_5, Math.Max(AgeGroup6_12, AgeGroup13_17));
            const double maxBarHeight = 115.0;
            if (maxDepCount > 0)
            {
                AgeGroup0_5BarHeight = AgeGroup0_5 > 0 ? Math.Max(8.0, (AgeGroup0_5 * 1.0 / maxDepCount) * maxBarHeight) : 0;
                AgeGroup6_12BarHeight = AgeGroup6_12 > 0 ? Math.Max(8.0, (AgeGroup6_12 * 1.0 / maxDepCount) * maxBarHeight) : 0;
                AgeGroup13_17BarHeight = AgeGroup13_17 > 0 ? Math.Max(8.0, (AgeGroup13_17 * 1.0 / maxDepCount) * maxBarHeight) : 0;
            }
            else
            {
                AgeGroup0_5BarHeight = 0;
                AgeGroup6_12BarHeight = 0;
                AgeGroup13_17BarHeight = 0;
            }

            // Load income brackets with detailed calculations (null-safe & case-insensitive)
            var incomeBrackets = dto.MonthlyIncomeBrackets ?? new System.Collections.Generic.Dictionary<string, int>();
            int incomeBelowMin = GetDictValue(incomeBrackets, "BELOW_MINIMUM_WAGE", "below_minimum_wage");
            int incomeMinWagePlus = GetDictValue(incomeBrackets, "MIN_WAGE_PLUS1_TO_20833", "min_wage_plus1_to_20833");
            int incomeAboveMin = GetDictValue(incomeBrackets, "20834_AND_ABOVE", "20834_and_above");

            int totalWithIncome = incomeBelowMin + incomeMinWagePlus + incomeAboveMin;
            IncomeBelowMinCount = incomeBelowMin;
            IncomeMinWageCount = incomeMinWagePlus;
            IncomeAboveMinCount = incomeAboveMin;

            if (totalWithIncome > 0)
            {
                IncomeBelowMinPercent = (incomeBelowMin * 100.0 / totalWithIncome);
                IncomeMinWagePercent = (incomeMinWagePlus * 100.0 / totalWithIncome);
                IncomeAboveMinPercent = (incomeAboveMin * 100.0 / totalWithIncome);

                IncomeBelowMinText = $"{incomeBelowMin} · {IncomeBelowMinPercent:F1}%";
                IncomeMinWageText = $"{incomeMinWagePlus} · {IncomeMinWagePercent:F1}%";
            }
            else
            {
                IncomeBelowMinPercent = 0;
                IncomeMinWagePercent = 0;
                IncomeAboveMinPercent = 0;
                IncomeBelowMinText = "0 · 0%";
                IncomeMinWageText = "0 · 0%";
            }

            double totalBarWidth = 300;
            IncomeBelowMinPercentWidth = (IncomeBelowMinPercent / 100.0) * totalBarWidth;
            IncomeMinWagePercentWidth = (IncomeMinWagePercent / 100.0) * totalBarWidth;
            IncomeAboveMinPercentWidth = (IncomeAboveMinPercent / 100.0) * totalBarWidth;

            // Load Solo Parent Statutory Age Brackets (null-safe & case-insensitive)
            var ageBrackets = dto.AgeBrackets ?? new System.Collections.Generic.Dictionary<string, int>();
            ParentAge19Below = GetDictValue(ageBrackets, "19_AND_BELOW", "19_and_below", "19_below");
            ParentAge20_39 = GetDictValue(ageBrackets, "20_39", "20-39");
            ParentAge40_59 = GetDictValue(ageBrackets, "40_59", "40-59");
            ParentAge60Above = GetDictValue(ageBrackets, "60_AND_ABOVE", "60_and_above", "60_above");

            int totalParentAge = ParentAge19Below + ParentAge20_39 + ParentAge40_59 + ParentAge60Above;
            if (totalParentAge > 0)
            {
                ParentAge19BelowPercent = (ParentAge19Below * 100.0 / totalParentAge);
                ParentAge20_39Percent = (ParentAge20_39 * 100.0 / totalParentAge);
                ParentAge40_59Percent = (ParentAge40_59 * 100.0 / totalParentAge);
                ParentAge60AbovePercent = (ParentAge60Above * 100.0 / totalParentAge);
            }
            else
            {
                ParentAge19BelowPercent = 0;
                ParentAge20_39Percent = 0;
                ParentAge40_59Percent = 0;
                ParentAge60AbovePercent = 0;
            }

            ParentAge19BelowBarWidth = Math.Max(0, (ParentAge19BelowPercent / 100.0) * 240.0);
            ParentAge20_39BarWidth = Math.Max(0, (ParentAge20_39Percent / 100.0) * 240.0);
            ParentAge40_59BarWidth = Math.Max(0, (ParentAge40_59Percent / 100.0) * 240.0);
            ParentAge60AboveBarWidth = Math.Max(0, (ParentAge60AbovePercent / 100.0) * 240.0);

            // Update Donut Chart Data
            AgeGroupData.Clear();
            var ageColors = new[] { "#7B1426", "#A14452", "#D9A3AB", "#EAD9DB" };
            var ageLabels = new[] { "19 & Below", "20-39", "40-59", "60 & Above" };
            var ageCounts = new[] { ParentAge19Below, ParentAge20_39, ParentAge40_59, ParentAge60Above };
            for (int i = 0; i < ageCounts.Length; i++)
            {
                AgeGroupData.Add(new DonutSegment 
                { 
                    Label = ageLabels[i], 
                    Value = ageCounts[i], 
                    Color = ageColors[i] 
                });
            }

            // Load Civil Status Collection
            CivilStatusData.Clear();
            var civilStatusOrder = new[] { "Single", "Married", "Widowed", "Separated", "Annulled" };
            var civilStatusCounts = new[] { SingleCount, MarriedCount, WidowedCount, SeparatedCount, AnnulledCount };
            for (int i = 0; i < civilStatusOrder.Length; i++)
            {
                CivilStatusData.Add(new ListItem { Label = civilStatusOrder[i], Value = civilStatusCounts[i] });
            }

            // Load Employment Status Collection
            EmploymentStatusData.Clear();
            var empLabels = new[] { "Employed", "Self-Employed", "Not Employed" };
            var empCounts = new[] { employed, selfEmployed, notEmployed };
            var empPercentages = new[] { EmployedPercent, SelfEmployedPercent, NotEmployedPercent };
            for (int i = 0; i < empLabels.Length; i++)
            {
                EmploymentStatusData.Add(new ProgressItem 
                { 
                    Label = empLabels[i], 
                    Value = empCounts[i], 
                    Percentage = empPercentages[i] 
                });
            }

            // Load Income Bracket Collection
            IncomeBracketData.Clear();
            var incomeLabels = new[] { "Below Minimum Wage", "Min Wage + ₱1-₱20,833", "₱20,834 & Above" };
            var incomeCounts = new[] { incomeBelowMin, incomeMinWagePlus, incomeAboveMin };
            var incomePercentages = new[] { IncomeBelowMinPercent, IncomeMinWagePercent, IncomeAboveMinPercent };
            for (int i = 0; i < incomeLabels.Length; i++)
            {
                IncomeBracketData.Add(new ProgressItem 
                { 
                    Label = incomeLabels[i], 
                    Value = incomeCounts[i], 
                    Percentage = incomePercentages[i] 
                });
            }

            // Load Dependents Age Column Chart Data
            DependentsAgeData.Clear();
            var depLabels = new[] { "6 & Below", "7-22", "22 & Above" };
            var depCounts = new[] { AgeGroup0_5, AgeGroup6_12, AgeGroup13_17 };
            var depColors = new[] { "#7B1426", "#A14452", "#D9A3AB" };
            for (int i = 0; i < depLabels.Length; i++)
            {
                DependentsAgeData.Add(new ColumnChartData 
                { 
                    Label = depLabels[i], 
                    Value = depCounts[i],
                    Color = depColors[i]
                });
            }

            // Load Top Cases (Classification/Circumstance) with horizontal bar data
            TopCasesData.Clear();
            var categories = dto.Categories ?? new System.Collections.Generic.Dictionary<string, int>();
            if (categories.Count > 0)
            {
                var sortedCases = categories.OrderByDescending(kvp => kvp.Value).ToList();
                int maxCaseCount = sortedCases.Count > 0 ? sortedCases[0].Value : 1;
                if (maxCaseCount <= 0) maxCaseCount = 1;

                int rank = 1;
                foreach (var kvp in sortedCases.Take(10))  // Limit to top 10 categories
                {
                    double barWidth = (kvp.Value * 1.0 / maxCaseCount) * 280;
                    TopCasesData.Add(new AreaChartData
                    {
                        Label = kvp.Key,
                        Value = kvp.Value,
                        BarWidth = barWidth,
                        Rank = rank,
                        Color = rank == 1 ? "#7D2245" : "#E58BA3"  // Top is maroon, rest are rose
                    });
                    rank++;
                }
            }

            // Get Top Barangay from AllBarangayData (set by LoadBarangayBreakdownDto)
            // This will be populated when barangay data loads
            UpdateTopBarangay();

            LastRefreshTime = DateTime.Now;

            // Update empty-state flag
            HasData = TotalSoloParents > 0;
        }

        /// <summary>
        /// Generate SVG arc path for employment ring charts.
        /// Canvas: 64x64, Radius: 28, Center: (32, 32)
        /// Arc starts at top (0°) and sweeps clockwise based on percentage.
        /// </summary>
        private string GenerateArcPath(double percentage)
        {
            if (percentage <= 0.001) return string.Empty;
            if (percentage >= 100.0) percentage = 99.99;

            const double centerX = 32.0;
            const double centerY = 32.0;
            const double radius = 28.0;

            // Convert percentage to degrees (0-360)
            double degrees = (percentage / 100.0) * 360.0;
            double radians = degrees * Math.PI / 180.0;

            // Start point (top center: 0°)
            double startX = centerX;
            double startY = centerY - radius;

            // End point (calculated from degrees)
            double endX = centerX + radius * Math.Sin(radians);
            double endY = centerY - radius * Math.Cos(radians);

            // Determine if arc is large (> 180°)
            int isLargeArc = degrees > 180.0 ? 1 : 0;

            // Build SVG arc path using InvariantCulture to avoid decimal localization bugs
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "M {0:F2},{1:F2} A {2:F2},{2:F2} 0 {3},1 {4:F2},{5:F2}",
                startX, startY, radius, isLargeArc, endX, endY);
        }

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DonutSegment
    {
        public string Label { get; set; }
        public int Value { get; set; }
        public string Color { get; set; }
    }

    public class ListItem
    {
        public string Label { get; set; }
        public int Value { get; set; }
    }

    public class ProgressItem
    {
        public string Label { get; set; }
        public int Value { get; set; }
        public double Percentage { get; set; }
    }

    public class ColumnChartData
    {
        public string Label { get; set; }
        public int Value { get; set; }
        public string Color { get; set; }
    }

    public class AreaChartData
    {
        public string Label { get; set; }
        public int Value { get; set; }
        public int XPosition { get; set; }
        public int Rank { get; set; }
        public string Color { get; set; }
        public double BarWidth { get; set; }
        public double MaxBarangayValue { get; set; } = 1;
        public double PercentageShare { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
        public int NewRegistrationsCount { get; set; }
        public int RenewalsCount { get; set; }

        public System.Windows.GridLength BarGridLength =>
            new System.Windows.GridLength(Math.Max(Value, 0), System.Windows.GridUnitType.Star);

        public System.Windows.GridLength RemainingGridLength =>
            new System.Windows.GridLength(Math.Max(MaxBarangayValue - Value, 0.001), System.Windows.GridUnitType.Star);

        public string PercentageShareText => $"{PercentageShare:F1}%";

        /// <summary>
        /// Returns Label in case-presented format (original casing preserved)
        /// </summary>
        public string DisplayLabel
        {
            get => Label;
        }

        public string TooltipSummary => 
            $"{Label}: {Value} Solo Parents ({PercentageShare:F1}%)\n" +
            $"Active: {ActiveCount}  |  Inactive: {InactiveCount}\n" +
            $"New: {NewRegistrationsCount}  |  Renewals: {RenewalsCount}";
    }
}
