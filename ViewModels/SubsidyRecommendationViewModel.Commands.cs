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
                if (IsLoading) return;
                if (int.TryParse(p?.ToString(), out int tab))
                {
                    if ((tab == 0 || tab == 1) && !IsActiveCycle)
                    {
                        ToastNotification.Show("Historical Fiscal Year",
                            $"Candidate triage and audit pipeline are only accessible for the operational fiscal cycle (FY {ActiveCycleFiscalYear}).",
                            ToastType.Info);
                        return;
                    }
                    ActiveTabIndex = tab;
                }
            }, _ => !IsLoading);
            ToggleBatchModeCommand = new RelayCommand(_ =>
            {
                if (!CanModifyGrantees) return;
                if (TabRecommendationsVisible)
                {
                    IsBatchMode = !IsBatchMode;
                }
                else if (TabShortlistVisible)
                {
                    IsShortlistBatchMode = !IsShortlistBatchMode;
                }
                OnPropertyChanged(nameof(IsActiveTabBatchMode));
            });
            SelectTopNCommand = new RelayCommand(_ => { if (!CanModifyGrantees) return; SelectTopN(); });
            ToggleShortlistBatchModeCommand = new RelayCommand(_ => { if (!CanModifyGrantees) return; IsShortlistBatchMode = !IsShortlistBatchMode; });
            ShortlistSelectedCommand = new RelayCommand(_ => { if (!CanModifyGrantees) return; MoveSelectedToShortlist(); });
            QuickShortlistCandidateCommand = new RelayCommand(p =>
            {
                if (!CanModifyGrantees) return;
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
                if (!CanModifyGrantees) return;
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
                if (!CanModifyGrantees) return;
                var selected = SelectedQueueItems;
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
                        HasSpecialNeedsDependent = r.HasSpecialNeedsDependent,
                        SpecialNeedsDependentsCount = r.SpecialNeedsDependentsCount,
                        CardExpiresAt = r.CardExpiresAt,
                        IsPantawidBeneficiary = r.IsPantawidBeneficiary, ModelVersion = r.ModelVersion,
                        Children0To6Count = r.Children0To6Count, Children7To22Count = r.Children7To22Count,
                        OtherIncomeSource = r.OtherIncomeSource, NeedsAndProblems = r.NeedsAndProblems,
                        ChildrenDetails = r.ChildrenDetails
                    };
                }
                if (item != null) { ActiveCandidate = item; IsDrawerOpen = true; _ = FetchRecordForDrawerAsync(item); }
            });
            CloseExplainDrawerCommand = new RelayCommand(_ => IsDrawerOpen = false);
            DrawerShortlistCommand = new RelayCommand(_ =>
            {
                if (!CanModifyGrantees) return;
                if (ActiveCandidate != null)
                {
                    bool alreadyInShortlist = ShortlistRows.Any(x => (x.SoloParentId != Guid.Empty && x.SoloParentId == ActiveCandidate.SoloParentId)
                                                                 || (!string.IsNullOrEmpty(x.SpId) && x.SpId == ActiveCandidate.SpId));
                    if (alreadyInShortlist)
                    {
                        ToastNotification.Show("Already in Shortlist", $"{ActiveCandidate.Name} is already in the Approved Selection.", ToastType.Info);
                    }
                    else
                    {
                        ShortlistCandidate(ActiveCandidate);
                    }
                    IsDrawerOpen = false;
                }
            });
            OpenOutlierModalCommand = new RelayCommand(p => { if (!CanModifyGrantees) return; OutlierTarget = p as SubsidyItem ?? ActiveCandidate; if (OutlierTarget != null) { _shortlistRevokeTargets = null; OutlierModalTitle = "Disqualify Candidate"; OutlierTargetNameAndId = null; IsOutlierModalOpen = true; } });
            CloseOutlierModalCommand = new RelayCommand(_ => { IsOutlierModalOpen = false; _shortlistRevokeTargets = null; });
            ConfirmOutlierRejectionCommand = new RelayCommand(_ => { if (!CanModifyGrantees) return; _ = ConfirmOutlierRejectionAsync(); });
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
                if (!CanModifyGrantees) return;
                if (p is DisqualifiedCandidateItem item)
                    _ = ReinstateCandidateAsync(item);
            });
            ApplyDisqualifiedReasonFilterCommand = new RelayCommand(p => {
                DisqualifiedReasonFilter = p?.ToString() ?? "All Reasons";
            });
            ConfirmGranteeCommand = new RelayCommand(p => { if (!CanModifyGrantees) return; ToggleGrantee(p as SelectionRow, true); });
            RevokeGranteeCommand = new RelayCommand(p => { if (!CanModifyGrantees) return; ToggleGrantee(p as SelectionRow, false); });
            ConfirmShortlistCandidateCommand = new RelayCommand(p => { if (!CanModifyGrantees) return; if (p is SelectionRow r) ToggleGrantee(r, !r.IsFinalGrantee); });
            RevokeShortlistCandidateCommand = new RelayCommand(p => { if (!CanModifyGrantees) return; PromptRevokeShortlist(p as SelectionRow); });
            BatchConfirmShortlistCommand = new RelayCommand(_ => { if (!CanModifyGrantees) return; BatchConfirmShortlist(); });
            BatchRevertShortlistCommand = new RelayCommand(_ => { if (!CanModifyGrantees) return; BatchRevertShortlist(); });
            BatchRevokeShortlistCommand = new RelayCommand(_ => { if (!CanModifyGrantees) return; BatchRevokeShortlist(); });
            SelectAllShortlistCommand = new RelayCommand(_ => { bool all = FilteredShortlistRows.Count > 0 && FilteredShortlistRows.All(x => x.IsSelected); foreach (var r in FilteredShortlistRows) r.IsSelected = !all; RecalculateShortlistSelectionCounts(); });
            ExportCityEducCommand = new RelayCommand(async _ => await ExportCityEducRosterAsync());
            ExportPantawid4PsCommand = new RelayCommand(async _ => await ExportPantawid4PsRosterAsync());
            ExportFinalLedgerCommand = new RelayCommand(async _ => await ExportFinalLedgerRosterAsync());
            ViewFullRecordCommand = new RelayCommand(async p => await OpenFullRecordViewAsync(p as SubsidyItem ?? ActiveCandidate));
            ExecuteQueueBackendSearchCommand = new RelayCommand(async _ => await ExecuteQueueBackendSearchAsync());
            QuickPredictCandidateCommand = new RelayCommand(async p => { if (!CanModifyGrantees) return; await RunQuickPredictAsync(p as SubsidyItem ?? ActiveCandidate); });
            OpenReassessmentFormCommand = new RelayCommand(async p => { if (!CanModifyGrantees) return; await OpenReassessmentFormAsync(p as SubsidyItem ?? ActiveCandidate); });
            RefreshCommand = new RelayCommand(async _ => { InvalidateCache(); await RefreshDataAsync(true); }, _ => !IsLoading); GenerateRecommendationsCommand = new RelayCommand(async _ => { if (!CanModifyGrantees) return; await GenerateAndRefreshAsync(); }, _ => !IsLoading && CanModifyGrantees);
            EditCycleCommand = new RelayCommand(_ => OpenEditCycleDialog());
            ViewDetailsCommand = new RelayCommand(_ => OpenViewDetailsDialog());
            ToggleRosterLockCommand = new RelayCommand(_ => ToggleRosterLock()); StartNewCycleCommand = new RelayCommand(async _ => await StartNewCycleAsync(), _ => !IsLoading && CanStartNewCycle); SaveQuotaCommand = new RelayCommand(async _ => { if (!CanModifyGrantees) return; await SaveQuotaAsync(); }, _ => CanModifyGrantees);
            RunMLTriageCommand = GenerateRecommendationsCommand;
        }

        private async Task GenerateAndRefreshAsync()
        {
            if (IsLoading) return;

            int yr = SelectedFiscalCycle?.FiscalYear ?? ActiveCycleFiscalYear;
            bool confirmed = ActionConfirmDialog.Show(
                "Re-Assess Queue Candidates",
                $"This will re-evaluate pending applicants in the Queue for FY {yr}.\n\nRecords will be checked for:\n• Active status (valid, non-expired Solo Parent ID)\n• Dependent eligibility (minor or special needs dependents)\n\nApproved shortlist selections will not be affected. Do you want to continue?",
                "Re-Assess",
                "Cancel",
                ConfirmThemeType.Primary);
            if (!confirmed) return;
            IsLoading = true;
            try
            {
                var targetCycleId = SelectedFiscalCycle?.CycleId ?? _activeCycleId;
                var res = await _apiService.GenerateRecommendationsAsync(targetCycleId, reassess: true);
                if (res != null && res.Succeeded)
                {
                    if (res.Data > 0)
                    {
                        ToastNotification.Show("Reassessment Complete", $"{res.Data} candidate(s) processed and updated for FY {yr}.", ToastType.Success);
                    }
                    else
                    {
                        ToastNotification.Show("Reassessment Up-to-Date", $"All candidates in the Queue are already evaluated and up to date for FY {yr}.", ToastType.Info);
                    }

                    if (SelectedFiscalCycle != null) await LoadCycleDataInternalAsync(SelectedFiscalCycle);
                    else await RefreshDataAsync();
                }
                else
                {
                    string err = res?.Errors != null && res.Errors.Count > 0
                        ? string.Join(", ", res.Errors)
                        : "Failed to complete candidate re-assessment. Please try again.";
                    ToastNotification.Show("Reassessment Failed", err, ToastType.Error);
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
            var selected = SelectedQueueItems;
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
                if (PagedRecommendations != null && PagedRecommendations.Contains(item))
                    PagedRecommendations.Remove(item);
                if (FilteredRecommendations != null && FilteredRecommendations.Contains(item))
                    FilteredRecommendations.Remove(item);
                if (_allItems != null && _allItems.Contains(item))
                    _allItems.Remove(item);

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
            var allSelected = SelectedShortlistRows.ToList();
            var targets = allSelected.Where(x => !x.IsFinalGrantee).ToList();
            if (targets.Count == 0)
            {
                if (allSelected.Count > 0)
                {
                    ToastNotification.Show("Already Confirmed", "All selected candidate(s) are already confirmed final grantees.", ToastType.Info);
                }
                else
                {
                    ToastNotification.Show("No Selection", "Please check candidate boxes to confirm.", ToastType.Info);
                }
                return;
            }
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

        private void BatchRevertShortlist()
        {
            var allSelected = SelectedShortlistRows.ToList();
            var targets = allSelected.Where(x => x.IsFinalGrantee).ToList();
            if (targets.Count == 0)
            {
                if (allSelected.Count > 0)
                {
                    ToastNotification.Show("No Confirmed Grantees", "None of the selected candidates are confirmed final grantees to revert.", ToastType.Info);
                }
                else
                {
                    ToastNotification.Show("No Selection", "Please check candidate boxes to revert.", ToastType.Info);
                }
                return;
            }

            bool ok = ActionConfirmDialog.Show(
                "Revert Selected Grantees",
                $"Revert {targets.Count} confirmed grantee(s) back to pending shortlist audit for FY {ActiveCycleFiscalYear}?",
                "Revert All",
                "Cancel",
                ConfirmThemeType.Warning);
            if (!ok) return;

            foreach (var r in targets)
            {
                r.IsFinalGrantee = false;
                r.IsSelected = false;
            }
            IsShortlistBatchMode = false;
            UpdateShortlistAndFinalCollections();
            RecalculateShortlistSelectionCounts();
            NotifyQuotaMetricsChanged();
            ToastNotification.Show("Batch Reverted", $"{targets.Count} candidate(s) reverted to pending shortlist.", ToastType.Info);
        }

        private void BatchRevokeShortlist()
        {
            var targets = SelectedShortlistRows.ToList();
            if (targets.Count == 0) { ToastNotification.Show("No Selection", "Please check candidate boxes to revoke.", ToastType.Info); return; }
            _shortlistRevokeTargets = targets; OutlierTarget = null;
            OutlierModalTitle = $"Revoke {targets.Count} Candidate(s)"; OutlierTargetNameAndId = $"{targets.Count} candidate(s) selected from Approved Selection";
            OnPropertyChanged(nameof(OutlierModalConfirmButtonText)); IsOutlierModalOpen = true;
        }
    }
}