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
        private List<SelectionRow> _shortlistRevokeTargets;
        private int _activeTabIndex, _budgetAmount = 10000, _granteeSlots = 10, _selectedFiscalYearIndex;
        private string _auditFilter = "All", _shortlistAuditFilter = "All", _outlierModalTitle = "Disqualify Candidate", _outlierRejectionReason = "Ineligible Income / Undisclosed Assets", _outlierNotes = string.Empty, _shortlistSearchText = string.Empty, _outlierTargetNameAndId;
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
        public bool TabRecommendationsVisible => ActiveTabIndex == 0; public bool TabShortlistVisible => ActiveTabIndex == 1; public bool TabFinalVisible => ActiveTabIndex == 2;
        public bool IsBatchMode { get => _isBatchMode; set { if (SetProperty(ref _isBatchMode, value)) { OnPropertyChanged(nameof(BatchSelectColumnWidth)); OnPropertyChanged(nameof(BatchModeButtonText)); } } }
        public double BatchSelectColumnWidth => IsBatchMode ? 38 : 0; public string BatchModeButtonText => IsBatchMode ? "✓ Batch Mode: On" : "☑ Batch Select";
        public int BudgetAmount { get => _budgetAmount; set { if (SetProperty(ref _budgetAmount, value)) { _granteeSlots = Math.Max(0, value / 1000); RecalculateQuotaAndFilter(); } } }
        public string GranteeSlotsText => $"{_granteeSlots} target slots"; public string SelectedCountSummary => $"{FilteredRecommendations.Count(x => x.IsSelected)} selected";
        public string SelectTopNLabel => (FilteredRecommendations.Count > 0 && FilteredRecommendations.Take(Math.Min(_granteeSlots, FilteredRecommendations.Count)).All(x => x.IsSelected)) ? $"✕ Deselect Top {_granteeSlots}" : $"✓ Select Top {_granteeSlots}";
        public string AuditFilterLabel => _auditFilter == "All" ? "Filter: All" : (_auditFilter == "CollegeAge" ? "Filter: City Educ" : "Filter: Cleared");
        public string ShortlistAuditFilterLabel => _shortlistAuditFilter == "All" ? "Filter: All" : (_shortlistAuditFilter == "CollegeAge" ? "Filter: City Educ" : "Filter: Cleared");
        public string Tab2QuotaSummary => $"Slots: {ShortlistRows.Count(x => !x.IsWaitlisted)} of {_granteeSlots} filled";
        public string ShortlistSummary => $"{ShortlistRows.Count(x => !x.IsWaitlisted)} of {_granteeSlots} slots allocated • ₱{ShortlistRows.Count(x => !x.IsWaitlisted) * 1000:N0} committed";
        public string FinalSummaryText => $"{FinalGranteesList.Count} official grantees confirmed";

        public bool IsAllShortlistSelected { get => FilteredShortlistRows.Count > 0 && FilteredShortlistRows.All(x => x.IsSelected); set { foreach (var r in FilteredShortlistRows) r.IsSelected = value; OnPropertyChanged(); RecalculateShortlistSelectionCounts(); } }
        public int ShortlistSelectedCount => FilteredShortlistRows.Count(x => x.IsSelected);
        public bool HasShortlistSelection => ShortlistSelectedCount > 0;
        public string ShortlistBatchActionLabel => $"{ShortlistSelectedCount} selected";
        public string SelectAllShortlistButtonText => (FilteredShortlistRows.Count > 0 && FilteredShortlistRows.All(x => x.IsSelected)) ? "✕ Clear All" : "☑ Select All";
        public void RecalculateShortlistSelectionCounts() { OnPropertyChanged(nameof(IsAllShortlistSelected)); OnPropertyChanged(nameof(ShortlistSelectedCount)); OnPropertyChanged(nameof(HasShortlistSelection)); OnPropertyChanged(nameof(ShortlistBatchActionLabel)); OnPropertyChanged(nameof(SelectAllShortlistButtonText)); }

        public SubsidyItem ActiveCandidate { get => _activeCandidate; set => SetProperty(ref _activeCandidate, value); }
        public bool IsDrawerOpen { get => _isDrawerOpen; set => SetProperty(ref _isDrawerOpen, value); } public bool IsOutlierModalOpen { get => _isOutlierModalOpen; set => SetProperty(ref _isOutlierModalOpen, value); }
        public string OutlierModalTitle { get => _outlierModalTitle; set => SetProperty(ref _outlierModalTitle, value); } public string OutlierModalConfirmButtonText => (_shortlistRevokeTargets != null && _shortlistRevokeTargets.Count > 0) ? "Confirm Revocation" : "Confirm Rejection";
        public SubsidyItem OutlierTarget { get => _outlierTarget; set { SetProperty(ref _outlierTarget, value); OnPropertyChanged(nameof(OutlierTargetNameAndId)); } }
        public string OutlierTargetNameAndId { get => _outlierTargetNameAndId ?? $"Candidate: {_outlierTarget?.Name} ({_outlierTarget?.SpId})"; set { _outlierTargetNameAndId = value; OnPropertyChanged(); } }
        public string OutlierRejectionReason { get => _outlierRejectionReason; set => SetProperty(ref _outlierRejectionReason, value); } public string OutlierNotes { get => _outlierNotes; set => SetProperty(ref _outlierNotes, value); }
        public bool OutlierAutoPromote { get => _outlierAutoPromote; set => SetProperty(ref _outlierAutoPromote, value); } public int SelectedFiscalYearIndex { get => _selectedFiscalYearIndex; set { if (SetProperty(ref _selectedFiscalYearIndex, value)) _ = OnFiscalYearChangedAsync(value); } }

        public ICommand SwitchTabCommand { get; private set; } public ICommand ToggleBatchModeCommand { get; private set; } public ICommand SelectTopNCommand { get; private set; } public ICommand ShortlistSelectedCommand { get; private set; }
        public ICommand QuickAcceptRowCommand { get; private set; } public ICommand QuickRejectRowCommand { get; private set; } public ICommand OpenExplainDrawerCommand { get; private set; } public ICommand CloseExplainDrawerCommand { get; private set; }
        public ICommand DrawerShortlistCommand { get; private set; } public ICommand OpenOutlierModalCommand { get; private set; } public ICommand CloseOutlierModalCommand { get; private set; } public ICommand ConfirmOutlierRejectionCommand { get; private set; }
        public ICommand ConfirmGranteeCommand { get; private set; } public ICommand RevokeGranteeCommand { get; private set; } public ICommand ConfirmShortlistCandidateCommand { get; private set; } public ICommand RevokeShortlistCandidateCommand { get; private set; } public ICommand BatchConfirmShortlistCommand { get; private set; } public ICommand BatchRevokeShortlistCommand { get; private set; } public ICommand SelectAllShortlistCommand { get; private set; }
        public ICommand ExportCityEducCommand { get; private set; } public ICommand ExportPantawid4PsCommand { get; private set; } public ICommand ExportFinalLedgerCommand { get; private set; } public ICommand ViewFullRecordCommand { get; private set; }

        private void InitCommands()
        {
            SwitchTabCommand = new RelayCommand(p => { if (int.TryParse(p?.ToString(), out int tab)) ActiveTabIndex = tab; });
            ToggleBatchModeCommand = new RelayCommand(_ => { IsBatchMode = !IsBatchMode; if (!IsBatchMode) { foreach (var item in FilteredRecommendations) item.IsSelected = false; RecalculateSelectionCounts(); } });
            SelectTopNCommand = new RelayCommand(_ => SelectTopN());
            ShortlistSelectedCommand = new RelayCommand(_ => MoveSelectedToShortlist());
            QuickAcceptRowCommand = new RelayCommand(p => { if (p is SubsidyItem item) ShortlistCandidate(item); });
            QuickRejectRowCommand = new RelayCommand(p => { OutlierTarget = p as SubsidyItem; if (OutlierTarget != null) { _shortlistRevokeTargets = null; OutlierModalTitle = "Disqualify Candidate"; OutlierTargetNameAndId = null; IsOutlierModalOpen = true; } });
            OpenExplainDrawerCommand = new RelayCommand(p => {
                if (p is SubsidyItem item) { ActiveCandidate = item; IsDrawerOpen = true; }
                else if (p is SelectionRow r) { ActiveCandidate = _allItems.FirstOrDefault(x => x.SpId == r.SpId) ?? new SubsidyItem { SpId = r.SpId, Name = r.Name, Barangay = r.Barangay, Priority = r.Priority, Score = r.Score, MonthlyIncome = r.MonthlyIncome, IncomePerCapita = r.IncomePerCapita, MinorDependentsCount = r.MinorDependentsCount, ToddlersUnder5Count = r.ToddlersUnder5Count, Circumstance = r.Circumstance, CollegeAgeDependentsCount = r.CollegeAgeDependentsCount, IsPantawidBeneficiary = r.IsPantawidBeneficiary }; IsDrawerOpen = true; }
            });
            CloseExplainDrawerCommand = new RelayCommand(_ => IsDrawerOpen = false);
            DrawerShortlistCommand = new RelayCommand(_ => { if (ActiveCandidate != null) { ShortlistCandidate(ActiveCandidate); IsDrawerOpen = false; } });
            OpenOutlierModalCommand = new RelayCommand(p => { OutlierTarget = p as SubsidyItem ?? ActiveCandidate; if (OutlierTarget != null) { _shortlistRevokeTargets = null; OutlierModalTitle = "Disqualify Candidate"; OutlierTargetNameAndId = null; IsOutlierModalOpen = true; } });
            CloseOutlierModalCommand = new RelayCommand(_ => { IsOutlierModalOpen = false; _shortlistRevokeTargets = null; });
            ConfirmOutlierRejectionCommand = new RelayCommand(_ => _ = ConfirmOutlierRejectionAsync());
            ConfirmGranteeCommand = new RelayCommand(p => ToggleGrantee(p as SelectionRow, true));
            RevokeGranteeCommand = new RelayCommand(p => ToggleGrantee(p as SelectionRow, false));
            ConfirmShortlistCandidateCommand = new RelayCommand(p => { if (p is SelectionRow r) ToggleGrantee(r, true); });
            RevokeShortlistCandidateCommand = new RelayCommand(p => PromptRevokeShortlist(p as SelectionRow));
            BatchConfirmShortlistCommand = new RelayCommand(_ => BatchConfirmShortlist());
            BatchRevokeShortlistCommand = new RelayCommand(_ => BatchRevokeShortlist());
            SelectAllShortlistCommand = new RelayCommand(_ => { bool all = FilteredShortlistRows.Count > 0 && FilteredShortlistRows.All(x => x.IsSelected); foreach (var r in FilteredShortlistRows) r.IsSelected = !all; RecalculateShortlistSelectionCounts(); });
            ExportCityEducCommand = new RelayCommand(async _ => {
                var c = ShortlistRows.Where(i => i.HasCollegeAgeDependent).ToList();
                if (c.Count == 0) { ToastNotification.Show("No Flagged Records", "No applicants have college-age dependents.", ToastType.Info); return; }
                await ExportCsvAsync($"CityEduc_College_Dependents_{DateTime.Now:yyyy}", _apiService.ExportCityEducRosterCsvAsync(ShortlistRows.ToList()), $"{c.Count} candidate(s) exported for City Educ cross-check.");
            });
            ExportPantawid4PsCommand = new RelayCommand(async _ => {
                var list = ShortlistRows.Where(i => i.IsPantawidBeneficiary).ToList();
                if (list.Count == 0) list = _allItems.Where(i => i.IsPantawidBeneficiary).Select(it => new SelectionRow { Rank = it.Rank, SpId = it.SpId, Name = it.Name, Barangay = it.Barangay, MonthlyIncome = it.MonthlyIncome, IncomePerCapita = it.IncomePerCapita, Dependants = it.Dependants, MinorDependentsCount = it.MinorDependentsCount, Circumstance = it.Circumstance, IsPantawidBeneficiary = true }).ToList();
                if (list.Count == 0) { ToastNotification.Show("No Records", "No applicants are tagged as 4Ps beneficiaries.", ToastType.Info); return; }
                await ExportCsvAsync($"Pantawid_4Ps_Beneficiaries_{DateTime.Now:yyyyMMdd}", _apiService.ExportPantawid4PsRosterCsvAsync(list), $"{list.Count} 4Ps candidate(s) exported successfully.");
            });
            ExportFinalLedgerCommand = new RelayCommand(async _ => {
                if (FinalGranteesList.Count == 0) { ToastNotification.Show("Ledger Empty", "No confirmed grantees to export.", ToastType.Warning); return; }
                int yr = SelectedFiscalYearIndex == 1 ? 2025 : SelectedFiscalYearIndex == 2 ? 2024 : 2026;
                await ExportCsvAsync($"Final_Grantee_Ledger_FY{yr}", _apiService.ExportFinalLedgerCsvAsync(FinalGranteesList.ToList(), yr), $"Final grantee ledger for FY {yr} exported successfully.");
            });
            ViewFullRecordCommand = new RelayCommand(p => { if (p is SubsidyItem i) ToastNotification.Show("Full Profile", $"{i.Name}: {i.Circumstance}", ToastType.Info); });
        }

        private async Task LoadInitialDataAsync()
        {
            var dtos = await _apiService.GetRecommendationsAsync(); _allItems.Clear();
            foreach (var d in dtos) _allItems.Add(new SubsidyItem { SpId = d.SpId, Name = d.Name, Barangay = d.Barangay, SubsidyType = d.SubsidyType, Priority = d.Priority, Score = d.Score, Confidence = d.Confidence, MonthlyIncome = d.MonthlyIncome, IncomePerCapita = d.IncomePerCapita, EmploymentStatus = d.EmploymentStatus, Dependants = d.Dependants, MinorDependentsCount = d.MinorDependentsCount, ToddlersUnder5Count = d.ToddlersUnder5Count, CollegeAgeDependentsCount = d.CollegeAgeDependentsCount, Circumstance = d.Circumstance, CivilStatus = d.CivilStatus, Sex = d.Sex, OtherIncomeSource = d.OtherIncomeSource, NeedsAndProblems = d.NeedsAndProblems, EconomicStrainScore = d.EconomicStrainScore, DependencyBurdenScore = d.DependencyBurdenScore, CareBurdenScore = d.CareBurdenScore, ResourceAdequacyScore = d.ResourceAdequacyScore, IsPantawidBeneficiary = d.IsPantawidBeneficiary, ChildrenDetails = d.Children?.Select(c => new ChildDetailItem { Name = c.Name, Age = c.Age }).ToList() ?? new List<ChildDetailItem>() });
            RecalculateQuotaAndFilter(); int idx = 0; foreach (var item in _allItems.Take(8)) ShortlistCandidate(item, notify: false, isConfirmed: idx++ < 5);
        }

        public void ApplyAuditFilter(string filter) { _auditFilter = filter; OnPropertyChanged(nameof(AuditFilterLabel)); RecalculateQuotaAndFilter(); }
        public void ApplyShortlistAuditFilter(string filter) { _shortlistAuditFilter = filter; OnPropertyChanged(nameof(ShortlistAuditFilterLabel)); FilterShortlistRows(); }
        public void RecalculateSelectionCounts() { OnPropertyChanged(nameof(SelectedCountSummary)); OnPropertyChanged(nameof(SelectTopNLabel)); }

        private void RecalculateQuotaAndFilter()
        {
            IEnumerable<SubsidyItem> q = _allItems.Where(x => !x.IsDisqualified);
            if (_auditFilter == "Clean") q = q.Where(x => !x.HasCollegeAgeDependent);
            else if (_auditFilter == "CollegeAge") q = q.Where(x => x.HasCollegeAgeDependent);
            var list = q.OrderByDescending(x => x.Score).ToList(); FilteredRecommendations.Clear();
            for (int i = 0; i < list.Count; i++) { list[i].Rank = i + 1; list[i].IsWithinQuota = (i < _granteeSlots); FilteredRecommendations.Add(list[i]); }
            OnPropertyChanged(nameof(GranteeSlotsText)); OnPropertyChanged(nameof(SelectTopNLabel)); OnPropertyChanged(nameof(SelectedCountSummary));
        }

        private void SelectTopN()
        {
            int limit = Math.Min(_granteeSlots, FilteredRecommendations.Count); if (limit <= 0) return;
            bool all = FilteredRecommendations.Take(limit).All(x => x.IsSelected);
            for (int i = 0; i < limit; i++) FilteredRecommendations[i].IsSelected = !all;
            RecalculateSelectionCounts();
            ToastNotification.Show(all ? "Selection Cleared" : "Selection Updated", all ? $"Deselected top {limit} candidates." : $"Selected top {limit} candidates for review.", ToastType.Info);
        }

        private void MoveSelectedToShortlist()
        {
            var selected = FilteredRecommendations.Where(x => x.IsSelected).ToList();
            if (selected.Count == 0) { ToastNotification.Show("No Selection", "Please select at least one pending candidate.", ToastType.Warning); return; }
            foreach (var item in selected) ShortlistCandidate(item, notify: false);
            UpdateShortlistAndFinalCollections(); ToastNotification.Show("Shortlisted", $"{selected.Count} candidate(s) moved to Audit & Verification.", ToastType.Success);
        }

        private void ShortlistCandidate(SubsidyItem item, bool notify = true, bool isConfirmed = false)
        {
            item.Status = "Approved";
            if (!ShortlistRows.Any(x => x.SpId == item.SpId))
            {
                var collegeKids = item.ChildrenDetails?.Where(c => c.Age >= 17 && c.Age <= 24).ToList() ?? new List<ChildDetailItem>();
                ShortlistRows.Add(new SelectionRow { Rank = ShortlistRows.Count + 1, SpId = item.SpId, Name = item.Name, Barangay = item.Barangay, Sex = item.Sex, CivilStatus = item.CivilStatus, Priority = item.Priority, Score = item.Score, Dependants = item.Dependants, MonthlyIncome = item.MonthlyIncome, IncomePerCapita = item.IncomePerCapita, MinorDependentsCount = item.MinorDependentsCount, ToddlersUnder5Count = item.ToddlersUnder5Count, Circumstance = item.Circumstance, HasCollegeAgeDependent = item.HasCollegeAgeDependent, CollegeAgeDependentsCount = item.CollegeAgeDependentsCount, CollegeAgeDependents = collegeKids, IsPantawidBeneficiary = item.IsPantawidBeneficiary, IsFinalGrantee = isConfirmed, IsWaitlisted = !isConfirmed });
            }
            UpdateShortlistAndFinalCollections(); if (notify) ToastNotification.Show("Candidate Shortlisted", $"{item.Name} moved to Audit & Verification.", ToastType.Success);
        }

        private void UpdateShortlistAndFinalCollections()
        {
            var sorted = ShortlistRows.OrderByDescending(x => x.Score).ToList(); ShortlistRows.Clear();
            for (int i = 0; i < sorted.Count; i++) { sorted[i].Rank = i + 1; sorted[i].IsWaitlisted = (i >= _granteeSlots); ShortlistRows.Add(sorted[i]); }
            FinalGranteesList.Clear(); foreach (var r in ShortlistRows.Where(x => x.IsFinalGrantee).OrderBy(x => x.Name)) FinalGranteesList.Add(r);
            FilterShortlistRows(); OnPropertyChanged(nameof(Tab2QuotaSummary)); OnPropertyChanged(nameof(ShortlistSummary)); OnPropertyChanged(nameof(FinalSummaryText));
        }

        private void FilterShortlistRows()
        {
            IEnumerable<SelectionRow> q = ShortlistRows;
            if (_shortlistAuditFilter == "Clean") q = q.Where(x => !x.HasCollegeAgeDependent);
            else if (_shortlistAuditFilter == "CollegeAge") q = q.Where(x => x.HasCollegeAgeDependent);
            if (!string.IsNullOrWhiteSpace(_shortlistSearchText))
            {
                string t = _shortlistSearchText.Trim().ToLowerInvariant();
                q = q.Where(x => (x.Name != null && x.Name.ToLowerInvariant().Contains(t)) || (x.SpId != null && x.SpId.ToLowerInvariant().Contains(t)) || (x.Barangay != null && x.Barangay.ToLowerInvariant().Contains(t)) || (x.CrossCheckText != null && x.CrossCheckText.ToLowerInvariant().Contains(t)) || (x.ComplianceRemark != null && x.ComplianceRemark.ToLowerInvariant().Contains(t)));
            }
            FilteredShortlistRows.Clear(); foreach (var r in q) FilteredShortlistRows.Add(r);
            RecalculateShortlistSelectionCounts();
        }

        private void PromptRevokeShortlist(SelectionRow r)
        {
            if (r == null) return;
            _shortlistRevokeTargets = new List<SelectionRow> { r }; OutlierTarget = _allItems.FirstOrDefault(x => x.SpId == r.SpId);
            OutlierModalTitle = "Revoke Candidate"; OutlierTargetNameAndId = $"Candidate: {r.Name} ({r.SpId})";
            OnPropertyChanged(nameof(OutlierModalConfirmButtonText)); IsOutlierModalOpen = true;
        }

        private void BatchConfirmShortlist()
        {
            var targets = FilteredShortlistRows.Where(x => x.IsSelected).ToList();
            if (targets.Count == 0) { ToastNotification.Show("No Selection", "Please check candidate boxes to confirm.", ToastType.Info); return; }
            foreach (var r in targets) { r.IsFinalGrantee = true; r.IsSelected = false; }
            UpdateShortlistAndFinalCollections(); RecalculateShortlistSelectionCounts();
            ToastNotification.Show("Batch Confirmed", $"{targets.Count} candidate(s) confirmed as final grantees.", ToastType.Success);
        }

        private void BatchRevokeShortlist()
        {
            var targets = FilteredShortlistRows.Where(x => x.IsSelected).ToList();
            if (targets.Count == 0) { ToastNotification.Show("No Selection", "Please check candidate boxes to revoke.", ToastType.Info); return; }
            _shortlistRevokeTargets = targets; OutlierTarget = null;
            OutlierModalTitle = $"Revoke {targets.Count} Candidate(s)"; OutlierTargetNameAndId = $"{targets.Count} candidate(s) selected from Approved Selection";
            OnPropertyChanged(nameof(OutlierModalConfirmButtonText)); IsOutlierModalOpen = true;
        }

        private async Task ConfirmOutlierRejectionAsync()
        {
            if (_shortlistRevokeTargets != null && _shortlistRevokeTargets.Count > 0)
            {
                int count = _shortlistRevokeTargets.Count;
                foreach (var r in _shortlistRevokeTargets)
                {
                    ShortlistRows.Remove(r); FinalGranteesList.Remove(r);
                    var orig = _allItems.FirstOrDefault(x => x.SpId == r.SpId);
                    if (orig != null) { orig.IsDisqualified = true; orig.DisqualificationReason = OutlierRejectionReason; orig.Status = "Rejected"; }
                    _ = _apiService.RejectOutlierAsync(new OutlierRejectionDto { SpId = r.SpId, Reason = OutlierRejectionReason, Notes = OutlierNotes, AutoPromoteWaitlist = OutlierAutoPromote });
                }
                _shortlistRevokeTargets = null; IsOutlierModalOpen = false;
                UpdateShortlistAndFinalCollections(); RecalculateQuotaAndFilter(); RecalculateShortlistSelectionCounts();
                ToastNotification.Show("Revocation Complete", $"{count} candidate(s) revoked ({OutlierRejectionReason}).", ToastType.Warning);
            }
            else if (OutlierTarget != null)
            {
                var target = OutlierTarget; target.IsDisqualified = true; target.DisqualificationReason = OutlierRejectionReason; target.Status = "Rejected";
                await _apiService.RejectOutlierAsync(new OutlierRejectionDto { SpId = target.SpId, Reason = OutlierRejectionReason, Notes = OutlierNotes, AutoPromoteWaitlist = OutlierAutoPromote });
                IsOutlierModalOpen = false; RecalculateQuotaAndFilter();
                ToastNotification.Show("Outlier Disqualified", $"{target.Name} removed from queue. Next candidate promoted.", ToastType.Warning);
            }
        }

        private void ToggleGrantee(SelectionRow row, bool confirm) { if (row == null) return; row.IsFinalGrantee = confirm; UpdateShortlistAndFinalCollections(); ToastNotification.Show(confirm ? "Confirmed" : "Revoked", $"{row.Name} {(confirm ? "confirmed as final grantee." : "removed from final grantees.")}", confirm ? ToastType.Success : ToastType.Warning); }
        private async Task OnFiscalYearChangedAsync(int index) { FinalGranteesList.Clear(); if (index == 0) foreach (var r in ShortlistRows.Where(x => x.IsFinalGrantee).OrderBy(x => x.Name)) FinalGranteesList.Add(r); else { var hist = await _apiService.GetFinalGranteesByYearAsync(index == 1 ? 2025 : 2024); foreach (var r in hist.OrderBy(x => x.Name)) FinalGranteesList.Add(r); } OnPropertyChanged(nameof(FinalSummaryText)); }
        private async Task ExportCsvAsync(string name, Task<string> generator, string msg) { var dlg = new SaveFileDialog { FileName = name, DefaultExt = ".csv", Filter = "CSV file (*.csv)|*.csv" }; if (dlg.ShowDialog() == true) { File.WriteAllText(dlg.FileName, await generator); ToastNotification.Show("Export Complete", msg, ToastType.Success); } }
    }
}
