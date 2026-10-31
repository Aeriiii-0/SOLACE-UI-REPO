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
                        _ = _apiService.RejectOutlierAsync(new RejectOutlierRequest { EvaluationId = r.EvaluationId, RejectionReason = OutlierRejectionReason, RejectionNotes = OutlierNotes, AutoPromote = OutlierAutoPromote });
                }
                _shortlistRevokeTargets = null;
                IsOutlierModalOpen = false;
                UpdateShortlistAndFinalCollections();
                RecalculateQuotaAndFilter();
                RecalculateShortlistSelectionCounts();
                ToastNotification.Show("Revocation Complete", $"{count} candidate(s) revoked ({OutlierRejectionReason}).", ToastType.Warning);
            }
            else if (OutlierTarget != null)
            {
                var target = OutlierTarget;
                target.IsDisqualified = true;
                target.DisqualificationReason = OutlierRejectionReason;
                target.Status = "Rejected";
                if (target.EvaluationId != Guid.Empty)
                    await _apiService.RejectOutlierAsync(new RejectOutlierRequest { EvaluationId = target.EvaluationId, RejectionReason = OutlierRejectionReason, RejectionNotes = OutlierNotes, AutoPromote = OutlierAutoPromote });
                IsOutlierModalOpen = false;
                RecalculateQuotaAndFilter();
                ToastNotification.Show("Outlier Disqualified", $"{target.Name} removed from queue. Next candidate promoted.", ToastType.Warning);
            }
        }

        private async void ToggleGrantee(SelectionRow row, bool confirm)
        {
            if (row == null) return;
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
                new RecordViewDialog(recordToView) { Owner = Application.Current.MainWindow }.ShowDialog();
            }
            catch (Exception ex) { ToastNotification.Show("Unable to Open Record", ex.Message, ToastType.Error); }
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
                            TotalBudget = GranteeSlots * (decimal)GrantAmountPerRecipient
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
