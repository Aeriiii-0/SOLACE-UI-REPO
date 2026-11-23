using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SOLUM_UI.DTOs.Archive;
using SOLUM_UI.Services.Api;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class ArchivedDossierDialog : Window
    {
        private readonly Guid _id;
        public bool WasReactivated { get; private set; }
        private ArchivedSoloParentDetailDto _dossier;

        public ArchivedDossierDialog(Guid id)
        {
            _id = id;
            InitializeComponent();
            Loaded += async (s, e) => await LoadDossierAsync();
        }

        private async Task LoadDossierAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                var response = await ArchiveApiService.Instance.GetArchivedDossierAsync(_id);

                if (response != null && response.Succeeded && response.Data != null)
                {
                    _dossier = response.Data;
                    PopulateUI();
                }
                else
                {
                    string err = response?.Errors != null && response.Errors.Count > 0
                        ? string.Join("\n", response.Errors)
                        : "Failed to load archived case dossier.";
                    ActionConfirmDialog.Error("Load Error", err, this);
                }
            }
            catch (Exception ex)
            {
                ActionConfirmDialog.Error("Load Error", ex.Message, this);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void PopulateUI()
        {
            if (_dossier == null) return;

            var p = _dossier.PersonalInfo;
            var c = _dossier.ContactDetails;
            var a = _dossier.AddressDetails;
            var emp = _dossier.Employment;

            TxtCitizenName.Text = $"{p.LastName}, {p.FirstName} {p.MiddleName}".Trim();
            TxtPhilsysId.Text = string.IsNullOrWhiteSpace(p.PhilsysCardNumber) ? "Not Provided" : p.PhilsysCardNumber;
            TxtDob.Text = $"{p.Birthdate:yyyy-MM-dd} ({p.Age} yrs)";
            TxtSex.Text = string.IsNullOrWhiteSpace(p.Sex) ? "Not Specified" : p.Sex;
            TxtCivilStatus.Text = string.IsNullOrWhiteSpace(p.CivilStatus) ? "Not Specified" : p.CivilStatus;

            TxtContact.Text = string.IsNullOrWhiteSpace(c.ApplicantContactNumber) ? "None" : c.ApplicantContactNumber;
            TxtBarangay.Text = string.IsNullOrWhiteSpace(a.Barangay) ? "Not Specified" : a.Barangay;
            TxtAddress.Text = string.IsNullOrWhiteSpace(a.Address) ? a.Barangay : a.Address;

            TxtEmploymentStatus.Text = string.IsNullOrWhiteSpace(emp.EmploymentStatus) ? "Not Provided" : emp.EmploymentStatus;
            TxtOccupation.Text = string.IsNullOrWhiteSpace(emp.Occupation) ? "None / Unemployed" : emp.Occupation;
            TxtMonthlyIncome.Text = emp.MonthlyIncome > 0 ? $"PHP {emp.MonthlyIncome:N2}" : "PHP 0.00";
            TxtSubsidyEligibility.Text = string.IsNullOrWhiteSpace(_dossier.SubsidyEligibility) ? "Pending Assessment" : _dossier.SubsidyEligibility;

            TxtDateArchived.Text = _dossier.DateArchived.ToString("yyyy-MM-dd HH:mm");
            TxtArchiveReason.Text = string.Equals(_dossier.ArchiveReason, "Inactive5Years", StringComparison.OrdinalIgnoreCase)
                ? "Inactive 5+ Years"
                : (string.Equals(_dossier.ArchiveReason, "DeletionExpired", StringComparison.OrdinalIgnoreCase)
                    ? "Deletion Expired"
                    : _dossier.ArchiveReason);
            TxtShredDate.Text = $"{_dossier.ScheduledShredDate:yyyy-MM-dd} (10-Yr Statutory)";

            ListFamilyMembers.ItemsSource = _dossier.FamilyMembers;
            ListRecordTerms.ItemsSource = _dossier.RecordTerms;

            if (_dossier.IsReactivated || _dossier.IsShredded)
            {
                if (BtnReactivateRecord != null) BtnReactivateRecord.Visibility = Visibility.Collapsed;
            }
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (PanelProfile == null || PanelFamily == null || PanelTerms == null) return;

            PanelProfile.Visibility = TabProfile.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            PanelFamily.Visibility = TabFamily.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            PanelTerms.Visibility = TabTerms.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            if (PanelFamily.Visibility == Visibility.Visible)
            {
                ClipTableGrid(FamilyTableGrid, 11);
            }
            else if (PanelTerms.Visibility == Visibility.Visible)
            {
                ClipTableGrid(TermsTableGrid, 11);
            }
        }

        private void TableClipGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Grid grid)
            {
                ClipTableGrid(grid, 11);
            }
        }

        private static void ClipTableGrid(Grid grid, double radius)
        {
            if (grid != null && grid.ActualWidth > 0 && grid.ActualHeight > 0)
            {
                grid.Clip = new RectangleGeometry(new Rect(0, 0, grid.ActualWidth, grid.ActualHeight), radius, radius);
            }
        }

        private void BtnReactivate_Click(object sender, RoutedEventArgs e)
        {
            var reactivateDialog = new ReactivateCitizenDialog(_id, _dossier?.FullName ?? string.Empty) { Owner = this };
            if (reactivateDialog.ShowDialog() == true)
            {
                WasReactivated = true;
                DialogResult = true;
                Close();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = WasReactivated;
            Close();
        }
    }
}
