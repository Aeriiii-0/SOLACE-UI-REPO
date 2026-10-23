using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SOLUM_UI.Models;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services;
using SOLUM_UI.Services.Api;
using SOLUM_UI.Views.Dialogs;

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
        private SoloParentRecord _foundRecord;
        private bool _duplicateCheckPassed = false;

        // Observable Family / Dependents Collection bound to ItemsControl
        private readonly ObservableCollection<FamilyMemberRow> _familyRowData = new ObservableCollection<FamilyMemberRow>();
        private FamilyMemberRow _editingFamilyMember = null;

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

            Loaded += (s, e) => SuppressDatePickerWatermarks();
            Closed += (s, e) =>
            {
                OcrService.WarmupProgressChanged -= OnOcrWarmupProgressChanged;
                _dismissCts?.Cancel();
            };

            OcrService.WarmupProgressChanged += OnOcrWarmupProgressChanged;

            FamilyCardsList.ItemsSource = _familyRowData;
            _familyRowData.CollectionChanged += (s, e) => UpdateFamilyCardsEmptyState();
            UpdateFamilyCardsEmptyState();

            _fieldBBoxes = OcrService.GetTemplateBoundingBoxes();
            DpDateOfApplication.SelectedDate = DateTime.Today;

            UpdateScannerStatusBadge();
            CheckServerHealth();
            SOLUM_UI.Services.FormHelper.BindSameAsApplicantAddress(ChkSameAsApplicant, TxtAddress, TxtEmergencyAddress);
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
                DialogShell.MaxHeight = Math.Min(screenH * 0.96, 920);
                DialogShell.Height    = Math.Min(screenH * 0.93, 890);
                DialogShell.Width     = Math.Min(screenW * 0.96, 1480);
                DialogShell.MinWidth  = Math.Min(screenW * 0.92, 1140);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_savedSuccessfully) return;
            if (!HasAnyInput()) return;

            bool discard = SOLUM_UI.Views.Dialogs.ActionConfirmDialog.Show(
                title: "Unsaved Changes",
                message: "You have unsaved information in this OCR session. Are you sure you want to discard changes and close?",
                confirmText: "Discard & Close",
                cancelText: "Continue Editing",
                theme: SOLUM_UI.Views.Dialogs.ConfirmThemeType.Warning,
                owner: this);

            if (!discard)
                e.Cancel = true;
        }

        private bool HasAnyInput()
        {
            return !string.IsNullOrWhiteSpace(TxtLastName?.Text) ||
                   !string.IsNullOrWhiteSpace(TxtFirstName?.Text) ||
                   !string.IsNullOrWhiteSpace(_currentImagePath);
        }

        /// <summary>
        /// Suppresses the default "Select a date" watermark in DatePickerTextBox controls
        /// to avoid overlapping with our custom MM/DD/YYYY placeholder in main form fields.
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

        #region Server Health Check & Engine Warmup Indicator

        private enum OcrEngineState
        {
            Loading,
            Ready,
            Failed
        }

        private CancellationTokenSource _dismissCts;
        private bool _isSpinning = false;

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

        private void WarmupRetry_Click(object sender, RoutedEventArgs e)
        {
            CheckServerHealth();
        }

        private void OnOcrWarmupProgressChanged(OcrWarmupProgress p)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (p == null) return;

                if (p.IsReady)
                {
                    StopWarmupSpinner();
                    TxtHeaderEngineStatus.Text = "AI Engine: Ready (Offline)";
                    UpdateEngineStatus(OcrEngineState.Ready, "OCR Engine: Ready (Online) — Click to re-check");

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
                    TxtHeaderEngineStatus.Text = "AI Engine: Offline";
                    UpdateEngineStatus(OcrEngineState.Failed, "OCR Engine: Offline — Click to retry");

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

                    TxtHeaderEngineStatus.Text = $"AI Engine: Warming up (~{p.EstimatedSecondsRemaining:F0}s)";
                    UpdateEngineStatus(OcrEngineState.Loading, $"OCR Engine: Warming Up... (~{p.EstimatedSecondsRemaining:F0}s remaining)");
                }
            });
        }

        private void UpdateEngineStatus(OcrEngineState state, string message)
        {
            Color dotColor;

            switch (state)
            {
                case OcrEngineState.Ready:
                    dotColor = (Color)ColorConverter.ConvertFromString("#22C55E"); // Vibrant Green
                    break;
                case OcrEngineState.Loading:
                    dotColor = (Color)ColorConverter.ConvertFromString("#F59E0B"); // Warm Amber
                    break;
                case OcrEngineState.Failed:
                default:
                    dotColor = (Color)ColorConverter.ConvertFromString("#EF4444"); // Red
                    break;
            }

            ServerStatusBadge.Background = new SolidColorBrush(dotColor);
            if (BadgeGlow != null)
            {
                BadgeGlow.Color = dotColor;
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
            PillEngineStatus.ToolTip = tip;
        }

        private async void CheckServerHealth()
        {
            _dismissCts?.Cancel();
            bool online = await OcrService.IsServerOnlineAsync();
            if (online)
            {
                TxtHeaderEngineStatus.Text = "AI Engine: Ready (Offline)";
                UpdateEngineStatus(OcrEngineState.Ready, "OCR Engine: Ready (Online) - Click to re-check");
                OcrWarmupBanner.Visibility = Visibility.Collapsed;
                return;
            }

            TxtHeaderEngineStatus.Text = "AI Engine: Warming up...";
            UpdateEngineStatus(OcrEngineState.Loading, "OCR Engine: Starting...");
            OcrWarmupBanner.Visibility = Visibility.Visible;

            await OcrService.EnsureServerRunningAsync(25);
        }

        private void ServerStatusBadge_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (OcrWarmupBanner.Visibility == Visibility.Visible)
            {
                CheckServerHealth();
            }
            else
            {
                OcrWarmupBanner.Visibility = Visibility.Visible;
                CheckServerHealth();
            }
        }

        #endregion

        #region Step Navigation

        private void Step1Header_Click(object sender, MouseButtonEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
            GoToStep(1);
        }

        private async void Step2Header_Click(object sender, MouseButtonEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
            if (!ValidateStep1()) return;
            if (!await CheckDuplicateAsync()) return;
            GoToStep(2);
        }

        private async void Step3Header_Click(object sender, MouseButtonEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
            if (!ValidateStep1()) { GoToStep(1); return; }
            if (!await CheckDuplicateAsync()) { GoToStep(1); return; }
            if (!ValidateStep2()) return;
            GoToStep(3);
        }

        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
            if (_currentStep == 1)
            {
                if (!ValidateStep1()) return;
                if (!await CheckDuplicateAsync()) return;
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

        private async Task<bool> CheckDuplicateAsync()
        {
            if (_duplicateCheckPassed) return true;

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
                        " You can view the existing record or continue ingesting this scanned document.";
                    DuplicateBannerPanel.Visibility = Visibility.Visible;
                    return false;
                }
            }
            catch
            {
                // If API/database check fails, allow user to proceed
            }
            finally
            {
                BtnNext.IsEnabled = true;
            }

            return true;
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

            ScrollStep1.Visibility = step == 1 ? Visibility.Visible  : Visibility.Collapsed;
            ScrollStep2.Visibility = step == 2 ? Visibility.Visible  : Visibility.Collapsed;
            ScrollStep3.Visibility = step == 3 ? Visibility.Visible  : Visibility.Collapsed;

            BtnBack.Visibility     = step > 1 ? Visibility.Visible  : Visibility.Collapsed;
            BtnNext.Visibility     = step < 3 ? Visibility.Visible  : Visibility.Collapsed;
            BtnSave.Visibility     = step == 3 ? Visibility.Visible : Visibility.Collapsed;

            if (step == 1)
            {
                Step1Dot.Background      = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));
                Step1DotLabel.Foreground = new SolidColorBrush(Colors.White);
                Step1Title.Foreground    = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));

                Step2Dot.Background      = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
                Step2DotLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
                Step2Title.Foreground    = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));

                Step3Dot.Background      = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
                Step3DotLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
                Step3Title.Foreground    = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
            }
            else if (step == 2)
            {
                Step1Dot.Background      = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));
                Step1DotLabel.Foreground = new SolidColorBrush(Colors.White);
                Step1Title.Foreground    = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));

                Step2Dot.Background      = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));
                Step2DotLabel.Foreground = new SolidColorBrush(Colors.White);
                Step2Title.Foreground    = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));

                Step3Dot.Background      = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
                Step3DotLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
                Step3Title.Foreground    = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
            }
            else
            {
                Step1Dot.Background      = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));
                Step1DotLabel.Foreground = new SolidColorBrush(Colors.White);
                Step1Title.Foreground    = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));

                Step2Dot.Background      = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));
                Step2DotLabel.Foreground = new SolidColorBrush(Colors.White);
                Step2Title.Foreground    = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));

                Step3Dot.Background      = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));
                Step3DotLabel.Foreground = new SolidColorBrush(Colors.White);
                Step3Title.Foreground    = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));
            }

            if (FormStepHint != null)
            {
                if (step == 1)
                {
                    FormStepHint.Text = "Step 1 of 3 — Primary Information";
                    if (ModeStripeHint != null)
                        ModeStripeHint.Text = " — Scanned CSWDO-066-2 form. Review personal details & verification status.";
                }
                else if (step == 2)
                {
                    FormStepHint.Text = "Step 2 of 3 — Secondary Information";
                    if (ModeStripeHint != null)
                        ModeStripeHint.Text = " — Scanned CSWDO-066-2 form. Review address, emergency contact, and employment.";
                }
                else
                {
                    FormStepHint.Text = "Step 3 of 3 — Circumstances & Family Composition";
                    if (ModeStripeHint != null)
                        ModeStripeHint.Text = " — Scanned CSWDO-066-2 form. Review circumstances, family composition, and needs.";
                }
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
                ActionConfirmDialog.Warning(
                    "Scanner Notice",
                    $"{ex.Message}\n\nYou can also click 'Upload File' if you already have a scanned image.",
                    this);
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
                ActionConfirmDialog.Error("OCR Error", "Failed to process document: " + ex.Message, this);
            }
            finally
            {
                OverlayProcessing.Visibility = Visibility.Collapsed;
            }
        }

        private void PopulateHitlFields(OcrFormResponse resp)
        {
            if (resp == null) return;
            _duplicateCheckPassed = false;
            if (DuplicateBannerPanel != null) DuplicateBannerPanel.Visibility = Visibility.Collapsed;
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
                string isEmp = GetVal(resp, "is_employed").ToLower();
                string isSelf = GetVal(resp, "is_self_employed").ToLower();
                string isNot = GetVal(resp, "is_not_employed").ToLower();

                if (empRaw.Contains("self") || isSelf == "true" || isSelf == "1") SetComboByContent(CmbEmploymentStatus, "Self-Employed");
                else if (empRaw.Contains("not") || empRaw.Contains("unemployed") || isNot == "true" || isNot == "1") SetComboByContent(CmbEmploymentStatus, "Not Employed");
                else if (empRaw.Contains("employed") || isEmp == "true" || isEmp == "1") SetComboByContent(CmbEmploymentStatus, "Employed");
                else CmbEmploymentStatus.SelectedIndex = 0;

                TxtAddress.Text   = GetVal(resp, "address");
                SetComboByContent(CmbBarangay, OcrService.FuzzyMatchBarangay(GetVal(resp, "barangay")));
                TxtContact.Text   = OcrService.FormatPhoneNumber(GetVal(resp, "contact_number"));

                TxtEmergencyContact.Text = OcrService.CleanPersonName(GetVal(resp, "emergency_name"));
                TxtRelationship.Text     = OcrService.FuzzyMatchRelationship(GetVal(resp, "emergency_relationship"));
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
                    TxtEmergencyAddress.Text = emAddr;
                }
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
                    if (ChkA6.IsChecked == true && CmbA6Declaration != null)
                    {
                        string subcode = resp.Circumstance.SubfieldCause?.ToLower() ?? string.Empty;
                        if (subcode.Contains("nullity")) SetComboByContent(CmbA6Declaration, "Nullity of marriage");
                        else if (subcode.Contains("annul")) SetComboByContent(CmbA6Declaration, "Annulment of marriage");
                        else CmbA6Declaration.SelectedIndex = 0;
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

                        var ocrRow = MakeFamilyRow();
                        ocrRow.MemberName          = OcrService.CleanPersonName(m.MemberName);
                        ocrRow.Sex                 = OcrService.IsOcrNotApplicable(m.Sex) ? string.Empty : OcrService.FuzzyMatchSex(m.Sex);
                        ocrRow.CivilStatus         = OcrService.IsOcrNotApplicable(m.CivilStatus) ? string.Empty : OcrService.FuzzyMatchCivilStatus(m.CivilStatus);
                        ocrRow.Relationship        = OcrService.IsOcrNotApplicable(m.Relationship) ? string.Empty : OcrService.FuzzyMatchRelationship(m.Relationship);
                        ocrRow.EducationEmployment = OcrService.IsOcrNotApplicable(m.EducationEmployment) ? string.Empty : m.EducationEmployment;
                        ocrRow.Income              = OcrService.IsOcrNotApplicable(m.Income) ? "0" : OcrService.FuzzyMatchIncome(m.Income);
                        if (dt.HasValue && dt.Value != DateTime.MinValue)
                            ocrRow.BirthdateDate = dt.Value;
                        else if (!string.IsNullOrWhiteSpace(normDob))
                            ocrRow.Birthdate = normDob;
                        _familyRowData.Add(ocrRow);
                    }
                }

                UpdateFamilyCardsEmptyState();

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

            // 2. Specific matching for Sex if combo is CmbSex
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

            // 3. Specific matching for Civil Status if combo is CmbCivilStatus
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

            // 4. Normalized prefix / contains match (e.g. "College Level" or "BS Nursing" matches "College")
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
                    this);
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

        private bool _isFormattingPhilSys = false;

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

        private void ToUpperBox(TextBox tb)
        {
            if (tb == null) return;
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

        private void Phone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[\d\-]+$");
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void Circumstance_Changed(object sender, RoutedEventArgs e)
        {
            if (PnlA2 != null) PnlA2.Visibility = ChkA2.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (PnlA4 != null) PnlA4.Visibility = ChkA4.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (PnlA5 != null) PnlA5.Visibility = ChkA5.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (PnlA6 != null) PnlA6.Visibility = ChkA6.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (PnlB  != null) PnlB.Visibility  = ChkB.IsChecked  == true ? Visibility.Visible : Visibility.Collapsed;
        }

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
                    this);

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
            string name = TxtMemberName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ActionConfirmDialog.Warning("Validation", "Please enter the member's full name.", this);
                TxtMemberName.Focus();
                return;
            }

            string rel = (CmbMemberRel.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(rel))
            {
                ActionConfirmDialog.Warning("Validation", "Please select a relationship.", this);
                CmbMemberRel.Focus();
                return;
            }

            string sex = (CmbMemberSex.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(sex))
            {
                ActionConfirmDialog.Warning("Validation", "Please select the member's sex.", this);
                CmbMemberSex.Focus();
                return;
            }

            if (!DpMemberDob.SelectedDate.HasValue)
            {
                ActionConfirmDialog.Warning("Validation", "Please select a valid birthdate.", this);
                DpMemberDob.Focus();
                return;
            }

            DateTime dob = DpMemberDob.SelectedDate.Value;
            int age = SOLUM_UI.Services.OcrService.CalculateAge(dob);
            if (age < 0 || age > 120) age = 0;

            string civil = (CmbMemberCivil.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(civil)) civil = "Single";

            string edu = TxtMemberEdu.Text?.Trim() ?? string.Empty;
            string income = TxtMemberIncome.Text?.Trim();
            if (string.IsNullOrWhiteSpace(income)) income = "0.00";

            bool hasSn = ChkMemberSpecialNeeds.IsChecked == true;
            string snCat = hasSn ? ((CmbSpecialNeedsCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty) : string.Empty;
            if (hasSn && string.IsNullOrWhiteSpace(snCat))
            {
                ActionConfirmDialog.Warning("Validation", "Please select a special needs category.", this);
                CmbSpecialNeedsCategory.Focus();
                return;
            }
            string snDesc = hasSn ? (TxtSpecialNeedsDescription.Text?.Trim() ?? string.Empty) : string.Empty;

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
                int age = SOLUM_UI.Services.OcrService.CalculateAge(DpMemberDob.SelectedDate.Value);
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
        

        private void TxtLastName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ToUpperBox(TxtLastName);
            ClearFieldError(TxtLastName, ErrLastName);
            MarkBadgeEdited(Badge_LastName, TxtConf_LastName);
            _duplicateCheckPassed = false;
            if (DuplicateBannerPanel != null) DuplicateBannerPanel.Visibility = Visibility.Collapsed;
        }

        private void TxtFirstName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ToUpperBox(TxtFirstName);
            ClearFieldError(TxtFirstName, ErrFirstName);
            MarkBadgeEdited(Badge_FirstName, TxtConf_FirstName);
            _duplicateCheckPassed = false;
            if (DuplicateBannerPanel != null) DuplicateBannerPanel.Visibility = Visibility.Collapsed;
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

        private void TxtMiddleName_TextChanged(object sender, TextChangedEventArgs e)  { ToUpperBox(TxtMiddleName); MarkBadgeEdited(Badge_MiddleName, TxtConf_MiddleName); }
        private void TxtExtension_TextChanged(object sender, TextChangedEventArgs e)    { ToUpperBox(TxtExtension); }
        private void TxtPhilSys_TextChanged(object sender, TextChangedEventArgs e)
        {
            FormatPhilSysBox(TxtPhilSys);
            ClearFieldError(TxtPhilSys, ErrPhilSys);
            MarkBadgeEdited(Badge_PhilSys, TxtConf_PhilSys);
            _duplicateCheckPassed = false;
            if (DuplicateBannerPanel != null) DuplicateBannerPanel.Visibility = Visibility.Collapsed;
        }
        private void TxtReligion_TextChanged(object sender, TextChangedEventArgs e)     { ToUpperBox(TxtReligion); }
        private void TxtOccupation_TextChanged(object sender, TextChangedEventArgs e)   { ToUpperBox(TxtOccupation); }
        private void TxtEmergencyContact_TextChanged(object sender, TextChangedEventArgs e) { ToUpperBox(TxtEmergencyContact); MarkBadgeEdited(Badge_EmergencyContact, TxtConf_EmergencyContact); }
        private void TxtEmergencyAddress_TextChanged(object sender, TextChangedEventArgs e) { ToUpperBox(TxtEmergencyAddress); }
        private void TxtA2Cause_TextChanged(object sender, TextChangedEventArgs e)      { }
        private void TxtA4Disability_TextChanged(object sender, TextChangedEventArgs e) { }

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

            string barangayValue = GetComboValue(CmbBarangay);
            if (string.IsNullOrWhiteSpace(barangayValue))
            { MarkError(CmbBarangay, ErrBarangay, "Required."); missing.Add("Barangay"); ok = false; }

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
                if (row.HasSpecialNeeds && string.IsNullOrWhiteSpace(row.SpecialNeedsCategory)) rowMissing.Add("Special Needs Category");

                if (rowMissing.Count > 0)
                {
                    missing.Add($"Family member #{rowNum} ({row.MemberName}) — missing: {string.Join(", ", rowMissing)}");
                    ok = false;
                }
            }

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
                ActionConfirmDialog.Error(
                    "Save Error",
                    "An error occurred while saving:\n\n" + ex.Message +
                    "\n\nYour form data has been preserved.",
                    this);
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
                        Income              = row.Income,
                        HasSpecialNeeds     = row.HasSpecialNeeds,
                        SpecialNeedsCategory = row.SpecialNeedsCategory,
                        SpecialNeedsDescription = row.SpecialNeedsDescription
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
                Barangay      = GetComboValue(CmbBarangay),
                Status        = GetComboValue(CmbStatus),
                LastUpdated   = DateTime.Now,

                // ApplicationType is determined by entry-point flow, not a checkbox.
                // OcrScanDialog is always the Add New Record flow.
                IsNewApplicant      = true,
                IsRenewal           = false,
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
                EmergencyRelationship  = TxtRelationship.Text.Trim(),
                EmergencyAddress       = TxtEmergencyAddress.Text.Trim(),
                EmergencyContactNumber = OcrService.FormatPhoneNumber(TxtEmergencyNumber.Text.Trim()),

                CircumstanceA1             = ChkA1.IsChecked == true && ChkA1.IsEnabled,
                CircumstanceA2             = ChkA2.IsChecked == true && ChkA2.IsEnabled,
                CircumstanceA2Cause        = ChkA2.IsChecked == true ? TxtA2Cause.Text.Trim() : string.Empty,
                CircumstanceA2Date         = ChkA2.IsChecked == true ? (DpA2Date.SelectedDate ?? DateTime.MinValue) : DateTime.MinValue,
                CircumstanceA3             = ChkA3.IsChecked == true && ChkA3.IsEnabled,
                CircumstanceA4             = ChkA4.IsChecked == true && ChkA4.IsEnabled,
                CircumstanceA4Disability   = ChkA4.IsChecked == true ? TxtA4Disability.Text.Trim() : string.Empty,
                CircumstanceA5             = ChkA5.IsChecked == true && ChkA5.IsEnabled,
                CircumstanceA5Period       = ChkA5.IsChecked == true ? TxtA5Period.Text.Trim() : string.Empty,
                CircumstanceA6             = ChkA6.IsChecked == true && ChkA6.IsEnabled,
                CircumstanceA6Nullity      = ChkA6.IsChecked == true && GetComboValue(CmbA6Declaration) == "Nullity of marriage",
                CircumstanceA6Annulment    = ChkA6.IsChecked == true && GetComboValue(CmbA6Declaration) == "Annulment of marriage",
                CircumstanceA7             = ChkA7.IsChecked == true && ChkA7.IsEnabled,
                CircumstanceB              = ChkB.IsChecked == true && ChkB.IsEnabled,
                CircumstanceBStayAbroad    = ChkB.IsChecked == true ? TxtBStayAbroad.Text.Trim() : string.Empty,
                CircumstanceC              = ChkC.IsChecked == true && ChkC.IsEnabled,
                CircumstanceD              = ChkD.IsChecked == true && ChkD.IsEnabled,
                CircumstanceE              = ChkE.IsChecked == true && ChkE.IsEnabled,
                CircumstanceF              = ChkF.IsChecked == true && ChkF.IsEnabled,

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

            sec = "Additional Classifications";
            Add(sec, "Pantawid Beneficiary", r.IsPantawidBeneficiary ? "✓ Yes" : "No");
            Add(sec, "Indigenous Person",    r.IsIndigenous ? "✓ Yes" : "No");
            Add(sec, "LGBTQ+",               r.IsLGBT ? "✓ Yes" : "No");

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
                string ocrDir = SOLUM_UI.Services.OcrService.FindOcrDirectory();
                string debugPath0 = System.IO.Path.Combine(ocrDir, "debug_detections.jpg");
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string debugPath1 = System.IO.Path.Combine(appDir, "debug_detections.jpg");
                string debugPath2 = System.IO.Path.GetFullPath(System.IO.Path.Combine(appDir, @"..\..\OcrService\debug_detections.jpg"));

                string targetPath = null;
                if (System.IO.File.Exists(debugPath0)) targetPath = debugPath0;
                else if (System.IO.File.Exists(debugPath1)) targetPath = debugPath1;
                else if (System.IO.File.Exists(debugPath2)) targetPath = debugPath2;

                if (targetPath != null && System.IO.File.Exists(targetPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(targetPath) { UseShellExecute = true });
                }
                else
                {
                    ActionConfirmDialog.Info(
                        "Debug Overlay",
                        "No debug detection overlay found yet. Please run an OCR scan first using the Global Spatial Engine!",
                        this);
                }
            }
            catch (Exception ex)
            {
                ActionConfirmDialog.Warning(
                    "Debug Overlay Error",
                    $"Could not open debug detection image: {ex.Message}",
                    this);
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
