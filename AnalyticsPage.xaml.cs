using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SOLUM_UI.Models;
using SOLUM_UI.Services;

namespace SOLUM_UI
{
    public partial class AnalyticsPage : Page
    {
        private int? _filterYear  = null;
        private int? _filterMonth = null;
        private string _currentPeriod = "All time";

        private readonly List<SoloParentRecord> _records = new List<SoloParentRecord>
        {
            new SoloParentRecord { Id="SP-001", Name="Santos, Maria Lim",             Sex="Female", CivilStatus="Single",    Barangay="Biñan Poblacion", Children=2, Status="Valid",    IsEmployed=true,  MonthlyIncome="below minimum wage", DateOfBirth=new DateTime(1990,3,15), CircumstanceA7=true,  IsNewApplicant=true,  LastUpdated=new DateTime(2025,3,15) },
            new SoloParentRecord { Id="SP-002", Name="Dela Cruz, Juan Reyes Jr.",     Sex="Male",   CivilStatus="Widowed",   Barangay="Malaban",         Children=3, Status="Valid",    IsSelfEmployed=true, MonthlyIncome="below minimum wage", DateOfBirth=new DateTime(1985,1,10), CircumstanceA2=true,  IsNewApplicant=true,  LastUpdated=new DateTime(2025,3,12) },
            new SoloParentRecord { Id="SP-003", Name="Reyes, Ana Mendoza",            Sex="Female", CivilStatus="Separated", Barangay="Canlalay",        Children=1, Status="Inactive", IsSelfEmployed=true, MonthlyIncome="below minimum wage", DateOfBirth=new DateTime(1993,6,20), CircumstanceA7=true,  IsRenewal=true,       LastUpdated=new DateTime(2025,3,14) },
            new SoloParentRecord { Id="SP-004", Name="Garcia, Pedro Torres III",      Sex="Male",   CivilStatus="Single",    Barangay="San Antonio",     Children=2, Status="Valid",    IsEmployed=true,  MonthlyIncome="Minimum wage +1 to Php 20833", DateOfBirth=new DateTime(1988,9,5),  CircumstanceC=true,   IsRenewal=true,       LastUpdated=new DateTime(2025,3,9)  },
            new SoloParentRecord { Id="SP-005", Name="Martinez, Rosa Cruz",           Sex="Female", CivilStatus="Widowed",   Barangay="Platero",         Children=4, Status="Inactive", IsNotEmployed=true, MonthlyIncome="below minimum wage", DateOfBirth=new DateTime(1978,11,2), CircumstanceA2=true,  IsNewApplicant=true,  LastUpdated=new DateTime(2025,3,28) },
            new SoloParentRecord { Id="SP-006", Name="Lim, Cynthia Tan",              Sex="Female", CivilStatus="Separated", Barangay="Loma",            Children=2, Status="Valid",    IsEmployed=true,  MonthlyIncome="Minimum wage +1 to Php 20833", DateOfBirth=new DateTime(1995,4,14), CircumstanceA7=true,  IsRenewal=true,       LastUpdated=new DateTime(2025,4,1)  },
            new SoloParentRecord { Id="SP-007", Name="Bautista, Carlos Ocampo",       Sex="Male",   CivilStatus="Widowed",   Barangay="Tubigan",         Children=3, Status="Valid",    IsSelfEmployed=true, MonthlyIncome="below minimum wage", DateOfBirth=new DateTime(1982,7,22), CircumstanceA2=true,  IsRenewal=true,       LastUpdated=new DateTime(2025,4,5)  },
            new SoloParentRecord { Id="SP-008", Name="Mendoza, Elena Flores",         Sex="Female", CivilStatus="Single",    Barangay="De La Paz",       Children=1, Status="Inactive", IsNotEmployed=true, MonthlyIncome="below minimum wage", DateOfBirth=new DateTime(2005,8,18), CircumstanceC=true,   IsNewApplicant=true,  LastUpdated=new DateTime(2025,4,10) },
            new SoloParentRecord { Id="SP-009", Name="Torres, Benjamin Ramos",        Sex="Male",   CivilStatus="Separated", Barangay="Casile",          Children=2, Status="Inactive", IsEmployed=true,  MonthlyIncome="Php 20834 and above",  DateOfBirth=new DateTime(1991,12,3), CircumstanceA5=true,  IsRenewal=true,       LastUpdated=new DateTime(2025,4,12) },
            new SoloParentRecord { Id="SP-010", Name="Navarro, Josephine Aquino",     Sex="Female", CivilStatus="Widowed",   Barangay="San Jose",        Children=3, Status="Valid",    IsEmployed=true,  MonthlyIncome="below minimum wage", DateOfBirth=new DateTime(1987,5,7),  CircumstanceA2=true,  IsRenewal=true,       LastUpdated=new DateTime(2025,4,15) },
            new SoloParentRecord { Id="SP-011", Name="Hernandez, Roberto Diaz",       Sex="Male",   CivilStatus="Annulled",  Barangay="Langkiwa",        Children=4, Status="Valid",    IsEmployed=true,  MonthlyIncome="Php 20834 and above",  DateOfBirth=new DateTime(1980,2,28), CircumstanceA6=true,  IsRenewal=true,       LastUpdated=new DateTime(2025,4,18) },
            new SoloParentRecord { Id="SP-012", Name="Castillo, Patricia Villanueva", Sex="Female", CivilStatus="Single",    Barangay="Malamig",         Children=1, Status="Inactive", IsSelfEmployed=true, MonthlyIncome="below minimum wage", DateOfBirth=new DateTime(1997,10,11),CircumstanceC=true,   IsNewApplicant=true,  LastUpdated=new DateTime(2025,4,20) },
        };

