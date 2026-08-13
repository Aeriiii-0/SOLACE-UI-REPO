using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace SOLUM_UI
{
    public class SubsidyItem : INotifyPropertyChanged
    {
        private string _status = "Pending";
        public string SpId { get; set; }
        public string Name { get; set; }
        public string Barangay { get; set; }
        public string SubsidyType { get; set; }
        public string Priority { get; set; }
        public string IncomeLevel { get; set; }
        public int Dependants { get; set; }
        public string CivilStatus { get; set; }
        public string DependantsLabel => Dependants + " dep.";
        public string CivilStatusShort
        {
            get
            {
                switch (CivilStatus)
                {
                    case "Widowed":   return "Widowed";
                    case "Separated": return "Separated";
                    case "Single":    return "Single";
                    case "Annulled":  return "Annulled";
                    default:          return CivilStatus ?? "";
                }
            }
        }
        public int PriorityOrder => Priority == "High" ? 0 : Priority == "Medium" ? 1 : 2;

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(StatusTextColor));
            }
        }

        public SolidColorBrush PriorityColor
        {
            get
            {
                switch (Priority)
                {
                    case "High":   return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8E8"));
                    case "Medium": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1"));
                    default:       return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
                }
            }
        }

        public SolidColorBrush PriorityTextColor
        {
            get
            {
                switch (Priority)
                {
                    case "High":   return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545"));
                    case "Medium": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17"));
                    default:       return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                }
            }
        }

        public SolidColorBrush StatusColor
        {
            get
            {
                switch (Status)
                {
                    case "Approved": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
                    case "Rejected": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8E8"));
                    default:         return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF0F5"));
                }
            }
        }

        public SolidColorBrush StatusTextColor
        {
            get
            {
                switch (Status)
                {
                    case "Approved": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                    case "Rejected": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545"));
                    default:         return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#702943"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public partial class SubsidyRecommendationPage : Page
    {
        private List<SubsidyItem> _allItems;
        private string _priorityFilter = "All";
        private string _statusFilter   = "All";
        private string _sortOption     = "By Barangay";

        public SubsidyRecommendationPage()
        {
            InitializeComponent();
            LoadRecommendations();
        }

        private void LoadRecommendations()
        {
            _allItems = new List<SubsidyItem>
            {
                new SubsidyItem { SpId="SP-001", Name="Santos, Maria Lim",         Barangay="San Roque",  SubsidyType="Financial Assistance",   Priority="High",   IncomeLevel="Low",    Dependants=2, CivilStatus="Single"    },
                new SubsidyItem { SpId="SP-002", Name="Dela Cruz, Juan Reyes Jr.", Barangay="Poblacion",  SubsidyType="Livelihood Program",     Priority="High",   IncomeLevel="Low",    Dependants=3, CivilStatus="Widowed"   },
                new SubsidyItem { SpId="SP-003", Name="Reyes, Ana Mendoza",        Barangay="Maligaya",   SubsidyType="SP ID Renewal",          Priority="Medium", IncomeLevel="Mid",    Dependants=1, CivilStatus="Separated" },
                new SubsidyItem { SpId="SP-004", Name="Garcia, Pedro Torres III",  Barangay="San Isidro", SubsidyType="Educational Assistance", Priority="Medium", IncomeLevel="Mid",    Dependants=2, CivilStatus="Single"    },
                new SubsidyItem { SpId="SP-005", Name="Martinez, Rosa Cruz",       Barangay="San Roque",  SubsidyType="Medical Assistance",     Priority="High",   IncomeLevel="Low",    Dependants=4, CivilStatus="Widowed"   },
                new SubsidyItem { SpId="SP-006", Name="Lim, Cynthia Tan",          Barangay="San Roque",  SubsidyType="Financial Assistance",   Priority="Medium", IncomeLevel="Mid",    Dependants=2, CivilStatus="Separated" },
                new SubsidyItem { SpId="SP-007", Name="Bautista, Carlos Ocampo",   Barangay="Poblacion",  SubsidyType="Housing Assistance",     Priority="High",   IncomeLevel="Low",    Dependants=3, CivilStatus="Widowed"   },
                new SubsidyItem { SpId="SP-008", Name="Mendoza, Elena Flores",     Barangay="Maligaya",   SubsidyType="Livelihood Program",     Priority="Low",    IncomeLevel="High",   Dependants=1, CivilStatus="Single"    },
                new SubsidyItem { SpId="SP-009", Name="Torres, Benjamin Ramos",    Barangay="San Isidro", SubsidyType="Educational Assistance", Priority="Low",    IncomeLevel="High",   Dependants=2, CivilStatus="Separated" },
                new SubsidyItem { SpId="SP-010", Name="Navarro, Josephine Aquino", Barangay="San Roque",  SubsidyType="Medical Assistance",     Priority="Medium", IncomeLevel="Low",    Dependants=3, CivilStatus="Widowed"   },
            };
            ApplyFilterAndSort();
        }

        private void ApplyFilterAndSort()
        {
            var filtered = new List<SubsidyItem>();
            foreach (var item in _allItems)
            {
                if (_priorityFilter != "All" && item.Priority != _priorityFilter) continue;
                if (_statusFilter   != "All" && item.Status   != _statusFilter)   continue;
                filtered.Add(item);
            }

            switch (_sortOption)
            {
                case "Priority: High First":
                    filtered.Sort((a, b) => a.PriorityOrder.CompareTo(b.PriorityOrder));
                    break;
                case "Priority: Low First":
                    filtered.Sort((a, b) => b.PriorityOrder.CompareTo(a.PriorityOrder));
                    break;
                case "Name A-Z":
                    filtered.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.OrdinalIgnoreCase));
                    break;
                default:
                    filtered.Sort((a, b) => string.Compare(a.Barangay, b.Barangay, System.StringComparison.OrdinalIgnoreCase));
                    break;
            }

            RecommendationList.ItemsSource = null;
            RecommendationList.ItemsSource = filtered;
            UpdateCounts();
        }

        private void UpdateCounts()
        {
            int pending = 0, approved = 0;
            foreach (var i in _allItems)
            {
                if (i.Status == "Approved") approved++;
                else if (i.Status == "Pending") pending++;
            }
            TxtPendingCount.Text  = pending  + " pending";
            TxtApprovedCount.Text = approved + " approved";
        }

        private void RecommendationList_Loaded(object sender, RoutedEventArgs e) => ResizeColumns();
        private void RecommendationList_SizeChanged(object sender, SizeChangedEventArgs e) => ResizeColumns();

        private void ResizeColumns()
        {
            if (RecommendationList.View is System.Windows.Controls.GridView gv && gv.Columns.Count >= 7)
            {
                double available = RecommendationList.ActualWidth - 22;
                if (available <= 0) return;

                const double spId       = 78;
                const double name       = 150;
                const double barangay   = 110;
                const double priority   = 96;
                const double status     = 104;
                const double indicators = 190;
                const double action     = 340;

                double spare = Math.Max(0, available - spId - name - barangay - priority - status - indicators - action);

                gv.Columns[0].Width = spId;
                gv.Columns[1].Width = name + spare * 0.5;
                gv.Columns[2].Width = barangay + spare * 0.5;
                gv.Columns[3].Width = priority;
                gv.Columns[4].Width = status;
                gv.Columns[5].Width = indicators;
                gv.Columns[6].Width = action;
            }
        }

        private void FilterDrop_Click(object sender, RoutedEventArgs e)
        {
            FilterPopup.IsOpen = true;
        }

        private void FilterApply_Click(object sender, RoutedEventArgs e)
        {
            _priorityFilter = (CmbFilterPriority?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
            _statusFilter   = (CmbFilterStatus?.SelectedItem  as ComboBoxItem)?.Content?.ToString() ?? "All";
            FilterPopup.IsOpen = false;
            ApplyFilterAndSort();
        }

        private void FilterReset_Click(object sender, RoutedEventArgs e)
        {
            if (CmbFilterPriority != null) CmbFilterPriority.SelectedIndex = 0;
            if (CmbFilterStatus   != null) CmbFilterStatus.SelectedIndex   = 0;
            _priorityFilter = "All";
            _statusFilter   = "All";
            FilterPopup.IsOpen = false;
            ApplyFilterAndSort();
        }

        private void SortDrop_Click(object sender, RoutedEventArgs e)
        {
            SortPopup.IsOpen = true;
        }

        private void SortOpt_Click(object sender, MouseButtonEventArgs e)
        {
            _sortOption = (sender as FrameworkElement)?.Tag?.ToString() ?? "By Barangay";
            SortPopup.IsOpen = false;
            ApplyFilterAndSort();
        }

        private void Approve_Click(object sender, RoutedEventArgs e)
        {
            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            SubsidyItem item = _allItems.Find(x => x.SpId == id);
            if (item != null)
            {
                item.Status = "Approved";
                ToastNotification.Show("Approved", item.Name + " subsidy approved.", ToastType.Success);
                ApplyFilterAndSort();
            }
        }

        private void Reject_Click(object sender, RoutedEventArgs e)
        {
            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            SubsidyItem item = _allItems.Find(x => x.SpId == id);
            if (item != null)
            {
                item.Status = "Rejected";
                ToastNotification.Show("Rejected", item.Name + " subsidy rejected.", ToastType.Warning);
                ApplyFilterAndSort();
            }
        }

        private void ViewRecord_Click(object sender, RoutedEventArgs e)
        {
            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            SubsidyItem item = _allItems.Find(x => x.SpId == id);
            if (item == null) return;

            SoloParentRecordsPage.CurrentUserName     = MainWindow.CurrentUserName;
            SoloParentRecordsPage.CurrentUserRole     = MainWindow.CurrentUserRole;
            SoloParentRecordsPage.CurrentUserBarangay = MainWindow.CurrentUserBarangay;

            var tempPage = new SoloParentRecordsPage();
            var vm = tempPage.FindRecord(id);
            if (vm == null) return;

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
                dialog.ShowDialog();
            }
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName   = "SubsidyRecommendations",
                DefaultExt = ".csv",
                Filter     = "CSV file (*.csv)|*.csv"
            };
            if (dlg.ShowDialog() != true) return;
            var sb = new StringBuilder();
            sb.AppendLine("SP ID,Name,Barangay,Subsidy Type,Priority,Status");
            foreach (var i in _allItems)
                sb.AppendLine(i.SpId + "," + i.Name + "," + i.Barangay + "," + i.SubsidyType + "," + i.Priority + "," + i.Status);
            System.IO.File.WriteAllText(dlg.FileName, sb.ToString());
            MessageBox.Show("Exported to:\n" + dlg.FileName, "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportRow_Click(object sender, RoutedEventArgs e)
        {
            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            SubsidyItem item = _allItems.Find(x => x.SpId == id);
            if (item == null) return;
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName   = item.SpId + "_subsidy",
                DefaultExt = ".csv",
                Filter     = "CSV file (*.csv)|*.csv"
            };
            if (dlg.ShowDialog() != true) return;
            var sb = new StringBuilder();
            sb.AppendLine("SP ID,Name,Barangay,Subsidy Type,Priority,Status");
            sb.AppendLine(item.SpId + "," + item.Name + "," + item.Barangay + "," + item.SubsidyType + "," + item.Priority + "," + item.Status);
            System.IO.File.WriteAllText(dlg.FileName, sb.ToString());
            MessageBox.Show("Exported to:\n" + dlg.FileName, "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
