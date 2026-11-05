using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SOLUM_UI.Core;
using SOLUM_UI.DTOs;
using SOLUM_UI.Models;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.Views;
using SOLUM_UI.Views.Dialogs;

namespace SOLUM_UI.ViewModels
{
    public partial class SubsidyRecommendationViewModel
    {
        private void InitCommands()
        {
            SwitchTabCommand = new RelayCommand(p =>
            {
                if (int.TryParse(p?.ToString(), out int tab))
                {
                    ActiveTabIndex = tab;
                }
            });
            ToggleBatchModeCommand = new RelayCommand(_ => { IsBatchMode = !IsBatchMode; if (!IsBatchMode) { foreach (var item in FilteredRecommendations) item.IsSelected = false; RecalculateSelectionCounts(); } });
            SelectTopNCommand = new RelayCommand(_ => SelectTopN());
            ToggleShortlistBatchModeCommand = new RelayCommand(_ => IsShortlistBatchMode = !IsShortlistBatchMode);
            ShortlistSelectedCommand = new RelayCommand(_ => MoveSelectedToShortlist());
            QuickShortlistCandidateCommand = new RelayCommand(p =>
            {
                if (IsBatchMode && (FilteredRecommendations.Any(x => x.IsSelected) || p is SubsidyItem))
                {
                    if (p is SubsidyItem it && !it.IsSelected)
                    {
                        it.IsSelected = true;
                        RecalculateSelectionCounts();
                    }
                    MoveSelectedToShortlist();
                }
                else if (p is SubsidyItem item)
                {
                    ShortlistCandidate(item);
                }
            });
            QuickAcceptRowCommand = QuickShortlistCandidateCommand;
            ApproveCandidateCommand = QuickShortlistCandidateCommand;
            QuickRejectRowCommand = new RelayCommand(p =>
            {
                if (IsBatchMode && (FilteredRecommendations.Any(x => x.IsSelected) || p is SubsidyItem))
                {
                    if (p is SubsidyItem it && !it.IsSelected)
                    {
                        it.IsSelected = true;
                        RecalculateSelectionCounts();
                    }
                    _batchRejectTargets = FilteredRecommendations.Where(x => x.IsSelected).ToList();
                    OutlierTarget = null;
                    _shortlistRevokeTargets = null;
                    OutlierModalTitle = $"Disqualify ({_batchRejectTargets.Count}) Candidates";
                    OutlierTargetNameAndId = $"{_batchRejectTargets.Count} Selected Candidates";
                    IsOutlierModalOpen = true;
                }
                else
                {
                    _batchRejectTargets = null;
                    OutlierTarget = p as SubsidyItem;
                    if (OutlierTarget != null)
                    {
                        _shortlistRevokeTargets = null;
                        OutlierModalTitle = "Disqualify Candidate";
                        OutlierTargetNameAndId = null;
                        IsOutlierModalOpen = true;
                    }
                }
            });
            BatchRejectQueueCommand = new RelayCommand(_ =>
            {
                var selected = FilteredRecommendations.Where(x => x.IsSelected).ToList();
                if (selected.Count == 0)
                {
                    ToastNotification.Show("No Selection", "Please check candidate boxes to reject.", ToastType.Info);
                    return;
                }
                _batchRejectTargets = selected;
                OutlierTarget = null;
                _shortlistRevokeTargets = null;
                OutlierModalTitle = $"Disqualify ({_batchRejectTargets.Count}) Candidates";
                OutlierTargetNameAndId = $"{_batchRejectTargets.Count} Selected Candidates";
                IsOutlierModalOpen = true;
            });
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
            OpenDisqualifiedModalCommand = new RelayCommand(_ => {
                DisqualifiedSearchText = string.Empty;
                DisqualifiedReasonFilter = "All Reasons";
                DisqualifiedCurrentPage = 1;
                FilterDisqualifiedCandidates();
                IsDisqualifiedModalOpen = true;
                _ = RefreshDisqualifiedCandidatesAsync();
            });
            CloseDisqualifiedModalCommand = new RelayCommand(_ => IsDisqualifiedModalOpen = false);
            RefreshDisqualifiedCommand = new RelayCommand(async _ => await RefreshDisqualifiedCandidatesAsync());
            DisqualifiedPrevPageCommand = new RelayCommand(_ => { if (CanDisqualifiedPrevPage) DisqualifiedCurrentPage--; });
            DisqualifiedNextPageCommand = new RelayCommand(_ => { if (CanDisqualifiedNextPage) DisqualifiedCurrentPage++; });
            ExportDisqualifiedCsvCommand = new RelayCommand(async _ => await ExportDisqualifiedCsvAsync());
            ReinstateCandidateCommand = new RelayCommand(p => {
                if (p is DisqualifiedCandidateItem item)
                    _ = ReinstateCandidateAsync(item);
            });
            ApplyDisqualifiedReasonFilterCommand = new RelayCommand(p => {
                DisqualifiedReasonFilter = p?.ToString() ?? "All Reasons";
            });
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
            QuickPredictCandidateCommand = new RelayCommand(async p => await RunQuickPredictAsync(p as SubsidyItem ?? ActiveCandidate));
            OpenReassessmentFormCommand = new RelayCommand(async p => await OpenReassessmentFormAsync(p as SubsidyItem ?? ActiveCandidate));
            RefreshCommand = new RelayCommand(async _ => { InvalidateCache(); await RefreshDataAsync(true); }, _ => !IsLoading); GenerateRecommendationsCommand = new RelayCommand(async _ => await GenerateAndRefreshAsync(), _ => !IsLoading);
            ToggleRosterLockCommand = new RelayCommand(_ => ToggleRosterLock()); StartNewCycleCommand = new RelayCommand(async _ => await StartNewCycleAsync()); SaveQuotaCommand = new RelayCommand(async _ => await SaveQuotaAsync());
            ExportAllQueueCommand = new RelayCommand(async _ => await ExportRecommendationsQueueAsync());
        }

