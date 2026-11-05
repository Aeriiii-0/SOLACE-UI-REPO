using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;
using SOLUM_UI.DTOs;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.Views;
using SOLUM_UI.Views.Dialogs;

namespace SOLUM_UI.ViewModels
{
    public partial class SubsidyRecommendationViewModel
    {
        public async void ToggleRosterLock()
        {
            var cycleId = SelectedFiscalCycle?.CycleId ?? _activeCycleId;
            int yr = SelectedFiscalCycle?.FiscalYear ?? ActiveCycleFiscalYear;
            if (!IsRosterLocked)
            {
                if (FinalGranteesList.Count == 0) { ToastNotification.Show("No Grantees", "Please confirm at least one grantee before locking the roster.", ToastType.Warning); return; }
                bool ok = ActionConfirmDialog.Show("Finalize & Lock Roster", $"Are you sure you want to finalize and lock the FY {yr} Grantee Roster ({FinalGranteesList.Count} confirmed recipients)?\n\nOnce locked, this roster is certified for official disbursement and candidates cannot be revoked without unlocking.", "Lock Roster", "Cancel", ConfirmThemeType.Primary);
                if (ok)
                {
                    if (cycleId.HasValue) await _apiService.FinalizeCycleAsync(cycleId.Value);
                    IsRosterLocked = true;
                    if (SelectedFiscalCycle != null) SelectedFiscalCycle.Status = "Finalized";
                    // Switch to Final Grantees tab and clear operational pipeline lists to avoid unwanted re-fetches
                    ActiveTabIndex = 2;
                    PagedRecommendations.Clear();
                    FilteredRecommendations.Clear();
                    _allItems.Clear();
                    QueueTotalCount = 0;
                    ShortlistRows.Clear();
                    FilteredShortlistRows.Clear();
                    NotifyCycleProperties();
                    ToastNotification.Show("Roster Finalized", $"FY {yr} roster locked for official disbursement.", ToastType.Success);
                }
            }
            else
            {
                bool ok = ActionConfirmDialog.Show("Unlock Roster for Amendments", $"Are you sure you want to unlock the FY {yr} roster?\n\nThis will re-enable candidate confirmation, revocation, and quota adjustments for administrative amendments.", "Unlock Roster", "Cancel", ConfirmThemeType.Warning);
                if (ok)
                {
                    if (cycleId.HasValue) await _apiService.UnlockCycleAsync(cycleId.Value);
                    IsRosterLocked = false;
                    if (SelectedFiscalCycle != null)
                    {
                        SelectedFiscalCycle.Status = "Active";
                        foreach (var c in AvailableFiscalCycles)
                        {
                            if (c != SelectedFiscalCycle && c.IsActive) c.Status = "Finalized";
                        }
                    }
                    NotifyCycleProperties();
                    ToastNotification.Show("Roster Unlocked", "Cycle unlocked. You can now make amendments to the final grantees and candidate selection.", ToastType.Info);
                }
            }
        }

        public async Task LoadCycleDataAsync(FiscalCycleOption cycle)
        {
            if (cycle == null) return;
            IsLoading = true;
            try { await LoadCycleDataInternalAsync(cycle); }
            catch (Exception ex) { ToastNotification.Show("Load Error", ex.Message, ToastType.Error); }
            finally { IsLoading = false; }
        }

