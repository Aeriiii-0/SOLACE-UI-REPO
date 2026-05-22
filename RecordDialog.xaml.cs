using System;
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

        private static readonly SolidColorBrush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
        private static readonly SolidColorBrush DefaultBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xB8, 0xC0));
        private static readonly Thickness DefaultThickness = new Thickness(1);

        public RecordDialog(SoloParentRecord existing = null)
        {
            InitializeComponent();
            _existing = existing;
            FamilyRows.ItemsSource = new string[4];

            if (_existing != null)
            {
                FormSubtitle.Text = "Edit Record — " + _existing.Id;
                PopulateFields(_existing);
            }
            else
            {
                FormSubtitle.Text = "New Applicant";
            }
        }

        private void PopulateFields(SoloParentRecord r)
        {
            TxtLastName.Text = r.Surname ?? string.Empty;
            TxtFirstName.Text = r.FirstName ?? string.Empty;
            TxtMiddleName.Text = r.MiddleName ?? string.Empty;
            TxtExtension.Text = r.ExtensionName ?? string.Empty;
            TxtPlaceOfBirth.Text = r.PlaceOfBirth ?? string.Empty;
            TxtCitizenship.Text = r.Citizenship ?? string.Empty;
            TxtHeight.Text = r.Height ?? string.Empty;
            TxtWeight.Text = r.Weight ?? string.Empty;
            TxtAddress.Text = r.Address ?? string.Empty;
            TxtContact.Text = r.ContactNumber ?? string.Empty;
            TxtBarangay.Text = r.Barangay ?? string.Empty;
            TxtSourceOfReferral.Text = r.SourceOfReferral ?? string.Empty;
            TxtCaseNo.Text = r.CaseNo ?? string.Empty;
            TxtOffense.Text = r.OffenseCommitted ?? string.Empty;
            TxtNatureOfReferral.Text = r.NatureOfReferral ?? string.Empty;
            TxtChildren.Text = r.Children.ToString();

            if (r.DateOfBirth != DateTime.MinValue) DpBirthdate.SelectedDate = r.DateOfBirth;
            if (r.DateAdmitted != DateTime.MinValue) DpDateAdmitted.SelectedDate = r.DateAdmitted;

            SetComboByContent(CmbSex, r.Gender);
            SetComboByContent(CmbCivilStatus, r.CivilStatus);
            SetComboByContent(CmbBloodType, r.BloodType);
            SetComboByContent(CmbStatus, r.Status);
        }

        private void SetComboByContent(ComboBox combo, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            foreach (ComboBoxItem item in combo.Items)
            {
                if (item.Content != null && item.Content.ToString() == value)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
        }

        private string GetComboValue(ComboBox combo)
        {
            ComboBoxItem selected = combo.SelectedItem as ComboBoxItem;
            return selected != null && selected.Content != null
                ? selected.Content.ToString()
                : string.Empty;
        }

        private void NameOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Z\s\.\-]+$");
            TextBox box = sender as TextBox;
            if (box == null) return;
            if (box.Name == "TxtLastName") ClearFieldError(TxtLastName, ErrLastName);
            if (box.Name == "TxtFirstName") ClearFieldError(TxtFirstName, ErrFirstName);
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
            TextBox box = sender as TextBox;
            if (box != null && box.Name == "TxtContact")
                ClearFieldError(TxtContact, ErrContact);
        }

        private void ApplicantType_Changed(object sender, RoutedEventArgs e)
        {
            CheckBox clicked = sender as CheckBox;
            if (clicked == null) return;
            if (clicked == ChkNewApplicant && ChkNewApplicant.IsChecked == true)
                ChkRenewal.IsChecked = false;
            else if (clicked == ChkRenewal && ChkRenewal.IsChecked == true)
                ChkNewApplicant.IsChecked = false;
        }

        private void TxtLastName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearFieldError(TxtLastName, ErrLastName);
        }

        private void TxtFirstName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearFieldError(TxtFirstName, ErrFirstName);
        }

        private void TxtBarangay_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearFieldError(TxtBarangay, ErrBarangay);
        }

        private void TxtAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearFieldError(TxtAddress, ErrAddress);
        }

        private void TxtContact_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearFieldError(TxtContact, ErrContact);
        }

        private void TxtChildren_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearFieldError(TxtChildren, ErrChildren);
        }

        private void TxtPlaceOfBirth_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearFieldError(TxtPlaceOfBirth, ErrPlaceOfBirth);
        }

        private void TxtCitizenship_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearFieldError(TxtCitizenship, ErrCitizenship);
        }

        private void CmbSex_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearFieldError(CmbSex, ErrSex);
        }

        private void CmbCivilStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearFieldError(CmbCivilStatus, ErrCivilStatus);
        }

        private void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearFieldError(CmbStatus, ErrStatus);
        }

        private void DpBirthdate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearDateError(DpBirthdate, ErrBirthdate);
        }

        private void DpDateAdmitted_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearDateError(DpDateAdmitted, ErrDateAdmitted);
        }

        private void MarkError(Control control, TextBlock label, string message)
        {
            control.BorderBrush = ErrorBrush;
            control.BorderThickness = new Thickness(1.5);
            label.Text = message;
            label.Visibility = Visibility.Visible;
        }

        private void MarkDateError(DatePicker picker, TextBlock label, string message)
        {
            picker.BorderBrush = ErrorBrush;
            picker.BorderThickness = new Thickness(1.5);
            label.Text = message;
            label.Visibility = Visibility.Visible;
        }

        private void ClearFieldError(Control control, TextBlock label)
        {
            control.BorderBrush = DefaultBrush;
            control.BorderThickness = DefaultThickness;
            label.Visibility = Visibility.Collapsed;
        }

        private void ClearDateError(DatePicker picker, TextBlock label)
        {
            picker.BorderBrush = DefaultBrush;
            picker.BorderThickness = DefaultThickness;
            label.Visibility = Visibility.Collapsed;
        }

        private bool ValidateFields()
        {
            bool valid = true;

            if (string.IsNullOrWhiteSpace(TxtLastName.Text))
            {
                MarkError(TxtLastName, ErrLastName, "Last name is required.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(TxtFirstName.Text))
            {
                MarkError(TxtFirstName, ErrFirstName, "First name is required.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(TxtPlaceOfBirth.Text))
            {
                MarkError(TxtPlaceOfBirth, ErrPlaceOfBirth, "Place of birth is required.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(TxtCitizenship.Text))
            {
                MarkError(TxtCitizenship, ErrCitizenship, "Citizenship is required.");
                valid = false;
            }

            if (CmbSex.SelectedItem == null)
            {
                MarkError(CmbSex, ErrSex, "Sex is required.");
                valid = false;
            }

            if (CmbCivilStatus.SelectedItem == null)
            {
                MarkError(CmbCivilStatus, ErrCivilStatus, "Civil status is required.");
                valid = false;
            }

            if (!DpBirthdate.SelectedDate.HasValue)
            {
                MarkDateError(DpBirthdate, ErrBirthdate, "Date of birth is required.");
                valid = false;
            }
            else if (DpBirthdate.SelectedDate.Value.Date > DateTime.Today)
            {
                MarkDateError(DpBirthdate, ErrBirthdate, "Date of birth cannot be in the future.");
                valid = false;
            }
            else if (DpBirthdate.SelectedDate.Value.Date > DateTime.Today.AddYears(-15))
            {
                MarkDateError(DpBirthdate, ErrBirthdate, "Registrant must be at least 15 years old.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(TxtAddress.Text))
            {
                MarkError(TxtAddress, ErrAddress, "Address is required.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(TxtContact.Text))
            {
                MarkError(TxtContact, ErrContact, "Contact number is required.");
                valid = false;
            }
            else if (TxtContact.Text.Trim().Length < 10)
            {
                MarkError(TxtContact, ErrContact, "Enter a valid contact number (min 10 digits).");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(TxtBarangay.Text))
            {
                MarkError(TxtBarangay, ErrBarangay, "Barangay is required.");
                valid = false;
            }

            if (!DpDateAdmitted.SelectedDate.HasValue)
            {
                MarkDateError(DpDateAdmitted, ErrDateAdmitted, "Date admitted is required.");
                valid = false;
            }
            else if (DpDateAdmitted.SelectedDate.Value.Date > DateTime.Today)
            {
                MarkDateError(DpDateAdmitted, ErrDateAdmitted, "Date admitted cannot be in the future.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(TxtChildren.Text))
            {
                MarkError(TxtChildren, ErrChildren, "Number of children is required.");
                valid = false;
            }
            else
            {
                int ch;
                if (!int.TryParse(TxtChildren.Text.Trim(), out ch) || ch < 0)
                {
                    MarkError(TxtChildren, ErrChildren, "Must be a valid non-negative number.");
                    valid = false;
                }
            }

            if (CmbStatus.SelectedItem == null)
            {
                MarkError(CmbStatus, ErrStatus, "Status is required.");
                valid = false;
            }

            return valid;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;

            if (!ValidateFields())
            {
                ValidationMessage.Visibility = Visibility.Visible;
                return;
            }

            int children = 0;
            int.TryParse(TxtChildren.Text.Trim(), out children);

            string surname = TxtLastName.Text.Trim();
            string first = TxtFirstName.Text.Trim();
            string middle = TxtMiddleName.Text.Trim();

            Result = new SoloParentRecord
            {
                Id = _existing != null ? _existing.Id : "SP-" + DateTime.Now.ToString("yyMMddHHmm"),
                Surname = surname,
                FirstName = first,
                MiddleName = middle,
                ExtensionName = TxtExtension.Text.Trim(),
                Name = surname + ", " + first + (string.IsNullOrEmpty(middle) ? "" : " " + middle),
                DateOfBirth = DpBirthdate.SelectedDate.Value,
                PlaceOfBirth = TxtPlaceOfBirth.Text.Trim(),
                Gender = GetComboValue(CmbSex),
                CivilStatus = GetComboValue(CmbCivilStatus),
                Citizenship = TxtCitizenship.Text.Trim(),
                BloodType = GetComboValue(CmbBloodType),
                Height = TxtHeight.Text.Trim(),
                Weight = TxtWeight.Text.Trim(),
                Address = TxtAddress.Text.Trim(),
                ContactNumber = TxtContact.Text.Trim(),
                Barangay = TxtBarangay.Text.Trim(),
                SourceOfReferral = TxtSourceOfReferral.Text.Trim(),
                DateAdmitted = DpDateAdmitted.SelectedDate.Value,
                CaseNo = TxtCaseNo.Text.Trim(),
                OffenseCommitted = TxtOffense.Text.Trim(),
                NatureOfReferral = TxtNatureOfReferral.Text.Trim(),
                Status = GetComboValue(CmbStatus),
                Children = children,
                LastUpdated = DateTime.Now
            };

            DialogResult = true;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