        private async Task GenerateAndRefreshAsync()
        {
            if (IsLoading) return;

            int yr = SelectedFiscalCycle?.FiscalYear ?? ActiveCycleFiscalYear;
            bool confirmed = ActionConfirmDialog.Show(
                "Re-Assess All Candidates",
                $"This will re-evaluate all candidates for Fiscal Year {yr} using the latest subsidy recommendation model.\n\nDepending on the number of applicants, this may take a moment to process. Do you want to continue?",
                "Re-Assess",
                "Cancel",
                ConfirmThemeType.Primary);
            if (!confirmed) return;
            IsLoading = true;
            try
            {
                var res = await _apiService.GenerateRecommendationsAsync(_activeCycleId);
                if (res != null && res.Succeeded)
                {
                    ToastNotification.Show("Evaluations Generated", $"{res.Data} candidate(s) processed.", ToastType.Success);
                    if (SelectedFiscalCycle != null) await LoadCycleDataInternalAsync(SelectedFiscalCycle);
                    else await RefreshDataAsync();
                }
            }
            catch (Exception ex) { ToastNotification.Show("Generation Error", ex.Message, ToastType.Error); }
            finally { IsLoading = false; }
        }

        private void SelectTopN()
        {
            if (FilteredRecommendations.Count == 0) return;
            bool all = FilteredRecommendations.All(x => x.IsSelected);
            foreach (var item in FilteredRecommendations) item.IsSelected = !all;
            RecalculateSelectionCounts();
        }

