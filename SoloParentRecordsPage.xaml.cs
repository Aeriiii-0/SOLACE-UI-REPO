using SOLUM_UI.Models;
using SOLUM_UI.Services;
using SOLUM_UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SOLUM_UI
{
    public partial class SoloParentRecordsPage : Page
    {
        public static string CurrentUserName { get; set; } = string.Empty;
        public static string CurrentUserRole { get; set; } = string.Empty;

        private List<SoloParentRecordViewModel> _allRecords;
        private string _statusFilter = "All";
        private string _genderFilter = "All";
        private string _sortOption = "Name A-Z";

        public SoloParentRecordsPage()
        {
            InitializeComponent();
            LoadRecords();
            Loaded += (s, e) => ResizeNameColumn();
            SizeChanged += Page_SizeChanged;
        }

        
        private void LoadRecords()
        {
            List<SoloParentRecord> raw = new List<SoloParentRecord>
            {
                new SoloParentRecord { Id = "SP-001", Surname = "Santos",    FirstName = "Maria", MiddleName = "L.", Name = "Santos, Maria L.",    Barangay = "San Roque",  Gender = "Female", CivilStatus = "Single",    DateOfBirth = new DateTime(1990, 3, 12), Children = 2, Status = "Active",   LastUpdated = new DateTime(2025, 3, 15) },
                new SoloParentRecord { Id = "SP-002", Surname = "Dela Cruz", FirstName = "Juan",  MiddleName = "R.", Name = "Dela Cruz, Juan R.",  Barangay = "Poblacion",  Gender = "Male",   CivilStatus = "Widowed",   DateOfBirth = new DateTime(1985, 7, 22), Children = 3, Status = "Active",   LastUpdated = new DateTime(2025, 3, 12) },
                new SoloParentRecord { Id = "SP-003", Surname = "Reyes",     FirstName = "Ana",   MiddleName = "M.", Name = "Reyes, Ana M.",       Barangay = "Maligaya",   Gender = "Female", CivilStatus = "Separated", DateOfBirth = new DateTime(1993, 1,  5), Children = 1, Status = "Pending",  LastUpdated = new DateTime(2025, 3, 14) },
                new SoloParentRecord { Id = "SP-004", Surname = "Garcia",    FirstName = "Pedro", MiddleName = "T.", Name = "Garcia, Pedro T.",    Barangay = "San Isidro", Gender = "Male",   CivilStatus = "Single",    DateOfBirth = new DateTime(1988, 9, 30), Children = 2, Status = "Active",   LastUpdated = new DateTime(2025, 3,  9) },
                new SoloParentRecord { Id = "SP-005", Surname = "Martinez",  FirstName = "Rosa",  MiddleName = "C.", Name = "Martinez, Rosa C.",   Barangay = "San Roque",  Gender = "Female", CivilStatus = "Widowed",   DateOfBirth = new DateTime(1979, 5, 18), Children = 4, Status = "Inactive", LastUpdated = new DateTime(2025, 3, 28) },
            };

            _allRecords = new List<SoloParentRecordViewModel>();
            foreach (SoloParentRecord r in raw)
                _allRecords.Add(new SoloParentRecordViewModel(r));

            ApplyFilters();
        }

       //toolbar funct
        private void ApplyFilters()
        {
            if (_allRecords == null) return;

            string query = SearchBox?.Text?.ToLower().Trim() ?? string.Empty;

            List<SoloParentRecordViewModel> filtered = new List<SoloParentRecordViewModel>();

            foreach (SoloParentRecordViewModel r in _allRecords)
            {
                if (_statusFilter != "All" && (r.Status ?? string.Empty) != _statusFilter) continue;
                if (_genderFilter != "All" && (r.Gender ?? string.Empty) != _genderFilter) continue;

                if (!string.IsNullOrEmpty(query))
                {
                    bool match =
                        (r.Name ?? "").ToLower().Contains(query) ||
                        (r.Id ?? "").ToLower().Contains(query) ||
                        (r.Barangay ?? "").ToLower().Contains(query) ||
                        (r.Status ?? "").ToLower().Contains(query) ||
                        (r.CivilStatus ?? "").ToLower().Contains(query) ||
                        (r.Gender ?? "").ToLower().Contains(query);
                    if (!match) continue;
                }

                filtered.Add(r);
            }

            switch (_sortOption)
            {
                case "Name A-Z": filtered.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)); break;
                case "Name Z-A": filtered.Sort((a, b) => string.Compare(b.Name, a.Name, StringComparison.OrdinalIgnoreCase)); break;
                case "Newest": filtered.Sort((a, b) => string.Compare(b.LastUpdatedFormatted, a.LastUpdatedFormatted, StringComparison.OrdinalIgnoreCase)); break;
                case "Oldest": filtered.Sort((a, b) => string.Compare(a.LastUpdatedFormatted, b.LastUpdatedFormatted, StringComparison.OrdinalIgnoreCase)); break;
                case "Barangay": filtered.Sort((a, b) => string.Compare(a.Barangay, b.Barangay, StringComparison.OrdinalIgnoreCase)); break;
            }

            for (int i = 0; i < filtered.Count; i++)
                filtered[i].SetAlternate(i % 2 != 0);

            RecordsList.ItemsSource = null;
            RecordsList.ItemsSource = filtered;
        }

       //toolbars
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void FilterDrop_Click(object sender, RoutedEventArgs e)
        {
            FilterPopup.IsOpen = true;
        }

        private void SortDrop_Click(object sender, RoutedEventArgs e)
        {
            SortPopup.IsOpen = true;
        }

        private void StatusOpt_Click(object sender, MouseButtonEventArgs e)
        {
            string tag = (sender as FrameworkElement)?.Tag?.ToString() ?? "All";
            _statusFilter = tag;
            FilterPopup.IsOpen = false;
            ApplyFilters();
        }

        private void SexOpt_Click(object sender, MouseButtonEventArgs e)
        {
            string tag = (sender as FrameworkElement)?.Tag?.ToString() ?? "All";
            _genderFilter = tag;
            FilterPopup.IsOpen = false;
            ApplyFilters();
        }

        private void SortOpt_Click(object sender, MouseButtonEventArgs e)
        {
            string tag = (sender as FrameworkElement)?.Tag?.ToString() ?? "Name A-Z";
            _sortOption = tag;
            SortPopup.IsOpen = false;
            ApplyFilters();
        }

        private void RecordsList_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

       //show act in double click
        private void RecordsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SoloParentRecordViewModel vm = RecordsList.SelectedItem as SoloParentRecordViewModel;
            if (vm == null) return;

            
            AuditLogService.Instance.LogView(vm.Name, GetCurrentUser());

            //open the activity detail window for this record
            
        }

        
        private void AddNewRecord_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
                mainWindow.MainContent.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 };

            RecordDialog dialog = new RecordDialog { Owner = Window.GetWindow(this) };

            dialog.Closed += (s, args) =>
            {
                if (mainWindow != null)
                    mainWindow.MainContent.Effect = null;
            };

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                _allRecords.Add(new SoloParentRecordViewModel(dialog.Result));

                AuditLogService.Instance.LogCreate(dialog.Result.Name, GetCurrentUser());

                ApplyFilters();
            }
        }

      
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            SoloParentRecordViewModel vm = _allRecords.Find(r => r.Id == id);
            if (vm == null) return;

            MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
                mainWindow.MainContent.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 };

            RecordDialog dialog = new RecordDialog(vm.RawModel) { Owner = Window.GetWindow(this) };

            dialog.Closed += (s, args) =>
            {
                if (mainWindow != null)
                    mainWindow.MainContent.Effect = null;
            };

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                int idx = _allRecords.FindIndex(r => r.Id == id);
                if (idx >= 0)
                    _allRecords[idx] = new SoloParentRecordViewModel(dialog.Result);

                AuditLogService.Instance.LogUpdate(dialog.Result.Name, GetCurrentUser());

                ApplyFilters();
            }
        }

       
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;

            SoloParentRecordViewModel vm = _allRecords.Find(r => r.Id == id);
            string recordName = vm != null ? vm.Name : id;

            MessageBoxResult confirm = MessageBox.Show(
                "Delete record " + id + "?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            _allRecords.RemoveAll(r => r.Id == id);

            
            AuditLogService.Instance.LogDelete(recordName, GetCurrentUser());

            ApplyFilters();
        }

       
        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeNameColumn();
        }

        private void ResizeNameColumn()
        {
            if (RecordsList.View is GridView gv && gv.Columns.Count > 1)
            {
                double totalFixed = 80 + 80 + 110 + 115 + 120 + 80 + 110 + 120 + 90;
                double available = RecordsList.ActualWidth - totalFixed - 20;
                if (available > 100)
                    gv.Columns[1].Width = available;
            }
        }
        private string GetCurrentUser()
        {
            if (!string.IsNullOrEmpty(CurrentUserName))
                return CurrentUserName;
            return "Admin";
        }
    }
}
