using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using SOLUM_UI.Core;
using SOLUM_UI.DTOs;
using SOLUM_UI.Models;
using SOLUM_UI.Services.Implementations;
using SOLUM_UI.Services.Interfaces;

namespace SOLUM_UI.ViewModels
{
    public class SubsidyRecommendationViewModel : ObservableObject
    {
        private readonly ISubsidyApiService _apiService;
        private readonly List<SubsidyItem> _allItems = new List<SubsidyItem>();
        private int _activeTabIndex, _budgetAmount = 10000, _granteeSlots = 10, _selectedFiscalYearIndex;
        private string _auditFilter = "All", _outlierRejectionReason = "Ineligible Income / Undisclosed Assets", _outlierNotes = string.Empty, _shortlistSearchText = string.Empty;
        private SubsidyItem _activeCandidate, _outlierTarget;
        private bool _isDrawerOpen, _isOutlierModalOpen, _outlierAutoPromote = true, _isBatchMode;

        public SubsidyRecommendationViewModel(ISubsidyApiService apiService = null)
        {
            _apiService = apiService ?? new SubsidyApiService();
            FilteredRecommendations = new ObservableCollection<SubsidyItem>();
            ShortlistRows = new ObservableCollection<SelectionRow>();
            FilteredShortlistRows = new ObservableCollection<SelectionRow>();
            FinalGranteesList = new ObservableCollection<SelectionRow>();
            InitCommands();
            _ = LoadInitialDataAsync();
        }

        public ObservableCollection<SubsidyItem> FilteredRecommendations { get; }
        public ObservableCollection<SelectionRow> ShortlistRows { get; }
        public ObservableCollection<SelectionRow> FilteredShortlistRows { get; }
        public ObservableCollection<SelectionRow> FinalGranteesList { get; }

        public string ShortlistSearchText { get => _shortlistSearchText; set { if (SetProperty(ref _shortlistSearchText, value)) FilterShortlistRows(); } }

        public int ActiveTabIndex { get => _activeTabIndex; set { if (SetProperty(ref _activeTabIndex, value)) { OnPropertyChanged(nameof(TabRecommendationsVisible)); OnPropertyChanged(nameof(TabShortlistVisible)); OnPropertyChanged(nameof(TabFinalVisible)); } } }
        public bool TabRecommendationsVisible => ActiveTabIndex == 0;
        public bool TabShortlistVisible => ActiveTabIndex == 1;
        public bool TabFinalVisible => ActiveTabIndex == 2;

        public bool IsBatchMode { get => _isBatchMode; set { if (SetProperty(ref _isBatchMode, value)) { OnPropertyChanged(nameof(BatchSelectColumnWidth)); OnPropertyChanged(nameof(BatchModeButtonText)); } } }
        public double BatchSelectColumnWidth => IsBatchMode ? 38 : 0; public string BatchModeButtonText => IsBatchMode ? "✓ Batch Mode: On" : "☑ Batch Select";
        public int BudgetAmount { get => _budgetAmount; set { if (SetProperty(ref _budgetAmount, value)) { _granteeSlots = Math.Max(0, value / 1000); RecalculateQuotaAndFilter(); } } }
        public string GranteeSlotsText => $"{_granteeSlots} target slots"; public string SelectedCountSummary => $"{FilteredRecommendations.Count(x => x.IsSelected)} selected";
        public string SelectTopNLabel => (FilteredRecommendations.Count > 0 && FilteredRecommendations.Take(Math.Min(_granteeSlots, FilteredRecommendations.Count)).All(x => x.IsSelected)) ? $"✕ Deselect Top {_granteeSlots}" : $"✓ Select Top {_granteeSlots}";
        public string AuditFilterLabel => _auditFilter == "All" ? "Cross-Check: All" : $"Filter: {_auditFilter}"; public string Tab2QuotaSummary => $"Slots: {ShortlistRows.Count(x => !x.IsWaitlisted)} of {_granteeSlots} filled";
        public string ShortlistSummary => $"{ShortlistRows.Count(x => !x.IsWaitlisted)} of {_granteeSlots} slots allocated • ₱{ShortlistRows.Count(x => !x.IsWaitlisted) * 1000:N0} committed";
        public string FinalSummaryText => $"{FinalGranteesList.Count(x => x.IsFinalGrantee)} of {FinalGranteesList.Count} confirmed as grantees";

