using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SOLUM_UI.Models;

namespace SOLUM_UI.ViewModels
{
    public class AuditLogViewModel : INotifyPropertyChanged
    {
        private readonly List<AuditLog> _allLogs;

    
        public ObservableCollection<AuditLog> PagedLogs { get; }
            = new ObservableCollection<AuditLog>();

        
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); CurrentPage = 1; ApplyFilter(); }
        }

        private string _selectedFilter = "All";
        public string SelectedFilter
        {
            get => _selectedFilter;
            set { _selectedFilter = value; OnPropertyChanged(); CurrentPage = 1; ApplyFilter(); }
        }

        public List<string> ActionFilters { get; } = new List<string>
        {
            "All", "Created", "Updated", "Deleted", "Viewed", "Logged In", "Logged Out", "System"
        };        private const int PageSize = 10;
        private int _totalFiltered = 0;

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (value < 1 || value > TotalPages) return;
                _currentPage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PageInfo));
                OnPropertyChanged(nameof(CanGoPrev));
                OnPropertyChanged(nameof(CanGoNext));
                LoadPage();
            }
        }

        public int TotalPages => Math.Max(1, (int)Math.Ceiling(_totalFiltered / (double)PageSize));
        public string PageInfo => $"Page {CurrentPage} of {TotalPages}";
        public bool CanGoPrev => CurrentPage > 1;
        public bool CanGoNext => CurrentPage < TotalPages;

        public ICommand PrevPageCommand { get; }
        public ICommand NextPageCommand { get; }

        private AuditLog _selectedLog;
        public AuditLog SelectedLog
        {
            get => _selectedLog;
            set { _selectedLog = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsDetailVisible)); }
        }

        public bool IsDetailVisible => SelectedLog != null;

        public ICommand OpenDetailCommand { get; }
        public ICommand CloseDetailCommand { get; }

        
        public AuditLogViewModel(List<AuditLog> logs)
        {
            _allLogs = logs ?? new List<AuditLog>();

            PrevPageCommand = new RelayCommand(_ => CurrentPage--, _ => CanGoPrev);
            NextPageCommand = new RelayCommand(_ => CurrentPage++, _ => CanGoNext);
            OpenDetailCommand = new RelayCommand(e => SelectedLog = e as AuditLog);
            CloseDetailCommand = new RelayCommand(_ => SelectedLog = null);

            ApplyFilter();
        }

        private List<AuditLog> _filteredCache = new List<AuditLog>();

        private void ApplyFilter()
        {
            var query = _allLogs.AsEnumerable();

            if (SelectedFilter != "All")
                query = query.Where(l => l.ActionLabel == SelectedFilter);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim().ToLower();
                query = query.Where(l =>
                    (l.Description ?? "").ToLower().Contains(term) ||
                    (l.PerformedBy ?? "").ToLower().Contains(term) ||
                    (l.RecordName ?? "").ToLower().Contains(term));
            }

            _filteredCache = query.OrderByDescending(l => l.Timestamp).ToList();
            _totalFiltered = _filteredCache.Count;

            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(PageInfo));
            OnPropertyChanged(nameof(CanGoPrev));
            OnPropertyChanged(nameof(CanGoNext));

            LoadPage();
        }

        private void LoadPage()
        {
            PagedLogs.Clear();
            foreach (var log in _filteredCache.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
                PagedLogs.Add(log);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
