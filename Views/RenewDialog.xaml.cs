using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
            Loaded += (s, e) => { ApplySize(); PopulateFields(); SuppressDatePickerWatermarks(); };
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

        /// <summary>
        /// Suppresses the default "Select a date" watermark in all DatePickerTextBox controls
        /// to avoid overlapping with our custom MM/DD/YYYY placeholder.
        /// </summary>
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
            TxtOccupation.Text       = r.Occupation          ?? string.Empty;
            TxtMonthlyIncome.Text    = r.MonthlyIncome       ?? string.Empty;
            TxtCasePresented.Text    = r.ProblemPresented    ?? string.Empty;
            TxtFamilyComposition.Text = r.FamilyComposition  ?? string.Empty;
            TxtNeedsProblems.Text    = r.NeedsAndProblems    ?? string.Empty;
            TxtOtherIncome.Text      = r.OtherIncomeSource   ?? string.Empty;
            TxtAdditionalClassifications.Text = r.AdditionalClassifications ?? string.Empty;

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
            // In renewal mode, all editable fields are optional
            // The user is just updating what they want to update
            // We don't need strict validation like we do for new records
            TxtValidationMsg.Visibility = Visibility.Collapsed;
            return true;
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

            // Changed fields summary - RENEWAL MODE (editable sections 3-6)
            string newIncome         = TxtMonthlyIncome.Text.Trim();
            string newEmpStatus      = ChkEmployed.IsChecked     == true ? "Employed"
                                     : ChkSelfEmployed.IsChecked == true ? "Self-Employed"
                                     : "Not Employed";
            string newOccupation     = TxtOccupation.Text.Trim();
            string newCasePresented  = TxtCasePresented.Text.Trim();
            string newFamily         = TxtFamilyComposition.Text.Trim();
            string newNeeds          = TxtNeedsProblems.Text.Trim();
            string newOtherIncome    = TxtOtherIncome.Text.Trim();
            string newClassifications = TxtAdditionalClassifications.Text.Trim();

            string oldEmpStatus = r.IsEmployed ? "Employed"
                                : r.IsSelfEmployed ? "Self-Employed"
                                : r.IsNotEmployed ? "Not Employed" : "—";

            var rows = new List<RenewReviewRow>
            {
                MakeRow("Occupation",                F(r.Occupation),          F(newOccupation)),
                MakeRow("Monthly Income",            F(r.MonthlyIncome),        F(string.IsNullOrWhiteSpace(newIncome) ? "—" : newIncome)),
                MakeRow("Employment Status",         oldEmpStatus,              newEmpStatus),
                MakeRow("III. Problem Presented",    F(r.ProblemPresented),     F(newCasePresented)),
                MakeRow("IV. Family Composition",    F(r.FamilyComposition),    F(newFamily)),
                MakeRow("V. Needs and Problems",     F(r.NeedsAndProblems),     F(newNeeds)),
                MakeRow("VI. Other Sources",         F(r.OtherIncomeSource),    F(newOtherIncome)),
                MakeRow("Additional Classifications", F(r.AdditionalClassifications), F(newClassifications)),
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

                IsPantawidBeneficiary = r.IsPantawidBeneficiary,
                IsIndigenous      = r.IsIndigenous,
                IsLGBT            = r.IsLGBT,

                DateOfApplication = r.DateOfApplication,

                // --- mutable fields (taken from the form) ---
                IsNewApplicant = false,
                IsRenewal      = true,
                LastUpdated    = DateTime.Today,   // signals the 1-year validity reset

                MonthlyIncome  = TxtMonthlyIncome.Text.Trim(),
                IsEmployed     = ChkEmployed.IsChecked     == true,
                IsSelfEmployed = ChkSelfEmployed.IsChecked == true,
                IsNotEmployed  = ChkNotEmployed.IsChecked  == true,
                Occupation     = TxtOccupation.Text.Trim(),
                ProblemPresented = TxtCasePresented.Text.Trim(),
                FamilyComposition = TxtFamilyComposition.Text.Trim(),
                NeedsAndProblems = TxtNeedsProblems.Text.Trim(),
                OtherIncomeSource = TxtOtherIncome.Text.Trim(),
                AdditionalClassifications = TxtAdditionalClassifications.Text.Trim(),

                // carry-over locked fields from original record
                CivilStatus    = r.CivilStatus,
                Address        = r.Address,
                Barangay       = r.Barangay,
                ContactNumber  = r.ContactNumber,
                Status         = r.Status,

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
        // Note: In renewal, we only have editable text fields (Occupation, Monthly Income, etc.)
        // Error validation is handled during the Validate() method

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
