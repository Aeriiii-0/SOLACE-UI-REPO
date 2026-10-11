using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SOLUM_UI.Core;
using SOLUM_UI.Models;

namespace SOLUM_UI.ViewModels
{
    public partial class SubsidyRecommendationViewModel
    {
        public ObservableCollection<SubsidyItem> PagedRecommendations { get; private set; }
        public ObservableCollection<SelectionRow> PagedShortlistRows { get; private set; }
        public ObservableCollection<SelectionRow> PagedFinalGrantees { get; private set; }

        private const int PageSize = 10;

        // Tab 0: Recommendations Queue Pagination
        private int _queueCurrentPage = 1;
        public int QueueCurrentPage
        {
            get => _queueCurrentPage;
            set { if (SetProperty(ref _queueCurrentPage, value)) UpdateQueuePaged(); }
        }
        public int QueueTotalPages { get; private set; } = 1;
        public bool CanQueuePrevPage => QueueCurrentPage > 1;
        public bool CanQueueNextPage => QueueCurrentPage < QueueTotalPages;
        public string QueuePageSummary
        {
            get
            {
                int total = FilteredRecommendations?.Count ?? 0;
                if (total == 0) return "No candidate records found";
                int start = (QueueCurrentPage - 1) * PageSize + 1;
                int end = Math.Min(QueueCurrentPage * PageSize, total);
                return $"Showing {start}–{end} of {total} records";
            }
        }
        public string QueuePageNumberLabel => $"{QueueCurrentPage} / {QueueTotalPages}";
        public string RecommendationsSummary => $"{FilteredRecommendations?.Count ?? 0} Candidates Evaluated ({Math.Min(GranteeSlots, FilteredRecommendations?.Count ?? 0)} Within Quota)";

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
                int start = (ShortlistCurrentPage - 1) * PageSize + 1;
                int end = Math.Min(ShortlistCurrentPage * PageSize, total);
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
                int start = (FinalCurrentPage - 1) * PageSize + 1;
                int end = Math.Min(FinalCurrentPage * PageSize, total);
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

            QueuePrevPageCommand = new RelayCommand(_ => { if (CanQueuePrevPage) QueueCurrentPage--; });
            QueueNextPageCommand = new RelayCommand(_ => { if (CanQueueNextPage) QueueCurrentPage++; });

            ShortlistPrevPageCommand = new RelayCommand(_ => { if (CanShortlistPrevPage) ShortlistCurrentPage--; });
            ShortlistNextPageCommand = new RelayCommand(_ => { if (CanShortlistNextPage) ShortlistCurrentPage++; });

            FinalPrevPageCommand = new RelayCommand(_ => { if (CanFinalPrevPage) FinalCurrentPage--; });
            FinalNextPageCommand = new RelayCommand(_ => { if (CanFinalNextPage) FinalCurrentPage++; });
        }

        public void UpdateQueuePaged()
        {
            int total = FilteredRecommendations?.Count ?? 0;
            QueueTotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            if (_queueCurrentPage > QueueTotalPages) _queueCurrentPage = QueueTotalPages;
            if (_queueCurrentPage < 1) _queueCurrentPage = 1;

            PagedRecommendations.Clear();
            if (FilteredRecommendations != null)
            {
                var slice = FilteredRecommendations.Skip((_queueCurrentPage - 1) * PageSize).Take(PageSize);
                foreach (var item in slice) PagedRecommendations.Add(item);
            }

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
            ShortlistTotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            if (_shortlistCurrentPage > ShortlistTotalPages) _shortlistCurrentPage = ShortlistTotalPages;
            if (_shortlistCurrentPage < 1) _shortlistCurrentPage = 1;

            PagedShortlistRows.Clear();
            if (FilteredShortlistRows != null)
            {
                var slice = FilteredShortlistRows.Skip((_shortlistCurrentPage - 1) * PageSize).Take(PageSize);
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
            FinalTotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            if (_finalCurrentPage > FinalTotalPages) _finalCurrentPage = FinalTotalPages;
            if (_finalCurrentPage < 1) _finalCurrentPage = 1;

            PagedFinalGrantees.Clear();
            if (FinalGranteesList != null)
            {
                var slice = FinalGranteesList.Skip((_finalCurrentPage - 1) * PageSize).Take(PageSize);
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
