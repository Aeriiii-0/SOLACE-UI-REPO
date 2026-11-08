using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SOLUM_UI.Core;
using SOLUM_UI.DTOs;
using SOLUM_UI.Models;
using SOLUM_UI.Services;

namespace SOLUM_UI.ViewModels
{
    public partial class SubsidyRecommendationViewModel
    {
        public ObservableCollection<SubsidyItem> PagedRecommendations { get; private set; }
        public ObservableCollection<SelectionRow> PagedShortlistRows { get; private set; }
        public ObservableCollection<SelectionRow> PagedFinalGrantees { get; private set; }

        // Dynamic batch sizes - default 30 for smooth initial vertical scroll room
        private int _queuePageSize = 30;
        public int QueuePageSize
        {
            get => _queuePageSize;
            set
            {
                if (value >= 1 && SetProperty(ref _queuePageSize, value))
                {
                    OnPropertyChanged(nameof(QueuePageSummary));
                    OnPropertyChanged(nameof(CanQueueLoadMore));
                }
            }
        }

        private int _shortlistPageSize = 30;
        public int ShortlistPageSize
        {
            get => _shortlistPageSize;
            set
            {
                if (value >= 1 && SetProperty(ref _shortlistPageSize, value))
                {
                    UpdateShortlistPaged();
                    OnPropertyChanged(nameof(CanShortlistLoadMore));
                }
            }
        }

        private int _finalPageSize = 50;
        public int FinalPageSize
        {
            get => _finalPageSize;
            set
            {
                if (value >= 1 && SetProperty(ref _finalPageSize, value))
                {
                    UpdateFinalPaged();
                }
            }
        }

        public async Task UpdateQueuePageSizeAsync(int newSize)
        {
            if (newSize < 1 || newSize == _queuePageSize) return;
            _queuePageSize = newSize;
            OnPropertyChanged(nameof(QueuePageSize));
            OnPropertyChanged(nameof(QueuePageSummary));
            OnPropertyChanged(nameof(CanQueueLoadMore));
            if (!_activeCycleId.HasValue || _activeCycleId.Value == Guid.Empty) return;
            await LoadQueuePageAsync(1);
        }

        public void UpdateShortlistPageSize(int newSize)
        {
            if (newSize < 1 || newSize == _shortlistPageSize) return;
            _shortlistPageSize = newSize;
            OnPropertyChanged(nameof(ShortlistPageSize));
            OnPropertyChanged(nameof(CanShortlistLoadMore));
            _ = LoadShortlistPageAsync(1);
        }

        public void UpdateFinalPageSize(int newSize)
        {
            if (newSize < 1 || newSize == _finalPageSize) return;
            _finalPageSize = newSize;
            OnPropertyChanged(nameof(FinalPageSize));
            _ = LoadFinalPageAsync(1);
        }

        // Tab 0: Recommendations Queue Infinite Scrolling State
        private int _queueCurrentPage = 1;
        public int QueueCurrentPage
        {
            get => _queueCurrentPage;
            set
            {
                if (SetProperty(ref _queueCurrentPage, value))
                {
                    OnPropertyChanged(nameof(CanQueuePrevPage));
                    OnPropertyChanged(nameof(CanQueueNextPage));
                    OnPropertyChanged(nameof(CanQueueLoadMore));
                    OnPropertyChanged(nameof(QueuePageNumberLabel));
                    OnPropertyChanged(nameof(QueuePageSummary));
                }
            }
        }

        private int _queueTotalPages = 1;
        public int QueueTotalPages
        {
            get => _queueTotalPages;
            private set
            {
                if (SetProperty(ref _queueTotalPages, value))
                {
                    OnPropertyChanged(nameof(CanQueuePrevPage));
                    OnPropertyChanged(nameof(CanQueueNextPage));
                    OnPropertyChanged(nameof(CanQueueLoadMore));
                    OnPropertyChanged(nameof(QueuePageNumberLabel));
                }
            }
        }

        private int _queueTotalCount = 0;
        public int QueueTotalCount
        {
            get => _queueTotalCount;
            set
            {
                if (SetProperty(ref _queueTotalCount, value))
                {
                    OnPropertyChanged(nameof(QueuePageSummary));
                    OnPropertyChanged(nameof(RecommendationsSummary));
                    OnPropertyChanged(nameof(CanQueueLoadMore));
                }
            }
        }

