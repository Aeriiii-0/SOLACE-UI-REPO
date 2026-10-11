using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SOLUM_UI.Core;
using SOLUM_UI.DTOs;
using SOLUM_UI.Models;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.Views.Dialogs;

namespace SOLUM_UI.ViewModels
{
    public partial class SubsidyRecommendationViewModel
    {
        private void InitCommands()
        {
            SwitchTabCommand = new RelayCommand(p => { if (int.TryParse(p?.ToString(), out int tab)) ActiveTabIndex = tab; });
            ToggleBatchModeCommand = new RelayCommand(_ => { IsBatchMode = !IsBatchMode; if (!IsBatchMode) { foreach (var item in FilteredRecommendations) item.IsSelected = false; RecalculateSelectionCounts(); } });
            SelectTopNCommand = new RelayCommand(_ => SelectTopN());
            ShortlistSelectedCommand = new RelayCommand(_ => MoveSelectedToShortlist());
            QuickAcceptRowCommand = new RelayCommand(p => { if (p is SubsidyItem item) ShortlistCandidate(item); });
            QuickRejectRowCommand = new RelayCommand(p => { OutlierTarget = p as SubsidyItem; if (OutlierTarget != null) { _shortlistRevokeTargets = null; OutlierModalTitle = "Disqualify Candidate"; OutlierTargetNameAndId = null; IsOutlierModalOpen = true; } });
            OpenExplainDrawerCommand = new RelayCommand(p => {
                SubsidyItem item = null;
                if (p is SubsidyItem it) item = it;
                else if (p is SelectionRow r)
                {
                    item = _allItems.FirstOrDefault(x => x.SpId == r.SpId) ?? new SubsidyItem
                    {
                        SoloParentId = r.SoloParentId, SpId = r.SpId, Name = r.Name, Barangay = r.Barangay,
                        Priority = r.Priority, Score = r.Score, MonthlyIncome = r.MonthlyIncome,
                        IncomePerCapita = r.IncomePerCapita, MinorDependentsCount = r.MinorDependentsCount,
                        ToddlersUnder5Count = r.ToddlersUnder5Count, Circumstance = r.Circumstance,
                        CollegeAgeDependentsCount = r.CollegeAgeDependentsCount,
                        IsPantawidBeneficiary = r.IsPantawidBeneficiary, ModelVersion = r.ModelVersion,
                        Children0To6Count = r.Children0To6Count, Children7To22Count = r.Children7To22Count,
                        OtherIncomeSource = r.OtherIncomeSource, NeedsAndProblems = r.NeedsAndProblems,
                        ChildrenDetails = r.ChildrenDetails
                    };
                }
                if (item != null) { ActiveCandidate = item; IsDrawerOpen = true; _ = FetchRecordForDrawerAsync(item); }
            });
            CloseExplainDrawerCommand = new RelayCommand(_ => IsDrawerOpen = false);
            DrawerShortlistCommand = new RelayCommand(_ => { if (ActiveCandidate != null) { ShortlistCandidate(ActiveCandidate); IsDrawerOpen = false; } });
            OpenOutlierModalCommand = new RelayCommand(p => { OutlierTarget = p as SubsidyItem ?? ActiveCandidate; if (OutlierTarget != null) { _shortlistRevokeTargets = null; OutlierModalTitle = "Disqualify Candidate"; OutlierTargetNameAndId = null; IsOutlierModalOpen = true; } });
            CloseOutlierModalCommand = new RelayCommand(_ => { IsOutlierModalOpen = false; _shortlistRevokeTargets = null; });
            ConfirmOutlierRejectionCommand = new RelayCommand(_ => _ = ConfirmOutlierRejectionAsync());
            ConfirmGranteeCommand = new RelayCommand(p => ToggleGrantee(p as SelectionRow, true));
            RevokeGranteeCommand = new RelayCommand(p => ToggleGrantee(p as SelectionRow, false));
            ConfirmShortlistCandidateCommand = new RelayCommand(p => { if (p is SelectionRow r) ToggleGrantee(r, !r.IsFinalGrantee); });
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
                int yr = SelectedFiscalCycle?.FiscalYear ?? ActiveCycleFiscalYear;
                await ExportCsvAsync($"Final_Grantee_Ledger_FY{yr}", _apiService.ExportFinalLedgerCsvAsync(FinalGranteesList.ToList(), yr), $"Final grantee ledger for FY {yr} exported successfully.");
            });
            ViewFullRecordCommand = new RelayCommand(async p => await OpenFullRecordViewAsync(p as SubsidyItem ?? ActiveCandidate));
            RefreshCommand = new RelayCommand(async _ => await RefreshDataAsync());
            GenerateRecommendationsCommand = new RelayCommand(async _ => await GenerateAndRefreshAsync());
            ToggleRosterLockCommand = new RelayCommand(_ => ToggleRosterLock());
            StartNewCycleCommand = new RelayCommand(async _ => await StartNewCycleAsync());
        }

        private async Task GenerateAndRefreshAsync()
        {
            try
            {
                var res = await _apiService.GenerateRecommendationsAsync(_activeCycleId);
                if (res != null && res.Succeeded)
                {
                    ToastNotification.Show("Evaluations Generated", $"{res.Data} candidate(s) processed.", ToastType.Success);
                    if (SelectedFiscalCycle != null) await LoadCycleDataAsync(SelectedFiscalCycle);
                    else await RefreshDataAsync();
                }
            }
            catch (Exception ex) { ToastNotification.Show("Generation Error", ex.Message, ToastType.Error); }
        }

        private void SelectTopN()
        {
            int limit = Math.Min(_granteeSlots, FilteredRecommendations.Count); if (limit <= 0) return;
            bool all = FilteredRecommendations.Take(limit).All(x => x.IsSelected);
            for (int i = 0; i < limit; i++) FilteredRecommendations[i].IsSelected = !all;
            RecalculateSelectionCounts();
            ToastNotification.Show(all ? "Selection Cleared" : "Selection Updated", all ? $"Deselected top {limit} candidates." : $"Selected top {limit} candidates for review.", ToastType.Info);
        }

        private async void MoveSelectedToShortlist()
        {
            var selected = FilteredRecommendations.Where(x => x.IsSelected).ToList();
            if (selected.Count == 0) { ToastNotification.Show("No Selection", "Please select at least one pending candidate.", ToastType.Warning); return; }
            var evalIds = selected.Where(x => x.EvaluationId != Guid.Empty).Select(x => x.EvaluationId).ToList();
            if (evalIds.Count > 0) await _apiService.ShortlistCandidatesAsync(_activeCycleId, evalIds);
            foreach (var item in selected)
            {
                item.Status = "Approved";
                if (!ShortlistRows.Any(x => x.SpId == item.SpId))
                {
                    var collegeKids = item.ChildrenDetails?.Where(c => c.Age >= 17 && c.Age <= 24).ToList() ?? new List<ChildDetailItem>();
                    ShortlistRows.Add(new SelectionRow { Rank = ShortlistRows.Count + 1, EvaluationId = item.EvaluationId, CycleId = item.CycleId, SoloParentId = item.SoloParentId, SpId = item.SpId, Name = item.Name, Barangay = item.Barangay, Sex = item.Sex, CivilStatus = item.CivilStatus, Priority = item.Priority, Score = item.Score, Dependants = item.Dependants, MonthlyIncome = item.MonthlyIncome, IncomePerCapita = item.IncomePerCapita, MinorDependentsCount = item.MinorDependentsCount, ToddlersUnder5Count = item.ToddlersUnder5Count, Circumstance = item.Circumstance, HasCollegeAgeDependent = item.HasCollegeAgeDependent, CollegeAgeDependentsCount = item.CollegeAgeDependentsCount, CollegeAgeDependents = collegeKids, IsPantawidBeneficiary = item.IsPantawidBeneficiary, IsFinalGrantee = false, IsWaitlisted = true, ModelVersion = item.ModelVersion });
                }
                _allItems.RemoveAll(x => x.SpId == item.SpId || (item.EvaluationId != Guid.Empty && x.EvaluationId == item.EvaluationId));
            }
            RecalculateQuotaAndFilter();
            UpdateShortlistAndFinalCollections();
            ToastNotification.Show("Shortlisted", $"{selected.Count} candidate(s) moved to Audit & Verification.", ToastType.Success);
        }

        private async void ShortlistCandidate(SubsidyItem item, bool notify = true, bool isConfirmed = false)
        {
            item.Status = "Approved";
            if (item.EvaluationId != Guid.Empty) await _apiService.ShortlistCandidatesAsync(_activeCycleId, new List<Guid> { item.EvaluationId });
            if (!ShortlistRows.Any(x => x.SpId == item.SpId))
            {
                var collegeKids = item.ChildrenDetails?.Where(c => c.Age >= 17 && c.Age <= 24).ToList() ?? new List<ChildDetailItem>();
                ShortlistRows.Add(new SelectionRow { Rank = ShortlistRows.Count + 1, EvaluationId = item.EvaluationId, CycleId = item.CycleId, SoloParentId = item.SoloParentId, SpId = item.SpId, Name = item.Name, Barangay = item.Barangay, Sex = item.Sex, CivilStatus = item.CivilStatus, Priority = item.Priority, Score = item.Score, Dependants = item.Dependants, MonthlyIncome = item.MonthlyIncome, IncomePerCapita = item.IncomePerCapita, MinorDependentsCount = item.MinorDependentsCount, ToddlersUnder5Count = item.ToddlersUnder5Count, Circumstance = item.Circumstance, HasCollegeAgeDependent = item.HasCollegeAgeDependent, CollegeAgeDependentsCount = item.CollegeAgeDependentsCount, CollegeAgeDependents = collegeKids, IsPantawidBeneficiary = item.IsPantawidBeneficiary, IsFinalGrantee = isConfirmed, IsWaitlisted = !isConfirmed, ModelVersion = item.ModelVersion });
            }
            _allItems.RemoveAll(x => x.SpId == item.SpId || (item.EvaluationId != Guid.Empty && x.EvaluationId == item.EvaluationId));
            RecalculateQuotaAndFilter();
            UpdateShortlistAndFinalCollections();
            if (notify) ToastNotification.Show("Candidate Shortlisted", $"{item.Name} moved to Audit & Verification.", ToastType.Success);
        }

        private void PromptRevokeShortlist(SelectionRow r)
        {
            if (r == null) return;
            _shortlistRevokeTargets = new List<SelectionRow> { r }; OutlierTarget = _allItems.FirstOrDefault(x => x.SpId == r.SpId);
            OutlierModalTitle = "Revoke Candidate"; OutlierTargetNameAndId = $"Candidate: {r.Name}";
            OnPropertyChanged(nameof(OutlierModalConfirmButtonText)); IsOutlierModalOpen = true;
        }

        private async void BatchConfirmShortlist()
        {
            var targets = FilteredShortlistRows.Where(x => x.IsSelected).ToList();
            if (targets.Count == 0) { ToastNotification.Show("No Selection", "Please check candidate boxes to confirm.", ToastType.Info); return; }
            var evalIds = targets.Where(x => x.EvaluationId != Guid.Empty).Select(x => x.EvaluationId).ToList();
            if (evalIds.Count > 0) await _apiService.ConfirmGranteesAsync(_activeCycleId, evalIds);
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
                    if (r.EvaluationId != Guid.Empty)
                        _ = _apiService.RejectOutlierAsync(new RejectOutlierRequest { EvaluationId = r.EvaluationId, RejectionReason = OutlierRejectionReason, RejectionNotes = OutlierNotes, AutoPromote = OutlierAutoPromote });
                }
                _shortlistRevokeTargets = null; IsOutlierModalOpen = false;
                UpdateShortlistAndFinalCollections(); RecalculateQuotaAndFilter(); RecalculateShortlistSelectionCounts();
                ToastNotification.Show("Revocation Complete", $"{count} candidate(s) revoked ({OutlierRejectionReason}).", ToastType.Warning);
            }
            else if (OutlierTarget != null)
            {
                var target = OutlierTarget; target.IsDisqualified = true; target.DisqualificationReason = OutlierRejectionReason; target.Status = "Rejected";
                if (target.EvaluationId != Guid.Empty)
                    await _apiService.RejectOutlierAsync(new RejectOutlierRequest { EvaluationId = target.EvaluationId, RejectionReason = OutlierRejectionReason, RejectionNotes = OutlierNotes, AutoPromote = OutlierAutoPromote });
                IsOutlierModalOpen = false; RecalculateQuotaAndFilter();
                ToastNotification.Show("Outlier Disqualified", $"{target.Name} removed from queue. Next candidate promoted.", ToastType.Warning);
            }
        }

        private async void ToggleGrantee(SelectionRow row, bool confirm)
        {
            if (row == null) return;
            row.IsFinalGrantee = confirm;
            if (confirm && row.EvaluationId != Guid.Empty)
                await _apiService.ConfirmGranteesAsync(_activeCycleId, new List<Guid> { row.EvaluationId });
            else if (!confirm && row.EvaluationId != Guid.Empty)
                await _apiService.RevokeGrantAsync(row.EvaluationId, "Revoked from Final Grantees", "Admin manual action");
            UpdateShortlistAndFinalCollections();
            NotifyQuotaMetricsChanged();
            ToastNotification.Show(confirm ? "Confirmed" : "Revoked", $"{row.Name} {(confirm ? "confirmed as final grantee." : "removed from final grantees.")}", confirm ? ToastType.Success : ToastType.Warning);
        }

        private async Task ExportCsvAsync(string name, Task<string> generator, string msg) { var dlg = new SaveFileDialog { FileName = name, DefaultExt = ".csv", Filter = "CSV file (*.csv)|*.csv" }; if (dlg.ShowDialog() == true) { File.WriteAllText(dlg.FileName, await generator, new System.Text.UTF8Encoding(true)); ToastNotification.Show("Export Complete", msg, ToastType.Success); } }

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
    }
}
