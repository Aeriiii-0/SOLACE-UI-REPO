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
        private DateTime? _birthdateDate;
        private string _civilStatus;
        private string _relationship;
        private string _educationEmployment;
        private string _income;

        public Guid? Id { get; set; }
        public bool IsNew { get; set; } = true;

        public event PropertyChangedEventHandler PropertyChanged;
        private void N(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        public string MemberName
        {
            get => _memberName;
            set { _memberName = value; N(nameof(MemberName)); }
        }

        public string Sex
        {
            get => _sex;
            set
            {
                string s = value?.Trim() ?? string.Empty;
                if (s.Equals("Female", StringComparison.OrdinalIgnoreCase) || s.Equals("F", StringComparison.OrdinalIgnoreCase))
                    _sex = "Female";
                else if (s.Equals("Male", StringComparison.OrdinalIgnoreCase) || s.Equals("M", StringComparison.OrdinalIgnoreCase))
                    _sex = "Male";
                else
                    _sex = s;
                N(nameof(Sex));
            }
        }

        public string OfficialSex => _sex ?? string.Empty;

        public string Age
        {
            get => _age;
            set { _age = SOLUM_UI.Services.OcrService.CleanAgeString(value); N(nameof(Age)); }
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
                        _birthdateDate = dt.Value;
                        N(nameof(BirthdateDate));
                        int calcAge = SOLUM_UI.Services.OcrService.CalculateAge(dt.Value);
                        if (calcAge >= 0 && calcAge <= 120) { _age = calcAge.ToString(); N(nameof(Age)); }
                    }
                }
                else
                {
                    _birthdateDate = null;
                    N(nameof(BirthdateDate));
                    _age = string.Empty;
                    N(nameof(Age));
                }
            }
        }

        public DateTime? BirthdateDate
        {
            get => _birthdateDate;
            set
            {
                _birthdateDate = value;
                N(nameof(BirthdateDate));
                if (value.HasValue && value.Value != DateTime.MinValue)
                {
                    _birthdate = value.Value.ToString("yyyy-MM-dd");
                    N(nameof(Birthdate));
                    int calcAge = SOLUM_UI.Services.OcrService.CalculateAge(value.Value);
                    if (calcAge >= 0 && calcAge <= 120) { _age = calcAge.ToString(); N(nameof(Age)); }
                }
                else
                {
                    _birthdate = string.Empty;
                    N(nameof(Birthdate));
                    _age = string.Empty;
                    N(nameof(Age));
                }
            }
        }

        public string CivilStatus
        {
            get => _civilStatus;
            set
            {
                string s = value?.Trim() ?? string.Empty;
                if (s.Equals("Single", StringComparison.OrdinalIgnoreCase)) _civilStatus = "Single";
                else if (s.Equals("Married", StringComparison.OrdinalIgnoreCase)) _civilStatus = "Married";
                else if (s.Equals("Widowed", StringComparison.OrdinalIgnoreCase)) _civilStatus = "Widowed";
                else if (s.Equals("Separated", StringComparison.OrdinalIgnoreCase) || s.Equals("Sep", StringComparison.OrdinalIgnoreCase) || s.Equals("Divorced", StringComparison.OrdinalIgnoreCase)) _civilStatus = "Separated";
                else if (s.Equals("Annulled", StringComparison.OrdinalIgnoreCase)) _civilStatus = "Annulled";
                else if (!string.IsNullOrWhiteSpace(s)) _civilStatus = char.ToUpper(s[0]) + (s.Length > 1 ? s.Substring(1) : "");
                else _civilStatus = string.Empty;
                N(nameof(CivilStatus));
            }
        }

        public string Relationship
        {
            get => _relationship;
            set
            {
                string s = value?.Trim() ?? string.Empty;
                if (s.Equals("Child", StringComparison.OrdinalIgnoreCase) || s.Equals("Son", StringComparison.OrdinalIgnoreCase) || s.Equals("Daughter", StringComparison.OrdinalIgnoreCase))
                    _relationship = "Child";
                else if (s.Equals("Spouse", StringComparison.OrdinalIgnoreCase) || s.Equals("Husband", StringComparison.OrdinalIgnoreCase) || s.Equals("Wife", StringComparison.OrdinalIgnoreCase))
                    _relationship = "Spouse";
                else if (s.Equals("Parent", StringComparison.OrdinalIgnoreCase) || s.Equals("Father", StringComparison.OrdinalIgnoreCase) || s.Equals("Mother", StringComparison.OrdinalIgnoreCase))
                    _relationship = "Parent";
                else if (s.Equals("Sibling", StringComparison.OrdinalIgnoreCase) || s.Equals("Brother", StringComparison.OrdinalIgnoreCase) || s.Equals("Sister", StringComparison.OrdinalIgnoreCase))
                    _relationship = "Sibling";
                else if (s.Equals("Grandchild", StringComparison.OrdinalIgnoreCase) || s.Equals("Grandson", StringComparison.OrdinalIgnoreCase) || s.Equals("Granddaughter", StringComparison.OrdinalIgnoreCase))
                    _relationship = "Grandchild";
                else if (s.Equals("Grandparent", StringComparison.OrdinalIgnoreCase) || s.Equals("Grandfather", StringComparison.OrdinalIgnoreCase) || s.Equals("Grandmother", StringComparison.OrdinalIgnoreCase))
                    _relationship = "Grandparent";
                else if (s.Equals("Relative", StringComparison.OrdinalIgnoreCase))
                    _relationship = "Relative";
                else if (s.Equals("Ward", StringComparison.OrdinalIgnoreCase))
                    _relationship = "Ward";
                else if (s.Equals("Other", StringComparison.OrdinalIgnoreCase))
                    _relationship = "Other";
                else if (!string.IsNullOrWhiteSpace(s))
                    _relationship = char.ToUpper(s[0]) + (s.Length > 1 ? s.Substring(1) : "");
                else
                    _relationship = string.Empty;
                N(nameof(Relationship));
            }
        }
        public string EducationEmployment { get => _educationEmployment; set { _educationEmployment = value; N(nameof(EducationEmployment)); } }

        public string Income
        {
            get => _income;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) { _income = string.Empty; N(nameof(Income)); return; }
                string clean = System.Text.RegularExpressions.Regex.Replace(value, @"[^\d\.]", "");
                _income = clean;
                N(nameof(Income));
            }
        }

        public System.Windows.Input.ICommand DeleteCommand { get; set; }
    }

    public partial class RecordDialog : Window
    {
        public SoloParentRecord Result { get; private set; }
        private readonly SoloParentRecord _existing;
        private readonly bool _isRenewal;
        private int _currentStep = 1;
        private bool _savedSuccessfully = false;
        private readonly ObservableCollection<FamilyMemberRow> _familyRowData = new ObservableCollection<FamilyMemberRow>();
        private readonly List<Guid> _removedFamilyMemberIds = new List<Guid>();

        private static readonly SolidColorBrush ErrorBrush   = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
        private static readonly SolidColorBrush DefaultBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xB8, 0xC0));
        private static readonly Thickness DefaultThickness   = new Thickness(1);

        public RecordDialog(SoloParentRecord existing = null)
        {
            InitializeComponent();
            _existing  = existing;
            _isRenewal = false;

            for (int i = 0; i < 5; i++)
                _familyRowData.Add(MakeFamilyRow());
            FamilyRows.ItemsSource = _familyRowData;

            if (_existing != null)
            {
                FormSubtitle.Text = string.IsNullOrEmpty(_existing.Id)
                    ? "Add New Record (OCR Pre-Populated — Please Verify)"
                    : "Edit Record — " + _existing.Id;
                PopulateFields(_existing);
                // Amber stripe for edit mode (only when editing an existing saved record)
                if (!string.IsNullOrEmpty(_existing.Id))
                    Loaded += (s, e) => ApplyEditStyle();
            }
            else
            {
                FormSubtitle.Text = "Add New Record";
                DpDateOfApplication.SelectedDate = DateTime.Today;
                // Show add-mode stripe when Loaded
                Loaded += (s, e) => ApplyModeStyle(isRenewal: false);
            }
        }

        /// <summary>
        /// Opens the form in renewal mode. Identity fields are locked; only
        /// Civil Status, Monthly Income and Employment Status remain editable.
        /// </summary>
        public RecordDialog(SoloParentRecord existing, bool isRenewal) : this(existing)
        {
            if (!isRenewal) return;
            _isRenewal        = true;
            FormSubtitle.Text = "Renew Record — " + (existing?.Id ?? string.Empty);
            FormStepHint.Text = "Renewal mode — update changeable fields only";
            Loaded           += (s, e) => { ApplyRenewalLocks(); ApplyModeStyle(isRenewal: true); };
        }

        /// <summary>
        /// Tints the header and shows the mode stripe.
        /// Add/Renew = green. Edit = amber.
        /// </summary>
        private void ApplyModeStyle(bool isRenewal)
        {
            if (isRenewal)
            {
                // ── Renewal — green (same palette as add, distinct icon/label) ──
                HeaderBorder.Background  = new SolidColorBrush(Color.FromRgb(0xF2, 0xFD, 0xF5));
                HeaderBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xC3, 0xE8, 0xCB));
                FormSubtitle.Foreground  = new SolidColorBrush(Color.FromRgb(0x1B, 0x5E, 0x20));
                FormStepHint.Foreground  = new SolidColorBrush(Color.FromRgb(0x38, 0x80, 0x44));

                ModeStripe.Background    = new SolidColorBrush(Color.FromRgb(0xF0, 0xFA, 0xF1));
                ModeStripe.BorderBrush   = new SolidColorBrush(Color.FromRgb(0xC8, 0xE6, 0xC9));
                ModeStripeLabel.Text      = "Renewal";
                ModeStripeLabel.Foreground = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
                ModeStripeHint.Text       = " — Only changeable fields are editable. Identity is locked.";
                ModeStripeHint.Foreground  = new SolidColorBrush(Color.FromRgb(0x55, 0x8B, 0x5A));
                // Renewal icon: refresh arrows
                ModeStripeIcon.Data       = Geometry.Parse("M17.65,6.35C16.2,4.9 14.21,4 12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20C15.73,20 18.84,17.45 19.73,14H17.65C16.83,16.33 14.61,18 12,18A6,6 0 0,1 6,12A6,6 0 0,1 12,6C13.66,6 15.14,6.69 16.22,7.78L13,11H20V4L17.65,6.35Z");
                ModeStripeIcon.Fill       = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
                Step1Dot.Background       = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
            }
            else
            {
                // ── New Registration — green ───────────────────────────────
                HeaderBorder.Background  = new SolidColorBrush(Color.FromRgb(0xF2, 0xFD, 0xF5));
                HeaderBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xC3, 0xE8, 0xCB));
                FormSubtitle.Foreground  = new SolidColorBrush(Color.FromRgb(0x1B, 0x5E, 0x20));
                FormStepHint.Foreground  = new SolidColorBrush(Color.FromRgb(0x38, 0x80, 0x44));

                ModeStripe.Background    = new SolidColorBrush(Color.FromRgb(0xF0, 0xFA, 0xF1));
                ModeStripe.BorderBrush   = new SolidColorBrush(Color.FromRgb(0xC8, 0xE6, 0xC9));
                ModeStripeLabel.Text      = "New Registration";
                ModeStripeLabel.Foreground = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
                ModeStripeHint.Text       = " — Fill in all sections to complete the record.";
                ModeStripeHint.Foreground  = new SolidColorBrush(Color.FromRgb(0x55, 0x8B, 0x5A));
                // Add icon: plus in circle
                ModeStripeIcon.Data       = Geometry.Parse("M12,20C7.59,20 4,16.41 4,12C4,7.59 7.59,4 12,4C16.41,4 20,7.59 20,12C20,16.41 16.41,20 12,20M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M13,7H11V11H7V13H11V17H13V13H17V11H13V7Z");
                ModeStripeIcon.Fill       = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
                Step1Dot.Background       = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
            }

            ModeStripe.Visibility = Visibility.Visible;
            ModeBadge.Visibility  = Visibility.Collapsed;
        }

        /// <summary>Amber/yellow tint applied to edit-existing-record mode.</summary>
        private void ApplyEditStyle()
        {
            HeaderBorder.Background  = new SolidColorBrush(Color.FromRgb(0xFF, 0xFB, 0xEE));
            HeaderBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xE0, 0x82));
            FormSubtitle.Foreground  = new SolidColorBrush(Color.FromRgb(0x6D, 0x4C, 0x00));
            FormStepHint.Foreground  = new SolidColorBrush(Color.FromRgb(0x9E, 0x72, 0x00));

            ModeStripe.Background    = new SolidColorBrush(Color.FromRgb(0xFF, 0xF8, 0xE1));
            ModeStripe.BorderBrush   = new SolidColorBrush(Color.FromRgb(0xFF, 0xD5, 0x4F));
            ModeStripeLabel.Text      = "Editing Record";
            ModeStripeLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xB3, 0x6A, 0x00));
            ModeStripeHint.Text       = " — All fields are editable.";
            ModeStripeHint.Foreground  = new SolidColorBrush(Color.FromRgb(0x99, 0x6A, 0x00));
            // Edit icon: pencil
            ModeStripeIcon.Data       = Geometry.Parse("M20.71,7.04C21.1,6.65 21.1,6 20.71,5.63L18.37,3.29C18,2.9 17.35,2.9 16.96,3.29L15.13,5.12L18.88,8.87M3,17.25V21H6.75L17.81,9.94L14.06,6.19L3,17.25Z");
            ModeStripeIcon.Fill       = new SolidColorBrush(Color.FromRgb(0xB3, 0x6A, 0x00));
            Step1Dot.Background       = new SolidColorBrush(Color.FromRgb(0xE6, 0x5C, 0x00));

            ModeStripe.Visibility = Visibility.Visible;
            ModeBadge.Visibility  = Visibility.Collapsed;
        }

        /// <summary>
        /// Locks every field that should not change during a renewal.
        /// Only Civil Status, Monthly Income and Employment Status stay editable.
        /// </summary>
        private void ApplyRenewalLocks()
        {
            void LockBox(TextBox tb)
            {
                if (tb == null) return;
                tb.IsReadOnly  = true;
                tb.Focusable   = false;
                tb.Cursor      = System.Windows.Input.Cursors.Arrow;
                tb.Background  = new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2));
                tb.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
            }
            void LockCombo(ComboBox cmb) { if (cmb == null) return; cmb.IsEnabled = false; cmb.Opacity = 0.55; }
            void LockDate(DatePicker dp) { if (dp == null)  return; dp.IsEnabled  = false; dp.Opacity  = 0.55; }

            // Identity
            LockBox(TxtLastName); LockBox(TxtFirstName); LockBox(TxtMiddleName); LockBox(TxtExtension);
            LockDate(DpBirthdate); LockBox(TxtPlaceOfBirth); LockCombo(CmbSex); LockBox(TxtPhilSys);

            // Background (fixed for renewal)
            LockCombo(CmbEducation); LockBox(TxtReligion); LockBox(TxtOccupation);

            // Address & contact
            LockBox(TxtAddress); LockBox(TxtBarangay); LockBox(TxtContact);

            // Emergency contact
            LockBox(TxtEmergencyContact); LockBox(TxtRelationship);
            LockBox(TxtEmergencyAddress); LockBox(TxtEmergencyNumber);

            // Date of application
            LockDate(DpDateOfApplication);

            // Circumstances
            foreach (var chk in new System.Windows.Controls.Primitives.ToggleButton[]
                { ChkA1, ChkA2, ChkA3, ChkA4, ChkA5, ChkA6, ChkA7,
                  ChkB, ChkC, ChkD, ChkE, ChkF })
            { if (chk != null) { chk.IsEnabled = false; chk.Opacity = 0.50; } }

            // Family composition, notes, classifications
            FamilyRows.IsEnabled = false; FamilyRows.Opacity = 0.55;
            LockBox(TxtNeeds); LockBox(TxtOtherIncome);
            ChkPantawid.IsEnabled   = false; ChkPantawid.Opacity   = 0.55;
            ChkIndigenous.IsEnabled = false; ChkIndigenous.Opacity = 0.55;
            ChkLGBTQ.IsEnabled      = false; ChkLGBTQ.Opacity      = 0.55;
            // CmbCivilStatus, TxtMonthlyIncome, ChkEmployed/ChkSelfEmployed/ChkNotEmployed stay ENABLED
        }

        /// <summary>
        /// Opens the form for a new record, pre-seeded with the primary identity fields
        /// gathered by the PreCheckDialog.
        /// </summary>
        public RecordDialog(PreCheckResult preCheck) : this(existing: null)
        {
            if (preCheck != null)
                ApplyPreCheck(preCheck);
        }

        private void ApplyPreCheck(PreCheckResult p)
        {
            if (p == null) return;
            TxtLastName.Text   = p.LastName   ?? string.Empty;
            TxtFirstName.Text  = p.FirstName  ?? string.Empty;
            TxtMiddleName.Text = p.MiddleName ?? string.Empty;
            TxtPhilSys.Text    = p.PhilSys    ?? string.Empty;
            if (p.Birthdate.HasValue)
            {
                DpBirthdate.SelectedDate = p.Birthdate;
                UpdateAge(p.Birthdate);
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
            PnlA6.Visibility = ChkA6.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            PnlB.Visibility  = ChkB.IsChecked  == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AddFamilyRow_Click(object sender, RoutedEventArgs e)
        {
            _familyRowData.Add(MakeFamilyRow(id: null, isNew: true));
        }

        private FamilyMemberRow MakeFamilyRow(Guid? id = null, bool isNew = true)
        {
            var row = new FamilyMemberRow
            {
                Id = id,
                IsNew = isNew
            };
            row.DeleteCommand = new RelayCommand(() =>
            {
                if (!row.IsNew && row.Id.HasValue)
                {
                    if (!_removedFamilyMemberIds.Contains(row.Id.Value))
                        _removedFamilyMemberIds.Add(row.Id.Value);
                }
                _familyRowData.Remove(row);
            });
            return row;
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

            SetComboByContent(CmbSex,         r.Sex);
            SetComboByContent(CmbCivilStatus, r.CivilStatus);
            SetComboByContent(CmbStatus,      r.Status);
            SetComboByContent(CmbEducation,   r.EducationalAttainment);

            TxtPhilSys.Text        = r.PhilSysNumber   ?? string.Empty;
            TxtReligion.Text       = r.Religion         ?? string.Empty;
            TxtOccupation.Text     = r.Occupation       ?? string.Empty;
            TxtMonthlyIncome.Text  = r.MonthlyIncome    ?? string.Empty;

            ChkEmployed.IsChecked    = false;
            ChkSelfEmployed.IsChecked = false;
            ChkNotEmployed.IsChecked  = false;
            
            if (r.IsEmployed) ChkEmployed.IsChecked = true;
            else if (r.IsSelfEmployed) ChkSelfEmployed.IsChecked = true;
            else if (r.IsNotEmployed) ChkNotEmployed.IsChecked = true;

            TxtEmergencyContact.Text = r.EmergencyContactName   ?? string.Empty;
            TxtRelationship.Text     = r.EmergencyRelationship  ?? string.Empty;
            TxtEmergencyAddress.Text = r.EmergencyAddress       ?? string.Empty;
            TxtEmergencyNumber.Text  = SOLUM_UI.Services.OcrService.FormatPhoneNumber(r.EmergencyContactNumber ?? string.Empty);

            ChkA1.IsChecked = false;
            ChkA2.IsChecked = false;
            ChkA3.IsChecked = false;
            ChkA4.IsChecked = false;
            ChkA5.IsChecked = false;
            ChkA6.IsChecked = false;
            ChkA7.IsChecked = false;
            ChkB.IsChecked = false;
            ChkC.IsChecked = false;
            ChkD.IsChecked = false;
            ChkE.IsChecked = false;
            ChkF.IsChecked = false;

            if (r.CircumstanceA1) ChkA1.IsChecked = true;
            else if (r.CircumstanceA2) ChkA2.IsChecked = true;
            else if (r.CircumstanceA3) ChkA3.IsChecked = true;
            else if (r.CircumstanceA4) ChkA4.IsChecked = true;
            else if (r.CircumstanceA5) ChkA5.IsChecked = true;
            else if (r.CircumstanceA6) ChkA6.IsChecked = true;
            else if (r.CircumstanceA7) ChkA7.IsChecked = true;
            else if (r.CircumstanceB) ChkB.IsChecked = true;
            else if (r.CircumstanceC) ChkC.IsChecked = true;
            else if (r.CircumstanceD) ChkD.IsChecked = true;
            else if (r.CircumstanceE) ChkE.IsChecked = true;
            else if (r.CircumstanceF) ChkF.IsChecked = true;

            TxtA2Cause.Text = r.CircumstanceA2Cause ?? string.Empty;
            if (r.CircumstanceA2Date != DateTime.MinValue) DpA2Date.SelectedDate = r.CircumstanceA2Date;
            TxtA4Disability.Text = r.CircumstanceA4Disability ?? string.Empty;
            TxtA5Period.Text = r.CircumstanceA5Period ?? string.Empty;
            if (r.CircumstanceA6Nullity)        SetComboByContent(CmbA6Declaration, "Nullity of marriage");
            else if (r.CircumstanceA6Annulment) SetComboByContent(CmbA6Declaration, "Annulment of marriage");
            else                                CmbA6Declaration.SelectedIndex = 0;

            TxtNeeds.Text       = r.NeedsAndProblems  ?? string.Empty;
            TxtOtherIncome.Text = r.OtherIncomeSource ?? string.Empty;

            ChkPantawid.IsChecked   = r.IsPantawidBeneficiary;
            ChkIndigenous.IsChecked = r.IsIndigenous;
            ChkLGBTQ.IsChecked      = r.IsLGBT;

            if (r.FamilyMembers != null && r.FamilyMembers.Count > 0)
            {
                _familyRowData.Clear();
                _removedFamilyMemberIds.Clear();
                if (r.RemovedFamilyMemberIds != null)
                {
                    _removedFamilyMemberIds.AddRange(r.RemovedFamilyMemberIds);
                }

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

                    var row = MakeFamilyRow(fm.Id, fm.IsNew);
                    row.MemberName          = fm.MemberName;
                    row.Sex                 = fm.Sex;
                    row.Age                 = ageVal;
                    row.Birthdate           = string.IsNullOrWhiteSpace(normDob) ? fm.Birthdate : normDob;
                    row.CivilStatus         = fm.CivilStatus;
                    row.Relationship        = fm.Relationship;
                    row.EducationEmployment = fm.EducationEmployment;
                    row.Income              = fm.Income;
                    _familyRowData.Add(row);
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
            bool isEdit = _existing != null && !string.IsNullOrEmpty(_existing.Id);

            ConfirmMode mode = _isRenewal ? ConfirmMode.Renew
                             : isEdit     ? ConfirmMode.Edit
                             :              ConfirmMode.Add;

            DialogShell.Visibility = Visibility.Hidden;

            var confirm = new ConfirmDialog(
                _isRenewal ? "Confirm Renewal"  : isEdit ? "Confirm Changes"  : "Confirm New Record",
                _isRenewal ? "Review the fields being renewed." : isEdit ? "Review your changes before saving." : "Review the information below before saving.",
                _isRenewal ? "Confirm Renewal"  : isEdit ? "Confirm Changes"  : "Confirm & Save",
                fields,
                screenW: Width, screenH: Height,
                screenLeft: Left, screenTop: Top,
                mode: mode
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
                        Id                  = row.Id,
                        IsNew               = row.IsNew,
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
                Status        = GetComboValue(CmbStatus),
                LastUpdated   = DateTime.Today,

                IsNewApplicant      = !_isRenewal,
                IsRenewal           = _isRenewal,
                DateOfApplication   = DpDateOfApplication.SelectedDate ?? DateTime.Today,

                EducationalAttainment = GetComboValue(CmbEducation),
                PhilSysNumber         = TxtPhilSys.Text.Trim(),
                Religion              = TxtReligion.Text.Trim(),
                Occupation            = TxtOccupation.Text.Trim(),
                MonthlyIncome         = TxtMonthlyIncome.Text.Trim(),
                IsEmployed            = ChkEmployed.IsChecked == true && ChkEmployed.IsEnabled,
                IsSelfEmployed        = ChkSelfEmployed.IsChecked == true && ChkSelfEmployed.IsEnabled,
                IsNotEmployed         = ChkNotEmployed.IsChecked == true && ChkNotEmployed.IsEnabled,

                EmergencyContactName   = TxtEmergencyContact.Text.Trim(),
                EmergencyRelationship  = TxtRelationship.Text.Trim(),
                EmergencyAddress       = TxtEmergencyAddress.Text.Trim(),
                EmergencyContactNumber = SOLUM_UI.Services.OcrService.FormatPhoneNumber(TxtEmergencyNumber.Text.Trim()),

                CircumstanceA1             = _isRenewal ? _existing.CircumstanceA1 : (ChkA1.IsChecked == true && ChkA1.IsEnabled),
                CircumstanceA2             = _isRenewal ? _existing.CircumstanceA2 : (ChkA2.IsChecked == true && ChkA2.IsEnabled),
                CircumstanceA2Cause        = _isRenewal ? _existing.CircumstanceA2Cause        : (ChkA2.IsChecked == true ? TxtA2Cause.Text.Trim() : string.Empty),
                CircumstanceA2Date         = _isRenewal ? _existing.CircumstanceA2Date         : (ChkA2.IsChecked == true ? (DpA2Date.SelectedDate ?? DateTime.MinValue) : DateTime.MinValue),
                CircumstanceA3             = _isRenewal ? _existing.CircumstanceA3 : (ChkA3.IsChecked == true && ChkA3.IsEnabled),
                CircumstanceA4             = _isRenewal ? _existing.CircumstanceA4 : (ChkA4.IsChecked == true && ChkA4.IsEnabled),
                CircumstanceA4Disability   = _isRenewal ? _existing.CircumstanceA4Disability   : (ChkA4.IsChecked == true ? TxtA4Disability.Text.Trim() : string.Empty),
                CircumstanceA5             = _isRenewal ? _existing.CircumstanceA5 : (ChkA5.IsChecked == true && ChkA5.IsEnabled),
                CircumstanceA5Period       = _isRenewal ? _existing.CircumstanceA5Period       : (ChkA5.IsChecked == true ? TxtA5Period.Text.Trim() : string.Empty),
                CircumstanceA6             = _isRenewal ? _existing.CircumstanceA6 : (ChkA6.IsChecked == true && ChkA6.IsEnabled),
                CircumstanceA6Nullity      = _isRenewal ? _existing.CircumstanceA6Nullity      : (ChkA6.IsChecked == true && GetComboValue(CmbA6Declaration) == "Nullity of marriage"),
                CircumstanceA6Annulment    = _isRenewal ? _existing.CircumstanceA6Annulment    : (ChkA6.IsChecked == true && GetComboValue(CmbA6Declaration) == "Annulment of marriage"),
                CircumstanceA7             = _isRenewal ? _existing.CircumstanceA7 : (ChkA7.IsChecked == true && ChkA7.IsEnabled),
                CircumstanceB              = _isRenewal ? _existing.CircumstanceB  : (ChkB.IsChecked  == true && ChkB.IsEnabled),
                CircumstanceBStayAbroad    = _isRenewal ? _existing.CircumstanceBStayAbroad    : (ChkB.IsChecked  == true ? TxtBStayAbroad.Text.Trim() : string.Empty),
                CircumstanceC              = _isRenewal ? _existing.CircumstanceC  : (ChkC.IsChecked  == true && ChkC.IsEnabled),
                CircumstanceD              = _isRenewal ? _existing.CircumstanceD  : (ChkD.IsChecked  == true && ChkD.IsEnabled),
                CircumstanceE              = _isRenewal ? _existing.CircumstanceE  : (ChkE.IsChecked  == true && ChkE.IsEnabled),
                CircumstanceF              = _isRenewal ? _existing.CircumstanceF  : (ChkF.IsChecked  == true && ChkF.IsEnabled),


                FamilyMembers          = members,
                RemovedFamilyMemberIds = new List<Guid>(_removedFamilyMemberIds),
                NeedsAndProblems       = _isRenewal ? (_existing?.NeedsAndProblems ?? string.Empty) : TxtNeeds.Text.Trim(),
                OtherIncomeSource      = _isRenewal ? (_existing?.OtherIncomeSource ?? string.Empty) : TxtOtherIncome.Text.Trim(),
                IsPantawidBeneficiary  = _isRenewal ? _existing.IsPantawidBeneficiary : (ChkPantawid.IsChecked == true),
                IsIndigenous           = _isRenewal ? _existing.IsIndigenous           : (ChkIndigenous.IsChecked == true),
                IsLGBT                 = _isRenewal ? _existing.IsLGBT                 : (ChkLGBTQ.IsChecked == true),
                Children          = members.Count
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

            if (r.CircumstanceA1)
                Add(sec, "Problem Presented", "A1. Gives birth as a result of rape", null);

            if (r.CircumstanceA2)
            {
                Add(sec, "Problem Presented", "A2. Death of Spouse", null);
                Add(sec, "   Cause of death",
                    F(r.CircumstanceA2Cause), null);
                Add(sec, "   Date of death",
                    r.CircumstanceA2Date != DateTime.MinValue
                        ? r.CircumstanceA2Date.ToString("MMMM d, yyyy") : "—", null);
            }

            if (r.CircumstanceA3)
                Add(sec, "Problem Presented", "A3. Detention of Spouse", null);

            if (r.CircumstanceA4)
            {
                Add(sec, "Problem Presented", "A4. Physical & Mental Incapacity of Spouse", null);
                Add(sec, "   Type of disability", F(r.CircumstanceA4Disability), null);
            }

            if (r.CircumstanceA5)
            {
                Add(sec, "Problem Presented", "A5. Legal or de facto Separation", null);
                Add(sec, "   Period of separation", F(r.CircumstanceA5Period), null);
            }

            if (r.CircumstanceA6)
            {
                string a6sub = r.CircumstanceA6Nullity ? "Nullity of marriage"
                             : r.CircumstanceA6Annulment ? "Annulment of marriage"
                             : "—";
                Add(sec, "Problem Presented", "A6. Declaration of: " + a6sub, null);
            }

            if (r.CircumstanceA7)
                Add(sec, "Problem Presented", "A7. Abandonment of spouse for at least 6 months", null);

            if (r.CircumstanceB)
            {
                Add(sec, "Problem Presented", "B. Spouse or any family member of an OFW", null);
                Add(sec, "   Length of stay abroad", F(r.CircumstanceBStayAbroad), null);
            }

            if (r.CircumstanceC) Add(sec, "Problem Presented", "C. Unmarried Mother or Father", null);
            if (r.CircumstanceD) Add(sec, "Problem Presented", "D. Legal Guardian / Adoptive / Foster Parent", null);
            if (r.CircumstanceE) Add(sec, "Problem Presented", "E. Relative within the 4th civil degree", null);
            if (r.CircumstanceF) Add(sec, "Problem Presented", "F. Pregnant Woman", null);

            if (!r.CircumstanceA1 && !r.CircumstanceA2 && !r.CircumstanceA3 && !r.CircumstanceA4 &&
                !r.CircumstanceA5 && !r.CircumstanceA6 && !r.CircumstanceA7 && !r.CircumstanceB  &&
                !r.CircumstanceC  && !r.CircumstanceD  && !r.CircumstanceE  && !r.CircumstanceF)
                Add(sec, "Problem Presented", "None selected", null);

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

            sec = "Additional Classifications";
            Add(sec, "Pantawid Beneficiary", r.IsPantawidBeneficiary ? "✓ Yes" : "No", _existing != null ? (_existing.IsPantawidBeneficiary ? "✓ Yes" : "No") : null);
            Add(sec, "Indigenous Person",    r.IsIndigenous ? "✓ Yes" : "No",    _existing != null ? (_existing.IsIndigenous ? "✓ Yes" : "No") : null);
            Add(sec, "LGBTQ+",               r.IsLGBT ? "✓ Yes" : "No",               _existing != null ? (_existing.IsLGBT ? "✓ Yes" : "No") : null);

            sec = "Application Status";
            Add(sec, "Status", r.Status, _existing?.Status);

            return list;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