        public SubsidyItem ActiveCandidate { get => _activeCandidate; set => SetProperty(ref _activeCandidate, value); }
        public bool IsDrawerOpen { get => _isDrawerOpen; set => SetProperty(ref _isDrawerOpen, value); }
        public bool IsOutlierModalOpen { get => _isOutlierModalOpen; set => SetProperty(ref _isOutlierModalOpen, value); }
        public SubsidyItem OutlierTarget { get => _outlierTarget; set { SetProperty(ref _outlierTarget, value); OnPropertyChanged(nameof(OutlierTargetNameAndId)); } }
        public string OutlierTargetNameAndId => $"Candidate: {_outlierTarget?.Name} ({_outlierTarget?.SpId})";
        public string OutlierRejectionReason { get => _outlierRejectionReason; set => SetProperty(ref _outlierRejectionReason, value); }
        public string OutlierNotes { get => _outlierNotes; set => SetProperty(ref _outlierNotes, value); }
        public bool OutlierAutoPromote { get => _outlierAutoPromote; set => SetProperty(ref _outlierAutoPromote, value); }
        public int SelectedFiscalYearIndex { get => _selectedFiscalYearIndex; set { if (SetProperty(ref _selectedFiscalYearIndex, value)) _ = OnFiscalYearChangedAsync(value); } }

        public ICommand SwitchTabCommand { get; private set; } public ICommand ToggleBatchModeCommand { get; private set; } public ICommand SelectTopNCommand { get; private set; } public ICommand ShortlistSelectedCommand { get; private set; }
        public ICommand QuickAcceptRowCommand { get; private set; } public ICommand QuickRejectRowCommand { get; private set; } public ICommand OpenExplainDrawerCommand { get; private set; } public ICommand CloseExplainDrawerCommand { get; private set; }
        public ICommand DrawerShortlistCommand { get; private set; } public ICommand OpenOutlierModalCommand { get; private set; } public ICommand CloseOutlierModalCommand { get; private set; } public ICommand ConfirmOutlierRejectionCommand { get; private set; }
        public ICommand ConfirmGranteeCommand { get; private set; } public ICommand RevokeGranteeCommand { get; private set; } public ICommand ConfirmShortlistCandidateCommand { get; private set; } public ICommand RejectShortlistCandidateCommand { get; private set; }
        public ICommand ExportCityEducCommand { get; private set; } public ICommand ExportFinalLedgerCommand { get; private set; } public ICommand ViewFullRecordCommand { get; private set; }

