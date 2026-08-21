using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SOLUM_UI.Models;
using SOLUM_UI.Services;

namespace SOLUM_UI
{
    public partial class OcrScanDialog : Window
    {
        public SoloParentRecord Result { get; private set; }
        private OcrFormResponse _ocrResponse = new OcrFormResponse();
        private string _currentImagePath = string.Empty;

        // Observable Dependents Collection bound to DataGrid
        private readonly ObservableCollection<FamilyMember> _dependents = new ObservableCollection<FamilyMember>();

        // Zoom Level
        private double _zoomFactor = 1.0;

        public OcrScanDialog()
        {
            InitializeComponent();
            GridFamilyMembers.ItemsSource = _dependents;
            InitializeCircumstancesList();
            WireFieldEditEvents();
            CheckServerHealth();
        }

        private async void CheckServerHealth()
        {
            TxtServerStatus.Text = "● Checking Engine...";
            ServerStatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));

            bool online = await OcrService.IsServerOnlineAsync();
            if (!online)
            {
                TxtServerStatus.Text = "● Warming Up Engine...";
                ServerStatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));
                online = await OcrService.EnsureServerRunningAsync(25);
            }

            ServerStatusBadge.Background = online ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#388E3C"))
                                                  : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D32F2F"));
            TxtServerStatus.Text = online ? "● OCR Engine Online" : "● OCR Engine Offline (Click to Retry)";
        }

        private void ServerStatusBadge_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CheckServerHealth();
        }

        private void InitializeCircumstancesList()
        {
            var circumstances = new[]
            {
                new { Code = "A1", Label = "A1. Gives birth as a result of rape" },
                new { Code = "A2", Label = "A2. Death of Spouse" },
                new { Code = "A3", Label = "A3. Detention of Spouse" },
                new { Code = "A4", Label = "A4. Physical & Mental Incapacity of Spouse" },
                new { Code = "A5", Label = "A5. Legal or de facto Separation" },
                new { Code = "A6", Label = "A6. Declaration of nullity / annulment of marriage" },
                new { Code = "A7", Label = "A7. Abandonment of spouse for at least 6 months" },
                new { Code = "B",  Label = "B. Spouse or family member of an OFW" },
                new { Code = "C",  Label = "C. Unmarried Mother or Father" },
                new { Code = "D",  Label = "D. Legal Guardian / Adoptive / Foster Parent" },
                new { Code = "E",  Label = "E. Relative within 4th civil degree" },
                new { Code = "F",  Label = "F. Pregnant Woman" }
            };

            CmbCircumstance.ItemsSource = circumstances;
            CmbCircumstance.SelectedValuePath = "Code";
            CmbCircumstance.DisplayMemberPath = "Label";
        }

        private readonly Dictionary<string, double[]> _fieldBBoxes = new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "last_name", new[] { 0.090, 0.188, 0.160, 0.032 } },
            { "first_name", new[] { 0.285, 0.188, 0.185, 0.032 } },
            { "middle_name", new[] { 0.515, 0.188, 0.150, 0.032 } },
            { "ext_name", new[] { 0.690, 0.188, 0.070, 0.032 } },
            { "birthdate", new[] { 0.090, 0.222, 0.120, 0.028 } },
            { "birthplace", new[] { 0.265, 0.222, 0.145, 0.028 } },
            { "age", new[] { 0.435, 0.222, 0.060, 0.028 } },
            { "sex", new[] { 0.885, 0.188, 0.080, 0.032 } },
            { "civil_status", new[] { 0.810, 0.188, 0.050, 0.032 } },
            { "educational_attainment", new[] { 0.585, 0.222, 0.140, 0.028 } },
            { "religion", new[] { 0.075, 0.255, 0.138, 0.028 } },
            { "philsys_number", new[] { 0.810, 0.222, 0.155, 0.028 } },
            { "address", new[] { 0.175, 0.285, 0.240, 0.028 } },
            { "barangay", new[] { 0.460, 0.285, 0.205, 0.028 } },
            { "monthly_income", new[] { 0.520, 0.255, 0.075, 0.028 } },
            { "occupation", new[] { 0.325, 0.255, 0.125, 0.028 } },
            { "employment_status", new[] { 0.595, 0.255, 0.365, 0.028 } },
            { "contact_number", new[] { 0.740, 0.285, 0.225, 0.028 } },
            { "emergency_name", new[] { 0.170, 0.315, 0.160, 0.028 } },
            { "emergency_relationship", new[] { 0.395, 0.315, 0.100, 0.028 } },
            { "emergency_address", new[] { 0.535, 0.315, 0.215, 0.028 } },
            { "emergency_number", new[] { 0.810, 0.315, 0.155, 0.028 } },
            { "circumstances_section", new[] { 0.029, 0.345, 0.930, 0.142 } }
        };

        private void WireFieldEditEvents()
        {
            var fieldTextBoxes = new (TextBox box, Border badge, TextBlock txt, string key)[]
            {
                (TxtLastName, Badge_LastName, TxtConf_LastName, "last_name"),
                (TxtFirstName, Badge_FirstName, TxtConf_FirstName, "first_name"),
                (TxtMiddleName, Badge_MiddleName, TxtConf_MiddleName, "middle_name"),
                (TxtBirthplace, Badge_Birthplace, TxtConf_Birthplace, "birthplace"),
                (TxtPhilSysNumber, Badge_PhilSysNumber, TxtConf_PhilSysNumber, "philsys_number"),
                (TxtAddress, Badge_Address, TxtConf_Address, "address"),
                (TxtBarangay, Badge_Barangay, TxtConf_Barangay, "barangay"),
                (TxtMonthlyIncome, Badge_MonthlyIncome, TxtConf_MonthlyIncome, "monthly_income"),
                (TxtOccupation, Badge_Occupation, TxtConf_Occupation, "occupation"),
                (TxtContactNumber, Badge_ContactNumber, TxtConf_ContactNumber, "contact_number"),
                (TxtEmergencyName, Badge_EmergencyName, TxtConf_EmergencyName, "emergency_name"),
                (TxtEmergencyAddress, Badge_EmergencyAddress, TxtConf_EmergencyAddress, "emergency_address"),
                (TxtEmergencyNumber, Badge_EmergencyNumber, TxtConf_EmergencyNumber, "emergency_number"),
            };

            foreach (var item in fieldTextBoxes)
            {
                var capturedBadge = item.badge;
                var capturedTxt = item.txt;
                var capturedKey = item.key;

                item.box.TextChanged += (s, e) =>
                {
                    if (capturedBadge.Visibility == Visibility.Visible && capturedTxt.Text != "Edited")
                    {
                        capturedBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EBF5FB"));
                        capturedTxt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2980B9"));
                        capturedTxt.Text = "Edited";
                        capturedBadge.ToolTip = "Field was modified by caseworker.";
                    }
                };

                item.box.GotFocus += (s, e) => FocusOnField(capturedKey);
                item.box.PreviewMouseDown += (s, e) => FocusOnField(capturedKey);
            }

            CmbBirthMonth.GotFocus += (s, e) => FocusOnField("birthdate");
            CmbBirthDay.GotFocus += (s, e) => FocusOnField("birthdate");
            TxtBirthYear.GotFocus += (s, e) => FocusOnField("birthdate");

            WireComboEditEvents(CmbExtension, Badge_Extension, TxtConf_Extension, "ext_name");
            WireComboEditEvents(CmbSex, Badge_Sex, TxtConf_Sex, "sex");
            WireComboEditEvents(CmbCivilStatus, Badge_CivilStatus, TxtConf_CivilStatus, "civil_status");
            WireComboEditEvents(CmbEducationalAttainment, Badge_EducationalAttainment, TxtConf_EducationalAttainment, "educational_attainment");
            WireComboEditEvents(CmbReligion, Badge_Religion, TxtConf_Religion, "religion");
            WireComboEditEvents(CmbEmploymentStatus, Badge_EmploymentStatus, TxtConf_EmploymentStatus, "employment_status");
            WireComboEditEvents(CmbEmergencyRelationship, Badge_EmergencyRelationship, TxtConf_EmergencyRelationship, "emergency_relationship");

            CmbCircumstance.GotFocus += (s, e) => FocusOnField("circumstances_section");
            TxtA2Cause.GotFocus += (s, e) => FocusOnField("circ_a2_cause");
            TxtA2Date.GotFocus += (s, e) => FocusOnField("circ_a2_date");
            TxtA4Disability.GotFocus += (s, e) => FocusOnField("circ_a4_disability");
            TxtA5Period.GotFocus += (s, e) => FocusOnField("circ_a5_period");
            TxtBStayAbroad.GotFocus += (s, e) => FocusOnField("circ_b_stay");
            ChkA6Nullity.GotFocus += (s, e) => FocusOnField("circ_a6_nullity");
            ChkA6Annulment.GotFocus += (s, e) => FocusOnField("circ_a6_annulment");
        }

        private void WireComboEditEvents(ComboBox cmb, Border badge, TextBlock txt, string key)
        {
            if (cmb == null) return;
            cmb.SelectionChanged += (s, e) =>
            {
                if (badge != null && badge.Visibility == Visibility.Visible && txt != null && txt.Text != "Edited")
                {
                    badge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EBF5FB"));
                    txt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2980B9"));
                    txt.Text = "Edited";
                    badge.ToolTip = "Field was modified by caseworker.";
                }
            };
            cmb.GotFocus += (s, e) => FocusOnField(key);
        }

        #region Document Upload & Auto-Extraction

        private async void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select CSWDO Solo Parent Application Form Scan",
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                await ProcessDocumentAsync(dlg.FileName);
            }
        }

        private async void AdjustTemplate_Click(object sender, RoutedEventArgs e)
        {
            var configWin = new OcrTemplateConfigWindow(_currentImagePath) { Owner = this };
            if (configWin.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(_currentImagePath) && File.Exists(_currentImagePath))
                {
                    await ProcessDocumentAsync(_currentImagePath);
                }
            }
        }

        private async Task ProcessDocumentAsync(string imagePath)
        {
            _currentImagePath = imagePath;
            TxtImageStatus.Text = System.IO.Path.GetFileName(imagePath);

            OverlayProcessing.Visibility = Visibility.Visible;
            OcrStageProgressBar.Value = 15;
            TxtProcessingStep.Text = "Step 1 of 4: Preprocessing & Contrast Enhancement";
            TxtCurrentExtractingField.Text = "Deskewing and enhancing document contrast...";

            // Load initial bitmap on UI thread
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(imagePath);
            bitmap.EndInit();
            ImgDocument.Source = bitmap;
            ImgDocument.Width = DocScrollViewer.ActualWidth > 50 ? DocScrollViewer.ActualWidth - 20 : 440;
            ZoomFit_Click(null, null);

            try
            {
                using (var cts = new CancellationTokenSource())
                {
                    // Simulated visual step ticker during async server extraction
                    var progressTask = Task.Run(async () =>
                    {
                        var steps = new[]
                        {
                            (25, "Step 2 of 4: Reading Form Template Bounding Boxes", "Extracting Last Name, First Name, Middle Name..."),
                            (45, "Step 2 of 4: Reading Form Template Bounding Boxes", "Extracting Demographics & Barangay..."),
                            (65, "Step 2 of 4: Reading Form Template Bounding Boxes", "Extracting Socioeconomic & Emergency Contact..."),
                            (80, "Step 3 of 4: Analyzing Encircled Circumstances", "Detecting pen marks in Section II..."),
                            (90, "Step 3 of 4: Reading Family Dependents Table", "Extracting child records from Section V..."),
                            (95, "Step 4 of 4: Finalizing Extraction", "Applying confidence scores and normalizations...")
                        };

                        foreach (var step in steps)
                        {
                            if (cts.Token.IsCancellationRequested) break;
                            await Task.Delay(350, cts.Token).ConfigureAwait(false);
                            if (cts.Token.IsCancellationRequested) break;

                            await Dispatcher.InvokeAsync(() =>
                            {
                                OcrStageProgressBar.Value = step.Item1;
                                TxtProcessingStep.Text = step.Item2;
                                TxtCurrentExtractingField.Text = step.Item3;
                            });
                        }
                    }, cts.Token);

                    // Execute actual OCR backend request
                    _ocrResponse = await OcrService.ExtractFormAsync(imagePath);
                    cts.Cancel();
                }

                // Finalize UI
                OcrStageProgressBar.Value = 100;
                TxtProcessingStep.Text = "Step 4 of 4: Complete!";
                TxtCurrentExtractingField.Text = "Extracted all fields with confidence scores.";
                await Task.Delay(200);

                // If backend returned preprocessed/deskewed image, use it
                if (!string.IsNullOrEmpty(_ocrResponse.PreviewImageBase64))
                {
                    byte[] bytes = Convert.FromBase64String(_ocrResponse.PreviewImageBase64);
                    using (var ms = new MemoryStream(bytes))
                    {
                        var deskewedBitmap = new BitmapImage();
                        deskewedBitmap.BeginInit();
                        deskewedBitmap.CacheOption = BitmapCacheOption.OnLoad;
                        deskewedBitmap.StreamSource = ms;
                        deskewedBitmap.EndInit();
                        ImgDocument.Source = deskewedBitmap;
                    }
                }

                // Populate Fields, Circumstances, Confidence Badges, & Dependents DataGrid
                PopulateHitlFields(_ocrResponse);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to process document: " + ex.Message, "OCR Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                OverlayProcessing.Visibility = Visibility.Collapsed;
            }
        }

        private void PopulateHitlFields(OcrFormResponse resp)
        {
            if (resp == null) return;

            // Overall Confidence Banner
            int pct = (int)(resp.OverallConfidence * 100);
            TxtOverallConfidence.Text = pct + "% " + resp.OverallRating;
            BadgeOverallConfidence.Background = resp.OverallConfidence >= 0.80 
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6F9EE"))
                : (resp.OverallConfidence >= 0.50 
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE")));

            // Section 1: Personal Info
            TxtLastName.Text          = GetVal(resp, "last_name");
            TxtFirstName.Text         = GetVal(resp, "first_name");
            TxtMiddleName.Text        = GetVal(resp, "middle_name");
            CmbExtension.Text         = OcrService.FuzzyMatchExtension(GetVal(resp, "ext_name"));
            
            string dobRaw = GetVal(resp, "birthdate");
            if (OcrService.TryParseOcrDate(dobRaw, out DateTime parsedDob))
            {
                SetBirthdateFields(parsedDob);
                TxtWarn_Birthdate.Visibility = Visibility.Collapsed;
                SetFieldConfidence("birthdate", Badge_Birthdate, TxtConf_Birthdate, resp);
            }
            else
            {
                SetBirthdateFields(null);
                if (!string.IsNullOrWhiteSpace(dobRaw))
                {
                    TxtWarn_Birthdate.Visibility = Visibility.Visible;
                    Badge_Birthdate.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE"));
                    TxtConf_Birthdate.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0392B"));
                    TxtConf_Birthdate.Text = "Review";
                    Badge_Birthdate.ToolTip = $"Unrecognized OCR date text: '{dobRaw}'. Please select Month, Day, and Year manually.";
                    Badge_Birthdate.Visibility = Visibility.Visible;
                }
                else
                {
                    TxtWarn_Birthdate.Visibility = Visibility.Collapsed;
                    Badge_Birthdate.Visibility = Visibility.Collapsed;
                }
            }

            TxtBirthplace.Text            = GetVal(resp, "birthplace");
            CmbSex.Text                   = OcrService.FuzzyMatchSex(GetVal(resp, "sex"));
            CmbCivilStatus.Text           = OcrService.FuzzyMatchCivilStatus(GetVal(resp, "civil_status"));
            CmbEducationalAttainment.Text = OcrService.FuzzyMatchEducation(GetVal(resp, "educational_attainment"));
            CmbReligion.Text              = OcrService.FuzzyMatchReligion(GetVal(resp, "religion"));
            TxtPhilSysNumber.Text         = GetVal(resp, "philsys_number");

            SetFieldConfidence("last_name", Badge_LastName, TxtConf_LastName, resp);
            SetFieldConfidence("first_name", Badge_FirstName, TxtConf_FirstName, resp);
            SetFieldConfidence("middle_name", Badge_MiddleName, TxtConf_MiddleName, resp);
            SetFieldConfidence("ext_name", Badge_Extension, TxtConf_Extension, resp);
            SetFieldConfidence("birthplace", Badge_Birthplace, TxtConf_Birthplace, resp);
            SetFieldConfidence("sex", Badge_Sex, TxtConf_Sex, resp);
            SetFieldConfidence("civil_status", Badge_CivilStatus, TxtConf_CivilStatus, resp);
            SetFieldConfidence("educational_attainment", Badge_EducationalAttainment, TxtConf_EducationalAttainment, resp);
            SetFieldConfidence("religion", Badge_Religion, TxtConf_Religion, resp);
            SetFieldConfidence("philsys_number", Badge_PhilSysNumber, TxtConf_PhilSysNumber, resp);

            // Section 2: Encircled Circumstance
            if (resp.Circumstance != null && resp.Circumstance.Detected && !string.IsNullOrEmpty(resp.Circumstance.Code))
            {
                BadgeCircumstanceDetected.Visibility = Visibility.Visible;
                int confPct = (int)(resp.Circumstance.Confidence * 100);
                TxtCircumstanceBadge.Text = $"● Encircled: {resp.Circumstance.Code} ({confPct}%)";
                CmbCircumstance.SelectedValue = resp.Circumstance.Code;
                UpdateCircumstanceSubfield(resp.Circumstance.Code);

                // Populate any detected sub-fields
                if (!string.IsNullOrEmpty(resp.Circumstance.SubfieldCause)) TxtA2Cause.Text = resp.Circumstance.SubfieldCause;
                if (!string.IsNullOrEmpty(resp.Circumstance.SubfieldDate)) TxtA2Date.Text = resp.Circumstance.SubfieldDate;
                if (!string.IsNullOrEmpty(resp.Circumstance.SubfieldDisability)) TxtA4Disability.Text = resp.Circumstance.SubfieldDisability;
                if (!string.IsNullOrEmpty(resp.Circumstance.SubfieldPeriod)) TxtA5Period.Text = resp.Circumstance.SubfieldPeriod;
                if (!string.IsNullOrEmpty(resp.Circumstance.SubfieldStayAbroad)) TxtBStayAbroad.Text = resp.Circumstance.SubfieldStayAbroad;
                ChkA6Nullity.IsChecked = resp.Circumstance.SubfieldNullity;
                ChkA6Annulment.IsChecked = resp.Circumstance.SubfieldAnnulment;
            }
            else
            {
                BadgeCircumstanceDetected.Visibility = Visibility.Collapsed;
                CmbCircumstance.SelectedIndex = -1;
                HideAllCircumstancePanels();
            }

            // Section 3: Address & Socioeconomic
            TxtAddress.Text           = GetVal(resp, "address");
            TxtBarangay.Text          = OcrService.FuzzyMatchBarangay(GetVal(resp, "barangay"));
            TxtMonthlyIncome.Text     = OcrService.FuzzyMatchIncome(GetVal(resp, "monthly_income"));
            TxtOccupation.Text        = GetVal(resp, "occupation");
            TxtContactNumber.Text     = GetVal(resp, "contact_number");

            string empRaw = GetVal(resp, "employment_status").ToLower();
            if (empRaw.Contains("self"))
                CmbEmploymentStatus.SelectedIndex = 1;
            else if (empRaw.Contains("not") || empRaw.Contains("unemployed"))
                CmbEmploymentStatus.SelectedIndex = 2;
            else if (empRaw.Contains("employed"))
                CmbEmploymentStatus.SelectedIndex = 0;
            else
                CmbEmploymentStatus.SelectedIndex = -1;

            SetFieldConfidence("address", Badge_Address, TxtConf_Address, resp);
            SetFieldConfidence("barangay", Badge_Barangay, TxtConf_Barangay, resp);
            SetFieldConfidence("monthly_income", Badge_MonthlyIncome, TxtConf_MonthlyIncome, resp);
            SetFieldConfidence("occupation", Badge_Occupation, TxtConf_Occupation, resp);
            SetFieldConfidence("employment_status", Badge_EmploymentStatus, TxtConf_EmploymentStatus, resp);
            SetFieldConfidence("contact_number", Badge_ContactNumber, TxtConf_ContactNumber, resp);

            // Section 4: Emergency Contact
            TxtEmergencyName.Text            = GetVal(resp, "emergency_name");
            CmbEmergencyRelationship.Text    = OcrService.FuzzyMatchRelationship(GetVal(resp, "emergency_relationship"));
            TxtEmergencyNumber.Text          = GetVal(resp, "emergency_number");
            TxtEmergencyAddress.Text         = GetVal(resp, "emergency_address");

            SetFieldConfidence("emergency_name", Badge_EmergencyName, TxtConf_EmergencyName, resp);
            SetFieldConfidence("emergency_relationship", Badge_EmergencyRelationship, TxtConf_EmergencyRelationship, resp);
            SetFieldConfidence("emergency_number", Badge_EmergencyNumber, TxtConf_EmergencyNumber, resp);
            SetFieldConfidence("emergency_address", Badge_EmergencyAddress, TxtConf_EmergencyAddress, resp);

            ValidatePhoneNumbers();

            // Section 5: Populate Dependents Grid
            _dependents.Clear();
            if (resp.FamilyMembers != null && resp.FamilyMembers.Count > 0)
            {
                foreach (var m in resp.FamilyMembers)
                {
                    _dependents.Add(m);
                }
            }
        }

        private void SetBirthdateFields(DateTime? dt)
        {
            if (dt.HasValue)
            {
                CmbBirthMonth.SelectedIndex = dt.Value.Month; // 1 to 12
                CmbBirthDay.SelectedIndex = dt.Value.Day;     // 1 to 31
                TxtBirthYear.Text = dt.Value.Year.ToString();
                TxtWarn_Birthdate.Visibility = Visibility.Collapsed;
            }
            else
            {
                CmbBirthMonth.SelectedIndex = 0;
                CmbBirthDay.SelectedIndex = 0;
                TxtBirthYear.Text = string.Empty;
            }
        }

        private DateTime? GetSelectedBirthdate()
        {
            if (CmbBirthMonth == null || CmbBirthDay == null || TxtBirthYear == null) return null;
            int m = CmbBirthMonth.SelectedIndex;
            int d = CmbBirthDay.SelectedIndex;
            if (m >= 1 && m <= 12 && d >= 1 && d <= 31 && int.TryParse(TxtBirthYear.Text?.Trim(), out int y) && y >= 1900 && y <= DateTime.Today.Year)
            {
                try
                {
                    return new DateTime(y, m, d);
                }
                catch { }
            }
            return null;
        }

        private void BirthdateField_Changed(object sender, RoutedEventArgs e)
        {
            if (Badge_Birthdate == null || TxtConf_Birthdate == null || TxtWarn_Birthdate == null) return;
            var dt = GetSelectedBirthdate();
            if (dt.HasValue)
            {
                TxtWarn_Birthdate.Visibility = Visibility.Collapsed;
                Badge_Birthdate.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6F9EE"));
                TxtConf_Birthdate.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                TxtConf_Birthdate.Text = "Edited";
                Badge_Birthdate.Visibility = Visibility.Visible;
            }
            else
            {
                if (CmbBirthMonth.SelectedIndex > 0 || CmbBirthDay.SelectedIndex > 0 || !string.IsNullOrWhiteSpace(TxtBirthYear.Text))
                {
                    TxtWarn_Birthdate.Visibility = Visibility.Visible;
                    Badge_Birthdate.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE"));
                    TxtConf_Birthdate.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0392B"));
                    TxtConf_Birthdate.Text = "Review";
                    Badge_Birthdate.Visibility = Visibility.Visible;
                }
                else
                {
                    TxtWarn_Birthdate.Visibility = Visibility.Collapsed;
                    Badge_Birthdate.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void FamilyMemberCell_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is string colKey)
            {
                var row = FindVisualParent<DataGridRow>(elem);
                if (row != null)
                {
                    int rowIndex = row.GetIndex() + 1; // 1 to 5
                    string targetFieldKey = $"fam_row{rowIndex}_{colKey}";
                    FocusOnField(targetFieldKey);
                }
            }
        }

        private void FamilyMemberCell_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            FamilyMemberCell_GotFocus(sender, e);
        }

        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            if (child == null) return null;
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindVisualParent<T>(parentObject);
        }

        private void CmbCircumstance_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string code = CmbCircumstance.SelectedValue?.ToString() ?? string.Empty;
            UpdateCircumstanceSubfield(code);
        }

        private void HideAllCircumstancePanels()
        {
            if (PnlA2 != null) PnlA2.Visibility = Visibility.Collapsed;
            if (PnlA4 != null) PnlA4.Visibility = Visibility.Collapsed;
            if (PnlA5 != null) PnlA5.Visibility = Visibility.Collapsed;
            if (PnlA6 != null) PnlA6.Visibility = Visibility.Collapsed;
            if (PnlB != null) PnlB.Visibility = Visibility.Collapsed;
        }

        private void UpdateCircumstanceSubfield(string code)
        {
            HideAllCircumstancePanels();

            if (code == "A2" && PnlA2 != null)
                PnlA2.Visibility = Visibility.Visible;
            else if (code == "A4" && PnlA4 != null)
                PnlA4.Visibility = Visibility.Visible;
            else if (code == "A5" && PnlA5 != null)
                PnlA5.Visibility = Visibility.Visible;
            else if (code == "A6" && PnlA6 != null)
                PnlA6.Visibility = Visibility.Visible;
            else if (code == "B" && PnlB != null)
                PnlB.Visibility = Visibility.Visible;
        }

        private void CmbEmploymentStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void TxtContactNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidatePhoneNumbers();
        }

        private void TxtEmergencyNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidatePhoneNumbers();
        }

        private void ValidatePhoneNumbers()
        {
            if (TxtWarn_ContactNumber != null && TxtContactNumber != null)
            {
                string digits = Regex.Replace(TxtContactNumber.Text ?? "", @"\D", "");
                bool valid = string.IsNullOrEmpty(TxtContactNumber.Text) || (digits.Length == 11 && digits.StartsWith("09"));
                TxtWarn_ContactNumber.Visibility = valid ? Visibility.Collapsed : Visibility.Visible;
            }

            if (TxtWarn_EmergencyNumber != null && TxtEmergencyNumber != null)
            {
                string digits = Regex.Replace(TxtEmergencyNumber.Text ?? "", @"\D", "");
                bool valid = string.IsNullOrEmpty(TxtEmergencyNumber.Text) || (digits.Length == 11 && digits.StartsWith("09"));
                TxtWarn_EmergencyNumber.Visibility = valid ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void SetFieldConfidence(string key, Border badge, TextBlock txtBadge, OcrFormResponse resp)
        {
            if (resp.Fields != null && resp.Fields.TryGetValue(key, out var f) && f.Confidence > 0)
            {
                badge.Visibility = Visibility.Visible;
                int pct = (int)(f.Confidence * 100);
                txtBadge.Text = $"{pct}%";

                if (f.Confidence >= 0.80)
                {
                    badge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6F9EE"));
                    txtBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                    badge.ToolTip = $"High Confidence ({pct}%): OCR strongly matched the handwritten stroke.";
                }
                else if (f.Confidence >= 0.50)
                {
                    badge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF4E5"));
                    txtBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D35400"));
                    badge.ToolTip = $"Moderate Confidence ({pct}%): Please verify handwritten characters.";
                }
                else
                {
                    badge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDEDEC"));
                    txtBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0392B"));
                    badge.ToolTip = $"Low Confidence ({pct}%): OCR had difficulty reading this field. Please check carefully.";
                }
            }
            else
            {
                badge.Visibility = Visibility.Collapsed;
            }
        }

        private string GetVal(OcrFormResponse resp, string key)
        {
            if (resp?.Fields != null && resp.Fields.TryGetValue(key, out var f))
                return f.Value ?? string.Empty;
            return string.Empty;
        }

        private void AddDependent_Click(object sender, RoutedEventArgs e)
        {
            _dependents.Add(new FamilyMember
            {
                MemberName = "New Dependent",
                Relationship = "Child",
                Age = "",
                Sex = "Female",
                CivilStatus = "Single",
                Birthdate = "",
                EducationEmployment = "",
                Income = "0"
            });
        }

        #endregion

        #region Zoom, Focus on Field & Drag Panning Controls

        private Point _scrollMousePoint;
        private double _hOffset = 0;
        private double _vOffset = 0;
        private bool _isDragging = false;

        private void DocScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _scrollMousePoint = e.GetPosition(DocScrollViewer);
            _hOffset = DocScrollViewer.HorizontalOffset;
            _vOffset = DocScrollViewer.VerticalOffset;
            DocScrollViewer.CaptureMouse();
            _isDragging = true;
        }

        private void DocScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && DocScrollViewer.IsMouseCaptured)
            {
                Point currentPoint = e.GetPosition(DocScrollViewer);
                double deltaX = _scrollMousePoint.X - currentPoint.X;
                double deltaY = _scrollMousePoint.Y - currentPoint.Y;

                DocScrollViewer.ScrollToHorizontalOffset(_hOffset + deltaX);
                DocScrollViewer.ScrollToVerticalOffset(_vOffset + deltaY);
            }
        }

        private void DocScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                DocScrollViewer.ReleaseMouseCapture();
            }
        }

        private void FocusOnField(string fieldKey)
        {
            if (ImgDocument.Source == null || ImgDocument.ActualWidth <= 0 || ImgDocument.ActualHeight <= 0)
                return;

            double[] bbox = null;
            if (_ocrResponse?.Fields != null && _ocrResponse.Fields.TryGetValue(fieldKey, out var fieldRes) && fieldRes.Bbox != null && fieldRes.Bbox.Length == 4)
            {
                bbox = fieldRes.Bbox;
            }
            else if (_fieldBBoxes.TryGetValue(fieldKey, out var defBbox))
            {
                bbox = defBbox;
            }
            else if (fieldKey.StartsWith("fam_row", StringComparison.OrdinalIgnoreCase))
            {
                // Dynamic fallback for family table cells: fam_row{r}_{col}
                var parts = fieldKey.Split('_');
                if (parts.Length >= 3 && int.TryParse(parts[1].Replace("row", ""), out int rNum))
                {
                    string col = parts[2].ToLowerInvariant();
                    double rTop = 0.513 + (rNum - 1) * 0.030;
                    double rH = 0.026;
                    switch (col)
                    {
                        case "name": bbox = new[] { 0.025, rTop, 0.230, rH }; break;
                        case "sex":  bbox = new[] { 0.255, rTop, 0.060, rH }; break;
                        case "age":  bbox = new[] { 0.315, rTop, 0.055, rH }; break;
                        case "dob":  bbox = new[] { 0.370, rTop, 0.135, rH }; break;
                        case "civ":  bbox = new[] { 0.505, rTop, 0.095, rH }; break;
                        case "rel":  bbox = new[] { 0.600, rTop, 0.120, rH }; break;
                        case "edu":  bbox = new[] { 0.720, rTop, 0.140, rH }; break;
                        case "inc":  bbox = new[] { 0.860, rTop, 0.105, rH }; break;
                    }
                }
            }

            if (bbox == null || bbox.Length < 4) return;

            double imgW = ImgDocument.ActualWidth;
            double imgH = ImgDocument.ActualHeight;

            HighlightCanvas.Children.Clear();
            double boxX = bbox[0] * imgW;
            double boxY = bbox[1] * imgH;
            double boxW = bbox[2] * imgW;
            double boxH = bbox[3] * imgH;

            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = Math.Max(24, boxW + 8),
                Height = Math.Max(18, boxH + 6),
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#702943")),
                StrokeThickness = 3.0,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = new SolidColorBrush(Color.FromArgb(40, 112, 41, 67)),
                RadiusX = 4,
                RadiusY = 4
            };
            Canvas.SetLeft(rect, Math.Max(0, boxX - 4));
            Canvas.SetTop(rect, Math.Max(0, boxY - 3));
            HighlightCanvas.Children.Add(rect);

            _zoomFactor = 3.5;
            ApplyZoom();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                double targetCenterX = (boxX + boxW / 2.0) * _zoomFactor;
                double targetCenterY = (boxY + boxH / 2.0) * _zoomFactor;

                double scrollX = targetCenterX - (DocScrollViewer.ViewportWidth / 2.0);
                double scrollY = targetCenterY - (DocScrollViewer.ViewportHeight / 2.0);

                DocScrollViewer.ScrollToHorizontalOffset(Math.Max(0, scrollX));
                DocScrollViewer.ScrollToVerticalOffset(Math.Max(0, scrollY));
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _zoomFactor = Math.Min(5.0, _zoomFactor + 0.3);
            ApplyZoom();
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _zoomFactor = Math.Max(0.5, _zoomFactor - 0.3);
            ApplyZoom();
        }

        private void ZoomFit_Click(object sender, RoutedEventArgs e)
        {
            HighlightCanvas.Children.Clear();
            _zoomFactor = 1.0;
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            CanvasContainer.LayoutTransform = new ScaleTransform(_zoomFactor, _zoomFactor);
        }

        #endregion

        #region Pre-populate & Transfer to Registration Form

        private void ConfirmPrepopulate_Click(object sender, RoutedEventArgs e)
        {
            var r = new SoloParentRecord
            {
                Surname                = TxtLastName.Text.Trim(),
                FirstName              = TxtFirstName.Text.Trim(),
                MiddleName             = TxtMiddleName.Text.Trim(),
                ExtensionName          = CmbExtension.Text.Trim(),
                PlaceOfBirth           = TxtBirthplace.Text.Trim(),
                CivilStatus            = CmbCivilStatus.Text.Trim(),
                Sex                    = CmbSex.Text.Trim(),
                EducationalAttainment  = CmbEducationalAttainment.Text.Trim(),
                Religion               = CmbReligion.Text.Trim(),
                PhilSysNumber          = TxtPhilSysNumber.Text.Trim(),
                Address                = TxtAddress.Text.Trim(),
                Barangay               = OcrService.FuzzyMatchBarangay(TxtBarangay.Text.Trim()),
                MonthlyIncome          = OcrService.FuzzyMatchIncome(TxtMonthlyIncome.Text.Trim()),
                Occupation             = TxtOccupation.Text.Trim(),
                ContactNumber          = TxtContactNumber.Text.Trim(),
                EmergencyContactName   = TxtEmergencyName.Text.Trim(),
                EmergencyRelationship  = CmbEmergencyRelationship.Text.Trim(),
                EmergencyAddress       = TxtEmergencyAddress.Text.Trim(),
                EmergencyContactNumber = TxtEmergencyNumber.Text.Trim(),
                Status                 = "Valid",
                DateOfApplication      = DateTime.Today,
                LastUpdated            = DateTime.Today
            };

            var bday = GetSelectedBirthdate();
            if (bday.HasValue)
            {
                r.DateOfBirth = bday.Value;
            }

            r.IsEmployed     = (CmbEmploymentStatus.SelectedIndex == 0);
            r.IsSelfEmployed = (CmbEmploymentStatus.SelectedIndex == 1);
            r.IsNotEmployed  = (CmbEmploymentStatus.SelectedIndex == 2);

            // Transfer Circumstances from the verified ComboBox and sub-fields
            string selectedCirc = CmbCircumstance.SelectedValue?.ToString() ?? string.Empty;

            r.CircumstanceA1 = (selectedCirc == "A1");

            r.CircumstanceA2 = (selectedCirc == "A2");
            if (r.CircumstanceA2)
            {
                r.CircumstanceA2Cause = TxtA2Cause.Text.Trim();
                if (OcrService.TryParseOcrDate(TxtA2Date.Text.Trim(), out var dDate))
                    r.CircumstanceA2Date = dDate;
            }

            r.CircumstanceA3 = (selectedCirc == "A3");

            r.CircumstanceA4 = (selectedCirc == "A4");
            if (r.CircumstanceA4)
            {
                r.CircumstanceA4Disability = TxtA4Disability.Text.Trim();
            }

            r.CircumstanceA5 = (selectedCirc == "A5");
            if (r.CircumstanceA5)
            {
                r.CircumstanceA5Period = TxtA5Period.Text.Trim();
            }

            r.CircumstanceA6 = (selectedCirc == "A6");

            r.CircumstanceA7 = (selectedCirc == "A7");

            r.CircumstanceB  = (selectedCirc == "B");
            if (r.CircumstanceB)
            {
                r.CircumstanceBStayAbroad = TxtBStayAbroad.Text.Trim();
            }

            r.CircumstanceC  = (selectedCirc == "C");
            r.CircumstanceD  = (selectedCirc == "D");
            r.CircumstanceE  = (selectedCirc == "E");
            r.CircumstanceF  = (selectedCirc == "F");

            // Transfer Dependents from the verified DataGrid (ensuring Income is numeric "0" if N/A)
            var cleanedDependents = new List<FamilyMember>();
            foreach (var m in _dependents)
            {
                m.Income = OcrService.FuzzyMatchIncome(m.Income);
                m.CivilStatus = OcrService.FuzzyMatchCivilStatus(m.CivilStatus);
                m.Relationship = OcrService.FuzzyMatchRelationship(m.Relationship);
                cleanedDependents.Add(m);
            }
            r.FamilyMembers = cleanedDependents;
            r.Children = cleanedDependents.Count;

            r.Name = r.FullName;
            Result = r;

            DialogResult = true;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion
    }
}
