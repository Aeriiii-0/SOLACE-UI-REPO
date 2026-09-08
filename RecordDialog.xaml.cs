using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SOLUM_UI.Models;

namespace SOLUM_UI
{
    public class FamilyMemberRow : INotifyPropertyChanged
    {
        private string _memberName;
        private string _sex;
        private string _age;
        private string _birthdate;
        private string _civilStatus;
        private string _relationship;
        private string _educationEmployment;
        private string _income;

        public event PropertyChangedEventHandler PropertyChanged;
        private void N(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        public string MemberName
        {
            get => _memberName;
            set
            {
                _memberName = value;
                N(nameof(MemberName));
            }
        }

        public string Sex
        {
            get => _sex;
            set
            {
                string s = value?.Trim() ?? string.Empty;
                if (s.Equals("Female", StringComparison.OrdinalIgnoreCase) || s.Equals("F", StringComparison.OrdinalIgnoreCase))
                    _sex = "F";
                else if (s.Equals("Male", StringComparison.OrdinalIgnoreCase) || s.Equals("M", StringComparison.OrdinalIgnoreCase))
                    _sex = "M";
                else if (s.Length > 0)
                    _sex = s.Substring(0, 1).ToUpperInvariant();
                else
                    _sex = string.Empty;
                N(nameof(Sex));
            }
        }

        public string OfficialSex
        {
            get
            {
                if (string.Equals(_sex, "F", StringComparison.OrdinalIgnoreCase)) return "Female";
                if (string.Equals(_sex, "M", StringComparison.OrdinalIgnoreCase)) return "Male";
                return _sex ?? string.Empty;
            }
        }

        public string Age
        {
            get => _age;
            set
            {
                _age = SOLUM_UI.Services.OcrService.CleanAgeString(value);
                N(nameof(Age));
            }
        }

        public string Birthdate
        {
            get => _birthdate;
            set
            {
                _birthdate = value;
                N(nameof(Birthdate));

                if (!string.IsNullOrWhiteSpace(value))
                {
                    string norm = SOLUM_UI.Services.OcrService.NormalizeDateString(value, out DateTime? dt);
                    if (dt.HasValue && dt.Value != DateTime.MinValue)
                    {
                        int calcAge = SOLUM_UI.Services.OcrService.CalculateAge(dt.Value);
                        if (calcAge >= 0 && calcAge <= 120)
                        {
                            _age = calcAge.ToString();
                            N(nameof(Age));
                        }
                    }
                }
                else
                {
                    _age = string.Empty;
                    N(nameof(Age));
                }
            }
        }

        public string CivilStatus         { get => _civilStatus;         set { _civilStatus = value;         N(nameof(CivilStatus)); } }
        public string Relationship        { get => _relationship;        set { _relationship = value;        N(nameof(Relationship)); } }
        public string EducationEmployment { get => _educationEmployment; set { _educationEmployment = value; N(nameof(EducationEmployment)); } }
        public string Income              { get => _income;              set { _income = value;              N(nameof(Income)); } }
    }

    public partial class RecordDialog : Window
    {
        public SoloParentRecord Result { get; private set; }
        private readonly SoloParentRecord _existing;
        private int _currentStep = 1;
        private bool _savedSuccessfully = false;
        private readonly ObservableCollection<FamilyMemberRow> _familyRowData = new ObservableCollection<FamilyMemberRow>();

        private static readonly SolidColorBrush ErrorBrush   = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
        private static readonly SolidColorBrush DefaultBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xB8, 0xC0));
        private static readonly Thickness DefaultThickness   = new Thickness(1);