        private bool _isLoadingMoreQueue = false;
        public bool IsLoadingMoreQueue
        {
            get => _isLoadingMoreQueue;
            private set
            {
                if (SetProperty(ref _isLoadingMoreQueue, value))
                {
                    OnPropertyChanged(nameof(CanQueueLoadMore));
                }
            }
        }

        public bool CanQueueLoadMore => !_isLoadingMoreQueue && (
            QueueCurrentPage < QueueTotalPages ||
            (FilteredRecommendations != null && PagedRecommendations.Count < FilteredRecommendations.Count)
        );

        public bool CanQueuePrevPage => QueueCurrentPage > 1;
        public bool CanQueueNextPage => QueueCurrentPage < QueueTotalPages;

        public string QueuePageSummary
        {
            get
            {
                int total = QueueTotalCount > 0 ? QueueTotalCount : (FilteredRecommendations?.Count ?? 0);
                if (total == 0) return "No candidate records found";
                int loaded = PagedRecommendations?.Count ?? 0;
                if (loaded >= total) return $"All {total} candidates loaded";
                return $"Showing {loaded} of {total} candidates";
            }
        }

        public string QueuePageNumberLabel => $"{QueueCurrentPage} / {QueueTotalPages}";
        public string RecommendationsSummary => $"{QueueTotalCount} Candidates Evaluated ({Math.Min(GranteeSlots, QueueTotalCount)} Within Quota)";

        public ICommand QueuePrevPageCommand { get; private set; }
        public ICommand QueueNextPageCommand { get; private set; }
        public ICommand LoadNextQueuePageCommand { get; private set; }

        // Tab 1: Shortlist Audit Infinite Scrolling State
        private int _shortlistCurrentPage = 1;
        public int ShortlistCurrentPage
        {
            get => _shortlistCurrentPage;
            set
            {
                if (SetProperty(ref _shortlistCurrentPage, value))
                {
                    OnPropertyChanged(nameof(CanShortlistPrevPage));
                    OnPropertyChanged(nameof(CanShortlistNextPage));
                    OnPropertyChanged(nameof(CanShortlistLoadMore));
                    OnPropertyChanged(nameof(ShortlistPageSummary));
                    OnPropertyChanged(nameof(ShortlistPageNumberLabel));
                }
            }
        }

        public int ShortlistTotalPages { get; private set; } = 1;
        public bool CanShortlistPrevPage => ShortlistCurrentPage > 1;
        public bool CanShortlistNextPage => ShortlistCurrentPage < ShortlistTotalPages;

        private bool _isLoadingMoreShortlist = false;
        public bool IsLoadingMoreShortlist
        {
            get => _isLoadingMoreShortlist;
            private set
            {
                if (SetProperty(ref _isLoadingMoreShortlist, value))
                {
                    OnPropertyChanged(nameof(CanShortlistLoadMore));
                }
            }
        }

        public bool CanShortlistLoadMore => !_isLoadingMoreShortlist && (
            ShortlistCurrentPage < ShortlistTotalPages ||
            (FilteredShortlistRows != null && PagedShortlistRows.Count < FilteredShortlistRows.Count)
        );

        public string ShortlistPageSummary
        {
            get
            {
                int total = FilteredShortlistRows?.Count ?? 0;
                if (total == 0) return "No shortlist records found";
                int loaded = PagedShortlistRows?.Count ?? 0;
                if (loaded >= total) return $"All {total} records loaded";
                return $"Showing {loaded} of {total} records";
            }
        }

        public string ShortlistPageNumberLabel => $"{ShortlistCurrentPage} / {ShortlistTotalPages}";

        public ICommand ShortlistPrevPageCommand { get; private set; }
        public ICommand ShortlistNextPageCommand { get; private set; }
        public ICommand LoadNextShortlistPageCommand { get; private set; }

