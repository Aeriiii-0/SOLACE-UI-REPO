using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SOLUM_UI.Models;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.Views;
using SOLUM_UI.Views.Dialogs;

namespace SOLUM_UI.ViewModels
{
    public partial class SubsidyRecommendationViewModel
    {
        public void ToggleRosterLock()
        {
            if (!IsRosterLocked)
            {
                if (FinalGranteesList.Count == 0) { ToastNotification.Show("No Grantees", "Please confirm at least one grantee before locking the roster.", ToastType.Warning); return; }
                bool ok = ActionConfirmDialog.Show("Finalize & Lock Roster", $"Are you sure you want to finalize and lock the FY {ActiveCycleFiscalYear} Grantee Roster ({FinalGranteesList.Count} confirmed recipients)?\n\nOnce locked, this roster is certified for official disbursement and candidates cannot be revoked without unlocking.", "Lock Roster", "Cancel", ConfirmThemeType.Primary);
                if (ok) { IsRosterLocked = true; ToastNotification.Show("Roster Finalized", $"FY {ActiveCycleFiscalYear} roster locked for official disbursement.", ToastType.Success); }
            }
            else
            {
                bool ok = ActionConfirmDialog.Show("Unlock Roster for Amendments", $"Are you sure you want to unlock the FY {ActiveCycleFiscalYear} roster?\n\nThis will re-enable candidate confirmation and revocation for administrative amendments.", "Unlock Roster", "Cancel", ConfirmThemeType.Warning);
                if (ok) { IsRosterLocked = false; ToastNotification.Show("Roster Unlocked", "You can now make amendments to the final grantees.", ToastType.Info); }
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
            if (cycle.IsActive)
            {
                _activeCycleId = cycle.CycleId;
                _activeCycleFiscalYear = cycle.FiscalYear;
                IsRosterLocked = string.Equals(cycle.Status, "Finalized", StringComparison.OrdinalIgnoreCase);

                GranteeSlots = cycle.AllocatedSlots > 0 ? cycle.AllocatedSlots : 100;
                GrantAmountPerRecipient = cycle.PerGranteeAmount > 0 ? (int)cycle.PerGranteeAmount : DefaultGrantAmount;

                // Concurrently fetch Grantees, Shortlist, and server-side Recommendations page 1 in parallel
                var granteesTask = _apiService.GetGranteesByFiscalYearAsync(cycle.FiscalYear);
                var shortlistTask = _apiService.GetShortlistAsync(cycle.CycleId);
                var recsTask = _apiService.GetRecommendationsPagedAsync(new SOLUM_UI.DTOs.GetSubsidyRecommendationsRequest
                {
                    CycleId = cycle.CycleId,
                    Page = 1,
                    PageSize = QueuePageSize,
                    SortBy = "PriorityScore",
                    SortOrder = "desc"
                });

                await Task.WhenAll(granteesTask, shortlistTask, recsTask);

                var grResp = await granteesTask;
                var slResp = await shortlistTask;
                var dtos = await recsTask;

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

                if (dtos?.Data != null)
                {
                    QueueTotalCount = dtos.Data.TotalCount;
                    QueueTotalPages = Math.Max(1, dtos.Data.TotalPages);
                    _queueCurrentPage = dtos.Data.Page;

                    if (dtos.Data.Items != null)
                    {
                        int startIndex = (dtos.Data.Page - 1) * dtos.Data.PageSize;
                        for (int i = 0; i < dtos.Data.Items.Count; i++)
                        {
                            var item = SubsidyModelMapper.ToItem(dtos.Data.Items[i]);
                            item.Rank = startIndex + i + 1;
                            item.IsWithinQuota = (item.Rank <= _granteeSlots);
                            PagedRecommendations.Add(item);
                            FilteredRecommendations.Add(item);
                            _allItems.Add(item);
                        }
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
            else
            {
                IsRosterLocked = true;
                var grResp = await _apiService.GetGranteesByFiscalYearAsync(cycle.FiscalYear);
                FinalGranteesList.Clear();
                int rk = 1;
                if (grResp?.Data != null && grResp.Data.Count > 0)
                {
                    foreach (var g in grResp.Data.OrderBy(x => x.FullName))
                        FinalGranteesList.Add(SubsidyModelMapper.GranteeToRow(g, rk++));
                }
                UpdateFinalPaged();
                OnPropertyChanged(nameof(FinalSummaryText));
            }
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
                        IsRosterLocked = false;
                        await RefreshDataAsync(forceReloadCycles: true);
                        ToastNotification.Show("New Cycle Active", $"Fiscal Year {dlg.ResultRequest.FiscalYear} cycle started successfully.", ToastType.Success);
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
            foreach (var item in PagedRecommendations)
            {
                item.IsWithinQuota = (item.Rank <= _granteeSlots);
            }
            foreach (var item in FilteredRecommendations)
            {
                item.IsWithinQuota = (item.Rank <= _granteeSlots);
            }
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
                .Where(x => x.SoloParentId != Guid.Empty && (x.ChildrenDetails == null || x.ChildrenDetails.Count == 0 || string.IsNullOrEmpty(x.Circumstance)))
                .Select(x => x.SoloParentId)
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
    }
}
