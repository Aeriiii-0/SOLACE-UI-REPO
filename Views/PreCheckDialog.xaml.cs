using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private bool _isPhilsysBlocked;

        private static readonly SolidColorBrush ErrorBrush   = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
        private static readonly SolidColorBrush DefaultBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xA0, 0xAA));

        private static readonly Regex NameRegex = new Regex(@"^[a-zA-ZñÑ\s\.\-']+$", RegexOptions.Compiled);
        private static readonly Regex NumberOnlyRegex = new Regex(@"^\d+$", RegexOptions.Compiled);
        private static readonly Regex PhilSysRegex = new Regex(@"^[\d\-]+$", RegexOptions.Compiled);

        public PreCheckDialog()
        {
            InitializeComponent();
            Loaded += (s, e) => SuppressDatePickerWatermarks();
        }
        /// <summary>
        /// Watermark suppression is now handled via XAML ControlTemplate in FormDate style.
        /// This method is kept for compatibility but no longer needed.
        /// </summary>
        private void SuppressDatePickerWatermarks()
        {
            // XAML template now properly suppresses native watermark by wrapping ScrollViewer in Grid
            // This prevents rendering of PART_Watermark element, so no code-behind suppression needed
        }
        /// <summary>
        /// Helper method to find all visual children of a given type.
        /// Note: Now kept for potential future use, but not used by SuppressDatePickerWatermarks.
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                    if (child is T typed)
                    {
                        yield return typed;
                    }
                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        // ── UI event handlers ─────────────────────────────────────────────────────

        private void NameOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !NameRegex.IsMatch(e.Text);
        }

        private void NumberOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !NumberOnlyRegex.IsMatch(e.Text);
        }

        private void PhilSys_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !PhilSysRegex.IsMatch(e.Text);
        }

        private void Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            ToUpperBox(tb);
            _foundRecord = null;
            HideBanner();
            ClearNameError(tb);
        }

        private void Extension_Changed(object sender, SelectionChangedEventArgs e)
        {
            _foundRecord = null;
            HideBanner();
        }

        private bool _isFormattingPhilSys = false;

        private void FormatPhilSysBox(TextBox tb)
        {
            if (tb == null || _isFormattingPhilSys) return;
            _isFormattingPhilSys = true;
            try
            {
                string raw = tb.Text ?? "";
                string digits = Regex.Replace(raw, @"\D", "");
                if (digits.Length > 16) digits = digits.Substring(0, 16);

                string formatted;
                if (digits.Length > 12)
                    formatted = $"{digits.Substring(0, 4)}-{digits.Substring(4, 4)}-{digits.Substring(8, 4)}-{digits.Substring(12)}";
                else if (digits.Length > 8)
                    formatted = $"{digits.Substring(0, 4)}-{digits.Substring(4, 4)}-{digits.Substring(8)}";
                else if (digits.Length > 4)
                    formatted = $"{digits.Substring(0, 4)}-{digits.Substring(4)}";
                else
                    formatted = digits;
                if (raw != formatted)
                {
                    int caret = tb.SelectionStart;
                    int diff = formatted.Length - raw.Length;
                    tb.Text = formatted;
                    tb.SelectionStart = Math.Max(0, Math.Min(formatted.Length, caret + diff));
                }
            }
            finally
            {
                _isFormattingPhilSys = false;
            }
        }

        private void PhilSys_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Back && sender is TextBox tb)
            {
                int sel = tb.SelectionStart;
                if (sel > 0 && tb.SelectionLength == 0 && sel <= tb.Text.Length && tb.Text[sel - 1] == '-')
                {
                    int rem = sel - 2;
                    if (rem >= 0)
                    {
                        tb.Text = tb.Text.Remove(rem, 2);
                        tb.SelectionStart = rem;
                        e.Handled = true;
                    }
                }
            }
        }

        private void PhilSys_TextChanged(object sender, TextChangedEventArgs e)
        {
            FormatPhilSysBox(TxtPhilSys);
            _foundRecord = null;
            HideBanner();
            if (ErrPhilSys != null && ErrPhilSys.Visibility != Visibility.Collapsed)
            {
                ErrPhilSys.Visibility = Visibility.Collapsed;
                if (TxtPhilSys != null) { TxtPhilSys.BorderBrush = DefaultBrush; TxtPhilSys.BorderThickness = new Thickness(1.2); }
            }
        }

        private void ToUpperBox(TextBox tb)
        {
            if (tb == null || tb.CharacterCasing == CharacterCasing.Upper) return;
            if (tb.Text != tb.Text.ToUpper())
            {
                int caret = tb.SelectionStart;
                tb.Text = tb.Text.ToUpper();
                tb.SelectionStart = Math.Min(caret + 1, tb.Text.Length);
            }
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
            if (_isPhilsysBlocked) return;
            if (!Validate()) return;
            // If a duplicate was already found and the user clicked "Add as New Anyway", proceed
            if (_foundRecord != null && BannerPanel.Visibility == Visibility.Visible)
            {
                ProceedWithForm();
                return;
            }
            await RunDuplicateCheck();
        }

        private void ViewRecord_Click(object sender, RoutedEventArgs e)
        {
            if (_foundRecord == null) return;
            ExistingRecordToView = _foundRecord;
            ProceedToFullForm = false;
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, true);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ProceedToFullForm = false;
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, false);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            ProceedToFullForm = false;
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, false);
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
            // PhilSys is now optional, but if provided, must contain valid digits
            if (!string.IsNullOrWhiteSpace(TxtPhilSys.Text))
            {
                string digits = Regex.Replace(TxtPhilSys.Text.Trim(), @"\D", "");
                if (digits.Length == 0)
                {
                    MarkError(TxtPhilSys, ErrPhilSys, "PhilSys number must contain digits.");
                    ok = false;
                }
            }
            // Enforce barangay validation - user must have a valid barangay assigned
            string userBarangay = MainWindow.CurrentUserBarangay;
            if (string.IsNullOrWhiteSpace(userBarangay))
            {
                ShowErrorBanner("Cannot proceed: Your user account does not have a barangay assigned. Contact system administrator.");
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
                string firstName  = TxtFirstName.Text.Trim();
                string middleName = TxtMiddleName.Text.Trim();
                string lastName   = TxtLastName.Text.Trim();
                string suffix     = (CmbExtension.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
                DateTime? birthdate = DpBirthdate.SelectedDate;
                string philsys    = TxtPhilSys.Text.Trim();

                SoloParentRecord foundDuplicate = null;
                bool isPhilsysDuplicate = false;

                // 1. Check PhilSys ID if provided (strictly unique)
                if (!string.IsNullOrWhiteSpace(philsys))
                {
                    string rawPhilsys = Regex.Replace(philsys, @"\D", "");
                    var philsysReq = new GetSoloParentRequest
                    {
                        PhilsysId = philsys,
                        Page      = 1,
                        PageSize  = 5
                    };
                    var pResp = await SoloParentApiService.Instance.GetSoloParentsAsync(philsysReq);
                    if (pResp.Succeeded && pResp.Data?.Items != null && pResp.Data.Items.Count > 0)
                    {
                        foreach (var item in pResp.Data.Items)
                        {
                            var detailResp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(item.Id);
                            if (detailResp.Succeeded && detailResp.Data != null)
                            {
                                var rec = SoloParentApiService.Instance.MapToRecord(detailResp.Data);
                                string recPhilsys = Regex.Replace(rec.PhilSysNumber ?? "", @"\D", "");
                                if (rawPhilsys == recPhilsys || (rawPhilsys.Length >= 12 && recPhilsys.Contains(rawPhilsys)))
                                {
                                    foundDuplicate = rec;
                                    isPhilsysDuplicate = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                // 2. Check Full Name ("firstname middlename lastname suffix") AND Birthday
                if (foundDuplicate == null && !string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                {
                    string inputCanonical = FormatCanonicalFullName(firstName, middleName, lastName, suffix);
                    string inputNormalized = NormalizeFullName(inputCanonical);

                    var nameReq = new GetSoloParentRequest
                    {
                        FirstName = firstName,
                        LastName  = lastName,
                        Birthdate = birthdate,
                        Page      = 1,
                        PageSize  = 10
                    };
                    var nameResp = await SoloParentApiService.Instance.GetSoloParentsAsync(nameReq);
                    if (nameResp.Succeeded && nameResp.Data?.Items != null && nameResp.Data.Items.Count > 0)
                    {
                        foreach (var item in nameResp.Data.Items)
                        {
                            var detailResp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(item.Id);
                            if (detailResp.Succeeded && detailResp.Data != null)
                            {
                                var rec = SoloParentApiService.Instance.MapToRecord(detailResp.Data);
                                string recCanonical = FormatCanonicalFullName(rec.FirstName, rec.MiddleName, rec.Surname, rec.ExtensionName);
                                string recNormalized = NormalizeFullName(recCanonical);

                                bool nameMatches = string.Equals(recNormalized, inputNormalized, StringComparison.OrdinalIgnoreCase);
                                bool dobMatches = birthdate.HasValue && rec.DateOfBirth.Date == birthdate.Value.Date;

                                if (nameMatches && dobMatches)
                                {
                                    foundDuplicate = rec;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (foundDuplicate != null)
                {
                    _foundRecord = foundDuplicate;
                    if (isPhilsysDuplicate)
                    {
                        ShowPhilsysDuplicateBanner(_foundRecord, philsys);
                    }
                    else
                    {
                        ShowDuplicateBanner(_foundRecord);
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
                if (!_isPhilsysBlocked && !BtnContinue.IsEnabled)
                {
                    BtnContinue.IsEnabled = true;
                    BtnContinueText.Text  = "Check & Continue";
                }
            }
        }

        private static string FormatCanonicalFullName(string first, string middle, string last, string suffix)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(first)) parts.Add(first.Trim());
            if (!string.IsNullOrWhiteSpace(middle)) parts.Add(middle.Trim());
            if (!string.IsNullOrWhiteSpace(last)) parts.Add(last.Trim());
            if (!string.IsNullOrWhiteSpace(suffix) && !string.Equals(suffix.Trim(), "NONE", StringComparison.OrdinalIgnoreCase))
                parts.Add(suffix.Trim());
            return string.Join(" ", parts);
        }

        private static string NormalizeFullName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            var trimmed = name.Trim().Replace(".", "");
            return System.Text.RegularExpressions.Regex.Replace(trimmed, @"\s+", " ").ToLowerInvariant();
        }

        private void ProceedWithForm()
        {
            ProceedToFullForm = true;
            // Use safe barangay - get from current user with "ALL" fallback if empty
            string userBarangay = MainWindow.CurrentUserBarangay;
            string safeBarangay = !string.IsNullOrWhiteSpace(userBarangay) ? userBarangay : "ALL";
            PrimaryData = new PreCheckResult
            {
                LastName   = TxtLastName.Text.Trim(),
                FirstName  = TxtFirstName.Text.Trim(),
                MiddleName = TxtMiddleName.Text.Trim(),
                Birthdate  = DpBirthdate.SelectedDate,
                PhilSys    = TxtPhilSys.Text.Trim(),
                Barangay   = safeBarangay,
                ExtensionName = (CmbExtension.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty
            };
            SOLUM_UI.Core.WindowPositionHelper.SafeSetDialogResult(this, true);
        }
        // ── Banner helpers ────────────────────────────────────────────────────────

        private void ShowDuplicateBanner(SoloParentRecord record)
        {
            _isPhilsysBlocked        = false;
            BannerPanel.Background   = new SolidColorBrush(Color.FromRgb(0xB7, 0x1C, 0x1C));
            BannerIcon.Text          = "!";
            BannerTitle.Text         = "Existing Record Found";
            string fullName          = FormatCanonicalFullName(record.FirstName, record.MiddleName, record.Surname, record.ExtensionName);
            string dobStr            = record.DateOfBirth != DateTime.MinValue ? record.DateOfBirth.ToString("MMMM d, yyyy") : "N/A";
            BannerText.Text          = $"A record matching \"{fullName}\" "
                                     + $"(DOB: {dobStr}) already exists in the system. "
                                     + $"You may view the existing record or add as a new entry anyway.";
            BtnViewRecord.Visibility = Visibility.Visible;
            BannerPanel.Visibility   = Visibility.Visible;
            BtnContinueText.Text     = "Add as New Anyway";
            BtnContinue.IsEnabled    = true;
        }

        private void ShowPhilsysDuplicateBanner(SoloParentRecord record, string philsys)
        {
            _isPhilsysBlocked        = true;
            BannerPanel.Background   = new SolidColorBrush(Color.FromRgb(0xB7, 0x1C, 0x1C));
            BannerIcon.Text          = "!";
            BannerTitle.Text         = "Duplicate PhilSys ID — Registration Blocked";
            string fullName          = FormatCanonicalFullName(record.FirstName, record.MiddleName, record.Surname, record.ExtensionName);
            BannerText.Text          = $"The PhilSys Card Number '{philsys}' is already assigned to '{fullName}'. PhilSys ID must be strictly unique. You cannot proceed with a duplicate PhilSys ID.";
            BtnViewRecord.Visibility = Visibility.Visible;
            BannerPanel.Visibility   = Visibility.Visible;
            BtnContinueText.Text     = "Check & Continue";
            BtnContinue.IsEnabled    = false;
        }

        private void ShowErrorBanner(string message)
        {
            BannerPanel.Background   = new SolidColorBrush(Color.FromRgb(0xE6, 0x5C, 0x00));
            BannerIcon.Text          = "!";
            BannerTitle.Text         = "Connection Error";
            BannerText.Text          = message;
            BtnViewRecord.Visibility = Visibility.Collapsed;
            BannerPanel.Visibility   = Visibility.Visible;
        }

        private void HideBanner()
        {
            _isPhilsysBlocked = false;
            if (BannerPanel != null && BannerPanel.Visibility != Visibility.Collapsed)
            {
                BannerPanel.Visibility   = Visibility.Collapsed;
                BtnViewRecord.Visibility = Visibility.Collapsed;
                BtnContinueText.Text     = "Check & Continue";
                BtnContinue.IsEnabled    = true;
            }
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
            TextBlock err = tb == TxtLastName  ? ErrLastName
                          : tb == TxtFirstName ? ErrFirstName
                          : null;
            if (err != null && err.Visibility != Visibility.Collapsed)
            {
                err.Visibility = Visibility.Collapsed;
                tb.BorderBrush     = DefaultBrush;
                tb.BorderThickness = new Thickness(1.2);
            }
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
        public string    Barangay   { get; set; }
        public string    ExtensionName { get; set; }
    }
}