        // Tab 2: Final Grantees Pagination
        private int _finalCurrentPage = 1;
        public int FinalCurrentPage
        {
            get => _finalCurrentPage;
            set { if (SetProperty(ref _finalCurrentPage, value)) UpdateFinalPaged(); }
        }
        public int FinalTotalPages { get; private set; } = 1;
        public bool CanFinalPrevPage => FinalCurrentPage > 1;
        public bool CanFinalNextPage => FinalCurrentPage < FinalTotalPages;
        public string FinalPageSummary
        {
            get
            {
                int total = FinalGranteesList?.Count ?? 0;
                if (total == 0) return "No grantee records found";
                int start = (FinalCurrentPage - 1) * FinalPageSize + 1;
                int end = Math.Min(FinalCurrentPage * FinalPageSize, total);
                return $"Showing {start} - {end} of {total} records";
            }
        }
        public string FinalPageNumberLabel => $"{FinalCurrentPage} / {FinalTotalPages}";

        public ICommand FinalPrevPageCommand { get; private set; }
        public ICommand FinalNextPageCommand { get; private set; }

        private void InitPagination()
        {
            PagedRecommendations = new ObservableCollection<SubsidyItem>();
            PagedShortlistRows = new ObservableCollection<SelectionRow>();
            PagedFinalGrantees = new ObservableCollection<SelectionRow>();

            QueuePrevPageCommand = new RelayCommand(async _ => { if (CanQueuePrevPage) await LoadQueuePageAsync(QueueCurrentPage - 1); });
            QueueNextPageCommand = new RelayCommand(async _ => { if (CanQueueLoadMore) await LoadNextQueuePageAsync(); });
            LoadNextQueuePageCommand = new RelayCommand(async _ => { if (CanQueueLoadMore) await LoadNextQueuePageAsync(); });

            ShortlistPrevPageCommand = new RelayCommand(async _ => { if (CanShortlistPrevPage) await LoadShortlistPageAsync(ShortlistCurrentPage - 1); });
            ShortlistNextPageCommand = new RelayCommand(async _ => { if (CanShortlistLoadMore) await LoadNextShortlistPageAsync(); });
            LoadNextShortlistPageCommand = new RelayCommand(async _ => { if (CanShortlistLoadMore) await LoadNextShortlistPageAsync(); });

            FinalPrevPageCommand = new RelayCommand(async _ => { if (CanFinalPrevPage) await LoadFinalPageAsync(FinalCurrentPage - 1); });
            FinalNextPageCommand = new RelayCommand(async _ => { if (CanFinalNextPage) await LoadFinalPageAsync(FinalCurrentPage + 1); });
        }

        public async Task LoadQueuePageAsync(int page)
        {
            if (page < 1) page = 1;
            QueueCurrentPage = page;
            var cycleId = SelectedFiscalCycle?.CycleId ?? _activeCycleId;

            if (cycleId.HasValue && !IsRosterLocked)
            {
                try
                {
                    var req = new GetSubsidyRecommendationsRequest
                    {
                        CycleId = cycleId.Value,
                        Page = QueueCurrentPage,
                        PageSize = QueuePageSize,
                        Search = _queueSearchText,
                        SortBy = "PriorityScore",
                        SortOrder = "desc"
                    };
                    var resp = await _apiService.GetRecommendationsPagedAsync(req);
                    if (resp?.Data != null && resp.Data.Items != null && resp.Data.Items.Count > 0)
                    {
                        QueueTotalPages = Math.Max(1, resp.Data.TotalPages);
                        QueueTotalCount = resp.Data.TotalCount;
                        PagedRecommendations.Clear();
                        int rank = (QueueCurrentPage - 1) * QueuePageSize + 1;
                        foreach (var d in resp.Data.Items)
                        {
                            var item = SubsidyModelMapper.ToItem(d);
                            item.Rank = rank++;
                            item.IsWithinQuota = (item.Rank <= _granteeSlots);
                            if (FilteredRecommendations != null)
                            {
                                var existing = FilteredRecommendations.FirstOrDefault(x => x.SoloParentId == item.SoloParentId);
                                if (existing != null) item.IsSelected = existing.IsSelected;
                            }
                            PagedRecommendations.Add(item);
                        }
                        OnPropertyChanged(nameof(QueueCurrentPage));
                        OnPropertyChanged(nameof(QueueTotalPages));
                        OnPropertyChanged(nameof(CanQueuePrevPage));
                        OnPropertyChanged(nameof(CanQueueNextPage));
                        OnPropertyChanged(nameof(CanQueueLoadMore));
                        OnPropertyChanged(nameof(QueuePageSummary));
                        OnPropertyChanged(nameof(QueuePageNumberLabel));
                        OnPropertyChanged(nameof(RecommendationsSummary));
                        EnrichVisibleCandidatesAsync();
                        return;
                    }
                }
                catch { }
            }

            UpdateQueuePaged();
            EnrichVisibleCandidatesAsync();
        }

