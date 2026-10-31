using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SOLUM_UI.Models;
using SOLUM_UI.Services;
using SOLUM_UI.Views.Dialogs;

namespace SOLUM_UI.Views.Components
{
    public partial class OcrDocumentViewportComponent : UserControl
    {
        public OcrFormResponse OcrResponse { get; private set; }
        public string CurrentImagePath { get; private set; } = string.Empty;

        public event Action<OcrFormResponse, string> DocumentProcessed;
        public event Action<OcrWarmupProgress> WarmupProgressChanged;
        public event Action<bool, string> ServerStatusChanged;
        public event Action<string> DocumentFieldClicked;

        private Dictionary<string, double[]> _fieldBBoxes = new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase);

        // Zoom factor, rotation, and drag panning
        private double _zoomFactor = 1.0;
        private double _currentRotationAngle = 0.0;
        private Point _scrollMousePoint;
        private Point _mouseDownPos;
        private double _hOffset = 0;
        private double _vOffset = 0;
        private bool _isDragging = false;
        private bool _hasDragged = false;
        private ScaleTransform _zoomTransform = new ScaleTransform(1.0, 1.0);
        private RotateTransform _rotateTransform = new RotateTransform(0.0);
        private TransformGroup _transformGroup;

        private CancellationTokenSource _dismissCts;
        private bool _isSpinning = false;
        private ScannerSettings _scannerSettings = ScannerSettings.Load();
        private string _lastFocusedFieldKey = null;
        private string _currentDeskewedImagePath = null;