        public async Task LoadCycleDataInternalAsync(FiscalCycleOption cycle)
        {
            if (cycle == null) return;
            _activeCycleId = cycle.CycleId;
            _activeCycleFiscalYear = cycle.FiscalYear;
            IsRosterLocked = cycle.IsFinalized || string.Equals(cycle.Status, "Finalized", StringComparison.OrdinalIgnoreCase) || !cycle.IsActive;
            GranteeSlots = cycle.AllocatedSlots > 0 ? cycle.AllocatedSlots : 100;
            GrantAmountPerRecipient = cycle.PerGranteeAmount > 0 ? (int)cycle.PerGranteeAmount : DefaultGrantAmount;
            // Fetch Grantees
            var granteesTask = _apiService.GetGranteesByFiscalYearAsync(cycle.FiscalYear);
            // Only fetch Shortlist, Queue, and Rejections if cycle is NOT locked
            Task<BaseResponse<List<SubsidyRecommendationDto>>> shortlistTask;
            Task<BaseResponse<List<SubsidyRecommendationDto>>> recsTask;
            Task<BaseResponse<List<SubsidyRecommendationDto>>> rejTask;
            if (!IsRosterLocked && cycle.CycleId.HasValue)
            {
                shortlistTask = _apiService.GetShortlistAsync(cycle.CycleId);
                recsTask = _apiService.GetAllRecommendationsAsync(new SOLUM_UI.DTOs.GetSubsidyRecommendationsRequest
                {
                    CycleId = cycle.CycleId,
                    SortBy = "PriorityScore",
                    SortOrder = "desc"
                });
                rejTask = _apiService.GetRejectedCandidatesAsync(cycle.CycleId);
            }
            else
            {
                shortlistTask = Task.FromResult<BaseResponse<List<SubsidyRecommendationDto>>>(null);
                recsTask = Task.FromResult<BaseResponse<List<SubsidyRecommendationDto>>>(null);
                rejTask = Task.FromResult<BaseResponse<List<SubsidyRecommendationDto>>>(null);
            }
            await Task.WhenAll(granteesTask, shortlistTask, recsTask, rejTask);
            var grResp = await granteesTask;
            var slResp = await shortlistTask;
            var dtos = await recsTask;
            var rejResp = await rejTask;
            DisqualifiedCandidates.Clear();
            if (rejResp?.Data != null && rejResp.Data.Count > 0)
            {
                foreach (var r in rejResp.Data)
                {
                    DisqualifiedCandidates.Add(new DisqualifiedCandidateItem
                    {
                        EvaluationId = r.EvaluationId,
                        SoloParentId = r.SoloParentId,
                        SpId = r.SpId,
                        FullName = r.Name,
                        Barangay = r.Barangay,
                        MonthlyIncome = (double)r.MonthlyIncome,
                        PriorityScore = r.Score,
                        Priority = r.Priority,
                        Dependents = r.Dependants,
                        RejectionReason = !string.IsNullOrEmpty(r.RejectionReason) ? r.RejectionReason : "Administrative Disqualification",
                        RejectionNotes = r.RejectionNotes,
                        DisqualifiedBy = !string.IsNullOrEmpty(r.ReviewedBy) ? r.ReviewedBy : "Admin / Reviewer",
                        DisqualifiedAt = r.ConfirmedAt ?? DateTime.Now
                    });
                }
            }
            FilterDisqualifiedCandidates();
            NotifyDisqualifiedMetricsChanged();
            FinalGranteesList.Clear();
            int rk = 1;
            if (grResp?.Data != null && grResp.Data.Count > 0)
            {
                foreach (var g in grResp.Data.OrderBy(x => x.FullName))
                    FinalGranteesList.Add(SubsidyModelMapper.GranteeToRow(g, rk++));
            }
            ShortlistRows.Clear();
            foreach (var g in FinalGranteesList) ShortlistRows.Add(g);
            if (slResp?.Data != null && slResp.Data.Count > 0)
            {
                foreach (var d in slResp.Data)
                {
                    bool inShortlist = ShortlistRows.Any(x => (!string.IsNullOrEmpty(d.SpId) && x.SpId == d.SpId)
                                                           || (d.EvaluationId != Guid.Empty && x.EvaluationId == d.EvaluationId)
                                                           || (d.SoloParentId != Guid.Empty && x.SoloParentId == d.SoloParentId)
                                                           || (!string.IsNullOrEmpty(d.Name) && string.Equals(x.Name, d.Name, StringComparison.OrdinalIgnoreCase)));
                    if (!inShortlist) ShortlistRows.Add(SubsidyModelMapper.ToRow(d, rk++));
                }
            }
            UpdateShortlistAndFinalCollections(enrich: false);
            PagedRecommendations.Clear();
            FilteredRecommendations.Clear();
            _allItems.Clear();
            if (dtos?.Data != null && dtos.Data.Count > 0)
            {
                QueueTotalCount = dtos.Data.Count;
                QueueTotalPages = 1;
                _queueCurrentPage = 1;
                for (int i = 0; i < dtos.Data.Count; i++)
                {
                    var item = SubsidyModelMapper.ToItem(dtos.Data[i]);
                    item.Rank = i + 1;
                    item.IsWithinQuota = (item.Rank <= _granteeSlots);
                    PagedRecommendations.Add(item);
                    FilteredRecommendations.Add(item);
                    _allItems.Add(item);
                }
            }
            else
            {
                QueueTotalCount = 0;
                QueueTotalPages = 1;
                _queueCurrentPage = 1;
            }
            OnPropertyChanged(nameof(QueueCurrentPage));
            OnPropertyChanged(nameof(CanQueuePrevPage));
            OnPropertyChanged(nameof(CanQueueNextPage));
            OnPropertyChanged(nameof(QueuePageNumberLabel));
            OnPropertyChanged(nameof(QueuePageSummary));
            OnPropertyChanged(nameof(RecommendationsSummary));
            RecalculateSelectionCounts();
            NotifyQuotaMetricsChanged();
            EnrichAllCandidatesFromRecordsAsync();
            LastRefreshedText = $"Updated: {DateTime.Now:h:mm tt}";
            RefreshTooltip = $"Refresh subsidy recommendations (Last refreshed: {DateTime.Now:h:mm:ss tt})";
        }

