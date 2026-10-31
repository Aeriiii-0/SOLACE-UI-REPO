using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SOLUM_UI.Models;
using SOLUM_UI.Services;
using SOLUM_UI.Views.Dialogs;

namespace SOLUM_UI.Views.Components
{
    public partial class OcrFamilyMembersComponent : UserControl
    {
        public event Action<string> FieldFocused;

        private readonly ObservableCollection<FamilyMemberRow> _familyRowData = new ObservableCollection<FamilyMemberRow>();
        private FamilyMemberRow _editingFamilyMember = null;
        private bool _isPopulating = false;
        public bool IsPopulating => _isPopulating;

        private static readonly SolidColorBrush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
        private static readonly SolidColorBrush DefaultBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xB8, 0xC0));
        private static readonly Thickness DefaultThickness = new Thickness(1);

        private static readonly Regex NameRegex = new Regex(@"^[a-zA-ZñÑ\s\.\-']+$", RegexOptions.Compiled);
        private static readonly Regex NumberOnlyRegex = new Regex(@"^\d+$", RegexOptions.Compiled);

        static OcrFamilyMembersComponent()
        {
            ErrorBrush.Freeze();
            DefaultBrush.Freeze();
        }

        public OcrFamilyMembersComponent()
        {
            InitializeComponent();

            FamilyCardsList.ItemsSource = _familyRowData;
            _familyRowData.CollectionChanged += (s, e) => UpdateFamilyCardsEmptyState();
            UpdateFamilyCardsEmptyState();
        }

        public ObservableCollection<FamilyMemberRow> FamilyRowData => _familyRowData;

        public bool CircumstanceA1 => ChkA1.IsChecked == true && ChkA1.IsEnabled;
        public bool CircumstanceA2 => ChkA2.IsChecked == true && ChkA2.IsEnabled;
        public string CircumstanceA2Cause => ChkA2.IsChecked == true ? TxtA2Cause.Text.Trim() : string.Empty;
        public DateTime CircumstanceA2Date => ChkA2.IsChecked == true ? (DpA2Date.SelectedDate ?? DateTime.MinValue) : DateTime.MinValue;
        public bool CircumstanceA3 => ChkA3.IsChecked == true && ChkA3.IsEnabled;
        public bool CircumstanceA4 => ChkA4.IsChecked == true && ChkA4.IsEnabled;
        public string CircumstanceA4Disability => ChkA4.IsChecked == true ? TxtA4Disability.Text.Trim() : string.Empty;
        public bool CircumstanceA5 => ChkA5.IsChecked == true && ChkA5.IsEnabled;
        public string CircumstanceA5Period => ChkA5.IsChecked == true ? TxtA5Period.Text.Trim() : string.Empty;
        public bool CircumstanceA6 => ChkA6.IsChecked == true && ChkA6.IsEnabled;
        public bool CircumstanceA6Nullity => ChkA6.IsChecked == true && GetComboValue(CmbA6Declaration) == "Nullity of marriage";
        public bool CircumstanceA6Annulment => ChkA6.IsChecked == true && GetComboValue(CmbA6Declaration) == "Annulment of marriage";
        public bool CircumstanceA7 => ChkA7.IsChecked == true && ChkA7.IsEnabled;
        public bool CircumstanceB => ChkB.IsChecked == true && ChkB.IsEnabled;
        public string CircumstanceBStayAbroad => ChkB.IsChecked == true ? TxtBStayAbroad.Text.Trim() : string.Empty;
        public bool CircumstanceC => ChkC.IsChecked == true && ChkC.IsEnabled;
        public bool CircumstanceD => ChkD.IsChecked == true && ChkD.IsEnabled;
        public bool CircumstanceE => ChkE.IsChecked == true && ChkE.IsEnabled;
        public bool CircumstanceF => ChkF.IsChecked == true && ChkF.IsEnabled;

        public string NeedsAndProblems => TxtNeeds.Text.Trim();
        public string OtherIncomeSource => TxtOtherIncome.Text.Trim();
        public string ApplicationStatus => GetComboValue(CmbStatus);

        #region Focus & Navigation Handling

        private void Field_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is string tag && !string.IsNullOrEmpty(tag))
            {
                FieldFocused?.Invoke(ResolveDynamicTag(elem, tag));
            }
        }

        private void Field_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is string tag && !string.IsNullOrEmpty(tag))
            {
                FieldFocused?.Invoke(ResolveDynamicTag(elem, tag));
            }
        }

        private string ResolveDynamicTag(FrameworkElement elem, string tag)
        {
            if (tag.StartsWith("fam_row", StringComparison.OrdinalIgnoreCase) && elem.DataContext is FamilyMemberRow row)
            {
                int rIndex = _familyRowData.IndexOf(row);
                if (rIndex >= 0)
                {
                    var parts = tag.Split('_');
                    string col = parts[parts.Length - 1];
                    return $"fam_row{rIndex + 1}_{col}";
                }
            }
            return tag;
        }

        #endregion

        #region Population & Data Binding

        public void Populate(OcrFormResponse resp)
        {
            if (resp == null) return;
            _isPopulating = true;
            try
            {
                // Circumstances
                if (resp.Circumstance != null && resp.Circumstance.Detected && !string.IsNullOrEmpty(resp.Circumstance.Code))
                {
                    if (BadgeCircumstanceDetected != null && TxtCircumstanceBadge != null)
                    {
                        BadgeCircumstanceDetected.Visibility = Visibility.Visible;
                        int cConf = (int)(resp.Circumstance.Confidence * 100);
                        TxtCircumstanceBadge.Text = $"● Detected: {resp.Circumstance.Code} ({cConf}%)";
                    }

                    string code = resp.Circumstance.Code.ToUpper();
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

                    if (code == "A1") ChkA1.IsChecked = true;
                    else if (code == "A2") ChkA2.IsChecked = true;
                    else if (code == "A3") ChkA3.IsChecked = true;
                    else if (code == "A4") ChkA4.IsChecked = true;
                    else if (code == "A5") ChkA5.IsChecked = true;
                    else if (code == "A6") ChkA6.IsChecked = true;
                    else if (code == "A7") ChkA7.IsChecked = true;
                    else if (code == "B") ChkB.IsChecked = true;
                    else if (code == "C") ChkC.IsChecked = true;
                    else if (code == "D") ChkD.IsChecked = true;
                    else if (code == "E") ChkE.IsChecked = true;
                    else if (code == "F") ChkF.IsChecked = true;

                    if (ChkA2.IsChecked == true)
                    {
                        TxtA2Cause.Text = (resp.Circumstance.SubfieldCause ?? string.Empty).ToUpperInvariant();
                        if (OcrService.TryParseOcrDate(resp.Circumstance.SubfieldDate, out var dDate))
                            DpA2Date.SelectedDate = dDate;
                    }
                    if (ChkA4.IsChecked == true)
                    {
                        TxtA4Disability.Text = (resp.Circumstance.SubfieldDisability ?? string.Empty).ToUpperInvariant();
                    }
                    if (ChkA5.IsChecked == true)
                    {
                        TxtA5Period.Text = (resp.Circumstance.SubfieldPeriod ?? string.Empty).ToUpperInvariant();
                    }
                    if (ChkA6.IsChecked == true && CmbA6Declaration != null)
                    {
                        string subcode = resp.Circumstance.SubfieldCause?.ToLower() ?? string.Empty;
                        if (subcode.Contains("nullity")) SetComboByContent(CmbA6Declaration, "Nullity of marriage");
                        else if (subcode.Contains("annul")) SetComboByContent(CmbA6Declaration, "Annulment of marriage");
                        else CmbA6Declaration.SelectedIndex = 0;
                    }
                    if (ChkB.IsChecked == true)
                    {
                        TxtBStayAbroad.Text = (resp.Circumstance.SubfieldStayAbroad ?? string.Empty).ToUpperInvariant();
                    }
                }
                else
                {
                    if (BadgeCircumstanceDetected != null) BadgeCircumstanceDetected.Visibility = Visibility.Collapsed;
                }
                Circumstance_Changed(null, null);

                // Family Composition Table
                _familyRowData.Clear();
                if (resp.FamilyMembers != null && resp.FamilyMembers.Count > 0)
                {
                    foreach (var m in resp.FamilyMembers)
                    {
                        if (string.IsNullOrWhiteSpace(m.MemberName) || OcrService.IsOcrNotApplicable(m.MemberName))
                            continue;

                        DateTime? dt = null;
                        string normDob = OcrService.IsOcrNotApplicable(m.Birthdate) ? string.Empty : OcrService.NormalizeDateString(m.Birthdate, out dt);
                        string ageVal = string.Empty;
                        if (dt.HasValue && dt.Value != DateTime.MinValue)
                        {
                            int calcAge = OcrService.CalculateAge(dt.Value);
                            if (calcAge >= 0 && calcAge <= 120)
                            {
                                ageVal = calcAge.ToString();
                            }
                        }
                        if (string.IsNullOrWhiteSpace(ageVal))
                        {
                            ageVal = OcrService.IsOcrNotApplicable(m.Age) ? string.Empty : m.Age;
                        }

                        var ocrRow = MakeFamilyRow();
                        ocrRow.MemberName = OcrService.CleanPersonName(m.MemberName);
                        ocrRow.Sex = OcrService.IsOcrNotApplicable(m.Sex) ? string.Empty : OcrService.FuzzyMatchSex(m.Sex);
                        ocrRow.CivilStatus = OcrService.IsOcrNotApplicable(m.CivilStatus) ? string.Empty : OcrService.FuzzyMatchCivilStatus(m.CivilStatus);
                        ocrRow.Relationship = OcrService.IsOcrNotApplicable(m.Relationship) ? string.Empty : (OcrService.FuzzyMatchRelationship(m.Relationship)?.ToUpperInvariant() ?? string.Empty);
                        ocrRow.EducationEmployment = (OcrService.IsOcrNotApplicable(m.EducationEmployment) ? string.Empty : m.EducationEmployment)?.ToUpperInvariant() ?? string.Empty;
                        ocrRow.Income = OcrService.IsOcrNotApplicable(m.Income) ? "0" : OcrService.FuzzyMatchIncome(m.Income);
                        if (dt.HasValue && dt.Value != DateTime.MinValue)
                            ocrRow.BirthdateDate = dt.Value;
                        else if (!string.IsNullOrWhiteSpace(normDob))
                            ocrRow.Birthdate = normDob;
                        _familyRowData.Add(ocrRow);
                    }
                }

                UpdateFamilyCardsEmptyState();

                // Needs & Other Sources of Income
                TxtNeeds.Text = GetVal(resp, "needs")?.ToUpperInvariant() ?? string.Empty;
                TxtOtherIncome.Text = GetVal(resp, "other_income")?.ToUpperInvariant() ?? string.Empty;
                SetFieldConfidence("needs", Badge_Needs, TxtConf_Needs, resp);
                SetFieldConfidence("other_income", Badge_OtherIncome, TxtConf_OtherIncome, resp);

                SetComboByContent(CmbStatus, "Valid");
            }
            finally
            {
                _isPopulating = false;
            }
        }

        private string GetVal(OcrFormResponse resp, string key)
        {
            if (resp?.Fields != null && resp.Fields.TryGetValue(key, out var f))
            {
                string val = f.Value ?? string.Empty;
                if (OcrService.IsOcrNotApplicable(val))
                    return string.Empty;
                return val;
            }
            return string.Empty;
        }

        private void SetFieldConfidence(string key, Border badge, TextBlock txtBadge, OcrFormResponse resp)
        {
            if (badge == null || txtBadge == null) return;

            if (resp?.Fields != null && resp.Fields.TryGetValue(key, out var f) && f != null && !string.IsNullOrWhiteSpace(f.Value))
            {
                int confPct = (int)(f.Confidence * 100);
                txtBadge.Text = $"{confPct}%";
                badge.Visibility = Visibility.Visible;

                if (confPct >= 80)
                {
                    badge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6F9EE"));
                    txtBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                }
                else if (confPct >= 50)
                {
                    badge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF4E5"));
                    txtBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D35400"));
                }
                else
                {
                    badge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDEDEC"));
                    txtBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0392B"));
                }
            }
            else
            {
                badge.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Circumstances Handling

        private void Circumstance_Changed(object sender, RoutedEventArgs e)
        {
            if (PnlA2 != null) PnlA2.Visibility = ChkA2.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (PnlA4 != null) PnlA4.Visibility = ChkA4.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (PnlA5 != null) PnlA5.Visibility = ChkA5.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (PnlA6 != null) PnlA6.Visibility = ChkA6.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (PnlB != null) PnlB.Visibility = ChkB.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Family Composition Add / Edit / Remove

        private void UpdateFamilyCardsEmptyState()
        {
            if (PnlNoFamilyMembers != null)
            {
                PnlNoFamilyMembers.Visibility = (_familyRowData == null || _familyRowData.Count == 0)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void BtnAddFamilyMember_Click(object sender, RoutedEventArgs e)
        {
            _editingFamilyMember = null;
            TxtAddMemberTitle.Text = "Add Household Member";
            BtnSaveMember.Content = "Add Member";

            TxtMemberName.Text = string.Empty;
            CmbMemberRel.SelectedIndex = -1;
            CmbMemberSex.SelectedIndex = -1;
            CmbMemberCivil.SelectedIndex = -1;
            DpMemberDob.SelectedDate = null;
            TxtMemberAge.Text = string.Empty;
            TxtMemberEdu.Text = string.Empty;
            TxtMemberIncome.Text = string.Empty;
            ChkMemberSpecialNeeds.IsChecked = false;
            CmbSpecialNeedsCategory.SelectedIndex = -1;
            TxtSpecialNeedsDescription.Text = string.Empty;
            if (PnlSpecialNeedsDetails != null)
            {
                PnlSpecialNeedsDetails.IsEnabled = false;
                PnlSpecialNeedsDetails.Opacity = 0.5;
            }

            AddMemberSubCard.Visibility = Visibility.Visible;
            TxtMemberName.Focus();
        }

        private void BtnEditFamilyMember_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is FamilyMemberRow row)
            {
                _editingFamilyMember = row;
                TxtAddMemberTitle.Text = $"Edit Household Member: {row.MemberName}";
                BtnSaveMember.Content = "Update Member";

                TxtMemberName.Text = row.MemberName;
                SetComboByContent(CmbMemberRel, row.Relationship);
                SetComboByContent(CmbMemberSex, row.Sex);
                SetComboByContent(CmbMemberCivil, row.CivilStatus);
                DpMemberDob.SelectedDate = row.BirthdateDate;
                TxtMemberAge.Text = row.Age;
                TxtMemberEdu.Text = row.EducationEmployment;
                TxtMemberIncome.Text = row.Income;

                bool hasSn = row.HasSpecialNeeds;
                ChkMemberSpecialNeeds.IsChecked = hasSn;
                if (PnlSpecialNeedsDetails != null)
                {
                    PnlSpecialNeedsDetails.IsEnabled = hasSn;
                    PnlSpecialNeedsDetails.Opacity = hasSn ? 1.0 : 0.5;
                }
                SetComboByContent(CmbSpecialNeedsCategory, row.SpecialNeedsCategory);
                TxtSpecialNeedsDescription.Text = row.SpecialNeedsDescription ?? string.Empty;

                AddMemberSubCard.Visibility = Visibility.Visible;
                TxtMemberName.Focus();
            }
        }

        private void BtnRemoveFamilyMember_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is FamilyMemberRow row)
            {
                bool confirm = ActionConfirmDialog.Show(
                    "Remove Household Member",
                    $"Are you sure you want to remove {row.MemberName} from the family composition?",
                    "Remove",
                    "Keep Member",
                    ConfirmThemeType.Danger,
                    Window.GetWindow(this));

                if (confirm)
                {
                    _familyRowData.Remove(row);
                    if (_editingFamilyMember == row)
                    {
                        AddMemberSubCard.Visibility = Visibility.Collapsed;
                        _editingFamilyMember = null;
                    }
                    UpdateFamilyCardsEmptyState();
                }
            }
        }

        private void BtnCancelMember_Click(object sender, RoutedEventArgs e)
        {
            AddMemberSubCard.Visibility = Visibility.Collapsed;
            _editingFamilyMember = null;
        }

        private void BtnSaveMember_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            string name = TxtMemberName.Text?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(name))
            {
                ActionConfirmDialog.Warning("Validation", "Please enter the member's full name.", window);
                TxtMemberName.Focus();
                return;
            }

            string rel = (CmbMemberRel.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(rel))
            {
                ActionConfirmDialog.Warning("Validation", "Please select a relationship.", window);
                CmbMemberRel.Focus();
                return;
            }

            string sex = (CmbMemberSex.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(sex))
            {
                ActionConfirmDialog.Warning("Validation", "Please select the member's sex.", window);
                CmbMemberSex.Focus();
                return;
            }

            if (!DpMemberDob.SelectedDate.HasValue)
            {
                ActionConfirmDialog.Warning("Validation", "Please select a valid birthdate.", window);
                DpMemberDob.Focus();
                return;
            }

            DateTime dob = DpMemberDob.SelectedDate.Value;
            int age = OcrService.CalculateAge(dob);
            if (age < 0 || age > 120) age = 0;

            string civil = (CmbMemberCivil.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(civil)) civil = "Single";

            string edu = (TxtMemberEdu.Text?.Trim() ?? string.Empty).ToUpperInvariant();
            string income = TxtMemberIncome.Text?.Trim();
            if (string.IsNullOrWhiteSpace(income)) income = "0.00";

            bool hasSn = ChkMemberSpecialNeeds.IsChecked == true;
            string snCat = hasSn ? ((CmbSpecialNeedsCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty) : string.Empty;
            if (hasSn && string.IsNullOrWhiteSpace(snCat))
            {
                ActionConfirmDialog.Warning("Validation", "Please select a special needs category.", window);
                CmbSpecialNeedsCategory.Focus();
                return;
            }
            string snDesc = hasSn ? (TxtSpecialNeedsDescription.Text?.Trim() ?? string.Empty).ToUpperInvariant() : string.Empty;

            if (_editingFamilyMember != null)
            {
                _editingFamilyMember.MemberName = name;
                _editingFamilyMember.Relationship = rel;
                _editingFamilyMember.Sex = sex;
                _editingFamilyMember.CivilStatus = civil;
                _editingFamilyMember.BirthdateDate = dob;
                _editingFamilyMember.Age = age.ToString();
                _editingFamilyMember.EducationEmployment = edu;
                _editingFamilyMember.Income = income;
                _editingFamilyMember.HasSpecialNeeds = hasSn;
                _editingFamilyMember.SpecialNeedsCategory = snCat;
                _editingFamilyMember.SpecialNeedsDescription = snDesc;
                _editingFamilyMember.RefreshAll();
                FamilyCardsList?.Items.Refresh();
            }
            else
            {
                var row = MakeFamilyRow();
                row.IsNew = true;
                row.MemberName = name;
                row.Relationship = rel;
                row.Sex = sex;
                row.CivilStatus = civil;
                row.BirthdateDate = dob;
                row.Age = age.ToString();
                row.EducationEmployment = edu;
                row.Income = income;
                row.HasSpecialNeeds = hasSn;
                row.SpecialNeedsCategory = snCat;
                row.SpecialNeedsDescription = snDesc;
                _familyRowData.Add(row);
            }

            AddMemberSubCard.Visibility = Visibility.Collapsed;
            _editingFamilyMember = null;
            UpdateFamilyCardsEmptyState();
        }

        private void DpMemberDob_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DpMemberDob.SelectedDate.HasValue)
            {
                int age = OcrService.CalculateAge(DpMemberDob.SelectedDate.Value);
                TxtMemberAge.Text = (age >= 0 && age <= 120) ? age.ToString() : "0";
            }
            else
            {
                TxtMemberAge.Text = string.Empty;
            }
        }

        private void ChkMemberSpecialNeeds_Changed(object sender, RoutedEventArgs e)
        {
            if (PnlSpecialNeedsDetails != null)
            {
                bool isSpecial = ChkMemberSpecialNeeds.IsChecked == true;
                PnlSpecialNeedsDetails.IsEnabled = isSpecial;
                PnlSpecialNeedsDetails.Opacity = isSpecial ? 1.0 : 0.5;
                if (!isSpecial)
                {
                    if (CmbSpecialNeedsCategory != null) CmbSpecialNeedsCategory.SelectedIndex = -1;
                    if (TxtSpecialNeedsDescription != null) TxtSpecialNeedsDescription.Text = string.Empty;
                }
            }
        }

        private FamilyMemberRow MakeFamilyRow()
        {
            var row = new FamilyMemberRow();
            row.DeleteCommand = new RelayCommand(() =>
            {
                _familyRowData.Remove(row);
                UpdateFamilyCardsEmptyState();
            });
            return row;
        }

        public List<FamilyMember> BuildFamilyMembers()
        {
            var members = new List<FamilyMember>();
            foreach (var item in _familyRowData)
            {
                var row = item as FamilyMemberRow;
                if (row != null && !string.IsNullOrWhiteSpace(row.MemberName) && !OcrService.IsOcrNotApplicable(row.MemberName))
                {
                    string normDob = OcrService.NormalizeDateString(row.Birthdate, out DateTime? dt);
                    string ageVal = row.Age;
                    if (dt.HasValue && dt.Value != DateTime.MinValue)
                    {
                        int calcAge = OcrService.CalculateAge(dt.Value);
                        if (calcAge >= 0 && calcAge <= 120)
                        {
                            ageVal = calcAge.ToString();
                        }
                    }

                    members.Add(new FamilyMember
                    {
                        MemberName = OcrService.CleanPersonName(row.MemberName),
                        Sex = row.OfficialSex,
                        Age = ageVal,
                        Birthdate = string.IsNullOrWhiteSpace(normDob) ? row.Birthdate : normDob,
                        CivilStatus = row.CivilStatus,
                        Relationship = row.Relationship,
                        EducationEmployment = row.EducationEmployment,
                        Income = row.Income,
                        HasSpecialNeeds = row.HasSpecialNeeds,
                        SpecialNeedsCategory = row.SpecialNeedsCategory,
                        SpecialNeedsDescription = row.SpecialNeedsDescription
                    });
                }
            }
            return members;
        }

        #endregion

        #region Formatters & ComboBox Helpers

        private void NameOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !NameRegex.IsMatch(e.Text);
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !NumberOnlyRegex.IsMatch(e.Text);
        }

        private void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearFieldError(CmbStatus, ErrStatus);
        }

        private void SetComboByContent(ComboBox combo, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || combo == null) return;
            string val = value.Trim();

            foreach (ComboBoxItem item in combo.Items)
            {
                string text = item.Content?.ToString();
                if (string.Equals(text, val, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedItem = item;
                    return;
                }
            }

            string valNorm = Regex.Replace(val.ToLowerInvariant(), @"[^a-z0-9]", "");
            foreach (ComboBoxItem item in combo.Items)
            {
                string text = item.Content?.ToString() ?? "";
                string textNorm = Regex.Replace(text.ToLowerInvariant(), @"[^a-z0-9]", "");
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
            ComboBoxItem sel = combo?.SelectedItem as ComboBoxItem;
            return sel?.Content?.ToString() ?? string.Empty;
        }

        #endregion

        #region Validation

        public bool Validate(out List<string> missing)
        {
            bool ok = true;
            missing = new List<string>();

            int rowNum = 0;
            foreach (var item in _familyRowData)
            {
                var row = item as FamilyMemberRow;
                if (row == null || string.IsNullOrWhiteSpace(row.MemberName)) continue;
                rowNum++;

                var rowMissing = new List<string>();
                if (string.IsNullOrWhiteSpace(row.Sex)) rowMissing.Add("Sex");
                if (!row.BirthdateDate.HasValue) rowMissing.Add("Birthdate");
                if (string.IsNullOrWhiteSpace(row.CivilStatus)) rowMissing.Add("Civil Status");
                if (string.IsNullOrWhiteSpace(row.Relationship)) rowMissing.Add("Relationship");
                if (string.IsNullOrWhiteSpace(row.EducationEmployment)) rowMissing.Add("Education/Employment");
                if (string.IsNullOrWhiteSpace(row.Income)) rowMissing.Add("Income");
                if (row.HasSpecialNeeds && string.IsNullOrWhiteSpace(row.SpecialNeedsCategory)) rowMissing.Add("Special Needs Category");

                if (rowMissing.Count > 0)
                {
                    missing.Add($"Family member #{rowNum} ({row.MemberName}) — missing: {string.Join(", ", rowMissing)}");
                    ok = false;
                }
            }

            if (CmbStatus.SelectedItem == null)
            {
                MarkError(CmbStatus, ErrStatus, "Required.");
                missing.Add("Application Status");
                ok = false;
            }

            return ok;
        }

        private void MarkError(Control ctrl, TextBlock lbl, string msg)
        {
            if (ctrl != null)
            {
                ctrl.BorderBrush = ErrorBrush;
                ctrl.BorderThickness = new Thickness(1.5);
            }
            if (lbl != null)
            {
                lbl.Text = msg;
                lbl.Visibility = Visibility.Visible;
            }
        }

        private void ClearFieldError(Control ctrl, TextBlock lbl)
        {
            if (lbl != null && lbl.Visibility != Visibility.Collapsed)
            {
                lbl.Visibility = Visibility.Collapsed;
                if (ctrl != null)
                {
                    ctrl.BorderBrush = DefaultBrush;
                    ctrl.BorderThickness = DefaultThickness;
                }
            }
        }

        #endregion
    }
}