        public RecordDialog(SoloParentRecord existing = null)
        {
            InitializeComponent();
            _existing = existing;

            for (int i = 0; i < 5; i++)
                _familyRowData.Add(new FamilyMemberRow());
            FamilyRows.ItemsSource = _familyRowData;

            if (_existing != null)
            {
                FormSubtitle.Text = string.IsNullOrEmpty(_existing.Id) 
                    ? "Add New Record (OCR Pre-Populated — Please Verify)" 
                    : "Edit Record — " + _existing.Id;
                PopulateFields(_existing);
            }
            else
            {
                FormSubtitle.Text = "Add New Record";
                DpDateOfApplication.SelectedDate = DateTime.Today;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ApplySize();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => ApplySize();

        private void ApplySize()
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

            DialogShell.MaxHeight = screenH * 0.90;
            DialogShell.Width     = Math.Min(screenW * 0.88, 1320);
            DialogShell.MinWidth  = Math.Max(screenW * 0.60, 680);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_savedSuccessfully) return;
            if (!HasAnyInput()) return;

            var result = MessageBox.Show(
                "You have unsaved information in this form.\n\nDiscard changes and close?",
                "Unsaved Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
                DialogShell.Visibility = Visibility.Visible;
            }
        }

        private bool HasAnyInput()
        {
            if (!string.IsNullOrWhiteSpace(TxtLastName.Text))   return true;
            if (!string.IsNullOrWhiteSpace(TxtFirstName.Text))  return true;
            if (!string.IsNullOrWhiteSpace(TxtMiddleName.Text)) return true;
            if (!string.IsNullOrWhiteSpace(TxtAddress.Text))    return true;
            if (!string.IsNullOrWhiteSpace(TxtBarangay.Text))   return true;
            if (!string.IsNullOrWhiteSpace(TxtContact.Text))    return true;
            if (DpBirthdate.SelectedDate.HasValue)               return true;
            if (CmbSex.SelectedItem != null)                     return true;
            if (CmbCivilStatus.SelectedItem != null)             return true;
            return false;
        }

