using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SOLUM_UI
{
    // represents one recommendation entry from the ml model
    public class SubsidyItem : INotifyPropertyChanged
    {
        private string _status = "Pending";

        public string SpId        { get; set; }
        public string Name        { get; set; }
        public string Barangay    { get; set; }
        public string SubsidyType { get; set; }
        public string Priority    { get; set; }
        public string IncomeLevel { get; set; }
        public int    Dependants  { get; set; }
        public string CivilStatus { get; set; }

        // score is 0-100, drives the percentage bar and ranking
        public double Score          { get; set; }
        public double ScoreRemainder => Math.Max(0, 100 - Score);
        // pre-calculated bar width (max 80px) for the progress pill
        public double ScoreBarWidth  => Score * 0.8;
        public double ScoreBarRemainder => Math.Max(0, 80 - ScoreBarWidth);

        public string DependantsLabel   => Dependants + " dep.";
        public string ScoreLabel        => Score.ToString("0") + "%";
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

        // used for sorting on the recommendations tab (high priority = lower number)
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

        public SolidColorBrush ScoreBarColor
        {
            get
            {
                if (Score >= 75) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545"));
                if (Score >= 50) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17"));
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
            }
        }

        public SolidColorBrush StatusColor
        {
            get
            {
                switch (Status)
                {
                    case "Approved":   return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
                    case "Rejected":   return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8E8"));
                    case "Waitlisted": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1"));
                    default:           return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF0F5"));
                }
            }
        }

        public SolidColorBrush StatusTextColor
        {
            get
            {
                switch (Status)
                {
                    case "Approved":   return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                    case "Rejected":   return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545"));
                    case "Waitlisted": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17"));
                    default:           return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#702943"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // flat row shown in the approved selection tab, carries rank + waitlist flag
    public class SelectionRow : INotifyPropertyChanged
    {
        private bool _isFinalGrantee;

        public int    Rank            { get; set; }
        public string SpId            { get; set; }
        public string Name            { get; set; }
        public string Barangay        { get; set; }
        public string SubsidyType     { get; set; }
        public string Priority        { get; set; }
        public double Score           { get; set; }
        public double ScoreRemainder  => Math.Max(0, 100 - Score);
        // pre-calculated bar width (max 80px) for the progress pill
        public double ScoreBarWidth      => Score * 0.8;
        public double ScoreBarRemainder  => Math.Max(0, 80 - ScoreBarWidth);
        public bool   IsWaitlisted    { get; set; }

        // toggled on the final confirmation tab
        public bool IsFinalGrantee
        {
            get => _isFinalGrantee;
            set
            {
                _isFinalGrantee = value;
                OnPropertyChanged(nameof(IsFinalGrantee));
                OnPropertyChanged(nameof(FinalBadgeColor));
                OnPropertyChanged(nameof(FinalBadgeText));
                OnPropertyChanged(nameof(FinalBadgeTextColor));
            }
        }

        public string ScoreLabel => Score.ToString("0") + "%";

        public SolidColorBrush RankBg =>
            IsWaitlisted
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EAF6F0"));

        public SolidColorBrush RankFg =>
            IsWaitlisted
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));

        public SolidColorBrush RowBg =>
            IsWaitlisted
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFDF5"))
                : new SolidColorBrush(Colors.White);

        public SolidColorBrush ScoreBarColor
        {
            get
            {
                if (Score >= 75) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545"));
                if (Score >= 50) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17"));
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
            }
        }

        // priority badge colors reused from subsidy item
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

        // final confirmation badge
        public SolidColorBrush FinalBadgeColor
        {
            get
            {
                if (!IsFinalGrantee) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"));
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
            }
        }

        public string FinalBadgeText     => IsFinalGrantee ? "✓ Granted" : "Pending";
        public SolidColorBrush FinalBadgeTextColor =>
            IsFinalGrantee
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));

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

        private int _budgetAmount = 10000;

        private List<SelectionRow> _selectionRows = new List<SelectionRow>();

        public SubsidyRecommendationPage()
        {
            InitializeComponent();
            LoadRecommendations();
        }

        private void LoadRecommendations()
        {
            _allItems = new List<SubsidyItem>
            {
                new SubsidyItem { SpId="SP-001", Name="Santos, Maria Lim",         Barangay="San Roque",  SubsidyType="Allowance", Priority="High",   IncomeLevel="Low",  Dependants=2, CivilStatus="Single",    Score=92 },
                new SubsidyItem { SpId="SP-002", Name="Dela Cruz, Juan Reyes Jr.", Barangay="Poblacion",  SubsidyType="Allowance", Priority="High",   IncomeLevel="Low",  Dependants=3, CivilStatus="Widowed",   Score=88 },
                new SubsidyItem { SpId="SP-003", Name="Reyes, Ana Mendoza",        Barangay="Maligaya",   SubsidyType="Allowance", Priority="Medium", IncomeLevel="Mid",  Dependants=1, CivilStatus="Separated", Score=61 },
                new SubsidyItem { SpId="SP-004", Name="Garcia, Pedro Torres III",  Barangay="San Isidro", SubsidyType="Allowance", Priority="Medium", IncomeLevel="Mid",  Dependants=2, CivilStatus="Single",    Score=67 },
                new SubsidyItem { SpId="SP-005", Name="Martinez, Rosa Cruz",       Barangay="San Roque",  SubsidyType="Allowance", Priority="High",   IncomeLevel="Low",  Dependants=4, CivilStatus="Widowed",   Score=95 },
                new SubsidyItem { SpId="SP-006", Name="Lim, Cynthia Tan",          Barangay="San Roque",  SubsidyType="Allowance", Priority="Medium", IncomeLevel="Mid",  Dependants=2, CivilStatus="Separated", Score=58 },
                new SubsidyItem { SpId="SP-007", Name="Bautista, Carlos Ocampo",   Barangay="Poblacion",  SubsidyType="Allowance", Priority="High",   IncomeLevel="Low",  Dependants=3, CivilStatus="Widowed",   Score=84 },
                new SubsidyItem { SpId="SP-008", Name="Mendoza, Elena Flores",     Barangay="Maligaya",   SubsidyType="Allowance", Priority="Low",    IncomeLevel="High", Dependants=1, CivilStatus="Single",    Score=41 },
                new SubsidyItem { SpId="SP-009", Name="Torres, Benjamin Ramos",    Barangay="San Isidro", SubsidyType="Allowance", Priority="Low",    IncomeLevel="High", Dependants=2, CivilStatus="Separated", Score=37 },
                new SubsidyItem { SpId="SP-010", Name="Navarro, Josephine Aquino", Barangay="San Roque",  SubsidyType="Allowance", Priority="Medium", IncomeLevel="Low",  Dependants=3, CivilStatus="Widowed",   Score=74 },
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

            // sorting the ranking based on selected option
            switch (_sortOption)
            {
                case "Priority: High First":
                    filtered.Sort((a, b) => a.PriorityOrder.CompareTo(b.PriorityOrder));
                    break;
                case "Priority: Low First":
                    filtered.Sort((a, b) => b.PriorityOrder.CompareTo(a.PriorityOrder));
                    break;
                case "Name A-Z":
                    filtered.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                    break;
                default:
                    filtered.Sort((a, b) => string.Compare(a.Barangay, b.Barangay, StringComparison.OrdinalIgnoreCase));
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

        private void RebuildSelectionList()
        {
            var approved = _allItems
                .Where(i => i.Status == "Approved")
                .OrderByDescending(i => i.Score)
                .ToList();

            int granteeSlots = _budgetAmount / 1000;

            _selectionRows.Clear();
            for (int idx = 0; idx < approved.Count; idx++)
            {
                var item = approved[idx];
                bool waitlisted = idx >= granteeSlots;

                _selectionRows.Add(new SelectionRow
                {
                    Rank           = idx + 1,
                    SpId           = item.SpId,
                    Name           = item.Name,
                    Barangay       = item.Barangay,
                    SubsidyType    = item.SubsidyType,
                    Priority       = item.Priority,
                    Score          = item.Score,
                    IsWaitlisted   = waitlisted,
                    IsFinalGrantee = false
                });
            }

            SelectionList.ItemsSource = null;
            SelectionList.ItemsSource = _selectionRows;

            int within = _selectionRows.Count(r => !r.IsWaitlisted);
            int onWait = _selectionRows.Count(r => r.IsWaitlisted);

            TxtSelectionSummary.Text =
                within + " grantees · " + onWait + " waitlisted · ₱" + _budgetAmount.ToString("N0") + " budget";

            UpdateDividerVisibility();
            UpdateFinalList();
        }

        // shows or hides the separator between partial approvals and waitlisted
        private void UpdateDividerVisibility()
        {
            bool hasWaitlisted = _selectionRows.Any(r => r.IsWaitlisted);
            WaitlistDivider.Visibility = hasWaitlisted ? Visibility.Visible : Visibility.Collapsed;
        }

        // final tab only shows non-waitlisted rows
        private void UpdateFinalList()
        {
            var finalRows = _selectionRows
                .Where(r => !r.IsWaitlisted)
                .ToList();

            FinalList.ItemsSource = null;
            FinalList.ItemsSource = finalRows;

            int granted   = finalRows.Count(r => r.IsFinalGrantee);
            int totalSlot = finalRows.Count;
            TxtFinalSummary.Text = granted + " of " + totalSlot + " confirmed as grantees";
        }

        // tab switching helpers
        private void TabRecommendations_Click(object sender, RoutedEventArgs e) => SwitchTab(0);
        private void TabSelection_Click(object sender, RoutedEventArgs e)
        {
            RebuildSelectionList();
            SwitchTab(1);
        }
        private void TabFinal_Click(object sender, RoutedEventArgs e)
        {
            RebuildSelectionList();
            SwitchTab(2);
        }

        private void SwitchTab(int index)
        {
            PanelRecommendations.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
            PanelSelection.Visibility       = index == 1 ? Visibility.Visible : Visibility.Collapsed;
            PanelFinal.Visibility           = index == 2 ? Visibility.Visible : Visibility.Collapsed;

            TabBtnRecommendations.Tag = index == 0 ? "Active" : "Inactive";
            TabBtnSelection.Tag       = index == 1 ? "Active" : "Inactive";
            TabBtnFinal.Tag           = index == 2 ? "Active" : "Inactive";
        }

        private void RecommendationList_Loaded(object sender, RoutedEventArgs e) => ResizeRecommendationColumns();
        private void RecommendationList_SizeChanged(object sender, SizeChangedEventArgs e) => ResizeRecommendationColumns();

        private void SelectionList_Loaded(object sender, RoutedEventArgs e) => ResizeSelectionColumns();
        private void SelectionList_SizeChanged(object sender, SizeChangedEventArgs e) => ResizeSelectionColumns();

        private void FinalList_Loaded(object sender, RoutedEventArgs e) => ResizeFinalColumns();
        private void FinalList_SizeChanged(object sender, SizeChangedEventArgs e) => ResizeFinalColumns();

        private void ResizeRecommendationColumns()
        {
            if (!(RecommendationList.View is System.Windows.Controls.GridView gv)) return;
            if (gv.Columns.Count < 8) return;

            double avail = RecommendationList.ActualWidth - 4;
            if (avail <= 0) return;

            double spId       = 78;
            double score      = 96;
            double priority   = 90;
            double status     = 96;
            double indicators = 160;
            double action     = 290;

            double flex = Math.Max(180, avail - spId - score - priority - status - indicators - action);

            gv.Columns[0].Width = spId;
            gv.Columns[1].Width = Math.Floor(flex * 0.57);
            gv.Columns[2].Width = Math.Floor(flex * 0.43);
            gv.Columns[3].Width = score;
            gv.Columns[4].Width = priority;
            gv.Columns[5].Width = status;
            gv.Columns[6].Width = indicators;
            gv.Columns[7].Width = action;
        }

        private void ResizeSelectionColumns()
        {
            if (!(SelectionList.View is System.Windows.Controls.GridView gv)) return;
            if (gv.Columns.Count < 8) return;

            double avail = SelectionList.ActualWidth - 4;
            if (avail <= 0) return;

            double rank     = 48;
            double spId     = 78;
            double score    = 80;
            double priority = 90;
            double type     = 150;
            double statusW  = 96;

            double flex = Math.Max(160, avail - rank - spId - score - priority - type - statusW);

            gv.Columns[0].Width = rank;
            gv.Columns[1].Width = spId;
            gv.Columns[2].Width = Math.Floor(flex * 0.57);
            gv.Columns[3].Width = Math.Floor(flex * 0.43);
            gv.Columns[4].Width = score;
            gv.Columns[5].Width = priority;
            gv.Columns[6].Width = type;
            gv.Columns[7].Width = statusW;
        }

        private void ResizeFinalColumns()
        {
            if (!(FinalList.View is System.Windows.Controls.GridView gv)) return;
            if (gv.Columns.Count < 9) return;

            double avail = FinalList.ActualWidth - 4;
            if (avail <= 0) return;

            double rank     = 48;
            double spId     = 78;
            double score    = 80;
            double priority = 90;
            double type     = 140;
            double finalSt  = 120;
            double action   = 168;

            double flex = Math.Max(160, avail - rank - spId - score - priority - type - finalSt - action);

            gv.Columns[0].Width = rank;
            gv.Columns[1].Width = spId;
            gv.Columns[2].Width = Math.Floor(flex * 0.57);
            gv.Columns[3].Width = Math.Floor(flex * 0.43);
            gv.Columns[4].Width = score;
            gv.Columns[5].Width = priority;
            gv.Columns[6].Width = type;
            gv.Columns[7].Width = finalSt;
            gv.Columns[8].Width = action;
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
                RebuildSelectionList();
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
                RebuildSelectionList();
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

        private void ApplyBudget_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtBudgetAmount.Text, out int amt) && amt >= 0)
            {
                _budgetAmount = amt;
                int slots = _budgetAmount / 1000;
                TxtGranteeCount.Text = slots + (slots == 1 ? " grantee" : " grantees");
                RebuildSelectionList();
            }
            else
            {
                ToastNotification.Show("Invalid Input", "Please enter a valid budget amount.", ToastType.Warning);
            }
        }

        private void BudgetInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtGranteeCount == null) return;
            if (int.TryParse(TxtBudgetAmount.Text, out int amt) && amt >= 0)
            {
                int slots = amt / 1000;
                TxtGranteeCount.Text = slots + (slots == 1 ? " grantee" : " grantees");
            }
            else
            {
                TxtGranteeCount.Text = "—";
            }
        }

        // marks a selection row as a final grantee
        private void ConfirmGrantee_Click(object sender, RoutedEventArgs e)
        {
            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            SelectionRow row = _selectionRows.FirstOrDefault(r => r.SpId == id);
            if (row != null)
            {
                row.IsFinalGrantee = true;
                UpdateFinalList();
                ToastNotification.Show("Confirmed", row.Name + " marked as final grantee.", ToastType.Success);
            }
        }

        // removes final grantee status
        private void RevokeGrantee_Click(object sender, RoutedEventArgs e)
        {
            string id = (sender as Button)?.Tag?.ToString() ?? string.Empty;
            SelectionRow row = _selectionRows.FirstOrDefault(r => r.SpId == id);
            if (row != null)
            {
                row.IsFinalGrantee = false;
                UpdateFinalList();
                ToastNotification.Show("Revoked", row.Name + " removed from final grantees.", ToastType.Warning);
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
            sb.AppendLine("SP ID,Name,Barangay,Subsidy Type,Priority,Score,Status");
            foreach (var i in _allItems)
                sb.AppendLine(i.SpId + "," + i.Name + "," + i.Barangay + "," + i.SubsidyType + "," + i.Priority + "," + i.Score + "," + i.Status);
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
            sb.AppendLine("SP ID,Name,Barangay,Subsidy Type,Priority,Score,Status");
            sb.AppendLine(item.SpId + "," + item.Name + "," + item.Barangay + "," + item.SubsidyType + "," + item.Priority + "," + item.Score + "," + item.Status);
            System.IO.File.WriteAllText(dlg.FileName, sb.ToString());
            MessageBox.Show("Exported to:\n" + dlg.FileName, "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
