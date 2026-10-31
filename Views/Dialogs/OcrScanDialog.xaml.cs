using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SOLUM_UI.Models;
using SOLUM_UI.Services;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class OcrScanDialog : Window
    {
        public SoloParentRecord Result { get; private set; }
        public OcrScanDialogViewModel ViewModel { get; }

        private bool _savedSuccessfully = false;

        public OcrScanDialog()
        {
            InitializeComponent();
            ViewModel = new OcrScanDialogViewModel();
            DataContext = ViewModel;

            // Wire up viewport and child component events
            DocumentViewport.DocumentProcessed += OnDocumentProcessed;
            DocumentViewport.ServerStatusChanged += OnServerStatusChanged;
            DocumentViewport.DocumentFieldClicked += OnDocumentFieldClicked;

            PrimaryInfoSection.FieldFocused += tag => DocumentViewport.FocusOnField(tag);
            SecondaryInfoSection.FieldFocused += tag => DocumentViewport.FocusOnField(tag);
            FamilyMembersSection.FieldFocused += tag => DocumentViewport.FocusOnField(tag);

            PrimaryInfoSection.DuplicateContinueRequested += () => GoToStep(2);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ApplySize();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => ApplySize();

        private Point _dragStartPoint;
        private bool _isDraggingShell = false;

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isDraggingShell = true;
                _dragStartPoint = e.GetPosition(this);
                ((UIElement)sender).CaptureMouse();
            }
        }

        private void Header_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingShell && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPos = e.GetPosition(this);
                double deltaX = currentPos.X - _dragStartPoint.X;
                double deltaY = currentPos.Y - _dragStartPoint.Y;

                DialogShellTransform.X += deltaX;
                DialogShellTransform.Y += deltaY;

                _dragStartPoint = currentPos;
            }
        }

        private void Header_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingShell)
            {
                _isDraggingShell = false;
                ((UIElement)sender).ReleaseMouseCapture();
            }
        }

        private void ApplySize()
        {
            double workW = SystemParameters.WorkArea.Width;
            double workH = SystemParameters.WorkArea.Height;
            double workLeft = SystemParameters.WorkArea.Left;
            double workTop = SystemParameters.WorkArea.Top;

            double screenW = workW;
            double screenH = workH;
            double screenLeft = workLeft;
            double screenTop = workTop;

            if (Owner != null && Owner.WindowState != WindowState.Maximized && Owner.ActualWidth > 0 && Owner.ActualHeight > 0)
            {
                screenW    = Math.Min(Owner.ActualWidth, workW);
                screenH    = Math.Min(Owner.ActualHeight, workH);
                screenLeft = Math.Max(workLeft, Owner.Left);
                screenTop  = Math.Max(workTop, Owner.Top);
            }

            if (Math.Abs(Width - screenW) > 0.5) Width = screenW;
            if (Math.Abs(Height - screenH) > 0.5) Height = screenH;
            if (Math.Abs(Left - screenLeft) > 0.5) Left = screenLeft;
            if (Math.Abs(Top - screenTop) > 0.5) Top = screenTop;

            if (DialogShell != null)
            {
                double availH = Math.Min(screenH, workH);
                double availW = Math.Min(screenW, workW);
                DialogShell.MaxHeight = Math.Max(500, Math.Min(availH - 30, 840));
                DialogShell.Height    = Math.Max(500, Math.Min(availH - 45, 800));
                DialogShell.MaxWidth  = Math.Max(800, Math.Min(availW - 30, 1440));
                DialogShell.Width     = Math.Max(800, Math.Min(availW - 40, 1360));
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_savedSuccessfully) return;
            if (!HasAnyInput()) return;

            bool discard = ActionConfirmDialog.Show(
                title: "Unsaved Changes",
                message: "You have unsaved information in this OCR session. Are you sure you want to discard changes and close?",
                confirmText: "Discard & Close",
                cancelText: "Continue Editing",
                theme: ConfirmThemeType.Warning,
                owner: this);

            if (!discard)
                e.Cancel = true;
        }

        private bool HasAnyInput()
        {
            return PrimaryInfoSection.HasAnyInput() || !string.IsNullOrWhiteSpace(DocumentViewport.CurrentImagePath);
        }

        private void OnDocumentProcessed(OcrFormResponse resp, string imagePath)
        {
            if (resp == null) return;

            ViewModel.SetConfidence(resp.OverallConfidence, resp.OverallRating);

            if (resp.ExecutionTimeSeconds > 0 && BadgeOcrDuration != null && TxtOcrDuration != null)
            {
                TxtOcrDuration.Text = $"{resp.ExecutionTimeSeconds:F1}s";
                BadgeOcrDuration.Visibility = Visibility.Visible;
            }

            ViewModel.EngineStatusTooltip = "Click to re-scan document (1-min cooldown)";

            PrimaryInfoSection.Populate(resp);
            SecondaryInfoSection.Populate(resp);
            FamilyMembersSection.Populate(resp);
        }

        private void OnServerStatusChanged(bool isReady, string message)
        {
            Dispatcher.InvokeAsync(() =>
            {
                string cleanMsg = message?.Replace("AI Engine:", "OCR:") ?? "OCR: Ready";
                ViewModel.EngineStatusHeader = cleanMsg;
                Color dotColor = isReady ? (Color)ColorConverter.ConvertFromString("#22C55E") : (Color)ColorConverter.ConvertFromString("#FFA500");
                ViewModel.EngineStatusDotBrush = new SolidColorBrush(dotColor);
                if (BadgeGlow != null)
                {
                    BadgeGlow.Color = dotColor;
                }
            });
        }

        private DateTime _lastManualRescanTime = DateTime.MinValue;
        private static readonly TimeSpan RescanCooldown = TimeSpan.FromSeconds(60);

        private void PillEngineStatus_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Intercept mouse down so HeaderBorder does not capture window drag
            e.Handled = true;
            ExecuteBadgeClick();
        }

        private void ServerStatusBadge_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void PillEngineStatus_MouseEnter(object sender, MouseEventArgs e)
        {
            if (PillEngineStatus != null)
            {
                PillEngineStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3E5EB"));
                PillEngineStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4B5C1"));
            }
        }

        private void PillEngineStatus_MouseLeave(object sender, MouseEventArgs e)
        {
            if (PillEngineStatus != null)
            {
                PillEngineStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBF5F7"));
                PillEngineStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EDE0E5"));
            }
        }

        private async void ExecuteBadgeClick()
        {
            if (DocumentViewport == null) return;

            // 1. Guard against clicks while OCR processing is already actively running
            if (DocumentViewport.IsProcessingDocument)
            {
                ViewModel.EngineStatusHeader = "OCR: Scanning...";
                return;
            }

            // 2. Check if an image is loaded for re-scanning
            string currentImg = DocumentViewport.CurrentImagePath;
            if (!string.IsNullOrEmpty(currentImg) && System.IO.File.Exists(currentImg))
            {
                // Enforce 60-second safety cooldown to prevent system overload
                var elapsed = DateTime.UtcNow - _lastManualRescanTime;
                if (elapsed < RescanCooldown)
                {
                    int remainingSec = (int)Math.Ceiling((RescanCooldown - elapsed).TotalSeconds);
                    ViewModel.EngineStatusHeader = $"OCR: Wait {remainingSec}s";
                    ViewModel.EngineStatusTooltip = $"Safety cooldown active to prevent system overload ({remainingSec}s remaining).";
                    return;
                }

                _lastManualRescanTime = DateTime.UtcNow;
                ViewModel.EngineStatusHeader = "OCR: Re-scanning...";
                ViewModel.EngineStatusTooltip = "Re-scanning current document with latest template layout...";
                try
                {
                    await DocumentViewport.ProcessDocumentAsync(currentImg);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("[OcrScanDialog] Manual rescan error: " + ex.Message);
                }
                return;
            }

            // If no document is loaded, perform standard server health check
            DocumentViewport.CheckServerHealth();
        }

        #region Step Navigation

        private void Step1Header_Click(object sender, MouseButtonEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
            GoToStep(1);
        }

        private async void Step2Header_Click(object sender, MouseButtonEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
            if (!PrimaryInfoSection.Validate(out var missing))
            {
                SetValidationMessage(missing);
                return;
            }
            if (!await PrimaryInfoSection.CheckDuplicateAsync()) return;
            GoToStep(2);
        }

        private async void Step3Header_Click(object sender, MouseButtonEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
            if (!PrimaryInfoSection.Validate(out var missing1))
            {
                SetValidationMessage(missing1);
                GoToStep(1);
                return;
            }
            if (!await PrimaryInfoSection.CheckDuplicateAsync())
            {
                GoToStep(1);
                return;
            }
            if (!SecondaryInfoSection.Validate(out var missing2))
            {
                SetValidationMessage(missing2);
                GoToStep(2);
                return;
            }
            GoToStep(3);
        }

        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
            if (ViewModel.CurrentStep == 1)
            {
                if (!PrimaryInfoSection.Validate(out var missing))
                {
                    SetValidationMessage(missing);
                    return;
                }
                if (!await PrimaryInfoSection.CheckDuplicateAsync()) return;
                GoToStep(2);
            }
            else if (ViewModel.CurrentStep == 2)
            {
                if (!SecondaryInfoSection.Validate(out var missing))
                {
                    SetValidationMessage(missing);
                    return;
                }
                GoToStep(3);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            ValidationMessage.Visibility = Visibility.Collapsed;
            if (ViewModel.CurrentStep == 3)
            {
                GoToStep(2);
            }
            else if (ViewModel.CurrentStep == 2)
            {
                GoToStep(1);
            }
        }

        private void OnDocumentFieldClicked(string fieldKey)
        {
            if (string.IsNullOrEmpty(fieldKey)) return;
            string key = fieldKey.ToLowerInvariant();

            int targetStep = 1;
            if (key.StartsWith("fam_") || key.StartsWith("circ_") || key == "needs" || key == "needs_and_problems")
            {
                targetStep = 3;
            }
            else if (key.Contains("address") || key.Contains("barangay") || key.Contains("contact") ||
                     key.Contains("emergency") || key.Contains("occupation") || key.Contains("income") ||
                     key.Contains("employed") || key.Contains("employment") || key.Contains("religion") ||
                     key.Contains("pantawid") || key.Contains("indigenous") || key.Contains("lgbtq") ||
                     key.Contains("birthplace") || key.Contains("civil_status") || key.Contains("sex") ||
                     key.Contains("educational_attainment"))
            {
                targetStep = 2;
            }
            else
            {
                // Personal identity fields: name, birthdate, age, philsys, date of application
                targetStep = 1;
            }

            if (ViewModel.CurrentStep != targetStep)
            {
                GoToStep(targetStep);
            }

            if (targetStep == 1)
            {
                PrimaryInfoSection.FocusField(fieldKey);
            }
            else if (targetStep == 2)
            {
                SecondaryInfoSection.FocusField(fieldKey);
            }
            else if (targetStep == 3)
            {
                FamilyMembersSection.FocusField(fieldKey);
            }
        }

        private void GoToStep(int step)
        {
            ViewModel.CurrentStep = step;

            PrimaryInfoSection.Visibility   = step == 1 ? Visibility.Visible  : Visibility.Collapsed;
            SecondaryInfoSection.Visibility = step == 2 ? Visibility.Visible  : Visibility.Collapsed;
            FamilyMembersSection.Visibility = step == 3 ? Visibility.Visible  : Visibility.Collapsed;

            BtnBack.Visibility = step > 1 ? Visibility.Visible  : Visibility.Collapsed;
            BtnNext.Visibility = step < 3 ? Visibility.Visible  : Visibility.Collapsed;
            BtnSave.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;

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
        }

        #endregion

        #region Validation & Record Saving

        private bool ValidateAll(out List<string> allMissing)
        {
            allMissing = new List<string>();
            bool ok1 = PrimaryInfoSection.Validate(out var m1);
            allMissing.AddRange(m1);

            bool ok2 = SecondaryInfoSection.Validate(out var m2);
            allMissing.AddRange(m2);

            bool ok3 = FamilyMembersSection.Validate(out var m3);
            allMissing.AddRange(m3);

            return ok1 && ok2 && ok3;
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
            if (!ValidateAll(out var missing))
            {
                SetValidationMessage(missing);
                return;
            }

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
            string surname = PrimaryInfoSection.LastName;
            string first   = PrimaryInfoSection.FirstName;
            string middle  = PrimaryInfoSection.MiddleName;

            var members = FamilyMembersSection.BuildFamilyMembers();

            return new SoloParentRecord
            {
                Id            = "SP-" + DateTime.Now.ToString("yyMMddHHmm"),
                Surname       = surname,
                FirstName     = first,
                MiddleName    = middle,
                ExtensionName = PrimaryInfoSection.ExtensionName,
                Name          = surname + ", " + first + (string.IsNullOrEmpty(middle) ? "" : " " + middle),
                DateOfBirth   = PrimaryInfoSection.Birthdate ?? DateTime.MinValue,
                PlaceOfBirth  = SecondaryInfoSection.PlaceOfBirth,
                Sex           = SecondaryInfoSection.Sex,
                CivilStatus   = SecondaryInfoSection.CivilStatus,
                Citizenship   = string.Empty,
                BloodType     = string.Empty,
                Height        = string.Empty,
                Weight        = string.Empty,
                Address       = SecondaryInfoSection.Address,
                ContactNumber = OcrService.FormatPhoneNumber(SecondaryInfoSection.ContactNumber),
                Barangay      = SecondaryInfoSection.Barangay,
                Status        = FamilyMembersSection.ApplicationStatus,
                LastUpdated   = DateTime.Now,

                IsNewApplicant      = true,
                IsRenewal           = false,
                DateOfApplication   = PrimaryInfoSection.ApplicationDate ?? DateTime.Today,

                EducationalAttainment = SecondaryInfoSection.EducationalAttainment,
                PhilSysNumber         = PrimaryInfoSection.PhilSysNumber,
                Religion              = SecondaryInfoSection.Religion,
                Occupation            = SecondaryInfoSection.Occupation,
                MonthlyIncome         = SecondaryInfoSection.MonthlyIncome,
                IsEmployed            = SecondaryInfoSection.IsEmployed,
                IsSelfEmployed        = SecondaryInfoSection.IsSelfEmployed,
                IsNotEmployed         = SecondaryInfoSection.IsNotEmployed,

                EmergencyContactName   = SecondaryInfoSection.EmergencyContactName,
                EmergencyRelationship  = SecondaryInfoSection.EmergencyRelationship,
                EmergencyAddress       = SecondaryInfoSection.EmergencyAddress,
                EmergencyContactNumber = OcrService.FormatPhoneNumber(SecondaryInfoSection.EmergencyContactNumber),

                CircumstanceA1             = FamilyMembersSection.CircumstanceA1,
                CircumstanceA2             = FamilyMembersSection.CircumstanceA2,
                CircumstanceA2Cause        = FamilyMembersSection.CircumstanceA2Cause,
                CircumstanceA2Date         = FamilyMembersSection.CircumstanceA2Date,
                CircumstanceA3             = FamilyMembersSection.CircumstanceA3,
                CircumstanceA4             = FamilyMembersSection.CircumstanceA4,
                CircumstanceA4Disability   = FamilyMembersSection.CircumstanceA4Disability,
                CircumstanceA5             = FamilyMembersSection.CircumstanceA5,
                CircumstanceA5Period       = FamilyMembersSection.CircumstanceA5Period,
                CircumstanceA6             = FamilyMembersSection.CircumstanceA6,
                CircumstanceA6Nullity      = FamilyMembersSection.CircumstanceA6Nullity,
                CircumstanceA6Annulment    = FamilyMembersSection.CircumstanceA6Annulment,
                CircumstanceA7             = FamilyMembersSection.CircumstanceA7,
                CircumstanceB              = FamilyMembersSection.CircumstanceB,
                CircumstanceBStayAbroad    = FamilyMembersSection.CircumstanceBStayAbroad,
                CircumstanceC              = FamilyMembersSection.CircumstanceC,
                CircumstanceD              = FamilyMembersSection.CircumstanceD,
                CircumstanceE              = FamilyMembersSection.CircumstanceE,
                CircumstanceF              = FamilyMembersSection.CircumstanceF,

                FamilyMembers     = members,
                Children          = members.Count,
                NeedsAndProblems  = FamilyMembersSection.NeedsAndProblems,
                OtherIncomeSource = FamilyMembersSection.OtherIncomeSource,

                IsPantawidBeneficiary = SecondaryInfoSection.IsPantawidBeneficiary,
                IsIndigenous          = SecondaryInfoSection.IsIndigenous,
                IsLGBT                = SecondaryInfoSection.IsLGBT
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
            Add(sec, "Age",                    PrimaryInfoSection.Age);
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

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion
    }
}
