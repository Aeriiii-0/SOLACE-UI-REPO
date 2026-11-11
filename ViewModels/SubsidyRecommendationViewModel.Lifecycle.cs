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
                    OnPropertyChanged(nameof(HasNoQueueResults));
                    OnPropertyChanged(nameof(QueueEmptyStateMessage));
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
            // Fetch Grantees for this specific cycle and semester
            var granteesTask = _apiService.GetGranteesAsync(cycle.FiscalYear, cycle.CycleNumber, cycle.CycleId);
            // Only fetch Shortlist, Queue, and Rejections if cycle is NOT locked
            Task<BaseResponse<List<SubsidyRecommendationDto>>> shortlistTask;
            Task<BaseResponse<PagedResult<SubsidyRecommendationDto>>> recsTask;
            Task<BaseResponse<List<SubsidyRecommendationDto>>> rejTask;
            if (!IsRosterLocked && cycle.CycleId.HasValue)
            {
                shortlistTask = _apiService.GetShortlistAsync(cycle.CycleId);
                recsTask = _apiService.GetRecommendationsPagedAsync(new SOLUM_UI.DTOs.GetSubsidyRecommendationsRequest
                {
                    CycleId = cycle.CycleId,
                    Page = 1,
                    PageSize = QueuePageSize,
                    SortBy = "PriorityScore",
                    SortOrder = "desc"
                });
                rejTask = _apiService.GetRejectedCandidatesAsync(cycle.CycleId);
            }
            else
            {
                shortlistTask = Task.FromResult<BaseResponse<List<SubsidyRecommendationDto>>>(null);
                recsTask = Task.FromResult<BaseResponse<PagedResult<SubsidyRecommendationDto>>>(null);
                rejTask = Task.FromResult<BaseResponse<List<SubsidyRecommendationDto>>>(null);
            }
            await Task.WhenAll(granteesTask, shortlistTask, recsTask, rejTask);
            var grResp = await granteesTask;
            var slResp = await shortlistTask;
            var pagedRecs = await recsTask;
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
            // Load paged recommendations directly from server-side paged result
            _allItems.Clear();
            FilteredRecommendations.Clear();
            PagedRecommendations.Clear();
            _queueCurrentPage = 1;

            if (pagedRecs?.Data != null && pagedRecs.Data.Items != null && pagedRecs.Data.Items.Count > 0)
            {
                QueueTotalPages = Math.Max(1, pagedRecs.Data.TotalPages);
                QueueTotalCount = pagedRecs.Data.TotalCount;
                for (int i = 0; i < pagedRecs.Data.Items.Count; i++)
                {
                    var item = SubsidyModelMapper.ToItem(pagedRecs.Data.Items[i]);
                    item.Rank = i + 1;
                    item.IsWithinQuota = (item.Rank <= _granteeSlots);
                    _allItems.Add(item);
                    FilteredRecommendations.Add(item);
                    PagedRecommendations.Add(item);
                }
            }
            else
            {
                QueueTotalPages = 1;
                QueueTotalCount = 0;
            }

            // Reset queue filter state on fresh data load
            _auditFilter = "All";
            _queueSearchText = string.Empty;
            OnPropertyChanged(nameof(AuditFilterLabel));
            OnPropertyChanged(nameof(QueueSearchText));
            OnPropertyChanged(nameof(QueueCurrentPage));
            OnPropertyChanged(nameof(QueueTotalPages));
            OnPropertyChanged(nameof(CanQueuePrevPage));
            OnPropertyChanged(nameof(CanQueueNextPage));
            OnPropertyChanged(nameof(QueuePageSummary));
            OnPropertyChanged(nameof(QueuePageNumberLabel));
            OnPropertyChanged(nameof(RecommendationsSummary));
            OnPropertyChanged(nameof(HasNoQueueResults));
            OnPropertyChanged(nameof(IsQueueFilterActive));
            OnPropertyChanged(nameof(QueueEmptyStateMessage));
            UpdateShortlistPaged();
            UpdateFinalPaged();
            RecalculateSelectionCounts();
            NotifyQuotaMetricsChanged();
            EnrichVisibleCandidatesAsync();
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

            int currentYear = ActiveCycleFiscalYear;
            int currentCycleNum = SelectedFiscalCycle?.CycleNumber ?? 1;

            int nextYear = currentCycleNum >= 2 ? currentYear + 1 : currentYear;
            int nextCycleNum = currentCycleNum >= 2 ? 1 : 2;

            string cycleDesc = nextCycleNum == 1 ? $"FY {nextYear} 1st Semester" : $"FY {nextYear} 2nd Semester";
            bool ok = ActionConfirmDialog.Show("Launch New Fiscal Cycle", $"Starting a new cycle will advance the active operational cycle to {cycleDesc}.\n\nThe current certified roster will be archived.\n\nDo you wish to proceed?", "Launch Cycle", "Cancel", ConfirmThemeType.Primary);
            if (!ok) return;
            var dlg = new NewCycleDialog(nextYear, nextCycleNum) { Owner = Application.Current?.MainWindow };
            bool? dlgResult;
            using (MainWindow.ModalScope(fullWindow: true))
            {
                dlgResult = dlg.ShowDialog();
            }
            if (dlgResult == true && dlg.ResultRequest != null)
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
                        ToastNotification.Show("New Cycle Active", $"Fiscal Year {dlg.ResultRequest.FiscalYear} (Semester {dlg.ResultRequest.CycleNumber ?? 1}) cycle started successfully. Fresh candidate recommendations generated.", ToastType.Success);
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

        public void ApplyShortlistAuditFilter(string filter) { _shortlistAuditFilter = filter; OnPropertyChanged(nameof(ShortlistAuditFilterLabel)); FilterShortlistRows(); }
        public void RecalculateSelectionCounts() { OnPropertyChanged(nameof(SelectedCountSummary)); OnPropertyChanged(nameof(SelectTopNLabel)); OnPropertyChanged(nameof(SelectAllQueueButtonText)); OnPropertyChanged(nameof(HasQueueSelection)); }

        public void ApplyAuditFilter(string filter) { _auditFilter = filter; OnPropertyChanged(nameof(AuditFilterLabel)); ApplyQueueFilter(); }

        public void ClearAuditFilter() { _auditFilter = "All"; OnPropertyChanged(nameof(AuditFilterLabel)); ApplyQueueFilter(); }

        /// <summary>
        /// Filters _allItems into FilteredRecommendations based on _auditFilter and QueueSearchText,
        /// re-ranks, resets to page 1, and updates pagination.
        /// </summary>
        public void ApplyQueueFilter()
        {
            IEnumerable<SubsidyItem> q = _allItems.OrderByDescending(x => x.Score);

            // Apply status/circumstance filter
            if (_auditFilter == "CollegeAge")
                q = q.Where(x => x.HasCollegeAgeDependent);
            else if (_auditFilter == "Clean")
                q = q.Where(x => !x.HasCollegeAgeDependent);
            // "All" → no filter

            // Apply search text
            if (!string.IsNullOrWhiteSpace(_queueSearchText))
            {
                string t = _queueSearchText.Trim().ToLowerInvariant();
                q = q.Where(x =>
                    (x.Name != null && x.Name.ToLowerInvariant().Contains(t)) ||
                    (x.SpId != null && x.SpId.ToLowerInvariant().Contains(t)) ||
                    (x.Barangay != null && x.Barangay.ToLowerInvariant().Contains(t)) ||
                    (x.Priority != null && x.Priority.ToLowerInvariant().Contains(t)));
            }

            FilteredRecommendations.Clear();
            int rank = 1;
            foreach (var item in q)
            {
                item.Rank = rank;
                item.IsWithinQuota = rank <= _granteeSlots;
                FilteredRecommendations.Add(item);
                rank++;
            }

            QueueTotalCount = FilteredRecommendations.Count;
            OnPropertyChanged(nameof(HasNoQueueResults));
            OnPropertyChanged(nameof(IsQueueFilterActive));
            OnPropertyChanged(nameof(QueueEmptyStateMessage));
            _queueCurrentPage = 1;
            UpdateQueuePaged();
            RecalculateSelectionCounts();
            NotifyQuotaMetricsChanged();
        }

        private void RecalculateQuotaAndFilter()
        {
            // Re-run filter to re-rank all items with the new slot count
            ApplyQueueFilter();
            // Update paged recommendations within-quota flags
            if (PagedRecommendations != null)
            {
                foreach (var item in PagedRecommendations)
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
            var sorted = ShortlistRows.OrderBy(x => x.IsFinalGrantee).ThenByDescending(x => x.Score).ToList();
            ShortlistRows.Clear();
            for (int i = 0; i < sorted.Count; i++) { sorted[i].Rank = i + 1; sorted[i].IsWaitlisted = !sorted[i].IsFinalGrantee && (i >= _granteeSlots); ShortlistRows.Add(sorted[i]); }
            FinalGranteesList.Clear(); foreach (var r in ShortlistRows.Where(x => x.IsFinalGrantee).OrderBy(x => x.Name)) FinalGranteesList.Add(r);
            UpdateFinalPaged();
            if (enrich) EnrichAllCandidatesFromRecordsAsync();
            FilterShortlistRows(); OnPropertyChanged(nameof(Tab2QuotaSummary)); OnPropertyChanged(nameof(ShortlistSummary)); OnPropertyChanged(nameof(FinalSummaryText));
        }

        public void EnrichVisibleCandidatesAsync()
        {
            var targets = PagedRecommendations
                .Select(x => x.SoloParentId)
                .Concat(PagedShortlistRows.Select(x => x.SoloParentId))
                .Concat(FinalGranteesList.Select(x => x.SoloParentId))
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
                                    foreach (var row in PagedShortlistRows.Where(x => x.SoloParentId == id)) SubsidyModelMapper.EnrichRowFromRecord(row, resp.Data);
                                    foreach (var row in ShortlistRows.Where(x => x.SoloParentId == id)) SubsidyModelMapper.EnrichRowFromRecord(row, resp.Data);
                                    foreach (var row in FinalGranteesList.Where(x => x.SoloParentId == id)) SubsidyModelMapper.EnrichRowFromRecord(row, resp.Data);
                                    foreach (var item in PagedRecommendations.Where(x => x.SoloParentId == id)) SubsidyModelMapper.EnrichItemFromRecord(item, resp.Data);
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

        public void EnrichAllCandidatesFromRecordsAsync() => EnrichVisibleCandidatesAsync();

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
            q = q.OrderBy(x => x.IsFinalGrantee).ThenByDescending(x => x.Score);
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

        /// <summary>
        /// Open Edit Cycle Dialog - triggered from menu
        /// </summary>
        private async void OpenEditCycleDialog()
        {
            try
            {
                if (SelectedFiscalCycle == null)
                {
                    ToastNotification.Show("No Cycle Selected", "Please select a fiscal cycle first.", ToastType.Warning);
                    return;
                }

                // Create a DTO from the selected fiscal cycle
                var cycleDto = new SubsidyCycleDto
                {
                    Id = SelectedFiscalCycle.CycleId ?? Guid.Empty,
                    FiscalYear = SelectedFiscalCycle.FiscalYear,
                    CycleNumber = SelectedFiscalCycle.CycleNumber,
                    Name = SelectedFiscalCycle.Name,
                    AllocatedSlots = SelectedFiscalCycle.AllocatedSlots,
                    PerGranteeAmount = SelectedFiscalCycle.PerGranteeAmount,
                    TotalBudget = SelectedFiscalCycle.TotalBudget,
                    Status = SelectedFiscalCycle.Status
                };

                // Show the edit cycle dialog
                var dialog = new EditCycleDialog(cycleDto)
                {
                    Owner = Application.Current?.MainWindow
                };

                bool? dialogResult;
                using (MainWindow.ModalScope(fullWindow: true))
                {
                    dialogResult = dialog.ShowDialog();
                }

                if (dialogResult == true && dialog.IsSaved)
                {
                    // Save the changes - the cycleDto was updated by reference in the dialog
                    await SaveCycleSettingsAsync(cycleDto);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show("Error", $"Failed to open cycle editor: {ex.Message}", ToastType.Error);
            }
        }

        /// <summary>
        /// Save Cycle Settings - calls the API to update cycle data
        /// </summary>
        private async Task SaveCycleSettingsAsync(SubsidyCycleDto updatedCycle)
        {
            try
            {
                if (updatedCycle.Id == Guid.Empty)
                {
                    ToastNotification.Show("Invalid Cycle", "Cannot update cycle: Invalid cycle ID.", ToastType.Error);
                    System.Diagnostics.Debug.WriteLine("[SaveCycleSettingsAsync] Invalid cycle ID (Guid.Empty)");
                    return;
                }

                // Prepare the update request
                var updateRequest = new UpdateSubsidyCycleRequest
                {
                    AllocatedSlots = updatedCycle.AllocatedSlots,
                    PerGranteeAmount = updatedCycle.PerGranteeAmount,
                    TotalBudget = updatedCycle.TotalBudget,
                    FiscalYear = updatedCycle.FiscalYear,
                    CycleNumber = updatedCycle.CycleNumber,
                    Name = updatedCycle.Name
                };

                // Log the request details for debugging
                System.Diagnostics.Debug.WriteLine($"[SaveCycleSettingsAsync] Updating cycle {updatedCycle.Id}");
                System.Diagnostics.Debug.WriteLine($"[SaveCycleSettingsAsync] AllocatedSlots: {updateRequest.AllocatedSlots}");
                System.Diagnostics.Debug.WriteLine($"[SaveCycleSettingsAsync] PerGranteeAmount: {updateRequest.PerGranteeAmount}");
                System.Diagnostics.Debug.WriteLine($"[SaveCycleSettingsAsync] TotalBudget: {updateRequest.TotalBudget}");

                // Call the API
                var response = await _apiService.UpdateCycleAsync(updatedCycle.Id, updateRequest);

                // Log the response
                System.Diagnostics.Debug.WriteLine($"[SaveCycleSettingsAsync] Response received. Succeeded: {response?.Succeeded}");
                if (response?.Errors != null && response.Errors.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[SaveCycleSettingsAsync] Errors: {string.Join(", ", response.Errors)}");
                }

                if (response != null && response.Succeeded)
                {
                    // Update the local fiscal cycle option
                    if (SelectedFiscalCycle != null)
                    {
                        SelectedFiscalCycle.AllocatedSlots = updatedCycle.AllocatedSlots;
                        SelectedFiscalCycle.PerGranteeAmount = updatedCycle.PerGranteeAmount;
                        SelectedFiscalCycle.TotalBudget = updatedCycle.TotalBudget;
                    }

                    // Update the ViewModel properties
                    GranteeSlots = updatedCycle.AllocatedSlots;
                    GrantAmountPerRecipient = (int)updatedCycle.PerGranteeAmount;

                    // Refresh the UI
                    NotifyCycleProperties();
                    NotifyQuotaMetricsChanged();
                    RecalculateQuotaAndFilter();

                    ToastNotification.Show("Settings Updated", 
                        $"Cycle settings for FY {updatedCycle.FiscalYear} updated successfully.\nSlots: {updatedCycle.AllocatedSlots:N0} | Amount: ₱{updatedCycle.PerGranteeAmount:N2} | Total: ₱{updatedCycle.TotalBudget:N2}", 
                        ToastType.Success);
                }
                else
                {
                    string errorMsg = response?.Errors != null && response.Errors.Count > 0
                        ? string.Join(", ", response.Errors)
                        : "Failed to update cycle settings. No error details provided.";
                    
                    System.Diagnostics.Debug.WriteLine($"[SaveCycleSettingsAsync] Update failed: {errorMsg}");
                    ToastNotification.Show("Update Failed", errorMsg, ToastType.Error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveCycleSettingsAsync] Exception: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[SaveCycleSettingsAsync] StackTrace: {ex.StackTrace}");
                
                ToastNotification.Show("Error", $"Failed to save cycle settings: {ex.Message}", ToastType.Error);
            }
        }

        /// <summary>
        /// Open View Details Dialog - triggered from menu to show cycle metadata
        /// </summary>
        private void OpenViewDetailsDialog()
        {
            try
            {
                if (SelectedFiscalCycle == null)
                {
                    ToastNotification.Show("No Cycle Selected", "Please select a fiscal cycle first.", ToastType.Warning);
                    return;
                }

                // Show view details modal/dialog with cycle metadata
                // This would integrate with a dialog service or modal component
                ToastNotification.Show("Cycle Details", $"View details for FY {SelectedFiscalCycle.FiscalYear}: Status={SelectedFiscalCycle.Status}, Allocated Slots={SelectedFiscalCycle.AllocatedSlots}.", ToastType.Info);
            }
            catch (Exception ex)
            {
                ToastNotification.Show("Error", $"Failed to open cycle details: {ex.Message}", ToastType.Error);
            }
        }
    }
}
