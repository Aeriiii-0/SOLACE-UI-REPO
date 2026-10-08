using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SOLUM_UI.Models;

namespace SOLUM_UI
{
    // Simple model for the review list rows
    public class RenewReviewRow
    {
        public string Label      { get; set; }
        public string OldValue   { get; set; }
        public string NewValue   { get; set; }
        public bool   IsChanged  { get; set; }

        // Drives the "After" text colour: maroon when changed, normal when same
        public SolidColorBrush ValueColor => IsChanged
            ? new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43))
            : new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x11));
    }

    public partial class RenewDialog : Window
    {
        // The caller reads this after DialogResult = true
        public SoloParentRecord Result { get; private set; }

        private readonly SoloParentRecord _original;
        private int _step = 1;

        private static readonly SolidColorBrush _errBrush     = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
        private static readonly SolidColorBrush _defaultBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xA0, 0xAA));
        private static readonly Thickness       _defaultThick  = new Thickness(1.2);

        public RenewDialog(SoloParentRecord original)
        {
            InitializeComponent();
            _original = original ?? throw new ArgumentNullException(nameof(original));
            Loaded += (s, e) => { ApplySize(); PopulateFields(); };
        }

        // ── sizing (same pattern as RecordDialog) ──────────────────────────────

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => ApplySize();

        private void ApplySize()
        {
            double sw, sh, sl, st;
            if (Owner != null && Owner.WindowState == WindowState.Maximized)
            {
                sw = SystemParameters.WorkArea.Width;
                sh = SystemParameters.WorkArea.Height;
                sl = SystemParameters.WorkArea.Left;
                st = SystemParameters.WorkArea.Top;
            }
            else if (Owner != null)
            {
                sw = Owner.ActualWidth;
                sh = Owner.ActualHeight;
                sl = Owner.Left;
                st = Owner.Top;
            }
            else
            {
                sw = SystemParameters.WorkArea.Width;
                sh = SystemParameters.WorkArea.Height;
                sl = SystemParameters.WorkArea.Left;
                st = SystemParameters.WorkArea.Top;
            }
            Width  = sw;
            Height = sh;
            Left   = sl;
            Top    = st;
            DialogShell.MaxHeight = sh * 0.90;
            DialogShell.Width     = Math.Min(sw * 0.62, 860);
        }

        // ── populate ───────────────────────────────────────────────────────────

        private void PopulateFields()
        {
            var r = _original;

            // Locked identity fields
            TxtLastName.Text  = r.Surname       ?? string.Empty;
            TxtFirstName.Text = r.FirstName     ?? string.Empty;
            TxtMiddleName.Text = r.MiddleName   ?? string.Empty;
            TxtExtension.Text = r.ExtensionName ?? string.Empty;
            TxtDOB.Text       = r.DateOfBirth != DateTime.MinValue
                                    ? r.DateOfBirth.ToString("MMMM d, yyyy") : string.Empty;
            TxtSex.Text       = r.Sex           ?? string.Empty;
            TxtPhilSys.Text   = r.PhilSysNumber ?? string.Empty;

            // Editable fields — pre-fill from existing record
            SetCombo(CmbCivilStatus, r.CivilStatus);
            TxtMonthlyIncome.Text = r.MonthlyIncome  ?? string.Empty;
            TxtOccupation.Text    = r.Occupation      ?? string.Empty;
            TxtAddress.Text       = r.Address         ?? string.Empty;
            TxtBarangay.Text      = r.Barangay        ?? string.Empty;
            TxtContact.Text       = SOLUM_UI.Services.OcrService.FormatPhoneNumber(r.ContactNumber ?? string.Empty);
            SetCombo(CmbStatus, string.IsNullOrWhiteSpace(r.Status) ? "Valid" : r.Status);

            if (r.IsEmployed)         ChkEmployed.IsChecked     = true;
            else if (r.IsSelfEmployed) ChkSelfEmployed.IsChecked = true;
            else if (r.IsNotEmployed)  ChkNotEmployed.IsChecked  = true;
            else                       ChkNotEmployed.IsChecked  = true; // default
        }

        private static void SetCombo(ComboBox cmb, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            foreach (ComboBoxItem item in cmb.Items)
            {
                if (string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    cmb.SelectedItem = item;
                    return;
                }
            }
        }

        private static string GetCombo(ComboBox cmb)
            => (cmb.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;

        // ── validation ─────────────────────────────────────────────────────────

        private bool Validate()
        {
            bool ok = true;
            var  missing = new List<string>();

            if (CmbCivilStatus.SelectedItem == null)
            {
                MarkErr(CmbCivilStatus, ErrCivilStatus, "Required.");
                missing.Add("Civil Status");
                ok = false;
            }

            if (string.IsNullOrWhiteSpace(TxtAddress.Text))
            {
                MarkErr(TxtAddress, ErrAddress, "Required.");
                missing.Add("Address");
                ok = false;
            }

            if (string.IsNullOrWhiteSpace(TxtBarangay.Text))
            {
                MarkErr(TxtBarangay, ErrBarangay, "Required.");
                missing.Add("Barangay");
                ok = false;
            }

            string digits = Regex.Replace(TxtContact.Text ?? "", @"\D", "");
            if (string.IsNullOrWhiteSpace(digits))
            {
                MarkErr(TxtContact, ErrContact, "Required.");
                missing.Add("Contact Number");
                ok = false;
            }
            else if (digits.Length != 11)
            {
                MarkErr(TxtContact, ErrContact, "Must be 11 digits.");
                missing.Add("Contact Number (11 digits)");
                ok = false;
            }

            if (!ok)
            {
                TxtValidationMsg.Text       = "Missing: " + string.Join(", ", missing) + ".";
                TxtValidationMsg.Visibility = Visibility.Visible;
            }
            else
            {
                TxtValidationMsg.Visibility = Visibility.Collapsed;
            }

            return ok;
        }

        private void MarkErr(Control ctrl, TextBlock lbl, string msg)
        {
            ctrl.BorderBrush     = _errBrush;
            ctrl.BorderThickness = new Thickness(1.5);
            lbl.Text             = msg;
            lbl.Visibility       = Visibility.Visible;
        }

        private void ClearErr(Control ctrl, TextBlock lbl)
        {
            ctrl.BorderBrush     = _defaultBrush;
            ctrl.BorderThickness = _defaultThick;
            lbl.Visibility       = Visibility.Collapsed;
        }

        // ── step navigation ────────────────────────────────────────────────────

        private void GoToStep(int step)
        {
            _step = step;
            bool onStep1 = step == 1;

            PanelStep1.Visibility  = onStep1 ? Visibility.Visible   : Visibility.Collapsed;
            PanelStep2.Visibility  = onStep1 ? Visibility.Collapsed  : Visibility.Visible;
            BtnNext.Visibility     = onStep1 ? Visibility.Visible   : Visibility.Collapsed;
            BtnConfirm.Visibility  = onStep1 ? Visibility.Collapsed  : Visibility.Visible;
            BtnBack.Visibility     = onStep1 ? Visibility.Collapsed  : Visibility.Visible;

            TxtSubtitle.Text = onStep1
                ? "Step 1 of 2 — Update Information"
                : "Step 2 of 2 — Review & Confirm";

            // step dots
            Step1Dot.Background = onStep1
                ? new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43))
                : new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));

            if (!onStep1)
            {
                Step2Dot.Background      = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));
                Step2DotLabel.Foreground = new SolidColorBrush(Colors.White);
                Step2Title.Foreground    = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));
                BuildReview();
            }
            else
            {
                Step2Dot.Background      = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
                Step2DotLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
                Step2Title.Foreground    = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
            }
        }

        // ── review page builder ────────────────────────────────────────────────

        private void BuildReview()
        {
            var r   = _original;
            string F(string v) => string.IsNullOrWhiteSpace(v) ? "—" : v;

            // Validity dates
            string renewedDate  = DateTime.Today.ToString("MMMM d, yyyy");
            string expiry       = DateTime.Today.AddYears(1).ToString("MMMM d, yyyy");
            TxtValidityDates.Text = $"Renewed: {renewedDate}  ·  Valid until: {expiry}";
            TxtNewExpiry.Text     = expiry;

            // Identity review (locked, always same)
            RevName.Text    = F(r.Name);
            RevDOB.Text     = r.DateOfBirth != DateTime.MinValue ? r.DateOfBirth.ToString("MMMM d, yyyy") : "—";
            RevSex.Text     = F(r.Sex);
            RevPhilSys.Text = F(r.PhilSysNumber);

            // Changed fields summary
            string newCivil     = GetCombo(CmbCivilStatus);
            string newIncome    = TxtMonthlyIncome.Text.Trim();
            string newEmpStatus = ChkEmployed.IsChecked     == true ? "Employed"
                                : ChkSelfEmployed.IsChecked == true ? "Self-Employed"
                                : "Not Employed";
            string newAddress   = TxtAddress.Text.Trim();
            string newBarangay  = TxtBarangay.Text.Trim();
            string newContact   = SOLUM_UI.Services.OcrService.FormatPhoneNumber(TxtContact.Text.Trim());
            string newOccupation = TxtOccupation.Text.Trim();
            string newStatus    = GetCombo(CmbStatus);

            string oldEmpStatus = r.IsEmployed ? "Employed"
                                : r.IsSelfEmployed ? "Self-Employed"
                                : r.IsNotEmployed ? "Not Employed" : "—";

            var rows = new List<RenewReviewRow>
            {
                MakeRow("Civil Status",      F(r.CivilStatus),   F(newCivil)),
                MakeRow("Monthly Income",    F(r.MonthlyIncome), F(string.IsNullOrWhiteSpace(newIncome) ? "—" : newIncome)),
                MakeRow("Employment",        oldEmpStatus,       newEmpStatus),
                MakeRow("Address",           F(r.Address),       F(newAddress)),
                MakeRow("Barangay",          F(r.Barangay),      F(newBarangay)),
                MakeRow("Contact Number",    F(SOLUM_UI.Services.OcrService.FormatPhoneNumber(r.ContactNumber)),
                                             F(newContact)),
                MakeRow("Occupation",        F(r.Occupation),    F(newOccupation)),
                MakeRow("Status",            F(r.Status),        F(newStatus)),
            };

            ReviewList.ItemsSource = rows;
        }

        private static RenewReviewRow MakeRow(string label, string oldVal, string newVal) =>
            new RenewReviewRow
            {
                Label     = label,
                OldValue  = oldVal,
                NewValue  = newVal,
                IsChanged = oldVal != newVal
            };

        // ── build result record ────────────────────────────────────────────────

        private SoloParentRecord BuildResult()
        {
            var r = _original;

            // Deep-copy original then overlay mutable fields
            return new SoloParentRecord
            {
                // --- identity (locked, copied verbatim) ---
                Id            = r.Id,
                CreatedBy     = r.CreatedBy,
                Surname       = r.Surname,
                FirstName     = r.FirstName,
                MiddleName    = r.MiddleName,
                ExtensionName = r.ExtensionName,
                Name          = r.Name,
                DateOfBirth   = r.DateOfBirth,
                PlaceOfBirth  = r.PlaceOfBirth,
                Sex           = r.Sex,
                PhilSysNumber = r.PhilSysNumber,
                EducationalAttainment = r.EducationalAttainment,
                Religion      = r.Religion,

                EmergencyContactName   = r.EmergencyContactName,
                EmergencyRelationship  = r.EmergencyRelationship,
                EmergencyAddress       = r.EmergencyAddress,
                EmergencyContactNumber = r.EmergencyContactNumber,

                FamilyMembers = r.FamilyMembers,
                Children      = r.Children,

                CircumstanceA1          = r.CircumstanceA1,
                CircumstanceA2          = r.CircumstanceA2,
                CircumstanceA2Cause     = r.CircumstanceA2Cause,
                CircumstanceA2Date      = r.CircumstanceA2Date,
                CircumstanceA3          = r.CircumstanceA3,
                CircumstanceA4          = r.CircumstanceA4,
                CircumstanceA4Disability = r.CircumstanceA4Disability,
                CircumstanceA5          = r.CircumstanceA5,
                CircumstanceA5Period    = r.CircumstanceA5Period,
                CircumstanceA6          = r.CircumstanceA6,
                CircumstanceA6Nullity   = r.CircumstanceA6Nullity,
                CircumstanceA6Annulment = r.CircumstanceA6Annulment,
                CircumstanceA7          = r.CircumstanceA7,
                CircumstanceB           = r.CircumstanceB,
                CircumstanceBStayAbroad = r.CircumstanceBStayAbroad,
                CircumstanceC           = r.CircumstanceC,
                CircumstanceD           = r.CircumstanceD,
                CircumstanceE           = r.CircumstanceE,
                CircumstanceF           = r.CircumstanceF,

                NeedsAndProblems  = r.NeedsAndProblems,
                OtherIncomeSource = r.OtherIncomeSource,
                IsPantawidBeneficiary = r.IsPantawidBeneficiary,
                IsIndigenous      = r.IsIndigenous,
                IsLGBT            = r.IsLGBT,

                DateOfApplication = r.DateOfApplication,

                // --- mutable fields (taken from the form) ---
                IsNewApplicant = false,
                IsRenewal      = true,
                LastUpdated    = DateTime.Today,   // signals the 1-year validity reset

                CivilStatus    = GetCombo(CmbCivilStatus),
                MonthlyIncome  = TxtMonthlyIncome.Text.Trim(),
                IsEmployed     = ChkEmployed.IsChecked     == true,
                IsSelfEmployed = ChkSelfEmployed.IsChecked == true,
                IsNotEmployed  = ChkNotEmployed.IsChecked  == true,
                Occupation     = TxtOccupation.Text.Trim(),
                Address        = TxtAddress.Text.Trim(),
                Barangay       = TxtBarangay.Text.Trim(),
                ContactNumber  = SOLUM_UI.Services.OcrService.FormatPhoneNumber(TxtContact.Text.Trim()),
                Status         = GetCombo(CmbStatus),

                // carry-through BloodType / Citizenship / Height / Weight
                Citizenship    = r.Citizenship,
                BloodType      = r.BloodType,
                Height         = r.Height,
                Weight         = r.Weight,
            };
        }

        // ── event handlers ─────────────────────────────────────────────────────

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            TxtValidationMsg.Visibility = Visibility.Collapsed;
            if (!Validate()) return;
            GoToStep(2);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            TxtValidationMsg.Visibility = Visibility.Collapsed;
            GoToStep(1);
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            Result       = BuildResult();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // ── field change / validation clear ───────────────────────────────────

        private void CmbCivilStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => ClearErr(CmbCivilStatus, ErrCivilStatus);

        private void TxtAddress_TextChanged(object sender, TextChangedEventArgs e)
            => ClearErr(TxtAddress, ErrAddress);

        private void TxtBarangay_TextChanged(object sender, TextChangedEventArgs e)
            => ClearErr(TxtBarangay, ErrBarangay);

        private void TxtContact_TextChanged(object sender, TextChangedEventArgs e)
        {
            FormatPhoneBox(TxtContact);
            ClearErr(TxtContact, ErrContact);
        }

        private bool _formattingPhone = false;

        private void FormatPhoneBox(TextBox tb)
        {
            if (tb == null || _formattingPhone) return;
            _formattingPhone = true;
            try
            {
                string raw    = tb.Text ?? "";
                string digits = Regex.Replace(raw, @"\D", "");
                if (digits.Length > 11) digits = digits.Substring(0, 11);

                string formatted;
                if (digits.Length == 11)
                    formatted = $"{digits.Substring(0, 4)}-{digits.Substring(4, 3)}-{digits.Substring(7, 4)}";
                else if (digits.Length > 7)
                    formatted = $"{digits.Substring(0, 4)}-{digits.Substring(4, 3)}-{digits.Substring(7)}";
                else if (digits.Length > 4)
                    formatted = $"{digits.Substring(0, 4)}-{digits.Substring(4)}";
                else
                    formatted = digits;

                if (raw != formatted)
                {
                    int caret = tb.SelectionStart;
                    int diff  = formatted.Length - raw.Length;
                    tb.Text            = formatted;
                    tb.SelectionStart  = Math.Max(0, Math.Min(formatted.Length, caret + diff));
                }
            }
            finally
            {
                _formattingPhone = false;
            }
        }

        private void Phone_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"^[\d\-]+$");

        private void Phone_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back && sender is TextBox tb)
            {
                int sel = tb.SelectionStart;
                if (sel > 0 && tb.SelectionLength == 0 && sel <= tb.Text.Length && tb.Text[sel - 1] == '-')
                {
                    int rem = sel - 2;
                    if (rem >= 0) { tb.Text = tb.Text.Remove(rem, 2); tb.SelectionStart = rem; e.Handled = true; }
                }
            }
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
    }
}
