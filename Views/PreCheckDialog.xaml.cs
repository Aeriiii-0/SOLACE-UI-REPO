using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services.Api;

namespace SOLUM_UI
{
    /// <summary>
    /// Step-1 identity gate for manual entry.
    /// Collects Last Name, First Name, Middle Name, Birthdate, and PhilSys Card Number,
    /// then checks the server for a duplicate before allowing the full RecordDialog to open.
    /// </summary>
    public partial class PreCheckDialog : Window
    {
        // ── Outputs ─────────────────────────────────────────────────────────────
        /// <summary>True when the user confirmed "Continue" and no duplicate blocked them.</summary>
        public bool ProceedToFullForm { get; private set; }

        /// <summary>When ProceedToFullForm is true, contains the primary-field values
        /// to pre-populate in RecordDialog.</summary>
        public PreCheckResult PrimaryData { get; private set; }

        /// <summary>When a duplicate was found AND the user clicked "View Existing Record",
        /// this holds that record so the caller can open RecordViewDialog.</summary>
        public SoloParentRecord ExistingRecordToView { get; private set; }

        // ── Private state ────────────────────────────────────────────────────────
        private SoloParentRecord _foundRecord;   // cached search result
        private bool _searching;

        private static readonly SolidColorBrush ErrorBrush   = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
        private static readonly SolidColorBrush DefaultBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xA0, 0xAA));

        public PreCheckDialog()
        {
            InitializeComponent();
        }

        // ── UI event handlers ─────────────────────────────────────────────────────

        private void NameOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-ZñÑ\s\.\-']+$");
        }

        private void NumberOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            _foundRecord = null;
            HideBanner();
            ClearNameError(sender as TextBox);
        }

        private void PhilSys_TextChanged(object sender, TextChangedEventArgs e)
        {
            _foundRecord = null;
            HideBanner();
            if (ErrPhilSys != null) ErrPhilSys.Visibility = Visibility.Collapsed;
            if (TxtPhilSys != null) { TxtPhilSys.BorderBrush = DefaultBrush; TxtPhilSys.BorderThickness = new Thickness(1.2); }
        }

        private void Birthdate_Changed(object sender, SelectionChangedEventArgs e)
        {
            _foundRecord = null;
            HideBanner();
            ClearBirthdateError();
        }

        private async void Continue_Click(object sender, RoutedEventArgs e)
        {
            if (_searching) return;

            if (!Validate()) return;

            await RunDuplicateCheck();
        }

        private void ViewRecord_Click(object sender, RoutedEventArgs e)
        {
            if (_foundRecord == null) return;
            ExistingRecordToView = _foundRecord;
            ProceedToFullForm = false;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ProceedToFullForm = false;
            DialogResult = false;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            ProceedToFullForm = false;
            DialogResult = false;
            Close();
        }

        // ── Core logic ────────────────────────────────────────────────────────────

        private bool Validate()
        {
            bool ok = true;

            if (string.IsNullOrWhiteSpace(TxtLastName.Text))
            {
                MarkError(TxtLastName, ErrLastName, "Last name is required.");
                ok = false;
            }
            if (string.IsNullOrWhiteSpace(TxtFirstName.Text))
            {
                MarkError(TxtFirstName, ErrFirstName, "First name is required.");
                ok = false;
            }
            if (!DpBirthdate.SelectedDate.HasValue)
            {
                ErrBirthdate.Text = "Birthdate is required.";
                ErrBirthdate.Visibility = Visibility.Visible;
                ok = false;
            }
            else if (DpBirthdate.SelectedDate.Value.Date > DateTime.Today)
            {
                ErrBirthdate.Text = "Birthdate cannot be in the future.";
                ErrBirthdate.Visibility = Visibility.Visible;
                ok = false;
            }
            else if (DpBirthdate.SelectedDate.Value.Date > DateTime.Today.AddYears(-1))
            {
                ErrBirthdate.Text = "Age seems too young for a solo parent.";
                ErrBirthdate.Visibility = Visibility.Visible;
                ok = false;
            }

            if (string.IsNullOrWhiteSpace(TxtPhilSys.Text))
            {
                MarkError(TxtPhilSys, ErrPhilSys, "PhilSys number is required.");
                ok = false;
            }
            else if (!Regex.IsMatch(TxtPhilSys.Text.Trim(), @"^\d+$"))
            {
                MarkError(TxtPhilSys, ErrPhilSys, "PhilSys number must contain digits only.");
                ok = false;
            }

            return ok;
        }

        private async Task RunDuplicateCheck()
        {
            _searching = true;
            BtnContinue.IsEnabled = false;
            BtnContinueText.Text  = "Checking…";
            HideBanner();

            try
            {
                string firstName = TxtFirstName.Text.Trim();
                string lastName  = TxtLastName.Text.Trim();
                string fullName  = lastName + " " + firstName;
                string philsys   = TxtPhilSys.Text.Trim();

                BaseResponse<PagedResult<SoloParentSummaryDto>> response = null;

                if (!string.IsNullOrWhiteSpace(philsys))
                {
                    var philsysReq = new GetSoloParentRequest
                    {
                        PhilsysId = philsys,
                        Page      = 1,
                        PageSize  = 5
                    };
                    response = await SoloParentApiService.Instance.GetSoloParentsAsync(philsysReq);
                }

                if (response == null || !response.Succeeded || response.Data?.Items == null || response.Data.Items.Count == 0)
                {
                    var request = new GetSoloParentRequest
                    {
                        Fullname = fullName,
                        Page     = 1,
                        PageSize = 5
                    };
                    response = await SoloParentApiService.Instance.GetSoloParentsAsync(request);
                }

                if (response.Succeeded && response.Data?.Items != null && response.Data.Items.Count > 0)
                {
                    // Fetch the full record for the first match
                    var summary = response.Data.Items[0];
                    var detailResp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(summary.Id);

                    if (detailResp.Succeeded && detailResp.Data != null)
                    {
                        _foundRecord = SoloParentApiService.Instance.MapToRecord(detailResp.Data);
                        ShowDuplicateBanner(_foundRecord);
                    }
                    else
                    {
                        // Summary exists but detail fetch failed — treat as not-found and proceed
                        ProceedWithForm();
                    }
                }
                else
                {
                    // No duplicate found — proceed
                    ProceedWithForm();
                }
            }
            catch (Exception ex)
            {
                ShowErrorBanner("Could not connect to server. You may continue anyway.\n" + ex.Message);
                // Still allow user to proceed on connectivity error
                BtnContinue.IsEnabled = true;
                BtnContinueText.Text  = "Continue Anyway";
            }
            finally
            {
                _searching = false;
                if (BtnContinue.IsEnabled == false)
                {
                    BtnContinue.IsEnabled = true;
                    BtnContinueText.Text  = "Check & Continue";
                }
            }
        }

        private void ProceedWithForm()
        {
            ProceedToFullForm = true;
            PrimaryData = new PreCheckResult
            {
                LastName   = TxtLastName.Text.Trim(),
                FirstName  = TxtFirstName.Text.Trim(),
                MiddleName = TxtMiddleName.Text.Trim(),
                Birthdate  = DpBirthdate.SelectedDate,
                PhilSys    = TxtPhilSys.Text.Trim()
            };
            DialogResult = true;
            Close();
        }

        // ── Banner helpers ────────────────────────────────────────────────────────

        private void ShowDuplicateBanner(SoloParentRecord record)
        {
            BannerPanel.Background   = new SolidColorBrush(Color.FromRgb(0xB7, 0x1C, 0x1C));
            BannerIcon.Text          = "⚠";
            BannerTitle.Text         = "Existing Record Found";
            BannerText.Text          = $"A record matching \"{record.FirstName} {record.Surname}\" "
                                     + $"(DOB: {record.DateOfBirth:MMMM d, yyyy}) already exists in the system. "
                                     + $"You may view the existing record or add as a new entry anyway.";
            BtnViewRecord.Visibility = Visibility.Visible;
            BannerPanel.Visibility   = Visibility.Visible;
            BtnContinueText.Text     = "Add as New Anyway";
            BtnContinue.IsEnabled    = true;
        }

        private void ShowErrorBanner(string message)
        {
            BannerPanel.Background   = new SolidColorBrush(Color.FromRgb(0xE6, 0x5C, 0x00));
            BannerIcon.Text          = "⚡";
            BannerTitle.Text         = "Connection Error";
            BannerText.Text          = message;
            BtnViewRecord.Visibility = Visibility.Collapsed;
            BannerPanel.Visibility   = Visibility.Visible;
        }

        private void HideBanner()
        {
            BannerPanel.Visibility   = Visibility.Collapsed;
            BtnViewRecord.Visibility = Visibility.Collapsed;
            BtnContinueText.Text     = "Check & Continue";
        }

        // ── Field error helpers ───────────────────────────────────────────────────

        private void MarkError(TextBox tb, TextBlock err, string msg)
        {
            if (tb  != null) { tb.BorderBrush = ErrorBrush; tb.BorderThickness = new Thickness(1.5); }
            if (err != null) { err.Text = msg; err.Visibility = Visibility.Visible; }
        }

        private void ClearNameError(TextBox tb)
        {
            if (tb == null) return;
            tb.BorderBrush     = DefaultBrush;
            tb.BorderThickness = new Thickness(1.2);
            TextBlock err = tb == TxtLastName  ? ErrLastName
                          : tb == TxtFirstName ? ErrFirstName
                          : null;
            if (err != null) err.Visibility = Visibility.Collapsed;
        }

        private void ClearBirthdateError()
        {
            if (ErrBirthdate != null) ErrBirthdate.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>Data bag passed back to the caller after a successful pre-check.</summary>
    public class PreCheckResult
    {
        public string    LastName   { get; set; }
        public string    FirstName  { get; set; }
        public string    MiddleName { get; set; }
        public DateTime? Birthdate  { get; set; }
        public string    PhilSys    { get; set; }
    }
}