        public async Task LoadNextQueuePageAsync()
        {
            if (_isLoadingMoreQueue) return;
            if (!CanQueueLoadMore) return;

            IsLoadingMoreQueue = true;
            try
            {
                int nextPage = QueueCurrentPage + 1;
                var cycleId = SelectedFiscalCycle?.CycleId ?? _activeCycleId;

                if (cycleId.HasValue && !IsRosterLocked)
                {
                    var req = new GetSubsidyRecommendationsRequest
                    {
                        CycleId = cycleId.Value,
                        Page = nextPage,
                        PageSize = QueuePageSize,
                        Search = _queueSearchText,
                        SortBy = "PriorityScore",
                        SortOrder = "desc"
                    };
                    var resp = await _apiService.GetRecommendationsPagedAsync(req);
                    if (resp?.Data != null && resp.Data.Items != null && resp.Data.Items.Count > 0)
                    {
                        QueueCurrentPage = nextPage;
                        QueueTotalPages = Math.Max(1, resp.Data.TotalPages);
                        QueueTotalCount = resp.Data.TotalCount;
                        int rank = PagedRecommendations.Count + 1;
                        foreach (var d in resp.Data.Items)
                        {
                            var item = SubsidyModelMapper.ToItem(d);
                            item.Rank = rank++;
                            item.IsWithinQuota = (item.Rank <= _granteeSlots);
                            if (!PagedRecommendations.Any(x => x.SoloParentId == item.SoloParentId))
                            {
                                if (FilteredRecommendations != null)
                                {
                                    var existing = FilteredRecommendations.FirstOrDefault(x => x.SoloParentId == item.SoloParentId);
                                    if (existing != null) item.IsSelected = existing.IsSelected;
                                }
                                PagedRecommendations.Add(item);
                            }
                        }
                        OnPropertyChanged(nameof(QueuePageSummary));
                        OnPropertyChanged(nameof(RecommendationsSummary));
                        EnrichVisibleCandidatesAsync();
                        return;
                    }
                }

                // In-memory fallback
                if (FilteredRecommendations != null && FilteredRecommendations.Count > PagedRecommendations.Count)
                {
                    QueueCurrentPage = nextPage;
                    var slice = FilteredRecommendations.Skip(PagedRecommendations.Count).Take(QueuePageSize);
                    foreach (var item in slice)
                    {
                        if (!PagedRecommendations.Any(x => x.SoloParentId == item.SoloParentId))
                        {
                            PagedRecommendations.Add(item);
                        }
                    }
                    OnPropertyChanged(nameof(QueuePageSummary));
                    EnrichVisibleCandidatesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Subsidy] LoadNextQueuePageAsync error: " + ex.Message);
            }
            finally
            {
                IsLoadingMoreQueue = false;
            }
        }

        public async Task LoadShortlistPageAsync(int page)
        {
            if (page < 1) page = 1;
            ShortlistCurrentPage = page;
            var cycleId = SelectedFiscalCycle?.CycleId ?? _activeCycleId;

            if (cycleId.HasValue && !IsRosterLocked)
            {
                try
                {
                    var req = new GetSubsidyShortlistRequest
                    {
                        CycleId = cycleId.Value,
                        Page = ShortlistCurrentPage,
                        PageSize = ShortlistPageSize,
                        Status = _shortlistAuditFilter
                    };
                    var resp = await _apiService.GetShortlistPagedAsync(req);
                    if (resp?.Data != null && resp.Data.Items != null && resp.Data.Items.Count > 0)
                    {
                        ShortlistTotalPages = Math.Max(1, resp.Data.TotalPages);
                        PagedShortlistRows.Clear();
                        int rank = (ShortlistCurrentPage - 1) * ShortlistPageSize + 1;
                        foreach (var d in resp.Data.Items)
                        {
                            var row = SubsidyModelMapper.ToRow(d, rank++);
                            if (FilteredShortlistRows != null)
                            {
                                var existing = FilteredShortlistRows.FirstOrDefault(x => x.SoloParentId == row.SoloParentId);
                                if (existing != null) row.IsSelected = existing.IsSelected;
                            }
                            PagedShortlistRows.Add(row);
                        }
                        OnPropertyChanged(nameof(ShortlistCurrentPage));
                        OnPropertyChanged(nameof(ShortlistTotalPages));
                        OnPropertyChanged(nameof(CanShortlistPrevPage));
                        OnPropertyChanged(nameof(CanShortlistNextPage));
                        OnPropertyChanged(nameof(CanShortlistLoadMore));
                        OnPropertyChanged(nameof(ShortlistPageSummary));
                        OnPropertyChanged(nameof(ShortlistPageNumberLabel));
                        EnrichVisibleCandidatesAsync();
                        return;
                    }
                }
                catch { }
            }

            UpdateShortlistPaged();
            EnrichVisibleCandidatesAsync();
        }