        private async void MoveSelectedToShortlist()
        {
            var selected = FilteredRecommendations.Where(x => x.IsSelected).ToList();
            if (selected.Count == 0) { ToastNotification.Show("No Selection", "Please select at least one pending candidate.", ToastType.Warning); return; }

            bool ok = ActionConfirmDialog.Show("Shortlist Selected", $"Move {selected.Count} selected candidate(s) to Approved Selection & Audit for FY {ActiveCycleFiscalYear}?", "Shortlist All", "Cancel", ConfirmThemeType.Primary);
            if (!ok) return;
            var evalIds = selected.Where(x => x.EvaluationId != Guid.Empty).Select(x => x.EvaluationId).ToList();
            if (evalIds.Count > 0)
            {
                try { await _apiService.ShortlistCandidatesAsync(_activeCycleId, evalIds); } catch { }
            }
            foreach (var item in selected)
            {
                item.Status = "Approved";
                item.IsSelected = false;
                if (!ShortlistRows.Any(x => (!string.IsNullOrEmpty(item.SpId) && x.SpId == item.SpId)
                                         || (item.EvaluationId != Guid.Empty && x.EvaluationId == item.EvaluationId)
                                         || (item.SoloParentId != Guid.Empty && x.SoloParentId == item.SoloParentId)
                                         || (!string.IsNullOrEmpty(item.Name) && string.Equals(x.Name, item.Name, StringComparison.OrdinalIgnoreCase))))
                {
                    var collegeKids = item.ChildrenDetails?.Where(c => c.Age >= 17 && c.Age <= 24).ToList() ?? new List<ChildDetailItem>();
                    ShortlistRows.Add(new SelectionRow
                    {
                        Rank = ShortlistRows.Count + 1,
                        EvaluationId = item.EvaluationId,
                        CycleId = item.CycleId,
                        SoloParentId = item.SoloParentId,
                        SpId = item.SpId,
                        Name = item.Name,
                        Barangay = item.Barangay,
                        Sex = item.Sex,
                        CivilStatus = item.CivilStatus,
                        Priority = item.Priority,
                        Score = item.Score,
                        CardExpiresAt = item.CardExpiresAt,
                        Dependants = item.Dependants,
                        MonthlyIncome = item.MonthlyIncome,
                        IncomePerCapita = item.IncomePerCapita,
                        MinorDependentsCount = item.MinorDependentsCount,
                        ToddlersUnder5Count = item.ToddlersUnder5Count,
                        Circumstance = item.Circumstance,
                        HasCollegeAgeDependent = item.HasCollegeAgeDependent,
                        CollegeAgeDependentsCount = item.CollegeAgeDependentsCount,
                        CollegeAgeDependents = collegeKids,
                        IsPantawidBeneficiary = item.IsPantawidBeneficiary,
                        IsFinalGrantee = false,
                        IsWaitlisted = true,
                        ModelVersion = item.ModelVersion,
                        Children0To6Count = item.Children0To6Count,
                        Children7To22Count = item.Children7To22Count,
                        OtherIncomeSource = item.OtherIncomeSource,
                        NeedsAndProblems = item.NeedsAndProblems,
                        ChildrenDetails = item.ChildrenDetails ?? new List<ChildDetailItem>()
                    });
                }
                _allItems.RemoveAll(x => (!string.IsNullOrEmpty(item.SpId) && x.SpId == item.SpId)
                                      || (item.EvaluationId != Guid.Empty && x.EvaluationId == item.EvaluationId)
                                      || (item.SoloParentId != Guid.Empty && x.SoloParentId == item.SoloParentId)
                                      || (!string.IsNullOrEmpty(item.Name) && string.Equals(x.Name, item.Name, StringComparison.OrdinalIgnoreCase)));
                FilteredRecommendations.Remove(item);
                PagedRecommendations.Remove(item);
            }
            IsBatchMode = false;
            RecalculateQuotaAndFilter();
            UpdateShortlistAndFinalCollections();
            RecalculateSelectionCounts();
            NotifyQuotaMetricsChanged();
            ToastNotification.Show("Candidates Shortlisted", $"{selected.Count} candidate(s) moved to Shortlist & Audit.", ToastType.Success);
        }

        private async void ShortlistCandidate(SubsidyItem item, bool notify = true, bool isConfirmed = false)
        {
            if (item == null) return;
            if (notify)
            {
                bool ok = ActionConfirmDialog.Show("Shortlist Candidate", $"Move {item.Name} to Approved Selection & Audit for FY {ActiveCycleFiscalYear}?", "Shortlist", "Cancel", ConfirmThemeType.Primary);
                if (!ok) return;
            }
            item.Status = "Approved";
            if (item.EvaluationId != Guid.Empty)
            {
                try { await _apiService.ShortlistCandidatesAsync(_activeCycleId, new List<Guid> { item.EvaluationId }); } catch { }
            }
            if (!ShortlistRows.Any(x => (!string.IsNullOrEmpty(item.SpId) && x.SpId == item.SpId)
                                     || (item.EvaluationId != Guid.Empty && x.EvaluationId == item.EvaluationId)
                                     || (item.SoloParentId != Guid.Empty && x.SoloParentId == item.SoloParentId)
                                     || (!string.IsNullOrEmpty(item.Name) && string.Equals(x.Name, item.Name, StringComparison.OrdinalIgnoreCase))))
            {
                var collegeKids = item.ChildrenDetails?.Where(c => c.Age >= 17 && c.Age <= 24).ToList() ?? new List<ChildDetailItem>();
                ShortlistRows.Add(new SelectionRow
                {
                    Rank = ShortlistRows.Count + 1,
                    EvaluationId = item.EvaluationId,
                    CycleId = item.CycleId,
                    SoloParentId = item.SoloParentId,
                    SpId = item.SpId,
                    Name = item.Name,
                    Barangay = item.Barangay,
                    Sex = item.Sex,
                    CivilStatus = item.CivilStatus,
                    Priority = item.Priority,
                    Score = item.Score,
                    CardExpiresAt = item.CardExpiresAt,
                    Dependants = item.Dependants,
                    MonthlyIncome = item.MonthlyIncome,
                    IncomePerCapita = item.IncomePerCapita,
                    MinorDependentsCount = item.MinorDependentsCount,
                    ToddlersUnder5Count = item.ToddlersUnder5Count,
                    Circumstance = item.Circumstance,
                    HasCollegeAgeDependent = item.HasCollegeAgeDependent,
                    CollegeAgeDependentsCount = item.CollegeAgeDependentsCount,
                    CollegeAgeDependents = collegeKids,
                    IsPantawidBeneficiary = item.IsPantawidBeneficiary,
                    IsFinalGrantee = isConfirmed,
                    IsWaitlisted = !isConfirmed,
                    ModelVersion = item.ModelVersion,
                    Children0To6Count = item.Children0To6Count,
                    Children7To22Count = item.Children7To22Count,
                    OtherIncomeSource = item.OtherIncomeSource,
                    NeedsAndProblems = item.NeedsAndProblems,
                    ChildrenDetails = item.ChildrenDetails ?? new List<ChildDetailItem>()
                });
            }
            _allItems.RemoveAll(x => (!string.IsNullOrEmpty(item.SpId) && x.SpId == item.SpId)
                                  || (item.EvaluationId != Guid.Empty && x.EvaluationId == item.EvaluationId)
                                  || (item.SoloParentId != Guid.Empty && x.SoloParentId == item.SoloParentId)
                                  || (!string.IsNullOrEmpty(item.Name) && string.Equals(x.Name, item.Name, StringComparison.OrdinalIgnoreCase)));
            FilteredRecommendations.Remove(item);
            PagedRecommendations.Remove(item);
            for (int i = 0; i < FilteredRecommendations.Count; i++)
            {
                FilteredRecommendations[i].Rank = i + 1;
                FilteredRecommendations[i].IsWithinQuota = (i + 1 <= _granteeSlots);
            }
            QueueTotalCount = FilteredRecommendations.Count;
            RecalculateQuotaAndFilter();
            UpdateShortlistAndFinalCollections();
            RecalculateSelectionCounts();
            NotifyQuotaMetricsChanged();
            if (notify) ToastNotification.Show("Candidate Shortlisted", $"{item.Name} moved to Shortlist & Audit.", ToastType.Success);
        }

