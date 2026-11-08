using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using SOLUM_UI.DTOs;
using SOLUM_UI.Models;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.Views.Dialogs;

namespace SOLUM_UI.ViewModels
{
    public partial class SubsidyRecommendationViewModel
    {
        private void PromptRevokeShortlist(SelectionRow r)
        {
            if (r == null) return;
            _shortlistRevokeTargets = new List<SelectionRow> { r };
            OutlierTarget = _allItems.FirstOrDefault(x => x.SpId == r.SpId);
            OutlierModalTitle = "Revoke Candidate";
            OutlierTargetNameAndId = $"Candidate: {r.Name}";
            OnPropertyChanged(nameof(OutlierModalConfirmButtonText));
            IsOutlierModalOpen = true;
        }

        private async Task ConfirmOutlierRejectionAsync()
        {
            if (_shortlistRevokeTargets != null && _shortlistRevokeTargets.Count > 0)
            {
                int count = _shortlistRevokeTargets.Count;
                foreach (var r in _shortlistRevokeTargets)
                {
                    ShortlistRows.Remove(r);
                    FinalGranteesList.Remove(r);
                    var orig = _allItems.FirstOrDefault(x => x.SpId == r.SpId);
                    if (orig != null) { orig.IsDisqualified = true; orig.DisqualificationReason = OutlierRejectionReason; orig.Status = "Rejected"; }
                    if (r.EvaluationId != Guid.Empty)
                        _ = _apiService.RejectOutlierAsync(new RejectOutlierRequest { EvaluationId = r.EvaluationId, RejectionReason = OutlierRejectionReason, RejectionNotes = OutlierNotes, AutoPromote = false });
                    AddDisqualifiedCandidateRecord(
                        r.EvaluationId,
                        r.SoloParentId,
                        r.SpId,
                        r.Name,
                        r.Barangay,
                        r.MonthlyIncome,
                        r.Score,
                        r.Priority,
                        r.MinorDependentsCount,
                        OutlierRejectionReason,
                        OutlierNotes,
                        "Admin / Reviewer"
                    );
                }
                _shortlistRevokeTargets = null;
                IsOutlierModalOpen = false;
                UpdateShortlistAndFinalCollections();
                RecalculateQuotaAndFilter();
                IsShortlistBatchMode = false;
                RecalculateShortlistSelectionCounts();
                NotifyDisqualifiedMetricsChanged();
                ToastNotification.Show("Revocation Complete", $"{count} candidate(s) revoked ({OutlierRejectionReason}).", ToastType.Warning);
            }
            else if (_batchRejectTargets != null && _batchRejectTargets.Count > 0)
            {
                int count = _batchRejectTargets.Count;
                foreach (var target in _batchRejectTargets)
                {
                    target.IsDisqualified = true;
                    target.DisqualificationReason = OutlierRejectionReason;
                    target.Status = "Rejected";
                    if (target.EvaluationId != Guid.Empty)
                        _ = _apiService.RejectOutlierAsync(new RejectOutlierRequest { EvaluationId = target.EvaluationId, RejectionReason = OutlierRejectionReason, RejectionNotes = OutlierNotes, AutoPromote = false });
                    AddDisqualifiedCandidateRecord(
                        target.EvaluationId,
                        target.SoloParentId,
                        target.SpId,
                        target.Name,
                        target.Barangay,
                        target.MonthlyIncome,
                        target.Score,
                        target.Priority,
                        target.Dependants,
                        OutlierRejectionReason,
                        OutlierNotes,
                        "Admin / Reviewer"
                    );
                    _allItems.Remove(target);
                    PagedRecommendations.Remove(target);
                    FilteredRecommendations.Remove(target);
                }
                _batchRejectTargets = null;
                IsOutlierModalOpen = false;
                IsBatchMode = false;
                RecalculateQuotaAndFilter();
                RecalculateSelectionCounts();
                NotifyDisqualifiedMetricsChanged();
                ToastNotification.Show("Candidates Disqualified", $"{count} candidate(s) disqualified from queue ({OutlierRejectionReason}).", ToastType.Warning);
            }
            else if (OutlierTarget != null)
            {
                var target = OutlierTarget;
                target.IsDisqualified = true;
                target.DisqualificationReason = OutlierRejectionReason;
                target.Status = "Rejected";
                if (target.EvaluationId != Guid.Empty)
                    await _apiService.RejectOutlierAsync(new RejectOutlierRequest { EvaluationId = target.EvaluationId, RejectionReason = OutlierRejectionReason, RejectionNotes = OutlierNotes, AutoPromote = false });
                AddDisqualifiedCandidateRecord(
                    target.EvaluationId,
                    target.SoloParentId,
                    target.SpId,
                    target.Name,
                    target.Barangay,
                    target.MonthlyIncome,
                    target.Score,
                    target.Priority,
                    target.Dependants,
                    OutlierRejectionReason,
                    OutlierNotes,
                    "Admin / Reviewer"
                );
                PagedRecommendations.Remove(target);
                FilteredRecommendations.Remove(target);
                if (QueueTotalCount > 0) QueueTotalCount--;
                IsOutlierModalOpen = false;
                RecalculateQuotaAndFilter();
                NotifyDisqualifiedMetricsChanged();
                ToastNotification.Show("Candidate Disqualified", $"{target.Name} disqualified from queue ({OutlierRejectionReason}).", ToastType.Warning);
            }
        }