        public async Task LoadNextShortlistPageAsync()
        {
            if (_isLoadingMoreShortlist) return;
            if (!CanShortlistLoadMore) return;

            IsLoadingMoreShortlist = true;
            try
            {
                int nextPage = ShortlistCurrentPage + 1;
                var cycleId = SelectedFiscalCycle?.CycleId ?? _activeCycleId;

                if (cycleId.HasValue && !IsRosterLocked)
                {
                    var req = new GetSubsidyShortlistRequest
                    {
                        CycleId = cycleId.Value,
                        Page = nextPage,
                        PageSize = ShortlistPageSize,
                        Status = _shortlistAuditFilter
                    };
                    var resp = await _apiService.GetShortlistPagedAsync(req);
                    if (resp?.Data != null && resp.Data.Items != null && resp.Data.Items.Count > 0)
                    {
                        ShortlistCurrentPage = nextPage;
                        ShortlistTotalPages = Math.Max(1, resp.Data.TotalPages);
                        int rank = PagedShortlistRows.Count + 1;
                        foreach (var d in resp.Data.Items)
                        {
                            var row = SubsidyModelMapper.ToRow(d, rank++);
                            if (!PagedShortlistRows.Any(x => x.SoloParentId == row.SoloParentId))
                            {
                                if (FilteredShortlistRows != null)
                                {
                                    var existing = FilteredShortlistRows.FirstOrDefault(x => x.SoloParentId == row.SoloParentId);
                                    if (existing != null) row.IsSelected = existing.IsSelected;
                                }
                                PagedShortlistRows.Add(row);
                            }
                        }
                        OnPropertyChanged(nameof(ShortlistPageSummary));
                        EnrichVisibleCandidatesAsync();
                        return;
                    }
                }

                // In-memory fallback
                if (FilteredShortlistRows != null && FilteredShortlistRows.Count > PagedShortlistRows.Count)
                {
                    ShortlistCurrentPage = nextPage;
                    var slice = FilteredShortlistRows.Skip(PagedShortlistRows.Count).Take(ShortlistPageSize);
                    foreach (var row in slice)
                    {
                        if (!PagedShortlistRows.Any(x => x.SoloParentId == row.SoloParentId))
                        {
                            PagedShortlistRows.Add(row);
                        }
                    }
                    OnPropertyChanged(nameof(ShortlistPageSummary));
                    EnrichVisibleCandidatesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Subsidy] LoadNextShortlistPageAsync error: " + ex.Message);
            }
            finally
            {
                IsLoadingMoreShortlist = false;
            }
        }

        public async Task LoadFinalPageAsync(int page)
        {
            if (page < 1) page = 1;
            FinalCurrentPage = page;
            var cycleId = SelectedFiscalCycle?.CycleId ?? _activeCycleId;
            int? yr = SelectedFiscalCycle?.FiscalYear ?? _activeCycleFiscalYear;
            int? cycleNum = SelectedFiscalCycle?.CycleNumber;

            try
            {
                var req = new GetGranteesRequest
                {
                    CycleId = cycleId,
                    FiscalYear = yr,
                    CycleNumber = cycleNum,
                    Page = FinalCurrentPage,
                    PageSize = FinalPageSize
                };
                var resp = await _apiService.GetGranteesPagedAsync(req);
                if (resp?.Data != null && resp.Data.Items != null && resp.Data.Items.Count > 0)
                {
                    FinalTotalPages = Math.Max(1, resp.Data.TotalPages);
                    PagedFinalGrantees.Clear();
                    int rank = (FinalCurrentPage - 1) * FinalPageSize + 1;
                    foreach (var g in resp.Data.Items)
                    {
                        PagedFinalGrantees.Add(SubsidyModelMapper.GranteeToRow(g, rank++));
                    }
                    OnPropertyChanged(nameof(FinalCurrentPage));
                    OnPropertyChanged(nameof(FinalTotalPages));
                    OnPropertyChanged(nameof(CanFinalPrevPage));
                    OnPropertyChanged(nameof(CanFinalNextPage));
                    OnPropertyChanged(nameof(FinalPageSummary));
                    OnPropertyChanged(nameof(FinalPageNumberLabel));
                    EnrichVisibleCandidatesAsync();
                    return;
                }
            }
            catch { }

            UpdateFinalPaged();
            EnrichVisibleCandidatesAsync();
        }

