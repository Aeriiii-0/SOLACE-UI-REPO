using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SOLUM_UI.Models;

namespace SOLUM_UI.Views.Components
{
    public partial class ModalCycleDetails : UserControl
    {
        public ModalCycleDetails(
            FiscalCycleOption cycle,
            int confirmedCount,
            int remainingSlots,
            int disqualifiedCount)
        {
            InitializeComponent();
            Populate(cycle, confirmedCount, remainingSlots, disqualifiedCount);
        }

        private void Populate(FiscalCycleOption cycle, int confirmedCount, int remainingSlots, int disqualifiedCount)
        {
            if (cycle == null) return;

            TxtTitle.Text = !string.IsNullOrWhiteSpace(cycle.Name)
                ? cycle.Name
                : $"FY {cycle.FiscalYear} ({cycle.SemesterLabel})";

            string status = string.IsNullOrWhiteSpace(cycle.Status) ? "Archived" : cycle.Status.Trim();
            TxtStatusBadge.Text = status.ToUpperInvariant();

            if (cycle.IsActive)
            {
                StatusBadge.Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xFC, 0xE7));
                TxtStatusBadge.Foreground = new SolidColorBrush(Color.FromRgb(0x16, 0x65, 0x34));
                TxtSubtitle.Text = "Active operational cycle open for candidate triage and approvals";
                TxtFooterNote.Text = "Operational cycle parameters can be updated via Edit Cycle Settings.";
            }
            else if (cycle.IsFinalized)
            {
                StatusBadge.Background = new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xC7));
                TxtStatusBadge.Foreground = new SolidColorBrush(Color.FromRgb(0xB4, 0x53, 0x09));
                TxtSubtitle.Text = "Finalized & locked cycle ready for official disbursement";
                TxtFooterNote.Text = "This roster is finalized. Unlock roster to make amendments.";
            }
            else
            {
                StatusBadge.Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF4, 0xF6));
                TxtStatusBadge.Foreground = new SolidColorBrush(Color.FromRgb(0x4B, 0x55, 0x63));
                TxtSubtitle.Text = "Closed historical cycle certified for reporting and audit";
                TxtFooterNote.Text = "Completed historical cycles are certified and read-only.";
            }

            TxtFiscalYear.Text = $"FY {cycle.FiscalYear}";
            TxtSemester.Text = cycle.CycleNumber == 1 ? "1st Semester"
                : cycle.CycleNumber == 2 ? "2nd Semester"
                : $"Cycle {cycle.CycleNumber}";

            if (cycle.StartDate.HasValue && cycle.EndDate.HasValue)
                TxtDateRange.Text = $"{cycle.StartDate.Value:MMM dd, yyyy} – {cycle.EndDate.Value:MMM dd, yyyy}";
            else if (cycle.StartDate.HasValue)
                TxtDateRange.Text = $"From {cycle.StartDate.Value:MMM dd, yyyy}";
            else
                TxtDateRange.Text = $"FY {cycle.FiscalYear} ({cycle.SemesterLabel})";

            int slots = cycle.AllocatedSlots > 0 ? cycle.AllocatedSlots : 0;
            decimal perGrantee = cycle.PerGranteeAmount > 0 ? cycle.PerGranteeAmount : 0m;
            decimal totalBudget = cycle.TotalBudget > 0 ? cycle.TotalBudget : slots * perGrantee;

            TxtAllocatedSlots.Text = $"{slots:N0} slots";
            TxtPerGranteeAmount.Text = $"₱{perGrantee:N2}";
            TxtTotalBudget.Text = $"₱{totalBudget:N2}";

            TxtConfirmedCount.Text = $"{confirmedCount:N0}";
            TxtRemainingSlots.Text = $"{remainingSlots:N0}";
            TxtDisqualifiedCount.Text = $"{disqualifiedCount:N0}";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.HideGlobalModal();
        }
    }
}
