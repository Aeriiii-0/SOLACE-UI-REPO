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
        private bool _isDrawerOpen, _isOutlierModalOpen, _outlierAutoPromote = true, _isBatchMode, _isLoading;

        public SubsidyRecommendationViewModel(ISubsidyApiService apiService = null)
        {
            _apiService = apiService ?? new SubsidyApiService();
            FilteredRecommendations = new ObservableCollection<SubsidyItem>();
            ShortlistRows = new ObservableCollection<SelectionRow>();
            FilteredShortlistRows = new ObservableCollection<SelectionRow>();
            FinalGranteesList = new ObservableCollection<SelectionRow>();
            AvailableFiscalCycles = new ObservableCollection<FiscalCycleOption>();
            InitCommands();
            InitPagination();
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
        public ObservableCollection<FiscalCycleOption> AvailableFiscalCycles { get; }

        private FiscalCycleOption _selectedFiscalCycle;
        public FiscalCycleOption SelectedFiscalCycle
        {
            get => _selectedFiscalCycle;
            set
            {
                if (SetProperty(ref _selectedFiscalCycle, value))
                {
                    OnPropertyChanged(nameof(CanModifyGrantees));
                    OnPropertyChanged(nameof(RosterStatusBadgeText));
                    OnPropertyChanged(nameof(RosterStatusBadgeBg));
                    OnPropertyChanged(nameof(RosterStatusBadgeFg));
                    if (value != null) _ = LoadCycleDataAsync(value);
                }
            }
        }

        public string ShortlistSearchText { get => _shortlistSearchText; set { if (SetProperty(ref _shortlistSearchText, value)) FilterShortlistRows(); } }
        public int ActiveTabIndex { get => _activeTabIndex; set { if (SetProperty(ref _activeTabIndex, value)) { OnPropertyChanged(nameof(TabRecommendationsVisible)); OnPropertyChanged(nameof(TabShortlistVisible)); OnPropertyChanged(nameof(TabFinalVisible)); } } }
        public bool TabRecommendationsVisible => ActiveTabIndex == 0; public bool TabShortlistVisible => ActiveTabIndex == 1; public bool TabFinalVisible => ActiveTabIndex == 2;
        public bool IsBatchMode { get => _isBatchMode; set { if (SetProperty(ref _isBatchMode, value)) { OnPropertyChanged(nameof(BatchSelectColumnWidth)); OnPropertyChanged(nameof(BatchModeButtonText)); } } }
        public double BatchSelectColumnWidth => IsBatchMode ? 38 : 0; public string BatchModeButtonText => IsBatchMode ? "Batch Mode: On" : "Batch Select";

        public int GranteeSlots
        {
            get => _granteeSlots;
            set
            {
                if (value < 0) value = 0;
                if (SetProperty(ref _granteeSlots, value))
                {
                    _budgetAmount = _granteeSlots * 1000;
                    OnPropertyChanged(nameof(BudgetAmount));
                    OnPropertyChanged(nameof(CommittedBudgetText));
                    OnPropertyChanged(nameof(GranteeSlotsText));
                    NotifyQuotaMetricsChanged();
                    RecalculateQuotaAndFilter();
                }
            }
        }
        public int BudgetAmount
        {
            get => _budgetAmount;
            set
            {
                if (SetProperty(ref _budgetAmount, value))
                {
                    _granteeSlots = Math.Max(0, value / 1000);
                    OnPropertyChanged(nameof(GranteeSlots));
                    OnPropertyChanged(nameof(CommittedBudgetText));
                    OnPropertyChanged(nameof(GranteeSlotsText));
                    NotifyQuotaMetricsChanged();
                    RecalculateQuotaAndFilter();
                }
            }
        }
        public string CommittedBudgetText => $"₱{_budgetAmount:N0}";
        public string GranteeSlotsText => $"{_granteeSlots} target slots";
        public string SelectedCountSummary => $"{FilteredRecommendations.Count(x => x.IsSelected)} selected";
        public string SelectTopNLabel => (FilteredRecommendations.Count > 0 && FilteredRecommendations.Take(Math.Min(_granteeSlots, FilteredRecommendations.Count)).All(x => x.IsSelected)) ? $"Deselect Top {_granteeSlots}" : $"Select Top {_granteeSlots}";

        public int ConfirmedGranteesCount => FinalGranteesList.Count;
        public int RemainingSlots => Math.Max(0, GranteeSlots - ConfirmedGranteesCount);
        public double QuotaFillPercentage => GranteeSlots > 0 ? Math.Min(100.0, ((double)ConfirmedGranteesCount / GranteeSlots) * 100.0) : 0.0;
        public string QuotaProgressText => $"{ConfirmedGranteesCount} of {GranteeSlots} confirmed ({QuotaFillPercentage:F0}%)";
        public string QuotaPercentageLabel => $"{QuotaFillPercentage:F0}% allocated";
        public void NotifyQuotaMetricsChanged()
        {
            OnPropertyChanged(nameof(ConfirmedGranteesCount));
            OnPropertyChanged(nameof(RemainingSlots));
            OnPropertyChanged(nameof(QuotaFillPercentage));
            OnPropertyChanged(nameof(QuotaProgressText));
            OnPropertyChanged(nameof(QuotaPercentageLabel));
            OnPropertyChanged(nameof(FinalSummaryText));
            OnPropertyChanged(nameof(Tab2QuotaSummary));
            OnPropertyChanged(nameof(ShortlistSummary));
        }

        public string AuditFilterLabel => _auditFilter == "All" ? "Filter: All" : (_auditFilter == "CollegeAge" ? "Filter: City Educ" : "Filter: Cleared");
        public string ShortlistAuditFilterLabel => _shortlistAuditFilter == "Pending" ? "Filter: Pending Confirmation" : (_shortlistAuditFilter == "Confirmed" ? "Filter: Confirmed Grantees" : (_shortlistAuditFilter == "CollegeAge" ? "Filter: City Educ" : (_shortlistAuditFilter == "Clean" ? "Filter: Cleared" : "Filter: All")));
        public string Tab2QuotaSummary => $"Slots: {ShortlistRows.Count(x => !x.IsWaitlisted)} of {_granteeSlots} filled";
        public string ShortlistSummary => $"{ShortlistRows.Count(x => !x.IsWaitlisted)} of {_granteeSlots} slots allocated • ₱{ShortlistRows.Count(x => !x.IsWaitlisted) * 1000:N0} committed";
        public string FinalSummaryText => $"{FinalGranteesList.Count} official grantees confirmed";

        public bool IsAllShortlistSelected { get => FilteredShortlistRows.Count > 0 && FilteredShortlistRows.All(x => x.IsSelected); set { foreach (var r in FilteredShortlistRows) r.IsSelected = value; OnPropertyChanged(); RecalculateShortlistSelectionCounts(); } }
        public int ShortlistSelectedCount => FilteredShortlistRows.Count(x => x.IsSelected);
        public bool HasShortlistSelection => ShortlistSelectedCount > 0;
        public string ShortlistBatchActionLabel => $"{ShortlistSelectedCount} selected";
        public string SelectAllShortlistButtonText => (FilteredShortlistRows.Count > 0 && FilteredShortlistRows.All(x => x.IsSelected)) ? "Clear All" : "Select All";
        public void RecalculateShortlistSelectionCounts() { OnPropertyChanged(nameof(IsAllShortlistSelected)); OnPropertyChanged(nameof(ShortlistSelectedCount)); OnPropertyChanged(nameof(HasShortlistSelection)); OnPropertyChanged(nameof(ShortlistBatchActionLabel)); OnPropertyChanged(nameof(SelectAllShortlistButtonText)); }

        public SubsidyItem ActiveCandidate { get => _activeCandidate; set => SetProperty(ref _activeCandidate, value); }
        public bool IsDrawerOpen { get => _isDrawerOpen; set => SetProperty(ref _isDrawerOpen, value); } public bool IsOutlierModalOpen { get => _isOutlierModalOpen; set => SetProperty(ref _isOutlierModalOpen, value); }
        public string OutlierModalTitle { get => _outlierModalTitle; set => SetProperty(ref _outlierModalTitle, value); } public string OutlierModalConfirmButtonText => (_shortlistRevokeTargets != null && _shortlistRevokeTargets.Count > 0) ? "Confirm Revocation" : "Confirm Rejection";
        public SubsidyItem OutlierTarget { get => _outlierTarget; set { SetProperty(ref _outlierTarget, value); OnPropertyChanged(nameof(OutlierTargetNameAndId)); } }
        public string OutlierTargetNameAndId { get => _outlierTargetNameAndId ?? $"Candidate: {_outlierTarget?.Name}"; set { _outlierTargetNameAndId = value; OnPropertyChanged(); } }
        public string OutlierRejectionReason { get => _outlierRejectionReason; set => SetProperty(ref _outlierRejectionReason, value); } public string OutlierNotes { get => _outlierNotes; set => SetProperty(ref _outlierNotes, value); }
        public bool OutlierAutoPromote { get => _outlierAutoPromote; set => SetProperty(ref _outlierAutoPromote, value); }
        public int SelectedFiscalYearIndex { get => _selectedFiscalYearIndex; set { if (SetProperty(ref _selectedFiscalYearIndex, value)) { OnPropertyChanged(nameof(CanModifyGrantees)); } } }

        private bool _isRosterLocked; private int _activeCycleFiscalYear = 2026;
        public bool IsRosterLocked { get => _isRosterLocked; set { if (SetProperty(ref _isRosterLocked, value)) { OnPropertyChanged(nameof(CanModifyGrantees)); OnPropertyChanged(nameof(RosterStatusBadgeText)); OnPropertyChanged(nameof(RosterStatusBadgeBg)); OnPropertyChanged(nameof(RosterStatusBadgeFg)); OnPropertyChanged(nameof(RosterLockActionText)); OnPropertyChanged(nameof(RosterLockActionBg)); OnPropertyChanged(nameof(RosterLockActionFg)); } } }
        public bool CanModifyGrantees => !IsRosterLocked && (SelectedFiscalCycle == null || SelectedFiscalCycle.IsActive);
        public string RosterStatusBadgeText => (SelectedFiscalCycle != null && !SelectedFiscalCycle.IsActive) ? "Archived Roster" : (IsRosterLocked ? "Finalized & Locked" : "Open for Review");
        public string RosterStatusBadgeBg => (SelectedFiscalCycle != null && !SelectedFiscalCycle.IsActive) ? "#F3F4F6" : (IsRosterLocked ? "#DCFCE7" : "#FEF3C7");
        public string RosterStatusBadgeFg => (SelectedFiscalCycle != null && !SelectedFiscalCycle.IsActive) ? "#4B5563" : (IsRosterLocked ? "#166534" : "#B45309");
        public string RosterLockActionText => IsRosterLocked ? "Unlock Roster" : "Finalize & Lock";
        public string RosterLockActionBg => IsRosterLocked ? "#FFFBEB" : "#702943";
        public string RosterLockActionFg => IsRosterLocked ? "#B45309" : "#FFFFFF";
        public int ActiveCycleFiscalYear => _activeCycleFiscalYear;

        public ICommand SwitchTabCommand { get; private set; } public ICommand ToggleBatchModeCommand { get; private set; } public ICommand SelectTopNCommand { get; private set; } public ICommand ShortlistSelectedCommand { get; private set; }
        public ICommand QuickAcceptRowCommand { get; private set; } public ICommand QuickRejectRowCommand { get; private set; } public ICommand OpenExplainDrawerCommand { get; private set; } public ICommand CloseExplainDrawerCommand { get; private set; }
        public ICommand DrawerShortlistCommand { get; private set; } public ICommand OpenOutlierModalCommand { get; private set; } public ICommand CloseOutlierModalCommand { get; private set; } public ICommand ConfirmOutlierRejectionCommand { get; private set; }
        public ICommand ConfirmGranteeCommand { get; private set; } public ICommand RevokeGranteeCommand { get; private set; } public ICommand ConfirmShortlistCandidateCommand { get; private set; } public ICommand RevokeShortlistCandidateCommand { get; private set; } public ICommand BatchConfirmShortlistCommand { get; private set; } public ICommand BatchRevokeShortlistCommand { get; private set; } public ICommand SelectAllShortlistCommand { get; private set; }
        public ICommand ExportCityEducCommand { get; private set; } public ICommand ExportPantawid4PsCommand { get; private set; } public ICommand ExportFinalLedgerCommand { get; private set; } public ICommand ViewFullRecordCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; } public ICommand GenerateRecommendationsCommand { get; private set; }
        public ICommand ToggleRosterLockCommand { get; private set; } public ICommand StartNewCycleCommand { get; private set; }

        public async Task RefreshDataAsync()
        {
            if (_isLoading) return;
            _isLoading = true;
            try
            {
                var cyclesResp = await _apiService.GetCyclesAsync();
                AvailableFiscalCycles.Clear();
                FiscalCycleOption activeOption = null;
                if (cyclesResp?.Data != null && cyclesResp.Data.Count > 0)
                {
                    foreach (var c in cyclesResp.Data)
                    {
                        var opt = new FiscalCycleOption
                        {
                            CycleId = c.Id,
                            FiscalYear = c.FiscalYear,
                            Status = c.Status,
                            AllocatedSlots = c.AllocatedSlots,
                            TotalBudget = c.TotalBudget,
                            PerGranteeAmount = c.PerGranteeAmount
                        };
                        AvailableFiscalCycles.Add(opt);
                        if (opt.IsActive && activeOption == null) activeOption = opt;
                    }
                }
                if (AvailableFiscalCycles.Count == 0)
                {
                    var fallback = new FiscalCycleOption { FiscalYear = DateTime.Now.Year, Status = "Active", AllocatedSlots = 100, TotalBudget = 100000m };
                    AvailableFiscalCycles.Add(fallback);
                    activeOption = fallback;
                }
                SelectedFiscalCycle = activeOption ?? AvailableFiscalCycles.FirstOrDefault();
            }
            catch (Exception ex) { ToastNotification.Show("API Error", ex.Message, ToastType.Error); }
            finally { _isLoading = false; }
        }
    }
}