        private void InitCommands()
        {
            SwitchTabCommand = new RelayCommand(p => { if (int.TryParse(p?.ToString(), out int tab)) ActiveTabIndex = tab; });
            ToggleBatchModeCommand = new RelayCommand(_ => { IsBatchMode = !IsBatchMode; if (!IsBatchMode) { foreach (var item in FilteredRecommendations) item.IsSelected = false; RecalculateSelectionCounts(); } });
            SelectTopNCommand = new RelayCommand(_ => SelectTopN());
            ShortlistSelectedCommand = new RelayCommand(_ => MoveSelectedToShortlist());
            QuickAcceptRowCommand = new RelayCommand(p => { if (p is SubsidyItem item) ShortlistCandidate(item); });
            OpenExplainDrawerCommand = new RelayCommand(p =>
            {
                if (p is SubsidyItem item) { ActiveCandidate = item; IsDrawerOpen = true; }
                else if (p is SelectionRow row)
                {
                    ActiveCandidate = _allItems.FirstOrDefault(x => x.SpId == row.SpId) ?? new SubsidyItem { SpId = row.SpId, Name = row.Name, Barangay = row.Barangay, Priority = row.Priority, Score = row.Score, MonthlyIncome = row.MonthlyIncome, IncomePerCapita = row.IncomePerCapita, MinorDependentsCount = row.MinorDependentsCount, ToddlersUnder5Count = row.ToddlersUnder5Count, Circumstance = row.Circumstance, CollegeAgeDependentsCount = row.CollegeAgeDependentsCount, IsPantawidBeneficiary = row.IsPantawidBeneficiary };
                    IsDrawerOpen = true;
                }
            });
            CloseExplainDrawerCommand = new RelayCommand(_ => IsDrawerOpen = false);
            DrawerShortlistCommand = new RelayCommand(_ => { if (ActiveCandidate != null) { ShortlistCandidate(ActiveCandidate); IsDrawerOpen = false; } });
            OpenOutlierModalCommand = new RelayCommand(p => { OutlierTarget = p as SubsidyItem ?? ActiveCandidate; if (OutlierTarget != null) IsOutlierModalOpen = true; });
            CloseOutlierModalCommand = new RelayCommand(_ => IsOutlierModalOpen = false);
            ConfirmOutlierRejectionCommand = new RelayCommand(_ => _ = ConfirmOutlierRejectionAsync());
            ConfirmGranteeCommand = new RelayCommand(p => ToggleGrantee(p as SelectionRow, true));
            RevokeGranteeCommand = new RelayCommand(p => ToggleGrantee(p as SelectionRow, false));
            ConfirmShortlistCandidateCommand = new RelayCommand(p => { if (p is SelectionRow r) ToggleGrantee(r, true); });
            RejectShortlistCandidateCommand = new RelayCommand(p => { if (p is SelectionRow r) RejectShortlistRow(r); });
            ExportCityEducCommand = new RelayCommand(_ => _ = ExportCityEducAsync());
            ExportFinalLedgerCommand = new RelayCommand(_ => _ = ExportFinalLedgerAsync());
            ViewFullRecordCommand = new RelayCommand(p => { if (p is SubsidyItem i) ToastNotification.Show("Full Profile", $"{i.Name}: {i.Circumstance}", ToastType.Info); });
        }

        private async Task LoadInitialDataAsync()
        {
            var dtos = await _apiService.GetRecommendationsAsync();
            _allItems.Clear();
            foreach (var d in dtos)
            {
                _allItems.Add(new SubsidyItem
                {
                    SpId = d.SpId, Name = d.Name, Barangay = d.Barangay, SubsidyType = d.SubsidyType, Priority = d.Priority, Score = d.Score, Confidence = d.Confidence,
                    MonthlyIncome = d.MonthlyIncome, IncomePerCapita = d.IncomePerCapita, EmploymentStatus = d.EmploymentStatus, Dependants = d.Dependants,
                    MinorDependentsCount = d.MinorDependentsCount, ToddlersUnder5Count = d.ToddlersUnder5Count, CollegeAgeDependentsCount = d.CollegeAgeDependentsCount,
                    Circumstance = d.Circumstance, CivilStatus = d.CivilStatus, OtherIncomeSource = d.OtherIncomeSource, NeedsAndProblems = d.NeedsAndProblems,
                    EconomicStrainScore = d.EconomicStrainScore, DependencyBurdenScore = d.DependencyBurdenScore, CareBurdenScore = d.CareBurdenScore,
                    ResourceAdequacyScore = d.ResourceAdequacyScore, IsPantawidBeneficiary = d.IsPantawidBeneficiary,
                    ChildrenDetails = d.Children?.Select(c => new ChildDetailItem { Name = c.Name, Age = c.Age }).ToList() ?? new List<ChildDetailItem>()
                });
            }
            RecalculateQuotaAndFilter();
            foreach (var item in _allItems.Take(8)) ShortlistCandidate(item, notify: false);
        }

        public void ApplyAuditFilter(string filter) { _auditFilter = filter; OnPropertyChanged(nameof(AuditFilterLabel)); RecalculateQuotaAndFilter(); }
        public void RecalculateSelectionCounts() { OnPropertyChanged(nameof(SelectedCountSummary)); OnPropertyChanged(nameof(SelectTopNLabel)); }