        public void AddDisqualifiedCandidateRecord(
            Guid evalId, Guid spId, string spNum, string name, string bar,
            double income, double score, string pri, int dep,
            string reason, string notes, string by)
        {
            var existing = DisqualifiedCandidates.FirstOrDefault(x => (!string.IsNullOrEmpty(spNum) && x.SpId == spNum) || (evalId != Guid.Empty && x.EvaluationId == evalId));
            if (existing != null)
            {
                existing.RejectionReason = reason;
                existing.RejectionNotes = notes;
                existing.DisqualifiedBy = by;
                existing.DisqualifiedAt = DateTime.Now;
            }
            else
            {
                var item = new DisqualifiedCandidateItem
                {
                    EvaluationId = evalId,
                    SoloParentId = spId,
                    SpId = spNum,
                    FullName = name,
                    Barangay = bar,
                    MonthlyIncome = income,
                    PriorityScore = score,
                    Priority = pri ?? "Medium",
                    Dependents = dep,
                    RejectionReason = reason,
                    RejectionNotes = notes,
                    DisqualifiedBy = by,
                    DisqualifiedAt = DateTime.Now
                };
                DisqualifiedCandidates.Insert(0, item);
            }
            FilterDisqualifiedCandidates();
        }

        public void FilterDisqualifiedCandidates()
        {
            if (DisqualifiedCandidates == null || FilteredDisqualifiedCandidates == null) return;
            FilteredDisqualifiedCandidates.Clear();
            foreach (var item in DisqualifiedCandidates)
            {
                FilteredDisqualifiedCandidates.Add(item);
            }
            OnPropertyChanged(nameof(HasFilteredDisqualifiedCandidates));
            OnPropertyChanged(nameof(HasNoDisqualifiedCandidates));
            UpdateDisqualifiedPaged();
        }