        public async Task StartNewCycleAsync()
        {
            if (!IsRosterLocked)
            {
                ToastNotification.Show("Active Cycle Incomplete", $"Please finalize and lock the FY {ActiveCycleFiscalYear} grantee roster before launching a new cycle.", ToastType.Warning);
                return;
            }

            int nextYear = ActiveCycleFiscalYear + 1;
            bool ok = ActionConfirmDialog.Show("Launch New Fiscal Cycle", $"Starting a new cycle will advance the active operational cycle to FY {nextYear}.\n\nThe current FY {ActiveCycleFiscalYear} certified roster will be archived.\n\nDo you wish to proceed?", "Launch Cycle", "Cancel", ConfirmThemeType.Primary);
            if (!ok) return;
            var dlg = new NewCycleDialog(nextYear) { Owner = Application.Current?.MainWindow };
            if (dlg.ShowDialog() == true && dlg.ResultRequest != null)
            {
                try
                {
                    var resp = await _apiService.CreateCycleAsync(dlg.ResultRequest);
                    if (resp != null && resp.Succeeded)
                    {
                        var newCycleId = resp.Data?.Id;
                        if (newCycleId.HasValue && newCycleId.Value != Guid.Empty)
                        {
                            await _apiService.GenerateRecommendationsAsync(newCycleId.Value);
                        }
                        IsRosterLocked = false;
                        await RefreshDataAsync(forceReloadCycles: true);
                        ActiveTabIndex = 0;
                        ToastNotification.Show("New Cycle Active", $"Fiscal Year {dlg.ResultRequest.FiscalYear} cycle started successfully. Fresh candidate recommendations generated.", ToastType.Success);
                    }
                    else
                    {
                        string err = resp?.Errors != null ? string.Join(", ", resp.Errors) : "Failed to create cycle.";
                        ToastNotification.Show("Error", err, ToastType.Error);
                    }
                }
                catch (Exception ex)
                {
                    ToastNotification.Show("Cycle Creation Error", ex.Message, ToastType.Error);
                }
            }
        }

        public async void ApplyAuditFilter(string filter) { _auditFilter = filter; OnPropertyChanged(nameof(AuditFilterLabel)); await LoadQueuePageAsync(1); }
        public void ApplyShortlistAuditFilter(string filter) { _shortlistAuditFilter = filter; OnPropertyChanged(nameof(ShortlistAuditFilterLabel)); FilterShortlistRows(); }
        public void RecalculateSelectionCounts() { OnPropertyChanged(nameof(SelectedCountSummary)); OnPropertyChanged(nameof(SelectTopNLabel)); OnPropertyChanged(nameof(SelectAllQueueButtonText)); OnPropertyChanged(nameof(HasQueueSelection)); }

        private void RecalculateQuotaAndFilter()
        {
            if (FilteredRecommendations != null)
            {
                for (int i = 0; i < FilteredRecommendations.Count; i++)
                {
                    FilteredRecommendations[i].Rank = i + 1;
                    FilteredRecommendations[i].IsWithinQuota = (i + 1 <= _granteeSlots);
                }
            }
            if (PagedRecommendations != null && PagedRecommendations != FilteredRecommendations)
            {
                foreach (var item in PagedRecommendations)
                {
                    item.IsWithinQuota = (item.Rank <= _granteeSlots);
                }
            }
            QueueTotalCount = FilteredRecommendations?.Count ?? 0;
            OnPropertyChanged(nameof(GranteeSlotsText));
            OnPropertyChanged(nameof(SelectTopNLabel));
            OnPropertyChanged(nameof(SelectAllQueueButtonText));
            OnPropertyChanged(nameof(SelectedCountSummary));
            OnPropertyChanged(nameof(HasQueueSelection));
            OnPropertyChanged(nameof(RecommendationsSummary));
        }

        private void UpdateShortlistAndFinalCollections(bool enrich = true)
        {
            var sorted = ShortlistRows.OrderByDescending(x => x.Score).ToList();
            ShortlistRows.Clear();
            for (int i = 0; i < sorted.Count; i++) { sorted[i].Rank = i + 1; sorted[i].IsWaitlisted = !sorted[i].IsFinalGrantee && (i >= _granteeSlots); ShortlistRows.Add(sorted[i]); }
            FinalGranteesList.Clear(); foreach (var r in ShortlistRows.Where(x => x.IsFinalGrantee).OrderBy(x => x.Name)) FinalGranteesList.Add(r);
            UpdateFinalPaged();
            if (enrich) EnrichAllCandidatesFromRecordsAsync();
            FilterShortlistRows(); OnPropertyChanged(nameof(Tab2QuotaSummary)); OnPropertyChanged(nameof(ShortlistSummary)); OnPropertyChanged(nameof(FinalSummaryText));
        }