        private void RecalculateQuotaAndFilter()
        {
            IEnumerable<SubsidyItem> q = _allItems.Where(x => !x.IsDisqualified);
            if (_auditFilter == "Clean") q = q.Where(x => !x.HasCollegeAgeDependent && !x.IsPantawidBeneficiary);
            else if (_auditFilter == "CollegeAge") q = q.Where(x => x.HasCollegeAgeDependent);
            else if (_auditFilter == "Pantawid") q = q.Where(x => x.IsPantawidBeneficiary);

            var list = q.OrderByDescending(x => x.Score).ToList();
            FilteredRecommendations.Clear();
            for (int i = 0; i < list.Count; i++) { list[i].Rank = i + 1; list[i].IsWithinQuota = (i < _granteeSlots); FilteredRecommendations.Add(list[i]); }
            OnPropertyChanged(nameof(GranteeSlotsText)); OnPropertyChanged(nameof(SelectTopNLabel)); OnPropertyChanged(nameof(SelectedCountSummary));
        }

        private void SelectTopN()
        {
            int limit = Math.Min(_granteeSlots, FilteredRecommendations.Count);
            if (limit <= 0) return;
            bool allSelected = FilteredRecommendations.Take(limit).All(x => x.IsSelected);
            for (int i = 0; i < limit; i++) FilteredRecommendations[i].IsSelected = !allSelected;
            RecalculateSelectionCounts();
            ToastNotification.Show(allSelected ? "Selection Cleared" : "Selection Updated", allSelected ? $"Deselected top {limit} candidates." : $"Selected top {limit} candidates for review.", ToastType.Info);
        }

        private void MoveSelectedToShortlist()
        {
            var selected = FilteredRecommendations.Where(x => x.IsSelected).ToList();
            if (selected.Count == 0) { ToastNotification.Show("No Selection", "Please select at least one pending candidate.", ToastType.Warning); return; }
            foreach (var item in selected) ShortlistCandidate(item, notify: false);
            UpdateShortlistAndFinalCollections();
            ToastNotification.Show("Shortlisted", $"{selected.Count} candidate(s) moved to Audit & Verification.", ToastType.Success);
        }

        private void ShortlistCandidate(SubsidyItem item, bool notify = true)
        {
            item.Status = "Approved";
            if (!ShortlistRows.Any(x => x.SpId == item.SpId))
            {
                ShortlistRows.Add(new SelectionRow { Rank = ShortlistRows.Count + 1, SpId = item.SpId, Name = item.Name, Barangay = item.Barangay, Priority = item.Priority, Score = item.Score, Dependants = item.Dependants, MonthlyIncome = item.MonthlyIncome, IncomePerCapita = item.IncomePerCapita, MinorDependentsCount = item.MinorDependentsCount, ToddlersUnder5Count = item.ToddlersUnder5Count, Circumstance = item.Circumstance, HasCollegeAgeDependent = item.HasCollegeAgeDependent, CollegeAgeDependentsCount = item.CollegeAgeDependentsCount, IsPantawidBeneficiary = item.IsPantawidBeneficiary, IsWaitlisted = ShortlistRows.Count >= _granteeSlots });
                FinalGranteesList.Add(ShortlistRows.Last());
            }
            UpdateShortlistAndFinalCollections();
            if (notify) ToastNotification.Show("Candidate Shortlisted", $"{item.Name} moved to Audit & Verification.", ToastType.Success);
        }

        private void UpdateShortlistAndFinalCollections()
        {
            for (int i = 0; i < ShortlistRows.Count; i++) { ShortlistRows[i].Rank = i + 1; ShortlistRows[i].IsWaitlisted = (i >= _granteeSlots); }
            FilterShortlistRows();
            OnPropertyChanged(nameof(Tab2QuotaSummary)); OnPropertyChanged(nameof(ShortlistSummary)); OnPropertyChanged(nameof(FinalSummaryText));
        }

