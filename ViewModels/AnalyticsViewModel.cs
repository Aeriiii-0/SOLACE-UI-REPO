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

        // Top 10 Barangays Data (for Area Chart)
        private ObservableCollection<AreaChartData> _barangayData;
        public ObservableCollection<AreaChartData> BarangayData
        {
            get => _barangayData;
            set => SetProperty(ref _barangayData, value);
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

        public AnalyticsViewModel()
        {
            AgeGroupData = new ObservableCollection<DonutSegment>();
            CivilStatusData = new ObservableCollection<ListItem>();
            EmploymentStatusData = new ObservableCollection<ProgressItem>();
            IncomeBracketData = new ObservableCollection<ProgressItem>();
            DependentsAgeData = new ObservableCollection<ColumnChartData>();
            BarangayData = new ObservableCollection<AreaChartData>();
            LastRefreshTime = DateTime.Now;
            IsLoading = false;
        }

        /// <summary>
        /// Populate ViewModel from MonthlyAnalyticsDto
        /// </summary>
        public void LoadFromAnalyticsDto(MonthlyAnalyticsDto dto)
        {
            if (dto == null)
                return;

            // Calculate KPIs
            TotalSoloParents = dto.TotalSoloParents;
            
            // Female/Male from Sex dictionary
            int female = dto.Sex.ContainsKey("Female") ? dto.Sex["Female"] : 0;
            int male = dto.Sex.ContainsKey("Male") ? dto.Sex["Male"] : 0;
            FemaleCount = female;
            MaleCount = male;

            // Total dependents sum
            TotalDependents = 0;
            foreach (var kvp in dto.DependentsAgeBrackets)
            {
                TotalDependents += kvp.Value;
            }

            // LGBT count - sum from Lgbt dictionary
            LgbtCount = 0;
            foreach (var kvp in dto.Lgbt)
            {
                LgbtCount += kvp.Value;
            }

            // Pantawid Beneficiary - sum from PantawidBeneficiary dictionary
            PantawidBeneficiaryCount = 0;
            foreach (var kvp in dto.PantawidBeneficiary)
            {
                PantawidBeneficiaryCount += kvp.Value;
            }

            // Load Age Group Data for Donut Chart
            AgeGroupData.Clear();
            var ageColors = new[] { "#7B1426", "#A14452", "#D9A3AB", "#EAD9DB" };
            var ageLabels = new[] { "19 & Below", "20-39", "40-59", "60 & Above" };
            var ageKeys = new[] { "19_AND_BELOW", "20_39", "40_59", "60_AND_ABOVE" };
            
            for (int i = 0; i < ageKeys.Length; i++)
            {
                int count = dto.AgeBrackets.ContainsKey(ageKeys[i]) ? dto.AgeBrackets[ageKeys[i]] : 0;
                AgeGroupData.Add(new DonutSegment 
                { 
                    Label = ageLabels[i], 
                    Value = count, 
                    Color = ageColors[i] 
                });
            }

            // Load Civil Status Data
            CivilStatusData.Clear();
            var civilStatusOrder = new[] { "Single", "Married", "Widowed", "Separated", "Annulled" };
            foreach (var status in civilStatusOrder)
            {
                int count = dto.CivilStatus.ContainsKey(status) ? dto.CivilStatus[status] : 0;
                CivilStatusData.Add(new ListItem { Label = status, Value = count });
            }

            // Load Employment Status Data
            EmploymentStatusData.Clear();
            var empLabels = new[] { "Employed", "Self-Employed", "Not Employed" };
            var empKeys = new[] { "employed", "self_employed", "not_employed" };
            for (int i = 0; i < empKeys.Length; i++)
            {
                int count = dto.EmploymentStatus.ContainsKey(empKeys[i]) ? dto.EmploymentStatus[empKeys[i]] : 0;
                double percentage = TotalSoloParents > 0 ? (count * 100.0 / TotalSoloParents) : 0;
                EmploymentStatusData.Add(new ProgressItem 
                { 
                    Label = empLabels[i], 
                    Value = count, 
                    Percentage = percentage 
                });
            }

            // Load Income Bracket Data
            IncomeBracketData.Clear();
            var incomeLabels = new[] { "Below Minimum Wage", "Min Wage + ₱1-₱20,833", "₱20,834 & Above" };
            var incomeKeys = new[] { "BELOW_MINIMUM_WAGE", "MIN_WAGE_PLUS1_TO_20833", "20834_AND_ABOVE" };
            for (int i = 0; i < incomeKeys.Length; i++)
            {
                int count = dto.MonthlyIncomeBrackets.ContainsKey(incomeKeys[i]) 
                    ? dto.MonthlyIncomeBrackets[incomeKeys[i]] : 0;
                double percentage = TotalSoloParents > 0 ? (count * 100.0 / TotalSoloParents) : 0;
                IncomeBracketData.Add(new ProgressItem 
                { 
                    Label = incomeLabels[i], 
                    Value = count, 
                    Percentage = percentage 
                });
            }

            // Load Dependents Age Data for Column Chart
            DependentsAgeData.Clear();
            var depLabels = new[] { "6 & Below", "7-22", "22 & Above" };
            var depKeys = new[] { "6_AND_BELOW", "7_TO_22", "22_AND_ABOVE" };
            for (int i = 0; i < depKeys.Length; i++)
            {
                int count = dto.DependentsAgeBrackets.ContainsKey(depKeys[i]) 
                    ? dto.DependentsAgeBrackets[depKeys[i]] : 0;
                DependentsAgeData.Add(new ColumnChartData 
                { 
                    Label = depLabels[i], 
                    Value = count,
                    Color = new[] { "#7B1426", "#A14452", "#D9A3AB" }[i]
                });
            }

            // Load Barangay Data (top 10)
            BarangayData.Clear();
            // Note: dto.Categories typically contains barangay counts
            // Sort by value (descending) and take top 10
            var barangayList = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>>();
            foreach (var kvp in dto.Categories)
            {
                // Skip "No Barangay" entries
                if (string.IsNullOrEmpty(kvp.Key) || kvp.Key.Equals("null", StringComparison.OrdinalIgnoreCase) || 
                    kvp.Key.Equals("No Barangay", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                barangayList.Add(kvp);
            }
            
            // Always include "No Barangay" or empty entries at the end if they exist
            int noBarangayCount = 0;
            foreach (var kvp in dto.Categories)
            {
                if (string.IsNullOrEmpty(kvp.Key) || kvp.Key.Equals("null", StringComparison.OrdinalIgnoreCase) || 
                    kvp.Key.Equals("No Barangay", StringComparison.OrdinalIgnoreCase))
                {
                    noBarangayCount += kvp.Value;
                }
            }
            
            barangayList.Sort((a, b) => b.Value.CompareTo(a.Value));
            
            int index = 0;
            foreach (var kvp in barangayList)
            {
                if (index >= 10) break;
                // Truncate long barangay names for better display
                string displayLabel = kvp.Key.Length > 15 ? kvp.Key.Substring(0, 12) + "..." : kvp.Key;
                BarangayData.Add(new AreaChartData 
                { 
                    Label = displayLabel, 
                    Value = kvp.Value,
                    XPosition = index
                });
                index++;
            }
            
            // Add "No Barangay" as last entry if it has data
            if (noBarangayCount > 0 && index < 10)
            {
                BarangayData.Add(new AreaChartData 
                { 
                    Label = "No Barangay", 
                    Value = noBarangayCount,
                    XPosition = index
                });
            }

            LastRefreshTime = DateTime.Now;
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
    }
}