        private async void BatchConfirmShortlist()
        {
            var targets = FilteredShortlistRows.Where(x => x.IsSelected).ToList();
            if (targets.Count == 0) { ToastNotification.Show("No Selection", "Please check candidate boxes to confirm.", ToastType.Info); return; }
            var expiredTargets = targets.Where(x => x.IsCardExpired).ToList();
            var validTargets = targets.Where(x => !x.IsCardExpired).ToList();
            if (validTargets.Count == 0)
            {
                ToastNotification.Show(
                    "Cards Expired",
                    "All selected candidate(s) have expired Solo Parent IDs. They cannot be confirmed to Final Grantees until renewed.",
                    ToastType.Error);
                return;
            }

            string confirmMsg = expiredTargets.Count > 0
                ? $"Confirm {validTargets.Count} eligible candidate(s) as official Final Grantees for FY {ActiveCycleFiscalYear}?\n\nNote: {expiredTargets.Count} candidate(s) with expired cards will be skipped and remain in audit."
                : $"Confirm {validTargets.Count} candidate(s) as official Final Grantees for FY {ActiveCycleFiscalYear}?";

            bool ok = ActionConfirmDialog.Show("Batch Confirm Grantees", confirmMsg, "Confirm Grantees", "Cancel", ConfirmThemeType.Success);
            if (!ok) return;
            var evalIds = validTargets.Where(x => x.EvaluationId != Guid.Empty).Select(x => x.EvaluationId).ToList();
            if (evalIds.Count > 0) await _apiService.ConfirmGranteesAsync(_activeCycleId, evalIds);
            foreach (var r in validTargets) { r.IsFinalGrantee = true; r.IsSelected = false; }
            foreach (var r in expiredTargets) { r.IsSelected = false; }
            IsShortlistBatchMode = false;
            UpdateShortlistAndFinalCollections();
            RecalculateShortlistSelectionCounts();
            NotifyQuotaMetricsChanged();
            ToastNotification.Show("Batch Confirmed", $"{validTargets.Count} candidate(s) confirmed as final grantees." + (expiredTargets.Count > 0 ? $" ({expiredTargets.Count} skipped due to expired cards)" : ""), ToastType.Success);
        }

        private void BatchRevokeShortlist()
        {
            var targets = FilteredShortlistRows.Where(x => x.IsSelected).ToList();
            if (targets.Count == 0) { ToastNotification.Show("No Selection", "Please check candidate boxes to revoke.", ToastType.Info); return; }
            _shortlistRevokeTargets = targets; OutlierTarget = null;
            OutlierModalTitle = $"Revoke {targets.Count} Candidate(s)"; OutlierTargetNameAndId = $"{targets.Count} candidate(s) selected from Approved Selection";
            OnPropertyChanged(nameof(OutlierModalConfirmButtonText)); IsOutlierModalOpen = true;
        }
    }
}