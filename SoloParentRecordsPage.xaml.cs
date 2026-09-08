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
        public static string CurrentUserName    { get; set; } = string.Empty;
        public static string CurrentUserRole    { get; set; } = string.Empty;
        public static string CurrentUserBarangay { get; set; } = string.Empty;

        private List<SoloParentRecordViewModel> _allRecords;
        private List<SoloParentRecordViewModel> _filteredRecords;

        private string _statusFilter  = "All";
        private string _sexFilter     = "All";
        private string _barangayFilter = "All";
        private string _sortOption    = "Name A-Z";

        private const int PageSize   = 10;
        private int _currentPage     = 1;

        private bool IsAdmin      => string.Equals(CurrentUserRole, "Administrator", StringComparison.OrdinalIgnoreCase);
        private bool IsBasicUser  => string.Equals(CurrentUserRole, "BasicUser",     StringComparison.OrdinalIgnoreCase);
        private bool IsEncoder    => string.Equals(CurrentUserRole, "Encoder",       StringComparison.OrdinalIgnoreCase);

        public SoloParentRecordsPage()
        {
            InitializeComponent();
            LoadRecords();
            Loaded += (s, e) =>
            {
                ApplyRoleView();
                ResizeNameColumn();
            };
            SizeChanged += Page_SizeChanged;
        }

        private void ApplyRoleView()
        {
            if (IsBasicUser)
            {
                AdminToolbar.Visibility      = Visibility.Collapsed;
                BasicUserToolbar.Visibility  = Visibility.Visible;
                EncoderSearchPanel.Visibility = Visibility.Collapsed;
                PagingRow.Visibility         = Visibility.Visible;
                _barangayFilter              = CurrentUserBarangay;

                if (ColSex         != null) ColSex.Width         = 0;
                if (ColCivilStatus != null) ColCivilStatus.Width = 0;
                if (ColChildren    != null) ColChildren.Width    = 0;
                if (ColLastUpdated != null) ColLastUpdated.Width = 0;
                if (ColBarangay    != null) ColBarangay.Width    = 0;
                if (ColActions     != null) ColActions.Width     = 80;
            }
            else if (IsEncoder)
            {
                AdminToolbar.Visibility      = Visibility.Collapsed;
                BasicUserToolbar.Visibility  = Visibility.Collapsed;
                EncoderSearchPanel.Visibility = Visibility.Visible;
                PagingRow.Visibility         = Visibility.Collapsed;
            }
            else
            {
                AdminToolbar.Visibility      = Visibility.Visible;
                BasicUserToolbar.Visibility  = Visibility.Collapsed;
                EncoderSearchPanel.Visibility = Visibility.Collapsed;
                PagingRow.Visibility         = Visibility.Visible;
            }
        }

        private void LoadRecords()
        {
            List<SoloParentRecord> raw = new List<SoloParentRecord>
            {
                new SoloParentRecord { Id = "SP-001", Surname = "Santos", FirstName = "Maria", MiddleName = "Lim",
                    Name = "Santos, Maria Lim", DateOfBirth = new DateTime(1990, 3, 12), PlaceOfBirth = "Biñan",
                    Sex = "Female", CivilStatus = "Single",
                    Address = "123 Rizal St.", Barangay = "Biñan Poblacion",
                    ContactNumber = "09171234567", Children = 2, Status = "Valid",
                    LastUpdated = new DateTime(2025, 3, 15) },
                new SoloParentRecord { Id = "SP-002", Surname = "Dela Cruz", FirstName = "Juan", MiddleName = "Reyes",
                    ExtensionName = "Jr.", Name = "Dela Cruz, Juan Reyes Jr.", DateOfBirth = new DateTime(1985, 7, 22),
                    PlaceOfBirth = "Biñan", Sex = "Male", CivilStatus = "Widowed",
                    Address = "456 Mabini Ave.", Barangay = "Malaban",
                    ContactNumber = "09281234567", Children = 3, Status = "Valid",
                    LastUpdated = new DateTime(2025, 3, 12) },
                new SoloParentRecord { Id = "SP-003", Surname = "Reyes", FirstName = "Ana", MiddleName = "Mendoza",
                    Name = "Reyes, Ana Mendoza", DateOfBirth = new DateTime(1993, 1, 5), PlaceOfBirth = "Biñan",
                    Sex = "Female", CivilStatus = "Separated",
                    Address = "789 Luna St.", Barangay = "Canlalay",
                    ContactNumber = "09391234567", Children = 1, Status = "Inactive",
                    LastUpdated = new DateTime(2025, 3, 14) },
                new SoloParentRecord { Id = "SP-004", Surname = "Garcia", FirstName = "Pedro", MiddleName = "Torres",
                    ExtensionName = "III", Name = "Garcia, Pedro Torres III", DateOfBirth = new DateTime(1988, 9, 30),
                    PlaceOfBirth = "Biñan", Sex = "Male", CivilStatus = "Single",
                    Address = "321 Bonifacio Rd.", Barangay = "San Antonio",
                    ContactNumber = "09501234567", Children = 2, Status = "Valid",
                    LastUpdated = new DateTime(2025, 3, 9) },
                new SoloParentRecord { Id = "SP-005", Surname = "Martinez", FirstName = "Rosa", MiddleName = "Cruz",
                    Name = "Martinez, Rosa Cruz", DateOfBirth = new DateTime(1979, 5, 18), PlaceOfBirth = "Biñan",
                    Sex = "Female", CivilStatus = "Widowed",
                    Address = "654 Aguinaldo Blvd.", Barangay = "Platero",
                    ContactNumber = "09611234567", Children = 4, Status = "Inactive",
                    LastUpdated = new DateTime(2025, 3, 28) },
                new SoloParentRecord { Id = "SP-006", Surname = "Lim", FirstName = "Cynthia", MiddleName = "Tan",
                    Name = "Lim, Cynthia Tan", DateOfBirth = new DateTime(1991, 8, 14), PlaceOfBirth = "Biñan",
                    Sex = "Female", CivilStatus = "Separated",
                    Address = "11 Taft Ave.", Barangay = "Loma",
                    ContactNumber = "09711234567", Children = 2, Status = "Valid",
                    LastUpdated = new DateTime(2025, 4, 1) },
                new SoloParentRecord { Id = "SP-007", Surname = "Bautista", FirstName = "Carlos", MiddleName = "Ocampo",
                    Name = "Bautista, Carlos Ocampo", DateOfBirth = new DateTime(1983, 11, 22), PlaceOfBirth = "Biñan",
                    Sex = "Male", CivilStatus = "Widowed",
                    Address = "22 Burgos St.", Barangay = "Tubigan",
                    ContactNumber = "09821234567", Children = 3, Status = "Valid",
                    LastUpdated = new DateTime(2025, 4, 5) },
                new SoloParentRecord { Id = "SP-008", Surname = "Mendoza", FirstName = "Elena", MiddleName = "Flores",
                    Name = "Mendoza, Elena Flores", DateOfBirth = new DateTime(1995, 4, 7), PlaceOfBirth = "Biñan",
                    Sex = "Female", CivilStatus = "Single",
                    Address = "33 MacArthur Hwy.", Barangay = "De La Paz",
                    ContactNumber = "09931234567", Children = 1, Status = "Inactive",
                    LastUpdated = new DateTime(2025, 4, 10) },
                new SoloParentRecord { Id = "SP-009", Surname = "Torres", FirstName = "Benjamin", MiddleName = "Ramos",
                    Name = "Torres, Benjamin Ramos", DateOfBirth = new DateTime(1980, 6, 30), PlaceOfBirth = "Biñan",
                    Sex = "Male", CivilStatus = "Separated",
                    Address = "44 Shaw Blvd.", Barangay = "Casile",
                    ContactNumber = "09041234567", Children = 2, Status = "Inactive",
                    LastUpdated = new DateTime(2025, 4, 12) },
                new SoloParentRecord { Id = "SP-010", Surname = "Navarro", FirstName = "Josephine", MiddleName = "Aquino",
                    Name = "Navarro, Josephine Aquino", DateOfBirth = new DateTime(1987, 2, 18), PlaceOfBirth = "Biñan",
                    Sex = "Female", CivilStatus = "Widowed",
                    Address = "55 San Jose Rd.", Barangay = "San Jose",
                    ContactNumber = "09151234567", Children = 3, Status = "Valid",
                    LastUpdated = new DateTime(2025, 4, 15) },
                new SoloParentRecord { Id = "SP-011", Surname = "Hernandez", FirstName = "Roberto", MiddleName = "Diaz",
                    Name = "Hernandez, Roberto Diaz", DateOfBirth = new DateTime(1978, 9, 5), PlaceOfBirth = "Biñan",
                    Sex = "Male", CivilStatus = "Annulled",
                    Address = "66 Langkiwa Ave.", Barangay = "Langkiwa",
                    ContactNumber = "09261234567", Children = 4, Status = "Valid",
                    LastUpdated = new DateTime(2025, 4, 18) },
                new SoloParentRecord { Id = "SP-012", Surname = "Castillo", FirstName = "Patricia", MiddleName = "Villanueva",
                    Name = "Castillo, Patricia Villanueva", DateOfBirth = new DateTime(1996, 12, 25), PlaceOfBirth = "Biñan",
                    Sex = "Female", CivilStatus = "Single",
                    Address = "77 Malamig St.", Barangay = "Malamig",
                    ContactNumber = "09371234567", Children = 1, Status = "Inactive",
                    LastUpdated = new DateTime(2025, 4, 20) },
            };

            _allRecords = new List<SoloParentRecordViewModel>();
            foreach (SoloParentRecord r in raw)
                _allRecords.Add(new SoloParentRecordViewModel(r));

            ApplyFiltersAndPage();
        }

        private void ApplyFiltersAndPage()
        {
            if (_allRecords == null) return;

            string effectiveBarangay = IsBasicUser ? CurrentUserBarangay : _barangayFilter;

            string idQuery       = SearchId?.Text?.Trim()        ?? string.Empty;
            string nameQuery     = SearchName?.Text?.ToLower().Trim()     ?? string.Empty;
            string barangayQuery = SearchBarangay?.Text?.ToLower().Trim() ?? string.Empty;

            if (IsBasicUser)
            {
                idQuery       = BasicSearchName?.Text?.ToLower().Trim() ?? string.Empty;
                nameQuery     = BasicSearchName?.Text?.ToLower().Trim() ?? string.Empty;
                barangayQuery = string.Empty;
            }

            _filteredRecords = new List<SoloParentRecordViewModel>();

            foreach (SoloParentRecordViewModel r in _allRecords)
            {
                if (_statusFilter != "All" && !IsBasicUser && (r.Status ?? string.Empty) != _statusFilter) continue;
                if (_sexFilter    != "All" && !IsBasicUser && (r.Sex    ?? string.Empty) != _sexFilter)    continue;

                if (!string.IsNullOrEmpty(effectiveBarangay) && effectiveBarangay != "All")
                {
                    if (!string.Equals(r.Barangay ?? string.Empty, effectiveBarangay, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                if (IsBasicUser)
                {
                    if (!string.IsNullOrEmpty(nameQuery) && !(r.Name ?? "").ToLower().Contains(nameQuery)) continue;
                }
                else if (!string.IsNullOrEmpty(idQuery))
                {
                    if (!(r.Id ?? "").ToLower().Contains(idQuery.ToLower())) continue;
                }
                else
                {
                    bool nameMatch     = string.IsNullOrEmpty(nameQuery)     || (r.Name     ?? "").ToLower().Contains(nameQuery);
                    bool barangayMatch = string.IsNullOrEmpty(barangayQuery) || (r.Barangay ?? "").ToLower().Contains(barangayQuery);
                    if (!nameMatch || !barangayMatch) continue;
                }

                _filteredRecords.Add(r);
            }

            switch (_sortOption)
            {
                case "Name A-Z": _filteredRecords.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)); break;
                case "Name Z-A": _filteredRecords.Sort((a, b) => string.Compare(b.Name, a.Name, StringComparison.OrdinalIgnoreCase)); break;
                case "Newest": _filteredRecords.Sort((a, b) => string.Compare(b.LastUpdatedFormatted, a.LastUpdatedFormatted, StringComparison.OrdinalIgnoreCase)); break;
                case "Oldest": _filteredRecords.Sort((a, b) => string.Compare(a.LastUpdatedFormatted, b.LastUpdatedFormatted, StringComparison.OrdinalIgnoreCase)); break;
                case "Barangay": _filteredRecords.Sort((a, b) => string.Compare(a.Barangay, b.Barangay, StringComparison.OrdinalIgnoreCase)); break;
            }

            _currentPage = 1;
            RenderPage();
        }

        private void ApplyFilters() => ApplyFiltersAndPage();

        private int TotalPages => Math.Max(1, (int)Math.Ceiling(_filteredRecords.Count / (double)PageSize));

        private void RenderPage()
        {
            if (_filteredRecords == null) return;

            int start = (_currentPage - 1) * PageSize;
            List<SoloParentRecordViewModel> page = _filteredRecords.GetRange(
                start, Math.Min(PageSize, _filteredRecords.Count - start));

            for (int i = 0; i < page.Count; i++)
                page[i].SetAlternate(i % 2 != 0);

            RecordsList.ItemsSource = null;
            RecordsList.ItemsSource = page;

            if (NoResultsPanel != null)
                NoResultsPanel.Visibility = _filteredRecords.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            UpdatePagingUI();
        }

        private void UpdatePagingUI()
        {
            if (PageInfoText != null)
                PageInfoText.Text = "Page " + _currentPage + " of " + TotalPages;

            if (BtnBack != null)
                BtnBack.Visibility = _currentPage > 1 ? Visibility.Visible : Visibility.Collapsed;

            if (BtnNext != null)
                BtnNext.Visibility = _currentPage < TotalPages ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < TotalPages)
            {
                _currentPage++;
                RenderPage();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                RenderPage();
            }
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e) => ApplyFiltersAndPage();

        private void EncoderSearchBtn_Click(object sender, RoutedEventArgs e) => ApplyFiltersAndPage();

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ApplyFiltersAndPage();
        }

        private void SearchFields_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool hasText = !string.IsNullOrEmpty(SearchId?.Text)
                        || !string.IsNullOrEmpty(SearchName?.Text)
                        || !string.IsNullOrEmpty(SearchBarangay?.Text);

            if (ClearSearchBtn != null)
                ClearSearchBtn.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;

            if (!hasText)
                ApplyFiltersAndPage();
        }

        private void EncoderFields_TextChanged(object sender, TextChangedEventArgs e) { }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchId.Text = string.Empty;
            SearchName.Text = string.Empty;
            SearchBarangay.Text = string.Empty;
            ClearSearchBtn.Visibility = Visibility.Collapsed;
            ApplyFiltersAndPage();
        }

        private void FilterDrop_Click(object sender, RoutedEventArgs e) => FilterPopup.IsOpen = true;
        private void SortDrop_Click(object sender, RoutedEventArgs e) => SortPopup.IsOpen = true;

        private void FilterCombo_Changed(object sender, SelectionChangedEventArgs e) { }

        private void FilterApply_Click(object sender, RoutedEventArgs e)
        {
            string sexVal      = (CmbFilterSex?.SelectedItem      as ComboBoxItem)?.Content?.ToString() ?? "All";
            string barangayVal = (CmbFilterBarangay?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
            string statusVal   = (CmbFilterStatus?.SelectedItem   as ComboBoxItem)?.Content?.ToString() ?? "All";
            _sexFilter      = sexVal;
            _barangayFilter = barangayVal;
            _statusFilter   = statusVal;
            FilterPopup.IsOpen = false;
            ApplyFiltersAndPage();
        }

        private void FilterReset_Click(object sender, RoutedEventArgs e)
        {
            if (CmbFilterSex      != null) CmbFilterSex.SelectedIndex      = 0;
            if (CmbFilterBarangay != null) CmbFilterBarangay.SelectedIndex = 0;
            if (CmbFilterStatus   != null) CmbFilterStatus.SelectedIndex   = 0;
            _sexFilter      = "All";
            _barangayFilter = "All";
            _statusFilter   = "All";
            FilterPopup.IsOpen = false;
            ApplyFiltersAndPage();
        }

        private void StatusOpt_Click(object sender, MouseButtonEventArgs e) { }

        private void SexOpt_Click(object sender, MouseButtonEventArgs e) { }

        private void SortOpt_Click(object sender, MouseButtonEventArgs e)
        {
            _sortOption = (sender as FrameworkElement)?.Tag?.ToString() ?? "Barangay";
            SortPopup.IsOpen = false;
            ApplyFiltersAndPage();
        }

        private void RecordsList_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void RecordsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SoloParentRecordViewModel vm = RecordsList.SelectedItem as SoloParentRecordViewModel;
            if (vm == null) return;

            AuditLogService.Instance.LogView(vm.RawModel.Id, vm.Name, GetCurrentUser(), CurrentUserRole);
            OpenViewDialog(vm);
        }

        private void OpenViewDialog(SoloParentRecordViewModel vm)
        {
            MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
                mainWindow.MainContent.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 };

            var view = new RecordViewDialog(vm.RawModel) { Owner = Window.GetWindow(this) };
            view.Closed += (s, args) => { if (mainWindow != null) mainWindow.MainContent.Effect = null; };
            view.ShowDialog();

            if (view.OpenedEdit)
            {
                if (mainWindow != null)
                    mainWindow.MainContent.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 };

                RecordDialog dialog = new RecordDialog(vm.RawModel) { Owner = Window.GetWindow(this) };
                dialog.Closed += (s, args) => { if (mainWindow != null) mainWindow.MainContent.Effect = null; };

                if (dialog.ShowDialog() == true && dialog.Result != null)
                {
                    int idx = _allRecords.FindIndex(r => r.Id == vm.Id);
                    if (idx >= 0) _allRecords[idx] = new SoloParentRecordViewModel(dialog.Result);
                    ToastNotification.Show("Record Updated", dialog.Result.Name + " was updated.", ToastType.Info);
                    AuditLogService.Instance.LogUpdate(dialog.Result.Id, dialog.Result.Name, GetCurrentUser(), CurrentUserRole,
                        BuildDiff(vm.RawModel, dialog.Result));
                    AuditLogService.Instance.LogView(vm.RawModel.Id, vm.Name, GetCurrentUser(), CurrentUserRole);
                    ApplyFiltersAndPage();
                }
            }
        }

        private void AddNewRecord_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
                mainWindow.MainContent.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 };

            var methodDialog = new EntryMethodDialog { Owner = Window.GetWindow(this) };
            bool? picked = methodDialog.ShowDialog();

            if (picked != true || methodDialog.Selected == EntryMethod.None)
            {
                if (mainWindow != null) mainWindow.MainContent.Effect = null;
                return;
            }

            if (methodDialog.Selected == EntryMethod.Scan)
            {
                var ocr = new OcrScanDialog { Owner = Window.GetWindow(this) };
                ocr.Closed += (s, args) => { if (mainWindow != null) mainWindow.MainContent.Effect = null; };
                
                if (ocr.ShowDialog() == true && ocr.Result != null)
                {
                    _allRecords.Add(new SoloParentRecordViewModel(ocr.Result));
                    ToastNotification.Show("Record Added", ocr.Result.Name + " was added successfully.", ToastType.Success);
                    AuditLogService.Instance.LogCreate(ocr.Result.Id, ocr.Result.Name, GetCurrentUser(), CurrentUserRole);
                    ApplyFiltersAndPage();
                }
                return;
            }

            RecordDialog dialog = new RecordDialog() { Owner = Window.GetWindow(this) };
            dialog.Closed += (s, args) => { if (mainWindow != null) mainWindow.MainContent.Effect = null; };

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                _allRecords.Add(new SoloParentRecordViewModel(dialog.Result));
                ToastNotification.Show("Record Added", dialog.Result.Name + " was added successfully.", ToastType.Success);
                AuditLogService.Instance.LogCreate(dialog.Result.Id, dialog.Result.Name, GetCurrentUser(), CurrentUserRole);
                ApplyFiltersAndPage();
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            SoloParentRecordViewModel vm = _allRecords.Find(r => r.Id == id);
            if (vm == null) return;

            if (IsBasicUser && !string.Equals(vm.RawModel.CreatedBy ?? string.Empty, CurrentUserName, StringComparison.OrdinalIgnoreCase))
            {
                ToastNotification.Show("Access Denied", "You can only edit records you created.", ToastType.Warning);
                return;
            }

            MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
                mainWindow.MainContent.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 };

            RecordDialog dialog = new RecordDialog(vm.RawModel) { Owner = Window.GetWindow(this) };
            dialog.Closed += (s, args) => { if (mainWindow != null) mainWindow.MainContent.Effect = null; };

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                int idx = _allRecords.FindIndex(r => r.Id == id);
                if (idx >= 0) _allRecords[idx] = new SoloParentRecordViewModel(dialog.Result);
                ToastNotification.Show("Record Updated", dialog.Result.Name + " was updated.", ToastType.Info);
                AuditLogService.Instance.LogUpdate(dialog.Result.Id, dialog.Result.Name, GetCurrentUser(), CurrentUserRole,
                    BuildDiff(vm.RawModel, dialog.Result));
                ApplyFiltersAndPage();
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (IsBasicUser)
            {
                ToastNotification.Show("Access Denied", "Basic users cannot delete records.", ToastType.Warning);
                return;
            }

            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            SoloParentRecordViewModel vm = _allRecords.Find(r => r.Id == id);
            string recordName = vm != null ? vm.Name : id;

            MessageBoxResult confirm = MessageBox.Show(
                "Delete record " + id + "?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            _allRecords.RemoveAll(r => r.Id == id);
            ToastNotification.Show("Record Deleted", "The record was removed.", ToastType.Warning);
            AuditLogService.Instance.LogDelete(id, recordName, GetCurrentUser(), CurrentUserRole);
            ApplyFiltersAndPage();
        }

        private void ExportRecord_Click(object sender, RoutedEventArgs e)
        {
            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            SoloParentRecordViewModel vm = _allRecords.Find(r => r.Id == id);
            if (vm == null) return;

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = vm.Id + "_" + (vm.Surname ?? "record"),
                DefaultExt = ".csv",
                Filter = "CSV file (*.csv)|*.csv"
            };

            if (dlg.ShowDialog() != true) return;

            var rec = vm.RawModel;
            var lines = new System.Text.StringBuilder();
            lines.AppendLine("Field,Value");
            lines.AppendLine("ID," + rec.Id);
            lines.AppendLine("Surname," + rec.Surname);
            lines.AppendLine("First Name," + rec.FirstName);
            lines.AppendLine("Middle Name," + rec.MiddleName);
            lines.AppendLine("Extension," + rec.ExtensionName);
            lines.AppendLine("Date of Birth," + rec.DateOfBirthFormatted);
            lines.AppendLine("Place of Birth," + rec.PlaceOfBirth);
            lines.AppendLine("Sex," + rec.Sex);
            lines.AppendLine("Civil Status," + rec.CivilStatus);
            lines.AppendLine("Citizenship," + rec.Citizenship);
            lines.AppendLine("Blood Type," + rec.BloodType);
            lines.AppendLine("Height," + rec.Height);
            lines.AppendLine("Weight," + rec.Weight);
            lines.AppendLine("Address," + rec.Address);
            lines.AppendLine("Barangay," + rec.Barangay);
            lines.AppendLine("Contact Number," + rec.ContactNumber);
            lines.AppendLine("Source of Referral," + rec.SourceOfReferral);
            lines.AppendLine("Date Admitted," + rec.DateAdmittedFormatted);
            lines.AppendLine("Case No.," + rec.CaseNo);
            lines.AppendLine("Offense Committed," + rec.OffenseCommitted);
            lines.AppendLine("Nature of Referral," + rec.NatureOfReferral);
            lines.AppendLine("Children," + rec.Children);
            lines.AppendLine("Status," + rec.Status);
            lines.AppendLine("Last Updated," + rec.LastUpdatedFormatted);

            System.IO.File.WriteAllText(dlg.FileName, lines.ToString());
            MessageBox.Show("Record exported to:\n" + dlg.FileName, "Export Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e) => ResizeNameColumn();

        private void ResizeNameColumn()
        {
            if (RecordsList.View is GridView gv && gv.Columns.Count >= 10)
            {
                const double id         = 72;
                const double sex        = 90;
                const double civil      = 120;
                const double dob        = 110;
                const double lastUpd    = 112;
                const double validUntil = 110;
                const double status     = 96;
                const double actions    = 76;

                double available = RecordsList.ActualWidth - 2;
                if (available <= 0) return;

                double totalFixed = id + sex + civil + dob + lastUpd + validUntil + status + actions;
                double flex = Math.Max(200, available - totalFixed);

                gv.Columns[0].Width = id;
                gv.Columns[1].Width = flex * 0.54;
                gv.Columns[2].Width = flex * 0.46;
                gv.Columns[3].Width = sex;
                gv.Columns[4].Width = civil;
                gv.Columns[5].Width = dob;
                gv.Columns[6].Width = lastUpd;
                gv.Columns[7].Width = validUntil;
                gv.Columns[8].Width = status;
                gv.Columns[9].Width = actions;
            }
        }

        private void BasicSearchName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFiltersAndPage();
        }

        private void BasicUserAddRecord_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
                mainWindow.MainContent.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 };

            RecordDialog dialog = new RecordDialog { Owner = Window.GetWindow(this) };
            dialog.Closed += (s, args) => { if (mainWindow != null) mainWindow.MainContent.Effect = null; };

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                dialog.Result.CreatedBy = CurrentUserName;
                dialog.Result.Barangay  = string.IsNullOrEmpty(dialog.Result.Barangay)
                    ? CurrentUserBarangay : dialog.Result.Barangay;

                _allRecords.Add(new SoloParentRecordViewModel(dialog.Result));
                ToastNotification.Show("Record Added", dialog.Result.Name + " was added successfully.", ToastType.Success);
                AuditLogService.Instance.LogCreate(dialog.Result.Id, dialog.Result.Name, GetCurrentUser(), CurrentUserRole);
                ApplyFiltersAndPage();
            }
        }

        private string GetCurrentUser() => !string.IsNullOrEmpty(CurrentUserName) ? CurrentUserName : "Admin";

        /// <summary>Compares two records field-by-field and returns a comma-separated summary of changed fields.</summary>
        private static string BuildDiff(SOLUM_UI.Models.SoloParentRecord before, SOLUM_UI.Models.SoloParentRecord after)
        {
            var changes = new System.Collections.Generic.List<string>();
            void Check(string label, string a, string b)
            {
                if (!string.Equals(a ?? "", b ?? "", StringComparison.Ordinal))
                    changes.Add(label + ": \"" + (a ?? "") + "\" → \"" + (b ?? "") + "\"");
            }
            Check("Last Name",    before.Surname,       after.Surname);
            Check("First Name",   before.FirstName,     after.FirstName);
            Check("Middle Name",  before.MiddleName,    after.MiddleName);
            Check("Extension",    before.ExtensionName, after.ExtensionName);
            Check("Sex",          before.Sex,           after.Sex);
            Check("Civil Status", before.CivilStatus,   after.CivilStatus);
            Check("Birthplace",   before.PlaceOfBirth,  after.PlaceOfBirth);
            Check("Address",      before.Address,       after.Address);
            Check("Barangay",     before.Barangay,      after.Barangay);
            Check("Contact",      before.ContactNumber, after.ContactNumber);
            Check("Status",       before.Status,        after.Status);
            Check("Education",    before.EducationalAttainment, after.EducationalAttainment);
            Check("Occupation",   before.Occupation,    after.Occupation);
            Check("Religion",     before.Religion,      after.Religion);
            if (before.DateOfBirth != after.DateOfBirth)
                changes.Add("Date of Birth: \"" + before.DateOfBirth.ToString("yyyy-MM-dd") + "\" → \"" + after.DateOfBirth.ToString("yyyy-MM-dd") + "\"");
            return string.Join("; ", changes);
        }

        public SoloParentRecordViewModel FindRecord(string spId)
        {
            return _allRecords?.Find(r => r.Id == spId);
        }

        public void HighlightRecord(string spId)
        {
            var vm = _allRecords?.Find(r => r.Id == spId);
            if (vm == null) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                RecordsList.SelectedItem = vm;
                RecordsList.ScrollIntoView(vm);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }
}
