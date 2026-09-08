using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
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
        private int _currentStep = 1;
        private bool _savedSuccessfully = false;
        private bool _isPopulating = false;

        // Observable Family / Dependents Collection bound to ItemsControl
        private readonly ObservableCollection<FamilyMemberRow> _familyRowData = new ObservableCollection<FamilyMemberRow>();

        // Template bounding boxes cache
        private Dictionary<string, double[]> _fieldBBoxes = new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase);

        // Zoom factor and drag panning
        private double _zoomFactor = 1.0;
        private Point _scrollMousePoint;
        private double _hOffset = 0;
        private double _vOffset = 0;
        private bool _isDragging = false;

        private static readonly SolidColorBrush ErrorBrush   = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
        private static readonly SolidColorBrush DefaultBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xB8, 0xC0));
        private static readonly Thickness DefaultThickness   = new Thickness(1);

        private ScannerSettings _scannerSettings = ScannerSettings.Load();

        public OcrScanDialog()
        {
            InitializeComponent();

            for (int i = 0; i < 5; i++)
                _familyRowData.Add(new FamilyMemberRow());
            FamilyRows.ItemsSource = _familyRowData;

            _fieldBBoxes = OcrService.GetTemplateBoundingBoxes();
            DpDateOfApplication.SelectedDate = DateTime.Today;

            UpdateScannerStatusBadge();
            CheckServerHealth();
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

            if (DialogShell != null)
            {
                DialogShell.MaxHeight = Math.Min(screenH * 0.95, 760);
                DialogShell.Height    = Math.Min(screenH * 0.90, 730);
                DialogShell.Width     = Math.Min(screenW * 0.95, 1360);
                DialogShell.MinWidth  = Math.Min(screenW * 0.90, 1050);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_savedSuccessfully) return;
            if (!HasAnyInput()) return;

            var result = MessageBox.Show(
                "You have unsaved information in this OCR session.\n\nDiscard and close?",
                "Unsaved Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result == MessageBoxResult.No)
                e.Cancel = true;
        }

        private bool HasAnyInput()
        {
            return !string.IsNullOrWhiteSpace(TxtLastName?.Text) ||
                   !string.IsNullOrWhiteSpace(TxtFirstName?.Text) ||
                   !string.IsNullOrWhiteSpace(_currentImagePath);
        }

        #region Server Health Check
 
         private enum OcrEngineState
         {
             Loading,
             Ready,
             Failed
         }

         private void UpdateEngineStatus(OcrEngineState state, string message)
         {
             Color dotColor;
             switch (state)
             {
                 case OcrEngineState.Ready:
                     dotColor = (Color)ColorConverter.ConvertFromString("#10B981"); // Vibrant Green
                     break;
                 case OcrEngineState.Loading:
                     dotColor = (Color)ColorConverter.ConvertFromString("#F59E0B"); // Orange
                     break;
                 case OcrEngineState.Failed:
                 default:
                     dotColor = (Color)ColorConverter.ConvertFromString("#EF4444"); // Red
                     break;
             }

             ServerStatusBadge.Background = new SolidColorBrush(dotColor);
             if (ServerStatusBadge.Effect is System.Windows.Media.Effects.DropShadowEffect glow)
             {
                 glow.Color = dotColor;
             }

             var tip = new ToolTip
             {
                 Content = message,
                 FontSize = 11,
                 FontWeight = FontWeights.SemiBold,
                 Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E2E")),
                 Foreground = Brushes.White,
                 BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333344")),
                 BorderThickness = new Thickness(1),
                 Padding = new Thickness(8, 5, 8, 5)
             };
             ServerStatusBadge.ToolTip = tip;
         }

         private async void CheckServerHealth()
         {
             UpdateEngineStatus(OcrEngineState.Loading, "OCR Engine: Checking Status...");

             bool online = await OcrService.IsServerOnlineAsync();
             if (!online)
             {
                 UpdateEngineStatus(OcrEngineState.Loading, "OCR Engine: Warming Up Engine...");
                 online = await OcrService.EnsureServerRunningAsync(25);
             }

             if (online)
             {
                 UpdateEngineStatus(OcrEngineState.Ready, "OCR Engine: Ready (Online) - Click to re-check");
             }
             else
             {
                 UpdateEngineStatus(OcrEngineState.Failed, "OCR Engine: Not Ready / Offline (Click to retry)");
             }
         }

         private void ServerStatusBadge_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
         {
             CheckServerHealth();
         }

         #endregion

        #region Step Navigation

        private void Step1Header_Click(object sender, MouseButtonEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
            GoToStep(1);
        }

        private void Step2Header_Click(object sender, MouseButtonEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
            if (!ValidateStep1()) return;
            GoToStep(2);
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

        #endregion

        #region Live Field-Sync Focus

        private void Field_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is string tag && !string.IsNullOrEmpty(tag))
            {
                FocusOnField(ResolveDynamicTag(elem, tag));
            }
        }

        private void Field_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is string tag && !string.IsNullOrEmpty(tag))
            {
                FocusOnField(ResolveDynamicTag(elem, tag));
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

        private void DatePicker_CalendarOpened(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is string tag && !string.IsNullOrEmpty(tag))
            {
                FocusOnField(tag);
            }
        }

        private void FocusOnField(string fieldKey)
        {
            if (string.IsNullOrEmpty(fieldKey)) return;

            if (ImgDocument.Source == null || ImgDocument.ActualWidth <= 0 || ImgDocument.ActualHeight <= 0)
                return;

            // Normalize aliases to their canonical template keys
            string key = fieldKey.Trim().ToLowerInvariant();
            if (key == "dob") key = "birthdate";
            else if (key == "date_of_application") key = "application_date";
            else if (key == "needs_and_problems") key = "needs";
            else if (key == "other_sources_of_income") key = "other_income";
            else if (key == "employed") key = "is_employed";
            else if (key == "self_employed") key = "is_self_employed";
            else if (key == "not_employed") key = "is_not_employed";

            double[] bbox = null;

            // 1. Primary: Use the adjusted bbox returned by the OCR engine for this document
            if (_ocrResponse?.Fields != null && _ocrResponse.Fields.TryGetValue(key, out var fieldRes) && fieldRes.Bbox != null && fieldRes.Bbox.Length == 4)
            {
                bbox = fieldRes.Bbox;
            }
            // 2. Secondary: Use the template bbox loaded from cswdo_template.json (strictly adheres to user adjustments)
            else if (_fieldBBoxes.TryGetValue(key, out var defBbox) && defBbox != null && defBbox.Length == 4)
            {
                bbox = defBbox;
            }
            // 3. Fallback for individual checkboxes if only parent employment_status was configured
            else if ((key == "is_employed" || key == "is_self_employed" || key == "is_not_employed") && _fieldBBoxes.TryGetValue("employment_status", out var empParent))
            {
                double px = empParent[0], py = empParent[1], pw = empParent[2], ph = empParent[3];
                if (key == "is_employed") bbox = new[] { px + pw * 0.07, py + ph * 0.35, pw * 0.05, ph * 0.50 };
                else if (key == "is_self_employed") bbox = new[] { px + pw * 0.33, py + ph * 0.35, pw * 0.05, ph * 0.50 };
                else if (key == "is_not_employed") bbox = new[] { px + pw * 0.66, py + ph * 0.35, pw * 0.06, ph * 0.50 };
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

            _zoomFactor = 2.8;
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

        #endregion

        #region Document Upload & OCR Processing

        private void ScannerSettings_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ScannerSettingsDialog(_scannerSettings) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                _scannerSettings = dlg.Settings;
                _scannerSettings.Save();
                UpdateScannerStatusBadge();
            }
        }

        private void UpdateScannerStatusBadge()
        {
            if (TxtScannerStatusBadge != null)
            {
                string dev = string.IsNullOrWhiteSpace(_scannerSettings.DeviceName) ? "Auto-detect" : _scannerSettings.DeviceName;
                TxtScannerStatusBadge.Text = $"Status: {dev} ({_scannerSettings.Dpi} DPI - {_scannerSettings.ColorMode})";
            }
        }

        private async void ScanHardware_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (BtnScanHardware != null) BtnScanHardware.IsEnabled = false;
                if (BtnSelectImage != null) BtnSelectImage.IsEnabled = false;
                if (BtnScannerSettings != null) BtnScannerSettings.IsEnabled = false;

                OverlayProcessing.Visibility = Visibility.Visible;
                OcrStageProgressBar.Value = 5;
                TxtProcessingStep.Text = "Step 1 of 4: Scanner Hardware Acquisition";
                TxtCurrentExtractingField.Text = "Connecting to scanner device...";
                TxtLiveElapsedTime.Text = "0.0s";

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                string scannedFile = await WiaScannerService.ScanDocumentAsync(
                    _scannerSettings,
                    status => Dispatcher.Invoke(() =>
                    {
                        TxtCurrentExtractingField.Text = status;
                        TxtLiveElapsedTime.Text = $"{stopwatch.Elapsed.TotalSeconds:F1}s";
                    })
                );

                stopwatch.Stop();

                if (!string.IsNullOrEmpty(scannedFile) && File.Exists(scannedFile))
                {
                    await ProcessDocumentAsync(scannedFile);
                }
            }
            catch (OperationCanceledException)
            {
                OverlayProcessing.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                OverlayProcessing.Visibility = Visibility.Collapsed;
                MessageBox.Show(
                    $"Scanner Notice:\n\n{ex.Message}\n\nYou can also click 'Upload File' if you already have a scanned image.",
                    "Scanner Communication Notice",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                if (BtnScanHardware != null) BtnScanHardware.IsEnabled = true;
                if (BtnSelectImage != null) BtnSelectImage.IsEnabled = true;
                if (BtnScannerSettings != null) BtnScannerSettings.IsEnabled = true;
            }
        }

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
                _fieldBBoxes = OcrService.GetTemplateBoundingBoxes();
                if (!string.IsNullOrEmpty(_currentImagePath) && File.Exists(_currentImagePath))
                {
                    await ProcessDocumentAsync(_currentImagePath);
                }
            }
        }

        private async Task ProcessDocumentAsync(string imagePath)
        {
            _currentImagePath = imagePath;

            if (EmptyCanvasState != null)
                EmptyCanvasState.Visibility = Visibility.Collapsed;

            OverlayProcessing.Visibility = Visibility.Visible;
            OcrStageProgressBar.Value = 15;
            TxtProcessingStep.Text = "Step 1 of 4: Preprocessing & Contrast Enhancement";
            TxtCurrentExtractingField.Text = "Deskewing and enhancing document contrast...";

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(imagePath);
            bitmap.EndInit();
            ImgDocument.Source = bitmap;
            ImgDocument.Width = DocScrollViewer.ActualWidth > 50 ? DocScrollViewer.ActualWidth - 20 : 440;
            ZoomFit_Click(null, null);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            TxtLiveElapsedTime.Text = "0.0s";

            try
            {
                using (var cts = new CancellationTokenSource())
                {
                    var progressTask = Task.Run(async () =>
                    {
                        var steps = new[]
                        {
                            (25, "Step 2 of 4: Reading Form Template Bounding Boxes", "Extracting Last Name, First Name, Middle Name..."),
                            (45, "Step 2 of 4: Reading Form Template Bounding Boxes", "Extracting Demographics & Barangay..."),
                            (65, "Step 2 of 4: Reading Form Template Bounding Boxes", "Extracting Socioeconomic & Emergency Contact..."),
                            (80, "Step 3 of 4: Analyzing Encircled Circumstances", "Detecting pen marks in Section II..."),
                            (90, "Step 3 of 4: Reading Family Dependents Table", "Extracting child records from Section III..."),
                            (95, "Step 4 of 4: Finalizing Extraction", "Applying confidence scores and normalizations...")
                        };

                        int stepIdx = 0;
                        while (!cts.Token.IsCancellationRequested)
                        {
                            await Task.Delay(100, cts.Token).ConfigureAwait(false);
                            if (cts.Token.IsCancellationRequested) break;

                            int idx = Math.Min(stepIdx / 4, steps.Length - 1);
                            var curStep = steps[idx];
                            stepIdx++;

                            await Dispatcher.InvokeAsync(() =>
                            {
                                OcrStageProgressBar.Value = curStep.Item1;
                                TxtProcessingStep.Text = curStep.Item2;
                                TxtCurrentExtractingField.Text = curStep.Item3;
                                TxtLiveElapsedTime.Text = $"{stopwatch.Elapsed.TotalSeconds:F1}s";
                            });
                        }
                    }, cts.Token);

                    _ocrResponse = await OcrService.ExtractFormAsync(imagePath, "global");
                    cts.Cancel();
                }

                stopwatch.Stop();
                if (_ocrResponse != null && _ocrResponse.ExecutionTimeSeconds <= 0)
                {
                    _ocrResponse.ExecutionTimeSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 2);
                }

                OcrStageProgressBar.Value = 100;
                TxtProcessingStep.Text = "Step 4 of 4: Complete!";
                TxtCurrentExtractingField.Text = "Extracted all fields with confidence scores.";
                await Task.Delay(200);

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
            _isPopulating = true;
            try
            {
                int pct = (int)(resp.OverallConfidence * 100);
                TxtOverallConfBadge.Text = $"Overall AI Confidence: {pct}% ({resp.OverallRating})";

                // Step 1: Identifying Information
                string appDateRaw = GetVal(resp, "application_date");
                if (OcrService.TryParseOcrDate(appDateRaw, out DateTime parsedAppDate))
                {
                    DpDateOfApplication.SelectedDate = parsedAppDate;
                }

                TxtLastName.Text     = OcrService.CleanPersonName(GetVal(resp, "last_name"));
                TxtFirstName.Text    = OcrService.CleanPersonName(GetVal(resp, "first_name"));
                TxtMiddleName.Text   = OcrService.CleanPersonName(GetVal(resp, "middle_name"));
                TxtExtension.Text    = OcrService.FuzzyMatchExtension(GetVal(resp, "ext_name"));
                TxtPlaceOfBirth.Text = GetVal(resp, "birthplace");

                SetComboByContent(CmbCivilStatus, OcrService.FuzzyMatchCivilStatus(GetVal(resp, "civil_status")));
                SetComboByContent(CmbSex,         OcrService.FuzzyMatchSex(GetVal(resp, "sex")));
                SetComboByContent(CmbEducation,   OcrService.FuzzyMatchEducation(GetVal(resp, "educational_attainment")));

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

                TxtPhilSys.Text       = GetVal(resp, "philsys_number");
                TxtReligion.Text      = OcrService.FuzzyMatchReligion(GetVal(resp, "religion"));
                TxtOccupation.Text    = GetVal(resp, "occupation");
                TxtMonthlyIncome.Text = OcrService.FuzzyMatchIncome(GetVal(resp, "monthly_income"));

                string empRaw = GetVal(resp, "employment_status").ToLower();
                ChkEmployed.IsChecked     = empRaw.Contains("employed") && !empRaw.Contains("not") && !empRaw.Contains("unemployed") && !empRaw.Contains("self");
                ChkSelfEmployed.IsChecked = empRaw.Contains("self");
                ChkNotEmployed.IsChecked  = empRaw.Contains("not") || empRaw.Contains("unemployed");

                TxtAddress.Text   = GetVal(resp, "address");
                TxtBarangay.Text  = OcrService.FuzzyMatchBarangay(GetVal(resp, "barangay"));
                TxtContact.Text   = OcrService.FormatPhoneNumber(GetVal(resp, "contact_number"));

                TxtEmergencyContact.Text = OcrService.CleanPersonName(GetVal(resp, "emergency_name"));
                TxtRelationship.Text     = OcrService.FuzzyMatchRelationship(GetVal(resp, "emergency_relationship"));
                TxtEmergencyAddress.Text = GetVal(resp, "emergency_address");
                TxtEmergencyNumber.Text  = OcrService.FormatPhoneNumber(GetVal(resp, "emergency_number"));

                // Confidence Badges
                SetFieldConfidence("application_date", Badge_AppDate, TxtConf_AppDate, resp);
                SetFieldConfidence("last_name", Badge_LastName, TxtConf_LastName, resp);
                SetFieldConfidence("first_name", Badge_FirstName, TxtConf_FirstName, resp);
                SetFieldConfidence("middle_name", Badge_MiddleName, TxtConf_MiddleName, resp);
                SetFieldConfidence("ext_name", Badge_Extension, TxtConf_Extension, resp);
                SetFieldConfidence("civil_status", Badge_CivilStatus, TxtConf_CivilStatus, resp);
                SetFieldConfidence("sex", Badge_Sex, TxtConf_Sex, resp);
                SetFieldConfidence("birthdate", Badge_Birthdate, TxtConf_Birthdate, resp);
                SetFieldConfidence("birthplace", Badge_Birthplace, TxtConf_Birthplace, resp);
                SetFieldConfidence("educational_attainment", Badge_Education, TxtConf_Education, resp);
                SetFieldConfidence("philsys_number", Badge_PhilSys, TxtConf_PhilSys, resp);
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

                // Step 2: Circumstances
                if (resp.Circumstance != null && resp.Circumstance.Detected && !string.IsNullOrEmpty(resp.Circumstance.Code))
                {
                    if (BadgeCircumstanceDetected != null && TxtCircumstanceBadge != null)
                    {
                        BadgeCircumstanceDetected.Visibility = Visibility.Visible;
                        int cConf = (int)(resp.Circumstance.Confidence * 100);
                        TxtCircumstanceBadge.Text = $"● Detected: {resp.Circumstance.Code} ({cConf}%)";
                    }

                    string code = resp.Circumstance.Code.ToUpper();
                    ChkA1.IsChecked = (code == "A1");
                    ChkA2.IsChecked = (code == "A2");
                    ChkA3.IsChecked = (code == "A3");
                    ChkA4.IsChecked = (code == "A4");
                    ChkA5.IsChecked = (code == "A5");
                    ChkA6.IsChecked = (code == "A6");
                    ChkA7.IsChecked = (code == "A7");
                    ChkB.IsChecked  = (code == "B");
                    ChkC.IsChecked  = (code == "C");
                    ChkD.IsChecked  = (code == "D");
                    ChkE.IsChecked  = (code == "E");
                    ChkF.IsChecked  = (code == "F");

                    if (ChkA2.IsChecked == true)
                    {
                        TxtA2Cause.Text = resp.Circumstance.SubfieldCause ?? string.Empty;
                        if (OcrService.TryParseOcrDate(resp.Circumstance.SubfieldDate, out var dDate))
                            DpA2Date.SelectedDate = dDate;
                    }
                    if (ChkA4.IsChecked == true)
                    {
                        TxtA4Disability.Text = resp.Circumstance.SubfieldDisability ?? string.Empty;
                    }
                    if (ChkA5.IsChecked == true)
                    {
                        TxtA5Period.Text = resp.Circumstance.SubfieldPeriod ?? string.Empty;
                    }
                    if (ChkB.IsChecked == true)
                    {
                        TxtBStayAbroad.Text = resp.Circumstance.SubfieldStayAbroad ?? string.Empty;
                    }
                }
                else
                {
                    if (BadgeCircumstanceDetected != null) BadgeCircumstanceDetected.Visibility = Visibility.Collapsed;
                }
                Circumstance_Changed(null, null);

                // Step 2: Family Composition Table
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

                        _familyRowData.Add(new FamilyMemberRow
                        {
                            MemberName          = OcrService.CleanPersonName(m.MemberName),
                            Sex                 = OcrService.IsOcrNotApplicable(m.Sex) ? string.Empty : m.Sex,
                            Age                 = ageVal,
                            Birthdate           = normDob,
                            CivilStatus         = OcrService.IsOcrNotApplicable(m.CivilStatus) ? string.Empty : OcrService.FuzzyMatchCivilStatus(m.CivilStatus),
                            Relationship        = OcrService.IsOcrNotApplicable(m.Relationship) ? string.Empty : OcrService.FuzzyMatchRelationship(m.Relationship),
                            EducationEmployment = OcrService.IsOcrNotApplicable(m.EducationEmployment) ? string.Empty : m.EducationEmployment,
                            Income              = OcrService.IsOcrNotApplicable(m.Income) ? "0" : OcrService.FuzzyMatchIncome(m.Income)
                        });
                    }
                }

                while (_familyRowData.Count < 5)
                {
                    _familyRowData.Add(new FamilyMemberRow());
                }

                // Step 2: Section IV Needs & Problems & Section V Other Sources of Income
                TxtNeeds.Text = GetVal(resp, "needs");
                TxtOtherIncome.Text = GetVal(resp, "other_income");
                SetFieldConfidence("needs", Badge_Needs, TxtConf_Needs, resp);
                SetFieldConfidence("other_income", Badge_OtherIncome, TxtConf_OtherIncome, resp);

                SetComboByContent(CmbStatus, "Valid");
            }
            finally
            {
                _isPopulating = false;
            }
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
            badge.Visibility = Visibility.Visible;
            badge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EBF5FB"));
            txt.Text = "Edited";
            txt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2980B9"));
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

        private void SetComboByContent(ComboBox combo, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || combo == null) return;
            string val = value.Trim();

            // 1. Exact match
            foreach (ComboBoxItem item in combo.Items)
            {
                string text = item.Content?.ToString();
                if (string.Equals(text, val, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedItem = item;
                    return;
                }
            }

            // 2. Normalized prefix / contains match (e.g. "College Level" or "BS Nursing" matches "College")
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
            ComboBoxItem sel = combo?.SelectedItem as ComboBoxItem;
            return sel?.Content?.ToString() ?? string.Empty;
        }

        #endregion

        #region Form Event Handlers & Dynamic Calculation

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

                string norm = OcrService.NormalizeDateString(tb.Text, out DateTime? dt);
                if (!string.IsNullOrEmpty(norm))
                {
                    row.Birthdate = norm;
                }
                if (dt.HasValue && dt.Value != DateTime.MinValue)
                {
                    int calcAge = OcrService.CalculateAge(dt.Value);
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
                row.MemberName = OcrService.CleanPersonName(tb.Text);
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

        private void Circumstance_Changed(object sender, RoutedEventArgs e)
        {
            if (PnlA2 != null) PnlA2.Visibility = ChkA2.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (PnlA4 != null) PnlA4.Visibility = ChkA4.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (PnlA5 != null) PnlA5.Visibility = ChkA5.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (PnlB  != null) PnlB.Visibility  = ChkB.IsChecked  == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AddFamilyRow_Click(object sender, RoutedEventArgs e)
        {
            _familyRowData.Add(new FamilyMemberRow());
        }

        private void TxtLastName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearFieldError(TxtLastName, ErrLastName);
            MarkBadgeEdited(Badge_LastName, TxtConf_LastName);
        }

        private void TxtFirstName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearFieldError(TxtFirstName, ErrFirstName);
            MarkBadgeEdited(Badge_FirstName, TxtConf_FirstName);
        }

        private void TxtBarangay_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearFieldError(TxtBarangay, ErrBarangay);
            MarkBadgeEdited(Badge_Barangay, TxtConf_Barangay);
        }

        private void TxtAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
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
            ClearFieldError(TxtPlaceOfBirth, ErrPlaceOfBirth);
            MarkBadgeEdited(Badge_Birthplace, TxtConf_Birthplace);
        }

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

        private void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearFieldError(CmbStatus, ErrStatus);
        }

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

        #endregion

        #region Form Validation & Record Saving

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

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
            if (!ValidateFields()) return;

            var preview = BuildRecord();
            var fields  = BuildConfirmFields(preview);

            DialogShell.Visibility = Visibility.Hidden;

            var confirm = new ConfirmDialog(
                "Confirm Scanned Record",
                "Review the verified document details below before committing to the database.",
                "Confirm & Save",
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
                        MemberName          = OcrService.CleanPersonName(row.MemberName),
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
                Id            = "SP-" + DateTime.Now.ToString("yyMMddHHmm"),
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
                ContactNumber = OcrService.FormatPhoneNumber(TxtContact.Text.Trim()),
                Barangay      = TxtBarangay.Text.Trim(),
                Status        = GetComboValue(CmbStatus),
                LastUpdated   = DateTime.Now,

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
                EmergencyContactNumber = OcrService.FormatPhoneNumber(TxtEmergencyNumber.Text.Trim()),

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

                FamilyMembers     = members,
                Children          = members.Count,
                NeedsAndProblems  = TxtNeeds.Text.Trim(),
                OtherIncomeSource = TxtOtherIncome.Text.Trim(),
            };
        }

        private List<ConfirmField> BuildConfirmFields(SoloParentRecord r)
        {
            string F(string v) => string.IsNullOrWhiteSpace(v) ? "—" : v;
            var list = new List<ConfirmField>();

            void Add(string section, string label, string newVal)
            {
                list.Add(new ConfirmField
                {
                    Section   = section,
                    Label     = label,
                    NewValue  = F(newVal),
                    OldValue  = null,
                    IsChanged = false
                });
            }

            string sec = "Applicant Type";
            string appType = r.IsNewApplicant ? "New Applicant" : r.IsRenewal ? "For Renewal" : "—";
            Add(sec, "Application Type",    appType);
            Add(sec, "Date of Application", r.DateOfApplication != DateTime.MinValue ? r.DateOfApplication.ToString("MMMM d, yyyy") : "—");

            sec = "Personal Information";
            Add(sec, "Full Name",              r.FullName);
            Add(sec, "Date of Birth",          r.DateOfBirth.ToString("MMMM d, yyyy"));
            Add(sec, "Age",                    TxtAge.Text);
            Add(sec, "Place of Birth",         r.PlaceOfBirth);
            Add(sec, "Sex",                    r.Sex);
            Add(sec, "Civil Status",           r.CivilStatus);
            Add(sec, "Educational Attainment", r.EducationalAttainment);
            Add(sec, "PhilSys Number",         r.PhilSysNumber);
            Add(sec, "Religion",               r.Religion);
            Add(sec, "Occupation",             r.Occupation);
            Add(sec, "Monthly Income",         r.MonthlyIncome);
            Add(sec, "Employment Status",      r.EmploymentStatusDisplay);
            Add(sec, "Address",                r.Address);
            Add(sec, "Barangay",               r.Barangay);
            Add(sec, "Contact Number",         r.ContactNumber);

            sec = "Emergency Contact";
            Add(sec, "Emergency Name",         r.EmergencyContactName);
            Add(sec, "Relationship",           r.EmergencyRelationship);
            Add(sec, "Emergency Address",      r.EmergencyAddress);
            Add(sec, "Emergency Phone",        r.EmergencyContactNumber);

            sec = "Circumstance";
            Add(sec, "Solo Parent Category",   r.CircumstancesDisplay);

            sec = "Family Composition";
            Add(sec, "Dependents Count",       r.FamilyMembers != null ? r.FamilyMembers.Count.ToString() : "0");

            sec = "Status";
            Add(sec, "Application Status",     r.Status);

            return list;
        }

        #endregion

        #region Zoom, Viewport & Pan Controls

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

        private void ViewDetections_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string debugPath1 = System.IO.Path.Combine(appDir, "debug_detections.jpg");
                string debugPath2 = System.IO.Path.GetFullPath(System.IO.Path.Combine(appDir, @"..\..\OcrService\debug_detections.jpg"));
                string debugPath3 = @"d:\Repos\SOLACE-UI-REPO\OcrService\debug_detections.jpg";

                string targetPath = null;
                if (System.IO.File.Exists(debugPath1)) targetPath = debugPath1;
                else if (System.IO.File.Exists(debugPath2)) targetPath = debugPath2;
                else if (System.IO.File.Exists(debugPath3)) targetPath = debugPath3;

                if (targetPath != null && System.IO.File.Exists(targetPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(targetPath) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("No debug detection overlay found yet. Please run an OCR scan first using the Global Spatial Engine!",
                        "Debug Overlay", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open debug detection image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion
    }
}
