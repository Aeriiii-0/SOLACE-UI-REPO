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
                }
            }
        }

        public string Name { get; set; }
        public string Relationship { get; set; }
        public string Sex { get; set; }
        public DateTime Birthdate { get; set; }
        public int Age { get; set; }
        public string EducationalLevel { get; set; }
        public decimal Income { get; set; }
        public string CivilStatus { get; set; }

        public string AgeDisplay => $"{Age} yrs old";
        public string IncomeDisplay => Income > 0 ? $"₱ {Income:N2}/mo" : "No income";

        public string StatusTag => IsRemoved ? "REMOVED" : (IsNew ? "NEW" : "ACTIVE");

        public Brush CardBg => IsRemoved
            ? new SolidColorBrush(Color.FromRgb(0xFE, 0xF2, 0xF2))
            : (IsNew ? new SolidColorBrush(Color.FromRgb(0xEF, 0xF6, 0xFF)) : new SolidColorBrush(Colors.White));

        public Brush BorderColor => IsRemoved
            ? new SolidColorBrush(Color.FromRgb(0xFE, 0xCA, 0xCA))
            : (IsNew ? new SolidColorBrush(Color.FromRgb(0xBF, 0xDB, 0xFE)) : new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB)));

        public Brush TextColor => IsRemoved
            ? new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF))
            : new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));

        public Brush TagBg => IsRemoved
            ? new SolidColorBrush(Color.FromRgb(0xFE, 0xCA, 0xCA))
            : (IsNew ? new SolidColorBrush(Color.FromRgb(0xDB, 0xEA, 0xFE)) : new SolidColorBrush(Color.FromRgb(0xD1, 0xFA, 0xE5)));

        public Brush TagFg => IsRemoved
            ? new SolidColorBrush(Color.FromRgb(0x99, 0x1B, 0x1B))
            : (IsNew ? new SolidColorBrush(Color.FromRgb(0x1E, 0x40, 0xAF)) : new SolidColorBrush(Color.FromRgb(0x06, 0x5F, 0x46)));

        public string ActionButtonText => IsRemoved ? "Undo" : "Remove";

        public Brush ActionButtonBg => IsRemoved
            ? new SolidColorBrush(Color.FromRgb(0xF3, 0xF4, 0xF6))
            : new SolidColorBrush(Color.FromRgb(0xFE, 0xF2, 0xF2));

        public Brush ActionButtonFg => IsRemoved
            ? new SolidColorBrush(Color.FromRgb(0x37, 0x41, 0x51))
            : new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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

        public RenewSoloParentRequest RenewRequest { get; private set; }
        public SoloParentRecord UpdatedRecord { get; private set; }

        private readonly ObservableCollection<DependentItemViewModel> _dependents = new ObservableCollection<DependentItemViewModel>();
        private readonly List<Guid> _removedFamilyMemberIds = new List<Guid>();
        private DependentItemViewModel _editingDependent = null;

        public RenewalReassessmentDialog(SoloParentRecord original, SoloParentDto dto = null)
        {
            InitializeComponent();
            _original = original ?? throw new ArgumentNullException(nameof(original));
            _dto = dto;

            DependentsList.ItemsSource = _dependents;

            Loaded += (s, e) =>
            {
                PopulateIdentity();
                PopulateStep1();
                PopulateStep2();
                PopulateStep3();
                UpdateStepView();
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9.]+$");
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

            TxtSoloParentId.Text = !string.IsNullOrWhiteSpace(_original.Id) ? _original.Id : "—";
            TxtPhilSysNumber.Text = !string.IsNullOrWhiteSpace(_original.PhilSysNumber) ? _original.PhilSysNumber : "None";
            TxtBarangay.Text = !string.IsNullOrWhiteSpace(_original.Barangay) ? _original.Barangay : "Biñan";

            string cat = !string.IsNullOrWhiteSpace(_original.ProblemPresented) ? _original.ProblemPresented : "Solo Parent";
            TxtCategoryBadge.Text = cat;

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
                        CivilStatus = fm.CivilStatus
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
                        CivilStatus = !string.IsNullOrWhiteSpace(fm.CivilStatus) ? fm.CivilStatus : "Single"
                    });
                }
            }

            TxtNoDependents.Visibility = _dependents.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PopulateStep3()
        {
            // Select Circumstance
            string pNum = _dto?.ProblemPresented?.ProblemPresentedNumber ?? string.Empty;
            if (string.IsNullOrWhiteSpace(pNum))
            {
                pNum = SoloParentApiService.GetCircumstanceCode(_original);
            }
            if (string.IsNullOrWhiteSpace(pNum))
            {
                pNum = SoloParentApiService.ExtractCircumstanceCode(_dto?.ProblemPresented?.Name);
            }

            bool matched = false;
            foreach (ComboBoxItem item in CmbCircumstance.Items)
            {
                string tag = item.Tag?.ToString();
                if (!string.IsNullOrWhiteSpace(tag) && string.Equals(tag, pNum, StringComparison.OrdinalIgnoreCase))
                {
                    CmbCircumstance.SelectedItem = item;
                    matched = true;
                    break;
                }
            }
            if (!matched && CmbCircumstance.Items.Count > 1) CmbCircumstance.SelectedIndex = 1; // Default A2 Death of Spouse

            // Conditional subfields
            TxtA2Cause.Text = _dto?.ProblemPresented?.CauseOfDeath ?? _original.CircumstanceA2Cause ?? string.Empty;
            if (DateTime.TryParse(_dto?.ProblemPresented?.date, out var a2Dt))
                DpA2Date.SelectedDate = a2Dt;
            else if (_original.CircumstanceA2Date != DateTime.MinValue)
                DpA2Date.SelectedDate = _original.CircumstanceA2Date;

            TxtA4Disability.Text = _dto?.ProblemPresented?.TypeOfDisability ?? _original.CircumstanceA4Disability ?? string.Empty;
            TxtA5Period.Text = _dto?.ProblemPresented?.PeriodOfSeparation ?? _original.CircumstanceA5Period ?? string.Empty;

            bool isNullity = _dto?.ProblemPresented?.isNullity ?? _original.CircumstanceA6Nullity;
            RdoNullity.IsChecked = isNullity;
            RdoAnnulment.IsChecked = !isNullity;

            decimal abroad = _dto?.ProblemPresented?.LengthOfAbroad ?? 0;
            TxtBStayAbroad.Text = abroad > 0 ? abroad.ToString("0") : (_original.CircumstanceBStayAbroad ?? string.Empty);

            // Needs & Problems
            TxtNeeds.Text = !string.IsNullOrWhiteSpace(_original.NeedsAndProblems)
                ? _original.NeedsAndProblems
                : (_dto?.NeedsAndProblems ?? string.Empty);
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
                    ActionConfirmDialog.Warning("Validation", "Please select a Civil Status.");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(TxtOccupation.Text))
                {
                    ActionConfirmDialog.Warning("Validation", "Please enter an Occupation or source of livelihood.");
                    TxtOccupation.Focus();
                    return false;
                }
                if (string.IsNullOrWhiteSpace(TxtMonthlyIncome.Text) || !decimal.TryParse(TxtMonthlyIncome.Text.Trim(), out _))
                {
                    ActionConfirmDialog.Warning("Validation", "Please enter a valid monthly income number.");
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
                if (CmbCircumstance.SelectedItem == null)
                {
                    ActionConfirmDialog.Warning("Validation", "Please select the primary Circumstance / Ground.");
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
            TxtChildName.Text = string.Empty;
            CmbChildRel.SelectedIndex = 0;
            CmbChildSex.SelectedIndex = 0;
            DpChildDob.SelectedDate = DateTime.Today.AddYears(-5);
            CmbChildEdu.SelectedIndex = 2; // Elementary
            TxtChildIncome.Text = "0.00";

            AddChildSubCard.Visibility = Visibility.Visible;
            TxtChildName.Focus();
        }

        private void BtnEditDependent_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is DependentItemViewModel item)
            {
                _editingDependent = item;
                TxtAddChildTitle.Text = $"Edit Dependent: {item.Name}";
                TxtChildName.Text = item.Name;

                // Relationship
                bool foundRel = false;
                foreach (ComboBoxItem cbi in CmbChildRel.Items)
                {
                    if (string.Equals(cbi.Content?.ToString(), item.Relationship, StringComparison.OrdinalIgnoreCase))
                    {
                        CmbChildRel.SelectedItem = cbi;
                        foundRel = true;
                        break;
                    }
                }
                if (!foundRel) CmbChildRel.SelectedIndex = 0;

                // Sex
                CmbChildSex.SelectedIndex = item.Sex.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                DpChildDob.SelectedDate = item.Birthdate;

                // Education
                bool foundEdu = false;
                foreach (ComboBoxItem cbi in CmbChildEdu.Items)
                {
                    if (string.Equals(cbi.Content?.ToString(), item.EducationalLevel, StringComparison.OrdinalIgnoreCase))
                    {
                        CmbChildEdu.SelectedItem = cbi;
                        foundEdu = true;
                        break;
                    }
                }
                if (!foundEdu) CmbChildEdu.SelectedIndex = 0;

                TxtChildIncome.Text = item.Income.ToString("N2");

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
            string name = TxtChildName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ActionConfirmDialog.Warning("Validation", "Please enter the dependent's full name.");
                TxtChildName.Focus();
                return;
            }

            DateTime dob = DpChildDob.SelectedDate ?? DateTime.Today.AddYears(-5);
            int age = DateTime.Today.Year - dob.Year;
            if (dob.Date > DateTime.Today.AddYears(-age)) age--;
            if (age < 0) age = 0;

            string rel = (CmbChildRel.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Child";
            string sex = (CmbChildSex.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Female";
            string edu = (CmbChildEdu.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "None";
            decimal income = 0;
            decimal.TryParse(TxtChildIncome.Text.Trim(), out income);

            if (_editingDependent != null)
            {
                _editingDependent.Name = name;
                _editingDependent.Relationship = rel;
                _editingDependent.Sex = sex;
                _editingDependent.Birthdate = dob;
                _editingDependent.Age = age;
                _editingDependent.EducationalLevel = edu;
                _editingDependent.Income = income;
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
                    CivilStatus = "Single"
                });
            }

            AddChildSubCard.Visibility = Visibility.Collapsed;
            _editingDependent = null;
            TxtNoDependents.Visibility = _dependents.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
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

        private void CmbCircumstance_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string tag = (CmbCircumstance.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;

            PanelSubA2.Visibility = tag == "A2" ? Visibility.Visible : Visibility.Collapsed;
            PanelSubA4.Visibility = tag == "A4" ? Visibility.Visible : Visibility.Collapsed;
            PanelSubA5.Visibility = tag == "A5" ? Visibility.Visible : Visibility.Collapsed;
            PanelSubA6.Visibility = tag == "A6" ? Visibility.Visible : Visibility.Collapsed;
            PanelSubB.Visibility = tag == "B" ? Visibility.Visible : Visibility.Collapsed;
        }

        // =========================================================================
        // Step 4 Review & Diff Calculation
        // =========================================================================

        private void BuildReviewDiff()
        {
            var diffs = new List<RenewalDiffRow>();

            // 1. Civil Status
            string oldCiv = _original.CivilStatus?.Trim() ?? "Single";
            string newCiv = (CmbCivilStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Single";
            diffs.Add(new RenewalDiffRow
            {
                FieldName = "Civil Status",
                OldValue = oldCiv,
                NewValue = newCiv,
                IsChanged = !string.Equals(oldCiv, newCiv, StringComparison.OrdinalIgnoreCase)
            });

            // 2. Employment Status
            string oldEmpStatus = _original.EmploymentStatusDisplay;
            string newEmpStatus = (CmbEmploymentStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Employed";
            diffs.Add(new RenewalDiffRow
            {
                FieldName = "Employment Status",
                OldValue = oldEmpStatus,
                NewValue = newEmpStatus,
                IsChanged = !string.Equals(oldEmpStatus, newEmpStatus, StringComparison.OrdinalIgnoreCase)
            });

            // 3. Occupation
            string oldOcc = !string.IsNullOrWhiteSpace(_original.Occupation) ? _original.Occupation : "—";
            string newOcc = TxtOccupation.Text.Trim();
            diffs.Add(new RenewalDiffRow
            {
                FieldName = "Occupation",
                OldValue = oldOcc,
                NewValue = newOcc,
                IsChanged = !string.Equals(oldOcc, newOcc, StringComparison.OrdinalIgnoreCase)
            });

            // 4. Monthly Income
            decimal oldInc = 0;
            decimal.TryParse(_original.MonthlyIncome, out oldInc);
            decimal newInc = 0;
            decimal.TryParse(TxtMonthlyIncome.Text.Trim(), out newInc);
            diffs.Add(new RenewalDiffRow
            {
                FieldName = "Monthly Income",
                OldValue = $"₱ {oldInc:N2}",
                NewValue = $"₱ {newInc:N2}",
                IsChanged = oldInc != newInc
            });

            // 5. Other Sources of Income
            string oldOther = !string.IsNullOrWhiteSpace(_original.OtherIncomeSource) ? _original.OtherIncomeSource : "None";
            string currentOther = !string.IsNullOrWhiteSpace(TxtOtherIncomeStep1.Text)
                ? TxtOtherIncomeStep1.Text.Trim()
                : TxtOtherIncomeStep3.Text.Trim();
            string newOther = !string.IsNullOrWhiteSpace(currentOther) ? currentOther : "None";
            diffs.Add(new RenewalDiffRow
            {
                FieldName = "Other Income Sources",
                OldValue = oldOther,
                NewValue = newOther,
                IsChanged = !string.Equals(oldOther, newOther, StringComparison.OrdinalIgnoreCase)
            });

            // 6. Circumstance Ground
            string oldTag = !string.IsNullOrWhiteSpace(_dto?.ProblemPresented?.ProblemPresentedNumber)
                ? SoloParentApiService.ExtractCircumstanceCode(_dto.ProblemPresented.ProblemPresentedNumber)
                : SoloParentApiService.GetCircumstanceCode(_original);
            if (string.IsNullOrWhiteSpace(oldTag))
                oldTag = SoloParentApiService.ExtractCircumstanceCode(_original.ProblemPresented);

            string newTag = (CmbCircumstance.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            string newCirc = (CmbCircumstance.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "—";

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
                    bool newNull = RdoNullity.IsChecked == true;
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
            string displayOldCirc = _isCircumstanceChanged
                ? (!string.IsNullOrWhiteSpace(_original.ProblemPresented) ? _original.ProblemPresented : (oldTag.Length > 0 ? oldTag : "—"))
                : newCirc;

            diffs.Add(new RenewalDiffRow
            {
                FieldName = "Circumstance Ground",
                OldValue = displayOldCirc,
                NewValue = newCirc,
                IsChanged = _isCircumstanceChanged
            });

            // 7. Needs and Problems
            string oldNeeds = !string.IsNullOrWhiteSpace(_original.NeedsAndProblems) ? _original.NeedsAndProblems : "None specified";
            string newNeeds = !string.IsNullOrWhiteSpace(TxtNeeds.Text.Trim()) ? TxtNeeds.Text.Trim() : "None specified";
            diffs.Add(new RenewalDiffRow
            {
                FieldName = "Needs & Problems",
                OldValue = oldNeeds,
                NewValue = newNeeds,
                IsChanged = !string.Equals(oldNeeds, newNeeds, StringComparison.OrdinalIgnoreCase)
            });

            ReviewDiffList.ItemsSource = diffs;

            // Dependents Summary
            int active = _dependents.Count(d => !d.IsRemoved);
            int added = _dependents.Count(d => d.IsNew && !d.IsRemoved);
            int removed = _dependents.Count(d => d.IsRemoved);
            bool dependentsModified = _dependents.Any(d => !d.IsNew && !d.IsRemoved && IsDependentChanged(d));

            TxtDependentsReconcileSummary.Text = $"Dependents: {active} active ({added} newly added, {removed} removed)";

            // Validity extension preview
            DateTime today = DateTime.Today;
            DateTime nextYear = today.AddYears(1);
            TxtNewValidityPreview.Text = $"New Validity Period: {today:MMMM d, yyyy} to {nextYear:MMMM d, yyyy}";

            bool hasAnyChanges = diffs.Any(d => d.IsChanged) || added > 0 || removed > 0 || dependentsModified;
            BtnSubmitRenew.IsEnabled = hasAnyChanges;
            BtnSubmitRenew.ToolTip = hasAnyChanges
                ? null
                : "No changes were made. Modify at least one field or cancel to use Quick Renewal.";
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
                        || orig.Income != d.Income;
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
                        || origInc != d.Income;
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
            string circTag = (CmbCircumstance.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "A2";
            string rawCircName = (CmbCircumstance.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Death of spouse";
            string circName = rawCircName.Contains("·") ? rawCircName.Substring(rawCircName.IndexOf("·") + 1).Trim() : rawCircName;

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
                    isNullity = circTag == "A6" ? RdoNullity.IsChecked == true : (bool?)null,
                    isAnnulmentOfMarraige = circTag == "A6" ? RdoAnnulment.IsChecked == true : (bool?)null
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
                        CivilStatus = d.CivilStatus
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
                        CivilStatus = d.CivilStatus
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
                    CivilStatus = d.CivilStatus
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

            // Build UpdatedRecord model
            UpdatedRecord = new SoloParentRecord
            {
                Id = _original.Id,
                CreatedBy = _original.CreatedBy,
                LastUpdated = DateTime.Now,
                Status = "Active",
                IsRenewal = true,
                IsNewApplicant = false,
                CurrentTermExpiresAt = DateTime.Today.AddYears(1),

                Surname = _original.Surname,
                FirstName = _original.FirstName,
                MiddleName = _original.MiddleName,
                ExtensionName = _original.ExtensionName,
                DateOfBirth = _original.DateOfBirth,
                PlaceOfBirth = _original.PlaceOfBirth,
                Sex = _original.Sex,
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
                CircumstanceA2Cause = problemPresented.CauseOfDeath,
                CircumstanceA2Date = DateTime.TryParse(problemPresented.date, out var a2DtVal) ? a2DtVal : DateTime.MinValue,
                CircumstanceA3 = circTag == "A3",
                CircumstanceA4 = circTag == "A4",
                CircumstanceA4Disability = problemPresented.TypeOfDisability,
                CircumstanceA5 = circTag == "A5",
                CircumstanceA5Period = problemPresented.PeriodOfSeparation,
                CircumstanceA6 = circTag == "A6",
                CircumstanceA6Nullity = problemPresented.isNullity == true,
                CircumstanceA6Annulment = problemPresented.isAnnulmentOfMarraige == true,
                CircumstanceA7 = circTag == "A7",
                CircumstanceB = circTag == "B",
                CircumstanceBStayAbroad = TxtBStayAbroad.Text.Trim(),
                CircumstanceC = circTag == "C",
                CircumstanceD = circTag == "D",
                CircumstanceE = circTag == "E",
                CircumstanceF = circTag == "F",

                FamilyMembers = updatedFamilyList,
                RemovedFamilyMemberIds = new List<Guid>(_removedFamilyMemberIds)
            };

            DialogResult = true;
            Close();
        }
    }
}
