using System;
using System.Windows;
using System.Windows.Controls;
using SOLUM_UI.DTOs.Archive;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services.Api;
using SOLUM_UI.Views;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class ReactivateCitizenDialog : Window
    {
        private readonly Guid _id;
        private readonly string _citizenName;

        public ReactivateCitizenDialog(Guid id, string citizenName = "")
        {
            _id = id;
            _citizenName = citizenName;
            InitializeComponent();
        }

        private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            string status = (CmbEmploymentStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Employed";
            string occupation = TxtOccupation.Text?.Trim().ToUpperInvariant();
            string notes = TxtReactivationNotes.Text?.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(occupation))
            {
                ActionConfirmDialog.Error("Validation Error", "Please provide the citizen's current occupation or source of livelihood.", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(notes))
            {
                ActionConfirmDialog.Error("Validation Error", "Please provide a justification note for reactivating this citizen.", this);
                return;
            }

            decimal.TryParse(TxtMonthlyIncome.Text?.Trim(), out decimal income);

            var req = new ReactivateArchivedRecordRequest
            {
                UpdatedEmployment = new EmploymentInfo
                {
                    EmploymentStatus = status,
                    Occupation = occupation,
                    MonthlyIncome = income
                },
                NeedsAndProblems = TxtNeedsAndProblems.Text?.Trim().ToUpperInvariant(),
                OtherSourcesOfIncome = TxtOtherIncome.Text?.Trim().ToUpperInvariant(),
                Notes = notes
            };

            try
            {
                IsEnabled = false;
                var response = await ArchiveApiService.Instance.ReactivateCitizenAsync(_id, req);

                if (response != null && response.Succeeded)
                {
                    ActionConfirmDialog.Show(
                        title: "Record Reactivated",
                        message: "The record has been successfully restored to the active solo parent directory with a new 1-year Card Term.",
                        confirmText: "OK",
                        theme: ConfirmThemeType.Success,
                        owner: this);

                    Services.AuditLogService.Instance.LogReactivate(
                        _id.ToString(),
                        !string.IsNullOrWhiteSpace(_citizenName) ? _citizenName : "Archived Record",
                        MainWindow.CurrentUserName,
                        MainWindow.CurrentUserRole,
                        notes);

                    SoloParentRecordsPage.ForceRefreshOnLoad = true;
                    (Application.Current.MainWindow as MainWindow)?.ClearPageCache("SoloParentRecords");
                    (Application.Current.MainWindow as MainWindow)?.ClearPageCache("Dashboard");
                    (Application.Current.MainWindow as MainWindow)?.ClearPageCache("Analytics");

                    DialogResult = true;
                    Close();
                }
                else
                {
                    string err = response?.Errors != null && response.Errors.Count > 0
                        ? string.Join("\n", response.Errors)
                        : "Failed to reactivate archived record.";
                    ActionConfirmDialog.Error("Reactivation Failed", err, this);
                }
            }
            catch (Exception ex)
            {
                ActionConfirmDialog.Error("Reactivation Error", ex.Message, this);
            }
            finally
            {
                IsEnabled = true;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