        private void FilterShortlistRows()
        {
            var q = ShortlistRows.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(_shortlistSearchText))
            {
                string t = _shortlistSearchText.Trim().ToLowerInvariant();
                q = q.Where(x => (x.Name != null && x.Name.ToLowerInvariant().Contains(t)) || (x.SpId != null && x.SpId.ToLowerInvariant().Contains(t)) || (x.Barangay != null && x.Barangay.ToLowerInvariant().Contains(t)) || (x.CrossCheckText != null && x.CrossCheckText.ToLowerInvariant().Contains(t)) || (x.ComplianceRemark != null && x.ComplianceRemark.ToLowerInvariant().Contains(t)));
            }
            FilteredShortlistRows.Clear(); foreach (var r in q) FilteredShortlistRows.Add(r);
        }

        private void RejectShortlistRow(SelectionRow r)
        {
            ShortlistRows.Remove(r); FinalGranteesList.Remove(r);
            var orig = _allItems.FirstOrDefault(x => x.SpId == r.SpId);
            if (orig != null) orig.Status = "Pending";
            UpdateShortlistAndFinalCollections(); RecalculateQuotaAndFilter();
            ToastNotification.Show("Returned to Queue", $"{r.Name} removed from shortlist and returned to recommendations queue.", ToastType.Warning);
        }

        private async Task ConfirmOutlierRejectionAsync()
        {
            if (OutlierTarget == null) return;
            var target = OutlierTarget; target.IsDisqualified = true; target.DisqualificationReason = OutlierRejectionReason; target.Status = "Rejected";
            await _apiService.RejectOutlierAsync(new OutlierRejectionDto { SpId = target.SpId, Reason = OutlierRejectionReason, Notes = OutlierNotes, AutoPromoteWaitlist = OutlierAutoPromote });
            IsOutlierModalOpen = false;
            RecalculateQuotaAndFilter();
            ToastNotification.Show("Outlier Disqualified", $"{target.Name} removed from queue. Next candidate promoted.", ToastType.Warning);
        }

        private void ToggleGrantee(SelectionRow row, bool confirm)
        {
            if (row == null) return; row.IsFinalGrantee = confirm; OnPropertyChanged(nameof(FinalSummaryText));
            ToastNotification.Show(confirm ? "Confirmed" : "Revoked", $"{row.Name} {(confirm ? "marked as final grantee." : "removed from final grantees.")}", confirm ? ToastType.Success : ToastType.Warning);
        }

        private async Task OnFiscalYearChangedAsync(int index)
        {
            FinalGranteesList.Clear();
            if (index == 0) foreach (var r in ShortlistRows) FinalGranteesList.Add(r);
            else { var hist = await _apiService.GetFinalGranteesByYearAsync(index == 1 ? 2025 : 2024); foreach (var r in hist) FinalGranteesList.Add(r); }
            OnPropertyChanged(nameof(FinalSummaryText));
        }

        private async Task ExportCityEducAsync()
        {
            var college = ShortlistRows.Where(i => i.HasCollegeAgeDependent).ToList();
            if (college.Count == 0) { ToastNotification.Show("No Flagged Records", "No applicants have college-age dependents.", ToastType.Info); return; }
            string csv = await _apiService.ExportCityEducRosterCsvAsync(ShortlistRows.ToList());
            SaveCsv($"CityEduc_CrossCheck_{DateTime.Now:yyyy}", csv, $"{college.Count} candidate(s) exported for City Educ cross-check.");
        }

        private async Task ExportFinalLedgerAsync()
        {
            if (FinalGranteesList.Count == 0) { ToastNotification.Show("Ledger Empty", "No confirmed grantees to export.", ToastType.Warning); return; }
            int yr = SelectedFiscalYearIndex == 1 ? 2025 : SelectedFiscalYearIndex == 2 ? 2024 : 2026;
            string csv = await _apiService.ExportFinalLedgerCsvAsync(FinalGranteesList.ToList(), yr);
            SaveCsv($"Final_Grantee_Ledger_FY{yr}", csv, $"Final grantee ledger for FY {yr} exported successfully.");
        }

        private void SaveCsv(string defName, string content, string msg)
        {
            var dlg = new SaveFileDialog { FileName = defName, DefaultExt = ".csv", Filter = "CSV file (*.csv)|*.csv" };
            if (dlg.ShowDialog() == true) { File.WriteAllText(dlg.FileName, content); ToastNotification.Show("Export Complete", msg, ToastType.Success); }
        }
    }
}
