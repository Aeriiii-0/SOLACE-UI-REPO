using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using SOLUM_UI.Models;

namespace SOLUM_UI
{
    public partial class AnalyticsPage : Page
    {
        private readonly List<SoloParentRecord> _records = new List<SoloParentRecord>
        {
            new SoloParentRecord { Name = "Santos, Maria Lim",          Sex = "Female", CivilStatus = "Single",    Barangay = "Biñan Poblacion", Children = 2, Status = "Valid",    LastUpdated = new DateTime(2025, 3, 15) },
            new SoloParentRecord { Name = "Dela Cruz, Juan Reyes Jr.",  Sex = "Male",   CivilStatus = "Widowed",   Barangay = "Malaban",         Children = 3, Status = "Valid",    LastUpdated = new DateTime(2025, 3, 12) },
            new SoloParentRecord { Name = "Reyes, Ana Mendoza",         Sex = "Female", CivilStatus = "Separated", Barangay = "Canlalay",        Children = 1, Status = "Inactive", LastUpdated = new DateTime(2025, 3, 14) },
            new SoloParentRecord { Name = "Garcia, Pedro Torres III",   Sex = "Male",   CivilStatus = "Single",    Barangay = "San Antonio",     Children = 2, Status = "Valid",    LastUpdated = new DateTime(2025, 3, 9)  },
            new SoloParentRecord { Name = "Martinez, Rosa Cruz",        Sex = "Female", CivilStatus = "Widowed",   Barangay = "Platero",         Children = 4, Status = "Inactive", LastUpdated = new DateTime(2025, 3, 28) },
            new SoloParentRecord { Name = "Lim, Cynthia Tan",           Sex = "Female", CivilStatus = "Separated", Barangay = "Loma",            Children = 2, Status = "Valid",    LastUpdated = new DateTime(2025, 4, 1)  },
            new SoloParentRecord { Name = "Bautista, Carlos Ocampo",    Sex = "Male",   CivilStatus = "Widowed",   Barangay = "Tubigan",         Children = 3, Status = "Valid",    LastUpdated = new DateTime(2025, 4, 5)  },
            new SoloParentRecord { Name = "Mendoza, Elena Flores",      Sex = "Female", CivilStatus = "Single",    Barangay = "De La Paz",       Children = 1, Status = "Inactive", LastUpdated = new DateTime(2025, 4, 10) },
            new SoloParentRecord { Name = "Torres, Benjamin Ramos",     Sex = "Male",   CivilStatus = "Separated", Barangay = "Casile",          Children = 2, Status = "Inactive", LastUpdated = new DateTime(2025, 4, 12) },
            new SoloParentRecord { Name = "Navarro, Josephine Aquino",  Sex = "Female", CivilStatus = "Widowed",   Barangay = "San Jose",        Children = 3, Status = "Valid",    LastUpdated = new DateTime(2025, 4, 15) },
            new SoloParentRecord { Name = "Hernandez, Roberto Diaz",    Sex = "Male",   CivilStatus = "Annulled",  Barangay = "Langkiwa",        Children = 4, Status = "Valid",    LastUpdated = new DateTime(2025, 4, 18) },
            new SoloParentRecord { Name = "Castillo, Patricia Villanueva", Sex = "Female", CivilStatus = "Single", Barangay = "Malamig",         Children = 1, Status = "Inactive", LastUpdated = new DateTime(2025, 4, 20) },
        };

        public AnalyticsPage()
        {
            InitializeComponent();
            Loaded += (s, e) => BuildAnalytics();
        }

        private void BuildAnalytics()
        {
            int total    = _records.Count;
            int active   = _records.Count(r => r.Status == "Valid");
            int inactive = _records.Count(r => r.Status == "Inactive");
            int totKids  = _records.Sum(r => r.Children);
            int newThisMonth = _records.Count(r =>
                r.LastUpdated.Year  == DateTime.Today.Year &&
                r.LastUpdated.Month == DateTime.Today.Month);

            TxtTotalRecords.Text   = total.ToString();
            TxtActiveRecords.Text  = active.ToString();
            TxtInactiveRecords.Text = inactive.ToString();
            TxtAvgChildren.Text    = total > 0 ? Math.Round((double)totKids / total, 1).ToString("0.0") : "0";
            TxtTotalChildren.Text  = totKids + " total";
            TxtNewThisMonth.Text   = newThisMonth + " this month";
            TxtActiveRate.Text     = total > 0 ? Math.Round(active   * 100.0 / total, 0) + "% of total" : "0%";
            TxtInactiveRate.Text   = total > 0 ? Math.Round(inactive * 100.0 / total, 0) + "% of total" : "0%";
            TxtLastRefresh.Text    = "Updated " + DateTime.Now.ToString("MMM d, h:mm tt");

            int female = _records.Count(r => r.Sex == "Female");
            int male   = _records.Count(r => r.Sex == "Male");
            TxtFemaleCount.Text = female.ToString();
            TxtMaleCount.Text   = male.ToString();
            TxtFemaleRate.Text  = total > 0 ? Math.Round(female * 100.0 / total, 0) + "%" : "0%";
            TxtMaleRate.Text    = total > 0 ? Math.Round(male   * 100.0 / total, 0) + "%" : "0%";

            const double maxBarW = 160.0;

            var barangayCounts = _records
                .GroupBy(r => r.Barangay ?? "Unknown")
                .OrderByDescending(g => g.Count())
                .Take(8)
                .ToList();

            int maxB = barangayCounts.Any() ? barangayCounts.Max(g => g.Count()) : 1;
            BarangayChart.ItemsSource = barangayCounts.Select(g => new ChartRow
            {
                Label    = g.Key,
                Count    = g.Count(),
                BarWidth = Math.Max(6, g.Count() * maxBarW / maxB),
                BarColor = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43))
            }).ToList();

            var civilCounts = _records
                .GroupBy(r => r.CivilStatus ?? "Unknown")
                .OrderByDescending(g => g.Count())
                .ToList();

            int maxC = civilCounts.Any() ? civilCounts.Max(g => g.Count()) : 1;
            var civilColors = new[] { "#702943", "#9B3060", "#C4788E", "#D4A0B0", "#E8D0D7" };
            int ci = 0;
            CivilStatusChart.ItemsSource = civilCounts.Select(g =>
            {
                var hex = civilColors[ci % civilColors.Length];
                ci++;
                var c = (Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
                return new ChartRow
                {
                    Label    = g.Key,
                    Count    = g.Count(),
                    BarWidth = Math.Max(6, g.Count() * maxBarW / maxC),
                    BarColor = new SolidColorBrush(c)
                };
            }).ToList();

            RecentList.ItemsSource = _records
                .OrderByDescending(r => r.LastUpdated)
                .Take(5)
                .Select(r => new RecentRow
                {
                    Name        = r.Name,
                    Barangay    = r.Barangay,
                    Status      = r.Status,
                    StatusColor = r.Status == "Valid"
                        ? new SolidColorBrush(Color.FromRgb(0xE8, 0xF5, 0xE9))
                        : new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xF3)),
                    StatusText  = r.Status == "Valid"
                        ? new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60))
                        : new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35))
                })
                .ToList();
        }

        private class ChartRow
        {
            public string Label    { get; set; }
            public int    Count    { get; set; }
            public double BarWidth { get; set; }
            public SolidColorBrush BarColor { get; set; }
        }

        private class RecentRow
        {
            public string Name        { get; set; }
            public string Barangay    { get; set; }
            public string Status      { get; set; }
            public SolidColorBrush StatusColor { get; set; }
            public SolidColorBrush StatusText  { get; set; }
        }
    }
}