        public void UpdateQueuePaged()
        {
            int total = FilteredRecommendations?.Count ?? 0;
            QueueTotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)QueuePageSize));
            if (_queueCurrentPage > QueueTotalPages) _queueCurrentPage = QueueTotalPages;
            if (_queueCurrentPage < 1) _queueCurrentPage = 1;
            PagedRecommendations.Clear();
            if (FilteredRecommendations != null)
            {
                int countToTake = Math.Min(total, QueuePageSize * _queueCurrentPage);
                var slice = FilteredRecommendations.Take(countToTake > 0 ? countToTake : QueuePageSize);
                foreach (var item in slice) PagedRecommendations.Add(item);
            }
            QueueTotalCount = total;
            OnPropertyChanged(nameof(QueueCurrentPage));
            OnPropertyChanged(nameof(QueueTotalPages));
            OnPropertyChanged(nameof(CanQueuePrevPage));
            OnPropertyChanged(nameof(CanQueueNextPage));
            OnPropertyChanged(nameof(CanQueueLoadMore));
            OnPropertyChanged(nameof(QueuePageSummary));
            OnPropertyChanged(nameof(QueuePageNumberLabel));
            OnPropertyChanged(nameof(RecommendationsSummary));
        }

        public void UpdateShortlistPaged()
        {
            int total = FilteredShortlistRows?.Count ?? 0;
            ShortlistTotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)ShortlistPageSize));
            if (_shortlistCurrentPage > ShortlistTotalPages) _shortlistCurrentPage = ShortlistTotalPages;
            if (_shortlistCurrentPage < 1) _shortlistCurrentPage = 1;
            PagedShortlistRows.Clear();
            if (FilteredShortlistRows != null)
            {
                int countToTake = Math.Min(total, ShortlistPageSize * _shortlistCurrentPage);
                var slice = FilteredShortlistRows.Take(countToTake > 0 ? countToTake : ShortlistPageSize);
                foreach (var item in slice) PagedShortlistRows.Add(item);
            }
            OnPropertyChanged(nameof(ShortlistCurrentPage));
            OnPropertyChanged(nameof(ShortlistTotalPages));
            OnPropertyChanged(nameof(CanShortlistPrevPage));
            OnPropertyChanged(nameof(CanShortlistNextPage));
            OnPropertyChanged(nameof(CanShortlistLoadMore));
            OnPropertyChanged(nameof(ShortlistPageSummary));
            OnPropertyChanged(nameof(ShortlistPageNumberLabel));
        }

        public void UpdateFinalPaged()
        {
            int total = FinalGranteesList?.Count ?? 0;
            FinalTotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)FinalPageSize));
            if (_finalCurrentPage > FinalTotalPages) _finalCurrentPage = FinalTotalPages;
            if (_finalCurrentPage < 1) _finalCurrentPage = 1;
            PagedFinalGrantees.Clear();
            if (FinalGranteesList != null)
            {
                var slice = FinalGranteesList.Skip((_finalCurrentPage - 1) * FinalPageSize).Take(FinalPageSize);
                foreach (var item in slice) PagedFinalGrantees.Add(item);
            }
            OnPropertyChanged(nameof(FinalCurrentPage));
            OnPropertyChanged(nameof(FinalTotalPages));
            OnPropertyChanged(nameof(CanFinalPrevPage));
            OnPropertyChanged(nameof(CanFinalNextPage));
            OnPropertyChanged(nameof(FinalPageSummary));
            OnPropertyChanged(nameof(FinalPageNumberLabel));
        }
    }
}