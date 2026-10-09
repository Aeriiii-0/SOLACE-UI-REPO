using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SOLUM_UI.Core;
using SOLUM_UI.Models;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.Services.Implementations;
using SOLUM_UI.Services.Interfaces;

namespace SOLUM_UI.ViewModels
{
    public partial class SubsidyRecommendationViewModel : ObservableObject
    {
        private readonly ISubsidyApiService _apiService;
        private readonly List<SubsidyItem> _allItems = new List<SubsidyItem>();
        private List<SelectionRow> _shortlistRevokeTargets;
        private Guid? _activeCycleId;
        private int _activeTabIndex, _budgetAmount = 100000, _granteeSlots = 100, _selectedFiscalYearIndex;
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
            if (!AuthApiService.Instance.IsAdmin)
            {
                ToastNotification.Show("Access Denied", "Administrator privilege required.", ToastType.Error);
                return;
            }
            _ = RefreshDataAsync();
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
        public ICommand RefreshCommand { get; private set; } public ICommand GenerateRecommendationsCommand { get; private set; }

        public async Task RefreshDataAsync()
        {
            try
            {
                var cycleResp = await _apiService.GetActiveCycleAsync();
                if (cycleResp != null && cycleResp.Succeeded && cycleResp.Data != null)
                {
                    _activeCycleId = cycleResp.Data.Id;
                    BudgetAmount = (int)cycleResp.Data.TotalBudget;
                }

                int yr = SelectedFiscalYearIndex == 1 ? 2025 : SelectedFiscalYearIndex == 2 ? 2024 : DateTime.Now.Year;
                var grResp = await _apiService.GetGranteesByFiscalYearAsync(yr);
                var slResp = await _apiService.GetShortlistAsync(_activeCycleId);
                ShortlistRows.Clear();
                int rk = 1;
                if (grResp?.Data != null)
                {
                    foreach (var g in grResp.Data)
                        ShortlistRows.Add(SubsidyModelMapper.GranteeToRow(g, rk++));
                }
                if (slResp?.Data != null)
                {
                    foreach (var d in slResp.Data)
                    {
                        if (!ShortlistRows.Any(x => x.SpId == d.SpId || (d.EvaluationId != Guid.Empty && x.EvaluationId == d.EvaluationId)))
                            ShortlistRows.Add(SubsidyModelMapper.ToRow(d, rk++));
                    }
                }
                UpdateShortlistAndFinalCollections();

                var dtos = await _apiService.GetRecommendationsAsync(_activeCycleId);
                _allItems.Clear();
                if (dtos?.Data != null)
                {
                    foreach (var d in dtos.Data)
                    {
                        bool inAudit = ShortlistRows.Any(s => s.SpId == d.SpId || (d.EvaluationId != Guid.Empty && s.EvaluationId == d.EvaluationId));
                        if (!inAudit) _allItems.Add(SubsidyModelMapper.ToItem(d));
                    }
                }
                RecalculateQuotaAndFilter();
            }
            catch (Exception ex) { ToastNotification.Show("API Error", ex.Message, ToastType.Error); }
        }

        public void ApplyAuditFilter(string filter) { _auditFilter = filter; OnPropertyChanged(nameof(AuditFilterLabel)); RecalculateQuotaAndFilter(); }
        public void ApplyShortlistAuditFilter(string filter) { _shortlistAuditFilter = filter; OnPropertyChanged(nameof(ShortlistAuditFilterLabel)); FilterShortlistRows(); }
        public void RecalculateSelectionCounts() { OnPropertyChanged(nameof(SelectedCountSummary)); OnPropertyChanged(nameof(SelectTopNLabel)); }

        private void RecalculateQuotaAndFilter()
        {
            IEnumerable<SubsidyItem> q = _allItems.Where(x => !x.IsDisqualified && !ShortlistRows.Any(s => s.SpId == x.SpId || (x.EvaluationId != Guid.Empty && s.EvaluationId == x.EvaluationId)));
            if (_auditFilter == "Clean") q = q.Where(x => !x.HasCollegeAgeDependent);
            else if (_auditFilter == "CollegeAge") q = q.Where(x => x.HasCollegeAgeDependent);
            var list = q.OrderByDescending(x => x.Score).ToList(); FilteredRecommendations.Clear();
            for (int i = 0; i < list.Count; i++) { list[i].Rank = i + 1; list[i].IsWithinQuota = (i < _granteeSlots); FilteredRecommendations.Add(list[i]); }
            OnPropertyChanged(nameof(GranteeSlotsText)); OnPropertyChanged(nameof(SelectTopNLabel)); OnPropertyChanged(nameof(SelectedCountSummary));
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
    }
}