        public void UpdateDisqualifiedPaged()
        {
            if (FilteredDisqualifiedCandidates == null || PagedDisqualifiedCandidates == null) return;

            int total = FilteredDisqualifiedCandidates.Count;
            int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)DisqualifiedPageSize));
            _disqualifiedTotalPages = totalPages;
            if (_disqualifiedCurrentPage > totalPages) _disqualifiedCurrentPage = totalPages;
            if (_disqualifiedCurrentPage < 1) _disqualifiedCurrentPage = 1;
            PagedDisqualifiedCandidates.Clear();
            var pageSlice = FilteredDisqualifiedCandidates
                .Skip((_disqualifiedCurrentPage - 1) * DisqualifiedPageSize)
                .Take(DisqualifiedPageSize);
            foreach (var item in pageSlice)
            {
                PagedDisqualifiedCandidates.Add(item);
            }
            OnPropertyChanged(nameof(DisqualifiedCurrentPage));
            OnPropertyChanged(nameof(DisqualifiedTotalPages));
            OnPropertyChanged(nameof(CanDisqualifiedPrevPage));
            OnPropertyChanged(nameof(CanDisqualifiedNextPage));
            OnPropertyChanged(nameof(DisqualifiedPageNumberLabel));
            OnPropertyChanged(nameof(DisqualifiedPageSummary));
        }

        public async Task ReinstateCandidateAsync(DisqualifiedCandidateItem item)
        {
            if (item == null) return;

            bool ok = ActionConfirmDialog.Show(
                "Reinstate Candidate",
                $"Are you sure you want to reinstate {item.FullName} ({item.SpId})?\n\nThis will remove their statutory disqualification and restore them to the active recommendations queue.",
                "Reinstate Candidate",
                "Cancel",
                ConfirmThemeType.Primary);
            if (!ok) return;
            try
            {
                if (item.EvaluationId != Guid.Empty)
                {
                    _ = _apiService.ReinstateCandidateAsync(item.EvaluationId);
                }
                DisqualifiedCandidates.Remove(item);
                FilteredDisqualifiedCandidates.Remove(item);
                UpdateDisqualifiedPaged();
                NotifyDisqualifiedMetricsChanged();
                var orig = _allItems.FirstOrDefault(x => (!string.IsNullOrEmpty(item.SpId) && x.SpId == item.SpId) || (item.EvaluationId != Guid.Empty && x.EvaluationId == item.EvaluationId));
                if (orig != null)
                {
                    orig.IsDisqualified = false;
                    orig.DisqualificationReason = null;
                    orig.Status = "Pending";
                    if (!PagedRecommendations.Contains(orig))
                        PagedRecommendations.Add(orig);
                    if (!FilteredRecommendations.Contains(orig))
                        FilteredRecommendations.Add(orig);
                }
                else
                {
                    var newItem = new SubsidyItem
                    {
                        EvaluationId = item.EvaluationId,
                        SoloParentId = item.SoloParentId,
                        SpId = item.SpId,
                        Name = item.FullName,
                        Barangay = item.Barangay,
                        MonthlyIncome = item.MonthlyIncome,
                        Score = item.PriorityScore,
                        Priority = item.Priority,
                        Dependants = item.Dependents,
                        Status = "Pending",
                        IsDisqualified = false
                    };
                    _allItems.Add(newItem);
                    PagedRecommendations.Add(newItem);
                    FilteredRecommendations.Add(newItem);
                }
                QueueTotalCount++;
                RecalculateQuotaAndFilter();
                ToastNotification.Show("Candidate Reinstated", $"{item.FullName} has been restored to the recommendations queue.", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastNotification.Show("Reinstatement Error", ex.Message, ToastType.Error);
            }
        }

        public async Task ExportDisqualifiedCsvAsync()
        {
            var itemsToExport = FilteredDisqualifiedCandidates.ToList();
            if (itemsToExport.Count == 0)
            {
                ToastNotification.Show("No Records", "No disqualified candidate records match the current filter.", ToastType.Info);
                return;
            }
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Disqualified_Candidates_Audit_FY{_activeCycleFiscalYear}_{DateTime.Now:yyyyMMdd}.csv",
                DefaultExt = ".csv",
                Filter = "CSV file (*.csv)|*.csv"
            };
            if (dlg.ShowDialog() == true)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Solo Parent ID,Full Name,Barangay,Monthly Income,Vulnerability Score,Priority Level,Dependents,Disqualification Reason,Audit Justification Notes,Disqualified By,Timestamp");
                foreach (var c in itemsToExport)
                {
                    string cleanNotes = (c.RejectionNotes ?? string.Empty).Replace("\"", "\"\"");
                    sb.AppendLine($"\"{c.SpId}\",\"{c.FullName}\",\"{c.Barangay}\",\"{c.MonthlyIncome:F2}\",\"{c.PriorityScore:F2}\",\"{c.Priority}\",\"{c.Dependents}\",\"{c.RejectionReason}\",\"{cleanNotes}\",\"{c.DisqualifiedBy}\",\"{c.FormattedDisqualifiedAt}\"");
                }
                System.IO.File.WriteAllText(dlg.FileName, sb.ToString(), new System.Text.UTF8Encoding(true));
                ToastNotification.Show("Export Complete", $"Exported {itemsToExport.Count} disqualified candidate record(s) to CSV.", ToastType.Success);
            }
        }

        private async void ToggleGrantee(SelectionRow row, bool confirm)
        {
            if (row == null) return;
            if (confirm)
            {
                if (row.IsCardExpired)
                {
                    ToastNotification.Show(
                        "Card Expired",
                        $"Cannot confirm {row.Name} to Final Grantees because their Solo Parent ID expired on {row.CardValidityDisplay}. Please renew their card first.",
                        ToastType.Error);
                    return;
                }

                bool ok = ActionConfirmDialog.Show(
                    "Confirm Final Grantee",
                    $"Confirm {row.Name} as an official Final Grantee for FY {ActiveCycleFiscalYear}?",
                    "Confirm Grantee",
                    "Cancel",
                    ConfirmThemeType.Success);
                if (!ok) return;
            }
            else
            {
                bool ok = ActionConfirmDialog.Show(
                    "Revert Grantee",
                    $"Revert {row.Name} back to pending selection audit?",
                    "Revert",
                    "Cancel",
                    ConfirmThemeType.Warning);
                if (!ok) return;
            }
            row.IsFinalGrantee = confirm;
            if (confirm && row.EvaluationId != Guid.Empty)
                await _apiService.ConfirmGranteesAsync(_activeCycleId, new List<Guid> { row.EvaluationId });
            UpdateShortlistAndFinalCollections();
            NotifyQuotaMetricsChanged();
            ToastNotification.Show(confirm ? "Confirmed" : "Unconfirmed", $"{row.Name} {(confirm ? "confirmed as final grantee." : "reverted to pending shortlist.")}", confirm ? ToastType.Success : ToastType.Info);
        }

        private async Task ExportCsvAsync(string name, Task<string> generator, string msg)
        {
            var dlg = new SaveFileDialog { FileName = name, DefaultExt = ".csv", Filter = "CSV file (*.csv)|*.csv" };
            if (dlg.ShowDialog() == true)
            {
                File.WriteAllText(dlg.FileName, await generator, new System.Text.UTF8Encoding(true));
                ToastNotification.Show("Export Complete", msg, ToastType.Success);
            }
        }

        public async Task ExportRecommendationsQueueAsync()
        {
            IsLoading = true;
            try
            {
                var resp = await _apiService.GetAllRecommendationsAsync(new GetSubsidyRecommendationsRequest { CycleId = _activeCycleId });
                if (resp?.Data == null || resp.Data.Count == 0)
                {
                    ToastNotification.Show("No Records", "No recommendations found to export.", ToastType.Info);
                    return;
                }
                var items = resp.Data.Select((d, idx) =>
                {
                    var item = SubsidyModelMapper.ToItem(d);
                    item.Rank = idx + 1;
                    return item;
                }).ToList();

                int yr = SelectedFiscalCycle?.FiscalYear ?? ActiveCycleFiscalYear;
                await ExportCsvAsync($"Recommendations_Queue_FY{yr}_{DateTime.Now:yyyyMMdd}", _apiService.ExportAllQueueCsvAsync(items), $"{items.Count} candidate recommendations exported successfully.");
            }
            catch (Exception ex)
            {
                ToastNotification.Show("Export Error", ex.Message, ToastType.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OpenFullRecordViewAsync(SubsidyItem item)
        {
            if (item == null) return;
            try
            {
                SoloParentRecord recordToView = null;
                Guid targetId = item.SoloParentId != Guid.Empty ? item.SoloParentId : (Guid.TryParse(item.SpId?.Replace("SP-", ""), out var parsed) ? parsed : Guid.Empty);
                if (targetId != Guid.Empty)
                {
                    var detailResp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(targetId);
                    if (detailResp != null && detailResp.Succeeded && detailResp.Data != null)
                        recordToView = SoloParentApiService.Instance.MapToRecord(detailResp.Data);
                }
                if (recordToView == null)
                    recordToView = new SoloParentRecord { Id = item.SpId, Name = item.Name, Barangay = item.Barangay, Sex = item.Sex, CivilStatus = item.CivilStatus, MonthlyIncome = item.MonthlyIncome.ToString("N2"), Status = "Active" };
                var dlg = new RecordViewDialog(recordToView) { Owner = Application.Current.MainWindow };
                dlg.ShowDialog();
                if (dlg.Renewed || dlg.OpenedEdit || dlg.RenewalResult != null)
                {
                    await RunQuickPredictAsync(item);
                }
            }
            catch (Exception ex) { ToastNotification.Show("Unable to Open Record", ex.Message, ToastType.Error); }
        }

        private async Task RunQuickPredictAsync(SubsidyItem item)
        {
            if (item == null) return;
            Guid targetId = item.SoloParentId != Guid.Empty ? item.SoloParentId : (Guid.TryParse(item.SpId?.Replace("SP-", ""), out var parsed) ? parsed : Guid.Empty);
            if (targetId == Guid.Empty)
            {
                ToastNotification.Show("Re-scoring Error", "Cannot determine applicant ID.", ToastType.Error);
                return;
            }
            if (_reassessmentCooldowns.TryGetValue(targetId, out var lastTime))
            {
                var elapsed = DateTime.UtcNow - lastTime;
                var cd = ReassessmentCooldown;
                if (elapsed < cd)
                {
                    int rem = (int)Math.Ceiling((cd - elapsed).TotalSeconds);
                    ToastNotification.Show("Re-assessment Cooldown", $"Please wait {rem}s before re-assessing {item.Name} again.", ToastType.Warning);
                    return;
                }
            }
            try
            {
                var resp = await _apiService.PredictCandidateAsync(targetId);
                if (resp != null && resp.Succeeded && resp.Data != null)
                {
                    SubsidyModelMapper.UpdateItemFromDto(item, resp.Data);
                    item.RefreshVisuals();
                    // Re-enrich with details like children items
                    await FetchRecordForDrawerAsync(item);
                    if (ActiveCandidate == item)
                    {
                        OnPropertyChanged(nameof(ActiveCandidate));
                    }
                    // Record cooldown timestamp
                    RecordReassessment(targetId);
                    // Recalculate quota & sort to update queue order and badges
                    RecalculateQuotaAndFilter();
                    ToastNotification.Show("Prediction Updated",
                        $"{item.Name} re-scored: {item.Score:F1} ({item.Priority}) via {resp.Data.ModelVersion}.",
                        ToastType.Success);
                }
                else
                {
                    string err = resp?.Errors != null && resp.Errors.Count > 0 ? string.Join(", ", resp.Errors) : "Prediction could not be computed.";
                    ToastNotification.Show("Re-scoring Failed", err, ToastType.Error);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show("Re-scoring Error", ex.Message, ToastType.Error);
            }
        }

        private async Task OpenReassessmentFormAsync(SubsidyItem item)
        {
            if (item == null) return;
            Guid targetId = item.SoloParentId != Guid.Empty ? item.SoloParentId : (Guid.TryParse(item.SpId?.Replace("SP-", ""), out var parsed) ? parsed : Guid.Empty);
            if (targetId == Guid.Empty) return;
            if (_reassessmentCooldowns.TryGetValue(targetId, out var lastTime))
            {
                var elapsed = DateTime.UtcNow - lastTime;
                var cd = ReassessmentCooldown;
                if (elapsed < cd)
                {
                    int rem = (int)Math.Ceiling((cd - elapsed).TotalSeconds);
                    ToastNotification.Show("Re-assessment Cooldown", $"Please wait {rem}s before re-assessing {item.Name} again.", ToastType.Warning);
                    return;
                }
            }
            try
            {
                SoloParentRecord recordToReassess = null;
                Models.Api.SoloParentDto existingDto = null;
                var detailResp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(targetId);
                if (detailResp != null && detailResp.Succeeded && detailResp.Data != null)
                {
                    existingDto = detailResp.Data;
                    recordToReassess = SoloParentApiService.Instance.MapToRecord(existingDto);
                }
                if (recordToReassess == null)
                {
                    ToastNotification.Show("Unable to Load Record", "Failed to retrieve candidate profile for re-assessment.", ToastType.Error);
                    return;
                }
                var dialog = new RenewalReassessmentDialog(recordToReassess, existingDto, isReassessmentMode: true)
                {
                    Owner = Application.Current.MainWindow
                };
                if (dialog.ShowDialog() == true && dialog.UpdatedRecord != null)
                {
                    // Save circumstance & profile updates via update request
                    var updateReq = SoloParentApiService.Instance.MapToUpdateRequest(targetId, dialog.UpdatedRecord, existingDto);
                    var saveResp = await SoloParentApiService.Instance.UpdateSoloParentAsync(targetId, updateReq);
                    if (saveResp != null && saveResp.Succeeded)
                    {
                        ToastNotification.Show("Record Updated", $"{recordToReassess.Name} circumstances updated successfully.", ToastType.Success);
                    }
                    if (dialog.UpdatedRecord.CurrentTermExpiresAt.HasValue)
                    {
                        item.CardExpiresAt = dialog.UpdatedRecord.CurrentTermExpiresAt.Value;
                        foreach (var r in ShortlistRows.Where(x => x.SoloParentId == targetId))
                            r.CardExpiresAt = dialog.UpdatedRecord.CurrentTermExpiresAt.Value;
                        foreach (var r in FinalGranteesList.Where(x => x.SoloParentId == targetId))
                            r.CardExpiresAt = dialog.UpdatedRecord.CurrentTermExpiresAt.Value;
                    }
                    // Run fresh ML prediction on the updated facts
                    await RunQuickPredictAsync(item);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show("Re-assessment Error", ex.Message, ToastType.Error);
            }
        }

        private async Task FetchRecordForDrawerAsync(SubsidyItem item)
        {
            if (item == null || item.SoloParentId == Guid.Empty) return;
            try
            {
                var resp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(item.SoloParentId);
                if (resp?.Data != null)
                    Application.Current?.Dispatcher?.Invoke(() => { SubsidyModelMapper.EnrichItemFromRecord(item, resp.Data); if (ActiveCandidate == item) OnPropertyChanged(nameof(ActiveCandidate)); });
            }
            catch { }
        }

        public async Task SaveQuotaAsync()
        {
            if (!CanModifyGrantees)
            {
                ToastNotification.Show("Action Unavailable", "Cannot modify quota for finalized or archived cycles.", ToastType.Warning);
                return;
            }
            if (GranteeSlots <= 0)
            {
                ToastNotification.Show("Invalid Quota", "Target grantees must be greater than 0.", ToastType.Warning);
                return;
            }
            if (GrantAmountPerRecipient <= 0)
            {
                ToastNotification.Show("Invalid Grant", "Subsidy grant amount must be greater than ₱0.", ToastType.Warning);
                return;
            }
            try
            {
                if (SelectedFiscalCycle != null)
                {
                    SelectedFiscalCycle.AllocatedSlots = GranteeSlots;
                    SelectedFiscalCycle.PerGranteeAmount = GrantAmountPerRecipient;
                    SelectedFiscalCycle.TotalBudget = GranteeSlots * (decimal)GrantAmountPerRecipient;
                }
                try
                {
                    var config = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);
                    if (config.AppSettings.Settings["DefaultSubsidyGrantAmount"] == null)
                        config.AppSettings.Settings.Add("DefaultSubsidyGrantAmount", GrantAmountPerRecipient.ToString());
                    else
                        config.AppSettings.Settings["DefaultSubsidyGrantAmount"].Value = GrantAmountPerRecipient.ToString();
                    if (config.AppSettings.Settings["DefaultSubsidyGranteeSlots"] == null)
                        config.AppSettings.Settings.Add("DefaultSubsidyGranteeSlots", GranteeSlots.ToString());
                    else
                        config.AppSettings.Settings["DefaultSubsidyGranteeSlots"].Value = GranteeSlots.ToString();
                    config.Save(System.Configuration.ConfigurationSaveMode.Modified);
                    System.Configuration.ConfigurationManager.RefreshSection("appSettings");
                }
                catch { }
                if (SelectedFiscalCycle?.CycleId.HasValue == true && SelectedFiscalCycle.IsActive)
                {
                    try
                    {
                        await _apiService.UpdateCycleAsync(SelectedFiscalCycle.CycleId.Value, new UpdateSubsidyCycleRequest
                        {
                            AllocatedSlots = GranteeSlots,
                            PerGranteeAmount = GrantAmountPerRecipient,
                            TotalBudget = GranteeSlots * (decimal)GrantAmountPerRecipient,
                            FiscalYear = SelectedFiscalCycle.FiscalYear,
                            CycleNumber = SelectedFiscalCycle.CycleNumber,
                            Name = SelectedFiscalCycle.Name
                        });
                    }
                    catch { }
                }
                RecalculateQuotaAndFilter();
                NotifyQuotaMetricsChanged();
                ToastNotification.Show("Quota Allocation Saved", $"FY {ActiveCycleFiscalYear}: {GranteeSlots} grantees @ ₱{GrantAmountPerRecipient:N0} (Total: ₱{(GranteeSlots * (decimal)GrantAmountPerRecipient):N2}).", ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastNotification.Show("Save Error", ex.Message, ToastType.Error);
            }
        }
    }
}