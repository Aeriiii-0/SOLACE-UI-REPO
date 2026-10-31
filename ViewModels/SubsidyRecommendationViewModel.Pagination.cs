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

        // Dynamic page sizes
        private int _queuePageSize = 10;
        public int QueuePageSize
        {
            get => _queuePageSize;
            set
            {
                if (value >= 1 && SetProperty(ref _queuePageSize, value))
                {
                    OnPropertyChanged(nameof(QueuePageSummary));
                }
            }
        }

        private int _shortlistPageSize = 10;
        public int ShortlistPageSize
        {
            get => _shortlistPageSize;
            set
            {
                if (value >= 1 && SetProperty(ref _shortlistPageSize, value))
                {
                    UpdateShortlistPaged();
                }
            }
        }

        private int _finalPageSize = 10;
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

            int oldPageSize = _queuePageSize;
            _queuePageSize = newSize;
            OnPropertyChanged(nameof(QueuePageSize));
            OnPropertyChanged(nameof(QueuePageSummary));

            if (!_activeCycleId.HasValue || _activeCycleId.Value == Guid.Empty) return;

            int firstVisibleItem = (_queueCurrentPage - 1) * oldPageSize + 1;
            int newPage = Math.Max(1, (int)Math.Ceiling(firstVisibleItem / (double)_queuePageSize));
            await LoadQueuePageAsync(newPage);
        }

        public void UpdateShortlistPageSize(int newSize)
        {
            if (newSize < 1 || newSize == _shortlistPageSize) return;

            int oldPageSize = _shortlistPageSize;
            _shortlistPageSize = newSize;
            OnPropertyChanged(nameof(ShortlistPageSize));

            int firstVisibleItem = (_shortlistCurrentPage - 1) * oldPageSize + 1;
            _shortlistCurrentPage = Math.Max(1, (int)Math.Ceiling(firstVisibleItem / (double)_shortlistPageSize));
            UpdateShortlistPaged();
        }

        public void UpdateFinalPageSize(int newSize)
        {
            if (newSize < 1 || newSize == _finalPageSize) return;

            int oldPageSize = _finalPageSize;
            _finalPageSize = newSize;
            OnPropertyChanged(nameof(FinalPageSize));

            int firstVisibleItem = (_finalCurrentPage - 1) * oldPageSize + 1;
            _finalCurrentPage = Math.Max(1, (int)Math.Ceiling(firstVisibleItem / (double)_finalPageSize));
            UpdateFinalPaged();
        }

        // Tab 0: Recommendations Queue Pagination (Server-Side)
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
                }
            }
        }

        public bool CanQueuePrevPage => QueueCurrentPage > 1;
        public bool CanQueueNextPage => QueueCurrentPage < QueueTotalPages;

        public string QueuePageSummary
        {
            get
            {
                int total = QueueTotalCount;
                if (total == 0) return "No candidate records found";
                int start = (QueueCurrentPage - 1) * QueuePageSize + 1;
                int end = Math.Min(QueueCurrentPage * QueuePageSize, total);
                return $"Showing {start}–{end} of {total} records";
            }
        }

        public string QueuePageNumberLabel => $"{QueueCurrentPage} / {QueueTotalPages}";
        public string RecommendationsSummary => $"{QueueTotalCount} Candidates Evaluated ({Math.Min(GranteeSlots, QueueTotalCount)} Within Quota)";

        public ICommand QueuePrevPageCommand { get; private set; }
        public ICommand QueueNextPageCommand { get; private set; }

        // Tab 1: Shortlist Audit Pagination
        private int _shortlistCurrentPage = 1;
        public int ShortlistCurrentPage
        {
            get => _shortlistCurrentPage;
            set { if (SetProperty(ref _shortlistCurrentPage, value)) UpdateShortlistPaged(); }
        }
        public int ShortlistTotalPages { get; private set; } = 1;
        public bool CanShortlistPrevPage => ShortlistCurrentPage > 1;
        public bool CanShortlistNextPage => ShortlistCurrentPage < ShortlistTotalPages;
        public string ShortlistPageSummary
        {
            get
            {
                int total = FilteredShortlistRows?.Count ?? 0;
                if (total == 0) return "No shortlist records found";
                int start = (ShortlistCurrentPage - 1) * ShortlistPageSize + 1;
                int end = Math.Min(ShortlistCurrentPage * ShortlistPageSize, total);
                return $"Showing {start}–{end} of {total} records";
            }
        }
        public string ShortlistPageNumberLabel => $"{ShortlistCurrentPage} / {ShortlistTotalPages}";

        public ICommand ShortlistPrevPageCommand { get; private set; }
        public ICommand ShortlistNextPageCommand { get; private set; }

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
                return $"Showing {start}–{end} of {total} records";
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
            QueueNextPageCommand = new RelayCommand(async _ => { if (CanQueueNextPage) await LoadQueuePageAsync(QueueCurrentPage + 1); });

            ShortlistPrevPageCommand = new RelayCommand(_ => { if (CanShortlistPrevPage) ShortlistCurrentPage--; });
            ShortlistNextPageCommand = new RelayCommand(_ => { if (CanShortlistNextPage) ShortlistCurrentPage++; });

            FinalPrevPageCommand = new RelayCommand(_ => { if (CanFinalPrevPage) FinalCurrentPage--; });
            FinalNextPageCommand = new RelayCommand(_ => { if (CanFinalNextPage) FinalCurrentPage++; });
        }

        public async Task LoadQueuePageAsync(int page)
        {
            if (page < 1) page = 1;
            IsLoading = true;
            try
            {
                var req = new GetSubsidyRecommendationsRequest
                {
                    CycleId = _activeCycleId,
                    Page = page,
                    PageSize = QueuePageSize,
                    SortBy = "PriorityScore",
                    SortOrder = "desc"
                };
                if (_auditFilter == "CollegeAge") req.NeedsCloserReview = true;

                var resp = await _apiService.GetRecommendationsPagedAsync(req);
                if (resp?.Data != null)
                {
                    QueueTotalCount = resp.Data.TotalCount;
                    QueueTotalPages = Math.Max(1, resp.Data.TotalPages);
                    _queueCurrentPage = resp.Data.Page;
                    OnPropertyChanged(nameof(QueueCurrentPage));
                    OnPropertyChanged(nameof(CanQueuePrevPage));
                    OnPropertyChanged(nameof(CanQueueNextPage));
                    OnPropertyChanged(nameof(QueuePageNumberLabel));
                    OnPropertyChanged(nameof(QueuePageSummary));
                    OnPropertyChanged(nameof(RecommendationsSummary));

                    PagedRecommendations.Clear();
                    FilteredRecommendations.Clear();

                    int startIndex = (resp.Data.Page - 1) * resp.Data.PageSize;
                    if (resp.Data.Items != null)
                    {
                        for (int i = 0; i < resp.Data.Items.Count; i++)
                        {
                            var item = SubsidyModelMapper.ToItem(resp.Data.Items[i]);
                            item.Rank = startIndex + i + 1;
                            item.IsWithinQuota = (item.Rank <= _granteeSlots);
                            PagedRecommendations.Add(item);
                            FilteredRecommendations.Add(item);
                        }
                    }
                    RecalculateSelectionCounts();
                }
                else
                {
                    PagedRecommendations.Clear();
                    FilteredRecommendations.Clear();
                    QueueTotalCount = 0;
                    QueueTotalPages = 1;
                    _queueCurrentPage = 1;
                    OnPropertyChanged(nameof(QueueCurrentPage));
                    OnPropertyChanged(nameof(CanQueuePrevPage));
                    OnPropertyChanged(nameof(CanQueueNextPage));
                    OnPropertyChanged(nameof(QueuePageNumberLabel));
                    OnPropertyChanged(nameof(QueuePageSummary));
                    OnPropertyChanged(nameof(RecommendationsSummary));
                    RecalculateSelectionCounts();
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show("Queue Load Error", ex.Message, ToastType.Error);
            }
            finally
            {
                IsLoading = false;
            }
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
                var slice = FilteredRecommendations.Skip((_queueCurrentPage - 1) * QueuePageSize).Take(QueuePageSize);
                foreach (var item in slice) PagedRecommendations.Add(item);
            }

            QueueTotalCount = total;
            OnPropertyChanged(nameof(QueueCurrentPage));
            OnPropertyChanged(nameof(QueueTotalPages));
            OnPropertyChanged(nameof(CanQueuePrevPage));
            OnPropertyChanged(nameof(CanQueueNextPage));
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
                var slice = FilteredShortlistRows.Skip((_shortlistCurrentPage - 1) * ShortlistPageSize).Take(ShortlistPageSize);
                foreach (var item in slice) PagedShortlistRows.Add(item);
            }

            OnPropertyChanged(nameof(ShortlistCurrentPage));
            OnPropertyChanged(nameof(ShortlistTotalPages));
            OnPropertyChanged(nameof(CanShortlistPrevPage));
            OnPropertyChanged(nameof(CanShortlistNextPage));
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
