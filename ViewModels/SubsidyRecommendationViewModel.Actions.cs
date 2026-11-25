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
            OutlierTarget = _allItems.FirstOrDefault(x => IsSameCandidate(x.EvaluationId, x.SoloParentId, x.SpId, r.EvaluationId, r.SoloParentId, r.SpId));
            OutlierModalTitle = "Revoke Candidate";
            OutlierTargetNameAndId = $"Candidate: {r.DisplayName}";
            OnPropertyChanged(nameof(OutlierModalConfirmButtonText));
            IsOutlierModalOpen = true;
        }

        private async Task ConfirmOutlierRejectionAsync()
        {
            try
            {
                if (_shortlistRevokeTargets != null && _shortlistRevokeTargets.Count > 0)
                {
                    var targets = _shortlistRevokeTargets.ToList();
                    _shortlistRevokeTargets = null;
                    IsOutlierModalOpen = false;
                    IsDrawerOpen = false;
                    ActiveCandidate = null;

                    int count = targets.Count;
                    var apiTasks = new List<Task<Models.Api.BaseResponse<SubsidyRecommendationDto>>>();
                    foreach (var r in targets)
                    {
                        ShortlistRows.Remove(r);
                        FinalGranteesList.Remove(r);
                        var orig = _allItems.FirstOrDefault(x => IsSameCandidate(x.EvaluationId, x.SoloParentId, x.SpId, r.EvaluationId, r.SoloParentId, r.SpId));
                        if (orig != null)
                        {
                            orig.IsDisqualified = true;
                            orig.DisqualificationReason = OutlierRejectionReason;
                            orig.Status = "Rejected";
                            _allItems.Remove(orig);
                        }
                        if (r.EvaluationId != Guid.Empty)
                        {
                            apiTasks.Add(_apiService.RejectOutlierAsync(new RejectOutlierRequest
                            {
                                EvaluationId = r.EvaluationId,
                                RejectionReason = OutlierRejectionReason,
                                RejectionNotes = OutlierNotes,
                                AutoPromote = false
                            }));
                        }
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
                    UpdateShortlistAndFinalCollections();
                    RecalculateQuotaAndFilter();
                    IsShortlistBatchMode = false;
                    RecalculateShortlistSelectionCounts();
                    NotifyDisqualifiedMetricsChanged();
                    ToastNotification.Show("Revocation Complete", $"{count} candidate(s) revoked ({OutlierRejectionReason}).", ToastType.Warning);

                    if (apiTasks.Count > 0)
                    {
                        var results = await Task.WhenAll(apiTasks);
                        var failed = results.FirstOrDefault(res => res != null && !res.Succeeded);
                        if (failed != null)
                        {
                            string err = failed.Errors != null && failed.Errors.Count > 0 ? string.Join(", ", failed.Errors) : "Server rejected revocation.";
                            ToastNotification.Show("Sync Warning", err, ToastType.Error);
                        }
                    }
                }
                else if (_batchRejectTargets != null && _batchRejectTargets.Count > 0)
                {
                    var targets = _batchRejectTargets.ToList();
                    _batchRejectTargets = null;
                    IsOutlierModalOpen = false;
                    IsDrawerOpen = false;
                    ActiveCandidate = null;

                    int count = targets.Count;
                    var apiTasks = new List<Task<Models.Api.BaseResponse<SubsidyRecommendationDto>>>();
                    foreach (var target in targets)
                    {
                        target.IsDisqualified = true;
                        target.DisqualificationReason = OutlierRejectionReason;
                        target.Status = "Rejected";
                        if (target.EvaluationId != Guid.Empty)
                        {
                            apiTasks.Add(_apiService.RejectOutlierAsync(new RejectOutlierRequest
                            {
                                EvaluationId = target.EvaluationId,
                                RejectionReason = OutlierRejectionReason,
                                RejectionNotes = OutlierNotes,
                                AutoPromote = false
                            }));
                        }
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
                        _allItems.RemoveAll(x => IsSameCandidate(x.EvaluationId, x.SoloParentId, x.SpId, target.EvaluationId, target.SoloParentId, target.SpId));
                        PagedRecommendations.Remove(target);
                        FilteredRecommendations.Remove(target);
                    }
                    IsBatchMode = false;
                    RecalculateQuotaAndFilter();
                    RecalculateSelectionCounts();
                    NotifyDisqualifiedMetricsChanged();
                    ToastNotification.Show("Candidates Disqualified", $"{count} candidate(s) disqualified from queue ({OutlierRejectionReason}).", ToastType.Warning);

                    if (apiTasks.Count > 0)
                    {
                        var results = await Task.WhenAll(apiTasks);
                        var failed = results.FirstOrDefault(res => res != null && !res.Succeeded);
                        if (failed != null)
                        {
                            string err = failed.Errors != null && failed.Errors.Count > 0 ? string.Join(", ", failed.Errors) : "Server rejected disqualification.";
                            ToastNotification.Show("Sync Warning", err, ToastType.Error);
                        }
                    }
                }
                else if (OutlierTarget != null)
                {
                    var target = OutlierTarget;
                    OutlierTarget = null;
                    IsOutlierModalOpen = false;
                    IsDrawerOpen = false;
                    ActiveCandidate = null;

                    target.IsDisqualified = true;
                    target.DisqualificationReason = OutlierRejectionReason;
                    target.Status = "Rejected";

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
                    _allItems.RemoveAll(x => IsSameCandidate(x.EvaluationId, x.SoloParentId, x.SpId, target.EvaluationId, target.SoloParentId, target.SpId));
                    PagedRecommendations.Remove(target);
                    FilteredRecommendations.Remove(target);
                    if (QueueTotalCount > 0) QueueTotalCount--;
                    RecalculateQuotaAndFilter();
                    NotifyDisqualifiedMetricsChanged();
                    ToastNotification.Show("Candidate Disqualified", $"{target.DisplayName} disqualified from queue ({OutlierRejectionReason}).", ToastType.Warning);

                    if (target.EvaluationId != Guid.Empty)
                    {
                        var resp = await _apiService.RejectOutlierAsync(new RejectOutlierRequest
                        {
                            EvaluationId = target.EvaluationId,
                            RejectionReason = OutlierRejectionReason,
                            RejectionNotes = OutlierNotes,
                            AutoPromote = false
                        });
                        if (resp != null && !resp.Succeeded)
                        {
                            string err = resp.Errors != null && resp.Errors.Count > 0 ? string.Join(", ", resp.Errors) : "Failed to persist disqualification on server.";
                            ToastNotification.Show("Sync Warning", err, ToastType.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                IsOutlierModalOpen = false;
                ToastNotification.Show("Disqualification Error", ex.Message, ToastType.Error);
            }
        }

        public void AddDisqualifiedCandidateRecord(
            Guid evalId, Guid spId, string spNum, string name, string bar,
            double income, double score, string pri, int dep,
            string reason, string notes, string by)
        {
            var existing = DisqualifiedCandidates.FirstOrDefault(x => IsSameCandidate(x.EvaluationId, x.SoloParentId, x.SpId, evalId, spId, spNum));
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
                    Index = DisqualifiedCandidates.Count + 1,
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
                _totalDisqualifiedCount++;
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
                $"Are you sure you want to reinstate {item.DisplayName}?\n\nThis will remove their statutory disqualification and restore them to the active recommendations queue.",
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
                if (_totalDisqualifiedCount > 0) _totalDisqualifiedCount--;
                UpdateDisqualifiedPaged();
                NotifyDisqualifiedMetricsChanged();
                var orig = _allItems.FirstOrDefault(x => IsSameCandidate(x.EvaluationId, x.SoloParentId, x.SpId, item.EvaluationId, item.SoloParentId, item.SpId));
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
                ToastNotification.Show("Candidate Reinstated", $"{item.DisplayName} has been restored to the recommendations queue.", ToastType.Success);
            }
            catch (Exception ex)
            {
                Views.Dialogs.ActionConfirmDialog.Error("Reinstatement Error", ex.Message);
            }
        }

        public async Task ExportDisqualifiedCsvAsync()
        {
            var itemsToExport = FilteredDisqualifiedCandidates.ToList();
            if (itemsToExport.Count == 0)
            {
                Views.Dialogs.ActionConfirmDialog.Warning("No Records", "No disqualified candidate records match the current filter.");
                return;
            }
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Disqualified_Candidates_Audit_FY{_activeCycleFiscalYear}_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".xlsx",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx|CSV file (*.csv)|*.csv"
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    bool isExcel = dlg.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);
                    if (isExcel)
                    {
                        SubsidyExcelExportService.ExportDisqualifiedToExcel(dlg.FileName, itemsToExport, _activeCycleFiscalYear);
                    }
                    else
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("No.,Full Name,Barangay,Monthly Income,Vulnerability Score,Priority Level,Dependents,Disqualification Reason,Audit Justification Notes,Disqualified By,Timestamp");
                        int dqCount = 0;
                        foreach (var c in itemsToExport)
                        {
                            sb.AppendLine($"{++dqCount},{FormHelper.SanitizeCsvField(c.FullName)},{FormHelper.SanitizeCsvField(c.Barangay)},\"{c.MonthlyIncome:F2}\",\"{c.PriorityScore:F2}\",{FormHelper.SanitizeCsvField(c.Priority)},\"{c.Dependents}\",{FormHelper.SanitizeCsvField(c.RejectionReason)},{FormHelper.SanitizeCsvField(c.RejectionNotes)},{FormHelper.SanitizeCsvField(c.DisqualifiedBy)},{FormHelper.SanitizeCsvField(c.FormattedDisqualifiedAt)}");
                        }
                        System.IO.File.WriteAllText(dlg.FileName, sb.ToString(), new System.Text.UTF8Encoding(true));
                    }

                    string fileType = isExcel ? "Excel" : "CSV";
                    ToastNotification.Show(
                        "Export Complete",
                        $"Exported {itemsToExport.Count} disqualified candidate record(s) to {fileType}: {System.IO.Path.GetFileName(dlg.FileName)}",
                        ToastType.Success);

                    PromptToOpenFile(dlg.FileName, fileType);
                }
                catch (Exception ex)
                {
                    Views.Dialogs.ActionConfirmDialog.Error("Export Error", "Export failed:\n" + ex.Message);
                }
            }
        }

        private async void ToggleGrantee(SelectionRow row, bool confirm)
        {
            if (row == null) return;
            if (confirm)
            {
                if (row.IsCardExpired)
                {
                    Views.Dialogs.ActionConfirmDialog.Error(
                        "Card Expired",
                        $"Cannot confirm {row.DisplayName} to Final Grantees because their Solo Parent ID expired on {row.CardValidityDisplay}. Please renew their card first.");
                    return;
                }

                bool ok = ActionConfirmDialog.Show(
                    "Confirm Final Grantee",
                    $"Confirm {row.DisplayName} as an official Final Grantee for FY {ActiveCycleFiscalYear}?",
                    "Confirm Grantee",
                    "Cancel",
                    ConfirmThemeType.Success);
                if (!ok) return;
            }
            else
            {
                bool ok = ActionConfirmDialog.Show(
                    "Revert Grantee",
                    $"Revert {row.DisplayName} back to pending selection audit?",
                    "Revert",
                    "Cancel",
                    ConfirmThemeType.Warning);
                if (!ok) return;
            }
            row.IsFinalGrantee = confirm;
            if (confirm && row.EvaluationId != Guid.Empty)
                await _apiService.ConfirmGranteesAsync(_activeCycleId, new List<Guid> { row.EvaluationId });
            IsDrawerOpen = false;
            ActiveCandidate = null;
            UpdateShortlistAndFinalCollections();
            NotifyQuotaMetricsChanged();
            if (confirm) ToastNotification.Show("Confirmed", $"{row.DisplayName} confirmed as final grantee.", ToastType.Success); else ToastNotification.Show("Unconfirmed", $"{row.DisplayName} reverted to pending shortlist.", ToastType.Info);
        }

        public async Task ExportCityEducRosterAsync()
        {
            var flagged = ShortlistRows.Where(i => i.HasCollegeAgeDependent).ToList();
            if (flagged.Count == 0)
            {
                Views.Dialogs.ActionConfirmDialog.Warning("No Flagged Records", "No applicants in the shortlist have college-age dependents (17–24 yrs).");
                return;
            }

            int yr = SelectedFiscalCycle?.FiscalYear ?? ActiveCycleFiscalYear;
            var dlg = new SaveFileDialog
            {
                FileName = $"CityEduc_College_Dependents_FY{yr}_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".pdf",
                Filter = "PDF Document (*.pdf)|*.pdf|Excel Workbook (*.xlsx)|*.xlsx"
            };

            if (dlg.ShowDialog() != true) return;

            IsActionProcessing = true;
            ActionProcessingMessage = "Preparing City Educ roster export...";
            try
            {
                // Ensure profile enrichment for any un-enriched flagged candidates
                foreach (var r in flagged.Where(i => i.CollegeAgeDependents == null || i.CollegeAgeDependents.Count == 0))
                {
                    if (r.SoloParentId != Guid.Empty)
                    {
                        try
                        {
                            var resp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(r.SoloParentId);
                            if (resp?.Data != null) SubsidyModelMapper.EnrichRowFromRecord(r, resp.Data);
                        }
                        catch { }
                    }
                }

                bool isPdf = dlg.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
                if (isPdf)
                {
                    SubsidyPdfExportService.ExportCityEducRosterToPdf(dlg.FileName, ShortlistRows.ToList(), yr);
                }
                else
                {
                    SubsidyExcelExportService.ExportCityEducRosterToExcel(dlg.FileName, ShortlistRows.ToList(), yr);
                }

                ToastNotification.Show(
                    "Export Complete",
                    $"{flagged.Count} candidate(s) exported for City Educ cross-check. Saved to: {Path.GetFileName(dlg.FileName)}",
                    ToastType.Success);

                PromptToOpenFile(dlg.FileName, isPdf ? "PDF" : "Excel");
            }
            catch (Exception ex)
            {
                Views.Dialogs.ActionConfirmDialog.Error("Export Error", "Export failed:\n" + ex.Message);
            }
            finally
            {
                IsActionProcessing = false;
            }
        }

        public async Task ExportPantawid4PsRosterAsync()
        {
            var list = ShortlistRows.Where(i => i.IsPantawidBeneficiary).ToList();
            if (list.Count == 0)
            {
                list = _allItems.Where(i => i.IsPantawidBeneficiary).Select(it => new SelectionRow
                {
                    Rank = it.Rank,
                    SoloParentId = it.SoloParentId,
                    SpId = it.SpId,
                    Name = it.Name,
                    Barangay = it.Barangay,
                    MonthlyIncome = it.MonthlyIncome,
                    IncomePerCapita = it.IncomePerCapita,
                    Dependants = it.Dependants,
                    MinorDependentsCount = it.MinorDependentsCount,
                    Circumstance = it.Circumstance,
                    IsPantawidBeneficiary = true
                }).ToList();
            }

            if (list.Count == 0)
            {
                Views.Dialogs.ActionConfirmDialog.Warning("No Records", "No applicants are tagged as 4Ps beneficiaries.");
                return;
            }

            int yr = SelectedFiscalCycle?.FiscalYear ?? ActiveCycleFiscalYear;
            var dlg = new SaveFileDialog
            {
                FileName = $"DSWD_4Ps_Beneficiaries_FY{yr}_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".pdf",
                Filter = "PDF Document (*.pdf)|*.pdf|Excel Workbook (*.xlsx)|*.xlsx"
            };

            if (dlg.ShowDialog() != true) return;

            IsActionProcessing = true;
            ActionProcessingMessage = "Preparing DSWD 4Ps roster export...";
            try
            {
                // Ensure profile enrichment for any un-enriched 4Ps candidates
                foreach (var r in list)
                {
                    if (r.SoloParentId != Guid.Empty && (string.IsNullOrEmpty(r.Address) || string.IsNullOrEmpty(r.ContactNumber) || string.IsNullOrEmpty(r.DateOfBirthFormatted) || r.DateOfBirthFormatted == "—" || string.IsNullOrEmpty(r.PhilSysCardNumber) || r.PhilSysCardNumber == "—"))
                    {
                        try
                        {
                            var resp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(r.SoloParentId);
                            if (resp?.Data != null) SubsidyModelMapper.EnrichRowFromRecord(r, resp.Data);
                        }
                        catch { }
                    }
                }

                bool isPdf = dlg.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
                if (isPdf)
                {
                    SubsidyPdfExportService.ExportPantawid4PsRosterToPdf(dlg.FileName, list, yr);
                }
                else
                {
                    SubsidyExcelExportService.ExportPantawid4PsRosterToExcel(dlg.FileName, list, yr);
                }

                ToastNotification.Show(
                    "Export Complete",
                    $"{list.Count} 4Ps candidate(s) exported successfully. Saved to: {Path.GetFileName(dlg.FileName)}",
                    ToastType.Success);

                PromptToOpenFile(dlg.FileName, isPdf ? "PDF" : "Excel");
            }
            catch (Exception ex)
            {
                Views.Dialogs.ActionConfirmDialog.Error("Export Error", "Export failed:\n" + ex.Message);
            }
            finally
            {
                IsActionProcessing = false;
            }
        }

        public async Task ExportFinalLedgerRosterAsync()
        {
            if (FinalGranteesList.Count == 0)
            {
                Views.Dialogs.ActionConfirmDialog.Warning("Ledger Empty", "No confirmed grantees to export.");
                return;
            }

            int yr = SelectedFiscalCycle?.FiscalYear ?? ActiveCycleFiscalYear;
            decimal grantAmount = GrantAmountPerRecipient > 0 ? (decimal)GrantAmountPerRecipient : 1000m;

            var dlg = new SaveFileDialog
            {
                FileName = $"Final_Grantee_Disbursement_Ledger_FY{yr}_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".pdf",
                Filter = "PDF Document (*.pdf)|*.pdf|Excel Workbook (*.xlsx)|*.xlsx"
            };

            if (dlg.ShowDialog() != true) return;

            IsActionProcessing = true;
            ActionProcessingMessage = "Preparing final grantee disbursement ledger export...";
            try
            {
                var items = FinalGranteesList.ToList();
                // Ensure profile enrichment
                foreach (var r in items)
                {
                    if (r.SoloParentId != Guid.Empty && (string.IsNullOrEmpty(r.Address) || string.IsNullOrEmpty(r.ContactNumber) || string.IsNullOrEmpty(r.DateOfBirthFormatted) || r.DateOfBirthFormatted == "—" || string.IsNullOrEmpty(r.PhilSysCardNumber) || r.PhilSysCardNumber == "—"))
                    {
                        try
                        {
                            var resp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(r.SoloParentId);
                            if (resp?.Data != null) SubsidyModelMapper.EnrichRowFromRecord(r, resp.Data);
                        }
                        catch { }
                    }
                }

                bool isPdf = dlg.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
                if (isPdf)
                {
                    SubsidyPdfExportService.ExportFinalLedgerToPdf(dlg.FileName, items, yr, grantAmount);
                }
                else
                {
                    SubsidyExcelExportService.ExportFinalLedgerToExcel(dlg.FileName, items, yr, grantAmount);
                }

                ToastNotification.Show(
                    "Export Complete",
                    $"Final grantee disbursement ledger for FY {yr} exported successfully. Saved to: {Path.GetFileName(dlg.FileName)}",
                    ToastType.Success);

                PromptToOpenFile(dlg.FileName, isPdf ? "PDF" : "Excel");
            }
            catch (Exception ex)
            {
                Views.Dialogs.ActionConfirmDialog.Error("Export Error", "Export failed:\n" + ex.Message);
            }
            finally
            {
                IsActionProcessing = false;
            }
        }

        private static void PromptToOpenFile(string filePath, string fileType)
        {
            var openDialog = new Views.Dialogs.OpenFilePromptDialog($"Would you like to open the exported {fileType} file now?")
            {
                Owner = Application.Current.MainWindow
            };

            if (openDialog.ShowDialog() == true && openDialog.ShouldOpenFile)
            {
                try
                {
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(filePath)
                        { UseShellExecute = true });
                }
                catch { }
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
                if (dlg.OpenedRenew)
                {
                    await RunQuickPredictAsync(item);
                }
                else if (dlg.Renewed || dlg.OpenedEdit || dlg.RenewalResult != null)
                {
                    await RunQuickPredictAsync(item);
                }
            }
            catch (Exception ex) { ToastNotification.Show("Unable to Open Record", ex.Message, ToastType.Error); }
        }

        private async Task RunQuickPredictAsync(SubsidyItem item)
        {
            if (item == null || IsReassessingCandidate) return;
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
                    ToastNotification.Show("Re-assessment Cooldown", $"Please wait {rem}s before re-assessing {item.DisplayName} again.", ToastType.Warning);
                    return;
                }
            }
            IsReassessingCandidate = true;
            try
            {
                var resp = await _apiService.PredictCandidateAsync(targetId);
                if (resp != null && resp.Succeeded && resp.Data != null && resp.Data.SoloParentId != Guid.Empty)
                {
                    // Record cooldown timestamp
                    RecordReassessment(targetId);

                    if (string.Equals(resp.Data.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
                    {
                        IsDrawerOpen = false;
                        ActiveCandidate = null;
                        await RefreshQueueWithRetainedCountAsync();
                        string dqReason = !string.IsNullOrWhiteSpace(resp.Data.RejectionReason)
                            ? resp.Data.RejectionReason
                            : "Statutory Disqualification";
                        ToastNotification.Show(
                            "Moved to Disqualified",
                            $"{item.DisplayName} was deterministically disqualified during reassessment ({dqReason}).",
                            ToastType.Warning);
                        return;
                    }

                    SubsidyModelMapper.UpdateItemFromDto(item, resp.Data);
                    item.RefreshVisuals();
                    // Re-enrich with details like children items
                    await FetchRecordForDrawerAsync(item);

                    // Check if candidate is currently in ShortlistRows (Approved Selection)
                    var shortlistRow = ShortlistRows.FirstOrDefault(x => IsSameCandidate(x.EvaluationId, x.SoloParentId, x.SpId, item.EvaluationId, targetId, item.SpId));
                    if (shortlistRow != null)
                    {
                        shortlistRow.Score = item.Score;
                        shortlistRow.Priority = item.Priority;
                        shortlistRow.Circumstance = item.Circumstance;
                        shortlistRow.MonthlyIncome = item.MonthlyIncome;
                        shortlistRow.IncomePerCapita = item.IncomePerCapita;
                        shortlistRow.Dependants = item.Dependants;
                        shortlistRow.MinorDependentsCount = item.MinorDependentsCount;
                        shortlistRow.ToddlersUnder5Count = item.ToddlersUnder5Count;
                        shortlistRow.HasSpecialNeedsDependent = item.HasSpecialNeedsDependent;
                        shortlistRow.SpecialNeedsDependentsCount = item.SpecialNeedsDependentsCount;
                        shortlistRow.HasCollegeAgeDependent = item.HasCollegeAgeDependent;
                        shortlistRow.CollegeAgeDependentsCount = item.CollegeAgeDependentsCount;
                        shortlistRow.IsPantawidBeneficiary = item.IsPantawidBeneficiary;
                        shortlistRow.ModelVersion = item.ModelVersion;
                        shortlistRow.NeedsAndProblems = item.NeedsAndProblems;
                        shortlistRow.OtherIncomeSource = item.OtherIncomeSource;
                        if (item.ChildrenDetails != null && item.ChildrenDetails.Count > 0)
                            shortlistRow.ChildrenDetails = item.ChildrenDetails;
                        UpdateShortlistAndFinalCollections(enrich: false);
                        ActiveCandidate = item;
                        OnPropertyChanged(nameof(ActiveCandidate));
                    }
                    else
                    {
                        // Refresh queue with retained count so rankings are restarted from #1 and user stays at their loaded depth
                        await RefreshQueueWithRetainedCountAsync();
                        if (ActiveCandidate != null && ActiveCandidate.SoloParentId == targetId)
                        {
                            item.Rank = ActiveCandidate.Rank;
                            await FetchRecordForDrawerAsync(ActiveCandidate);
                            ActiveCandidate.RefreshVisuals();
                            OnPropertyChanged(nameof(ActiveCandidate));
                        }
                    }

                    ToastNotification.Show("Prediction Updated",
                        $"{item.DisplayName} re-scored: {item.Score:F1} ({item.Priority}) via {resp.Data.ModelVersion}. Current rank: #{item.Rank}.",
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
            finally
            {
                IsReassessingCandidate = false;
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
                    ToastNotification.Show("Re-assessment Cooldown", $"Please wait {rem}s before re-assessing {item.DisplayName} again.", ToastType.Warning);
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
                        ToastNotification.Show("Record Updated", $"{item.DisplayName} circumstances updated successfully.", ToastType.Success);
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
                IsDrawerLoading = true;
                var resp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(item.SoloParentId);
                if (resp?.Data != null)
                    Application.Current?.Dispatcher?.Invoke(() => { SubsidyModelMapper.EnrichItemFromRecord(item, resp.Data); if (ActiveCandidate == item) OnPropertyChanged(nameof(ActiveCandidate)); });
            }
            catch { }
            finally
            {
                IsDrawerLoading = false;
            }
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