        public void EnrichAllCandidatesFromRecordsAsync()
        {
            var targets = ShortlistRows.Concat(FinalGranteesList)
                .Select(x => x.SoloParentId)
                .Concat(FilteredRecommendations.Select(x => x.SoloParentId))
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
            if (targets.Count == 0) return;
            Task.Run(async () =>
            {
                using (var sem = new System.Threading.SemaphoreSlim(4))
                {
                    var tasks = targets.Select(async id =>
                    {
                        await sem.WaitAsync();
                        try
                        {
                            var resp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(id);
                            if (resp?.Data != null)
                            {
                                Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                                {
                                    foreach (var row in ShortlistRows.Where(x => x.SoloParentId == id)) SubsidyModelMapper.EnrichRowFromRecord(row, resp.Data);
                                    foreach (var row in FinalGranteesList.Where(x => x.SoloParentId == id)) SubsidyModelMapper.EnrichRowFromRecord(row, resp.Data);
                                    foreach (var item in FilteredRecommendations.Where(x => x.SoloParentId == id)) SubsidyModelMapper.EnrichItemFromRecord(item, resp.Data);
                                    if (ActiveCandidate?.SoloParentId == id) OnPropertyChanged(nameof(ActiveCandidate));
                                }));
                            }
                        }
                        catch { }
                        finally { sem.Release(); }
                    });
                    await Task.WhenAll(tasks);
                }
            });
        }

        private void FilterShortlistRows()
        {
            IEnumerable<SelectionRow> q = ShortlistRows;
            if (_shortlistAuditFilter == "Pending") q = q.Where(x => !x.IsFinalGrantee);
            else if (_shortlistAuditFilter == "Confirmed") q = q.Where(x => x.IsFinalGrantee);
            else if (_shortlistAuditFilter == "Clean") q = q.Where(x => !x.HasCollegeAgeDependent);
            else if (_shortlistAuditFilter == "CollegeAge") q = q.Where(x => x.HasCollegeAgeDependent);
            if (!string.IsNullOrWhiteSpace(_shortlistSearchText))
            {
                string t = _shortlistSearchText.Trim().ToLowerInvariant();
                q = q.Where(x => (x.Name != null && x.Name.ToLowerInvariant().Contains(t)) || (x.SpId != null && x.SpId.ToLowerInvariant().Contains(t)) || (x.Barangay != null && x.Barangay.ToLowerInvariant().Contains(t)) || (x.CrossCheckText != null && x.CrossCheckText.ToLowerInvariant().Contains(t)) || (x.ComplianceRemark != null && x.ComplianceRemark.ToLowerInvariant().Contains(t)));
            }
            FilteredShortlistRows.Clear(); foreach (var r in q) FilteredShortlistRows.Add(r);
            UpdateShortlistPaged();
            RecalculateShortlistSelectionCounts();
        }

        public async Task RefreshDisqualifiedCandidatesAsync()
        {
            try
            {
                var cycleId = _activeCycleId ?? SelectedFiscalCycle?.CycleId;
                var rejResp = await _apiService.GetRejectedCandidatesAsync(cycleId, _disqualifiedSearchText, _disqualifiedReasonFilter);
                DisqualifiedCandidates.Clear();
                if (rejResp?.Data != null && rejResp.Data.Count > 0)
                {
                    foreach (var r in rejResp.Data)
                    {
                        DisqualifiedCandidates.Add(new DisqualifiedCandidateItem
                        {
                            EvaluationId = r.EvaluationId,
                            SoloParentId = r.SoloParentId,
                            SpId = r.SpId,
                            FullName = r.Name,
                            Barangay = r.Barangay,
                            MonthlyIncome = (double)r.MonthlyIncome,
                            PriorityScore = r.Score,
                            Priority = r.Priority,
                            Dependents = r.Dependants,
                            RejectionReason = !string.IsNullOrEmpty(r.RejectionReason) ? r.RejectionReason : "Administrative Disqualification",
                            RejectionNotes = r.RejectionNotes,
                            DisqualifiedBy = !string.IsNullOrEmpty(r.ReviewedBy) ? r.ReviewedBy : "Admin / Reviewer",
                            DisqualifiedAt = r.ConfirmedAt ?? DateTime.Now
                        });
                    }
                }
                FilterDisqualifiedCandidates();
                NotifyDisqualifiedMetricsChanged();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RefreshDisqualifiedCandidatesAsync] Error: {ex.Message}");
            }
        }
    }
}