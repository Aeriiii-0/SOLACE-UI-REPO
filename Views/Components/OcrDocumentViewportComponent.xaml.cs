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
        public bool IsProcessingDocument => OverlayProcessing?.Visibility == Visibility.Visible;

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
        private readonly List<string> _sessionScannedFiles = new List<string>();
        private string _lastFocusedFieldKey = null;
        private string _currentDeskewedImagePath = null;
        private CancellationTokenSource _processCts;

        public void CancelActiveProcessing()
        {
            try
            {
                _processCts?.Cancel();
            }
            catch { }
            OverlayProcessing.Visibility = Visibility.Collapsed;
        }

        private void CancelOcr_Click(object sender, RoutedEventArgs e)
        {
            CancelActiveProcessing();
        }

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
            Task.Run(() => WiaScannerService.CleanupOrphanedTempScans());
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            OcrService.WarmupProgressChanged -= OnOcrWarmupProgressChanged;
            _dismissCts?.Cancel();

            // Purge any temporary scans created during this session
            foreach (var f in _sessionScannedFiles)
            {
                try { if (File.Exists(f)) File.Delete(f); } catch { }
            }
            _sessionScannedFiles.Clear();
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
                ServerStatusChanged?.Invoke(true, "OCR: Ready");
                OcrWarmupBanner.Visibility = Visibility.Collapsed;
                return;
            }

            ServerStatusChanged?.Invoke(false, "OCR: Warming up...");
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

        public void FocusOnField(string fieldKey, bool autoCenter = true)
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
                if (key.StartsWith("fam_row_", StringComparison.OrdinalIgnoreCase))
                    key = "fam_row1_" + key.Substring("fam_row_".Length);

                if (key.Contains("_civil")) key = key.Replace("_civil", "_civ");
                else if (key.Contains("_income")) key = key.Replace("_income", "_inc");
                else if (key.Contains("_education")) key = key.Replace("_education", "_edu");
                else if (key.Contains("_relationship")) key = key.Replace("_relationship", "_rel");
                else if (key.Contains("_birthdate")) key = key.Replace("_birthdate", "_dob");
            }

            double[] bbox = null;

            // Prefer dynamically aligned bounding box returned by OCR engine for this specific scan
            if (OcrResponse?.Fields != null && OcrResponse.Fields.TryGetValue(key, out var fieldRes) && fieldRes.Bbox != null && fieldRes.Bbox.Length == 4)
            {
                bbox = fieldRes.Bbox;
            }
            else if (_fieldBBoxes.TryGetValue(key, out var defBbox) && defBbox != null && defBbox.Length == 4)
            {
                bbox = defBbox;
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

            if (autoCenter)
            {
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
        }

        #endregion

        #region OCR Settings Popover Handlers

        private void BtnOcrSettings_Click(object sender, RoutedEventArgs e)
        {
            if (OcrSettingsPopup != null)
            {
                OcrSettingsPopup.IsOpen = !OcrSettingsPopup.IsOpen;
            }
        }

        private void MenuScannerSettings_Click(object sender, MouseButtonEventArgs e)
        {
            if (OcrSettingsPopup != null) OcrSettingsPopup.IsOpen = false;
            ScannerSettings_Click(sender, e);
        }

        private void MenuAdjustBoxes_Click(object sender, MouseButtonEventArgs e)
        {
            if (OcrSettingsPopup != null) OcrSettingsPopup.IsOpen = false;
            AdjustTemplate_Click(sender, e);
        }

        private void MenuViewAiBoxes_Click(object sender, MouseButtonEventArgs e)
        {
            if (OcrSettingsPopup != null) OcrSettingsPopup.IsOpen = false;
            ViewDetections_Click(sender, e);
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
            string dev = string.IsNullOrWhiteSpace(_scannerSettings.DeviceName) ? "Auto-detect" : _scannerSettings.DeviceName;
            string paper = string.IsNullOrWhiteSpace(_scannerSettings.PaperSize) ? "8.5x13" : _scannerSettings.PaperSize;
            if (TxtScannerStatusBadge != null)
            {
                TxtScannerStatusBadge.Text = $"Status: {dev} ({_scannerSettings.Dpi} DPI - {_scannerSettings.ColorMode} - {paper})";
            }
            if (TxtMenuScannerSub != null)
            {
                TxtMenuScannerSub.Text = $"{dev} • {_scannerSettings.Dpi} DPI • {_scannerSettings.ColorMode} • {paper}";
            }
        }

        private void TransitionStatusText(TextBlock targetTextBlock, string newText, bool animate = true)
        {
            if (targetTextBlock == null || string.IsNullOrEmpty(newText)) return;

            var transform = targetTextBlock.RenderTransform as TranslateTransform;
            if (transform == null)
            {
                transform = new TranslateTransform(0, 0);
                targetTextBlock.RenderTransform = transform;
            }

            if (!animate || string.Equals(targetTextBlock.Text, newText, StringComparison.Ordinal))
            {
                targetTextBlock.BeginAnimation(UIElement.OpacityProperty, null);
                transform.BeginAnimation(TranslateTransform.XProperty, null);
                targetTextBlock.Text = newText;
                targetTextBlock.Opacity = 1.0;
                transform.X = 0;
                return;
            }

            // 1. Outbound Animation: Slide to the left (-14px) with smooth cubic easing while fading out
            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(160))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            var slideLeft = new DoubleAnimation(0.0, -14.0, TimeSpan.FromMilliseconds(160))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            slideLeft.Completed += (s, e) =>
            {
                targetTextBlock.Text = newText;
                transform.X = 14.0;

                // 2. Inbound Animation: Glide in from the right (+14px) into center (0px) with smooth cubic easing
                var fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var slideFromRight = new DoubleAnimation(14.0, 0.0, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                targetTextBlock.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                transform.BeginAnimation(TranslateTransform.XProperty, slideFromRight);
            };

            targetTextBlock.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            transform.BeginAnimation(TranslateTransform.XProperty, slideLeft);
        }

        private async void ScanHardware_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            try
            {
                if (BtnScanHardware != null) BtnScanHardware.IsEnabled = false;
                if (BtnSelectImage != null) BtnSelectImage.IsEnabled = false;
                if (BtnOcrSettings != null) BtnOcrSettings.IsEnabled = false;

                OverlayProcessing.Visibility = Visibility.Visible;
                if (TxtProcessingPhaseBadge != null)
                    TxtProcessingPhaseBadge.Text = "PHASE 1 OF 2 • OPTICAL HARDWARE SCAN";
                if (TxtProcessingTitle != null)
                    TxtProcessingTitle.Text = "Scanning Document from Printer...";

                OcrStageProgressBar.IsIndeterminate = true;
                TransitionStatusText(TxtProcessingStep, "Hardware Optical Acquisition", animate: false);
                TransitionStatusText(TxtCurrentExtractingField, "Connecting to optical scanner feed...", animate: false);

                _processCts?.Cancel();
                _processCts = new CancellationTokenSource();
                var scanToken = _processCts.Token;

                string scannedFile = null;
                using (var scanMsgCts = CancellationTokenSource.CreateLinkedTokenSource(scanToken))
                {
                    var msgToken = scanMsgCts.Token;
                    var scanMessagesTask = Task.Run(async () =>
                    {
                        var hwMessages = new[]
                        {
                            ("Calibrating Scanner Head", "Positioning optical carriage for 8.5 x 13 form..."),
                            ("Optical Sensor Sweep", $"Scanning physical document surface at {_scannerSettings.Dpi} DPI..."),
                            ("Acquiring Image Buffer", "Capturing high-contrast optical scan lines..."),
                            ("Optimizing Scan Exposure", "Balancing flatbed illumination & paper contrast..."),
                            ("Reading Hardware Stream", "Buffering full-page document image data..."),
                            ("Finalizing Hardware Scan", "Transferring scanned image into SOLACE...")
                        };

                        int msgIdx = 0;
                        while (!msgToken.IsCancellationRequested)
                        {
                            try
                            {
                                await Task.Delay(3500, msgToken).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                            if (msgToken.IsCancellationRequested) break;

                            var pair = hwMessages[msgIdx % hwMessages.Length];
                            msgIdx++;

                            await Dispatcher.InvokeAsync(() =>
                            {
                                if (!msgToken.IsCancellationRequested)
                                {
                                    TransitionStatusText(TxtProcessingStep, pair.Item1, animate: true);
                                    TransitionStatusText(TxtCurrentExtractingField, pair.Item2, animate: true);
                                }
                            });
                        }
                    }, msgToken);

                    scannedFile = await WiaScannerService.ScanDocumentAsync(
                        _scannerSettings,
                        status => Dispatcher.InvokeAsync(() =>
                        {
                            if (!string.IsNullOrWhiteSpace(status))
                            {
                                TransitionStatusText(TxtCurrentExtractingField, status, animate: true);
                            }
                        })
                    );

                    scanMsgCts.Cancel();
                }

                OcrStageProgressBar.IsIndeterminate = false;

                if (!string.IsNullOrEmpty(scannedFile) && File.Exists(scannedFile))
                {
                    _sessionScannedFiles.Add(scannedFile);
                    await ProcessDocumentAsync(scannedFile, isPhaseTwoOfHardwareScan: true);
                }
            }
            catch (OperationCanceledException)
            {
                OcrStageProgressBar.IsIndeterminate = false;
                OverlayProcessing.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                OcrStageProgressBar.IsIndeterminate = false;
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
                if (BtnOcrSettings != null) BtnOcrSettings.IsEnabled = true;
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

        public async Task ProcessDocumentAsync(string imagePath, bool isPhaseTwoOfHardwareScan = false)
        {
            CurrentImagePath = imagePath;
            _currentDeskewedImagePath = null;
            var window = Window.GetWindow(this);

            if (EmptyCanvasState != null)
                EmptyCanvasState.Visibility = Visibility.Collapsed;

            OverlayProcessing.Visibility = Visibility.Visible;
            if (TxtProcessingPhaseBadge != null)
            {
                TxtProcessingPhaseBadge.Text = isPhaseTwoOfHardwareScan
                    ? "PHASE 2 OF 2 • AI OCR EXTRACTION"
                    : "AI OCR EXTRACTION";
            }
            if (TxtProcessingTitle != null)
            {
                TxtProcessingTitle.Text = "Ingesting Application Form...";
            }

            if (!await OcrService.IsServerOnlineAsync())
            {
                TransitionStatusText(TxtProcessingStep, "Warming Up AI OCR Engine...", animate: isPhaseTwoOfHardwareScan);
                TransitionStatusText(TxtCurrentExtractingField, "Loading OpenVINO neural network models...", animate: isPhaseTwoOfHardwareScan);
                OcrStageProgressBar.IsIndeterminate = true;
                bool warm = await OcrService.EnsureServerRunningAsync(25);
                OcrStageProgressBar.IsIndeterminate = false;
                if (!warm)
                {
                    throw new Exception("Local OCR server is offline and could not be started automatically. Please click 'Retry' or verify Python installation.");
                }
            }

            OcrStageProgressBar.IsIndeterminate = false;
            OcrStageProgressBar.Value = 18;
            TransitionStatusText(TxtProcessingStep, "Step 1 of 4: Document Alignment & Contrast", animate: isPhaseTwoOfHardwareScan);
            TransitionStatusText(TxtCurrentExtractingField, "Straightening page tilt & suppressing scanner borders...", animate: isPhaseTwoOfHardwareScan);

            var bitmap = new BitmapImage();
            using (var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = fs;
                bitmap.EndInit();
            }
            bitmap.Freeze();
            ImgDocument.Source = bitmap;
            ImgDocument.Width = DocScrollViewer.ActualWidth > 50 ? DocScrollViewer.ActualWidth - 20 : 440;
            ZoomFit_Click(null, null);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _processCts?.Cancel();
                _processCts = new CancellationTokenSource();
                var token = _processCts.Token;

                using (var ctsLinked = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    var linkedToken = ctsLinked.Token;
                    var progressTask = Task.Run(async () =>
                    {
                        var steps = new[]
                        {
                            (32, "Step 2 of 4: Form Landmark Registration", "Detecting table grid lines & section anchors..."),
                            (48, "Step 2 of 4: Reading Section I Demographics", "Extracting Last Name, First Name, Middle Name..."),
                            (64, "Step 2 of 4: Reading Address & Barangay", "Matching Biñan barangay & residential address..."),
                            (76, "Step 2 of 4: Reading Socioeconomic Data", "Extracting occupation, income & emergency contact..."),
                            (86, "Step 3 of 4: Analyzing Encircled Circumstances", "Detecting pen markings in Section II..."),
                            (93, "Step 3 of 4: Reading Family Dependents Table", "Extracting child records & ages from Section III..."),
                            (97, "Step 4 of 4: Finalizing Field Extraction", "Computing confidence scores & normalizations...")
                        };

                        int stepIdx = 0;
                        while (!linkedToken.IsCancellationRequested)
                        {
                            try
                            {
                                await Task.Delay(3500, linkedToken).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                            if (linkedToken.IsCancellationRequested) break;

                            int idx = Math.Min(stepIdx, steps.Length - 1);
                            var curStep = steps[idx];
                            stepIdx++;

                            await Dispatcher.InvokeAsync(() =>
                            {
                                if (!linkedToken.IsCancellationRequested)
                                {
                                    OcrStageProgressBar.Value = curStep.Item1;
                                    TransitionStatusText(TxtProcessingStep, curStep.Item2, animate: true);
                                    TransitionStatusText(TxtCurrentExtractingField, curStep.Item3, animate: true);
                                }
                            });
                        }
                    }, linkedToken);

                    OcrResponse = await OcrService.ExtractFormAsync(imagePath, "global", token);
                    ctsLinked.Cancel();
                }

                stopwatch.Stop();
                if (OcrResponse != null && OcrResponse.ExecutionTimeSeconds <= 0)
                {
                    OcrResponse.ExecutionTimeSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 2);
                }

                OcrStageProgressBar.Value = 100;
                TransitionStatusText(TxtProcessingStep, "Step 4 of 4: Extraction Complete!", animate: true);
                TransitionStatusText(TxtCurrentExtractingField, "Extracted all fields with confidence scores.", animate: true);

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
                        string uniqueDeskewed = Path.Combine(Path.GetTempPath(), $"solace_deskewed_{DateTime.Now:yyyyMMdd_HHmmss}_{DateTime.Now.Ticks % 10000}.jpg");
                        File.WriteAllBytes(uniqueDeskewed, bytes);
                        _currentDeskewedImagePath = uniqueDeskewed;
                        _sessionScannedFiles.Add(uniqueDeskewed);

                        string deskewedTemp = Path.Combine(Path.GetTempPath(), "solace_last_deskewed_preview.jpg");
                        File.WriteAllBytes(deskewedTemp, bytes);
                    }
                    catch { }
                }

                DocumentProcessed?.Invoke(OcrResponse, imagePath);
            }
            catch (OperationCanceledException)
            {
                // OCR scan cancelled by user force stop
                System.Diagnostics.Debug.WriteLine("[OcrDocumentViewport] OCR processing cancelled by user.");
            }
            catch (Exception ex)
            {
                if (_processCts != null && _processCts.IsCancellationRequested) return;
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
            var boxesToSearch = new Dictionary<string, double[]>(_fieldBBoxes ?? new Dictionary<string, double[]>());
            if (OcrResponse?.Fields != null)
            {
                foreach (var kvp in OcrResponse.Fields)
                {
                    if (kvp.Value?.Bbox != null && kvp.Value.Bbox.Length == 4)
                    {
                        boxesToSearch[kvp.Key] = kvp.Value.Bbox;
                    }
                }
            }

            if (boxesToSearch.Count == 0) return;

            string bestKey = null;
            double minArea = double.MaxValue;

            foreach (var kvp in boxesToSearch)
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
                // When clicking a box directly on the document, highlight without jarring auto-center pan/zoom
                FocusOnField(bestKey, autoCenter: false);
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
