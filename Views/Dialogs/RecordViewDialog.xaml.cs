using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SOLUM_UI.Models;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class RecordViewDialog : Window
    {
        private readonly RecordViewDialogViewModel _vm;

        // ── Public result surface — unchanged API for all call-sites ──────────
        public bool OpenedEdit    => _vm.OpenedEdit;
        public bool Renewed       => _vm.Renewed;        // kept for compat
        public bool OpenedRenew   => _vm.OpenedRenew;
        public bool IsQuickRenew  => _vm.IsQuickRenew;
        public bool OpenedDelete  => _vm.OpenedDelete;
        public SoloParentRecord RenewalResult => _vm.RenewalResult;

        public RecordViewDialog(SoloParentRecord record)
        {
            InitializeComponent();

            _vm = new RecordViewDialogViewModel(record);
            DataContext = _vm;

            // When the VM decides the dialog should close it fires RequestClose;
            // the code-behind is the only layer that can touch Window.DialogResult.
            _vm.RequestClose += result => { DialogResult = result; Close(); };

            Loaded += (s, e) => { ApplySize(); BuildContent(); };
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => ApplySize();
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) { }

        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer sv)
            {
                sv.ScrollToVerticalOffset(sv.VerticalOffset - (e.Delta * 0.7));
                e.Handled = true;
            }
        }

        // ── UI-layout logic (permitted in code-behind per AGENT.md §2) ────────
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
            double screenW, screenH, screenLeft, screenTop;
            double workW = SystemParameters.WorkArea.Width;
            double workH = SystemParameters.WorkArea.Height;
            double workLeft = SystemParameters.WorkArea.Left;
            double workTop = SystemParameters.WorkArea.Top;

            if (Owner != null && Owner.WindowState != WindowState.Maximized && Owner.ActualWidth > 0 && Owner.ActualHeight > 0)
            {
                screenW    = Math.Min(Owner.ActualWidth, workW);
                screenH    = Math.Min(Owner.ActualHeight, workH);
                screenLeft = Math.Max(workLeft, Owner.Left);
                screenTop  = Math.Max(workTop, Owner.Top);
            }
            else
            {
                screenW    = workW;
                screenH    = workH;
                screenLeft = workLeft;
                screenTop  = workTop;
            }

            if (Math.Abs(Width - screenW) > 0.5) Width = screenW;
            if (Math.Abs(Height - screenH) > 0.5) Height = screenH;
            if (Math.Abs(Left - screenLeft) > 0.5) Left = screenLeft;
            if (Math.Abs(Top - screenTop) > 0.5) Top = screenTop;

            if (DialogShell != null)
            {
                DialogShell.MaxHeight = Math.Max(500, Math.Min(screenH - 40, 840));
                DialogShell.Width     = Math.Min(Math.Max(screenW * 0.74, 950), 1040);
            }
        }

        // ── Programmatic card-builders ────────────────────────────────────────

        private void BuildContent()
        {
            var r = _vm.Record;
            if (r == null) return;

            TxtRecordName.Text = r.Name ?? string.Empty;

            int age = 0;
            if (r.DateOfBirth != DateTime.MinValue)
            {
                age = DateTime.Today.Year - r.DateOfBirth.Year;
                if (r.DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
            }

            TxtSexAge.Text = (r.Sex ?? "—") + (age > 0 ? $"  ·  {age} years old" : string.Empty);

            string brgy = !string.IsNullOrWhiteSpace(r.Barangay) ? r.Barangay : "Biñan";
            if (!brgy.StartsWith("Brgy", StringComparison.OrdinalIgnoreCase) && !brgy.StartsWith("Barangay", StringComparison.OrdinalIgnoreCase))
                brgy = "Brgy. " + brgy;
            TxtBarangayBadge.Text = brgy;

            string phone = string.IsNullOrWhiteSpace(r.ContactNumber) ? "Not applicable" : SOLUM_UI.Services.OcrService.FormatPhoneNumber(r.ContactNumber);
            TxtContact.Text = phone;

            string status = string.IsNullOrWhiteSpace(r.Status) ? "Active" : r.Status;
            TxtStatusBadge.Text = status;

            // Status styling
            if (string.Equals(status, "Valid", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                StatusBadge.Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xF8, 0xEE));
                StatusBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(0xA5, 0xD6, 0xA7));
                StatusDot.Fill = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
                TxtStatusBadge.Foreground = new SolidColorBrush(Color.FromRgb(0x1B, 0x5E, 0x20));
            }
            else if (string.Equals(status, "For Renewal", StringComparison.OrdinalIgnoreCase))
            {
                StatusBadge.Background = new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xC7));
                StatusBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFC, 0xD3, 0x4D));
                StatusDot.Fill = new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06));
                TxtStatusBadge.Foreground = new SolidColorBrush(Color.FromRgb(0x92, 0x40, 0x0E));
            }
            else
            {
                StatusBadge.Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF4, 0xF6));
                StatusBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB));
                StatusDot.Fill = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
                TxtStatusBadge.Foreground = new SolidColorBrush(Color.FromRgb(0x4B, 0x55, 0x63));
            }

            // Validity styling
            string validUntil = r.CurrentTermExpiresAt.HasValue
                ? r.CurrentTermExpiresAt.Value.ToString("MMM d, yyyy")
                : r.LastUpdated != DateTime.MinValue
                    ? r.LastUpdated.AddYears(1).ToString("MMM d, yyyy")
                    : "Not applicable";

            TxtValidUntil.Text = "Valid until: " + validUntil;
            TxtSpId.Text = !string.IsNullOrWhiteSpace(r.Id) ? $"ID: {r.Id}" : string.Empty;

            CardPanel.Children.Clear();

            // 1. Identity & Personal Info
            AddCard("Identity & Personal Information", new[]
            {
                ("Last Name",      r.Surname),
                ("First Name",     r.FirstName),
                ("Middle Name",    r.MiddleName),
                ("Extension Name", r.ExtensionName),
                ("Sex",            r.Sex),
                ("Civil Status",   r.CivilStatus),
                ("Date of Birth",  r.DateOfBirth != DateTime.MinValue ? r.DateOfBirth.ToString("MMMM d, yyyy") : "Not applicable"),
                ("Age",            age > 0 ? $"{age} years old" : "Not applicable"),
                ("Place of Birth", r.PlaceOfBirth),
                ("Citizenship",    string.IsNullOrWhiteSpace(r.Citizenship) ? "Filipino" : r.Citizenship),
            });

            // 2. Background & Socio-Economic
            AddCard("Background & Socio-Economic", new[]
            {
                ("Educational Attainment", r.EducationalAttainment),
                ("Religion",               r.Religion),
                ("Occupation",             r.Occupation),
                ("Monthly Income",         string.IsNullOrWhiteSpace(r.MonthlyIncome) ? "Not applicable" : "₱ " + r.MonthlyIncome),
                ("Employment Status",      r.EmploymentStatusDisplay),
                ("PhilSys Card No.",       r.PhilSysNumber),
            });

            // 3. Address & Contacts
            AddCard("Address & Contact", new[]
            {
                ("Address",     r.Address),
                ("Barangay",    r.Barangay),
                ("Contact No.", string.IsNullOrWhiteSpace(r.ContactNumber) ? "Not applicable" : SOLUM_UI.Services.OcrService.FormatPhoneNumber(r.ContactNumber)),
            });

            // 4. Emergency Contact
            AddCard("Emergency Contact", new[]
            {
                ("Contact Person", r.EmergencyContactName),
                ("Relationship",   r.EmergencyRelationship),
                ("Address",        r.EmergencyAddress),
                ("Contact No.",    string.IsNullOrWhiteSpace(r.EmergencyContactNumber) ? "Not applicable" : SOLUM_UI.Services.OcrService.FormatPhoneNumber(r.EmergencyContactNumber)),
            });

            // 5. Circumstances
            AddCircumstancesCard(r.CircumstancesDisplay);

            // 6. Additional Classifications (Shows "Not applicable" if none present)
            AddClassificationsCard(r);

            // 7. Family Composition (with non-colliding layout and Special Needs badges)
            AddFamilyCard(r.FamilyMembers);

            // 8. Assessment / Needs & Income
            AddCard("Assessment / Needs & Income", new[]
            {
                ("Needs & Problems",        string.IsNullOrWhiteSpace(r.NeedsAndProblems) || r.NeedsAndProblems.Equals("None specified", StringComparison.OrdinalIgnoreCase) ? "Not applicable" : r.NeedsAndProblems),
                ("Other Sources of Income", string.IsNullOrWhiteSpace(r.OtherIncomeSource) || r.OtherIncomeSource.Equals("None", StringComparison.OrdinalIgnoreCase) ? "Not applicable" : r.OtherIncomeSource),
            });

            // 9. System Information
            AddCard("System Information", new[]
            {
                ("Record ID",            r.Id),
                ("Application Type",     r.ApplicationType),
                ("Date of Application",  r.DateOfApplication != DateTime.MinValue ? r.DateOfApplication.ToString("MMMM d, yyyy") : "Not applicable"),
                ("Current Term Expiry",  validUntil),
                ("Encoded By",           !string.IsNullOrWhiteSpace(r.CreatedBy) ? r.CreatedBy : "Not applicable"),
                ("Last Modified By",     !string.IsNullOrWhiteSpace(r.LastModifiedBy) ? r.LastModifiedBy : "Not applicable"),
                ("Last Updated",         r.LastUpdated != DateTime.MinValue ? r.LastUpdated.ToString("MMMM d, yyyy") : "Not applicable"),
                ("Record Status",        !string.IsNullOrWhiteSpace(r.Status) ? r.Status : "Not applicable"),
            });
        }

        private void AddCard(string title, (string Label, string Value)[] fields)
        {
            var card  = MakeCard();
            var inner = new StackPanel();
            inner.Children.Add(MakeSectionHeader(title));

            var grid = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int row = 0, col = 0;
            foreach (var (label, value) in fields)
            {
                if (col == 0)
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var cell = MakeCell(label, value);
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, col);
                grid.Children.Add(cell);

                col++;
                if (col > 1) { col = 0; row++; }
            }

            inner.Children.Add(grid);
            card.Child = inner;
            CardPanel.Children.Add(card);
        }

        private void AddCircumstancesCard(string display)
        {
            var card  = MakeCard();
            var inner = new StackPanel();
            inner.Children.Add(MakeSectionHeader("Qualifying Circumstances"));

            var wrap = new WrapPanel { Margin = new Thickness(0, 10, 0, 2) };

            if (!string.IsNullOrWhiteSpace(display) && display != "—" && !display.Equals("Not applicable", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var line in display.Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var chip = new Border
                    {
                        Background      = RecordViewDialogViewModel.BrChipBg,
                        BorderBrush     = RecordViewDialogViewModel.BrChipBdr,
                        BorderThickness = new Thickness(1),
                        CornerRadius    = new CornerRadius(8),
                        Padding         = new Thickness(10, 5, 10, 5),
                        Margin          = new Thickness(0, 0, 8, 8)
                    };
                    var sp = new StackPanel { Orientation = Orientation.Horizontal };
                    sp.Children.Add(new TextBlock
                    {
                        Text       = "✓ ",
                        FontSize   = 11,
                        FontWeight = FontWeights.Bold,
                        Foreground = RecordViewDialogViewModel.BrChipTxt,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    sp.Children.Add(new TextBlock
                    {
                        Text       = line.Trim(),
                        FontSize   = 11.5,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = RecordViewDialogViewModel.BrChipTxt,
                        FontFamily = new FontFamily("Segoe UI"),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    chip.Child = sp;
                    wrap.Children.Add(chip);
                }
            }
            else
            {
                wrap.Children.Add(new TextBlock
                {
                    Text       = "Not applicable",
                    FontSize   = 12.5,
                    Foreground = RecordViewDialogViewModel.BrMutedVal,
                    FontFamily = new FontFamily("Segoe UI"),
                    Margin     = new Thickness(2, 4, 0, 4)
                });
            }

            inner.Children.Add(wrap);
            card.Child = inner;
            CardPanel.Children.Add(card);
        }

        private void AddClassificationsCard(SoloParentRecord r)
        {
            var card = MakeCard();
            var inner = new StackPanel();
            inner.Children.Add(MakeSectionHeader("Additional Classifications"));

            bool hasAny = r.IsPantawidBeneficiary || r.IsIndigenous || r.IsLGBT;

            if (!hasAny)
            {
                inner.Children.Add(new TextBlock
                {
                    Text       = "Not applicable",
                    FontSize   = 12.5,
                    Foreground = RecordViewDialogViewModel.BrMutedVal,
                    Margin     = new Thickness(2, 10, 0, 4),
                    FontFamily = new FontFamily("Segoe UI")
                });
            }
            else
            {
                var wrap = new WrapPanel { Margin = new Thickness(0, 10, 0, 2) };

                if (r.IsPantawidBeneficiary)
                    wrap.Children.Add(MakeActiveClassificationBadge("Pantawid (4Ps) Beneficiary", Color.FromRgb(0x27, 0xAE, 0x60)));
                if (r.IsIndigenous)
                    wrap.Children.Add(MakeActiveClassificationBadge("Indigenous Person", Color.FromRgb(0x70, 0x29, 0x43)));
                if (r.IsLGBT)
                    wrap.Children.Add(MakeActiveClassificationBadge("LGBTQ+ Member", Color.FromRgb(0x8E, 0x44, 0xAD)));

                inner.Children.Add(wrap);
            }

            card.Child = inner;
            CardPanel.Children.Add(card);
        }

        private Border MakeActiveClassificationBadge(string title, Color activeColor)
        {
            var bdr = new Border
            {
                CornerRadius    = new CornerRadius(8),
                Padding         = new Thickness(10, 6, 10, 6),
                Margin          = new Thickness(0, 0, 8, 8),
                BorderThickness = new Thickness(1),
                Background      = new SolidColorBrush(Color.FromArgb(20, activeColor.R, activeColor.G, activeColor.B)),
                BorderBrush     = new SolidColorBrush(Color.FromArgb(80, activeColor.R, activeColor.G, activeColor.B))
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new Ellipse
            {
                Width             = 7,
                Height            = 7,
                Fill              = new SolidColorBrush(activeColor),
                Margin            = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            sp.Children.Add(new TextBlock
            {
                Text              = $"✓ {title}",
                FontSize          = 11.5,
                FontWeight        = FontWeights.Bold,
                Foreground        = new SolidColorBrush(activeColor),
                FontFamily        = new FontFamily("Segoe UI"),
                VerticalAlignment = VerticalAlignment.Center
            });
            bdr.Child = sp;
            return bdr;
        }

        private void AddFamilyCard(List<FamilyMember> members)
        {
            var card  = MakeCard();
            var inner = new StackPanel();

            int totalMembers = members?.Count ?? 0;
            int snCount = members?.Count(m => m.HasSpecialNeeds) ?? 0;

            string countText = totalMembers == 1 ? "1 DEPENDENT" : $"{totalMembers} DEPENDENTS";
            if (snCount > 0)
            {
                countText += $" · {snCount} WITH SPECIAL NEEDS (PWD)";
            }

            inner.Children.Add(MakeSectionHeader($"Family Composition ({countText})"));

            if (totalMembers == 0)
            {
                inner.Children.Add(new TextBlock
                {
                    Text       = "Not applicable",
                    FontSize   = 12.5,
                    Foreground = RecordViewDialogViewModel.BrMutedVal,
                    Margin     = new Thickness(2, 10, 0, 4),
                    FontFamily = new FontFamily("Segoe UI")
                });
                card.Child = inner;
                CardPanel.Children.Add(card);
                return;
            }

            // 4-Column non-colliding layout matching RecordDialog
            string[] colLabels = { "NAME & RELATIONSHIP", "BIRTHDATE & AGE", "EDUCATION & INCOME", "SPECIAL NEEDS (PWD)" };
            double[] widths    = { 2.4, 1.8, 2.0, 2.2 };

            var headerGrid = new Grid { Margin = new Thickness(0, 12, 0, 6) };
            for (int c = 0; c < colLabels.Length; c++)
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(widths[c], GridUnitType.Star) });
            headerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            for (int c = 0; c < colLabels.Length; c++)
            {
                var tb = new TextBlock
                {
                    Text       = colLabels[c],
                    FontSize   = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = RecordViewDialogViewModel.BrHead,
                    Margin     = new Thickness(6, 0, 6, 0),
                    FontFamily = new FontFamily("Segoe UI")
                };
                Grid.SetColumn(tb, c);
                headerGrid.Children.Add(tb);
            }
            inner.Children.Add(headerGrid);
            inner.Children.Add(new Border { Height = 1, Background = RecordViewDialogViewModel.BrDivider, Margin = new Thickness(0, 2, 0, 4) });

            bool alt = false;
            foreach (var m in members)
            {
                var rowBorder = new Border
                {
                    Background   = alt ? RecordViewDialogViewModel.BrRowAlt : Brushes.White,
                    BorderBrush  = alt ? RecordViewDialogViewModel.BrDivider : Brushes.Transparent,
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    CornerRadius = new CornerRadius(6),
                    Padding      = new Thickness(6, 9, 6, 9),
                    Margin       = new Thickness(0, 1, 0, 1)
                };
                var rowGrid = new Grid();
                for (int c = 0; c < colLabels.Length; c++)
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(widths[c], GridUnitType.Star) });
                rowGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Column 0: Name & Relationship
                var col0 = new StackPanel { Margin = new Thickness(6, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
                col0.Children.Add(new TextBlock
                {
                    Text         = string.IsNullOrWhiteSpace(m.MemberName) ? "Not applicable" : m.MemberName,
                    FontSize     = 12.5,
                    FontWeight   = FontWeights.SemiBold,
                    Foreground   = RecordViewDialogViewModel.BrValue,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    FontFamily   = new FontFamily("Segoe UI")
                });
                string relInfo = !string.IsNullOrWhiteSpace(m.Relationship) ? m.Relationship : "Dependent";
                if (!string.IsNullOrWhiteSpace(m.CivilStatus) && !m.CivilStatus.Equals("—", StringComparison.OrdinalIgnoreCase))
                    relInfo += $" · {m.CivilStatus}";
                col0.Children.Add(new TextBlock
                {
                    Text         = relInfo,
                    FontSize     = 11,
                    Foreground   = RecordViewDialogViewModel.BrLabel,
                    Margin       = new Thickness(0, 2, 0, 0),
                    FontFamily   = new FontFamily("Segoe UI")
                });
                Grid.SetColumn(col0, 0);
                rowGrid.Children.Add(col0);

                // Column 1: Birthdate & Sex/Age
                var col1 = new StackPanel { Margin = new Thickness(6, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
                col1.Children.Add(new TextBlock
                {
                    Text         = string.IsNullOrWhiteSpace(m.Birthdate) ? "Not applicable" : m.Birthdate,
                    FontSize     = 12,
                    Foreground   = RecordViewDialogViewModel.BrValue,
                    FontFamily   = new FontFamily("Segoe UI")
                });
                string sexAge;
                if (!string.IsNullOrWhiteSpace(m.Sex) && !string.IsNullOrWhiteSpace(m.Age))
                    sexAge = $"{m.Sex} · {m.Age} yrs old";
                else if (!string.IsNullOrWhiteSpace(m.Sex))
                    sexAge = m.Sex;
                else if (!string.IsNullOrWhiteSpace(m.Age))
                    sexAge = $"{m.Age} yrs old";
                else
                    sexAge = "Not applicable";

                col1.Children.Add(new TextBlock
                {
                    Text         = sexAge,
                    FontSize     = 11,
                    Foreground   = RecordViewDialogViewModel.BrLabel,
                    Margin       = new Thickness(0, 2, 0, 0),
                    FontFamily   = new FontFamily("Segoe UI")
                });
                Grid.SetColumn(col1, 1);
                rowGrid.Children.Add(col1);

                // Column 2: Education & Monthly Income
                var col2 = new StackPanel { Margin = new Thickness(6, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
                col2.Children.Add(new TextBlock
                {
                    Text         = string.IsNullOrWhiteSpace(m.EducationEmployment) ? "Not applicable" : m.EducationEmployment,
                    FontSize     = 12,
                    Foreground   = RecordViewDialogViewModel.BrValue,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    FontFamily   = new FontFamily("Segoe UI")
                });
                string incDisplay = "No income";
                if (decimal.TryParse(m.Income, out var incVal))
                    incDisplay = incVal > 0 ? $"₱ {incVal:N2}/mo" : "No income";
                else if (!string.IsNullOrWhiteSpace(m.Income) && m.Income != "0")
                    incDisplay = m.Income;

                col2.Children.Add(new TextBlock
                {
                    Text         = incDisplay,
                    FontSize     = 11,
                    Foreground   = RecordViewDialogViewModel.BrLabel,
                    Margin       = new Thickness(0, 2, 0, 0),
                    FontFamily   = new FontFamily("Segoe UI")
                });
                Grid.SetColumn(col2, 2);
                rowGrid.Children.Add(col2);

                // Column 3: Special Needs / PWD
                UIElement snElement;
                if (m.HasSpecialNeeds)
                {
                    var snPanel = new StackPanel { Margin = new Thickness(6, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
                    var snBadge = new Border
                    {
                        Background      = new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xC7)),
                        BorderBrush     = new SolidColorBrush(Color.FromRgb(0xFC, 0xD3, 0x4D)),
                        BorderThickness = new Thickness(1),
                        CornerRadius    = new CornerRadius(10),
                        Padding         = new Thickness(8, 3, 8, 3),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    var badgeStack = new StackPanel { Orientation = Orientation.Horizontal };
                    badgeStack.Children.Add(new Ellipse
                    {
                        Width             = 6,
                        Height            = 6,
                        Fill              = new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06)),
                        Margin            = new Thickness(0, 0, 5, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    string catText = !string.IsNullOrWhiteSpace(m.SpecialNeedsCategory) ? m.SpecialNeedsCategory : "Special Needs (PWD)";
                    badgeStack.Children.Add(new TextBlock
                    {
                        Text       = catText,
                        FontSize   = 10.5,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x92, 0x40, 0x0E)),
                        FontFamily = new FontFamily("Segoe UI"),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    snBadge.Child = badgeStack;
                    snPanel.Children.Add(snBadge);

                    if (!string.IsNullOrWhiteSpace(m.SpecialNeedsDescription))
                    {
                        snPanel.Children.Add(new TextBlock
                        {
                            Text         = m.SpecialNeedsDescription.Trim(),
                            FontSize     = 10,
                            Foreground   = new SolidColorBrush(Color.FromRgb(0x78, 0x35, 0x0F)),
                            Margin       = new Thickness(2, 3, 0, 0),
                            TextWrapping = TextWrapping.Wrap,
                            FontFamily   = new FontFamily("Segoe UI")
                        });
                    }
                    snElement = snPanel;
                }
                else
                {
                    snElement = new TextBlock
                    {
                        Text              = "Not applicable",
                        FontSize          = 11.5,
                        Foreground        = RecordViewDialogViewModel.BrMutedVal,
                        Margin            = new Thickness(6, 0, 6, 0),
                        FontFamily        = new FontFamily("Segoe UI"),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }

                Grid.SetColumn(snElement, 3);
                rowGrid.Children.Add(snElement);

                rowBorder.Child = rowGrid;
                inner.Children.Add(rowBorder);
                alt = !alt;
            }

            card.Child = inner;
            CardPanel.Children.Add(card);
        }

        private Border MakeCard() => new Border
        {
            Background      = Brushes.White,
            CornerRadius    = new CornerRadius(12),
            BorderBrush     = RecordViewDialogViewModel.BrCardBdr,
            BorderThickness = new Thickness(1.2),
            Padding         = new Thickness(18, 14, 18, 12),
            Margin          = new Thickness(0, 0, 0, 12)
        };

        private UIElement MakeSectionHeader(string title)
        {
            var container = new Border
            {
                Background      = Brushes.Transparent,
                Padding         = new Thickness(0, 0, 0, 8),
                Margin          = new Thickness(0, 0, 0, 4),
                BorderBrush     = RecordViewDialogViewModel.BrDivider,
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(new Border
            {
                Width             = 3.5,
                Height            = 14,
                CornerRadius      = new CornerRadius(2),
                Background        = RecordViewDialogViewModel.BrHead,
                Margin            = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            sp.Children.Add(new TextBlock
            {
                Text              = title.ToUpper(),
                FontSize          = 11.5,
                FontWeight        = FontWeights.Bold,
                Foreground        = RecordViewDialogViewModel.BrHead,
                FontFamily        = new FontFamily("Segoe UI"),
                VerticalAlignment = VerticalAlignment.Center
            });
            container.Child = sp;
            return container;
        }

        private StackPanel MakeCell(string label, string value)
        {
            var sp = new StackPanel { Margin = new Thickness(4, 0, 4, 12) };
            sp.Children.Add(new TextBlock
            {
                Text       = label.ToUpper(),
                FontSize   = 10,
                Foreground = RecordViewDialogViewModel.BrLabel,
                Margin     = new Thickness(0, 0, 0, 3),
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeights.Bold
            });
            bool isBlank = string.IsNullOrWhiteSpace(value) || value == "—" || value.Equals("None", StringComparison.OrdinalIgnoreCase);
            string displayVal = isBlank ? "Not applicable" : value;
            sp.Children.Add(new TextBlock
            {
                Text         = displayVal,
                FontSize     = 13.5,
                FontWeight   = isBlank ? FontWeights.Normal : FontWeights.SemiBold,
                Foreground   = isBlank ? RecordViewDialogViewModel.BrMutedVal : RecordViewDialogViewModel.BrValue,
                TextWrapping = TextWrapping.Wrap,
                FontFamily   = new FontFamily("Segoe UI")
            });
            return sp;
        }

        // ── Event handlers — overlay cancel / confirm stay here; toolbar handlers moved to RecordActionToolbar ──

        private void DeleteCancel_Click(object sender, RoutedEventArgs e)
        {
            // VM sets IsDeleteOverlayVisible = false → binding collapses overlay.
            _vm.HandleDeleteCancel();
        }

        private void DeleteConfirm_Click(object sender, RoutedEventArgs e)
        {
            // VM sets IsDeleteOverlayVisible = false, OpenedDelete = true,
            // then fires RequestClose(true) → constructor lambda closes the Window.
            _vm.HandleDeleteConfirm();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // VM fires RequestClose(false).
            _vm.HandleClose();
        }
    }
}
