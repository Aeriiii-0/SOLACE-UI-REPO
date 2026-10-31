using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services.Api;

namespace SOLUM_UI.Views.Dialogs
{
    public class DependentItemViewModel : INotifyPropertyChanged
    {
        public Guid? Id { get; set; }
        public bool IsNew { get; set; }

        private bool _isRemoved;
        public bool IsRemoved
        {
            get => _isRemoved;
            set
            {
                if (_isRemoved != value)
                {
                    _isRemoved = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusTag));
                    OnPropertyChanged(nameof(CardBg));
                    OnPropertyChanged(nameof(BorderColor));
                    OnPropertyChanged(nameof(TextColor));
                    OnPropertyChanged(nameof(TagBg));
                    OnPropertyChanged(nameof(TagFg));
                    OnPropertyChanged(nameof(ActionButtonText));
                    OnPropertyChanged(nameof(ActionButtonBg));
                    OnPropertyChanged(nameof(ActionButtonFg));
                    OnPropertyChanged(nameof(ActionButtonBorder));
                    OnPropertyChanged(nameof(ActionButtonIconData));
                    OnPropertyChanged(nameof(RemoveButtonToolTip));
                    OnPropertyChanged(nameof(EditButtonVisibility));
                }
            }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value?.ToUpper();
                    OnPropertyChanged();
                }
            }
        }

        private string _relationship;
        public string Relationship
        {
            get => _relationship;
            set
            {
                if (_relationship != value)
                {
                    _relationship = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _sex;
        public string Sex
        {
            get => _sex;
            set
            {
                if (_sex != value)
                {
                    _sex = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime _birthdate;
        public DateTime Birthdate
        {
            get => _birthdate;
            set
            {
                if (_birthdate != value)
                {
                    _birthdate = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _age;
        public int Age
        {
            get => _age;
            set
            {
                if (_age != value)
                {
                    _age = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AgeDisplay));
                }
            }
        }

        private string _educationalLevel;
        public string EducationalLevel
        {
            get => _educationalLevel;
            set
            {
                if (_educationalLevel != value)
                {
                    _educationalLevel = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _income;
        public decimal Income
        {
            get => _income;
            set
            {
                if (_income != value)
                {
                    _income = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IncomeDisplay));
                }
            }
        }

        private string _civilStatus;
        public string CivilStatus
        {
            get => _civilStatus;
            set
            {
                if (_civilStatus != value)
                {
                    _civilStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _hasSpecialNeeds;
        public bool HasSpecialNeeds
        {
            get => _hasSpecialNeeds;
            set
            {
                if (_hasSpecialNeeds != value)
                {
                    _hasSpecialNeeds = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisabilityTitleDisplay));
                    OnPropertyChanged(nameof(DisabilitySubtitleDisplay));
                    OnPropertyChanged(nameof(DisabilitySubtitleVisibility));
                    OnPropertyChanged(nameof(DisabilityTextColor));
                    OnPropertyChanged(nameof(DisabilityTextWeight));
                }
            }
        }

        private string _specialNeedsCategory;
        public string SpecialNeedsCategory
        {
            get => _specialNeedsCategory;
            set
            {
                if (_specialNeedsCategory != value)
                {
                    _specialNeedsCategory = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisabilityTitleDisplay));
                }
            }
        }

        private string _specialNeedsDescription;
        public string SpecialNeedsDescription
        {
            get => _specialNeedsDescription;
            set
            {
                if (_specialNeedsDescription != value)
                {
                    _specialNeedsDescription = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisabilitySubtitleDisplay));
                    OnPropertyChanged(nameof(DisabilitySubtitleVisibility));
                }
            }
        }

        public string AgeDisplay => $"{Age} yrs old";
        public string IncomeDisplay => Income > 0 ? $"₱ {Income:N2}/mo" : "No income";

        public string StatusTag => IsRemoved ? "REMOVED" : (IsNew ? "NEW" : "ACTIVE");

        public Brush CardBg => IsRemoved
            ? new SolidColorBrush(Color.FromRgb(0xFE, 0xF2, 0xF2))
            : (IsNew ? new SolidColorBrush(Color.FromRgb(0xFA, 0xEE, 0xF3)) : new SolidColorBrush(Colors.White));

        public Brush BorderColor => IsRemoved
            ? new SolidColorBrush(Color.FromRgb(0xFE, 0xCA, 0xCA))
            : (IsNew ? new SolidColorBrush(Color.FromRgb(0xD0, 0x98, 0xAF)) : new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB)));

        public Brush TextColor => IsRemoved
            ? new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF))
            : new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));

        public Brush TagBg => IsRemoved
            ? new SolidColorBrush(Color.FromRgb(0xFE, 0xCA, 0xCA))
            : (IsNew ? new SolidColorBrush(Color.FromRgb(0xF5, 0xDC, 0xE5)) : new SolidColorBrush(Color.FromRgb(0xD1, 0xFA, 0xE5)));

        public Brush TagFg => IsRemoved
            ? new SolidColorBrush(Color.FromRgb(0x99, 0x1B, 0x1B))
            : (IsNew ? new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43)) : new SolidColorBrush(Color.FromRgb(0x06, 0x5F, 0x46)));

        public string ActionButtonText => IsRemoved ? "Undo" : "Remove";

        public Brush ActionButtonBg => IsRemoved
            ? new SolidColorBrush(Color.FromRgb(0xF3, 0xF4, 0xF6))
            : new SolidColorBrush(Color.FromRgb(0xFE, 0xF2, 0xF2));

        public Brush ActionButtonFg => IsRemoved
            ? new SolidColorBrush(Color.FromRgb(0x37, 0x41, 0x51))
            : new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));

        public Brush ActionButtonBorder => IsRemoved
            ? new SolidColorBrush(Color.FromRgb(0xD1, 0xD5, 0xDB))
            : new SolidColorBrush(Color.FromRgb(0xFE, 0xCA, 0xCA));

        public string ActionButtonIconData => IsRemoved
            ? "M12.5,8C9.85,8 7.45,9 5.6,10.6L2,7V16H11L7.38,12.38C8.77,11.22 10.54,10.5 12.5,10.5C16.04,10.5 19.05,12.81 20.1,16L22.47,15.22C21.08,11.03 17.15,8 12.5,8Z"
            : "M19,4H15.5L14.5,3H9.5L8.5,4H5V6H19M6,19A2,2 0 0,0 8,21H16A2,2 0 0,0 18,19V7H6V19Z";

        public string RemoveButtonToolTip => IsRemoved ? "Restore dependent" : "Remove dependent";
        public Visibility EditButtonVisibility => IsRemoved ? Visibility.Collapsed : Visibility.Visible;

        public string DisabilityTitleDisplay => HasSpecialNeeds
            ? (!string.IsNullOrWhiteSpace(SpecialNeedsCategory) ? SpecialNeedsCategory : "Special Needs / PWD")
            : "None";

        public string DisabilitySubtitleDisplay => (HasSpecialNeeds && !string.IsNullOrWhiteSpace(SpecialNeedsDescription))
            ? SpecialNeedsDescription
            : string.Empty;

        public Visibility DisabilitySubtitleVisibility => (HasSpecialNeeds && !string.IsNullOrWhiteSpace(SpecialNeedsDescription))
            ? Visibility.Visible : Visibility.Collapsed;

        public Brush DisabilityTextColor => HasSpecialNeeds
            ? new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43))
            : new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));

        public FontWeight DisabilityTextWeight => HasSpecialNeeds ? FontWeights.SemiBold : FontWeights.Normal;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void RefreshAll()
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    public class RenewalDiffRow
    {
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public bool IsChanged { get; set; }
        public FontWeight NewValueWeight => IsChanged ? FontWeights.Bold : FontWeights.Normal;
        public Brush NewValueColor => IsChanged
            ? new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43))
            : new SolidColorBrush(Color.FromRgb(0x37, 0x41, 0x51));
    }

    public partial class RenewalReassessmentDialog : Window
    {
        private readonly SoloParentRecord _original;
        private readonly SoloParentDto _dto;
        private int _currentStep = 1;
        private bool _isSyncingOtherIncome = false;
        private bool _isCircumstanceChanged = false;

        private static readonly Regex DecimalRegex = new Regex(@"^[0-9.]+$", RegexOptions.Compiled);

        public RenewSoloParentRequest RenewRequest { get; private set; }
        public SoloParentRecord UpdatedRecord { get; private set; }

        private readonly ObservableCollection<DependentItemViewModel> _dependents = new ObservableCollection<DependentItemViewModel>();
        private readonly List<Guid> _removedFamilyMemberIds = new List<Guid>();
        private DependentItemViewModel _editingDependent = null;
        private readonly bool _isReassessmentMode;

        public RenewalReassessmentDialog(SoloParentRecord original, SoloParentDto dto = null, bool isReassessmentMode = false)
        {
            InitializeComponent();
            _original = original ?? throw new ArgumentNullException(nameof(original));
            _dto = dto;
            _isReassessmentMode = isReassessmentMode;

            DependentsList.ItemsSource = _dependents;

            Loaded += (s, e) =>
            {
                if (!_isReassessmentMode && !_original.CanRenew(out string failureReason, out _))
                {
                    ActionConfirmDialog.Warning(
                        "Renewal Not Allowed",
                        failureReason ?? "This solo parent record has been active for less than a month and cannot be renewed yet.",
                        this);
                    DialogResult = false;
                    Close();
                    return;
                }

                if (_isReassessmentMode)
                {
                    Title = "Case Re-assessment";
                    if (TxtDialogTitle != null) TxtDialogTitle.Text = "Case Re-assessment & Eligibility Review";
                    if (TxtDialogSubtitle != null) TxtDialogSubtitle.Text = "Update circumstances, income, and family composition to evaluate vulnerability and subsidy priority.";
                }

                PopulateIdentity();
                PopulateStep1();
                PopulateStep2();
                PopulateStep3();
                UpdateStepView();
            };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ApplySize();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => ApplySize();

        private Point _dragStartPoint;
        private Point _currentMousePoint;
        private bool _isDraggingShell = false;
        private bool _hasPendingDragMove = false;

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isDraggingShell = true;
                _hasPendingDragMove = false;
                _dragStartPoint = e.GetPosition(this);
                _currentMousePoint = _dragStartPoint;
                ((UIElement)sender).CaptureMouse();
                CompositionTarget.Rendering += OnCompositionTargetRendering;
            }
        }

        private void Header_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingShell && e.LeftButton == MouseButtonState.Pressed)
            {
                _currentMousePoint = e.GetPosition(this);
                _hasPendingDragMove = true;
            }
        }

        private void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            if (!_isDraggingShell || !_hasPendingDragMove || DialogShellTransform == null) return;

            double deltaX = _currentMousePoint.X - _dragStartPoint.X;
            double deltaY = _currentMousePoint.Y - _dragStartPoint.Y;

            if (Math.Abs(deltaX) > 0.05 || Math.Abs(deltaY) > 0.05)
            {
                DialogShellTransform.X += deltaX;
                DialogShellTransform.Y += deltaY;
                _dragStartPoint = _currentMousePoint;
            }
            _hasPendingDragMove = false;
        }

        private void Header_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingShell)
            {
                _isDraggingShell = false;
                _hasPendingDragMove = false;
                CompositionTarget.Rendering -= OnCompositionTargetRendering;
                ((UIElement)sender).ReleaseMouseCapture();
            }
        }

        private void ApplySize()
        {
            double workW = SystemParameters.WorkArea.Width;
            double workH = SystemParameters.WorkArea.Height;
            double workLeft = SystemParameters.WorkArea.Left;
            double workTop = SystemParameters.WorkArea.Top;

            double screenW = workW;
            double screenH = workH;
            double screenLeft = workLeft;
            double screenTop = workTop;

            if (Owner != null && Owner.WindowState != WindowState.Maximized && Owner.ActualWidth > 0 && Owner.ActualHeight > 0)
            {
                screenW    = Math.Min(Owner.ActualWidth, workW);
                screenH    = Math.Min(Owner.ActualHeight, workH);
                screenLeft = Math.Max(workLeft, Owner.Left);
                screenTop  = Math.Max(workTop, Owner.Top);
            }

            if (Math.Abs(Width - screenW) > 0.5) Width = screenW;
            if (Math.Abs(Height - screenH) > 0.5) Height = screenH;
            if (Math.Abs(Left - screenLeft) > 0.5) Left = screenLeft;
            if (Math.Abs(Top - screenTop) > 0.5) Top = screenTop;

            if (DialogShell != null)
            {
                DialogShell.MaxHeight = Math.Max(500, Math.Min(screenH - 40, 840));
                DialogShell.Width     = Math.Min(screenW * 0.88, 1320);
                DialogShell.MinWidth  = Math.Max(screenW * 0.60, 680);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !DecimalRegex.IsMatch(e.Text);
        }

        private void TxtOtherIncomeStep1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingOtherIncome || TxtOtherIncomeStep3 == null) return;
            _isSyncingOtherIncome = true;
            try
            {
                TxtOtherIncomeStep3.Text = TxtOtherIncomeStep1.Text;
            }
            finally
            {
                _isSyncingOtherIncome = false;
            }
        }

        private void TxtOtherIncomeStep3_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingOtherIncome || TxtOtherIncomeStep1 == null) return;
            _isSyncingOtherIncome = true;
            try
            {
                TxtOtherIncomeStep1.Text = TxtOtherIncomeStep3.Text;
            }
            finally
            {
                _isSyncingOtherIncome = false;
            }
        }

        // =========================================================================
        // Step Population
        // =========================================================================

        private void PopulateIdentity()
        {
            string fullName = !string.IsNullOrWhiteSpace(_original.FullName) ? _original.FullName : _original.Name;
            TxtApplicantName.Text = fullName;

            // Initials
            string initials = "SP";
            if (!string.IsNullOrWhiteSpace(_original.FirstName) && !string.IsNullOrWhiteSpace(_original.Surname))
            {
                initials = $"{_original.FirstName[0]}{_original.Surname[0]}".ToUpper();
            }
            TxtInitials.Text = initials;

            TxtPhilSysNumber.Text = !string.IsNullOrWhiteSpace(_original.PhilSysNumber) ? _original.PhilSysNumber : "None";
            TxtBarangay.Text = !string.IsNullOrWhiteSpace(_original.Barangay) ? _original.Barangay : "Biñan";

            string expStr = _original.CurrentTermExpiresAt.HasValue
                ? _original.CurrentTermExpiresAt.Value.ToString("MMM d, yyyy")
                : DateTime.Today.ToString("MMM d, yyyy");

            TxtExpiryNotice.Text = $"Current Expiry: {expStr}";
        }

        private void PopulateStep1()
        {
            // Civil Status
            string civ = _original.CivilStatus?.Trim() ?? "Single";
            bool foundCiv = false;
            foreach (ComboBoxItem item in CmbCivilStatus.Items)
            {
                if (string.Equals(item.Content?.ToString(), civ, StringComparison.OrdinalIgnoreCase))
                {
                    CmbCivilStatus.SelectedItem = item;
                    foundCiv = true;
                    break;
                }
            }
            if (!foundCiv) CmbCivilStatus.SelectedIndex = 0;

            // Employment Status
            if (_original.IsSelfEmployed)
                CmbEmploymentStatus.SelectedIndex = 1;
            else if (_original.IsNotEmployed)
                CmbEmploymentStatus.SelectedIndex = 2;
            else
                CmbEmploymentStatus.SelectedIndex = 0; // Default Employed

            // Occupation & Income
            TxtOccupation.Text = !string.IsNullOrWhiteSpace(_original.Occupation) ? _original.Occupation : "Employed";
            TxtMonthlyIncome.Text = !string.IsNullOrWhiteSpace(_original.MonthlyIncome) ? _original.MonthlyIncome : "0.00";
            string initOtherIncome = _original.OtherIncomeSource ?? (_dto?.OtherSourcesOfIncome ?? string.Empty);
            TxtOtherIncomeStep1.Text = initOtherIncome;
            TxtOtherIncomeStep3.Text = initOtherIncome;
        }

        private void PopulateStep2()
        {
            _dependents.Clear();

            // Populate from DTO if available, otherwise record.FamilyMembers
            if (_dto?.FamilyMembers != null && _dto.FamilyMembers.Count > 0)
            {
                foreach (var fm in _dto.FamilyMembers)
                {
                    _dependents.Add(new DependentItemViewModel
                    {
                        Id = fm.Id,
                        IsNew = false,
                        IsRemoved = false,
                        Name = fm.Name,
                        Relationship = fm.Relationship,
                        Sex = fm.Sex,
                        Birthdate = fm.Birthdate,
                        Age = fm.Age,
                        EducationalLevel = fm.EducationalLevel,
                        Income = fm.Income,
                        CivilStatus = fm.CivilStatus,
                        HasSpecialNeeds = fm.HasSpecialNeeds,
                        SpecialNeedsCategory = fm.SpecialNeedsCategory,
                        SpecialNeedsDescription = fm.SpecialNeedsDescription
                    });
                }
            }
            else if (_original.FamilyMembers != null && _original.FamilyMembers.Count > 0)
            {
                foreach (var fm in _original.FamilyMembers)
                {
                    int age = 0;
                    int.TryParse(fm.Age, out age);
                    DateTime dob = DateTime.TryParse(fm.Birthdate, out var parsedDob) ? parsedDob : DateTime.Today.AddYears(-age);
                    decimal income = 0;
                    decimal.TryParse(fm.Income, out income);

                    _dependents.Add(new DependentItemViewModel
                    {
                        Id = fm.Id,
                        IsNew = fm.IsNew,
                        IsRemoved = false,
                        Name = fm.MemberName,
                        Relationship = !string.IsNullOrWhiteSpace(fm.Relationship) ? fm.Relationship : "Child",
                        Sex = !string.IsNullOrWhiteSpace(fm.Sex) ? fm.Sex : "Female",
                        Birthdate = dob,
                        Age = age,
                        EducationalLevel = !string.IsNullOrWhiteSpace(fm.EducationEmployment) ? fm.EducationEmployment : "Student",
                        Income = income,
                        CivilStatus = !string.IsNullOrWhiteSpace(fm.CivilStatus) ? fm.CivilStatus : "Single",
                        HasSpecialNeeds = fm.HasSpecialNeeds,
                        SpecialNeedsCategory = fm.SpecialNeedsCategory,
                        SpecialNeedsDescription = fm.SpecialNeedsDescription
                    });
                }
            }

            TxtNoDependents.Visibility = _dependents.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PopulateStep3()
        {
            // Clear all circumstance radios
            ChkA1.IsChecked = false;
            ChkA2.IsChecked = false;
            ChkA3.IsChecked = false;
            ChkA4.IsChecked = false;
            ChkA5.IsChecked = false;
            ChkA6.IsChecked = false;
            ChkA7.IsChecked = false;
            ChkB.IsChecked  = false;
            ChkC.IsChecked  = false;
            ChkD.IsChecked  = false;
            ChkE.IsChecked  = false;
            ChkF.IsChecked  = false;

            string pNum = _dto?.ProblemPresented?.ProblemPresentedNumber ?? string.Empty;
            if (string.IsNullOrWhiteSpace(pNum))
            {
                pNum = SoloParentApiService.GetCircumstanceCode(_original);
            }
            if (string.IsNullOrWhiteSpace(pNum))
            {
                pNum = SoloParentApiService.ExtractCircumstanceCode(_dto?.ProblemPresented?.Name);
            }
            if (string.IsNullOrWhiteSpace(pNum))
            {
                pNum = SoloParentApiService.ExtractCircumstanceCode(_original.ProblemPresented);
            }

            if (string.Equals(pNum, "A1", StringComparison.OrdinalIgnoreCase) || _original.CircumstanceA1) ChkA1.IsChecked = true;
            else if (string.Equals(pNum, "A2", StringComparison.OrdinalIgnoreCase) || _original.CircumstanceA2) ChkA2.IsChecked = true;
            else if (string.Equals(pNum, "A3", StringComparison.OrdinalIgnoreCase) || _original.CircumstanceA3) ChkA3.IsChecked = true;
            else if (string.Equals(pNum, "A4", StringComparison.OrdinalIgnoreCase) || _original.CircumstanceA4) ChkA4.IsChecked = true;
            else if (string.Equals(pNum, "A5", StringComparison.OrdinalIgnoreCase) || _original.CircumstanceA5) ChkA5.IsChecked = true;
            else if (string.Equals(pNum, "A6", StringComparison.OrdinalIgnoreCase) || _original.CircumstanceA6) ChkA6.IsChecked = true;
            else if (string.Equals(pNum, "A7", StringComparison.OrdinalIgnoreCase) || _original.CircumstanceA7) ChkA7.IsChecked = true;
            else if (string.Equals(pNum, "B",  StringComparison.OrdinalIgnoreCase) || _original.CircumstanceB)  ChkB.IsChecked = true;
            else if (string.Equals(pNum, "C",  StringComparison.OrdinalIgnoreCase) || _original.CircumstanceC)  ChkC.IsChecked = true;
            else if (string.Equals(pNum, "D",  StringComparison.OrdinalIgnoreCase) || _original.CircumstanceD)  ChkD.IsChecked = true;
            else if (string.Equals(pNum, "E",  StringComparison.OrdinalIgnoreCase) || _original.CircumstanceE)  ChkE.IsChecked = true;
            else if (string.Equals(pNum, "F",  StringComparison.OrdinalIgnoreCase) || _original.CircumstanceF)  ChkF.IsChecked = true;
            else ChkA2.IsChecked = true; // Default fallback

            // Conditional subfields
            TxtA2Cause.Text = _dto?.ProblemPresented?.CauseOfDeath ?? _original.CircumstanceA2Cause ?? string.Empty;
            if (DateTime.TryParse(_dto?.ProblemPresented?.date, out var a2Dt))
                DpA2Date.SelectedDate = a2Dt;
            else if (_original.CircumstanceA2Date != DateTime.MinValue)
                DpA2Date.SelectedDate = _original.CircumstanceA2Date;

            TxtA4Disability.Text = _dto?.ProblemPresented?.TypeOfDisability ?? _original.CircumstanceA4Disability ?? string.Empty;
            TxtA5Period.Text = _dto?.ProblemPresented?.PeriodOfSeparation ?? _original.CircumstanceA5Period ?? string.Empty;

            bool isNullity = _dto?.ProblemPresented?.isNullity ?? _original.CircumstanceA6Nullity;
            bool isAnnulment = _dto?.ProblemPresented?.isAnnulmentOfMarraige ?? _original.CircumstanceA6Annulment;
            if (isNullity)
                SetComboByContent(CmbA6Declaration, "Nullity of marriage");
            else if (isAnnulment)
                SetComboByContent(CmbA6Declaration, "Annulment of marriage");
            else
                CmbA6Declaration.SelectedIndex = 0;

            decimal abroad = _dto?.ProblemPresented?.LengthOfAbroad ?? 0;
            TxtBStayAbroad.Text = abroad > 0 ? abroad.ToString("0") : (_original.CircumstanceBStayAbroad ?? string.Empty);

            // Needs & Problems
            TxtNeeds.Text = !string.IsNullOrWhiteSpace(_original.NeedsAndProblems)
                ? _original.NeedsAndProblems
                : (_dto?.NeedsAndProblems ?? string.Empty);

            Circumstance_Changed(null, null);
        }

        private string GetSelectedCircumstanceTag()
        {
            if (ChkA1.IsChecked == true) return "A1";
            if (ChkA2.IsChecked == true) return "A2";
            if (ChkA3.IsChecked == true) return "A3";
            if (ChkA4.IsChecked == true) return "A4";
            if (ChkA5.IsChecked == true) return "A5";
            if (ChkA6.IsChecked == true) return "A6";
            if (ChkA7.IsChecked == true) return "A7";
            if (ChkB.IsChecked == true)  return "B";
            if (ChkC.IsChecked == true)  return "C";
            if (ChkD.IsChecked == true)  return "D";
            if (ChkE.IsChecked == true)  return "E";
            if (ChkF.IsChecked == true)  return "F";
            return "";
        }

        private static string GetCircumstanceTitle(string tag)
        {
            switch ((tag ?? "").ToUpperInvariant())
            {
                case "A1": return "Gives birth as a result of rape";
                case "A2": return "Death of Spouse";
                case "A3": return "Detention of Spouse";
                case "A4": return "Physical & Mental Incapacity of Spouse";
                case "A5": return "Legal or de facto Separation";
                case "A6": return "Declaration of nullity or annulment of marriage";
                case "A7": return "Abandonment of spouse for at least 6 months";
                case "B":  return "Spouse or any family member of an OFW";
                case "C":  return "Unmarried Mother or Father";
                case "D":  return "Legal Guardian / Adoptive / Foster Parent";
                case "E":  return "Relative within the fourth (4th) civil degree of consanguinity or affinity";
                case "F":  return "Pregnant Woman";
                default:   return tag ?? "—";
            }
        }

        private static string GetComboValue(ComboBox cb)
        {
            if (cb?.SelectedItem is ComboBoxItem item)
                return item.Content?.ToString() ?? string.Empty;
            return cb?.Text ?? string.Empty;
        }

        // =========================================================================
        // Stepper & Navigation
        // =========================================================================

        private void UpdateStepView()
        {
            Step1Panel.Visibility = _currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2Panel.Visibility = _currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3Panel.Visibility = _currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;
            Step4Panel.Visibility = _currentStep == 4 ? Visibility.Visible : Visibility.Collapsed;

            // Stepper indicators
            var activeBg = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));
            var doneBg = new SolidColorBrush(Color.FromRgb(0x8B, 0x35, 0x55));
            var inactiveBg = new SolidColorBrush(Color.FromRgb(0xE0, 0xD0, 0xD6));
            var activeFg = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));
            var inactiveFg = new SolidColorBrush(Color.FromRgb(0x77, 0x77, 0x77));

            // Step 1 Indicator
            Step1Indicator.Background = _currentStep >= 1 ? activeBg : inactiveBg;
            Step1Label.Foreground = _currentStep == 1 ? activeFg : inactiveFg;

            // Step 2 Indicator
            Step2Indicator.Background = _currentStep >= 2 ? activeBg : inactiveBg;
            Step2Num.Foreground = _currentStep >= 2 ? Brushes.White : inactiveFg;
            Step2Label.Foreground = _currentStep == 2 ? activeFg : inactiveFg;

            // Step 3 Indicator
            Step3Indicator.Background = _currentStep >= 3 ? activeBg : inactiveBg;
            Step3Num.Foreground = _currentStep >= 3 ? Brushes.White : inactiveFg;
            Step3Label.Foreground = _currentStep == 3 ? activeFg : inactiveFg;

            // Step 4 Indicator
            Step4Indicator.Background = _currentStep >= 4 ? activeBg : inactiveBg;
            Step4Num.Foreground = _currentStep >= 4 ? Brushes.White : inactiveFg;
            Step4Label.Foreground = _currentStep == 4 ? activeFg : inactiveFg;

            // Buttons
            BtnBack.Visibility = _currentStep > 1 ? Visibility.Visible : Visibility.Collapsed;
            BtnNext.Visibility = _currentStep < 4 ? Visibility.Visible : Visibility.Collapsed;
            BtnSubmitRenew.Visibility = _currentStep == 4 ? Visibility.Visible : Visibility.Collapsed;

            if (_currentStep == 4)
            {
                BuildReviewDiff();
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateCurrentStep()) return;

            if (_currentStep < 4)
            {
                _currentStep++;
                UpdateStepView();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep > 1)
            {
                _currentStep--;
                UpdateStepView();
            }
        }

        private bool ValidateCurrentStep()
        {
            if (_currentStep == 1)
            {
                if (CmbCivilStatus.SelectedItem == null)
                {
                    ToastNotification.Show("Validation Required", "Please select a Civil Status.", ToastType.Warning);
                    CmbCivilStatus.Focus();
                    return false;
                }
                if (string.IsNullOrWhiteSpace(TxtOccupation.Text))
                {
                    ToastNotification.Show("Validation Required", "Please enter an Occupation or source of livelihood.", ToastType.Warning);
                    TxtOccupation.Focus();
                    return false;
                }
                if (string.IsNullOrWhiteSpace(TxtMonthlyIncome.Text) || !decimal.TryParse(TxtMonthlyIncome.Text.Trim(), out _))
                {
                    ToastNotification.Show("Validation Required", "Please enter a valid monthly income number.", ToastType.Warning);
                    TxtMonthlyIncome.Focus();
                    return false;
                }
            }
            else if (_currentStep == 2)
            {
                int activeCount = _dependents.Count(d => !d.IsRemoved);
                if (activeCount == 0)
                {
                    bool proceed = ActionConfirmDialog.Show(
                        "No Active Dependents",
                        "There are currently 0 active dependents listed. Solo parent records usually require at least one dependent.\n\nDo you wish to continue?",
                        "Continue Anyway",
                        "Review Dependents",
                        ConfirmThemeType.Warning,
                        this);
                    if (!proceed) return false;
                }
            }
            else if (_currentStep == 3)
            {
                string tag = GetSelectedCircumstanceTag();
                if (string.IsNullOrWhiteSpace(tag))
                {
                    ToastNotification.Show("Validation Required", "Please select the primary Circumstance / Ground.", ToastType.Warning);
                    return false;
                }
            }

            return true;
        }

        // =========================================================================
        // Step 2 Dependent Management
        // =========================================================================

        private void BtnAddChild_Click(object sender, RoutedEventArgs e)
        {
            _editingDependent = null;
            TxtAddChildTitle.Text = "Add New Dependent";
            BtnSaveChild.Content = "Add Dependent";

            TxtChildName.Text = string.Empty;
            CmbChildRel.SelectedIndex = -1;
            CmbChildSex.SelectedIndex = -1;
            CmbChildCivil.SelectedIndex = -1;
            DpChildDob.SelectedDate = null;
            TxtChildAge.Text = string.Empty;
            CmbChildEdu.SelectedIndex = -1;
            TxtChildIncome.Text = string.Empty;
            ChkChildSpecialNeeds.IsChecked = false;
            CmbChildSpecialNeedsCategory.SelectedIndex = -1;
            TxtChildSpecialNeedsDescription.Text = string.Empty;
            if (PnlChildSpecialNeedsDetails != null)
            {
                PnlChildSpecialNeedsDetails.IsEnabled = false;
                PnlChildSpecialNeedsDetails.Opacity = 0.5;
            }

            AddChildSubCard.Visibility = Visibility.Visible;
            TxtChildName.Focus();
        }

        private void BtnEditDependent_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is DependentItemViewModel item)
            {
                _editingDependent = item;
                TxtAddChildTitle.Text = $"Edit Dependent: {item.Name}";
                BtnSaveChild.Content = "Update Dependent";

                TxtChildName.Text = item.Name;
                SetComboByContent(CmbChildRel, item.Relationship);
                SetComboByContent(CmbChildSex, item.Sex);
                SetComboByContent(CmbChildCivil, item.CivilStatus);
                DpChildDob.SelectedDate = item.Birthdate;
                TxtChildAge.Text = item.Age.ToString();
                SetComboByContent(CmbChildEdu, item.EducationalLevel);
                TxtChildIncome.Text = item.Income > 0 ? item.Income.ToString("N2") : string.Empty;

                bool hasSn = item.HasSpecialNeeds;
                ChkChildSpecialNeeds.IsChecked = hasSn;
                if (PnlChildSpecialNeedsDetails != null)
                {
                    PnlChildSpecialNeedsDetails.IsEnabled = hasSn;
                    PnlChildSpecialNeedsDetails.Opacity = hasSn ? 1.0 : 0.5;
                }
                SetComboByContent(CmbChildSpecialNeedsCategory, item.SpecialNeedsCategory);
                TxtChildSpecialNeedsDescription.Text = item.SpecialNeedsDescription ?? string.Empty;

                AddChildSubCard.Visibility = Visibility.Visible;
                TxtChildName.Focus();
            }
        }

        private void BtnCancelChild_Click(object sender, RoutedEventArgs e)
        {
            AddChildSubCard.Visibility = Visibility.Collapsed;
            _editingDependent = null;
        }

        private void BtnSaveChild_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtChildName.Text?.Trim()?.ToUpper();
            if (string.IsNullOrWhiteSpace(name))
            {
                ToastNotification.Show("Validation Required", "Please enter the dependent's full name.", ToastType.Warning);
                TxtChildName.Focus();
                return;
            }

            string rel = (CmbChildRel.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(rel))
            {
                ToastNotification.Show("Validation Required", "Please select a relationship.", ToastType.Warning);
                CmbChildRel.Focus();
                return;
            }

            string sex = (CmbChildSex.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(sex))
            {
                ToastNotification.Show("Validation Required", "Please select the dependent's sex.", ToastType.Warning);
                CmbChildSex.Focus();
                return;
            }

            if (!DpChildDob.SelectedDate.HasValue)
            {
                ToastNotification.Show("Validation Required", "Please select a valid birthdate.", ToastType.Warning);
                DpChildDob.Focus();
                return;
            }

            DateTime dob = DpChildDob.SelectedDate.Value;
            int age = SOLUM_UI.Services.OcrService.CalculateAge(dob);
            if (age < 0 || age > 120) age = 0;

            string civil = (CmbChildCivil.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(civil)) civil = "Single";

            string edu = (CmbChildEdu.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(edu)) edu = "None";

            decimal income = 0;
            if (!string.IsNullOrWhiteSpace(TxtChildIncome.Text))
            {
                decimal.TryParse(TxtChildIncome.Text.Trim(), out income);
            }

            bool hasSn = ChkChildSpecialNeeds.IsChecked == true;
            string snCat = hasSn ? ((CmbChildSpecialNeedsCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty) : string.Empty;
            if (hasSn && string.IsNullOrWhiteSpace(snCat))
            {
                ToastNotification.Show("Validation Required", "Please select a special needs category.", ToastType.Warning);
                CmbChildSpecialNeedsCategory.Focus();
                return;
            }
            string snDesc = hasSn ? (TxtChildSpecialNeedsDescription.Text?.Trim() ?? string.Empty) : string.Empty;

            if (_editingDependent != null)
            {
                _editingDependent.Name = name;
                _editingDependent.Relationship = rel;
                _editingDependent.Sex = sex;
                _editingDependent.CivilStatus = civil;
                _editingDependent.Birthdate = dob;
                _editingDependent.Age = age;
                _editingDependent.EducationalLevel = edu;
                _editingDependent.Income = income;
                _editingDependent.HasSpecialNeeds = hasSn;
                _editingDependent.SpecialNeedsCategory = snCat;
                _editingDependent.SpecialNeedsDescription = snDesc;
                _editingDependent.RefreshAll();
                DependentsList?.Items.Refresh();
            }
            else
            {
                _dependents.Add(new DependentItemViewModel
                {
                    Id = null,
                    IsNew = true,
                    IsRemoved = false,
                    Name = name,
                    Relationship = rel,
                    Sex = sex,
                    Birthdate = dob,
                    Age = age,
                    EducationalLevel = edu,
                    Income = income,
                    CivilStatus = civil,
                    HasSpecialNeeds = hasSn,
                    SpecialNeedsCategory = snCat,
                    SpecialNeedsDescription = snDesc
                });
            }

            AddChildSubCard.Visibility = Visibility.Collapsed;
            _editingDependent = null;
            TxtNoDependents.Visibility = _dependents.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void DpChildDob_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DpChildDob.SelectedDate.HasValue)
            {
                int age = SOLUM_UI.Services.OcrService.CalculateAge(DpChildDob.SelectedDate.Value);
                TxtChildAge.Text = (age >= 0 && age <= 120) ? age.ToString() : "0";
            }
            else
            {
                TxtChildAge.Text = string.Empty;
            }
        }

        private void ChkChildSpecialNeeds_Changed(object sender, RoutedEventArgs e)
        {
            if (PnlChildSpecialNeedsDetails != null)
            {
                bool isSpecial = ChkChildSpecialNeeds.IsChecked == true;
                PnlChildSpecialNeedsDetails.IsEnabled = isSpecial;
                PnlChildSpecialNeedsDetails.Opacity = isSpecial ? 1.0 : 0.5;
                if (!isSpecial)
                {
                    if (CmbChildSpecialNeedsCategory != null) CmbChildSpecialNeedsCategory.SelectedIndex = -1;
                    if (TxtChildSpecialNeedsDescription != null) TxtChildSpecialNeedsDescription.Text = string.Empty;
                }
            }
        }

        private void SetComboByContent(ComboBox combo, string value)
        {
            if (combo == null) return;
            if (string.IsNullOrWhiteSpace(value))
            {
                combo.SelectedIndex = -1;
                return;
            }

            string val = value.Trim();
            foreach (ComboBoxItem item in combo.Items)
            {
                if (string.Equals(item.Content?.ToString(), val, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedItem = item;
                    return;
                }
            }

            foreach (ComboBoxItem item in combo.Items)
            {
                string text = item.Content?.ToString() ?? "";
                if (text.IndexOf(val, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    val.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }

            combo.SelectedIndex = -1;
        }

        private void BtnToggleRemoveDependent_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is DependentItemViewModel item)
            {
                if (item.IsRemoved)
                {
                    // Undo remove
                    item.IsRemoved = false;
                    if (item.Id.HasValue) _removedFamilyMemberIds.Remove(item.Id.Value);
                }
                else
                {
                    if (item.IsNew)
                    {
                        // Completely delete if it was just added in this session
                        _dependents.Remove(item);
                    }
                    else
                    {
                        // Mark as removed and record ID
                        item.IsRemoved = true;
                        if (item.Id.HasValue && !_removedFamilyMemberIds.Contains(item.Id.Value))
                        {
                            _removedFamilyMemberIds.Add(item.Id.Value);
                        }
                    }
                }

                TxtNoDependents.Visibility = _dependents.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // =========================================================================
        // Step 3 Circumstance Conditional Display
        // =========================================================================

        // =========================================================================
        // Step 3 Circumstance Conditional Display
        // =========================================================================

        private void Circumstance_Changed(object sender, RoutedEventArgs e)
        {
            if (PnlA2 != null) PnlA2.Visibility = ChkA2.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (PnlA4 != null) PnlA4.Visibility = ChkA4.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (PnlA5 != null) PnlA5.Visibility = ChkA5.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (PnlA6 != null) PnlA6.Visibility = ChkA6.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (PnlB != null)  PnlB.Visibility  = ChkB.IsChecked  == true ? Visibility.Visible : Visibility.Collapsed;
        }

        // =========================================================================
        // Step 4 Review & Diff Calculation
        // =========================================================================

        private void BuildReviewDiff()
        {
            var diffs = new List<RenewalDiffRow>();

            // 1. Civil Status
            string oldCiv = !string.IsNullOrWhiteSpace(_original.CivilStatus) ? _original.CivilStatus.Trim() : "Single";
            string newCiv = (CmbCivilStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Single";
            if (!string.Equals(oldCiv, newCiv, StringComparison.OrdinalIgnoreCase))
            {
                diffs.Add(new RenewalDiffRow
                {
                    FieldName = "Civil Status",
                    OldValue = oldCiv,
                    NewValue = newCiv,
                    IsChanged = true
                });
            }

            // 2. Employment Status
            string oldEmpStatus = _original.EmploymentStatusDisplay;
            string newEmpStatus = (CmbEmploymentStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Employed";
            if (!string.Equals(oldEmpStatus.Trim(), newEmpStatus.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                diffs.Add(new RenewalDiffRow
                {
                    FieldName = "Employment Status",
                    OldValue = oldEmpStatus,
                    NewValue = newEmpStatus,
                    IsChanged = true
                });
            }

            // 3. Occupation
            string oldOcc = !string.IsNullOrWhiteSpace(_original.Occupation) ? _original.Occupation.Trim() : "—";
            string newOcc = TxtOccupation.Text.Trim();
            if (!string.Equals((oldOcc == "—" ? "" : oldOcc), newOcc, StringComparison.OrdinalIgnoreCase))
            {
                diffs.Add(new RenewalDiffRow
                {
                    FieldName = "Occupation",
                    OldValue = oldOcc,
                    NewValue = string.IsNullOrWhiteSpace(newOcc) ? "—" : newOcc,
                    IsChanged = true
                });
            }

            // 4. Monthly Income (compare numerical values to prevent currency formatting differences from triggering false diffs)
            decimal oldInc = 0;
            decimal.TryParse(_original.MonthlyIncome, out oldInc);
            decimal newInc = 0;
            decimal.TryParse(TxtMonthlyIncome.Text.Trim(), out newInc);
            if (oldInc != newInc)
            {
                diffs.Add(new RenewalDiffRow
                {
                    FieldName = "Monthly Income",
                    OldValue = $"₱ {oldInc:N2}",
                    NewValue = $"₱ {newInc:N2}",
                    IsChanged = true
                });
            }

            // 5. Other Sources of Income
            string oldOther = !string.IsNullOrWhiteSpace(_original.OtherIncomeSource) ? _original.OtherIncomeSource.Trim() : "None";
            string currentOther = !string.IsNullOrWhiteSpace(TxtOtherIncomeStep1.Text)
                ? TxtOtherIncomeStep1.Text.Trim()
                : TxtOtherIncomeStep3.Text.Trim();
            string newOther = !string.IsNullOrWhiteSpace(currentOther) ? currentOther : "None";
            if (!string.Equals(oldOther, newOther, StringComparison.OrdinalIgnoreCase))
            {
                diffs.Add(new RenewalDiffRow
                {
                    FieldName = "Other Income Sources",
                    OldValue = oldOther,
                    NewValue = newOther,
                    IsChanged = true
                });
            }

            // 6. Circumstance Ground
            string oldTag = !string.IsNullOrWhiteSpace(_dto?.ProblemPresented?.ProblemPresentedNumber)
                ? SoloParentApiService.ExtractCircumstanceCode(_dto.ProblemPresented.ProblemPresentedNumber)
                : SoloParentApiService.GetCircumstanceCode(_original);
            if (string.IsNullOrWhiteSpace(oldTag))
                oldTag = SoloParentApiService.ExtractCircumstanceCode(_original.ProblemPresented);

            string newTag = GetSelectedCircumstanceTag();
            string newCirc = $"{newTag}. {GetCircumstanceTitle(newTag)}";

            bool circGroundChanged = !string.Equals(oldTag, newTag, StringComparison.OrdinalIgnoreCase);

            // Check if subfields changed for the selected circumstance
            bool subfieldsChanged = false;
            if (!circGroundChanged)
            {
                if (newTag == "A2")
                {
                    string oldCause = (_dto?.ProblemPresented?.CauseOfDeath ?? _original.CircumstanceA2Cause ?? "").Trim();
                    string newCause = TxtA2Cause.Text.Trim();
                    string oldDate = DateTime.TryParse(_dto?.ProblemPresented?.date, out var oD)
                        ? oD.ToString("yyyy-MM-dd")
                        : (_original.CircumstanceA2Date != DateTime.MinValue ? _original.CircumstanceA2Date.ToString("yyyy-MM-dd") : "");
                    string newDate = DpA2Date.SelectedDate.HasValue ? DpA2Date.SelectedDate.Value.ToString("yyyy-MM-dd") : "";
                    subfieldsChanged = !string.Equals(oldCause, newCause, StringComparison.OrdinalIgnoreCase) ||
                                       !string.Equals(oldDate, newDate, StringComparison.OrdinalIgnoreCase);
                }
                else if (newTag == "A4")
                {
                    string oldDis = (_dto?.ProblemPresented?.TypeOfDisability ?? _original.CircumstanceA4Disability ?? "").Trim();
                    string newDis = TxtA4Disability.Text.Trim();
                    subfieldsChanged = !string.Equals(oldDis, newDis, StringComparison.OrdinalIgnoreCase);
                }
                else if (newTag == "A5")
                {
                    string oldPer = (_dto?.ProblemPresented?.PeriodOfSeparation ?? _original.CircumstanceA5Period ?? "").Trim();
                    string newPer = TxtA5Period.Text.Trim();
                    subfieldsChanged = !string.Equals(oldPer, newPer, StringComparison.OrdinalIgnoreCase);
                }
                else if (newTag == "A6")
                {
                    bool oldNull = _dto?.ProblemPresented?.isNullity ?? _original.CircumstanceA6Nullity;
                    bool newNull = GetComboValue(CmbA6Declaration) == "Nullity of marriage";
                    subfieldsChanged = oldNull != newNull;
                }
                else if (newTag == "B")
                {
                    decimal oldAbr = _dto?.ProblemPresented?.LengthOfAbroad ?? (decimal.TryParse(_original.CircumstanceBStayAbroad, out var oA) ? oA : 0);
                    decimal.TryParse(TxtBStayAbroad.Text.Trim(), out var newAbr);
                    subfieldsChanged = oldAbr != newAbr;
                }
            }

            _isCircumstanceChanged = circGroundChanged || subfieldsChanged;
            string displayOldCirc = !string.IsNullOrWhiteSpace(_original.ProblemPresented)
                ? _original.ProblemPresented
                : (!string.IsNullOrWhiteSpace(oldTag) ? $"{oldTag}. {GetCircumstanceTitle(oldTag)}" : "—");

            if (_isCircumstanceChanged)
            {
                diffs.Add(new RenewalDiffRow
                {
                    FieldName = "Circumstance Ground",
                    OldValue = displayOldCirc,
                    NewValue = newCirc,
                    IsChanged = true
                });
            }

            // 7. Needs and Problems
            string oldNeeds = !string.IsNullOrWhiteSpace(_original.NeedsAndProblems) ? _original.NeedsAndProblems.Trim() : "None specified";
            string newNeeds = !string.IsNullOrWhiteSpace(TxtNeeds.Text.Trim()) ? TxtNeeds.Text.Trim() : "None specified";
            if (!string.Equals(oldNeeds, newNeeds, StringComparison.OrdinalIgnoreCase))
            {
                diffs.Add(new RenewalDiffRow
                {
                    FieldName = "Needs & Problems",
                    OldValue = oldNeeds,
                    NewValue = newNeeds,
                    IsChanged = true
                });
            }

            // 8. Dependents changes (only list changed dependents)
            foreach (var d in _dependents.Where(x => x.IsNew && !x.IsRemoved))
            {
                diffs.Add(new RenewalDiffRow
                {
                    FieldName = "Dependent (Added)",
                    OldValue = "—",
                    NewValue = $"{d.Name} ({d.Relationship}, age {d.Age})",
                    IsChanged = true
                });
            }

            foreach (var d in _dependents.Where(x => x.IsRemoved))
            {
                diffs.Add(new RenewalDiffRow
                {
                    FieldName = "Dependent (Removed)",
                    OldValue = $"{d.Name} ({d.Relationship})",
                    NewValue = "Removed from household",
                    IsChanged = true
                });
            }

            foreach (var d in _dependents.Where(x => !x.IsNew && !x.IsRemoved && IsDependentChanged(x)))
            {
                diffs.Add(new RenewalDiffRow
                {
                    FieldName = $"Dependent ({d.Name})",
                    OldValue = GetOriginalDependentSummary(d),
                    NewValue = $"{d.EducationalLevel}, {d.IncomeDisplay}{(d.HasSpecialNeeds ? $", {d.SpecialNeedsCategory}" : "")}",
                    IsChanged = true
                });
            }

            ReviewDiffList.ItemsSource = diffs;

            bool hasChanges = diffs.Count > 0;
            if (DiffTableHeader != null) DiffTableHeader.Visibility = hasChanges ? Visibility.Visible : Visibility.Collapsed;
            if (DiffTableDivider != null) DiffTableDivider.Visibility = hasChanges ? Visibility.Visible : Visibility.Collapsed;
            if (PanelNoDiffs != null) PanelNoDiffs.Visibility = hasChanges ? Visibility.Collapsed : Visibility.Visible;

            // Dependents Summary
            int active = _dependents.Count(d => !d.IsRemoved);
            int added = _dependents.Count(d => d.IsNew && !d.IsRemoved);
            int removed = _dependents.Count(d => d.IsRemoved);

            TxtDependentsReconcileSummary.Text = $"Dependents: {active} active ({added} newly added, {removed} removed)";

            // Validity extension preview
            DateTime today = DateTime.Today;
            DateTime nextYear = today.AddYears(1);
            TxtNewValidityPreview.Text = $"New Validity Period: {today:MMMM d, yyyy} to {nextYear:MMMM d, yyyy}";

            BtnSubmitRenew.IsEnabled = true;
            BtnSubmitRenew.ToolTip = null;
        }

        private string GetOriginalDependentSummary(DependentItemViewModel d)
        {
            if (_original?.FamilyMembers != null)
            {
                var orig = _original.FamilyMembers.FirstOrDefault(fm => (fm.Id.HasValue && fm.Id == d.Id) || string.Equals(fm.MemberName, d.Name, StringComparison.OrdinalIgnoreCase));
                if (orig != null)
                {
                    string inc = decimal.TryParse(orig.Income, out var oi) ? $"₱ {oi:N2}" : orig.Income;
                    string sn = orig.HasSpecialNeeds ? $", {orig.SpecialNeedsCategory}" : "";
                    return $"{orig.EducationEmployment}, {inc}{sn}";
                }
            }
            return "Previous details";
        }

        private bool IsDependentChanged(DependentItemViewModel d)
        {
            if (d == null || d.IsNew || d.IsRemoved) return false;

            if (_dto?.FamilyMembers != null && _dto.FamilyMembers.Count > 0)
            {
                var orig = _dto.FamilyMembers.FirstOrDefault(fm => fm.Id == d.Id || string.Equals(fm.Name, d.Name, StringComparison.OrdinalIgnoreCase));
                if (orig != null)
                {
                    return !string.Equals((orig.Name ?? "").Trim(), (d.Name ?? "").Trim(), StringComparison.OrdinalIgnoreCase)
                        || !string.Equals((orig.Relationship ?? "").Trim(), (d.Relationship ?? "").Trim(), StringComparison.OrdinalIgnoreCase)
                        || !string.Equals((orig.Sex ?? "").Trim(), (d.Sex ?? "").Trim(), StringComparison.OrdinalIgnoreCase)
                        || orig.Birthdate.Date != d.Birthdate.Date
                        || !string.Equals((orig.EducationalLevel ?? "").Trim(), (d.EducationalLevel ?? "").Trim(), StringComparison.OrdinalIgnoreCase)
                        || orig.Income != d.Income
                        || orig.HasSpecialNeeds != d.HasSpecialNeeds
                        || !string.Equals((orig.SpecialNeedsCategory ?? "").Trim(), (d.SpecialNeedsCategory ?? "").Trim(), StringComparison.OrdinalIgnoreCase)
                        || !string.Equals((orig.SpecialNeedsDescription ?? "").Trim(), (d.SpecialNeedsDescription ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
                }
            }

            if (_original?.FamilyMembers != null && _original.FamilyMembers.Count > 0)
            {
                var orig = _original.FamilyMembers.FirstOrDefault(fm => (fm.Id.HasValue && fm.Id == d.Id) || string.Equals(fm.MemberName, d.Name, StringComparison.OrdinalIgnoreCase));
                if (orig != null)
                {
                    DateTime origDob = DateTime.TryParse(orig.Birthdate, out var parsedDob) ? parsedDob : DateTime.MinValue;
                    decimal origInc = 0;
                    decimal.TryParse(orig.Income, out origInc);

                    return !string.Equals((orig.MemberName ?? "").Trim(), (d.Name ?? "").Trim(), StringComparison.OrdinalIgnoreCase)
                        || !string.Equals((orig.Relationship ?? "").Trim(), (d.Relationship ?? "").Trim(), StringComparison.OrdinalIgnoreCase)
                        || !string.Equals((orig.Sex ?? "").Trim(), (d.Sex ?? "").Trim(), StringComparison.OrdinalIgnoreCase)
                        || (origDob != DateTime.MinValue && origDob.Date != d.Birthdate.Date)
                        || !string.Equals((orig.EducationEmployment ?? "").Trim(), (d.EducationalLevel ?? "").Trim(), StringComparison.OrdinalIgnoreCase)
                        || origInc != d.Income
                        || orig.HasSpecialNeeds != d.HasSpecialNeeds
                        || !string.Equals((orig.SpecialNeedsCategory ?? "").Trim(), (d.SpecialNeedsCategory ?? "").Trim(), StringComparison.OrdinalIgnoreCase)
                        || !string.Equals((orig.SpecialNeedsDescription ?? "").Trim(), (d.SpecialNeedsDescription ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        // =========================================================================
        // Step 4 Final Submission
        // =========================================================================

        private void BtnSubmitRenew_Click(object sender, RoutedEventArgs e)
        {
            if (!BtnSubmitRenew.IsEnabled) return;

            if (!_original.CanRenew(out string failureReason, out _))
            {
                ActionConfirmDialog.Warning(
                    "Renewal Not Allowed",
                    failureReason ?? "This solo parent record has been active for less than a month and cannot be renewed yet.",
                    this);
                return;
            }

            // Build RenewSoloParentRequest
            string civ = (CmbCivilStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Single";
            string empStatusTag = (CmbEmploymentStatus.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "employed";
            string occupation = TxtOccupation.Text.Trim();
            decimal monthlyIncome = 0;
            decimal.TryParse(TxtMonthlyIncome.Text.Trim(), out monthlyIncome);
            string otherIncome = !string.IsNullOrWhiteSpace(TxtOtherIncomeStep1.Text)
                ? TxtOtherIncomeStep1.Text.Trim()
                : TxtOtherIncomeStep3.Text.Trim();
            string needs = TxtNeeds.Text.Trim();

            // Circumstance
            string circTag = GetSelectedCircumstanceTag();
            string circName = GetCircumstanceTitle(circTag);

            decimal? lengthAbroad = null;
            if (decimal.TryParse(TxtBStayAbroad.Text.Trim(), out var lab)) lengthAbroad = lab;

            ProblemPresentedDetails problemPresented;
            if (_dto?.ProblemPresented != null && !_isCircumstanceChanged)
            {
                problemPresented = _dto.ProblemPresented;
            }
            else
            {
                problemPresented = new ProblemPresentedDetails
                {
                    ProblemPresentedNumber = circTag,
                    Name = circName,
                    CauseOfDeath = circTag == "A2" && !string.IsNullOrWhiteSpace(TxtA2Cause.Text) ? TxtA2Cause.Text.Trim() : null,
                    date = circTag == "A2" && DpA2Date.SelectedDate.HasValue ? DpA2Date.SelectedDate.Value.ToString("yyyy-MM-dd") : null,
                    TypeOfDisability = circTag == "A4" && !string.IsNullOrWhiteSpace(TxtA4Disability.Text) ? TxtA4Disability.Text.Trim() : null,
                    PeriodOfSeparation = circTag == "A5" && !string.IsNullOrWhiteSpace(TxtA5Period.Text) ? TxtA5Period.Text.Trim() : null,
                    LengthOfAbroad = circTag == "B" ? lengthAbroad : null,
                    isNullity = circTag == "A6" ? GetComboValue(CmbA6Declaration) == "Nullity of marriage" : (bool?)null,
                    isAnnulmentOfMarraige = circTag == "A6" ? GetComboValue(CmbA6Declaration) == "Annulment of marriage" : (bool?)null
                };
            }

            // Family members
            var renewFm = new RenewFamilyMembersRequest();
            var updatedFamilyList = new List<FamilyMember>();

            foreach (var d in _dependents)
            {
                if (d.IsRemoved) continue;

                if (d.Id.HasValue && !d.IsNew)
                {
                    renewFm.ExistingFamilyMembers.Add(new UpdateFamilyMemberRequest
                    {
                        Id = d.Id.Value,
                        Name = d.Name,
                        Relationship = d.Relationship,
                        Sex = d.Sex,
                        Birthdate = d.Birthdate,
                        Age = d.Age,
                        EducationalLevel = d.EducationalLevel,
                        Income = d.Income,
                        CivilStatus = d.CivilStatus,
                        HasSpecialNeeds = d.HasSpecialNeeds,
                        SpecialNeedsCategory = d.SpecialNeedsCategory,
                        SpecialNeedsDescription = d.SpecialNeedsDescription
                    });
                }
                else
                {
                    renewFm.NewFamilyMembers.Add(new CreateFamilyMemberRequest
                    {
                        Name = d.Name,
                        Relationship = d.Relationship,
                        Sex = d.Sex,
                        Birthdate = d.Birthdate,
                        Age = d.Age,
                        EducationalLevel = d.EducationalLevel,
                        Income = d.Income,
                        CivilStatus = d.CivilStatus,
                        HasSpecialNeeds = d.HasSpecialNeeds,
                        SpecialNeedsCategory = d.SpecialNeedsCategory,
                        SpecialNeedsDescription = d.SpecialNeedsDescription
                    });
                }

                updatedFamilyList.Add(new FamilyMember
                {
                    Id = d.Id,
                    IsNew = d.IsNew,
                    MemberName = d.Name,
                    Relationship = d.Relationship,
                    Sex = d.Sex,
                    Birthdate = d.Birthdate.ToString("yyyy-MM-dd"),
                    Age = d.Age.ToString(),
                    EducationEmployment = d.EducationalLevel,
                    Income = d.Income.ToString("N2"),
                    CivilStatus = d.CivilStatus,
                    HasSpecialNeeds = d.HasSpecialNeeds,
                    SpecialNeedsCategory = d.SpecialNeedsCategory,
                    SpecialNeedsDescription = d.SpecialNeedsDescription
                });
            }

            renewFm.RemovedFamilyMemberIds = new List<Guid>(_removedFamilyMemberIds);

            RenewRequest = new RenewSoloParentRequest
            {
                CivilStatus = civ,
                Employment = new EmploymentInfo
                {
                    EmploymentStatus = empStatusTag,
                    Occupation = occupation,
                    MonthlyIncome = monthlyIncome
                },
                FamilyMembers = renewFm,
                ProblemPresented = problemPresented,
                NeedsAndProblems = needs,
                OtherSourcesOfIncome = otherIncome
            };

            // Build UpdatedRecord model (copy all immutable fields to avoid false positive changes in audit trail)
            UpdatedRecord = new SoloParentRecord
            {
                Id = _original.Id,
                CreatedBy = _original.CreatedBy,
                LastUpdated = DateTime.Now,
                Status = "Active",
                IsRenewal = true,
                IsNewApplicant = false,
                CurrentTermExpiresAt = _isReassessmentMode ? (_original.CurrentTermExpiresAt ?? DateTime.Today.AddYears(1)) : DateTime.Today.AddYears(1),
                DateOfApplication = _original.DateOfApplication,
                RecordTerms = _original.RecordTerms,

                Name = _original.Name,
                Surname = _original.Surname,
                FirstName = _original.FirstName,
                MiddleName = _original.MiddleName,
                ExtensionName = _original.ExtensionName,
                DateOfBirth = _original.DateOfBirth,
                PlaceOfBirth = _original.PlaceOfBirth,
                Sex = _original.Sex,
                Citizenship = _original.Citizenship,
                BloodType = _original.BloodType,
                Height = _original.Height,
                Weight = _original.Weight,
                PhilSysNumber = _original.PhilSysNumber,
                Religion = _original.Religion,
                EducationalAttainment = _original.EducationalAttainment,

                Address = _original.Address,
                Barangay = _original.Barangay,
                ContactNumber = _original.ContactNumber,
                EmergencyContactName = _original.EmergencyContactName,
                EmergencyRelationship = _original.EmergencyRelationship,
                EmergencyAddress = _original.EmergencyAddress,
                EmergencyContactNumber = _original.EmergencyContactNumber,

                IsPantawidBeneficiary = _original.IsPantawidBeneficiary,
                IsIndigenous = _original.IsIndigenous,
                IsLGBT = _original.IsLGBT,

                // Updated fields
                CivilStatus = civ,
                Occupation = occupation,
                MonthlyIncome = monthlyIncome.ToString("N2"),
                IsEmployed = empStatusTag == "employed",
                IsSelfEmployed = empStatusTag == "self_employed",
                IsNotEmployed = empStatusTag == "not_employed",
                OtherIncomeSource = otherIncome,
                NeedsAndProblems = needs,
                ProblemPresented = circName,

                CircumstanceA1 = circTag == "A1",
                CircumstanceA2 = circTag == "A2",
                CircumstanceA2Cause = circTag == "A2" ? TxtA2Cause.Text.Trim() : string.Empty,
                CircumstanceA2Date = circTag == "A2" && DpA2Date.SelectedDate.HasValue ? DpA2Date.SelectedDate.Value : DateTime.MinValue,
                CircumstanceA3 = circTag == "A3",
                CircumstanceA4 = circTag == "A4",
                CircumstanceA4Disability = circTag == "A4" ? TxtA4Disability.Text.Trim() : string.Empty,
                CircumstanceA5 = circTag == "A5",
                CircumstanceA5Period = circTag == "A5" ? TxtA5Period.Text.Trim() : string.Empty,
                CircumstanceA6 = circTag == "A6",
                CircumstanceA6Nullity = circTag == "A6" && GetComboValue(CmbA6Declaration) == "Nullity of marriage",
                CircumstanceA6Annulment = circTag == "A6" && GetComboValue(CmbA6Declaration) == "Annulment of marriage",
                CircumstanceA7 = circTag == "A7",
                CircumstanceB = circTag == "B",
                CircumstanceBStayAbroad = circTag == "B" ? TxtBStayAbroad.Text.Trim() : string.Empty,
                CircumstanceC = circTag == "C",
                CircumstanceD = circTag == "D",
                CircumstanceE = circTag == "E",
                CircumstanceF = circTag == "F",

                FamilyMembers = updatedFamilyList,
                RemovedFamilyMemberIds = new List<Guid>(_removedFamilyMemberIds),
                Children = updatedFamilyList.Count,
                FamilyComposition = _original.FamilyComposition,
                AdditionalClassifications = _original.AdditionalClassifications
            };

            DialogResult = true;
            Close();
        }
    }
}