        private void Circumstance_Changed(object sender, RoutedEventArgs e)
        {
            PnlA2.Visibility = ChkA2.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            PnlA4.Visibility = ChkA4.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            PnlA5.Visibility = ChkA5.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            PnlB.Visibility  = ChkB.IsChecked  == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AddFamilyRow_Click(object sender, RoutedEventArgs e)
        {
            _familyRowData.Add(new FamilyMemberRow());
        }

        private void PopulateFields(SoloParentRecord r)
        {
            TxtLastName.Text     = r.Surname       ?? string.Empty;
            TxtFirstName.Text    = r.FirstName     ?? string.Empty;
            TxtMiddleName.Text   = r.MiddleName    ?? string.Empty;
            TxtExtension.Text    = r.ExtensionName ?? string.Empty;
            TxtPlaceOfBirth.Text = r.PlaceOfBirth  ?? string.Empty;
            TxtAddress.Text      = r.Address       ?? string.Empty;
            TxtContact.Text      = SOLUM_UI.Services.OcrService.FormatPhoneNumber(r.ContactNumber ?? string.Empty);
            TxtBarangay.Text     = r.Barangay      ?? string.Empty;

            if (r.DateOfBirth != DateTime.MinValue) DpBirthdate.SelectedDate = r.DateOfBirth;
            DpDateOfApplication.SelectedDate = r.DateOfApplication != DateTime.MinValue ? r.DateOfApplication : DateTime.Today;

            ChkNewApplicant.IsChecked = r.IsNewApplicant;
            ChkRenewal.IsChecked      = r.IsRenewal;

            SetComboByContent(CmbSex,         r.Sex);
            SetComboByContent(CmbCivilStatus, r.CivilStatus);
            SetComboByContent(CmbStatus,      r.Status);
            SetComboByContent(CmbEducation,   r.EducationalAttainment);

            TxtPhilSys.Text        = r.PhilSysNumber   ?? string.Empty;
            TxtReligion.Text       = r.Religion         ?? string.Empty;
            TxtOccupation.Text     = r.Occupation       ?? string.Empty;
            TxtMonthlyIncome.Text  = r.MonthlyIncome    ?? string.Empty;

            ChkEmployed.IsChecked    = r.IsEmployed;
            ChkSelfEmployed.IsChecked = r.IsSelfEmployed;
            ChkNotEmployed.IsChecked  = r.IsNotEmployed;

            TxtEmergencyContact.Text = r.EmergencyContactName   ?? string.Empty;
            TxtRelationship.Text     = r.EmergencyRelationship  ?? string.Empty;
            TxtEmergencyAddress.Text = r.EmergencyAddress       ?? string.Empty;
            TxtEmergencyNumber.Text  = SOLUM_UI.Services.OcrService.FormatPhoneNumber(r.EmergencyContactNumber ?? string.Empty);

            ChkA1.IsChecked = r.CircumstanceA1;
            ChkA2.IsChecked = r.CircumstanceA2;
            TxtA2Cause.Text = r.CircumstanceA2Cause ?? string.Empty;
            if (r.CircumstanceA2Date != DateTime.MinValue) DpA2Date.SelectedDate = r.CircumstanceA2Date;
            ChkA3.IsChecked = r.CircumstanceA3;
            ChkA4.IsChecked = r.CircumstanceA4;
            TxtA4Disability.Text = r.CircumstanceA4Disability ?? string.Empty;
            ChkA5.IsChecked = r.CircumstanceA5;
            TxtA5Period.Text = r.CircumstanceA5Period ?? string.Empty;
            ChkA6.IsChecked = r.CircumstanceA6;
            ChkA7.IsChecked = r.CircumstanceA7;
            ChkB.IsChecked  = r.CircumstanceB;
            TxtBStayAbroad.Text = r.CircumstanceBStayAbroad ?? string.Empty;
            ChkC.IsChecked = r.CircumstanceC;
            ChkD.IsChecked = r.CircumstanceD;
            ChkE.IsChecked = r.CircumstanceE;
            ChkF.IsChecked = r.CircumstanceF;

            TxtNeeds.Text       = r.NeedsAndProblems  ?? string.Empty;
            TxtOtherIncome.Text = r.OtherIncomeSource ?? string.Empty;

            if (r.FamilyMembers != null && r.FamilyMembers.Count > 0)
            {
                _familyRowData.Clear();
                foreach (var fm in r.FamilyMembers)
                {
                    string normDob = SOLUM_UI.Services.OcrService.NormalizeDateString(fm.Birthdate, out DateTime? dt);
                    string ageVal = fm.Age;
                    if (dt.HasValue && dt.Value != DateTime.MinValue)
                    {
                        int calcAge = SOLUM_UI.Services.OcrService.CalculateAge(dt.Value);
                        if (calcAge >= 0 && calcAge <= 120)
                        {
                            ageVal = calcAge.ToString();
                        }
                    }

                    _familyRowData.Add(new FamilyMemberRow
                    {
                        MemberName          = fm.MemberName,
                        Sex                 = fm.Sex,
                        Age                 = ageVal,
                        Birthdate           = string.IsNullOrWhiteSpace(normDob) ? fm.Birthdate : normDob,
                        CivilStatus         = fm.CivilStatus,
                        Relationship        = fm.Relationship,
                        EducationEmployment = fm.EducationEmployment,
                        Income              = fm.Income
                    });
                }
            }

            Circumstance_Changed(null, null);
            UpdateAge(r.DateOfBirth);
        }

        private void SetComboByContent(ComboBox combo, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || combo == null) return;
            string val = value.Trim();
            foreach (ComboBoxItem item in combo.Items)
            {
                if (string.Equals(item.Content?.ToString(), val, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedItem = item;
                    return;
                }
            }

            string valNorm = System.Text.RegularExpressions.Regex.Replace(val.ToLowerInvariant(), @"[^a-z0-9]", "");
            foreach (ComboBoxItem item in combo.Items)
            {
                string text = item.Content?.ToString() ?? "";
                string textNorm = System.Text.RegularExpressions.Regex.Replace(text.ToLowerInvariant(), @"[^a-z0-9]", "");
                if (string.IsNullOrEmpty(textNorm)) continue;
                if (valNorm.StartsWith(textNorm) || textNorm.StartsWith(valNorm) ||
                    valNorm.Contains(textNorm) || textNorm.Contains(valNorm))
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
        }

        private string GetComboValue(ComboBox combo)
        {
            ComboBoxItem sel = combo.SelectedItem as ComboBoxItem;
            return sel?.Content?.ToString() ?? string.Empty;
        }

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

        private void NameOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-ZñÑ\s\.\-']+$");
        }

        private void FamilyBirthdate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is FamilyMemberRow row)
            {
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    row.Birthdate = string.Empty;
                    row.Age = string.Empty;
                    return;
                }

