using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class DeleteRecordDialog : Window
    {
        public bool Confirmed { get; private set; }
        public string DeletionReason { get; private set; } = string.Empty;

        public DeleteRecordDialog(string citizenName, string details = null)
        {
            InitializeComponent();
            TxtCitizenName.Text = string.IsNullOrWhiteSpace(citizenName) ? "Unknown Citizen" : citizenName;

            string cleanDetails = details;
            if (!string.IsNullOrWhiteSpace(cleanDetails))
            {
                // Never show raw UUID or "Record ID: <guid>"
                if (Guid.TryParse(cleanDetails.Trim(), out _) ||
                    cleanDetails.IndexOf("Record ID:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    Regex.IsMatch(cleanDetails, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}"))
                {
                    cleanDetails = "Solo Parent Case File";
                }
            }
            TxtCitizenSub.Text = string.IsNullOrWhiteSpace(cleanDetails) ? "Solo Parent Case File" : cleanDetails;
        }

        private void CmbReason_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PnlError != null)
            {
                PnlError.Visibility = Visibility.Collapsed;
            }
            if (TxtError != null)
            {
                TxtError.Text = string.Empty;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            DialogResult = false;
            Close();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = CmbReason.SelectedItem as ComboBoxItem;
            string selectedText = selectedItem?.Content?.ToString();

            if (selectedItem == null || CmbReason.SelectedIndex <= 0 ||
                string.IsNullOrWhiteSpace(selectedText) ||
                selectedText.StartsWith("Select a reason", StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Please select a reason for deletion.");
                CmbReason.Focus();
                return;
            }

            string remarks = TxtRemarks.Text?.Trim() ?? string.Empty;

            if (selectedText.IndexOf("Other Reason", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (string.IsNullOrWhiteSpace(remarks))
                {
                    ShowError("Please provide specific details in remarks for 'Other Reason'.");
                    TxtRemarks.Focus();
                    return;
                }
                DeletionReason = remarks;
            }
            else
            {
                DeletionReason = string.IsNullOrWhiteSpace(remarks)
                    ? selectedText
                    : $"{selectedText}: {remarks}";
            }

            Confirmed = true;
            DialogResult = true;
            Close();
        }

        private void ShowError(string message)
        {
            if (TxtError != null)
            {
                TxtError.Text = message;
            }
            if (PnlError != null)
            {
                PnlError.Visibility = Visibility.Visible;
            }
        }
    }
}
