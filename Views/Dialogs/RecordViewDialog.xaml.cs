using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        // ── UI-layout logic (permitted in code-behind per AGENT.md §2) ────────
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
            DialogShell.MaxHeight = screenH * 0.90;
            DialogShell.Width     = Math.Min(screenW * 0.62, 880);
        }

        // ── Programmatic card-builders (Phase 4 will replace with UserControls) ─

        private void BuildContent()
        {
            var r = _vm.Record;

            TxtRecordName.Text = r.Name ?? string.Empty;

            int age = 0;
            if (r.DateOfBirth != DateTime.MinValue)
            {
                age = DateTime.Today.Year - r.DateOfBirth.Year;
                if (r.DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
            }
            TxtSexAge.Text      = (r.Sex ?? "—") + (age > 0 ? "  ·  " + age + " years old" : string.Empty);
            TxtContact.Text     = string.IsNullOrWhiteSpace(r.ContactNumber) ? string.Empty : SOLUM_UI.Services.OcrService.FormatPhoneNumber(r.ContactNumber);
            TxtStatusBadge.Text = string.IsNullOrWhiteSpace(r.Status) ? string.Empty : r.Status;
            TxtSpId.Text        = r.Id ?? string.Empty;

            if (r.Status == "Inactive")
            {
                StatusBadge.Background    = RecordViewDialogViewModel.Clr(0xF0, 0xF0, 0xF0);
                TxtStatusBadge.Foreground = RecordViewDialogViewModel.Clr(0x88, 0x88, 0x88);
            }

            CardPanel.Children.Clear();

            string appType = r.IsNewApplicant ? "New Applicant" : r.IsRenewal ? "For Renewal" : "—";

            AddCard("Application", new[]
            {
                ("Type",                appType),
                ("Date of Application", r.DateOfApplication != DateTime.MinValue ? r.DateOfApplication.ToString("MMMM d, yyyy") : "—"),
            });

            AddCard("Identity", new[]
            {
                ("Last Name",      r.Surname),
                ("First Name",     r.FirstName),
                ("Middle Name",    r.MiddleName),
                ("Extension Name", r.ExtensionName),
                ("Sex",            r.Sex),
                ("Civil Status",   r.CivilStatus),
                ("Date of Birth",  r.DateOfBirth != DateTime.MinValue ? r.DateOfBirth.ToString("MMMM d, yyyy") : "—"),
                ("Age",            age > 0 ? age.ToString() : "—"),
                ("Place of Birth", r.PlaceOfBirth),
            });

            AddCard("Background", new[]
            {
                ("Educational Attainment", r.EducationalAttainment),
                ("Religion",               r.Religion),
                ("Occupation",             r.Occupation),
                ("Monthly Income",         string.IsNullOrWhiteSpace(r.MonthlyIncome) ? "—" : "₱ " + r.MonthlyIncome),
                ("Employment Status",      r.EmploymentStatusDisplay),
                ("PhilSys Card No.",       r.PhilSysNumber),
            });

            AddCard("Address & Contact", new[]
            {
                ("Address",     r.Address),
                ("Barangay",    r.Barangay),
                ("Contact No.", SOLUM_UI.Services.OcrService.FormatPhoneNumber(r.ContactNumber)),
            });

            AddCard("Emergency Contact", new[]
            {
                ("Contact Person", r.EmergencyContactName),
                ("Relationship",   r.EmergencyRelationship),
                ("Address",        r.EmergencyAddress),
                ("Contact No.",    SOLUM_UI.Services.OcrService.FormatPhoneNumber(r.EmergencyContactNumber)),
            });

            AddCircumstancesCard(r.CircumstancesDisplay);

            if (r.FamilyMembers != null && r.FamilyMembers.Count > 0)
                AddFamilyCard(r.FamilyMembers);

            if (!string.IsNullOrWhiteSpace(r.NeedsAndProblems) || !string.IsNullOrWhiteSpace(r.OtherIncomeSource))
                AddCard("Notes", new[]
                {
                    ("Needs and Problems",      string.IsNullOrWhiteSpace(r.NeedsAndProblems) ? "—" : r.NeedsAndProblems),
                    ("Other Sources of Income", string.IsNullOrWhiteSpace(r.OtherIncomeSource) ? "—" : r.OtherIncomeSource),
                });

            AddCard("Additional Classifications", new[]
            {
                ("Pantawid Beneficiary", r.IsPantawidBeneficiary ? "Yes" : "No"),
                ("Indigenous Person",    r.IsIndigenous ? "Yes" : "No"),
                ("LGBTQ+",               r.IsLGBT ? "Yes" : "No"),
            });

            AddCard("Record Info", new[]
            {
                ("Last Updated", r.LastUpdated != DateTime.MinValue ? r.LastUpdated.ToString("MMMM d, yyyy") : "—"),
                ("Created By",   r.CreatedBy),
            });
        }

        private void AddCard(string title, (string Label, string Value)[] fields)
        {
            var card  = MakeCard();
            var inner = new StackPanel();
            inner.Children.Add(MakeSectionHeader(title));

            var grid = new Grid { Margin = new Thickness(0, 12, 0, 0) };
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
            inner.Children.Add(MakeSectionHeader("Circumstances"));

            var wrap = new WrapPanel { Margin = new Thickness(0, 10, 0, 2) };

            if (!string.IsNullOrWhiteSpace(display) && display != "—")
            {
                foreach (var line in display.Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    wrap.Children.Add(new Border
                    {
                        Background   = RecordViewDialogViewModel.BrChipBg,
                        CornerRadius = new CornerRadius(6),
                        Padding      = new Thickness(10, 5, 10, 5),
                        Margin       = new Thickness(0, 0, 8, 8),
                        Child        = new TextBlock
                        {
                            Text       = line.Trim(), FontSize = 12,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = RecordViewDialogViewModel.BrChipTxt, FontFamily = new FontFamily("Segoe UI")
                        }
                    });
                }
            }
            else
            {
                wrap.Children.Add(new TextBlock
                {
                    Text = "None selected", FontSize = 13, Foreground = RecordViewDialogViewModel.BrLabel,
                    FontFamily = new FontFamily("Segoe UI")
                });
            }

            inner.Children.Add(wrap);
            card.Child = inner;
            CardPanel.Children.Add(card);
        }

        private void AddFamilyCard(System.Collections.Generic.List<FamilyMember> members)
        {
            var card  = MakeCard();
            var inner = new StackPanel();
            inner.Children.Add(MakeSectionHeader("Family Composition"));

            string[] colLabels = { "Name", "Sex", "Age", "Birthdate", "Civil Status", "Relationship", "Education / Employment", "Income" };
            double[] widths    = { 2.2, 0.5, 0.5, 1.0, 1.0, 1.1, 1.8, 0.8 };

            var headerGrid = new Grid { Margin = new Thickness(0, 12, 0, 4) };
            for (int c = 0; c < colLabels.Length; c++)
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(widths[c], GridUnitType.Star) });
            headerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < colLabels.Length; c++)
            {
                var tb = new TextBlock
                {
                    Text = colLabels[c], FontSize = 10, FontWeight = FontWeights.Bold,
                    Foreground = RecordViewDialogViewModel.BrHead, Margin = new Thickness(4, 0, 4, 0),
                    FontFamily = new FontFamily("Segoe UI")
                };
                Grid.SetColumn(tb, c);
                headerGrid.Children.Add(tb);
            }
            inner.Children.Add(headerGrid);
            inner.Children.Add(new Border { Height = 1, Background = RecordViewDialogViewModel.BrDivider, Margin = new Thickness(0, 4, 0, 0) });

            bool alt = false;
            foreach (var m in members)
            {
                var rowBorder = new Border
                {
                    Background   = alt ? RecordViewDialogViewModel.BrRowAlt : Brushes.White,
                    CornerRadius = new CornerRadius(5),
                    Padding      = new Thickness(0, 6, 0, 6),
                    Margin       = new Thickness(0, 1, 0, 0)
                };
                var rowGrid = new Grid();
                for (int c = 0; c < colLabels.Length; c++)
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(widths[c], GridUnitType.Star) });
                rowGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                string[] vals = { m.MemberName, m.Sex, m.Age, m.Birthdate, m.CivilStatus, m.Relationship, m.EducationEmployment, m.Income };
                for (int c = 0; c < vals.Length; c++)
                {
                    var tb = new TextBlock
                    {
                        Text         = string.IsNullOrWhiteSpace(vals[c]) ? "—" : vals[c],
                        FontSize     = 12, Foreground = RecordViewDialogViewModel.BrValue, TextWrapping = TextWrapping.Wrap,
                        Margin       = new Thickness(4, 0, 4, 0), FontFamily = new FontFamily("Segoe UI")
                    };
                    Grid.SetColumn(tb, c);
                    rowGrid.Children.Add(tb);
                }
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
            CornerRadius    = new CornerRadius(10),
            BorderBrush     = RecordViewDialogViewModel.BrCardBdr,
            BorderThickness = new Thickness(1),
            Padding         = new Thickness(16, 14, 16, 12),
            Margin          = new Thickness(0, 0, 0, 10)
        };

        private UIElement MakeSectionHeader(string title)
        {
            var container = new Border
            {
                Background      = Brushes.White,
                CornerRadius    = new CornerRadius(0),
                Padding         = new Thickness(0, 0, 0, 6),
                Margin          = new Thickness(0, 0, 0, 0),
                BorderBrush     = RecordViewDialogViewModel.BrDivider,
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            container.Child = new TextBlock
            {
                Text = title.ToUpper(), FontSize = 12, FontWeight = FontWeights.Bold,
                Foreground = RecordViewDialogViewModel.BrHead, FontFamily = new FontFamily("Segoe UI"),
                VerticalAlignment = VerticalAlignment.Center
            };
            return container;
        }

        private StackPanel MakeCell(string label, string value)
        {
            var sp = new StackPanel { Margin = new Thickness(4, 0, 4, 14) };
            sp.Children.Add(new TextBlock
            {
                Text = label, FontSize = 10, Foreground = RecordViewDialogViewModel.BrLabel,
                Margin = new Thickness(0, 0, 0, 3), FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeights.SemiBold
            });
            sp.Children.Add(new TextBlock
            {
                Text         = string.IsNullOrWhiteSpace(value) ? "—" : value,
                FontSize     = 14, FontWeight = FontWeights.SemiBold,
                Foreground   = RecordViewDialogViewModel.BrValue, TextWrapping = TextWrapping.Wrap,
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