        public OcrDocumentViewportComponent()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _fieldBBoxes = OcrService.GetTemplateBoundingBoxes();
            UpdateScannerStatusBadge();
            OcrService.WarmupProgressChanged += OnOcrWarmupProgressChanged;
            CheckServerHealth();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            OcrService.WarmupProgressChanged -= OnOcrWarmupProgressChanged;
            _dismissCts?.Cancel();
        }

        #region Server Health Check & Engine Warmup

        private void StartWarmupSpinner()
        {
            if (_isSpinning || WarmupIconRotate == null) return;
            _isSpinning = true;
            var anim = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(1.2),
                RepeatBehavior = RepeatBehavior.Forever
            };
            WarmupIconRotate.BeginAnimation(RotateTransform.AngleProperty, anim);
        }

        private void StopWarmupSpinner()
        {
            if (!_isSpinning || WarmupIconRotate == null) return;
            _isSpinning = false;
            WarmupIconRotate.BeginAnimation(RotateTransform.AngleProperty, null);
            WarmupIconRotate.Angle = 0;
        }

        private void ScheduleBannerAutoDismiss(int delayMs)
        {
            _dismissCts?.Cancel();
            _dismissCts = new CancellationTokenSource();
            var token = _dismissCts.Token;

            Task.Delay(delayMs, token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        if (OcrWarmupBanner != null)
                        {
                            OcrWarmupBanner.Visibility = Visibility.Collapsed;
                        }
                    });
                }
            }, TaskScheduler.Default);
        }

        private void DismissWarmup_Click(object sender, RoutedEventArgs e)
        {
            _dismissCts?.Cancel();
            OcrWarmupBanner.Visibility = Visibility.Collapsed;
        }

        public void WarmupRetry_Click(object sender, RoutedEventArgs e)
        {
            CheckServerHealth();
        }

        public async void CheckServerHealth()
        {
            _dismissCts?.Cancel();
            bool online = await OcrService.IsServerOnlineAsync();
            if (online)
            {
                ServerStatusChanged?.Invoke(true, "AI Engine: Ready");
                OcrWarmupBanner.Visibility = Visibility.Collapsed;
                return;
            }

            ServerStatusChanged?.Invoke(false, "AI Engine: Warming up...");
            OcrWarmupBanner.Visibility = Visibility.Visible;
            await OcrService.EnsureServerRunningAsync(25);
        }

        private void OnOcrWarmupProgressChanged(OcrWarmupProgress p)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (p == null) return;
                WarmupProgressChanged?.Invoke(p);

                if (p.IsReady)
                {
                    StopWarmupSpinner();
                    ServerStatusChanged?.Invoke(true, "AI Engine: Ready");

                    OcrWarmupBanner.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4"));
                    OcrWarmupBanner.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
                    TxtWarmupTitle.Text = "AI OCR Engine Ready";
                    TxtWarmupTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
                    TxtWarmupStepDetail.Text = p.Detail;
                    TxtWarmupElapsed.Text = $"{p.ElapsedSeconds:F1}s";
                    BadgeWarmupTime.Visibility = Visibility.Collapsed;
                    BtnWarmupRetry.Visibility = Visibility.Collapsed;
                    BtnDismissWarmup.Visibility = Visibility.Visible;
                    WarmupProgressBar.Value = 100;
                    WarmupProgressBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));

                    IconWarmupBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));
                    IconWarmupPath.Data = Geometry.Parse("M9,20.42L2.79,14.21L5.62,11.38L9,14.77L18.88,4.88L21.71,7.71L9,20.42Z");

                    ScheduleBannerAutoDismiss(2500);
                }
                else if (p.HasFailed)
                {
                    StopWarmupSpinner();
                    ServerStatusChanged?.Invoke(false, "AI Engine: Offline");

                    OcrWarmupBanner.Visibility = Visibility.Visible;
                    OcrWarmupBanner.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF2F2"));
                    OcrWarmupBanner.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FECACA"));
                    TxtWarmupTitle.Text = string.IsNullOrEmpty(p.Stage) ? "AI OCR Engine Offline" : p.Stage;
                    TxtWarmupTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
                    TxtWarmupStepDetail.Text = p.Detail;
                    TxtWarmupElapsed.Text = $"{p.ElapsedSeconds:F1}s";
                    BadgeWarmupTime.Visibility = Visibility.Collapsed;
                    BtnWarmupRetry.Visibility = Visibility.Visible;
                    BtnDismissWarmup.Visibility = Visibility.Visible;
                    WarmupProgressBar.Value = 0;
                    WarmupProgressBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));

                    IconWarmupBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                    IconWarmupPath.Data = Geometry.Parse("M13,14H11V10H13M13,18H11V16H13M1,21H23L12,2L1,21Z");
                }
                else
                {
                    StartWarmupSpinner();
                    OcrWarmupBanner.Visibility = Visibility.Visible;
                    OcrWarmupBanner.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF5F7"));
                    OcrWarmupBanner.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0B8C4"));
                    TxtWarmupTitle.Text = p.Stage;
                    TxtWarmupTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#702943"));
                    TxtWarmupStepDetail.Text = p.Detail;
                    TxtWarmupElapsed.Text = $"{p.ElapsedSeconds:F1}s elapsed";
                    BadgeWarmupTime.Visibility = Visibility.Visible;
                    TxtWarmupEstimatedTime.Text = p.EstimatedSecondsRemaining > 0
                        ? $"~{p.EstimatedSecondsRemaining:F0}s remaining"
                        : "Almost ready...";
                    BtnWarmupRetry.Visibility = Visibility.Collapsed;
                    BtnDismissWarmup.Visibility = Visibility.Collapsed;
                    WarmupProgressBar.Value = p.Percent;
                    WarmupProgressBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#702943"));

                    IconWarmupBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#702943"));
                    IconWarmupPath.Data = Geometry.Parse("M12,4V2A10,10 0 0,0 2,12H4A8,8 0 0,1 12,4Z");

                    ServerStatusChanged?.Invoke(false, $"AI Engine: Warming up (~{p.EstimatedSecondsRemaining:F0}s)");
                }
            });
        }

        #endregion

        #region Focus & Coordinate Highlighting

        public void FocusOnField(string fieldKey)
        {
            if (string.IsNullOrEmpty(fieldKey)) return;
            _lastFocusedFieldKey = fieldKey;

            if (ImgDocument.Source == null || ImgDocument.ActualWidth <= 0 || ImgDocument.ActualHeight <= 0)
                return;

            string key = fieldKey.Trim().ToLowerInvariant();
            if (key == "dob") key = "birthdate";
            else if (key == "date_of_application") key = "application_date";
            else if (key == "needs_and_problems") key = "needs";
            else if (key == "other_sources_of_income") key = "other_income";
            else if (key == "employed") key = "is_employed";
            else if (key == "self_employed") key = "is_self_employed";
            else if (key == "not_employed") key = "is_not_employed";

            if (key == "employment_status")
            {
                // Direct focus to the marked checkbox, defaulting to is_employed if none marked yet
                if (OcrResponse?.Fields != null)
                {
                    if (OcrResponse.Fields.TryGetValue("is_not_employed", out var n) && (n?.Value == "True" || n?.Value == "1"))
                        key = "is_not_employed";
                    else if (OcrResponse.Fields.TryGetValue("is_self_employed", out var s) && (s?.Value == "True" || s?.Value == "1"))
                        key = "is_self_employed";
                    else if (OcrResponse.Fields.TryGetValue("is_employed", out var e) && (e?.Value == "True" || e?.Value == "1"))
                        key = "is_employed";
                    else
                        key = "is_employed";
                }
                else
                {
                    key = "is_employed";
                }
            }

            if (key.StartsWith("fam_row", StringComparison.OrdinalIgnoreCase))
            {
                if (key.Contains("_civil")) key = key.Replace("_civil", "_civ");
                else if (key.Contains("_income")) key = key.Replace("_income", "_inc");
                else if (key.Contains("_education")) key = key.Replace("_education", "_edu");
                else if (key.Contains("_relationship")) key = key.Replace("_relationship", "_rel");
            }

            double[] bbox = null;

            // Prioritize calibrated template bounding box so Picture Preview matches Adjustable Boxes 1:1
            if (_fieldBBoxes.TryGetValue(key, out var defBbox) && defBbox != null && defBbox.Length == 4)
            {
                bbox = defBbox;
            }
            else if (OcrResponse?.Fields != null && OcrResponse.Fields.TryGetValue(key, out var fieldRes) && fieldRes.Bbox != null && fieldRes.Bbox.Length == 4)
            {
                bbox = fieldRes.Bbox;
            }
            else if (key == "is_employed") bbox = new[] { 0.6167, 0.2347, 0.0084, 0.0056 };
            else if (key == "is_self_employed") bbox = new[] { 0.7056, 0.2347, 0.0084, 0.0056 };
            else if (key == "is_not_employed") bbox = new[] { 0.8256, 0.2347, 0.0084, 0.0056 };

            if (bbox == null || bbox.Length < 4) return;

            double imgW = ImgDocument.ActualWidth;
            double imgH = ImgDocument.ActualHeight;

            HighlightCanvas.Children.Clear();
            double boxX = bbox[0] * imgW;
            double boxY = bbox[1] * imgH;
            double boxW = bbox[2] * imgW;
            double boxH = bbox[3] * imgH;

            // Outset dashed highlight frame: hugs the field boundary cleanly from outside
            // Transparent interior ensures handwriting and checkboxes remain completely unobscured
            double pad = 3.0;
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = Math.Max(4, boxW + pad * 2.0),
                Height = Math.Max(4, boxH + pad * 2.0),
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E11D48")), // Rose-600 clear accent
                StrokeThickness = 1.6,
                StrokeDashArray = new DoubleCollection { 4, 3 },
                Fill = Brushes.Transparent, // Pure transparent fill so handwriting/ticks are 100% visible
                RadiusX = 3,
                RadiusY = 3
            };
            Canvas.SetLeft(rect, boxX - pad);
            Canvas.SetTop(rect, boxY - pad);
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

        #region Scanning & Document Processing

        private void ScannerSettings_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            var dlg = new ScannerSettingsDialog(_scannerSettings) { Owner = window };
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
            var window = Window.GetWindow(this);
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
                ActionConfirmDialog.Warning(
                    "Scanner Notice",
                    $"{ex.Message}\n\nYou can also click 'Upload File' if you already have a scanned image.",
                    window);
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
            var window = Window.GetWindow(this);
            string imgToPass = !string.IsNullOrEmpty(_currentDeskewedImagePath) && File.Exists(_currentDeskewedImagePath)
                ? _currentDeskewedImagePath
                : CurrentImagePath;
            var configWin = new OcrTemplateConfigWindow(imgToPass, _lastFocusedFieldKey) { Owner = window };
            bool? res = configWin.ShowDialog();
            if (res == true || configWin.TemplateSaved)
            {
                _fieldBBoxes = OcrService.GetTemplateBoundingBoxes();
                if (OcrResponse?.Fields != null)
                {
                    foreach (var kvp in _fieldBBoxes)
                    {
                        if (OcrResponse.Fields.TryGetValue(kvp.Key, out var fld))
                        {
                            fld.Bbox = kvp.Value;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(CurrentImagePath) && File.Exists(CurrentImagePath))
                {
                    await ProcessDocumentAsync(CurrentImagePath);
                }
                else if (!string.IsNullOrEmpty(_lastFocusedFieldKey))
                {
                    FocusOnField(_lastFocusedFieldKey);
                }
            }
        }

        public async Task ProcessDocumentAsync(string imagePath)
        {
            CurrentImagePath = imagePath;
            var window = Window.GetWindow(this);

            if (EmptyCanvasState != null)
                EmptyCanvasState.Visibility = Visibility.Collapsed;

            OverlayProcessing.Visibility = Visibility.Visible;

            if (!await OcrService.IsServerOnlineAsync())
            {
                TxtProcessingStep.Text = "Waiting for AI OCR Engine to finish warming up...";
                TxtCurrentExtractingField.Text = "Loading OpenVINO neural network models...";
                OcrStageProgressBar.IsIndeterminate = true;
                bool warm = await OcrService.EnsureServerRunningAsync(25);
                OcrStageProgressBar.IsIndeterminate = false;
                if (!warm)
                {
                    throw new Exception("Local OCR server is offline and could not be started automatically. Please click 'Retry' or verify Python installation.");
                }
            }

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

                    OcrResponse = await OcrService.ExtractFormAsync(imagePath, "global");
                    cts.Cancel();
                }

                stopwatch.Stop();
                if (OcrResponse != null && OcrResponse.ExecutionTimeSeconds <= 0)
                {
                    OcrResponse.ExecutionTimeSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 2);
                }

                OcrStageProgressBar.Value = 100;
                TxtProcessingStep.Text = "Step 4 of 4: Complete!";
                TxtCurrentExtractingField.Text = "Extracted all fields with confidence scores.";

                if (!string.IsNullOrEmpty(OcrResponse.PreviewImageBase64))
                {
                    byte[] bytes = Convert.FromBase64String(OcrResponse.PreviewImageBase64);
                    using (var ms = new MemoryStream(bytes))
                    {
                        var deskewedBitmap = new BitmapImage();
                        deskewedBitmap.BeginInit();
                        deskewedBitmap.CacheOption = BitmapCacheOption.OnLoad;
                        deskewedBitmap.StreamSource = ms;
                        deskewedBitmap.EndInit();
                        ImgDocument.Source = deskewedBitmap;
                    }
                    try
                    {
                        string deskewedTemp = Path.Combine(Path.GetTempPath(), "solace_last_deskewed_preview.jpg");
                        File.WriteAllBytes(deskewedTemp, bytes);
                        _currentDeskewedImagePath = deskewedTemp;
                    }
                    catch { }
                }

                DocumentProcessed?.Invoke(OcrResponse, imagePath);
            }
            catch (Exception ex)
            {
                ActionConfirmDialog.Error("OCR Error", "Failed to process document: " + ex.Message, window);
            }
            finally
            {
                OverlayProcessing.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Pan, Zoom & Document Interaction Controls

        private void DocScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _scrollMousePoint = e.GetPosition(DocScrollViewer);
            _mouseDownPos = _scrollMousePoint;
            _hOffset = DocScrollViewer.HorizontalOffset;
            _vOffset = DocScrollViewer.VerticalOffset;
            _hasDragged = false;
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

                if (Math.Abs(currentPoint.X - _mouseDownPos.X) > 4 || Math.Abs(currentPoint.Y - _mouseDownPos.Y) > 4)
                {
                    _hasDragged = true;
                }

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

                // If mouse wasn't dragged, it's a click to jump to field!
                if (!_hasDragged && ImgDocument.Source != null && ImgDocument.ActualWidth > 0 && ImgDocument.ActualHeight > 0)
                {
                    Point clickOnImg = e.GetPosition(ImgDocument);
                    double nx = clickOnImg.X / ImgDocument.ActualWidth;
                    double ny = clickOnImg.Y / ImgDocument.ActualHeight;

                    if (nx >= 0.0 && nx <= 1.0 && ny >= 0.0 && ny <= 1.0)
                    {
                        HandleDocumentClick(nx, ny);
                    }
                }
            }
        }

        private void HandleDocumentClick(double nx, double ny)
        {
            if (_fieldBBoxes == null || _fieldBBoxes.Count == 0) return;

            string bestKey = null;
            double minArea = double.MaxValue;

            foreach (var kvp in _fieldBBoxes)
            {
                var b = kvp.Value;
                if (b == null || b.Length < 4) continue;
                double bx = b[0], by = b[1], bw = b[2], bh = b[3];
                if (nx >= bx && nx <= bx + bw && ny >= by && ny <= by + bh)
                {
                    double area = bw * bh;
                    if (area < minArea)
                    {
                        minArea = area;
                        bestKey = kvp.Key;
                    }
                }
            }

            if (bestKey != null)
            {
                FocusOnField(bestKey);
                DocumentFieldClicked?.Invoke(bestKey);
            }
        }

        private void DocScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ImgDocument.Source == null || ImgDocument.ActualWidth <= 0 || ImgDocument.ActualHeight <= 0)
                return;

            // Zoom ONLY when Ctrl is held down
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                Point mousePosInViewer = e.GetPosition(DocScrollViewer);

                double oldZoom = _zoomFactor;
                double zoomDelta = e.Delta > 0 ? 0.15 : -0.15;
                double newZoom = Math.Max(0.3, Math.Min(5.0, _zoomFactor + zoomDelta));

                if (Math.Abs(newZoom - oldZoom) < 0.001)
                {
                    e.Handled = true;
                    return;
                }

                double contentX = (DocScrollViewer.HorizontalOffset + mousePosInViewer.X) / oldZoom;
                double contentY = (DocScrollViewer.VerticalOffset + mousePosInViewer.Y) / oldZoom;

                _zoomFactor = newZoom;
                ApplyZoom();

                double newHorizontalOffset = (contentX * newZoom) - mousePosInViewer.X;
                double newVerticalOffset = (contentY * newZoom) - mousePosInViewer.Y;

                DocScrollViewer.ScrollToHorizontalOffset(Math.Max(0, newHorizontalOffset));
                DocScrollViewer.ScrollToVerticalOffset(Math.Max(0, newVerticalOffset));

                e.Handled = true;
                return;
            }

            // Normal scroll behavior without Ctrl:
            // Scroll vertically (or Shift+Wheel horizontally) smoothly without accidental zooming
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                double newOffset = DocScrollViewer.HorizontalOffset - (e.Delta * 0.8);
                DocScrollViewer.ScrollToHorizontalOffset(Math.Max(0, Math.Min(DocScrollViewer.ScrollableWidth, newOffset)));
                e.Handled = true;
            }
            else
            {
                double newOffset = DocScrollViewer.VerticalOffset - (e.Delta * 0.8);
                DocScrollViewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(DocScrollViewer.ScrollableHeight, newOffset)));
                e.Handled = true;
            }
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _zoomFactor = Math.Min(5.0, _zoomFactor + 0.3);
            ApplyZoom();
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _zoomFactor = Math.Max(0.3, _zoomFactor - 0.3);
            ApplyZoom();
        }

        private void ZoomFit_Click(object sender, RoutedEventArgs e)
        {
            HighlightCanvas.Children.Clear();
            _zoomFactor = 1.0;
            _currentRotationAngle = 0.0;
            ApplyZoom();
            DocScrollViewer.ScrollToHorizontalOffset(0);
            DocScrollViewer.ScrollToVerticalOffset(0);
        }

        private void ZoomReset_Click(object sender, MouseButtonEventArgs e)
        {
            _zoomFactor = 1.0;
            ApplyZoom();
        }

        private void RotateDocument_Click(object sender, RoutedEventArgs e)
        {
            _currentRotationAngle = (_currentRotationAngle + 90.0) % 360.0;
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            if (_transformGroup == null)
            {
                _zoomTransform = new ScaleTransform(_zoomFactor, _zoomFactor);
                _rotateTransform = new RotateTransform(_currentRotationAngle);
                _transformGroup = new TransformGroup();
                _transformGroup.Children.Add(_zoomTransform);
                _transformGroup.Children.Add(_rotateTransform);
                CanvasContainer.LayoutTransform = _transformGroup;
            }
            else
            {
                _zoomTransform.ScaleX = _zoomFactor;
                _zoomTransform.ScaleY = _zoomFactor;
                _rotateTransform.Angle = _currentRotationAngle;
            }

            if (TxtZoomLevel != null)
            {
                TxtZoomLevel.Text = $"{_zoomFactor * 100:F0}%";
            }
        }

        private void ViewDetections_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            try
            {
                string targetPath = null;
                // 1. Prefer the explicit debug image from the current scan response
                if (OcrResponse != null && !string.IsNullOrEmpty(OcrResponse.DebugImagePath) && File.Exists(OcrResponse.DebugImagePath))
                {
                    targetPath = OcrResponse.DebugImagePath;
                }

                // 2. Check %TEMP%/solace_last_scan_detections.jpg and project directories
                if (targetPath == null)
                {
                    var candidates = new List<string>();
                    string tempScan = Path.Combine(Path.GetTempPath(), "solace_last_scan_detections.jpg");
                    candidates.Add(tempScan);

                    string ocrDir = OcrService.FindOcrDirectory();
                    if (!string.IsNullOrEmpty(ocrDir))
                    {
                        candidates.Add(Path.Combine(ocrDir, "debug_detections.jpg"));
                    }
                    string appDir = AppDomain.CurrentDomain.BaseDirectory;
                    candidates.Add(Path.Combine(appDir, "debug_detections.jpg"));
                    try
                    {
                        candidates.Add(Path.GetFullPath(Path.Combine(appDir, @"..\..\OcrService\debug_detections.jpg")));
                    }
                    catch { }

                    DateTime bestTime = DateTime.MinValue;
                    foreach (var path in candidates)
                    {
                        if (File.Exists(path))
                        {
                            var wt = File.GetLastWriteTime(path);
                            if (wt > bestTime || targetPath == null)
                            {
                                bestTime = wt;
                                targetPath = path;
                            }
                        }
                    }
                }

                if (targetPath != null && File.Exists(targetPath))
                {
                    // Copy to a uniquely named file in temp to force Windows Photos to NEVER display a cached previous image
                    string uniquePath = Path.Combine(Path.GetTempPath(), $"solace_ai_detections_{DateTime.Now:yyyyMMdd_HHmmss}_{DateTime.Now.Ticks % 1000}.jpg");
                    File.Copy(targetPath, uniquePath, true);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uniquePath) { UseShellExecute = true });
                }
                else
                {
                    ActionConfirmDialog.Info(
                        "Debug Overlay",
                        "No AI detection overlay found for this document yet. Please click 'Scan Document' or 'Upload File' to run the OCR engine first!",
                        window);
                }
            }
            catch (Exception ex)
            {
                ActionConfirmDialog.Warning(
                    "Debug Overlay Error",
                    $"Could not open debug detection image: {ex.Message}",
                    window);
            }
        }

        #endregion
    }
}
