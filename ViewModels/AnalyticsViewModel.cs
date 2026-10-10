using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            set => SetProperty(ref _totalSoloParents, value);
        }

        private int _femaleCount;
        public int FemaleCount
        {
            get => _femaleCount;
            set => SetProperty(ref _femaleCount, value);
        }

        private int _maleCount;
        public int MaleCount
        {
            get => _maleCount;
            set => SetProperty(ref _maleCount, value);
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
            set => SetProperty(ref _incomeBelowMinCount, value);
        }

        private int _incomeMinWageCount;
        public int IncomeMinWageCount
        {
            get => _incomeMinWageCount;
            set => SetProperty(ref _incomeMinWageCount, value);
        }

        private int _incomeAboveMinCount;
        public int IncomeAboveMinCount
        {
            get => _incomeAboveMinCount;
            set => SetProperty(ref _incomeAboveMinCount, value);
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
            CurrentBarangayPage = 1;
            TotalBarangayPages = 1;
            HasNextBarangayPage = false;
            HasPreviousBarangayPage = false;
            
            // Initialize date range to current month
            StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            EndDate = DateTime.Now;
            SelectedBarangay = "ALL";
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

            // Update navigation buttons
            HasPreviousBarangayPage = CurrentBarangayPage > 1;
            HasNextBarangayPage = CurrentBarangayPage < TotalBarangayPages;
        }

        /// <summary>
        /// Populate ViewModel from MonthlyAnalyticsDto
        /// </summary>
        public void LoadFromAnalyticsDto(MonthlyAnalyticsDto dto)
        {
            if (dto == null)
                return;

            // Store the DTO for export purposes
            AnalyticsData = dto;

            // Calculate KPIs
            TotalSoloParents = dto.TotalSoloParents;
            
            // Female/Male from Sex dictionary (null-safe)
            var sex = dto.Sex ?? new System.Collections.Generic.Dictionary<string, int>();
            int female = sex.ContainsKey("Female") ? sex["Female"] : 0;
            int male = sex.ContainsKey("Male") ? sex["Male"] : 0;
            FemaleCount = female;
            MaleCount = male;

            // Calculate female/male percentages for progress bar
            if (TotalSoloParents > 0)
            {
                double femalePercent = (female * 100.0 / TotalSoloParents);
                double malePercent = (male * 100.0 / TotalSoloParents);
                FemalePercentText = $"{femalePercent:F1}% Female";
                FemalePercentWidth = femalePercent * 2.5; // Assuming max width of ~250 for progress bar
            }
            else
            {
                FemalePercentText = "0% Female";
                FemalePercentWidth = 0;
            }

            // Total dependents sum (null-safe)
            var dependentsAge = dto.DependentsAgeBrackets ?? new System.Collections.Generic.Dictionary<string, int>();
            TotalDependents = 0;
            foreach (var kvp in dependentsAge)
            {
                TotalDependents += kvp.Value;
            }

            // LGBT count - sum from Lgbt dictionary (null-safe)
            var lgbt = dto.Lgbt ?? new System.Collections.Generic.Dictionary<string, int>();
            LgbtCount = 0;
            foreach (var kvp in lgbt)
            {
                LgbtCount += kvp.Value;
            }

            // Pantawid Beneficiary - sum from PantawidBeneficiary dictionary (null-safe)
            var pantawid = dto.PantawidBeneficiary ?? new System.Collections.Generic.Dictionary<string, int>();
            PantawidBeneficiaryCount = 0;
            foreach (var kvp in pantawid)
            {
                PantawidBeneficiaryCount += kvp.Value;
            }

            // Load civil status simple counts (null-safe)
            var civilStatus = dto.CivilStatus ?? new System.Collections.Generic.Dictionary<string, int>();
            SingleCount = civilStatus.ContainsKey("Single") ? civilStatus["Single"] : 0;
            MarriedCount = civilStatus.ContainsKey("Married") ? civilStatus["Married"] : 0;
            WidowedCount = civilStatus.ContainsKey("Widowed") ? civilStatus["Widowed"] : 0;
            SeparatedCount = civilStatus.ContainsKey("Separated") ? civilStatus["Separated"] : 0;
            AnnulledCount = civilStatus.ContainsKey("Annulled") ? civilStatus["Annulled"] : 0;

            // Load employment status simple text counts (null-safe)
            var employment = dto.EmploymentStatus ?? new System.Collections.Generic.Dictionary<string, int>();
            int employed = employment.ContainsKey("employed") ? employment["employed"] : 0;
            int selfEmployed = employment.ContainsKey("self_employed") ? employment["self_employed"] : 0;
            int notEmployed = employment.ContainsKey("not_employed") ? employment["not_employed"] : 0;
            
            EmployedText = $"{employed} ({(TotalSoloParents > 0 ? (employed * 100.0 / TotalSoloParents) : 0):F1}%)";
            SelfEmployedText = $"{selfEmployed} ({(TotalSoloParents > 0 ? (selfEmployed * 100.0 / TotalSoloParents) : 0):F1}%)";
            NotEmployedText = $"{notEmployed} ({(TotalSoloParents > 0 ? (notEmployed * 100.0 / TotalSoloParents) : 0):F1}%)";

            // Calculate employment percentages for ring charts
            EmployedPercent = TotalSoloParents > 0 ? (employed * 100.0 / TotalSoloParents) : 0;
            SelfEmployedPercent = TotalSoloParents > 0 ? (selfEmployed * 100.0 / TotalSoloParents) : 0;
            NotEmployedPercent = TotalSoloParents > 0 ? (notEmployed * 100.0 / TotalSoloParents) : 0;

            // Generate arc paths for employment ring charts (64x64 canvas, radius 28, center at 32,32)
            EmployedArcPath = GenerateArcPath(EmployedPercent);
            SelfEmployedArcPath = GenerateArcPath(SelfEmployedPercent);
            NotEmployedArcPath = GenerateArcPath(NotEmployedPercent);

            // Load dependents age brackets (reuse null-safe local)
            AgeGroup0_5 = dependentsAge.ContainsKey("6_AND_BELOW") ? dependentsAge["6_AND_BELOW"] : 0;
            AgeGroup6_12 = dependentsAge.ContainsKey("7_TO_22") ? dependentsAge["7_TO_22"] : 0;
            AgeGroup13_17 = dependentsAge.ContainsKey("22_AND_ABOVE") ? dependentsAge["22_AND_ABOVE"] : 0;

            // Calculate age group percentages
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

            // Load income brackets with detailed calculations (null-safe)
            var incomeBrackets = dto.MonthlyIncomeBrackets ?? new System.Collections.Generic.Dictionary<string, int>();
            int incomeBelowMin = incomeBrackets.ContainsKey("BELOW_MINIMUM_WAGE") 
                ? incomeBrackets["BELOW_MINIMUM_WAGE"] : 0;
            int incomeMinWagePlus = incomeBrackets.ContainsKey("MIN_WAGE_PLUS1_TO_20833") 
                ? incomeBrackets["MIN_WAGE_PLUS1_TO_20833"] : 0;
            int incomeAboveMin = incomeBrackets.ContainsKey("20834_AND_ABOVE")
                ? incomeBrackets["20834_AND_ABOVE"] : 0;
            
            int totalWithIncome = incomeBelowMin + incomeMinWagePlus + incomeAboveMin;
            
            // Store individual counts
            IncomeBelowMinCount = incomeBelowMin;
            IncomeMinWageCount = incomeMinWagePlus;
            IncomeAboveMinCount = incomeAboveMin;
            
            // Calculate percentages
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
            
            // Calculate income bar widths (pixel-based, assuming 300px total width)
            double totalBarWidth = 300;
            IncomeBelowMinPercentWidth = (IncomeBelowMinPercent / 100.0) * totalBarWidth;
            IncomeMinWagePercentWidth = (IncomeMinWagePercent / 100.0) * totalBarWidth;
            IncomeAboveMinPercentWidth = (IncomeAboveMinPercent / 100.0) * totalBarWidth;

            // Calculate age group bar heights (scale to 0-160px for visual display)
            int totalDependentsByAgeForBars = AgeGroup0_5 + AgeGroup6_12 + AgeGroup13_17;
            if (totalDependentsByAgeForBars > 0)
            {
                double maxBarHeight = 160; // Maximum height in pixels
                AgeGroup0_5BarHeight = (AgeGroup0_5 * maxBarHeight) / totalDependentsByAgeForBars;
                AgeGroup6_12BarHeight = (AgeGroup6_12 * maxBarHeight) / totalDependentsByAgeForBars;
                AgeGroup13_17BarHeight = (AgeGroup13_17 * maxBarHeight) / totalDependentsByAgeForBars;
            }
            else
            {
                AgeGroup0_5BarHeight = 0;
                AgeGroup6_12BarHeight = 0;
                AgeGroup13_17BarHeight = 0;
            }

            // Load Age Group Data for Donut Chart (null-safe)
            var ageBrackets = dto.AgeBrackets ?? new System.Collections.Generic.Dictionary<string, int>();
            AgeGroupData.Clear();
            var ageColors = new[] { "#7B1426", "#A14452", "#D9A3AB", "#EAD9DB" };
            var ageLabels = new[] { "19 & Below", "20-39", "40-59", "60 & Above" };
            var ageKeys = new[] { "19_AND_BELOW", "20_39", "40_59", "60_AND_ABOVE" };
            
            for (int i = 0; i < ageKeys.Length; i++)
            {
                int count = ageBrackets.ContainsKey(ageKeys[i]) ? ageBrackets[ageKeys[i]] : 0;
                AgeGroupData.Add(new DonutSegment 
                { 
                    Label = ageLabels[i], 
                    Value = count, 
                    Color = ageColors[i] 
                });
            }

            // Load Civil Status Data (reuse null-safe local)
            CivilStatusData.Clear();
            var civilStatusOrder = new[] { "Single", "Married", "Widowed", "Separated", "Annulled" };
            foreach (var status in civilStatusOrder)
            {
                int count = civilStatus.ContainsKey(status) ? civilStatus[status] : 0;
                CivilStatusData.Add(new ListItem { Label = status, Value = count });
            }

            // Load Employment Status Data (reuse null-safe local)
            EmploymentStatusData.Clear();
            var empLabels = new[] { "Employed", "Self-Employed", "Not Employed" };
            var empKeys = new[] { "employed", "self_employed", "not_employed" };
            for (int i = 0; i < empKeys.Length; i++)
            {
                int count = employment.ContainsKey(empKeys[i]) ? employment[empKeys[i]] : 0;
                double percentage = TotalSoloParents > 0 ? (count * 100.0 / TotalSoloParents) : 0;
                EmploymentStatusData.Add(new ProgressItem 
                { 
                    Label = empLabels[i], 
                    Value = count, 
                    Percentage = percentage 
                });
            }

            // Load Income Bracket Data (reuse null-safe local)
            IncomeBracketData.Clear();
            var incomeLabels = new[] { "Below Minimum Wage", "Min Wage + ₱1-₱20,833", "₱20,834 & Above" };
            var incomeKeys = new[] { "BELOW_MINIMUM_WAGE", "MIN_WAGE_PLUS1_TO_20833", "20834_AND_ABOVE" };
            for (int i = 0; i < incomeKeys.Length; i++)
            {
                int count = incomeBrackets.ContainsKey(incomeKeys[i]) 
                    ? incomeBrackets[incomeKeys[i]] : 0;
                double percentage = TotalSoloParents > 0 ? (count * 100.0 / TotalSoloParents) : 0;
                IncomeBracketData.Add(new ProgressItem 
                { 
                    Label = incomeLabels[i], 
                    Value = count, 
                    Percentage = percentage 
                });
            }

            // Load Dependents Age Data for Column Chart (reuse null-safe local)
            DependentsAgeData.Clear();
            var depLabels = new[] { "6 & Below", "7-22", "22 & Above" };
            var depKeys = new[] { "6_AND_BELOW", "7_TO_22", "22_AND_ABOVE" };
            for (int i = 0; i < depKeys.Length; i++)
            {
                int count = dependentsAge.ContainsKey(depKeys[i]) 
                    ? dependentsAge[depKeys[i]] : 0;
                DependentsAgeData.Add(new ColumnChartData 
                { 
                    Label = depLabels[i], 
                    Value = count,
                    Color = new[] { "#7B1426", "#A14452", "#D9A3AB" }[i]
                });
            }

            // Load Barangay Data (ALL barangays - for pagination) (null-safe)
            var categories = dto.Categories ?? new System.Collections.Generic.Dictionary<string, int>();
            AllBarangayData.Clear();
            var barangayList = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>>();
            foreach (var kvp in categories)
            {
                if (string.IsNullOrEmpty(kvp.Key) || kvp.Key.Equals("null", StringComparison.OrdinalIgnoreCase) || 
                    kvp.Key.Equals("No Barangay", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                barangayList.Add(kvp);
            }

            barangayList.Sort((a, b) => b.Value.CompareTo(a.Value));
            
            // Find max value for bar width calculation
            int maxBarangayValue = barangayList.Count > 0 ? barangayList[0].Value : 1;
            
            int barangayIndex = 0;
            foreach (var kvp in barangayList)
            {
                string displayLabel = kvp.Key.Length > 20 ? kvp.Key.Substring(0, 17) + "..." : kvp.Key;
                double barWidth = (kvp.Value * 1.0 / maxBarangayValue) * 300;
                
                AllBarangayData.Add(new AreaChartData 
                { 
                    Label = displayLabel,
                    Value = kvp.Value,
                    XPosition = barangayIndex,
                    Rank = barangayIndex + 1,
                    Color = "#B03058", // Burgundy
                    BarWidth = barWidth
                });
                barangayIndex++;
            }

            // Calculate pagination
            const int barangaysPerPage = 10;
            TotalBarangayPages = (int)Math.Ceiling(AllBarangayData.Count / (double)barangaysPerPage);
            CurrentBarangayPage = 1;
            UpdateBarangayPage();

            // Load Top Cases Presented Data (from Categories/ClassificationCircumstance) (reuse null-safe local)
            TopCasesData.Clear();
            var casesList = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>>();
            foreach (var kvp in categories)
            {
                // Skip empty/null entries
                if (string.IsNullOrEmpty(kvp.Key) || kvp.Key.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                casesList.Add(kvp);
            }

            casesList.Sort((a, b) => b.Value.CompareTo(a.Value));

            // Find max value for bar width calculation
            int maxCasesValue = casesList.Count > 0 ? casesList[0].Value : 1;

            int caseIndex = 0;
            foreach (var kvp in casesList)
            {
                if (caseIndex >= 10) break;
                // Truncate long case names for better display
                string displayCaseLabel = kvp.Key.Length > 20 ? kvp.Key.Substring(0, 17) + "..." : kvp.Key;
                double casesBarWidth = (kvp.Value * 1.0 / maxCasesValue) * 300; // Max width ~300px

                TopCasesData.Add(new AreaChartData
                {
                    Label = displayCaseLabel,
                    Value = kvp.Value,
                    XPosition = caseIndex,
                    Rank = caseIndex + 1,
                    Color = "#4ECDC4", // Teal color for cases
                    BarWidth = casesBarWidth
                });
                caseIndex++;
            }

            LastRefreshTime = DateTime.Now;

            // Update empty-state flag
            HasData = TotalSoloParents > 0;
        }


        /// <summary>
        /// Generate SVG arc path for employment ring charts.
        /// Canvas: 64x64, Radius: 28, Center: (32, 32)
        /// Arc starts at top (0°) and sweeps clockwise based on percentage.
        /// Percentage of 25% = 90° arc, 50% = 180° arc, 100% = 360° (full circle).
        /// </summary>
        private string GenerateArcPath(double percentage)
        {
            if (percentage < 0.5) percentage = 0.5; // Minimum visible arc
            if (percentage > 99.5) percentage = 99.5; // Prevent full circle which breaks arc

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
            bool isLargeArc = degrees > 180;

            // Build SVG arc path
            string arcPath = $"M {startX},{startY} A {radius},{radius} 0 {(isLargeArc ? 1 : 0)},1 {endX},{endY}";
            return arcPath;
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

        /// <summary>
        /// Returns Label in case-presented format (original casing preserved)
        /// </summary>
        public string DisplayLabel
        {
            get => Label;
        }
    }
}
