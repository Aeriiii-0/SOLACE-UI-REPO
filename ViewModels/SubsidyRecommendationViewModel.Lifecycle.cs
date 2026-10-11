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
                if (FinalGranteesList.Count == 0)
                {
                    ToastNotification.Show("No Grantees", "Please confirm at least one grantee before locking the roster.", ToastType.Warning);
                    return;
                }
                bool ok = ActionConfirmDialog.Show(
                    "Finalize & Lock Roster",
                    $"Are you sure you want to finalize and lock the FY {ActiveCycleFiscalYear} Grantee Roster ({FinalGranteesList.Count} confirmed recipients)?\n\nOnce locked, this roster is certified for official disbursement and candidates cannot be revoked without unlocking.",
                    "Lock Roster", "Cancel", ConfirmThemeType.Primary);
                if (ok)
                {
                    IsRosterLocked = true;
                    ToastNotification.Show("Roster Finalized", $"FY {ActiveCycleFiscalYear} roster locked for official disbursement.", ToastType.Success);
                }
            }
            else
            {
                bool ok = ActionConfirmDialog.Show(
                    "Unlock Roster for Amendments",
                    $"Are you sure you want to unlock the FY {ActiveCycleFiscalYear} roster?\n\nThis will re-enable candidate confirmation and revocation for administrative amendments.",
                    "Unlock Roster", "Cancel", ConfirmThemeType.Warning);
                if (ok)
                {
                    IsRosterLocked = false;
                    ToastNotification.Show("Roster Unlocked", "You can now make amendments to the final grantees.", ToastType.Info);
                }
            }
        }

        private bool _isLoadingCycle;
        public async Task LoadCycleDataAsync(FiscalCycleOption cycle)
        {
            if (cycle == null || _isLoadingCycle) return;
            _isLoadingCycle = true;
            try
            {
                if (cycle.IsActive)
                {
                    _activeCycleId = cycle.CycleId;
                    _activeCycleFiscalYear = cycle.FiscalYear;
                    IsRosterLocked = string.Equals(cycle.Status, "Finalized", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    IsRosterLocked = true;
                }

                GranteeSlots = cycle.AllocatedSlots > 0 ? cycle.AllocatedSlots : 100;

                var grResp = await _apiService.GetGranteesByFiscalYearAsync(cycle.FiscalYear);
                FinalGranteesList.Clear();
                int rk = 1;
                if (grResp?.Data != null && grResp.Data.Count > 0)
                {
                    foreach (var g in grResp.Data.OrderBy(x => x.FullName))
                        FinalGranteesList.Add(SubsidyModelMapper.GranteeToRow(g, rk++));
                }

                ShortlistRows.Clear();
                foreach (var g in FinalGranteesList)
                {
                    ShortlistRows.Add(g);
                }

                if (cycle.CycleId.HasValue && cycle.CycleId.Value != Guid.Empty)
                {
                    var slResp = await _apiService.GetShortlistAsync(cycle.CycleId.Value);
                    if (slResp?.Data != null)
                    {
                        foreach (var d in slResp.Data)
                        {
                            bool inShortlist = ShortlistRows.Any(x => (!string.IsNullOrEmpty(d.SpId) && x.SpId == d.SpId)
                                                                   || (d.EvaluationId != Guid.Empty && x.EvaluationId == d.EvaluationId)
                                                                   || (d.SoloParentId != Guid.Empty && x.SoloParentId == d.SoloParentId)
                                                                   || (!string.IsNullOrEmpty(d.Name) && string.Equals(x.Name, d.Name, StringComparison.OrdinalIgnoreCase)));
                            if (!inShortlist)
                                ShortlistRows.Add(SubsidyModelMapper.ToRow(d, rk++));
                        }
                    }
                }
                UpdateShortlistAndFinalCollections();

                _allItems.Clear();
                if (cycle.CycleId.HasValue && cycle.CycleId.Value != Guid.Empty)
                {
                    var dtos = await _apiService.GetRecommendationsAsync(cycle.CycleId.Value);
                    if (dtos?.Data != null)
                    {
                        foreach (var d in dtos.Data)
                        {
                            bool inAudit = ShortlistRows.Any(s => (!string.IsNullOrEmpty(d.SpId) && s.SpId == d.SpId)
                                                               || (d.EvaluationId != Guid.Empty && s.EvaluationId == d.EvaluationId)
                                                               || (d.SoloParentId != Guid.Empty && s.SoloParentId == d.SoloParentId)
                                                               || (!string.IsNullOrEmpty(d.Name) && string.Equals(s.Name, d.Name, StringComparison.OrdinalIgnoreCase)));
                            bool alreadyInAllItems = _allItems.Any(x => (d.EvaluationId != Guid.Empty && x.EvaluationId == d.EvaluationId)
                                                                     || (d.SoloParentId != Guid.Empty && x.SoloParentId == d.SoloParentId)
                                                                     || (!string.IsNullOrEmpty(d.SpId) && x.SpId == d.SpId)
                                                                     || (!string.IsNullOrEmpty(d.Name) && string.Equals(x.Name, d.Name, StringComparison.OrdinalIgnoreCase)));
                            if (!inAudit && !alreadyInAllItems) _allItems.Add(SubsidyModelMapper.ToItem(d));
                        }
                    }
                }
                RecalculateQuotaAndFilter();
                NotifyQuotaMetricsChanged();
                EnrichAllCandidatesFromRecordsAsync();
            }
            catch (Exception ex)
            {
                ToastNotification.Show("Load Error", ex.Message, ToastType.Error);
            }
            finally { _isLoadingCycle = false; }
        }

        public async Task StartNewCycleAsync()
        {
            int nextYear = ActiveCycleFiscalYear + 1;
            var dlg = new NewCycleDialog(nextYear) { Owner = Application.Current?.MainWindow };
            if (dlg.ShowDialog() == true && dlg.ResultRequest != null)
            {
                try
                {
                    var resp = await _apiService.CreateCycleAsync(dlg.ResultRequest);
                    if (resp != null && resp.Succeeded)
                    {
                        IsRosterLocked = false;
                        await RefreshDataAsync();
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

        public void ApplyAuditFilter(string filter) { _auditFilter = filter; OnPropertyChanged(nameof(AuditFilterLabel)); RecalculateQuotaAndFilter(); }
        public void ApplyShortlistAuditFilter(string filter) { _shortlistAuditFilter = filter; OnPropertyChanged(nameof(ShortlistAuditFilterLabel)); FilterShortlistRows(); }
        public void RecalculateSelectionCounts() { OnPropertyChanged(nameof(SelectedCountSummary)); OnPropertyChanged(nameof(SelectTopNLabel)); }

        private void RecalculateQuotaAndFilter()
        {
            IEnumerable<SubsidyItem> q = _allItems.Where(x => !x.IsDisqualified && !ShortlistRows.Any(s => (!string.IsNullOrEmpty(s.SpId) && s.SpId == x.SpId) || (x.EvaluationId != Guid.Empty && s.EvaluationId == x.EvaluationId) || (x.SoloParentId != Guid.Empty && s.SoloParentId == x.SoloParentId) || (!string.IsNullOrEmpty(x.Name) && string.Equals(s.Name, x.Name, StringComparison.OrdinalIgnoreCase))));
            if (_auditFilter == "Clean") q = q.Where(x => !x.HasCollegeAgeDependent);
            else if (_auditFilter == "CollegeAge") q = q.Where(x => x.HasCollegeAgeDependent);
            var list = q.GroupBy(x => x.SoloParentId != Guid.Empty ? x.SoloParentId.ToString() : (x.EvaluationId != Guid.Empty ? x.EvaluationId.ToString() : (x.SpId ?? x.Name)))
                        .Select(g => g.First())
                        .OrderByDescending(x => x.Score)
                        .ToList();
            FilteredRecommendations.Clear();
            for (int i = 0; i < list.Count; i++) { list[i].Rank = i + 1; list[i].IsWithinQuota = (i < _granteeSlots); FilteredRecommendations.Add(list[i]); }
            UpdateQueuePaged();
            OnPropertyChanged(nameof(GranteeSlotsText)); OnPropertyChanged(nameof(SelectTopNLabel)); OnPropertyChanged(nameof(SelectedCountSummary));
        }

        private void UpdateShortlistAndFinalCollections()
        {
            var sorted = ShortlistRows.OrderBy(x => x.IsFinalGrantee).ThenByDescending(x => x.Score).ToList();
            ShortlistRows.Clear();
            for (int i = 0; i < sorted.Count; i++) { sorted[i].Rank = i + 1; sorted[i].IsWaitlisted = !sorted[i].IsFinalGrantee && (i >= _granteeSlots); ShortlistRows.Add(sorted[i]); }
            FinalGranteesList.Clear(); foreach (var r in ShortlistRows.Where(x => x.IsFinalGrantee).OrderBy(x => x.Name)) FinalGranteesList.Add(r);
            UpdateFinalPaged();
            EnrichAllCandidatesFromRecordsAsync();
            FilterShortlistRows(); OnPropertyChanged(nameof(Tab2QuotaSummary)); OnPropertyChanged(nameof(ShortlistSummary)); OnPropertyChanged(nameof(FinalSummaryText));
        }

        public void EnrichAllCandidatesFromRecordsAsync()
        {
            var targets = _allItems.Select(x => x.SoloParentId)
                .Concat(ShortlistRows.Select(x => x.SoloParentId))
                .Concat(FinalGranteesList.Select(x => x.SoloParentId))
                .Where(id => id != Guid.Empty).Distinct().ToList();
            if (targets.Count == 0) return;
            Task.Run(async () =>
            {
                var tasks = targets.Select(async id =>
                {
                    try
                    {
                        var resp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(id);
                        if (resp?.Data != null)
                        {
                            Application.Current?.Dispatcher?.Invoke(() =>
                            {
                                foreach (var item in _allItems.Where(x => x.SoloParentId == id)) SubsidyModelMapper.EnrichItemFromRecord(item, resp.Data);
                                foreach (var row in ShortlistRows.Where(x => x.SoloParentId == id)) SubsidyModelMapper.EnrichRowFromRecord(row, resp.Data);
                                foreach (var row in FinalGranteesList.Where(x => x.SoloParentId == id)) SubsidyModelMapper.EnrichRowFromRecord(row, resp.Data);
                                if (ActiveCandidate?.SoloParentId == id) OnPropertyChanged(nameof(ActiveCandidate));
                            });
                        }
                    }
                    catch { }
                });
                await Task.WhenAll(tasks);
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
