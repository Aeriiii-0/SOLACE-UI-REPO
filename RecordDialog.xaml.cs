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
    public partial class RecordDialog : Window
    {
        public SoloParentRecord Result { get; private set; }
        private readonly SoloParentRecord _existing;

        private static readonly SolidColorBrush ErrorBrush   = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
        private static readonly SolidColorBrush DefaultBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xB8, 0xC0));
        private static readonly Thickness DefaultThickness   = new Thickness(1);

        public RecordDialog(SoloParentRecord existing = null)
        {
            InitializeComponent();
            _existing = existing;
            FamilyRows.ItemsSource = new string[5];

            Loaded += OnLoaded;

            if (_existing != null)
            {
                FormSubtitle.Text = "Edit Record — " + _existing.Id;
                PopulateFields(_existing);
            }
            else
            {
                FormSubtitle.Text = "Add New Record";
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            double screenW, screenH, screenLeft, screenTop;

            if (Owner != null && Owner.WindowState == WindowState.Maximized)
            {
                screenW    = SystemParameters.WorkArea.Width;
                screenH    = SystemParameters.WorkArea.Height;
                screenLeft = SystemParameters.WorkArea.Left;
                screenTop  = SystemParameters.WorkArea.Top;
            }
            else if (Owner != null)
            {
                screenW    = Owner.ActualWidth;
                screenH    = Owner.ActualHeight;
                screenLeft = Owner.Left;
                screenTop  = Owner.Top;
            }
            else
            {
                screenW    = SystemParameters.WorkArea.Width;
                screenH    = SystemParameters.WorkArea.Height;
                screenLeft = SystemParameters.WorkArea.Left;
                screenTop  = SystemParameters.WorkArea.Top;
            }

            Width  = screenW;
            Height = screenH;
            Left   = screenLeft;
            Top    = screenTop;

            DialogShell.MaxHeight = screenH * 0.85;
            DialogShell.Width     = Math.Min(screenW * 0.72, 820);
        }

        //field population

        private void PopulateFields(SoloParentRecord r)
        {
            TxtLastName.Text         = r.Surname        ?? string.Empty;
            TxtFirstName.Text        = r.FirstName      ?? string.Empty;
            TxtMiddleName.Text       = r.MiddleName     ?? string.Empty;
            TxtExtension.Text        = r.ExtensionName  ?? string.Empty;
            TxtPlaceOfBirth.Text     = r.PlaceOfBirth   ?? string.Empty;
            TxtCitizenship.Text      = r.Citizenship    ?? string.Empty;
            TxtHeight.Text           = r.Height         ?? string.Empty;
            TxtWeight.Text           = r.Weight         ?? string.Empty;
            TxtAddress.Text          = r.Address        ?? string.Empty;
            TxtContact.Text          = r.ContactNumber  ?? string.Empty;
            TxtBarangay.Text         = r.Barangay       ?? string.Empty;
            TxtSourceOfReferral.Text = r.SourceOfReferral ?? string.Empty;
            TxtCaseNo.Text           = r.CaseNo         ?? string.Empty;
            TxtOffense.Text          = r.OffenseCommitted ?? string.Empty;
            TxtNatureOfReferral.Text = r.NatureOfReferral ?? string.Empty;
            TxtChildren.Text         = r.Children.ToString();

            if (r.DateOfBirth   != DateTime.MinValue) DpBirthdate.SelectedDate    = r.DateOfBirth;
            if (r.DateAdmitted  != DateTime.MinValue) DpDateAdmitted.SelectedDate = r.DateAdmitted;

            SetComboByContent(CmbSex,         r.Sex);
            SetComboByContent(CmbCivilStatus, r.CivilStatus);
            SetComboByContent(CmbBloodType,   r.BloodType);
            SetComboByContent(CmbStatus,      r.Status);

            UpdateAge(r.DateOfBirth);
        }

        private void SetComboByContent(ComboBox combo, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            foreach (ComboBoxItem item in combo.Items)
                if (item.Content?.ToString() == value) { combo.SelectedItem = item; return; }
        }

        private string GetComboValue(ComboBox combo)
        {
            ComboBoxItem sel = combo.SelectedItem as ComboBoxItem;
            return sel?.Content?.ToString() ?? string.Empty;
        }

        //calculating age

        private void UpdateAge(DateTime? dob)
        {
            if (!dob.HasValue || dob.Value == DateTime.MinValue)
            {
                TxtAge.Text = string.Empty;
                return;
            }
            DateTime today = DateTime.Today;
            int age = today.Year - dob.Value.Year;
            if (dob.Value.Date > today.AddYears(-age)) age--;
            TxtAge.Text = age.ToString();
        }

        private void DpBirthdate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAge(DpBirthdate.SelectedDate);
            ClearDateError(DpBirthdate, ErrBirthdate);
        }

        //filtersw

        private void NameOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Z\s\.\-]+$");
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void ApplicantType_Changed(object sender, RoutedEventArgs e)
        {
            CheckBox clicked = sender as CheckBox;
            if (clicked == null) return;
            if (clicked == ChkNewApplicant && ChkNewApplicant.IsChecked == true) ChkRenewal.IsChecked = false;
            else if (clicked == ChkRenewal && ChkRenewal.IsChecked == true) ChkNewApplicant.IsChecked = false;
        }

        //clear on type

        private void TxtLastName_TextChanged(object sender, TextChangedEventArgs e)       => ClearFieldError(TxtLastName,     ErrLastName);
        private void TxtFirstName_TextChanged(object sender, TextChangedEventArgs e)      => ClearFieldError(TxtFirstName,    ErrFirstName);
        private void TxtBarangay_TextChanged(object sender, TextChangedEventArgs e)       => ClearFieldError(TxtBarangay,     ErrBarangay);
        private void TxtAddress_TextChanged(object sender, TextChangedEventArgs e)        => ClearFieldError(TxtAddress,      ErrAddress);
        private void TxtContact_TextChanged(object sender, TextChangedEventArgs e)        => ClearFieldError(TxtContact,      ErrContact);
        private void TxtChildren_TextChanged(object sender, TextChangedEventArgs e)       => ClearFieldError(TxtChildren,     ErrChildren);
        private void TxtPlaceOfBirth_TextChanged(object sender, TextChangedEventArgs e)   => ClearFieldError(TxtPlaceOfBirth, ErrPlaceOfBirth);
        private void TxtCitizenship_TextChanged(object sender, TextChangedEventArgs e)    => ClearFieldError(TxtCitizenship,  ErrCitizenship);
        private void CmbSex_SelectionChanged(object sender, SelectionChangedEventArgs e)         => ClearFieldError(CmbSex,         ErrSex);
        private void CmbCivilStatus_SelectionChanged(object sender, SelectionChangedEventArgs e) => ClearFieldError(CmbCivilStatus, ErrCivilStatus);
        private void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)      => ClearFieldError(CmbStatus,      ErrStatus);
        private void DpDateAdmitted_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => ClearDateError(DpDateAdmitted, ErrDateAdmitted);

        //error handling sa ui

        private void MarkError(Control ctrl, TextBlock lbl, string msg)
        {
            ctrl.BorderBrush     = ErrorBrush;
            ctrl.BorderThickness = new Thickness(1.5);
            lbl.Text             = msg;
            lbl.Visibility       = Visibility.Visible;
        }

        private void MarkDateError(DatePicker dp, TextBlock lbl, string msg)
        {
            dp.BorderBrush     = ErrorBrush;
            dp.BorderThickness = new Thickness(1.5);
            lbl.Text           = msg;
            lbl.Visibility     = Visibility.Visible;
        }

        private void ClearFieldError(Control ctrl, TextBlock lbl)
        {
            ctrl.BorderBrush     = DefaultBrush;
            ctrl.BorderThickness = DefaultThickness;
            lbl.Visibility       = Visibility.Collapsed;
        }

        private void ClearDateError(DatePicker dp, TextBlock lbl)
        {
            dp.BorderBrush     = DefaultBrush;
            dp.BorderThickness = DefaultThickness;
            lbl.Visibility     = Visibility.Collapsed;
        }

        //vqalidations

        private bool ValidateFields()
        {
            bool ok = true;

            if (string.IsNullOrWhiteSpace(TxtLastName.Text))
            { MarkError(TxtLastName, ErrLastName, "Last name is required."); ok = false; }

            if (string.IsNullOrWhiteSpace(TxtFirstName.Text))
            { MarkError(TxtFirstName, ErrFirstName, "First name is required."); ok = false; }

            if (string.IsNullOrWhiteSpace(TxtPlaceOfBirth.Text))
            { MarkError(TxtPlaceOfBirth, ErrPlaceOfBirth, "Place of birth is required."); ok = false; }

            if (string.IsNullOrWhiteSpace(TxtCitizenship.Text))
            { MarkError(TxtCitizenship, ErrCitizenship, "Citizenship is required."); ok = false; }

            if (CmbSex.SelectedItem == null)
            { MarkError(CmbSex, ErrSex, "Sex is required."); ok = false; }

            if (CmbCivilStatus.SelectedItem == null)
            { MarkError(CmbCivilStatus, ErrCivilStatus, "Civil status is required."); ok = false; }

            if (!DpBirthdate.SelectedDate.HasValue)
            { MarkDateError(DpBirthdate, ErrBirthdate, "Date of birth is required."); ok = false; }
            else if (DpBirthdate.SelectedDate.Value.Date > DateTime.Today)
            { MarkDateError(DpBirthdate, ErrBirthdate, "Date of birth cannot be in the future."); ok = false; }
            else if (DpBirthdate.SelectedDate.Value.Date > DateTime.Today.AddYears(-15))
            { MarkDateError(DpBirthdate, ErrBirthdate, "Registrant must be at least 15 years old."); ok = false; }

            if (string.IsNullOrWhiteSpace(TxtAddress.Text))
            { MarkError(TxtAddress, ErrAddress, "Address is required."); ok = false; }

            if (string.IsNullOrWhiteSpace(TxtContact.Text))
            { MarkError(TxtContact, ErrContact, "Contact number is required."); ok = false; }
            else if (TxtContact.Text.Trim().Length != 11)
            { MarkError(TxtContact, ErrContact, "Contact number must be exactly 11 digits."); ok = false; }

            if (string.IsNullOrWhiteSpace(TxtBarangay.Text))
            { MarkError(TxtBarangay, ErrBarangay, "Barangay is required."); ok = false; }

            if (!DpDateAdmitted.SelectedDate.HasValue)
            { MarkDateError(DpDateAdmitted, ErrDateAdmitted, "Date admitted is required."); ok = false; }
            else if (DpDateAdmitted.SelectedDate.Value.Date > DateTime.Today)
            { MarkDateError(DpDateAdmitted, ErrDateAdmitted, "Date admitted cannot be in the future."); ok = false; }

            if (string.IsNullOrWhiteSpace(TxtChildren.Text))
            { MarkError(TxtChildren, ErrChildren, "Number of children is required."); ok = false; }
            else
            {
                int ch;
                if (!int.TryParse(TxtChildren.Text.Trim(), out ch) || ch < 0)
                { MarkError(TxtChildren, ErrChildren, "Must be a valid non-negative number."); ok = false; }
            }

            if (CmbStatus.SelectedItem == null)
            { MarkError(CmbStatus, ErrStatus, "Status is required."); ok = false; }

            return ok;
        }



        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
            if (!ValidateFields())
            {
                ValidationMessage.Visibility = Visibility.Visible;
                return;
            }

            var preview = BuildRecord();
            var fields  = BuildConfirmFields(preview);
            bool isEdit = _existing != null;

            DialogShell.Visibility = Visibility.Hidden;

            var confirm = new ConfirmDialog(
                isEdit ? "Confirm Changes" : "Confirm New Record",
                isEdit ? "Review your changes before saving." : "Review the information below before saving.",
                isEdit ? "Confirm Changes" : "Confirm & Save",
                fields,
                screenW: Width, screenH: Height,
                screenLeft: Left, screenTop: Top
            ) { Owner = Owner ?? this };

            confirm.ShowDialog();

            if (!confirm.Confirmed)
            {
                DialogShell.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                Result = preview;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                DialogShell.Visibility = Visibility.Visible;
                MessageBox.Show(
                    "An error occurred while saving:\n\n" + ex.Message +
                    "\n\nYour form data has been preserved.",
                    "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private SoloParentRecord BuildRecord()
        {
            int.TryParse(TxtChildren.Text.Trim(), out int children);
            string surname = TxtLastName.Text.Trim();
            string first   = TxtFirstName.Text.Trim();
            string middle  = TxtMiddleName.Text.Trim();

            return new SoloParentRecord
            {
                Id               = _existing != null ? _existing.Id : "SP-" + DateTime.Now.ToString("yyMMddHHmm"),
                Surname          = surname,
                FirstName        = first,
                MiddleName       = middle,
                ExtensionName    = TxtExtension.Text.Trim(),
                Name             = surname + ", " + first + (string.IsNullOrEmpty(middle) ? "" : " " + middle),
                DateOfBirth      = DpBirthdate.SelectedDate.Value,
                PlaceOfBirth     = TxtPlaceOfBirth.Text.Trim(),
                Sex              = GetComboValue(CmbSex),
                CivilStatus      = GetComboValue(CmbCivilStatus),
                Citizenship      = TxtCitizenship.Text.Trim(),
                BloodType        = GetComboValue(CmbBloodType),
                Height           = TxtHeight.Text.Trim(),
                Weight           = TxtWeight.Text.Trim(),
                Address          = TxtAddress.Text.Trim(),
                ContactNumber    = TxtContact.Text.Trim(),
                Barangay         = TxtBarangay.Text.Trim(),
                SourceOfReferral = TxtSourceOfReferral.Text.Trim(),
                DateAdmitted     = DpDateAdmitted.SelectedDate.Value,
                CaseNo           = TxtCaseNo.Text.Trim(),
                OffenseCommitted = TxtOffense.Text.Trim(),
                NatureOfReferral = TxtNatureOfReferral.Text.Trim(),
                Status           = GetComboValue(CmbStatus),
                Children         = children,
                LastUpdated      = DateTime.Now
            };
        }

        private List<ConfirmField> BuildConfirmFields(SoloParentRecord r)
        {
            bool isEdit = _existing != null;

            string F(string v) => string.IsNullOrWhiteSpace(v) ? "—" : v;

            var list = new List<ConfirmField>();

            void Add(string section, string label, string newVal, string oldVal = null)
            {
                bool changed = isEdit && oldVal != null && oldVal != newVal;
                list.Add(new ConfirmField
                {
                    Section   = section,
                    Label     = label,
                    NewValue  = F(newVal),
                    OldValue  = oldVal != null ? F(oldVal) : null,
                    IsChanged = changed
                });
            }

            string sec = "Personal Information";
            Add(sec, "Last Name",      r.Surname,       _existing?.Surname);
            Add(sec, "First Name",     r.FirstName,     _existing?.FirstName);
            Add(sec, "Middle Name",    r.MiddleName,    _existing?.MiddleName);
            Add(sec, "Extension",      r.ExtensionName, _existing?.ExtensionName);
            Add(sec, "Sex",            r.Sex,           _existing?.Sex);
            Add(sec, "Civil Status",   r.CivilStatus,   _existing?.CivilStatus);
            Add(sec, "Date of Birth",  r.DateOfBirth.ToString("MMMM d, yyyy"),
                                       _existing?.DateOfBirth.ToString("MMMM d, yyyy"));
            Add(sec, "Age",            TxtAge.Text,     null);
            Add(sec, "Place of Birth", r.PlaceOfBirth,  _existing?.PlaceOfBirth);
            Add(sec, "Citizenship",    r.Citizenship,   _existing?.Citizenship);
            Add(sec, "Blood Type",     r.BloodType,     _existing?.BloodType);
            Add(sec, "Height (cm)",    r.Height,        _existing?.Height);
            Add(sec, "Weight (kg)",    r.Weight,        _existing?.Weight);

            sec = "Contact & Location";
            Add(sec, "Address",      r.Address,        _existing?.Address);
            Add(sec, "Barangay",     r.Barangay,       _existing?.Barangay);
            Add(sec, "Contact No.",  r.ContactNumber,  _existing?.ContactNumber);

            sec = "Referral Information";
            Add(sec, "Source of Referral", r.SourceOfReferral, _existing?.SourceOfReferral);
            Add(sec, "Date Admitted",  r.DateAdmitted.ToString("MMMM d, yyyy"),
                                       _existing?.DateAdmitted.ToString("MMMM d, yyyy"));
            Add(sec, "Case No.",       r.CaseNo,            _existing?.CaseNo);
            Add(sec, "Offense",        r.OffenseCommitted,  _existing?.OffenseCommitted);
            Add(sec, "Nature",         r.NatureOfReferral,  _existing?.NatureOfReferral);

            sec = "Status";
            Add(sec, "No. of Children", r.Children.ToString(), _existing?.Children.ToString());
            Add(sec, "Status",          r.Status,               _existing?.Status);

            return list;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
