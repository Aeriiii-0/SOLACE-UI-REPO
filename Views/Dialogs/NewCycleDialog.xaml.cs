using System;
using System.Windows;
using System.Windows.Controls;
using SOLUM_UI.DTOs;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class NewCycleDialog : Window
    {
        public CreateSubsidyCycleRequest ResultRequest { get; private set; }

        public NewCycleDialog(int suggestedYear = 2027, int defaultGrantees = 100, decimal defaultPerGrantee = 1000m)
        {
            InitializeComponent();
            TxtFiscalYear.Text = suggestedYear.ToString();
            TxtGrantees.Text = defaultGrantees.ToString();
            TxtPerGrantee.Text = defaultPerGrantee.ToString("N2");
            UpdateBudgetPreview();
        }

        private void Inputs_Changed(object sender, TextChangedEventArgs e)
        {
            UpdateBudgetPreview();
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
            DialogResult = false;
            Close();
        }

        private void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtFiscalYear.Text?.Trim(), out var fy) || fy < 2000 || fy > 2100)
            {
                MessageBox.Show("Please enter a valid Fiscal Year (e.g. 2027).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            decimal totalBudget = grantees * perGrantee;
            ResultRequest = new CreateSubsidyCycleRequest
            {
                FiscalYear = fy,
                TotalBudget = totalBudget,
                PerGranteeAmount = perGrantee,
                AllocatedSlots = grantees
            };

            DialogResult = true;
            Close();
        }
    }
}