        public AnalyticsPage()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                PopulateFilterDropdowns();
                BuildAnalytics();
                BarangayCanvas.SizeChanged += (ss, ee) => DrawBarangayChart(GetFiltered());
            };
        }

        private void PopulateFilterDropdowns()
        {
            var years = _records.Select(r => r.LastUpdated.Year).Distinct().OrderByDescending(y => y).ToList();
            CmbFilterYear.Items.Clear();
            CmbFilterYear.Items.Add(new ComboBoxItem { Content = "All Years", Tag = null });
            foreach (var y in years)
                CmbFilterYear.Items.Add(new ComboBoxItem { Content = y.ToString(), Tag = y });
            CmbFilterYear.SelectedIndex = 0;

            CmbFilterMonth.Items.Clear();
            CmbFilterMonth.Items.Add(new ComboBoxItem { Content = "All Months", Tag = null });
            for (int m = 1; m <= 12; m++)
                CmbFilterMonth.Items.Add(new ComboBoxItem { Content = new DateTime(2000, m, 1).ToString("MMMM"), Tag = m });
            CmbFilterMonth.SelectedIndex = 0;
        }

        private void FilterDrop_Click(object sender, RoutedEventArgs e) => FilterPopup.IsOpen = true;

        private void FilterApply_Click(object sender, RoutedEventArgs e)
        {
            _filterYear  = (CmbFilterYear.SelectedItem  as ComboBoxItem)?.Tag as int?;
            _filterMonth = (CmbFilterMonth.SelectedItem as ComboBoxItem)?.Tag as int?;
            FilterPopup.IsOpen = false;
            UpdateFilterBadge();
            BuildAnalytics();
        }

        private void FilterReset_Click(object sender, RoutedEventArgs e)
        {
            CmbFilterYear.SelectedIndex  = 0;
            CmbFilterMonth.SelectedIndex = 0;
            _filterYear  = null;
            _filterMonth = null;
            FilterPopup.IsOpen = false;
            UpdateFilterBadge();
            BuildAnalytics();
        }

        private void UpdateFilterBadge()
        {
            if (_filterYear == null && _filterMonth == null)
            {
                ActiveFilterBadge.Visibility = Visibility.Collapsed;
                return;
            }
            var parts = new List<string>();
            if (_filterYear  != null) parts.Add(_filterYear.ToString());
            if (_filterMonth != null) parts.Add(new DateTime(2000, _filterMonth.Value, 1).ToString("MMMM"));
            TxtActiveFilter.Text         = string.Join(" · ", parts);
            ActiveFilterBadge.Visibility = Visibility.Visible;
        }

        private List<SoloParentRecord> GetFiltered()
        {
            return _records.Where(r =>
                (_filterYear  == null || r.LastUpdated.Year  == _filterYear.Value) &&
                (_filterMonth == null || r.LastUpdated.Month == _filterMonth.Value)
            ).ToList();
        }

        private int age(SoloParentRecord r) =>
            r.DateOfBirth != DateTime.MinValue
                ? (int)((DateTime.Today - r.DateOfBirth).TotalDays / 365.25)
                : 0;

        private void BuildAnalytics()
        {
            var d = GetFiltered();
            int total = d.Count;

            string period = _filterYear == null && _filterMonth == null ? "All time"
                : _filterYear != null && _filterMonth != null
                    ? new DateTime(2000, _filterMonth.Value, 1).ToString("MMMM") + " " + _filterYear
                : _filterYear != null ? _filterYear.ToString()
                : new DateTime(2000, _filterMonth.Value, 1).ToString("MMMM");

            TxtTotalRecords.Text = total.ToString();
            TxtPeriodLabel.Text  = period;
            _currentPeriod       = period;
            TxtLastRefresh.Text  = "Updated " + DateTime.Now.ToString("MMM d, h:mm tt");

            int valid    = d.Count(r => r.Status == "Valid");
            int inactive = d.Count(r => r.Status == "Inactive");
            TxtValidRecords.Text   = valid.ToString();
            TxtValidRate.Text      = total > 0 ? Math.Round(valid    * 100.0 / total, 0) + "% of total" : "—";
            TxtInactiveRecords.Text = inactive.ToString();
            TxtInactiveRate.Text    = total > 0 ? Math.Round(inactive * 100.0 / total, 0) + "% of total" : "—";

            int newApplicants = d.Count(r => r.IsNewApplicant);
            int renewals      = d.Count(r => r.IsRenewal);
            TxtNewSpic.Text     = newApplicants.ToString();
            TxtRenewedSpic.Text = renewals.ToString();

            // for showing gender analytics
            int female = d.Count(r => r.Sex == "Female");
            int male   = d.Count(r => r.Sex == "Male");
            TxtFemaleCircle.Text = female.ToString();
            TxtMaleCircle.Text   = male.ToString();

            int totKids = d.Sum(r => r.Children);
            TxtTotalChildren.Text = totKids.ToString();
            TxtAvgChildren.Text   = total > 0 ? Math.Round((double)totKids / total, 1).ToString("0.0") + " avg." : "—";

            bindHorizBars(AgeChart, new[]
            {
                ("19 & below",   d.Count(r => age(r) <= 19)),
                ("20 – 39",      d.Count(r => age(r) >= 20 && age(r) <= 39)),
                ("40 – 59",      d.Count(r => age(r) >= 40 && age(r) <= 59)),
                ("60 and above", d.Count(r => age(r) >= 60)),
            }, "#702943");

            bindHorizBars(CivilStatusChart, new[]
            {
                ("Single",      d.Count(r => r.CivilStatus == "Single")),
                ("Married",     d.Count(r => r.CivilStatus == "Married")),
                ("Widowed",     d.Count(r => r.CivilStatus == "Widowed")),
                ("Sep./Annul.", d.Count(r => r.CivilStatus == "Separated" || r.CivilStatus == "Annulled")),
            }, "#9B3060");

            bindHorizBars(EmploymentChart, new[]
            {
                ("Employed",     d.Count(r => r.IsEmployed)),
                ("Self-Employed",d.Count(r => r.IsSelfEmployed)),
                ("Not Employed", d.Count(r => r.IsNotEmployed)),
            }, "#C4788E");

            bindHorizBars(IncomeChart, new[]
            {
                ("Below Min. Wage",    d.Count(r => r.MonthlyIncome == "below minimum wage")),
                ("Min.+1 – ₱20,833",  d.Count(r => r.MonthlyIncome == "Minimum wage +1 to Php 20833")),
                ("₱20,834 and above", d.Count(r => r.MonthlyIncome == "Php 20834 and above")),
            }, "#702943");

            bindHorizBars(DependantsAgeChart, new[]
            {
                ("6 & below",      d.Sum(r => r.FamilyMembers.Count(m => { int a = 0; int.TryParse(m.Age, out a); return a <= 6; }))),
                ("7 – 22",         d.Sum(r => r.FamilyMembers.Count(m => { int a = 0; int.TryParse(m.Age, out a); return a >= 7 && a <= 22; }))),
                ("22 and above",   d.Sum(r => r.FamilyMembers.Count(m => { int a = 0; int.TryParse(m.Age, out a); return a > 22; }))),
            }, "#F57F17");

            var catRows = new[]
            {
                ("a1. Consequence of rape",               d.Count(r => r.CircumstanceA1)),
                ("a2. Widow/widower",                     d.Count(r => r.CircumstanceA2)),
                ("a3. Spouse of PDL",                     d.Count(r => r.CircumstanceA3)),
                ("a4. Spouse of PWD",                     d.Count(r => r.CircumstanceA4)),
                ("a5. Separated/de facto",                d.Count(r => r.CircumstanceA5)),
                ("a6. Annulled",                          d.Count(r => r.CircumstanceA6)),
                ("a7. Abandoned",                         d.Count(r => r.CircumstanceA7)),
                ("b. Spouse/Relative of OFW",             d.Count(r => r.CircumstanceB)),
                ("c. Unmarried person",                   d.Count(r => r.CircumstanceC)),
                ("d. Guardian/Adoptive/Foster",           d.Count(r => r.CircumstanceD)),
                ("e. Relative",                           d.Count(r => r.CircumstanceE)),
                ("f. Pregnant woman",                     d.Count(r => r.CircumstanceF)),
            };
            int catTotal = catRows.Sum(x => x.Item2);
            var color702 = (Color)ColorConverter.ConvertFromString("#702943");
            var catBrush = new SolidColorBrush(color702);
            CategoryChartLeft.ItemsSource  = catRows.Take(6).Select(r => new ChartRow { Label = r.Item1, Count = r.Item2, Pct = catTotal > 0 ? Math.Round(r.Item2 * 100.0 / catTotal, 0).ToString("0") + "%" : "—", BarColor = catBrush }).ToList();
            CategoryChartRight.ItemsSource = catRows.Skip(6).Select(r => new ChartRow { Label = r.Item1, Count = r.Item2, Pct = catTotal > 0 ? Math.Round(r.Item2 * 100.0 / catTotal, 0).ToString("0") + "%" : "—", BarColor = catBrush }).ToList();

            DrawBarangayChart(d);

            var recent = d.OrderByDescending(r => r.LastUpdated).Take(12).ToList();
            RecentList.ItemsSource = recent.Select(r => new RecentRow
            {
                SpId      = r.Id,
                Name      = r.Name,
                Barangay  = r.Barangay,
                DateLabel = r.LastUpdated.ToString("MMM d, yyyy"),
                Status    = r.Status,
                StatusBg  = r.Status == "Valid"
                    ? new SolidColorBrush(Color.FromRgb(0xE8, 0xF5, 0xE9))
                    : new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2)),
                StatusFg  = r.Status == "Valid"
                    ? new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60))
                    : new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99))
            }).ToList();
        }

        private void bindHorizBars(ItemsControl chart, (string label, int count)[] rows, string hexColor)
        {
            int total = rows.Sum(r => r.count);
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            chart.ItemsSource = rows.Select(r => new ChartRow
            {
                Label    = r.label,
                Count    = r.count,
                Pct      = total > 0 ? Math.Round(r.count * 100.0 / total, 0).ToString("0") + "%" : "—",
                BarColor = new SolidColorBrush(color)
            }).ToList();
        }

        private void DrawBarangayChart(List<SoloParentRecord> data)
        {
            BarangayCanvas.Children.Clear();
            BarangayLabels.ItemsSource = null;

            var groups = data.GroupBy(r => r.Barangay ?? "Unknown")
                             .OrderByDescending(g => g.Count())
                             .Take(10).ToList();
            if (!groups.Any()) return;

            double canvasH = BarangayCanvas.ActualHeight > 0 ? BarangayCanvas.ActualHeight : 160;
            double canvasW = BarangayCanvas.ActualWidth  > 0 ? BarangayCanvas.ActualWidth  : 400;
            int    maxCount = groups.Max(g => g.Count());
            int    n        = groups.Count;
            double totalBarW = canvasW * 0.65;
            double barW     = Math.Floor(totalBarW / n);
            double gap      = (canvasW - barW * n) / (n + 1);
            double chartH   = canvasH - 2;

            var gridBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xD8, 0xDE));
            for (int step = 1; step <= 4; step++)
            {
                double gy = chartH * (1 - step / 4.0);
                BarangayCanvas.Children.Add(new Line
                {
                    X1 = 0, X2 = canvasW, Y1 = gy, Y2 = gy,
                    Stroke = gridBrush, StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection(new double[] { 4, 3 })
                });
            }

            var fill = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));
            var hov  = new SolidColorBrush(Color.FromRgb(0x9B, 0x30, 0x60));

            for (int i = 0; i < n; i++)
            {
                double barH = Math.Max(4, groups[i].Count() * chartH / maxCount);
                double x    = gap + i * (barW + gap);
                double y    = chartH - barH;
                var rect = new Rectangle
                {
                    Width = barW, Height = barH, Fill = fill,
                    RadiusX = 4, RadiusY = 4,
                    Tag     = groups[i].Key, Cursor = Cursors.Hand,
                    ToolTip = groups[i].Key + "  ·  " + groups[i].Count() + " record(s)"
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                rect.MouseEnter += (s, e) => ((Rectangle)s).Fill = hov;
                rect.MouseLeave += (s, e) => ((Rectangle)s).Fill = fill;
                BarangayCanvas.Children.Add(rect);

                var lbl = new TextBlock
                {
                    Text = groups[i].Count().ToString(),
                    FontSize = 9, FontWeight = FontWeights.SemiBold,
                    Foreground = fill, FontFamily = new FontFamily("Segoe UI")
                };
                Canvas.SetLeft(lbl, x + barW / 2 - 4);
                Canvas.SetTop(lbl, Math.Max(0, y - 14));
                BarangayCanvas.Children.Add(lbl);
            }

            BarangayLabels.ItemsSource = groups.Select(g => new ChartRow { Label = g.Key }).ToList();
        }

        private void ExportXlsx_Click(object sender, RoutedEventArgs e)
        {
            var saveDlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName   = "SoloParents_Summary_" + DateTime.Now.ToString("yyyyMMdd"),
                DefaultExt = ".xlsx",
                Filter     = "Excel Workbook (*.xlsx)|*.xlsx"
            };
            if (saveDlg.ShowDialog() != true) return;

            var mw = Window.GetWindow(this) as MainWindow;
            if (mw != null) mw.MainContent.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 };

            double w = mw?.Width  ?? 1280;
            double h = mw?.Height ?? 800;
            double l = mw?.Left   ?? 0;
            double t = mw?.Top    ?? 0;

            var optsDlg = new ExportOptionsDialog(
                System.IO.Path.GetFileName(saveDlg.FileName), w, h, l, t)
            { Owner = mw ?? Window.GetWindow(this) };
            optsDlg.ShowDialog();

            if (mw != null) mw.MainContent.Effect = null;

            if (!optsDlg.Confirmed) return;

            try
            {
                AnalyticsExportService.Export(saveDlg.FileName, GetFiltered(), _currentPeriod, optsDlg.Password);
                ToastNotification.Show("Exported", "Saved to " + System.IO.Path.GetFileName(saveDlg.FileName), ToastType.Success);
                if (optsDlg.OpenAfter)
                    System.Diagnostics.Process.Start(saveDlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export failed:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RecentRecord_Click(object sender, MouseButtonEventArgs e)
        {
            string spId = (sender as FrameworkElement)?.Tag?.ToString();
            if (string.IsNullOrEmpty(spId)) return;
            var record = _records.FirstOrDefault(r => r.Id == spId);
            if (record == null) return;

            MainWindow mw = Window.GetWindow(this) as MainWindow;
            if (mw != null) mw.MainContent.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 };

            var view = new RecordViewDialog(record) { Owner = Window.GetWindow(this) };
            view.Closed += (s, args) => { if (mw != null) mw.MainContent.Effect = null; };
            view.ShowDialog();

            if (view.OpenedEdit)
            {
                if (mw != null) mw.MainContent.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 };
                var dlg = new RecordDialog(record) { Owner = Window.GetWindow(this) };
                dlg.Closed += (s, args) => { if (mw != null) mw.MainContent.Effect = null; };
                dlg.ShowDialog();
            }
        }

        private class ChartRow
        {
            public string          Label    { get; set; }
            public int             Count    { get; set; }
            public string          Pct      { get; set; }
            public SolidColorBrush BarColor { get; set; }
        }

        private class RecentRow
        {
            public string          SpId      { get; set; }
            public string          Name      { get; set; }
            public string          Barangay  { get; set; }
            public string          DateLabel { get; set; }
            public string          Status    { get; set; }
            public SolidColorBrush StatusBg  { get; set; }
            public SolidColorBrush StatusFg  { get; set; }
        }
    }
}
