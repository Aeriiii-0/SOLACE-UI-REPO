using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.Views.Dialogs;

namespace SOLUM_UI.Views.Components
{
    public partial class OcrPrimaryInfoComponent : UserControl
    {
        public event Action<string> FieldFocused;
        public event Action DuplicateContinueRequested;

        public SoloParentRecord FoundRecord { get; private set; }
        public bool DuplicateCheckPassed { get; set; } = false;

        private bool _isPopulating = false;
        private bool _isFormattingPhilSys = false;

        private static readonly SolidColorBrush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
        private static readonly SolidColorBrush DefaultBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xB8, 0xC0));
        private static readonly Thickness DefaultThickness = new Thickness(1);
        private static readonly SolidColorBrush EditedBadgeBg = new SolidColorBrush(Color.FromRgb(0xEB, 0xF5, 0xFB));
        private static readonly SolidColorBrush EditedBadgeFg = new SolidColorBrush(Color.FromRgb(0x29, 0x80, 0xB9));

        private static readonly Regex NameRegex = new Regex(@"^[a-zA-ZñÑ\s\.\-']+$", RegexOptions.Compiled);
        private static readonly Regex NumberOnlyRegex = new Regex(@"^\d+$", RegexOptions.Compiled);

        static OcrPrimaryInfoComponent()
        {
            ErrorBrush.Freeze();
            DefaultBrush.Freeze();
            EditedBadgeBg.Freeze();
            EditedBadgeFg.Freeze();
        }

        public OcrPrimaryInfoComponent()
        {
            InitializeComponent();
            DpDateOfApplication.SelectedDate = DateTime.Today;
        }

        public DateTime? ApplicationDate => DpDateOfApplication.SelectedDate;
        public string LastName => TxtLastName.Text.Trim();
        public string FirstName => TxtFirstName.Text.Trim();
        public string MiddleName => TxtMiddleName.Text.Trim();
        public string ExtensionName => TxtExtension.Text.Trim();
        public DateTime? Birthdate => DpBirthdate.SelectedDate;
        public string Age => TxtAge.Text;
        public string PhilSysNumber => TxtPhilSys.Text.Trim();

        public bool HasAnyInput()
        {
            return !string.IsNullOrWhiteSpace(TxtLastName.Text) ||
                   !string.IsNullOrWhiteSpace(TxtFirstName.Text);
        }

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

        private void DatePicker_CalendarOpened(object sender, RoutedEventArgs e)
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
            DuplicateCheckPassed = false;
            if (DuplicateBannerPanel != null) DuplicateBannerPanel.Visibility = Visibility.Collapsed;
            _isPopulating = true;
            try
            {
                string appDateRaw = GetVal(resp, "application_date");
                if (OcrService.TryParseOcrDate(appDateRaw, out DateTime parsedAppDate))
                {
                    DpDateOfApplication.SelectedDate = parsedAppDate;
                }

                TxtLastName.Text = OcrService.CleanPersonName(GetVal(resp, "last_name"));
                TxtFirstName.Text = OcrService.CleanPersonName(GetVal(resp, "first_name"));
                TxtMiddleName.Text = OcrService.CleanPersonName(GetVal(resp, "middle_name"));
                TxtExtension.Text = OcrService.FuzzyMatchExtension(GetVal(resp, "ext_name"))?.ToUpperInvariant() ?? string.Empty;

                string dobRaw = GetVal(resp, "birthdate");
                if (OcrService.TryParseOcrDate(dobRaw, out DateTime parsedDob))
                {
                    DpBirthdate.SelectedDate = parsedDob;
                    UpdateAge(parsedDob);
                }
                else
                {
                    DpBirthdate.SelectedDate = null;
                    UpdateAge(null);
                }

                TxtPhilSys.Text = GetVal(resp, "philsys_number");

                SetFieldConfidence("application_date", Badge_AppDate, TxtConf_AppDate, resp);
                SetFieldConfidence("last_name", Badge_LastName, TxtConf_LastName, resp);
                SetFieldConfidence("first_name", Badge_FirstName, TxtConf_FirstName, resp);
                SetFieldConfidence("middle_name", Badge_MiddleName, TxtConf_MiddleName, resp);
                SetFieldConfidence("ext_name", Badge_Extension, TxtConf_Extension, resp);
                SetFieldConfidence("birthdate", Badge_Birthdate, TxtConf_Birthdate, resp);
                SetFieldConfidence("philsys_number", Badge_PhilSys, TxtConf_PhilSys, resp);
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

        #region TextChanged & Formatting

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

        private void FormatPhilSysBox(TextBox tb)
        {
            if (tb == null || _isFormattingPhilSys) return;
            _isFormattingPhilSys = true;
            try
            {
                string raw = tb.Text ?? "";
                string formatted = RecordDialog.FormatPhilSysNumber(raw);
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

        private void TxtLastName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ToUpperBox(TxtLastName);
            ClearFieldError(TxtLastName, ErrLastName);
            MarkBadgeEdited(Badge_LastName, TxtConf_LastName);
            DuplicateCheckPassed = false;
            if (DuplicateBannerPanel != null) DuplicateBannerPanel.Visibility = Visibility.Collapsed;
        }

        private void TxtFirstName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ToUpperBox(TxtFirstName);
            ClearFieldError(TxtFirstName, ErrFirstName);
            MarkBadgeEdited(Badge_FirstName, TxtConf_FirstName);
            DuplicateCheckPassed = false;
            if (DuplicateBannerPanel != null) DuplicateBannerPanel.Visibility = Visibility.Collapsed;
        }

        private void TxtMiddleName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ToUpperBox(TxtMiddleName);
            MarkBadgeEdited(Badge_MiddleName, TxtConf_MiddleName);
        }

        private void TxtExtension_TextChanged(object sender, TextChangedEventArgs e)
        {
            ToUpperBox(TxtExtension);
        }

        private void TxtPhilSys_TextChanged(object sender, TextChangedEventArgs e)
        {
            FormatPhilSysBox(TxtPhilSys);
            ClearFieldError(TxtPhilSys, ErrPhilSys);
            MarkBadgeEdited(Badge_PhilSys, TxtConf_PhilSys);
            DuplicateCheckPassed = false;
            if (DuplicateBannerPanel != null) DuplicateBannerPanel.Visibility = Visibility.Collapsed;
        }

        private void NameOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !NameRegex.IsMatch(e.Text);
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !NumberOnlyRegex.IsMatch(e.Text);
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

        #endregion

        #region Date Handling & Age Calculation

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

            if (dob.Value.Date > today)
            {
                TxtAge.Text = string.Empty;
                ErrBirthdate.Text = "Cannot be in the future.";
                ErrBirthdate.Visibility = Visibility.Visible;
                return;
            }

            if (age > 120)
            {
                TxtAge.Text = string.Empty;
                ErrBirthdate.Text = "Age cannot exceed 120 years.";
                ErrBirthdate.Visibility = Visibility.Visible;
                DpBirthdate.SelectedDate = null;
                ActionConfirmDialog.Warning(
                    "Invalid Age",
                    "Invalid date of birth.\n\nAge cannot exceed 120 years.\n\nPlease select a valid birthdate.",
                    Window.GetWindow(this));
                return;
            }

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
                if (OcrService.TryParseOcrDate(e.Text, out DateTime dt))
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
                    if (OcrService.TryParseOcrDate(tb.Text, out DateTime dt))
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

        #endregion

        #region Validation & Duplicate Checking

        public bool Validate(out List<string> missing)
        {
            bool ok = true;
            missing = new List<string>();

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

            return ok;
        }

        public async Task<bool> CheckDuplicateAsync()
        {
            if (DuplicateCheckPassed) return true;

            try
            {
                string fName = TxtFirstName.Text.Trim();
                string lName = TxtLastName.Text.Trim();
                string philsys = TxtPhilSys.Text.Trim();

                var matches = new List<SoloParentRecord>();

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
                    FoundRecord = matches[0];
                    DuplicateBannerTitle.Text = $"Duplicate Found ({matches.Count} matching record{(matches.Count > 1 ? "s" : "")})";
                    DuplicateBannerText.Text = $"An existing record exists for '{FoundRecord.Name}'" +
                        (!string.IsNullOrWhiteSpace(FoundRecord.PhilSysNumber) ? $" with PhilSys ID {FoundRecord.PhilSysNumber}." : ".") +
                        " You can view the existing record or continue ingesting this scanned document.";
                    DuplicateBannerPanel.Visibility = Visibility.Visible;
                    return false;
                }
            }
            catch
            {
                // If API/database check fails, allow user to proceed
            }

            return true;
        }

        private void ViewExistingRecord_Click(object sender, RoutedEventArgs e)
        {
            if (FoundRecord != null)
            {
                var viewer = new RecordViewDialog(FoundRecord) { Owner = Window.GetWindow(this) };
                viewer.ShowDialog();
            }
        }

        private void ContinueAnyway_Click(object sender, RoutedEventArgs e)
        {
            DuplicateCheckPassed = true;
            DuplicateBannerPanel.Visibility = Visibility.Collapsed;
            DuplicateContinueRequested?.Invoke();
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

        private void MarkDateError(DatePicker dp, TextBlock lbl, string msg)
        {
            if (dp != null)
            {
                dp.BorderBrush = ErrorBrush;
                dp.BorderThickness = new Thickness(1.5);
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

        private void ClearDateError(DatePicker dp, TextBlock lbl)
        {
            if (dp != null)
            {
                dp.BorderBrush = DefaultBrush;
                dp.BorderThickness = DefaultThickness;
            }
            if (lbl != null)
            {
                lbl.Visibility = Visibility.Collapsed;
            }
        }

        #endregion
    }
}
