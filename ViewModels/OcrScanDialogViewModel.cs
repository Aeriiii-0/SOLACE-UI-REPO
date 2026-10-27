using System;
using System.ComponentModel;
using System.Windows.Media;
using SOLUM_UI.Services;

namespace SOLUM_UI.ViewModels
{
    /// <summary>
    /// Presentation state and stepper orchestrator for OCR Ingestion & Verification dialog.
    /// Manages the 3-step wizard workflow, AI engine health/warmup monitoring, and overall confidence scores.
    /// </summary>
    public class OcrScanDialogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private int _currentStep = 1;
        private string _stepHintText = "Step 1 of 3 — Primary Information";
        private string _modeStripeHintText = " — Scanned CSWDO-066-2 form. Review fields on the right, synchronized with the scan preview.";
        private string _overallConfidenceText = "Overall AI Confidence: 0%";
        private string _engineStatusHeader = "AI Engine: Checking...";
        private string _engineStatusTooltip = "Click to check OCR engine status or restart server";
        private SolidColorBrush _engineStatusDotBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA500"));
        private SolidColorBrush _engineStatusGlowBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA500"));
        private bool _isWarmupBannerVisible = false;
        private string _warmupTitle = "AI OCR Engine Warming Up...";
        private string _warmupStepDetail = "Initializing local AI background service & OpenVINO models...";
        private string _warmupElapsedText = "0.0s elapsed";
        private string _warmupEstimatedTimeText = "~6s remaining";
        private double _warmupProgressPercent = 15;
        private bool _canRetryWarmup = false;
        private bool _canDismissWarmup = false;
        private string _scannerStatusText = "Status: Scanner Ready (300 DPI)";

        public OcrScanDialogViewModel()
        {
        }

        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                if (_currentStep != value)
                {
                    _currentStep = value;
                    OnPropertyChanged(nameof(CurrentStep));
                    OnPropertyChanged(nameof(CanGoBack));
                    OnPropertyChanged(nameof(CanGoNext));
                    OnPropertyChanged(nameof(CanSave));
                    UpdateStepHints();
                }
            }
        }

        public bool CanGoBack => _currentStep > 1;
        public bool CanGoNext => _currentStep < 3;
        public bool CanSave => _currentStep == 3;

        public string StepHintText
        {
            get => _stepHintText;
            set
            {
                if (_stepHintText != value)
                {
                    _stepHintText = value;
                    OnPropertyChanged(nameof(StepHintText));
                }
            }
        }

        public string ModeStripeHintText
        {
            get => _modeStripeHintText;
            set
            {
                if (_modeStripeHintText != value)
                {
                    _modeStripeHintText = value;
                    OnPropertyChanged(nameof(ModeStripeHintText));
                }
            }
        }

        public string OverallConfidenceText
        {
            get => _overallConfidenceText;
            set
            {
                if (_overallConfidenceText != value)
                {
                    _overallConfidenceText = value;
                    OnPropertyChanged(nameof(OverallConfidenceText));
                }
            }
        }

        public string EngineStatusHeader
        {
            get => _engineStatusHeader;
            set
            {
                if (_engineStatusHeader != value)
                {
                    _engineStatusHeader = value;
                    OnPropertyChanged(nameof(EngineStatusHeader));
                }
            }
        }

        public string EngineStatusTooltip
        {
            get => _engineStatusTooltip;
            set
            {
                if (_engineStatusTooltip != value)
                {
                    _engineStatusTooltip = value;
                    OnPropertyChanged(nameof(EngineStatusTooltip));
                }
            }
        }

        public SolidColorBrush EngineStatusDotBrush
        {
            get => _engineStatusDotBrush;
            set
            {
                if (_engineStatusDotBrush != value)
                {
                    _engineStatusDotBrush = value;
                    OnPropertyChanged(nameof(EngineStatusDotBrush));
                }
            }
        }

        public SolidColorBrush EngineStatusGlowBrush
        {
            get => _engineStatusGlowBrush;
            set
            {
                if (_engineStatusGlowBrush != value)
                {
                    _engineStatusGlowBrush = value;
                    OnPropertyChanged(nameof(EngineStatusGlowBrush));
                }
            }
        }

        public bool IsWarmupBannerVisible
        {
            get => _isWarmupBannerVisible;
            set
            {
                if (_isWarmupBannerVisible != value)
                {
                    _isWarmupBannerVisible = value;
                    OnPropertyChanged(nameof(IsWarmupBannerVisible));
                }
            }
        }

        public string WarmupTitle
        {
            get => _warmupTitle;
            set
            {
                if (_warmupTitle != value)
                {
                    _warmupTitle = value;
                    OnPropertyChanged(nameof(WarmupTitle));
                }
            }
        }

        public string WarmupStepDetail
        {
            get => _warmupStepDetail;
            set
            {
                if (_warmupStepDetail != value)
                {
                    _warmupStepDetail = value;
                    OnPropertyChanged(nameof(WarmupStepDetail));
                }
            }
        }

        public string WarmupElapsedText
        {
            get => _warmupElapsedText;
            set
            {
                if (_warmupElapsedText != value)
                {
                    _warmupElapsedText = value;
                    OnPropertyChanged(nameof(WarmupElapsedText));
                }
            }
        }

        public string WarmupEstimatedTimeText
        {
            get => _warmupEstimatedTimeText;
            set
            {
                if (_warmupEstimatedTimeText != value)
                {
                    _warmupEstimatedTimeText = value;
                    OnPropertyChanged(nameof(WarmupEstimatedTimeText));
                }
            }
        }

        public double WarmupProgressPercent
        {
            get => _warmupProgressPercent;
            set
            {
                if (Math.Abs(_warmupProgressPercent - value) > 0.01)
                {
                    _warmupProgressPercent = value;
                    OnPropertyChanged(nameof(WarmupProgressPercent));
                }
            }
        }

        public bool CanRetryWarmup
        {
            get => _canRetryWarmup;
            set
            {
                if (_canRetryWarmup != value)
                {
                    _canRetryWarmup = value;
                    OnPropertyChanged(nameof(CanRetryWarmup));
                }
            }
        }

        public bool CanDismissWarmup
        {
            get => _canDismissWarmup;
            set
            {
                if (_canDismissWarmup != value)
                {
                    _canDismissWarmup = value;
                    OnPropertyChanged(nameof(CanDismissWarmup));
                }
            }
        }

        public string ScannerStatusText
        {
            get => _scannerStatusText;
            set
            {
                if (_scannerStatusText != value)
                {
                    _scannerStatusText = value;
                    OnPropertyChanged(nameof(ScannerStatusText));
                }
            }
        }

        public void SetConfidence(double confidence, string rating)
        {
            int pct = (int)(confidence * 100);
            OverallConfidenceText = $"Overall AI Confidence: {pct}% ({rating})";
        }

        private void UpdateStepHints()
        {
            if (_currentStep == 1)
            {
                StepHintText = "Step 1 of 3 — Primary Information";
                ModeStripeHintText = " — Scanned CSWDO-066-2 form. Review personal details & verification status.";
            }
            else if (_currentStep == 2)
            {
                StepHintText = "Step 2 of 3 — Secondary Information";
                ModeStripeHintText = " — Scanned CSWDO-066-2 form. Review address, emergency contact, and employment.";
            }
            else
            {
                StepHintText = "Step 3 of 3 — Circumstances & Family Composition";
                ModeStripeHintText = " — Scanned CSWDO-066-2 form. Review circumstances, family composition, and needs.";
            }
        }
    }
}
