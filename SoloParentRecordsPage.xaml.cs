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
        private string _sexFilter = "All";
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
                new SoloParentRecord
                {
                    Id = "SP-001", Surname = "Santos", FirstName = "Maria", MiddleName = "Lim",
                    ExtensionName = "", Name = "Santos, Maria Lim",
                    DateOfBirth = new DateTime(1990, 3, 12), PlaceOfBirth = "Quezon City",
                    Sex = "Female", CivilStatus = "Single", Citizenship = "Filipino",
                    BloodType = "O+", Height = "158", Weight = "52",
                    Address = "123 Rizal St., Brgy. San Roque", Barangay = "San Roque",
                    ContactNumber = "09171234567", SourceOfReferral = "DSWD",
                    DateAdmitted = new DateTime(2024, 1, 10), CaseNo = "2024-001",
                    OffenseCommitted = "", NatureOfReferral = "Financial Assistance",
                    Children = 2, Status = "Active", LastUpdated = new DateTime(2025, 3, 15)
                },
                new SoloParentRecord
                {
                    Id = "SP-002", Surname = "Dela Cruz", FirstName = "Juan", MiddleName = "Reyes",
                    ExtensionName = "Jr.", Name = "Dela Cruz, Juan Reyes Jr.",
                    DateOfBirth = new DateTime(1985, 7, 22), PlaceOfBirth = "Manila",
                    Sex = "Male", CivilStatus = "Widowed", Citizenship = "Filipino",
                    BloodType = "A+", Height = "170", Weight = "68",
                    Address = "456 Mabini Ave., Brgy. Poblacion", Barangay = "Poblacion",
                    ContactNumber = "09281234567", SourceOfReferral = "LGU",
                    DateAdmitted = new DateTime(2023, 6, 5), CaseNo = "2023-045",
                    OffenseCommitted = "", NatureOfReferral = "Livelihood Program",
                    Children = 3, Status = "Active", LastUpdated = new DateTime(2025, 3, 12)
                },
                new SoloParentRecord
                {
                    Id = "SP-003", Surname = "Reyes", FirstName = "Ana", MiddleName = "Mendoza",
                    ExtensionName = "", Name = "Reyes, Ana Mendoza",
                    DateOfBirth = new DateTime(1993, 1, 5), PlaceOfBirth = "Caloocan",
                    Sex = "Female", CivilStatus = "Separated", Citizenship = "Filipino",
                    BloodType = "B+", Height = "155", Weight = "49",
                    Address = "789 Luna St., Brgy. Maligaya", Barangay = "Maligaya",
                    ContactNumber = "09391234567", SourceOfReferral = "Barangay",
                    DateAdmitted = new DateTime(2024, 3, 20), CaseNo = "2024-012",
                    OffenseCommitted = "", NatureOfReferral = "Solo Parent ID",
                    Children = 1, Status = "Pending", LastUpdated = new DateTime(2025, 3, 14)
                },
                new SoloParentRecord
                {
                    Id = "SP-004", Surname = "Garcia", FirstName = "Pedro", MiddleName = "Torres",
                    ExtensionName = "III", Name = "Garcia, Pedro Torres III",
                    DateOfBirth = new DateTime(1988, 9, 30), PlaceOfBirth = "Pasig",
                    Sex = "Male", CivilStatus = "Single", Citizenship = "Filipino",
                    BloodType = "AB+", Height = "172", Weight = "74",
                    Address = "321 Bonifacio Rd., Brgy. San Isidro", Barangay = "San Isidro",
                    ContactNumber = "09501234567", SourceOfReferral = "DSWD",
                    DateAdmitted = new DateTime(2023, 11, 15), CaseNo = "2023-089",
                    OffenseCommitted = "", NatureOfReferral = "Educational Assistance",
                    Children = 2, Status = "Active", LastUpdated = new DateTime(2025, 3, 9)
                },
                new SoloParentRecord
                {
                    Id = "SP-005", Surname = "Martinez", FirstName = "Rosa", MiddleName = "Cruz",
                    ExtensionName = "", Name = "Martinez, Rosa Cruz",
                    DateOfBirth = new DateTime(1979, 5, 18), PlaceOfBirth = "Marikina",
                    Sex = "Female", CivilStatus = "Widowed", Citizenship = "Filipino",
                    BloodType = "O-", Height = "152", Weight = "55",
                    Address = "654 Aguinaldo Blvd., Brgy. San Roque", Barangay = "San Roque",
                    ContactNumber = "09611234567", SourceOfReferral = "NGO",
                    DateAdmitted = new DateTime(2022, 8, 3), CaseNo = "2022-034",
                    OffenseCommitted = "", NatureOfReferral = "Medical Assistance",
                    Children = 4, Status = "Inactive", LastUpdated = new DateTime(2025, 3, 28)
                },
            };

            _allRecords = new List<SoloParentRecordViewModel>();
            foreach (SoloParentRecord r in raw)
                _allRecords.Add(new SoloParentRecordViewModel(r));

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allRecords == null) return;

            string query = SearchBox?.Text?.ToLower().Trim() ?? string.Empty;

            List<SoloParentRecordViewModel> filtered = new List<SoloParentRecordViewModel>();

            foreach (SoloParentRecordViewModel r in _allRecords)
            {
                if (_statusFilter != "All" && (r.Status ?? string.Empty) != _statusFilter) continue;
                if (_sexFilter != "All" && (r.Sex ?? string.Empty) != _sexFilter) continue;

                if (!string.IsNullOrEmpty(query))
                {
                    bool match =
                        (r.Name ?? "").ToLower().Contains(query) ||
                        (r.Id ?? "").ToLower().Contains(query) ||
                        (r.Barangay ?? "").ToLower().Contains(query) ||
                        (r.Status ?? "").ToLower().Contains(query) ||
                        (r.CivilStatus ?? "").ToLower().Contains(query) ||
                        (r.Sex ?? "").ToLower().Contains(query);
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
            _sexFilter = tag;
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
                int idx = _allRecords.FindIndex(r => r.Id == vm.Id);
                if (idx >= 0)
                    _allRecords[idx] = new SoloParentRecordViewModel(dialog.Result);

                AuditLogService.Instance.LogUpdate(dialog.Result.Name, GetCurrentUser());

                ApplyFilters();
            }
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
                    gv.Columns[1].Width = available + 30;
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
