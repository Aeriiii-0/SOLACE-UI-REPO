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

namespace SOLUM_UI
{
    public partial class AnalyticsPage : Page
    {
        private int? _filterYear  = null;
        private int? _filterMonth = null;

        private readonly List<SoloParentRecord> _records = new List<SoloParentRecord>
        {
            new SoloParentRecord { Id="SP-001", Name="Santos, Maria Lim",             Sex="Female", CivilStatus="Single",    Barangay="Biñan Poblacion", Children=2, Status="Valid",    LastUpdated=new DateTime(2025,3,15)  },
            new SoloParentRecord { Id="SP-002", Name="Dela Cruz, Juan Reyes Jr.",     Sex="Male",   CivilStatus="Widowed",   Barangay="Malaban",         Children=3, Status="Valid",    LastUpdated=new DateTime(2025,3,12)  },
            new SoloParentRecord { Id="SP-003", Name="Reyes, Ana Mendoza",            Sex="Female", CivilStatus="Separated", Barangay="Canlalay",        Children=1, Status="Inactive", LastUpdated=new DateTime(2025,3,14)  },
            new SoloParentRecord { Id="SP-004", Name="Garcia, Pedro Torres III",      Sex="Male",   CivilStatus="Single",    Barangay="San Antonio",     Children=2, Status="Valid",    LastUpdated=new DateTime(2025,3,9)   },
            new SoloParentRecord { Id="SP-005", Name="Martinez, Rosa Cruz",           Sex="Female", CivilStatus="Widowed",   Barangay="Platero",         Children=4, Status="Inactive", LastUpdated=new DateTime(2025,3,28)  },
            new SoloParentRecord { Id="SP-006", Name="Lim, Cynthia Tan",              Sex="Female", CivilStatus="Separated", Barangay="Loma",            Children=2, Status="Valid",    LastUpdated=new DateTime(2025,4,1)   },
            new SoloParentRecord { Id="SP-007", Name="Bautista, Carlos Ocampo",       Sex="Male",   CivilStatus="Widowed",   Barangay="Tubigan",         Children=3, Status="Valid",    LastUpdated=new DateTime(2025,4,5)   },
            new SoloParentRecord { Id="SP-008", Name="Mendoza, Elena Flores",         Sex="Female", CivilStatus="Single",    Barangay="De La Paz",       Children=1, Status="Inactive", LastUpdated=new DateTime(2025,4,10)  },
            new SoloParentRecord { Id="SP-009", Name="Torres, Benjamin Ramos",        Sex="Male",   CivilStatus="Separated", Barangay="Casile",          Children=2, Status="Inactive", LastUpdated=new DateTime(2025,4,12)  },
            new SoloParentRecord { Id="SP-010", Name="Navarro, Josephine Aquino",     Sex="Female", CivilStatus="Widowed",   Barangay="San Jose",        Children=3, Status="Valid",    LastUpdated=new DateTime(2025,4,15)  },
            new SoloParentRecord { Id="SP-011", Name="Hernandez, Roberto Diaz",       Sex="Male",   CivilStatus="Annulled",  Barangay="Langkiwa",        Children=4, Status="Valid",    LastUpdated=new DateTime(2025,4,18)  },
            new SoloParentRecord { Id="SP-012", Name="Castillo, Patricia Villanueva", Sex="Female", CivilStatus="Single",    Barangay="Malamig",         Children=1, Status="Inactive", LastUpdated=new DateTime(2025,4,20)  },
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

        private void FilterDrop_Click(object sender, RoutedEventArgs e)
        {
            FilterPopup.IsOpen = true;
        }

        private void FilterApply_Click(object sender, RoutedEventArgs e)
        {
            var yearItem  = CmbFilterYear.SelectedItem  as ComboBoxItem;
            var monthItem = CmbFilterMonth.SelectedItem as ComboBoxItem;
            _filterYear  = yearItem?.Tag  as int?;
            _filterMonth = monthItem?.Tag as int?;
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

        private void BuildAnalytics()
        {
            var data    = GetFiltered();
            int total   = data.Count;
            int active  = data.Count(r => r.Status == "Valid");
            int inactive= data.Count(r => r.Status == "Inactive");
            int totKids = data.Sum(r => r.Children);
            int female  = data.Count(r => r.Sex == "Female");
            int male    = data.Count(r => r.Sex == "Male");

            string periodLabel = _filterYear == null && _filterMonth == null ? "All time"
                               : _filterYear != null && _filterMonth != null
                                   ? new DateTime(2000, _filterMonth.Value, 1).ToString("MMMM") + " " + _filterYear
                               : _filterYear  != null ? _filterYear.ToString()
                               : new DateTime(2000, _filterMonth.Value, 1).ToString("MMMM");

            TxtTotalRecords.Text    = total.ToString();
            TxtPeriodLabel.Text     = periodLabel;
            TxtActiveRecords.Text   = active.ToString();
            TxtActiveRate.Text      = total > 0 ? Math.Round(active   * 100.0 / total, 0) + "% valid"   : "—";
            TxtInactiveRecords.Text = inactive.ToString();
            TxtInactiveRate.Text    = total > 0 ? Math.Round(inactive * 100.0 / total, 0) + "% inactive" : "—";
            TxtTotalChildren.Text   = totKids.ToString();
            TxtAvgChildren.Text     = total > 0 ? Math.Round((double)totKids / total, 1).ToString("0.0") + " avg." : "—";
            TxtFemaleCount.Text     = female.ToString();
            TxtMaleCount.Text       = male + " male";
            TxtFemaleCircle.Text    = female.ToString();
            TxtMaleCircle.Text      = male.ToString();
            TxtFemaleRate.Text      = total > 0 ? Math.Round(female * 100.0 / total, 0) + "%" : "0%";
            TxtMaleRate.Text        = total > 0 ? Math.Round(male   * 100.0 / total, 0) + "%" : "0%";
            TxtLastRefresh.Text     = "Updated " + DateTime.Now.ToString("MMM d, h:mm tt");

            DrawBarangayChart(data);

            var civilGroups = data.GroupBy(r => r.CivilStatus ?? "Unknown")
                                  .OrderByDescending(g => g.Count()).ToList();
            int maxC = civilGroups.Any() ? civilGroups.Max(g => g.Count()) : 1;
            var palette = new[] { "#702943","#9B3060","#C4788E","#D4A0B0","#E8D0D7" };
            int ci = 0;
            CivilStatusChart.ItemsSource = civilGroups.Select(g =>
            {
                var hex = palette[ci++ % palette.Length];
                var c   = (Color)ColorConverter.ConvertFromString(hex);
                return new ChartRow { Label = g.Key, Count = g.Count(), BarWidth = Math.Max(4, g.Count() * 130.0 / maxC), BarColor = new SolidColorBrush(c) };
            }).ToList();

            var recent = data.OrderByDescending(r => r.LastUpdated).Take(12).ToList();
            RecentList.ItemsSource = recent.Select(r => new RecentRow
            {
                SpId      = r.Id,
                Name      = r.Name,
                Barangay  = r.Barangay,
                DateLabel = r.LastUpdated.ToString("MMM d, yyyy"),
                Status    = r.Status,
                StatusBg  = r.Status == "Valid"
                    ? new SolidColorBrush(Color.FromRgb(0xE8,0xF5,0xE9))
                    : new SolidColorBrush(Color.FromRgb(0xF2,0xF2,0xF2)),
                StatusFg  = r.Status == "Valid"
                    ? new SolidColorBrush(Color.FromRgb(0x27,0xAE,0x60))
                    : new SolidColorBrush(Color.FromRgb(0x99,0x99,0x99))
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

            int maxCount  = groups.Max(g => g.Count());
            int n         = groups.Count;
            double totalBarW = canvasW * 0.65;
            double barW   = Math.Floor(totalBarW / n);
            double totalGap  = canvasW - barW * n;
            double gap    = totalGap / (n + 1);
            double chartH = canvasH - 2;

            var gridBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xD8, 0xDE));
            for (int step = 1; step <= 4; step++)
            {
                double y = chartH * (1 - step / 4.0);
                BarangayCanvas.Children.Add(new Line
                {
                    X1 = 0, X2 = canvasW, Y1 = y, Y2 = y,
                    Stroke = gridBrush, StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection(new double[] { 4, 3 })
                });
            }

            var barFill = new SolidColorBrush(Color.FromRgb(0x70,0x29,0x43));
            var barHov  = new SolidColorBrush(Color.FromRgb(0x9B,0x30,0x60));

            for (int i = 0; i < n; i++)
            {
                double barH = Math.Max(4, groups[i].Count() * chartH / maxCount);
                double x    = gap + i * (barW + gap);
                double y    = chartH - barH;

                var rect = new Rectangle
                {
                    Width   = barW,
                    Height  = barH,
                    Fill    = barFill,
                    RadiusX = 4,
                    RadiusY = 4,
                    Tag     = groups[i].Key,
                    Cursor  = Cursors.Hand,
                    ToolTip = groups[i].Key + "  ·  " + groups[i].Count() + " record(s)"
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                rect.MouseEnter += (s, e) => ((Rectangle)s).Fill = barHov;
                rect.MouseLeave += (s, e) => ((Rectangle)s).Fill = barFill;
                BarangayCanvas.Children.Add(rect);

                var lbl = new TextBlock
                {
                    Text       = groups[i].Count().ToString(),
                    FontSize   = 9,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = barFill,
                    FontFamily = new FontFamily("Segoe UI")
                };
                Canvas.SetLeft(lbl, x + barW / 2 - 4);
                Canvas.SetTop(lbl,  Math.Max(0, y - 14));
                BarangayCanvas.Children.Add(lbl);
            }

            BarangayLabels.ItemsSource = groups.Select(g => new ChartRow { Label = g.Key }).ToList();
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
            public double          BarWidth { get; set; }
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
