using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services.Api;
using SOLUM_UI.Views.Dialogs;

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
            set { _memberName = value?.ToUpper() ?? string.Empty; N(nameof(MemberName)); }
        }

        public string Sex
        {
            get => _sex;
            set
            {
                string s = value?.Trim() ?? string.Empty;
                string matched = SOLUM_UI.Services.OcrService.FuzzyMatchSex(s);
                if (!string.IsNullOrEmpty(matched))
                    _sex = matched;
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
                    _birthdate = value.Value.ToString("dd/MM/yyyy");
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
                string matched = SOLUM_UI.Services.OcrService.FuzzyMatchCivilStatus(s);
                if (!string.IsNullOrEmpty(matched))
                    _civilStatus = matched;
                else if (!string.IsNullOrWhiteSpace(s))
                    _civilStatus = char.ToUpper(s[0]) + (s.Length > 1 ? s.Substring(1) : "");
                else
                    _civilStatus = string.Empty;
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
        public string EducationEmployment { get => _educationEmployment; set { _educationEmployment = value?.ToUpper() ?? string.Empty; N(nameof(EducationEmployment)); } }

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
        private bool _isPopulating = false;
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

            Loaded += (s, e) => SuppressDatePickerWatermarks();

            if (_existing != null)
            {
                FormSubtitle.Text = string.IsNullOrEmpty(_existing.Id)
                    ? "Add New Record (OCR Pre-Populated — Please Verify)"
                    : "Edit Record — " + _existing.Id;
                PopulateFields(_existing);
                // Amber stripe for edit mode (only when editing an existing saved record)
                if (!string.IsNullOrEmpty(_existing.Id))
                    Loaded += (s, e) => { ApplyEditStyle(); UpdateSaveButtonState(); };
            }
            else
            {
                FormSubtitle.Text = "Add New Record";
                DpDateOfApplication.SelectedDate = DateTime.Today;
                CmbEmploymentStatus.SelectedIndex = 0; // Default to empty selection
                // Show add-mode stripe when Loaded
                Loaded += (s, e) => { ApplyModeStyle(isRenewal: false); UpdateSaveButtonState(); };
            }

            // Routed event handlers to react to any input change and update save button state
            AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent, new TextChangedEventHandler((s, e) => UpdateSaveButtonState()));
            AddHandler(System.Windows.Controls.Primitives.Selector.SelectionChangedEvent, new SelectionChangedEventHandler((s, e) => UpdateSaveButtonState()));
            AddHandler(DatePicker.SelectedDateChangedEvent, new EventHandler<SelectionChangedEventArgs>((s, e) => UpdateSaveButtonState()));
            AddHandler(System.Windows.Controls.Primitives.ToggleButton.CheckedEvent, new RoutedEventHandler((s, e) => UpdateSaveButtonState()));
            AddHandler(System.Windows.Controls.Primitives.ToggleButton.UncheckedEvent, new RoutedEventHandler((s, e) => UpdateSaveButtonState()));
            _familyRowData.CollectionChanged += (s, e) => UpdateSaveButtonState();
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
                
                // Set DialogShell border to green for renewal mode
                DialogShell.BorderBrush   = new SolidColorBrush(Color.FromRgb(0xC8, 0xE6, 0xC9));
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
                
                // Set DialogShell border to green for add mode
                DialogShell.BorderBrush   = new SolidColorBrush(Color.FromRgb(0xC8, 0xE6, 0xC9));
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
            
            // Set DialogShell border to yellow for edit mode
            DialogShell.BorderBrush   = new SolidColorBrush(Color.FromRgb(0xFF, 0xD5, 0x4F));

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
            LockBox(TxtLastName); LockBox(TxtFirstName); LockBox(TxtMiddleName);
            LockCombo(CmbExtension);
            LockDate(DpBirthdate); LockBox(TxtPlaceOfBirth); LockCombo(CmbSex); LockBox(TxtPhilSys);

            // Background (Education and Religion are fixed for renewal; Occupation, Income, Employment remain editable)
            LockCombo(CmbEducation); LockBox(TxtReligion);

            // Address & contact
            LockBox(TxtAddress); LockCombo(CmbBarangay); LockBox(TxtContact);

            // Emergency contact
            LockBox(TxtEmergencyContact); LockCombo(CmbRelationship);
            LockBox(TxtEmergencyAddress); LockBox(TxtEmergencyNumber);

            // Date of application
            LockDate(DpDateOfApplication);

            // Additional classifications are fixed for renewal
            ChkPantawid.IsEnabled   = false; ChkPantawid.Opacity   = 0.55;
            ChkIndigenous.IsEnabled = false; ChkIndigenous.Opacity = 0.55;
            ChkLGBTQ.IsEnabled      = false; ChkLGBTQ.Opacity      = 0.55;

            // Mutable fields remaining ENABLED for renewal re-assessment:
            // - CmbCivilStatus
            // - TxtOccupation, TxtMonthlyIncome, CmbEmploymentStatus
            // - Circumstances (ChkA1..ChkF, details)
            // - FamilyRows (+ Add Row, delete row, edit row)
            // - TxtNeeds, TxtOtherIncome
        }

        /// <summary>
        /// Suppresses the default "Select a date" watermark in DatePickerTextBox controls
        /// used for main form fields (DateOfApplication, Birthdate) to avoid overlapping 
        /// with our custom MM/DD/YYYY placeholder.
        /// Family composition DatePickers use standard FormDate styling with no overlay.
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

            bool discard = ActionConfirmDialog.Show(
                title: "Unsaved Changes",
                message: "You have unsaved information in this form. Are you sure you want to discard your changes and close?",
                confirmText: "Discard Changes",
                cancelText: "Keep Editing",
                theme: ConfirmThemeType.Warning,
                owner: this);

            if (!discard)
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
            if (CmbBarangay.SelectedItem != null)               return true;
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
            row.PropertyChanged += (s, e) => UpdateSaveButtonState();
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
            try
            {
                _isPopulating = true;
                TxtLastName.Text     = r.Surname       ?? string.Empty;
            TxtFirstName.Text    = r.FirstName     ?? string.Empty;
            TxtMiddleName.Text   = r.MiddleName    ?? string.Empty;
            SetComboByContent(CmbExtension, r.ExtensionName);
            TxtPlaceOfBirth.Text = r.PlaceOfBirth  ?? string.Empty;
            TxtAddress.Text      = r.Address       ?? string.Empty;
            TxtContact.Text      = SOLUM_UI.Services.OcrService.FormatPhoneNumber(r.ContactNumber ?? string.Empty);

            if (r.DateOfBirth != DateTime.MinValue) DpBirthdate.SelectedDate = r.DateOfBirth;
            DpDateOfApplication.SelectedDate = r.DateOfApplication != DateTime.MinValue ? r.DateOfApplication : DateTime.Today;

            SetComboByContent(CmbSex,         r.Sex);
            SetComboByContent(CmbCivilStatus, r.CivilStatus);
            SetComboByContent(CmbEducation,   r.EducationalAttainment);
            SetComboByContent(CmbBarangay,    r.Barangay);

            TxtPhilSys.Text        = r.PhilSysNumber   ?? string.Empty;
            TxtReligion.Text       = r.Religion         ?? string.Empty;
            TxtOccupation.Text     = r.Occupation       ?? string.Empty;
            TxtMonthlyIncome.Text  = r.MonthlyIncome    ?? string.Empty;

            // Set Employment Status dropdown
            if (r.IsEmployed) SetComboByContent(CmbEmploymentStatus, "Employed");
            else if (r.IsSelfEmployed) SetComboByContent(CmbEmploymentStatus, "Self-Employed");
            else if (r.IsNotEmployed) SetComboByContent(CmbEmploymentStatus, "Not Employed");
            else CmbEmploymentStatus.SelectedIndex = 0;

            TxtEmergencyContact.Text = r.EmergencyContactName   ?? string.Empty;
            SetComboByContent(CmbRelationship, r.EmergencyRelationship);
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

            TxtBStayAbroad.Text = r.CircumstanceBStayAbroad ?? string.Empty;

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
        finally
        {
            _isPopulating = false;
        }
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

            // Specific matching for Sex if combo is CmbSex
            if (combo == CmbSex)
            {
                string matchedSex = SOLUM_UI.Services.OcrService.FuzzyMatchSex(val);
                if (!string.IsNullOrEmpty(matchedSex))
                {
                    foreach (ComboBoxItem item in combo.Items)
                    {
                        if (string.Equals(item.Content?.ToString(), matchedSex, StringComparison.OrdinalIgnoreCase))
                        {
                            combo.SelectedItem = item;
                            return;
                        }
                    }
                }
            }

            // Specific matching for Civil Status if combo is CmbCivilStatus
            if (combo == CmbCivilStatus)
            {
                string matchedCiv = SOLUM_UI.Services.OcrService.FuzzyMatchCivilStatus(val);
                if (!string.IsNullOrEmpty(matchedCiv))
                {
                    foreach (ComboBoxItem item in combo.Items)
                    {
                        if (string.Equals(item.Content?.ToString(), matchedCiv, StringComparison.OrdinalIgnoreCase))
                        {
                            combo.SelectedItem = item;
                            return;
                        }
                    }
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

            var fallbackItem = new ComboBoxItem { Content = val };
            combo.Items.Add(fallbackItem);
            combo.SelectedItem = fallbackItem;
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
                ErrBirthdate.Visibility = Visibility.Collapsed;
                return;
            }
            DateTime today = DateTime.Today;
            int age = today.Year - dob.Value.Year;
            if (dob.Value.Date > today.AddYears(-age)) age--;
            
            // Validate age - cannot exceed 120
            if (age > 120)
            {
                TxtAge.Text = string.Empty;
                ErrBirthdate.Text = "Age cannot exceed 120 years.";
                ErrBirthdate.Visibility = Visibility.Visible;
                DpBirthdate.SelectedDate = null;
                MessageBox.Show(
                    "Invalid date of birth.\n\nAge cannot exceed 120 years.\n\nPlease select a valid birthdate.",
                    "Invalid Age",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            
            // Valid age - clear any errors
            TxtAge.Text = age.ToString();
            ErrBirthdate.Visibility = Visibility.Collapsed;
        }

        private void DpBirthdate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAge(DpBirthdate.SelectedDate);
            ClearDateError(DpBirthdate, ErrBirthdate);
        }

        private void DatePicker_DateValidationError(object sender, DatePickerDateValidationErrorEventArgs e)
        {
            if (sender is DatePicker dp && !string.IsNullOrWhiteSpace(e.Text))
            {
                if (SOLUM_UI.Services.OcrService.TryParseOcrDate(e.Text, out DateTime dt))
                {
                    dp.SelectedDate = dt;
                    e.ThrowException = false;
                }
            }
        }

        private void DatePicker_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is DatePicker dp)
            {
                var tb = dp.Template?.FindName("PART_TextBox", dp) as System.Windows.Controls.Primitives.DatePickerTextBox;
                if (tb != null && !string.IsNullOrWhiteSpace(tb.Text))
                {
                    if (SOLUM_UI.Services.OcrService.TryParseOcrDate(tb.Text, out DateTime dt))
                    {
                        if (dp.SelectedDate != dt)
                        {
                            dp.SelectedDate = dt;
                        }
                        tb.Text = dt.ToString("dd/MM/yyyy");
                    }
                }
            }
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

        private void ToUpperBox(TextBox tb)
        {
            if (_isPopulating || tb == null) return;
            if (tb.Text != tb.Text.ToUpper())
            {
                int caret = tb.SelectionStart;
                tb.Text = tb.Text.ToUpper();
                tb.SelectionStart = Math.Min(caret + 1, tb.Text.Length);
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

        private void TxtLastName_TextChanged(object sender, TextChangedEventArgs e)       { ToUpperBox(TxtLastName); ClearFieldError(TxtLastName,     ErrLastName); }
        private void TxtFirstName_TextChanged(object sender, TextChangedEventArgs e)      { ToUpperBox(TxtFirstName); ClearFieldError(TxtFirstName,    ErrFirstName); }
        private void TxtMiddleName_TextChanged(object sender, TextChangedEventArgs e)     { ToUpperBox(TxtMiddleName); }

        private SoloParentRecord _foundRecord;
        private bool _isFormattingPhilSys = false;
        private bool _duplicateCheckPassed = false;

        private void CmbExtension_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void CmbBarangay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearFieldError(CmbBarangay, ErrBarangay);
        }

        private void PhilSys_PreviewKeyDown(object sender, KeyEventArgs e)
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

        public static string FormatPhilSysNumber(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "—";
            string digits = Regex.Replace(raw, @"\D", "");
            if (string.IsNullOrEmpty(digits)) return "—";
            if (digits.Length > 16) digits = digits.Substring(0, 16);

            if (digits.Length > 12)
                return $"{digits.Substring(0, 4)}-{digits.Substring(4, 4)}-{digits.Substring(8, 4)}-{digits.Substring(12)}";
            if (digits.Length > 8)
                return $"{digits.Substring(0, 4)}-{digits.Substring(4, 4)}-{digits.Substring(8)}";
            if (digits.Length > 4)
                return $"{digits.Substring(0, 4)}-{digits.Substring(4)}";
            return digits;
        }

        private void FormatPhilSysBox(TextBox tb)
        {
            if (tb == null || _isFormattingPhilSys) return;
            _isFormattingPhilSys = true;
            try
            {
                string raw = tb.Text ?? "";
                string formatted = FormatPhilSysNumber(raw);
                if (formatted == "—") formatted = "";

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

        private void TxtAddress_TextChanged(object sender, TextChangedEventArgs e)        { ToUpperBox(TxtAddress); ClearFieldError(TxtAddress,      ErrAddress); }
        private void TxtContact_TextChanged(object sender, TextChangedEventArgs e)
        {
            FormatPhoneBox(TxtContact);
            ClearFieldError(TxtContact, ErrContact);
        }

        private void TxtEmergencyNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            FormatPhoneBox(TxtEmergencyNumber);
        }
        private void TxtPlaceOfBirth_TextChanged(object sender, TextChangedEventArgs e)   { ToUpperBox(TxtPlaceOfBirth); ClearFieldError(TxtPlaceOfBirth, ErrPlaceOfBirth); }
        private void TxtPhilSys_TextChanged(object sender, TextChangedEventArgs e)
        {
            FormatPhilSysBox(TxtPhilSys);
            ClearFieldError(TxtPhilSys, ErrPhilSys);
            _duplicateCheckPassed = false;
            if (DuplicateBannerPanel != null) DuplicateBannerPanel.Visibility = Visibility.Collapsed;
        }
        private void TxtReligion_TextChanged(object sender, TextChangedEventArgs e)       { ToUpperBox(TxtReligion); }
        private void TxtOccupation_TextChanged(object sender, TextChangedEventArgs e)     { ToUpperBox(TxtOccupation); }
        private void TxtEmergencyContact_TextChanged(object sender, TextChangedEventArgs e) { ToUpperBox(TxtEmergencyContact); }
        private void TxtEmergencyAddress_TextChanged(object sender, TextChangedEventArgs e) { ToUpperBox(TxtEmergencyAddress); }

        private void ChkSameAsApplicant_Checked(object sender, RoutedEventArgs e)
        {
            // Copy applicant address to emergency contact address
            if (TxtAddress != null && TxtEmergencyAddress != null)
            {
                TxtEmergencyAddress.Text = TxtAddress.Text;
                TxtEmergencyAddress.IsEnabled = false;
            }
        }

        private void ChkSameAsApplicant_Unchecked(object sender, RoutedEventArgs e)
        {
            // Enable the emergency contact address field for editing
            if (TxtEmergencyAddress != null)
            {
                TxtEmergencyAddress.IsEnabled = true;
            }
        }

        private void TxtA2Cause_TextChanged(object sender, TextChangedEventArgs e)        { }
        private void TxtA4Disability_TextChanged(object sender, TextChangedEventArgs e)   { }
        private void TxtA5Period_TextChanged(object sender, TextChangedEventArgs e)       { }
        private void TxtBStayAbroad_TextChanged(object sender, TextChangedEventArgs e)    { }
        private void TxtNeeds_TextChanged(object sender, TextChangedEventArgs e)          { }
        private void TxtOtherIncome_TextChanged(object sender, TextChangedEventArgs e)    { }
        private void CmbSex_SelectionChanged(object sender, SelectionChangedEventArgs e)         => ClearFieldError(CmbSex,         ErrSex);
        private void CmbCivilStatus_SelectionChanged(object sender, SelectionChangedEventArgs e) => ClearFieldError(CmbCivilStatus, ErrCivilStatus);

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

            if (!DpBirthdate.SelectedDate.HasValue)
            { MarkDateError(DpBirthdate, ErrBirthdate, "Required."); missing.Add("Birthdate"); ok = false; }
            else if (DpBirthdate.SelectedDate.Value.Date > DateTime.Today)
            { MarkDateError(DpBirthdate, ErrBirthdate, "Cannot be in the future."); missing.Add("Birthdate (future date)"); ok = false; }
            else if (DpBirthdate.SelectedDate.Value.Date > DateTime.Today.AddYears(-15))
            { MarkDateError(DpBirthdate, ErrBirthdate, "Must be at least 15 years old."); missing.Add("Birthdate (must be 15+)"); ok = false; }

            string philsysRaw = Regex.Replace(TxtPhilSys.Text ?? "", @"\D", "");
            if (!string.IsNullOrWhiteSpace(philsysRaw) && philsysRaw.Length != 16)
            {
                MarkError(TxtPhilSys, ErrPhilSys, "PhilSys must be 16 digits (XXXX-XXXX-XXXX-XXXX).");
                missing.Add("PhilSys Card Number (16 digits)");
                ok = false;
            }

            if (!ok)
                SetValidationMessage(missing);

            return ok;
        }

        private bool ValidateStep2()
        {
            bool ok = true;
            var missing = new List<string>();

            if (CmbCivilStatus.SelectedItem == null)
            { MarkError(CmbCivilStatus, ErrCivilStatus, "Required."); missing.Add("Civil Status"); ok = false; }

            if (CmbSex.SelectedItem == null)
            { MarkError(CmbSex, ErrSex, "Required."); missing.Add("Sex"); ok = false; }

            if (string.IsNullOrWhiteSpace(TxtPlaceOfBirth.Text))
            { MarkError(TxtPlaceOfBirth, ErrPlaceOfBirth, "Required."); missing.Add("Birthplace"); ok = false; }

            if (string.IsNullOrWhiteSpace(TxtAddress.Text))
            { MarkError(TxtAddress, ErrAddress, "Required."); missing.Add("Address"); ok = false; }

            string brgy = GetComboValue(CmbBarangay);
            if (string.IsNullOrWhiteSpace(brgy))
            {
                MarkError(CmbBarangay, ErrBarangay, "Required.");
                missing.Add("Barangay");
                ok = false;
            }

            string contactDigits = Regex.Replace(TxtContact.Text ?? "", @"\D", "");
            if (string.IsNullOrWhiteSpace(contactDigits))
            { MarkError(TxtContact, ErrContact, "Required."); missing.Add("Contact Number"); ok = false; }
            else if (contactDigits.Length != 11)
            { MarkError(TxtContact, ErrContact, "Must be 11 digits."); missing.Add("Contact Number (11 digits)"); ok = false; }

            if (!ok)
                SetValidationMessage(missing);

            return ok;
        }

        private bool ValidateFields()
        {
            bool ok = ValidateStep1() && ValidateStep2();
            var missing = new List<string>();

            // Family composition: every named row must be fully filled
            int rowNum = 0;
            foreach (var item in _familyRowData)
            {
                var row = item as FamilyMemberRow;
                if (row == null || string.IsNullOrWhiteSpace(row.MemberName)) continue;
                rowNum++;

                var rowMissing = new List<string>();
                if (string.IsNullOrWhiteSpace(row.Sex))                 rowMissing.Add("Sex");
                if (!row.BirthdateDate.HasValue)                         rowMissing.Add("Birthdate");
                if (string.IsNullOrWhiteSpace(row.CivilStatus))         rowMissing.Add("Civil Status");
                if (string.IsNullOrWhiteSpace(row.Relationship))        rowMissing.Add("Relationship");
                if (string.IsNullOrWhiteSpace(row.EducationEmployment)) rowMissing.Add("Education/Employment");
                if (string.IsNullOrWhiteSpace(row.Income))              rowMissing.Add("Income");

                if (rowMissing.Count > 0)
                {
                    missing.Add($"Family member #{rowNum} ({row.MemberName}) — missing: {string.Join(", ", rowMissing)}");
                    ok = false;
                }
            }

            if (!ok && missing.Count > 0)
                SetValidationMessage(missing);

            return ok;
        }

        private void SetValidationMessage(List<string> missing)
        {
            if (missing.Count == 0) return;
            var sb = new StringBuilder("Missing or invalid required fields: ");
            sb.Append(string.Join(", ", missing));
            sb.Append(".");
            ValidationMessage.Text       = sb.ToString();
            ValidationMessage.Visibility = Visibility.Visible;
        }

        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;

            if (_currentStep == 1)
            {
                if (!ValidateStep1()) return;

                // If editing an existing record or duplicate already confirmed, continue to Step 2
                if (_existing != null || _duplicateCheckPassed)
                {
                    GoToStep(2);
                    return;
                }

                // Run Duplicate Pre-Check on Step 1
                BtnNext.IsEnabled = false;
                try
                {
                    string fName = TxtFirstName.Text.Trim();
                    string lName = TxtLastName.Text.Trim();
                    string philsys = TxtPhilSys.Text.Trim();

                    var matches = new List<SoloParentRecord>();

                    // 1. Check PhilSys if provided
                    if (!string.IsNullOrWhiteSpace(philsys))
                    {
                        var philsysResp = await SoloParentApiService.Instance.GetSoloParentsAsync(new GetSoloParentRequest { PhilsysId = philsys, PageSize = 5 });
                        if (philsysResp.Succeeded && philsysResp.Data?.Items != null)
                        {
                            foreach (var item in philsysResp.Data.Items)
                            {
                                if (!matches.Any(m => m.Id == item.Id.ToString()))
                                {
                                    var detailResp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(item.Id);
                                    if (detailResp.Succeeded && detailResp.Data != null)
                                    {
                                        matches.Add(SoloParentApiService.Instance.MapToRecord(detailResp.Data));
                                    }
                                }
                            }
                        }
                    }

                    // 2. Check First Name and Last Name
                    if (!string.IsNullOrWhiteSpace(fName) && !string.IsNullOrWhiteSpace(lName))
                    {
                        var nameResp = await SoloParentApiService.Instance.GetSoloParentsAsync(new GetSoloParentRequest { FirstName = fName, LastName = lName, PageSize = 5 });
                        if (nameResp.Succeeded && nameResp.Data?.Items != null)
                        {
                            foreach (var item in nameResp.Data.Items)
                            {
                                if (!matches.Any(m => m.Id == item.Id.ToString()))
                                {
                                    var detailResp = await SoloParentApiService.Instance.GetSoloParentByIdAsync(item.Id);
                                    if (detailResp.Succeeded && detailResp.Data != null)
                                    {
                                        matches.Add(SoloParentApiService.Instance.MapToRecord(detailResp.Data));
                                    }
                                }
                            }
                        }
                    }

                    if (matches.Count > 0)
                    {
                        _foundRecord = matches[0];
                        DuplicateBannerTitle.Text = $"Duplicate Found ({matches.Count} matching record{(matches.Count > 1 ? "s" : "")})";
                        DuplicateBannerText.Text = $"An existing record exists for '{_foundRecord.Name}'" +
                            (!string.IsNullOrWhiteSpace(_foundRecord.PhilSysNumber) ? $" with PhilSys ID {_foundRecord.PhilSysNumber}." : ".") +
                            " You can view the existing record or continue adding this record.";
                        DuplicateBannerPanel.Visibility = Visibility.Visible;
                        return;
                    }
                }
                catch
                {
                    // If network/API check fails, allow user to proceed
                }
                finally
                {
                    BtnNext.IsEnabled = true;
                }

                GoToStep(2);
            }
            else if (_currentStep == 2)
            {
                if (!ValidateStep2()) return;
                GoToStep(3);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
            if (_currentStep == 3)
            {
                GoToStep(2);
            }
            else if (_currentStep == 2)
            {
                GoToStep(1);
            }
        }

        private void ViewExistingRecord_Click(object sender, RoutedEventArgs e)
        {
            if (_foundRecord != null)
            {
                var viewer = new SOLUM_UI.Views.Dialogs.RecordViewDialog(_foundRecord) { Owner = this };
                viewer.ShowDialog();
            }
        }

        private void ContinueAnyway_Click(object sender, RoutedEventArgs e)
        {
            _duplicateCheckPassed = true;
            DuplicateBannerPanel.Visibility = Visibility.Collapsed;
            GoToStep(2);
        }

        private void GoToStep(int step)
        {
            _currentStep = step;

            ScrollStep1.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
            ScrollStep2.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
            ScrollStep3.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;

            BtnBack.Visibility = step > 1 ? Visibility.Visible : Visibility.Collapsed;
            BtnNext.Visibility = step < 3 ? Visibility.Visible : Visibility.Collapsed;
            BtnSave.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;

            if (step == 1)
            {
                FormStepHint.Text = "Step 1 of 3 — Primary Information";
                Step1Dot.Background = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));
                Step1DotLabel.Foreground = new SolidColorBrush(Colors.White);
                Step1Title.Foreground = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));

                Step2Dot.Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
                Step2DotLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
                Step2Title.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));

                Step3Dot.Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
                Step3DotLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
                Step3Title.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
            }
            else if (step == 2)
            {
                FormStepHint.Text = "Step 2 of 3 — Secondary Information";
                Step1Dot.Background = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));
                Step1DotLabel.Foreground = new SolidColorBrush(Colors.White);
                Step1Title.Foreground = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));

                Step2Dot.Background = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));
                Step2DotLabel.Foreground = new SolidColorBrush(Colors.White);
                Step2Title.Foreground = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));

                Step3Dot.Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
                Step3DotLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
                Step3Title.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
            }
            else
            {
                FormStepHint.Text = "Step 3 of 3 — Circumstances & Family Composition";
                Step1Dot.Background = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));
                Step1DotLabel.Foreground = new SolidColorBrush(Colors.White);
                Step1Title.Foreground = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));

                Step2Dot.Background = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));
                Step2DotLabel.Foreground = new SolidColorBrush(Colors.White);
                Step2Title.Foreground = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));

                Step3Dot.Background = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));
                Step3DotLabel.Foreground = new SolidColorBrush(Colors.White);
                Step3Title.Foreground = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));
            }

            UpdateSaveButtonState();
        }

        private void UpdateSaveButtonState()
        {
            if (_isPopulating || BtnSave == null) return;
            bool isEdit = _existing != null && !string.IsNullOrEmpty(_existing.Id);
            if (!isEdit)
            {
                BtnSave.IsEnabled = true;
                BtnSave.Opacity = 1.0;
                BtnSave.ToolTip = null;
                return;
            }

            bool hasChanges = HasAnyChanges();
            BtnSave.IsEnabled = hasChanges;
            BtnSave.Opacity = hasChanges ? 1.0 : 0.45;
            BtnSave.ToolTip = hasChanges ? null : "No changes have been made to this record.";

            if (_currentStep == 3)
            {
                if (!hasChanges)
                {
                    ValidationMessage.Text = "No changes have been made to this record.";
                    ValidationMessage.Visibility = Visibility.Visible;
                }
                else if (ValidationMessage.Text == "No changes have been made to this record.")
                {
                    ValidationMessage.Text = string.Empty;
                    ValidationMessage.Visibility = Visibility.Collapsed;
                }
            }
        }

        private bool HasAnyChanges()
        {
            bool isEdit = _existing != null && !string.IsNullOrEmpty(_existing.Id);
            if (!isEdit) return true;

            if (_removedFamilyMemberIds != null && _removedFamilyMemberIds.Count > 0)
                return true;

            try
            {
                var preview = BuildRecord();
                var fields  = BuildConfirmFields(preview);
                return fields != null && fields.Any(f => f.IsChanged);
            }
            catch
            {
                return true;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
            bool isEdit = _existing != null && !string.IsNullOrEmpty(_existing.Id);
            if (isEdit && !HasAnyChanges())
            {
                ValidationMessage.Text = "No changes have been made to this record.";
                ValidationMessage.Visibility = Visibility.Visible;
                return;
            }

            if (!ValidateFields()) return;

            var preview = BuildRecord();
            var fields  = BuildConfirmFields(preview);

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
            string ext     = GetComboValue(CmbExtension);
            string brgy    = GetComboValue(CmbBarangay);
            bool isEdit    = _existing != null && !string.IsNullOrEmpty(_existing.Id);

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
                ExtensionName = ext,
                Name          = surname + ", " + first + (string.IsNullOrEmpty(middle) ? "" : " " + middle) + (string.IsNullOrEmpty(ext) ? "" : " " + ext),
                DateOfBirth   = DpBirthdate.SelectedDate ?? (_existing != null ? _existing.DateOfBirth : DateTime.MinValue),
                PlaceOfBirth  = TxtPlaceOfBirth.Text.Trim(),
                Sex           = GetComboValue(CmbSex),
                CivilStatus   = GetComboValue(CmbCivilStatus),
                Citizenship   = string.Empty,
                BloodType     = string.Empty,
                Height        = string.Empty,
                Weight        = string.Empty,
                Address       = TxtAddress.Text.Trim(),
                ContactNumber = SOLUM_UI.Services.OcrService.FormatPhoneNumber(TxtContact.Text.Trim()),
                Barangay      = brgy,
                LastUpdated   = DateTime.Today,

                IsNewApplicant      = isEdit ? (_existing?.IsNewApplicant ?? true) : !_isRenewal,
                IsRenewal           = isEdit ? (_existing?.IsRenewal ?? false) : _isRenewal,
                DateOfApplication   = DpDateOfApplication.SelectedDate ?? DateTime.Today,

                EducationalAttainment = GetComboValue(CmbEducation),
                PhilSysNumber         = TxtPhilSys.Text.Trim(),
                Religion              = TxtReligion.Text.Trim(),
                Occupation            = TxtOccupation.Text.Trim(),
                MonthlyIncome         = TxtMonthlyIncome.Text.Trim(),
                IsEmployed            = GetComboValue(CmbEmploymentStatus) == "Employed",
                IsSelfEmployed        = GetComboValue(CmbEmploymentStatus) == "Self-Employed",
                IsNotEmployed         = GetComboValue(CmbEmploymentStatus) == "Not Employed",

                EmergencyContactName   = TxtEmergencyContact.Text.Trim(),
                EmergencyRelationship  = GetComboValue(CmbRelationship),
                EmergencyAddress       = TxtEmergencyAddress.Text.Trim(),
                EmergencyContactNumber = SOLUM_UI.Services.OcrService.FormatPhoneNumber(TxtEmergencyNumber.Text.Trim()),

                CircumstanceA1             = ChkA1.IsChecked == true,
                CircumstanceA2             = ChkA2.IsChecked == true,
                CircumstanceA2Cause        = ChkA2.IsChecked == true ? TxtA2Cause.Text.Trim() : string.Empty,
                CircumstanceA2Date         = ChkA2.IsChecked == true ? (DpA2Date.SelectedDate ?? DateTime.MinValue) : DateTime.MinValue,
                CircumstanceA3             = ChkA3.IsChecked == true,
                CircumstanceA4             = ChkA4.IsChecked == true,
                CircumstanceA4Disability   = ChkA4.IsChecked == true ? TxtA4Disability.Text.Trim() : string.Empty,
                CircumstanceA5             = ChkA5.IsChecked == true,
                CircumstanceA5Period       = ChkA5.IsChecked == true ? TxtA5Period.Text.Trim() : string.Empty,
                CircumstanceA6             = ChkA6.IsChecked == true,
                CircumstanceA6Nullity      = ChkA6.IsChecked == true && GetComboValue(CmbA6Declaration) == "Nullity of marriage",
                CircumstanceA6Annulment    = ChkA6.IsChecked == true && GetComboValue(CmbA6Declaration) == "Annulment of marriage",
                CircumstanceA7             = ChkA7.IsChecked == true,
                CircumstanceB              = ChkB.IsChecked == true,
                CircumstanceBStayAbroad    = ChkB.IsChecked == true ? TxtBStayAbroad.Text.Trim() : string.Empty,
                CircumstanceC              = ChkC.IsChecked == true,
                CircumstanceD              = ChkD.IsChecked == true,
                CircumstanceE              = ChkE.IsChecked == true,
                CircumstanceF              = ChkF.IsChecked == true,

                FamilyMembers          = members,
                RemovedFamilyMemberIds = new List<Guid>(_removedFamilyMemberIds),
                ProblemPresented       = _existing?.ProblemPresented ?? string.Empty,
                NeedsAndProblems       = TxtNeeds.Text.Trim(),
                OtherIncomeSource      = TxtOtherIncome.Text.Trim(),
                IsPantawidBeneficiary  = _isRenewal ? _existing.IsPantawidBeneficiary : (ChkPantawid.IsChecked == true),
                IsIndigenous           = _isRenewal ? _existing.IsIndigenous           : (ChkIndigenous.IsChecked == true),
                IsLGBT                 = _isRenewal ? _existing.IsLGBT                 : (ChkLGBTQ.IsChecked == true),
                Children          = members.Count
            };
        }

        private List<ConfirmField> BuildConfirmFields(SoloParentRecord r)
        {
            bool isEdit = _existing != null && !string.IsNullOrEmpty(_existing.Id);
            string F(string v) => string.IsNullOrWhiteSpace(v) ? "—" : v.Trim();
            var list = new List<ConfirmField>();

            void AddText(string section, string label, string newVal, string oldVal)
            {
                string sNew = (newVal ?? "").Trim();
                string sOld = (oldVal ?? "").Trim();
                bool changed = isEdit && !string.Equals(sOld, sNew, StringComparison.OrdinalIgnoreCase);

                list.Add(new ConfirmField
                {
                    Section   = section,
                    Label     = label,
                    NewValue  = F(sNew),
                    OldValue  = isEdit ? F(sOld) : null,
                    IsChanged = changed
                });
            }

            void AddPhone(string section, string label, string newVal, string oldVal)
            {
                string dNew = Regex.Replace(newVal ?? "", @"\D", "");
                string dOld = Regex.Replace(oldVal ?? "", @"\D", "");
                bool changed = isEdit && !string.Equals(dOld, dNew, StringComparison.Ordinal);

                string formattedNew = string.IsNullOrWhiteSpace(dNew) ? "—" : SOLUM_UI.Services.OcrService.FormatPhoneNumber(dNew);
                string formattedOld = string.IsNullOrWhiteSpace(dOld) ? "—" : SOLUM_UI.Services.OcrService.FormatPhoneNumber(dOld);

                list.Add(new ConfirmField
                {
                    Section   = section,
                    Label     = label,
                    NewValue  = formattedNew,
                    OldValue  = isEdit ? formattedOld : null,
                    IsChanged = changed
                });
            }

            void AddPhilSys(string section, string label, string newVal, string oldVal)
            {
                string dNew = Regex.Replace(newVal ?? "", @"\D", "");
                string dOld = Regex.Replace(oldVal ?? "", @"\D", "");
                bool changed = isEdit && !string.Equals(dOld, dNew, StringComparison.Ordinal);

                string formattedNew = FormatPhilSysNumber(dNew);
                string formattedOld = FormatPhilSysNumber(dOld);

                list.Add(new ConfirmField
                {
                    Section   = section,
                    Label     = label,
                    NewValue  = formattedNew,
                    OldValue  = isEdit ? formattedOld : null,
                    IsChanged = changed
                });
            }

            void AddCurrency(string section, string label, string newVal, string oldVal)
            {
                string cNew = Regex.Replace(newVal ?? "", @"[^\d.]", "");
                string cOld = Regex.Replace(oldVal ?? "", @"[^\d.]", "");

                bool changed = false;
                if (isEdit)
                {
                    if (decimal.TryParse(cNew, out var dNew) && decimal.TryParse(cOld, out var dOld))
                        changed = dNew != dOld;
                    else
                        changed = !string.Equals(cNew, cOld, StringComparison.OrdinalIgnoreCase);
                }

                string formattedNew = string.IsNullOrWhiteSpace(cNew) ? "—" : "₱ " + cNew;
                string formattedOld = string.IsNullOrWhiteSpace(cOld) ? "—" : "₱ " + cOld;

                list.Add(new ConfirmField
                {
                    Section   = section,
                    Label     = label,
                    NewValue  = formattedNew,
                    OldValue  = isEdit ? formattedOld : null,
                    IsChanged = changed
                });
            }

            void AddDate(string section, string label, DateTime newDt, DateTime oldDt)
            {
                bool hasNew = newDt != DateTime.MinValue;
                bool hasOld = oldDt != DateTime.MinValue;

                bool changed = isEdit && (hasNew != hasOld || (hasNew && hasOld && newDt.Date != oldDt.Date));

                string formattedNew = hasNew ? newDt.ToString("MMMM d, yyyy") : "—";
                string formattedOld = hasOld ? oldDt.ToString("MMMM d, yyyy") : "—";

                list.Add(new ConfirmField
                {
                    Section   = section,
                    Label     = label,
                    NewValue  = formattedNew,
                    OldValue  = isEdit ? formattedOld : null,
                    IsChanged = changed
                });
            }

            void AddBool(string section, string label, bool newBool, bool oldBool)
            {
                bool changed = isEdit && (newBool != oldBool);
                string displayNew = newBool ? "✓ Yes" : "No";
                string displayOld = oldBool ? "✓ Yes" : "No";

                list.Add(new ConfirmField
                {
                    Section   = section,
                    Label     = label,
                    NewValue  = displayNew,
                    OldValue  = isEdit ? displayOld : null,
                    IsChanged = changed
                });
            }

            // 1. Applicant Type (Only for New Registrations)
            if (!isEdit)
            {
                string secApp = "Applicant Type";
                string appTypeNew = r.IsNewApplicant ? "New Applicant" : r.IsRenewal ? "For Renewal" : "—";
                string appTypeOld = _existing != null ? (_existing.IsNewApplicant ? "New Applicant" : _existing.IsRenewal ? "For Renewal" : "—") : "—";
                AddText(secApp, "Application Type", appTypeNew, appTypeOld);
                AddDate(secApp, "Date of Application", r.DateOfApplication, _existing != null ? _existing.DateOfApplication : DateTime.MinValue);
            }

            // 2. Personal Information
            string sec = "Personal Information";
            if (isEdit)
            {
                AddDate(sec, "Date of Application", r.DateOfApplication, _existing != null ? _existing.DateOfApplication : DateTime.MinValue);
            }
            AddText(sec, "Last Name",              r.Surname,                 _existing?.Surname);
            AddText(sec, "First Name",             r.FirstName,               _existing?.FirstName);
            AddText(sec, "Middle Name",            r.MiddleName,              _existing?.MiddleName);
            AddText(sec, "Extension",              r.ExtensionName,           _existing?.ExtensionName);
            AddText(sec, "Sex",                    r.Sex,                     _existing?.Sex);
            AddText(sec, "Civil Status",           r.CivilStatus,             _existing?.CivilStatus);
            AddDate(sec, "Date of Birth",          r.DateOfBirth,             _existing != null ? _existing.DateOfBirth : DateTime.MinValue);

            string newAge = string.IsNullOrWhiteSpace(TxtAge.Text) ? "—" : TxtAge.Text.Trim();
            string oldAge = (_existing != null && _existing.DateOfBirth != DateTime.MinValue)
                ? SOLUM_UI.Services.OcrService.CalculateAge(_existing.DateOfBirth).ToString()
                : "—";
            AddText(sec, "Age",                    newAge,                    oldAge);

            AddText(sec, "Birthplace",             r.PlaceOfBirth,            _existing?.PlaceOfBirth);
            AddText(sec, "Educational Attainment", r.EducationalAttainment,   _existing?.EducationalAttainment);
            AddPhilSys(sec, "PhilSys Card No.",    r.PhilSysNumber,           _existing?.PhilSysNumber);
            AddText(sec, "Religion",               r.Religion,                _existing?.Religion);
            AddText(sec, "Occupation",             r.Occupation,              _existing?.Occupation);
            AddCurrency(sec, "Monthly Income",     r.MonthlyIncome,           _existing?.MonthlyIncome);
            AddText(sec, "Employment Status",      r.EmploymentStatusDisplay, _existing?.EmploymentStatusDisplay);

            // 3. Address & Contact
            sec = "Address & Contact";
            AddText(sec, "Address",                r.Address,                 _existing?.Address);
            AddText(sec, "Barangay",               r.Barangay,                _existing?.Barangay);
            AddPhone(sec, "Contact No.",           r.ContactNumber,           _existing?.ContactNumber);

            // 4. Emergency Contact
            sec = "Emergency Contact";
            AddText(sec, "Contact Person",         r.EmergencyContactName,    _existing?.EmergencyContactName);
            AddText(sec, "Relationship",           r.EmergencyRelationship,   _existing?.EmergencyRelationship);
            AddText(sec, "Address",                r.EmergencyAddress,        _existing?.EmergencyAddress);
            AddPhone(sec, "Contact No.",           r.EmergencyContactNumber,  _existing?.EmergencyContactNumber);

            // 5. Circumstances
            sec = "Circumstances";
            string oldCirc = _existing != null ? _existing.CircumstancesDisplay : "—";
            string newCirc = r.CircumstancesDisplay;
            bool circChanged = false;
            if (isEdit && _existing != null)
            {
                bool sameFlags =
                    r.CircumstanceA1 == _existing.CircumstanceA1 &&
                    r.CircumstanceA2 == _existing.CircumstanceA2 &&
                    r.CircumstanceA3 == _existing.CircumstanceA3 &&
                    r.CircumstanceA4 == _existing.CircumstanceA4 &&
                    r.CircumstanceA5 == _existing.CircumstanceA5 &&
                    r.CircumstanceA6 == _existing.CircumstanceA6 &&
                    r.CircumstanceA7 == _existing.CircumstanceA7 &&
                    r.CircumstanceB  == _existing.CircumstanceB &&
                    r.CircumstanceC  == _existing.CircumstanceC &&
                    r.CircumstanceD  == _existing.CircumstanceD &&
                    r.CircumstanceE  == _existing.CircumstanceE &&
                    r.CircumstanceF  == _existing.CircumstanceF;

                if (!sameFlags)
                {
                    circChanged = true;
                }
                else
                {
                    if (r.CircumstanceA2)
                    {
                        circChanged = !string.Equals((r.CircumstanceA2Cause ?? "").Trim(), (_existing.CircumstanceA2Cause ?? "").Trim(), StringComparison.OrdinalIgnoreCase) ||
                                      (r.CircumstanceA2Date.Date != _existing.CircumstanceA2Date.Date);
                    }
                    else if (r.CircumstanceA4)
                    {
                        circChanged = !string.Equals((r.CircumstanceA4Disability ?? "").Trim(), (_existing.CircumstanceA4Disability ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
                    }
                    else if (r.CircumstanceA5)
                    {
                        circChanged = !string.Equals((r.CircumstanceA5Period ?? "").Trim(), (_existing.CircumstanceA5Period ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
                    }
                    else if (r.CircumstanceA6)
                    {
                        circChanged = (r.CircumstanceA6Nullity != _existing.CircumstanceA6Nullity) ||
                                      (r.CircumstanceA6Annulment != _existing.CircumstanceA6Annulment);
                    }
                    else if (r.CircumstanceB)
                    {
                        circChanged = !string.Equals((r.CircumstanceBStayAbroad ?? "").Trim(), (_existing.CircumstanceBStayAbroad ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            string circDisplayOld = isEdit ? (!circChanged ? newCirc : (string.IsNullOrWhiteSpace(oldCirc) ? "None selected" : oldCirc)) : null;
            string circDisplayNew = string.IsNullOrWhiteSpace(newCirc) ? "None selected" : newCirc;

            list.Add(new ConfirmField
            {
                Section   = sec,
                Label     = "Problem Presented",
                NewValue  = circDisplayNew,
                OldValue  = circDisplayOld,
                IsChanged = circChanged
            });

            // 6. Family Composition
            sec = "Family Composition";
            string FormatMember(FamilyMember m)
            {
                if (m == null) return "—";
                string name = !string.IsNullOrWhiteSpace(m.MemberName) ? m.MemberName.Trim() : "Unnamed";
                string rel  = !string.IsNullOrWhiteSpace(m.Relationship) ? $" ({m.Relationship.Trim()})" : "";
                string age  = !string.IsNullOrWhiteSpace(m.Age) ? $", Age {m.Age.Trim()}" : "";
                string sex  = !string.IsNullOrWhiteSpace(m.Sex) ? $", {m.Sex.Trim()}" : "";
                return $"{name}{rel}{age}{sex}";
            }

            int newCount = r.FamilyMembers != null ? r.FamilyMembers.Count : 0;
            int oldCount = (_existing != null && _existing.FamilyMembers != null) ? _existing.FamilyMembers.Count : 0;

            if (newCount == 0 && oldCount == 0)
            {
                list.Add(new ConfirmField
                {
                    Section   = sec,
                    Label     = "Dependents",
                    NewValue  = "None registered",
                    OldValue  = isEdit ? "None registered" : null,
                    IsChanged = false
                });
            }
            else
            {
                for (int i = 0; i < newCount; i++)
                {
                    var m = r.FamilyMembers[i];
                    string memberLabel = "Member " + (i + 1);

                    FamilyMember oldM = null;
                    if (_existing?.FamilyMembers != null)
                    {
                        if (m.Id.HasValue && m.Id.Value != Guid.Empty)
                            oldM = _existing.FamilyMembers.Find(x => x.Id == m.Id);
                        else if (i < oldCount)
                            oldM = _existing.FamilyMembers[i];
                    }

                    string newSummary = FormatMember(m);
                    string oldSummary = oldM != null ? FormatMember(oldM) : null;
                    bool memberChanged = isEdit && (oldM == null || !string.Equals(oldSummary, newSummary, StringComparison.OrdinalIgnoreCase));

                    list.Add(new ConfirmField
                    {
                        Section   = sec,
                        Label     = memberLabel + (isEdit && oldM == null ? " (New)" : ""),
                        NewValue  = newSummary,
                        OldValue  = isEdit ? (oldSummary ?? "— (Not previously recorded)") : null,
                        IsChanged = memberChanged
                    });
                }

                if (isEdit && _existing?.FamilyMembers != null)
                {
                    foreach (var oldM in _existing.FamilyMembers)
                    {
                        bool stillExists = r.FamilyMembers != null && r.FamilyMembers.Exists(m =>
                            (m.Id.HasValue && oldM.Id.HasValue && m.Id.Value == oldM.Id.Value) ||
                            string.Equals(m.MemberName, oldM.MemberName, StringComparison.OrdinalIgnoreCase));

                        if (!stillExists)
                        {
                            list.Add(new ConfirmField
                            {
                                Section   = sec,
                                Label     = "Member (Removed)",
                                NewValue  = "— (Removed from record)",
                                OldValue  = FormatMember(oldM),
                                IsChanged = true
                            });
                        }
                    }
                }
            }

            // 7. Needs & Income
            sec = "Needs & Income";
            AddText(sec, "Needs and Problems",      r.NeedsAndProblems,  _existing?.NeedsAndProblems);
            AddText(sec, "Other Sources of Income", r.OtherIncomeSource, _existing?.OtherIncomeSource);

            // 8. Additional Classifications
            sec = "Additional Classifications";
            AddBool(sec, "Pantawid Beneficiary", r.IsPantawidBeneficiary, _existing != null && _existing.IsPantawidBeneficiary);
            AddBool(sec, "Indigenous Person",    r.IsIndigenous,          _existing != null && _existing.IsIndigenous);
            AddBool(sec, "LGBTQ+",               r.IsLGBT,                _existing != null && _existing.IsLGBT);

            return list;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
