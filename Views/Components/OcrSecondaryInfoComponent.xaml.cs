using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SOLUM_UI.Models;
using SOLUM_UI.Services;

namespace SOLUM_UI.Views.Components
{
    public partial class OcrSecondaryInfoComponent : UserControl
    {
        public event Action<string> FieldFocused;

        private bool _isPopulating = false;
        private bool _isFormattingPhone = false;

        private static readonly SolidColorBrush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
        private static readonly SolidColorBrush DefaultBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xB8, 0xC0));
        private static readonly Thickness DefaultThickness = new Thickness(1);
        private static readonly SolidColorBrush EditedBadgeBg = new SolidColorBrush(Color.FromRgb(0xEB, 0xF5, 0xFB));
        private static readonly SolidColorBrush EditedBadgeFg = new SolidColorBrush(Color.FromRgb(0x29, 0x80, 0xB9));

        private static readonly Regex NameRegex = new Regex(@"^[a-zA-ZñÑ\s\.\-']+$", RegexOptions.Compiled);
        private static readonly Regex PhoneRegex = new Regex(@"^[\d\-]+$", RegexOptions.Compiled);
        private static readonly Regex NumberOnlyRegex = new Regex(@"^\d+$", RegexOptions.Compiled);

        static OcrSecondaryInfoComponent()
        {
            ErrorBrush.Freeze();
            DefaultBrush.Freeze();
            EditedBadgeBg.Freeze();
            EditedBadgeFg.Freeze();
        }

        public OcrSecondaryInfoComponent()
        {
            InitializeComponent();
            FormHelper.BindSameAsApplicantAddress(ChkSameAsApplicant, TxtAddress, TxtEmergencyAddress);
        }

        public string CivilStatus => GetComboValue(CmbCivilStatus);
        public string Sex => GetComboValue(CmbSex);
        public string PlaceOfBirth => TxtPlaceOfBirth.Text.Trim();
        public string Address => TxtAddress.Text.Trim();
        public string Barangay => GetComboValue(CmbBarangay);
        public string ContactNumber => TxtContact.Text.Trim();
        public string EducationalAttainment => GetComboValue(CmbEducation);
        public string Religion => TxtReligion.Text.Trim();
        public string Occupation => TxtOccupation.Text.Trim();
        public string MonthlyIncome => TxtMonthlyIncome.Text.Trim();
        public string EmploymentStatusDisplay => GetComboValue(CmbEmploymentStatus);
        public bool IsEmployed => GetComboValue(CmbEmploymentStatus) == "Employed";
        public bool IsSelfEmployed => GetComboValue(CmbEmploymentStatus) == "Self-Employed";
        public bool IsNotEmployed => GetComboValue(CmbEmploymentStatus) == "Not Employed";
        public string EmergencyContactName => TxtEmergencyContact.Text.Trim();
        public string EmergencyRelationship => TxtRelationship.Text.Trim();
        public string EmergencyAddress => TxtEmergencyAddress.Text.Trim();
        public string EmergencyContactNumber => TxtEmergencyNumber.Text.Trim();
        public bool IsPantawidBeneficiary => ChkPantawid.IsChecked == true;
        public bool IsIndigenous => ChkIndigenous.IsChecked == true;
        public bool IsLGBT => ChkLGBTQ.IsChecked == true;

        #region Focus & Navigation Handling

        public bool FocusField(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return false;
            var el = FindElementByTag(this, tag);
            if (el != null)
            {
                el.BringIntoView();
                el.Focus();
                return true;
            }
            return false;
        }

        private static FrameworkElement FindElementByTag(DependencyObject parent, string tag)
        {
            if (parent == null || string.IsNullOrEmpty(tag)) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement fe && fe.Tag != null)
                {
                    string feTag = fe.Tag.ToString();
                    if (string.Equals(feTag, tag, StringComparison.OrdinalIgnoreCase))
                        return fe;
                    if ((tag == "dob" || tag == "birthdate") && (feTag == "dob" || feTag == "birthdate"))
                        return fe;
                    if (tag.StartsWith("fam_row", StringComparison.OrdinalIgnoreCase) && feTag.StartsWith("fam_row", StringComparison.OrdinalIgnoreCase))
                    {
                        int tagIdx = tag.LastIndexOf('_');
                        int feIdx = feTag.LastIndexOf('_');
                        if (tagIdx >= 0 && feIdx >= 0 && string.Equals(tag.Substring(tagIdx), feTag.Substring(feIdx), StringComparison.OrdinalIgnoreCase))
                            return fe;
                    }
                }
                var res = FindElementByTag(child, tag);
                if (res != null) return res;
            }
            return null;
        }

        private void Field_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is string tag && !string.IsNullOrEmpty(tag))
            {
                FieldFocused?.Invoke(tag);
            }
        }

        private void Field_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is string tag && !string.IsNullOrEmpty(tag))
            {
                FieldFocused?.Invoke(tag);
            }
        }

        #endregion

        #region Data Population & Confidence Badges

        public void Populate(OcrFormResponse resp)
        {
            if (resp == null) return;
            _isPopulating = true;
            try
            {
                TxtPlaceOfBirth.Text = GetVal(resp, "birthplace")?.ToUpperInvariant() ?? string.Empty;
                SetComboByContent(CmbCivilStatus, OcrService.FuzzyMatchCivilStatus(GetVal(resp, "civil_status")));
                SetComboByContent(CmbSex, OcrService.FuzzyMatchSex(GetVal(resp, "sex")));
                SetComboByContent(CmbEducation, OcrService.FuzzyMatchEducation(GetVal(resp, "educational_attainment")));

                TxtReligion.Text = OcrService.FuzzyMatchReligion(GetVal(resp, "religion"))?.ToUpperInvariant() ?? string.Empty;
                TxtOccupation.Text = GetVal(resp, "occupation")?.ToUpperInvariant() ?? string.Empty;
                TxtMonthlyIncome.Text = OcrService.FuzzyMatchIncome(GetVal(resp, "monthly_income"));

                string empRaw = GetVal(resp, "employment_status").ToLower();
                string isEmp = GetVal(resp, "is_employed").ToLower();
                string isSelf = GetVal(resp, "is_self_employed").ToLower();
                string isNot = GetVal(resp, "is_not_employed").ToLower();

                if (empRaw.Contains("self") || isSelf == "true" || isSelf == "1") SetComboByContent(CmbEmploymentStatus, "Self-Employed");
                else if (empRaw.Contains("not") || empRaw.Contains("unemployed") || isNot == "true" || isNot == "1") SetComboByContent(CmbEmploymentStatus, "Not Employed");
                else if (empRaw.Contains("employed") || isEmp == "true" || isEmp == "1") SetComboByContent(CmbEmploymentStatus, "Employed");
                else CmbEmploymentStatus.SelectedIndex = 0;

                TxtAddress.Text = GetVal(resp, "address")?.ToUpperInvariant() ?? string.Empty;
                SetComboByContent(CmbBarangay, OcrService.FuzzyMatchBarangay(GetVal(resp, "barangay")));
                TxtContact.Text = OcrService.FormatPhoneNumber(GetVal(resp, "contact_number"));

                TxtEmergencyContact.Text = OcrService.CleanPersonName(GetVal(resp, "emergency_name"));
                TxtRelationship.Text = OcrService.FuzzyMatchRelationship(GetVal(resp, "emergency_relationship"))?.ToUpperInvariant() ?? string.Empty;
                string emAddr = GetVal(resp, "emergency_address");
                string cleanEmAddr = emAddr?.Trim();
                if (!string.IsNullOrWhiteSpace(cleanEmAddr) &&
                    (cleanEmAddr.Equals("same", StringComparison.OrdinalIgnoreCase) ||
                     cleanEmAddr.Equals("same as applicant", StringComparison.OrdinalIgnoreCase) ||
                     cleanEmAddr.Equals("same as above", StringComparison.OrdinalIgnoreCase) ||
                     cleanEmAddr.Equals("do", StringComparison.OrdinalIgnoreCase) ||
                     cleanEmAddr.Equals("-do-", StringComparison.OrdinalIgnoreCase) ||
                     cleanEmAddr.Equals("\"\"", StringComparison.OrdinalIgnoreCase)))
                {
                    ChkSameAsApplicant.IsChecked = true;
                }
                else if (!string.IsNullOrWhiteSpace(cleanEmAddr) && !string.IsNullOrWhiteSpace(TxtAddress.Text) &&
                         string.Equals(cleanEmAddr, TxtAddress.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    ChkSameAsApplicant.IsChecked = true;
                }
                else
                {
                    ChkSameAsApplicant.IsChecked = false;
                    TxtEmergencyAddress.Text = emAddr?.ToUpperInvariant() ?? string.Empty;
                }
                TxtEmergencyNumber.Text = OcrService.FormatPhoneNumber(GetVal(resp, "emergency_number"));

                SetFieldConfidence("civil_status", Badge_CivilStatus, TxtConf_CivilStatus, resp);
                SetFieldConfidence("sex", Badge_Sex, TxtConf_Sex, resp);
                SetFieldConfidence("birthplace", Badge_Birthplace, TxtConf_Birthplace, resp);
                SetFieldConfidence("educational_attainment", Badge_Education, TxtConf_Education, resp);
                SetFieldConfidence("religion", Badge_Religion, TxtConf_Religion, resp);
                SetFieldConfidence("occupation", Badge_Occupation, TxtConf_Occupation, resp);
                SetFieldConfidence("monthly_income", Badge_MonthlyIncome, TxtConf_MonthlyIncome, resp);
                SetFieldConfidence("employment_status", Badge_EmploymentStatus, TxtConf_EmploymentStatus, resp);
                SetFieldConfidence("address", Badge_Address, TxtConf_Address, resp);
                SetFieldConfidence("barangay", Badge_Barangay, TxtConf_Barangay, resp);
                SetFieldConfidence("contact_number", Badge_Contact, TxtConf_Contact, resp);
                SetFieldConfidence("emergency_name", Badge_EmergencyContact, TxtConf_EmergencyContact, resp);
                SetFieldConfidence("emergency_relationship", Badge_Relationship, TxtConf_Relationship, resp);
                SetFieldConfidence("emergency_number", Badge_EmergencyNumber, TxtConf_EmergencyNumber, resp);
                SetFieldConfidence("emergency_address", Badge_EmergencyAddress, TxtConf_EmergencyAddress, resp);
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

        private void MarkBadgeEdited(Border badge, TextBlock txt)
        {
            if (_isPopulating || badge == null || txt == null) return;
            if (badge.Visibility == Visibility.Visible && txt.Text == "Edited") return;
            badge.Visibility = Visibility.Visible;
            badge.Background = EditedBadgeBg;
            txt.Text = "Edited";
            txt.Foreground = EditedBadgeFg;
        }

        #endregion

        #region TextChanged, Formatting & ComboBox Helpers

        private void ToUpperBox(TextBox tb)
        {
            if (tb == null || tb.CharacterCasing == CharacterCasing.Upper) return;
            if (tb.Text != tb.Text.ToUpper())
            {
                int caret = tb.SelectionStart;
                tb.Text = tb.Text.ToUpper();
                tb.SelectionStart = Math.Min(caret + 1, tb.Text.Length);
            }
        }

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

        private void TxtAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            ToUpperBox(TxtAddress);
            ClearFieldError(TxtAddress, ErrAddress);
            MarkBadgeEdited(Badge_Address, TxtConf_Address);
        }

        private void TxtContact_TextChanged(object sender, TextChangedEventArgs e)
        {
            FormatPhoneBox(TxtContact);
            ClearFieldError(TxtContact, ErrContact);
            MarkBadgeEdited(Badge_Contact, TxtConf_Contact);
        }

        private void TxtEmergencyNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            FormatPhoneBox(TxtEmergencyNumber);
            MarkBadgeEdited(Badge_EmergencyNumber, TxtConf_EmergencyNumber);
        }

        private void TxtPlaceOfBirth_TextChanged(object sender, TextChangedEventArgs e)
        {
            ToUpperBox(TxtPlaceOfBirth);
            ClearFieldError(TxtPlaceOfBirth, ErrPlaceOfBirth);
            MarkBadgeEdited(Badge_Birthplace, TxtConf_Birthplace);
        }

        private void TxtReligion_TextChanged(object sender, TextChangedEventArgs e) => ToUpperBox(TxtReligion);
        private void TxtOccupation_TextChanged(object sender, TextChangedEventArgs e) => ToUpperBox(TxtOccupation);
        private void TxtEmergencyContact_TextChanged(object sender, TextChangedEventArgs e)
        {
            ToUpperBox(TxtEmergencyContact);
            MarkBadgeEdited(Badge_EmergencyContact, TxtConf_EmergencyContact);
        }
        private void TxtEmergencyAddress_TextChanged(object sender, TextChangedEventArgs e) => ToUpperBox(TxtEmergencyAddress);

        private void CmbSex_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearFieldError(CmbSex, ErrSex);
            MarkBadgeEdited(Badge_Sex, TxtConf_Sex);
        }

        private void CmbCivilStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearFieldError(CmbCivilStatus, ErrCivilStatus);
            MarkBadgeEdited(Badge_CivilStatus, TxtConf_CivilStatus);
        }

        private void CmbBarangay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearFieldError(CmbBarangay, ErrBarangay);
            MarkBadgeEdited(Badge_Barangay, TxtConf_Barangay);
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
            e.Handled = !PhoneRegex.IsMatch(e.Text);
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !NumberOnlyRegex.IsMatch(e.Text);
        }

        private void NameOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !NameRegex.IsMatch(e.Text);
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

            if (combo == CmbSex)
            {
                string matchedSex = OcrService.FuzzyMatchSex(val);
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

            if (combo == CmbCivilStatus)
            {
                string matchedCiv = OcrService.FuzzyMatchCivilStatus(val);
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

            if (CmbCivilStatus.SelectedItem == null)
            { MarkError(CmbCivilStatus, ErrCivilStatus, "Required."); missing.Add("Civil Status"); ok = false; }

            if (CmbSex.SelectedItem == null)
            { MarkError(CmbSex, ErrSex, "Required."); missing.Add("Sex"); ok = false; }

            if (string.IsNullOrWhiteSpace(TxtPlaceOfBirth.Text))
            { MarkError(TxtPlaceOfBirth, ErrPlaceOfBirth, "Required."); missing.Add("Birthplace"); ok = false; }

            if (string.IsNullOrWhiteSpace(TxtAddress.Text))
            { MarkError(TxtAddress, ErrAddress, "Required."); missing.Add("Address"); ok = false; }

            string barangayValue = GetComboValue(CmbBarangay);
            if (string.IsNullOrWhiteSpace(barangayValue))
            { MarkError(CmbBarangay, ErrBarangay, "Required."); missing.Add("Barangay"); ok = false; }

            string contactDigits = Regex.Replace(TxtContact.Text ?? "", @"\D", "");
            if (string.IsNullOrWhiteSpace(contactDigits))
            { MarkError(TxtContact, ErrContact, "Required."); missing.Add("Contact Number"); ok = false; }
            else if (contactDigits.Length != 11)
            { MarkError(TxtContact, ErrContact, "Must be 11 digits."); missing.Add("Contact Number (11 digits)"); ok = false; }

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