                string norm = SOLUM_UI.Services.OcrService.NormalizeDateString(tb.Text, out DateTime? dt);
                if (!string.IsNullOrEmpty(norm))
                {
                    row.Birthdate = norm;
                }
                if (dt.HasValue && dt.Value != DateTime.MinValue)
                {
                    int calcAge = SOLUM_UI.Services.OcrService.CalculateAge(dt.Value);
                    if (calcAge >= 0 && calcAge <= 120)
                    {
                        row.Age = calcAge.ToString();
                    }
                }
                else
                {
                    row.Age = string.Empty;
                }
            }
        }

        private void FamilyMemberName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is FamilyMemberRow row)
            {
                row.MemberName = SOLUM_UI.Services.OcrService.CleanPersonName(tb.Text);
            }
        }

        private bool _isFormattingPhone = false;

        private void FormatPhoneBox(TextBox tb)
        {
            if (tb == null || _isFormattingPhone) return;
            _isFormattingPhone = true;
            try
            {
                string raw = tb.Text ?? "";
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
                    int diff = formatted.Length - raw.Length;
                    tb.Text = formatted;
                    tb.SelectionStart = Math.Max(0, Math.Min(formatted.Length, caret + diff));
                }
            }
            finally
            {
                _isFormattingPhone = false;
            }
        }

        private void Phone_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back && sender is TextBox tb)
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

        private void Phone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[\d\-]+$");
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

        private void TxtLastName_TextChanged(object sender, TextChangedEventArgs e)       => ClearFieldError(TxtLastName,     ErrLastName);
        private void TxtFirstName_TextChanged(object sender, TextChangedEventArgs e)      => ClearFieldError(TxtFirstName,    ErrFirstName);
        private void TxtBarangay_TextChanged(object sender, TextChangedEventArgs e)       => ClearFieldError(TxtBarangay,     ErrBarangay);
        private void TxtAddress_TextChanged(object sender, TextChangedEventArgs e)        => ClearFieldError(TxtAddress,      ErrAddress);
        private void TxtContact_TextChanged(object sender, TextChangedEventArgs e)
        {
            FormatPhoneBox(TxtContact);
            ClearFieldError(TxtContact, ErrContact);
        }

        private void TxtEmergencyNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            FormatPhoneBox(TxtEmergencyNumber);
        }
        private void TxtPlaceOfBirth_TextChanged(object sender, TextChangedEventArgs e)   => ClearFieldError(TxtPlaceOfBirth, ErrPlaceOfBirth);
        private void CmbSex_SelectionChanged(object sender, SelectionChangedEventArgs e)         => ClearFieldError(CmbSex,         ErrSex);
        private void CmbCivilStatus_SelectionChanged(object sender, SelectionChangedEventArgs e) => ClearFieldError(CmbCivilStatus, ErrCivilStatus);
        private void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)      => ClearFieldError(CmbStatus,      ErrStatus);

        private void MarkError(Control ctrl, TextBlock lbl, string msg)
        {
            if (ctrl != null)
            {
                ctrl.BorderBrush     = ErrorBrush;
                ctrl.BorderThickness = new Thickness(1.5);
            }
            if (lbl != null)
            {
                lbl.Text       = msg;
                lbl.Visibility = Visibility.Visible;
            }
        }

        private void MarkDateError(DatePicker dp, TextBlock lbl, string msg)
        {
            if (dp != null)
            {
                dp.BorderBrush     = ErrorBrush;
                dp.BorderThickness = new Thickness(1.5);
            }
            if (lbl != null)
            {
                lbl.Text       = msg;
                lbl.Visibility = Visibility.Visible;
            }
        }

        private void ClearFieldError(Control ctrl, TextBlock lbl)
        {
            if (ctrl != null)
            {
                ctrl.BorderBrush     = DefaultBrush;
                ctrl.BorderThickness = DefaultThickness;
            }
            if (lbl != null)
            {
                lbl.Visibility = Visibility.Collapsed;
            }
        }

        private void ClearDateError(DatePicker dp, TextBlock lbl)
        {
            if (dp != null)
            {
                dp.BorderBrush     = DefaultBrush;
                dp.BorderThickness = DefaultThickness;
            }
            if (lbl != null)
            {
                lbl.Visibility = Visibility.Collapsed;
            }
        }

        private bool ValidateStep1()
        {
            bool ok = true;
            var missing = new List<string>();

            if (string.IsNullOrWhiteSpace(TxtLastName.Text))
            { MarkError(TxtLastName, ErrLastName, "Required."); missing.Add("Last Name"); ok = false; }

            if (string.IsNullOrWhiteSpace(TxtFirstName.Text))
            { MarkError(TxtFirstName, ErrFirstName, "Required."); missing.Add("First Name"); ok = false; }

            if (string.IsNullOrWhiteSpace(TxtPlaceOfBirth.Text))
            { MarkError(TxtPlaceOfBirth, ErrPlaceOfBirth, "Required."); missing.Add("Birthplace"); ok = false; }

            if (CmbSex.SelectedItem == null)
            { MarkError(CmbSex, ErrSex, "Required."); missing.Add("Sex"); ok = false; }

            if (CmbCivilStatus.SelectedItem == null)
            { MarkError(CmbCivilStatus, ErrCivilStatus, "Required."); missing.Add("Civil Status"); ok = false; }

            if (!DpBirthdate.SelectedDate.HasValue)
            { MarkDateError(DpBirthdate, ErrBirthdate, "Required."); missing.Add("Birthdate"); ok = false; }
            else if (DpBirthdate.SelectedDate.Value.Date > DateTime.Today)
            { MarkDateError(DpBirthdate, ErrBirthdate, "Cannot be in the future."); missing.Add("Birthdate (future date)"); ok = false; }
            else if (DpBirthdate.SelectedDate.Value.Date > DateTime.Today.AddYears(-15))
            { MarkDateError(DpBirthdate, ErrBirthdate, "Must be at least 15 years old."); missing.Add("Birthdate (must be 15+)"); ok = false; }

            if (string.IsNullOrWhiteSpace(TxtAddress.Text))
            { MarkError(TxtAddress, ErrAddress, "Required."); missing.Add("Address"); ok = false; }

            string contactDigits = Regex.Replace(TxtContact.Text ?? "", @"\D", "");
            if (string.IsNullOrWhiteSpace(contactDigits))
            { MarkError(TxtContact, ErrContact, "Required."); missing.Add("Contact Number"); ok = false; }
            else if (contactDigits.Length != 11)
            { MarkError(TxtContact, ErrContact, "Must be 11 digits."); missing.Add("Contact Number (11 digits)"); ok = false; }

            if (string.IsNullOrWhiteSpace(TxtBarangay.Text))
            { MarkError(TxtBarangay, ErrBarangay, "Required."); missing.Add("Barangay"); ok = false; }

            if (!ok)
                SetValidationMessage(missing);

            return ok;
        }

        private bool ValidateFields()
        {
            bool ok = ValidateStep1();
            var missing = new List<string>();

            if (CmbStatus.SelectedItem == null)
            { MarkError(CmbStatus, ErrStatus, "Required."); missing.Add("Application Status"); ok = false; }

            if (!ok && missing.Count > 0)
                SetValidationMessage(missing);

            return ok;
        }

        private void SetValidationMessage(List<string> missing)
        {
            if (missing.Count == 0) return;
            var sb = new StringBuilder("Missing required fields: ");
            sb.Append(string.Join(", ", missing));
            sb.Append(".");
            ValidationMessage.Text       = sb.ToString();
            ValidationMessage.Visibility = Visibility.Visible;
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
            if (!ValidateStep1()) return;
            GoToStep(2);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
            GoToStep(1);
        }

        private void GoToStep(int step)
        {
            _currentStep = step;
            bool onStep1 = step == 1;

            ScrollStep1.Visibility = onStep1 ? Visibility.Visible  : Visibility.Collapsed;
            ScrollStep2.Visibility = onStep1 ? Visibility.Collapsed : Visibility.Visible;
            BtnNext.Visibility     = onStep1 ? Visibility.Visible  : Visibility.Collapsed;
            BtnSave.Visibility     = onStep1 ? Visibility.Collapsed : Visibility.Visible;
            BtnBack.Visibility     = onStep1 ? Visibility.Collapsed : Visibility.Visible;

            FormStepHint.Text = onStep1
                ? "Step 1 of 2 — Personal Information"
                : "Step 2 of 2 — Circumstances & Status";

            Step1Dot.Background = onStep1
                ? new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43))
                : new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));

            if (!onStep1)
            {
                Step2Dot.Background      = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));
                Step2DotLabel.Foreground = new SolidColorBrush(Colors.White);
                Step2Title.Foreground    = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));
            }
            else
            {
                Step2Dot.Background      = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
                Step2DotLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
                Step2Title.Foreground    = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
            if (!ValidateFields()) return;

            var preview = BuildRecord();
            var fields  = BuildConfirmFields(preview);
            bool isEdit = _existing != null;

            DialogShell.Visibility = Visibility.Hidden;

            var confirm = new ConfirmDialog(
                isEdit ? "Confirm Changes"  : "Confirm New Record",
                isEdit ? "Review your changes before saving." : "Review the information below before saving.",
                isEdit ? "Confirm Changes"  : "Confirm & Save",
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
                _savedSuccessfully = true;
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
            string surname = TxtLastName.Text.Trim();
            string first   = TxtFirstName.Text.Trim();
            string middle  = TxtMiddleName.Text.Trim();

            var members = new System.Collections.Generic.List<FamilyMember>();
            foreach (var item in _familyRowData)
            {
                var row = item as FamilyMemberRow;
                if (row != null && !string.IsNullOrWhiteSpace(row.MemberName))
                {
                    string normDob = SOLUM_UI.Services.OcrService.NormalizeDateString(row.Birthdate, out DateTime? dt);
                    string ageVal = row.Age;
                    if (dt.HasValue && dt.Value != DateTime.MinValue)
                    {
                        int calcAge = SOLUM_UI.Services.OcrService.CalculateAge(dt.Value);
                        if (calcAge >= 0 && calcAge <= 120)
                        {
                            ageVal = calcAge.ToString();
                        }
                    }

                    members.Add(new FamilyMember
                    {
                        MemberName          = SOLUM_UI.Services.OcrService.CleanPersonName(row.MemberName),
                        Sex                 = row.OfficialSex,
                        Age                 = ageVal,
                        Birthdate           = string.IsNullOrWhiteSpace(normDob) ? row.Birthdate : normDob,
                        CivilStatus         = row.CivilStatus,
                        Relationship        = row.Relationship,
                        EducationEmployment = row.EducationEmployment,
                        Income              = row.Income
                    });
                }
            }

            return new SoloParentRecord
            {
                Id            = _existing != null ? _existing.Id : "SP-" + DateTime.Now.ToString("yyMMddHHmm"),
                Surname       = surname,
                FirstName     = first,
                MiddleName    = middle,
                ExtensionName = TxtExtension.Text.Trim(),
                Name          = surname + ", " + first + (string.IsNullOrEmpty(middle) ? "" : " " + middle),
                DateOfBirth   = DpBirthdate.SelectedDate.Value,
                PlaceOfBirth  = TxtPlaceOfBirth.Text.Trim(),
                Sex           = GetComboValue(CmbSex),
                CivilStatus   = GetComboValue(CmbCivilStatus),
                Citizenship   = string.Empty,
                BloodType     = string.Empty,
                Height        = string.Empty,
                Weight        = string.Empty,
                Address       = TxtAddress.Text.Trim(),
                ContactNumber = SOLUM_UI.Services.OcrService.FormatPhoneNumber(TxtContact.Text.Trim()),
                Barangay      = TxtBarangay.Text.Trim(),
                DateAdmitted  = _existing?.DateAdmitted ?? DateTime.Today,
                Status        = GetComboValue(CmbStatus),
                LastUpdated   = DateTime.Today,

                IsNewApplicant      = ChkNewApplicant.IsChecked == true,
                IsRenewal           = ChkRenewal.IsChecked == true,
                DateOfApplication   = DpDateOfApplication.SelectedDate ?? DateTime.Today,

                EducationalAttainment = GetComboValue(CmbEducation),
                PhilSysNumber         = TxtPhilSys.Text.Trim(),
                Religion              = TxtReligion.Text.Trim(),
                Occupation            = TxtOccupation.Text.Trim(),
                MonthlyIncome         = TxtMonthlyIncome.Text.Trim(),
                IsEmployed            = ChkEmployed.IsChecked == true,
                IsSelfEmployed        = ChkSelfEmployed.IsChecked == true,
                IsNotEmployed         = ChkNotEmployed.IsChecked == true,

                EmergencyContactName   = TxtEmergencyContact.Text.Trim(),
                EmergencyRelationship  = TxtRelationship.Text.Trim(),
                EmergencyAddress       = TxtEmergencyAddress.Text.Trim(),
                EmergencyContactNumber = SOLUM_UI.Services.OcrService.FormatPhoneNumber(TxtEmergencyNumber.Text.Trim()),

                CircumstanceA1             = ChkA1.IsChecked == true,
                CircumstanceA2             = ChkA2.IsChecked == true,
                CircumstanceA2Cause        = TxtA2Cause.Text.Trim(),
                CircumstanceA2Date         = DpA2Date.SelectedDate ?? DateTime.MinValue,
                CircumstanceA3             = ChkA3.IsChecked == true,
                CircumstanceA4             = ChkA4.IsChecked == true,
                CircumstanceA4Disability   = TxtA4Disability.Text.Trim(),
                CircumstanceA5             = ChkA5.IsChecked == true,
                CircumstanceA5Period       = TxtA5Period.Text.Trim(),
                CircumstanceA6             = ChkA6.IsChecked == true,
                CircumstanceA7             = ChkA7.IsChecked == true,
                CircumstanceB              = ChkB.IsChecked == true,
                CircumstanceBStayAbroad    = TxtBStayAbroad.Text.Trim(),
                CircumstanceC              = ChkC.IsChecked == true,
                CircumstanceD              = ChkD.IsChecked == true,
                CircumstanceE              = ChkE.IsChecked == true,
                CircumstanceF              = ChkF.IsChecked == true,

                FamilyMembers   = members,
                NeedsAndProblems  = TxtNeeds.Text.Trim(),
                OtherIncomeSource = TxtOtherIncome.Text.Trim(),
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

            string sec = "Applicant Type";
            string appType = r.IsNewApplicant ? "New Applicant" : r.IsRenewal ? "For Renewal" : "—";
            Add(sec, "Application Type",    appType, null);
            Add(sec, "Date of Application", r.DateOfApplication != DateTime.MinValue ? r.DateOfApplication.ToString("MMMM d, yyyy") : "—", null);

            sec = "Personal Information";
            Add(sec, "Last Name",    r.Surname,       _existing?.Surname);
            Add(sec, "First Name",   r.FirstName,     _existing?.FirstName);
            Add(sec, "Middle Name",  r.MiddleName,    _existing?.MiddleName);
            Add(sec, "Extension",    r.ExtensionName, _existing?.ExtensionName);
            Add(sec, "Sex",          r.Sex,           _existing?.Sex);
            Add(sec, "Civil Status", r.CivilStatus,   _existing?.CivilStatus);
            Add(sec, "Date of Birth", r.DateOfBirth.ToString("MMMM d, yyyy"), _existing?.DateOfBirth.ToString("MMMM d, yyyy"));
            Add(sec, "Age",           TxtAge.Text,     null);
            Add(sec, "Birthplace",    r.PlaceOfBirth,  _existing?.PlaceOfBirth);
            Add(sec, "Educational Attainment", r.EducationalAttainment, _existing?.EducationalAttainment);
            Add(sec, "PhilSys Card No.", r.PhilSysNumber, _existing?.PhilSysNumber);
            Add(sec, "Religion",     r.Religion,   _existing?.Religion);
            Add(sec, "Occupation",   r.Occupation, _existing?.Occupation);
            Add(sec, "Monthly Income", string.IsNullOrWhiteSpace(r.MonthlyIncome) ? "—" : "₱ " + r.MonthlyIncome, null);
            Add(sec, "Employment Status", r.EmploymentStatusDisplay, null);

            sec = "Address & Contact";
            Add(sec, "Address",     r.Address,       _existing?.Address);
            Add(sec, "Barangay",    r.Barangay,      _existing?.Barangay);
            Add(sec, "Contact No.", r.ContactNumber, _existing?.ContactNumber);

            sec = "Emergency Contact";
            Add(sec, "Contact Person", r.EmergencyContactName,   _existing?.EmergencyContactName);
            Add(sec, "Relationship",   r.EmergencyRelationship,  _existing?.EmergencyRelationship);
            Add(sec, "Address",        r.EmergencyAddress,       _existing?.EmergencyAddress);
            Add(sec, "Contact No.",    r.EmergencyContactNumber, _existing?.EmergencyContactNumber);

            sec = "Circumstances";
            Add(sec, "Circumstances of Being Solo Parent", r.CircumstancesDisplay, null);

            if (r.FamilyMembers != null && r.FamilyMembers.Count > 0)
            {
                sec = "Family Composition";
                for (int i = 0; i < r.FamilyMembers.Count; i++)
                {
                    var m = r.FamilyMembers[i];
                    string memberLabel = "Member " + (i + 1);
                    string memberSummary = m.MemberName
                        + (string.IsNullOrWhiteSpace(m.Relationship) ? "" : " (" + m.Relationship + ")")
                        + (string.IsNullOrWhiteSpace(m.Age) ? "" : ", Age " + m.Age)
                        + (string.IsNullOrWhiteSpace(m.Sex) ? "" : ", " + m.Sex);
                    Add(sec, memberLabel, memberSummary, null);
                }
            }

            sec = "Needs & Income";
            Add(sec, "Needs and Problems",      r.NeedsAndProblems,  _existing?.NeedsAndProblems);
            Add(sec, "Other Sources of Income", r.OtherIncomeSource, _existing?.OtherIncomeSource);

            sec = "Application Status";
            Add(sec, "Status", r.Status, _existing?.Status);

            return list;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
