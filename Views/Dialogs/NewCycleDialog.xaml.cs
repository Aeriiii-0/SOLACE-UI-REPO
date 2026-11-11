using System;
using System.Windows;
using System.Windows.Controls;
using SOLUM_UI.DTOs;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class NewCycleDialog : Window
    {
        public CreateSubsidyCycleRequest ResultRequest { get; private set; }

        public NewCycleDialog(int suggestedYear = 2026, int suggestedCycleNumber = 1, int defaultGrantees = 100, decimal defaultPerGrantee = 1000m)
        {
            InitializeComponent();
            TxtFiscalYear.Text = suggestedYear.ToString();
            TxtGrantees.Text = defaultGrantees.ToString();
            TxtPerGrantee.Text = defaultPerGrantee.ToString("N2");
            if (CmbSemester != null)
            {
                CmbSemester.SelectedIndex = suggestedCycleNumber == 2 ? 1 : 0;
            }
            UpdateDefaultName();
            UpdateBudgetPreview();
        }

        private void CmbSemester_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDefaultName();
        }

        private void UpdateDefaultName()
        {
            if (TxtCycleName == null || TxtFiscalYear == null) return;
            if (int.TryParse(TxtFiscalYear.Text?.Trim(), out var fy))
            {
                int sem = CmbSemester?.SelectedIndex == 1 ? 2 : 1;
                string autoName = $"FY {fy} - {(sem == 1 ? "1st Semester" : "2nd Semester")}";
                if (string.IsNullOrWhiteSpace(TxtCycleName.Text) || TxtCycleName.Text.StartsWith("FY "))
                {
                    TxtCycleName.Text = autoName;
                }
            }
        }

        private void Inputs_Changed(object sender, TextChangedEventArgs e)
        {
            UpdateBudgetPreview();
            if (sender == TxtFiscalYear)
            {
                UpdateDefaultName();
            }
        }

        private void UpdateBudgetPreview()
        {
            if (TxtBudgetPreview == null) return;
            if (int.TryParse(TxtGrantees.Text?.Replace(",", "").Trim(), out var grantees) &&
                decimal.TryParse(TxtPerGrantee.Text?.Replace(",", "").Trim(), out var perGrantee) &&
                grantees > 0 && perGrantee > 0)
            {
                decimal total = grantees * perGrantee;
                TxtBudgetPreview.Text = $"₱{total:N2}";
            }
            else
            {
                TxtBudgetPreview.Text = "—";
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, false);
        }

        private void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtFiscalYear.Text?.Trim(), out var fy) || fy < 2000 || fy > 2100)
            {
                MessageBox.Show("Please enter a valid Fiscal Year (e.g. 2026).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(TxtGrantees.Text?.Replace(",", "").Trim(), out var grantees) || grantees <= 0)
            {
                MessageBox.Show("Please enter a valid Target Beneficiary Quota greater than 0.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!decimal.TryParse(TxtPerGrantee.Text?.Replace(",", "").Trim(), out var perGrantee) || perGrantee <= 0)
            {
                MessageBox.Show("Please enter a valid Allowance Per Beneficiary greater than 0.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int cycleNumber = CmbSemester?.SelectedIndex == 1 ? 2 : 1;
            string cycleName = TxtCycleName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(cycleName))
            {
                cycleName = $"FY {fy} - {(cycleNumber == 1 ? "1st Semester" : "2nd Semester")}";
            }

            decimal totalBudget = grantees * perGrantee;
            ResultRequest = new CreateSubsidyCycleRequest
            {
                FiscalYear = fy,
                CycleNumber = cycleNumber,
                Name = cycleName,
                TotalBudget = totalBudget,
                PerGranteeAmount = perGrantee,
                AllocatedSlots = grantees
            };
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, true);
        }
    }
}
