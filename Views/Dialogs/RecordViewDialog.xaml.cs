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
            DialogShell.MaxHeight = Math.Min(screenH * 0.92, 880);
            DialogShell.Width     = Math.Min(Math.Max(screenW * 0.74, 950), 1040);
        }

        // ── Programmatic card-builders (Phase 4 will replace with UserControls) ─

                        private void BuildContent()

        {

            CardPanel.Children.Clear();

            var r = _vm.Record;

            if (r == null) return;



            int age = r.DateOfBirth != DateTime.MinValue ? SOLUM_UI.Services.OcrService.CalculateAge(r.DateOfBirth) : 0;

            string formattedContact = SOLUM_UI.Services.OcrService.FormatPhoneNumber(r.ContactNumber);



            // ── Populate Top Profile Summary Banner ──

            string fullName = !string.IsNullOrWhiteSpace(r.FullName) ? r.FullName : (r.Name ?? string.Empty);

            TxtRecordName.Text = fullName;



            // Avatar initials (first letter of first name + first letter of last name, or "SP")

            string initials = "SP";

            string f = r.FirstName?.Trim();

            string l = r.Surname?.Trim();

            if (!string.IsNullOrEmpty(f) && !string.IsNullOrEmpty(l))

                initials = $"{char.ToUpper(f[0])}{char.ToUpper(l[0])}";

            else if (!string.IsNullOrEmpty(fullName))

            {

                var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 2)

                    initials = $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[parts.Length - 1][0])}";

                else if (parts.Length == 1 && parts[0].Length >= 2)

                    initials = parts[0].Substring(0, 2).ToUpper();

            }

            TxtAvatarInitials.Text = initials;



            // Sex & Age

            string sexStr = !string.IsNullOrWhiteSpace(r.Sex) ? r.Sex : "—";

            string ageStr = age > 0 ? $"{age} years old" : string.Empty;

            TxtSexAge.Text = !string.IsNullOrEmpty(ageStr) ? $"{sexStr}  •  {ageStr}" : sexStr;



            // Barangay Badge

            TxtBarangayBadge.Text = !string.IsNullOrWhiteSpace(r.Barangay) ? $"•  Brgy. {r.Barangay}, Biñan" : "•  City of Biñan";



            // Contact

            TxtContact.Text = !string.IsNullOrWhiteSpace(formattedContact) ? $"📞 {formattedContact}" : "📞 No contact number provided";



            // Status Badge

            string status = !string.IsNullOrWhiteSpace(r.Status) ? r.Status : "Active";

            TxtStatusBadge.Text = status;

            if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))

            {

                StatusBadge.Background = RecordViewDialogViewModel.Clr(0xE8, 0xF5, 0xE9);

                StatusBadge.BorderBrush = RecordViewDialogViewModel.Clr(0xA5, 0xD6, 0xA7);

                StatusDot.Fill = RecordViewDialogViewModel.Clr(0x2E, 0x7D, 0x32);

                TxtStatusBadge.Foreground = RecordViewDialogViewModel.Clr(0x1B, 0x5E, 0x20);

            }

            else if (string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase))

            {

                StatusBadge.Background = RecordViewDialogViewModel.Clr(0xF3, 0xF4, 0xF6);

                StatusBadge.BorderBrush = RecordViewDialogViewModel.Clr(0xE5, 0xE7, 0xEB);

                StatusDot.Fill = RecordViewDialogViewModel.Clr(0x9C, 0xA3, 0xAF);

                TxtStatusBadge.Foreground = RecordViewDialogViewModel.Clr(0x4B, 0x55, 0x63);

            }

            else if (string.Equals(status, "For Renewal", StringComparison.OrdinalIgnoreCase))

            {

                StatusBadge.Background = RecordViewDialogViewModel.Clr(0xFE, 0xF6, 0xEB);

                StatusBadge.BorderBrush = RecordViewDialogViewModel.Clr(0xF4, 0xD6, 0xB4);

                StatusDot.Fill = RecordViewDialogViewModel.Clr(0xD9, 0x77, 0x06);

                TxtStatusBadge.Foreground = RecordViewDialogViewModel.Clr(0x8B, 0x4E, 0x12);

            }

            else

            {

                StatusBadge.Background = RecordViewDialogViewModel.Clr(0xFE, 0xF2, 0xF2);

                StatusBadge.BorderBrush = RecordViewDialogViewModel.Clr(0xFE, 0xCA, 0xCA);

                StatusDot.Fill = RecordViewDialogViewModel.Clr(0xDC, 0x26, 0x26);

                TxtStatusBadge.Foreground = RecordViewDialogViewModel.Clr(0x99, 0x1B, 0x1B);

            }



            // Validity Badge

            DateTime? validDate = _vm.ValidUntil ?? r.CurrentTermExpiresAt;
            if (validDate.HasValue && validDate.Value != DateTime.MinValue)

            {

                ValidityBadge.Visibility = Visibility.Visible;

                TxtValidUntil.Text = $"Valid Until: {validDate.Value:MMM d, yyyy}";

            }

            else

            {

                ValidityBadge.Visibility = Visibility.Collapsed;

            }



            // Footer SP ID

            TxtSpId.Text = !string.IsNullOrWhiteSpace(r.Id) ? $"SOLO PARENT ID: {r.Id}" : string.Empty;



            // Two-column responsive layout for executive readability

            var twoColGrid = new Grid { Margin = new Thickness(0, 0, 0, 14) };

            twoColGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            twoColGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });

            twoColGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });



            var leftCol = new StackPanel();

            Grid.SetColumn(leftCol, 0);



            var rightCol = new StackPanel();

            Grid.SetColumn(rightCol, 2);



            twoColGrid.Children.Add(leftCol);

            twoColGrid.Children.Add(rightCol);



            // Left Column: Identity, Residence, Livelihood

            leftCol.Children.Add(CreateCard("Personal & Civil Details", new (string, string, bool)[]

            {

                ("Civil Status",   r.CivilStatus, false),

                ("Sex",            r.Sex, false),

                ("Date of Birth",  r.DateOfBirth != DateTime.MinValue ? r.DateOfBirth.ToString("MMMM d, yyyy") : "—", false),

                ("Age",            age > 0 ? $"{age} years old" : "—", false),

                ("Place of Birth", r.PlaceOfBirth, true),

                ("Religion",       r.Religion, false),

                ("PhilSys Card No.", r.PhilSysNumber, false),

            }));



            leftCol.Children.Add(CreateCard("Residence & Identification", new (string, string, bool)[]

            {

                ("Barangay",         r.Barangay, false),

                ("Contact Number",   formattedContact, false),

                ("Complete Address", r.Address, true),

            }));



            leftCol.Children.Add(CreateCard("Employment & Livelihood", new (string, string, bool)[]

            {

                ("Employment Status",      r.EmploymentStatusDisplay, false),

                ("Monthly Income",         string.IsNullOrWhiteSpace(r.MonthlyIncome) ? "—" : "₱ " + r.MonthlyIncome, false),

                ("Occupation",             r.Occupation, true),

                ("Educational Attainment", r.EducationalAttainment, true),

                ("Other Income Sources",   string.IsNullOrWhiteSpace(r.OtherIncomeSource) ? "—" : r.OtherIncomeSource, true),

            }));



            // Right Column: Circumstances & Classifications, Emergency Contact

            rightCol.Children.Add(CreateCircumstancesAndNeedsCard(r));



            if (!string.IsNullOrWhiteSpace(r.EmergencyContactName) ||

                !string.IsNullOrWhiteSpace(r.EmergencyContactNumber) ||

                !string.IsNullOrWhiteSpace(r.EmergencyAddress))

            {

                rightCol.Children.Add(CreateCard("Emergency Contact", new (string, string, bool)[]

                {

                    ("Contact Person", r.EmergencyContactName, false),

                    ("Relationship",   r.EmergencyRelationship, false),

                    ("Contact No.",    SOLUM_UI.Services.OcrService.FormatPhoneNumber(r.EmergencyContactNumber), false),

                    ("Complete Address", r.EmergencyAddress, true),

                }));

            }



            CardPanel.Children.Add(twoColGrid);



            // Full Width: Family Composition

            if (r.FamilyMembers != null && r.FamilyMembers.Count > 0)

                CardPanel.Children.Add(CreateFamilyCard(r.FamilyMembers));

            else

                CardPanel.Children.Add(CreateEmptyFamilyCard());



            // Footer Metadata

            CardPanel.Children.Add(CreateSystemInfoCard(r));

        }



        private Border CreateCard(string title, (string Label, string Value, bool FullWidth)[] fields)

        {

            var card  = MakeCard();

            var inner = new StackPanel();

            inner.Children.Add(MakeSectionHeader(title));



            var grid = new Grid { Margin = new Thickness(0, 10, 0, 0) };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });



            int row = 0, col = 0;

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });



            foreach (var (label, value, fullWidth) in fields)

            {

                if (fullWidth)

                {

                    if (col != 0)

                    {

                        row++;

                        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                        col = 0;

                    }

                    var cell = MakeCell(label, value);

                    Grid.SetRow(cell, row);

                    Grid.SetColumn(cell, 0);

                    Grid.SetColumnSpan(cell, 2);

                    grid.Children.Add(cell);



                    row++;

                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    col = 0;

                }

                else

                {

                    var cell = MakeCell(label, value);

                    Grid.SetRow(cell, row);

                    Grid.SetColumn(cell, col);

                    grid.Children.Add(cell);



                    col++;

                    if (col > 1)

                    {

                        col = 0;

                        row++;

                        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    }

                }

            }



            inner.Children.Add(grid);

            card.Child = inner;

            return card;

        }



        private Border CreateCircumstancesAndNeedsCard(SoloParentRecord r)

        {

            var card  = MakeCard();

            var inner = new StackPanel();

            inner.Children.Add(MakeSectionHeader("Circumstances & Classifications"));



            string display = r.CircumstancesDisplay;

            bool hasCircumstance = false;



            if (!string.IsNullOrWhiteSpace(display) && display != "—")

            {

                var circSp = new StackPanel { Margin = new Thickness(0, 10, 0, 6) };

                foreach (var rawLine in display.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))

                {

                    string line = rawLine.Trim();

                    if (string.IsNullOrWhiteSpace(line)) continue;

                    hasCircumstance = true;



                    var item = new Border

                    {

                        Background = RecordViewDialogViewModel.BrChipBg,

                        BorderBrush = RecordViewDialogViewModel.BrChipBdr,

                        BorderThickness = new Thickness(1),

                        CornerRadius = new CornerRadius(8),

                        Padding = new Thickness(12, 8, 12, 8),

                        Margin = new Thickness(0, 0, 0, 6)

                    };

                    var itemSp = new StackPanel { Orientation = Orientation.Horizontal };

                    itemSp.Children.Add(new TextBlock

                    {

                        Text = "●",

                        FontSize = 8,

                        Foreground = RecordViewDialogViewModel.BrHead,

                        Margin = new Thickness(0, 3, 8, 0),

                        VerticalAlignment = VerticalAlignment.Top

                    });

                    itemSp.Children.Add(new TextBlock

                    {

                        Text = line,

                        FontSize = 12,

                        FontWeight = FontWeights.SemiBold,

                        Foreground = RecordViewDialogViewModel.BrChipTxt,

                        TextWrapping = TextWrapping.Wrap,

                        FontFamily = new FontFamily("Segoe UI")

                    });

                    item.Child = itemSp;

                    circSp.Children.Add(item);

                }

                inner.Children.Add(circSp);

            }



            // Special demographic tags

            var wrap = new WrapPanel { Margin = new Thickness(0, 4, 0, 4) };

            if (r.IsPantawidBeneficiary)

            {

                hasCircumstance = true;

                wrap.Children.Add(new Border

                {

                    Background      = RecordViewDialogViewModel.Clr(0xFE, 0xF6, 0xEB),

                    BorderBrush     = RecordViewDialogViewModel.Clr(0xF4, 0xD6, 0xB4),

                    BorderThickness = new Thickness(1),

                    CornerRadius    = new CornerRadius(12),

                    Padding         = new Thickness(10, 4, 10, 4),

                    Margin          = new Thickness(0, 0, 6, 6),

                    Child           = new TextBlock

                    {

                        Text       = "4Ps / Pantawid Beneficiary", FontSize = 11.5,

                        FontWeight = FontWeights.SemiBold,

                        Foreground = RecordViewDialogViewModel.Clr(0x8B, 0x4E, 0x12),

                        FontFamily = new FontFamily("Segoe UI")

                    }

                });

            }



            if (r.IsIndigenous)

            {

                hasCircumstance = true;

                wrap.Children.Add(new Border

                {

                    Background      = RecordViewDialogViewModel.Clr(0xFA, 0xF0, 0xF3),

                    BorderBrush     = RecordViewDialogViewModel.Clr(0xDF, 0x9B, 0xB1),

                    BorderThickness = new Thickness(1),

                    CornerRadius    = new CornerRadius(12),

                    Padding         = new Thickness(10, 4, 10, 4),

                    Margin          = new Thickness(0, 0, 6, 6),

                    Child           = new TextBlock

                    {

                        Text       = "Indigenous Peoples (IP)", FontSize = 11.5,

                        FontWeight = FontWeights.SemiBold,

                        Foreground = RecordViewDialogViewModel.Clr(0x70, 0x29, 0x43),

                        FontFamily = new FontFamily("Segoe UI")

                    }

                });

            }



            if (r.IsLGBT)

            {

                hasCircumstance = true;

                wrap.Children.Add(new Border

                {

                    Background      = RecordViewDialogViewModel.Clr(0xF4, 0xF1, 0xF6),

                    BorderBrush     = RecordViewDialogViewModel.Clr(0xD5, 0xCE, 0xDE),

                    BorderThickness = new Thickness(1),

                    CornerRadius    = new CornerRadius(12),

                    Padding         = new Thickness(10, 4, 10, 4),

                    Margin          = new Thickness(0, 0, 6, 6),

                    Child           = new TextBlock

                    {

                        Text       = "LGBTQ+", FontSize = 11.5,

                        FontWeight = FontWeights.SemiBold,

                        Foreground = RecordViewDialogViewModel.Clr(0x56, 0x3E, 0x6E),

                        FontFamily = new FontFamily("Segoe UI")

                    }

                });

            }



            if (!hasCircumstance)

            {

                wrap.Children.Add(new TextBlock

                {

                    Text = "No specific circumstances or demographic classifications indicated.",

                    FontSize = 12, Foreground = RecordViewDialogViewModel.BrLabel,

                    FontFamily = new FontFamily("Segoe UI"), FontStyle = FontStyles.Italic

                });

            }



            inner.Children.Add(wrap);



            // Identified Needs & Problems (clean narrative callout if present)

            if (!string.IsNullOrWhiteSpace(r.NeedsAndProblems) &&

                !string.Equals(r.NeedsAndProblems.Trim(), "None specified", StringComparison.OrdinalIgnoreCase) &&

                !string.Equals(r.NeedsAndProblems.Trim(), "None", StringComparison.OrdinalIgnoreCase))

            {

                var needsBox = new Border

                {

                    Background      = RecordViewDialogViewModel.Clr(0xFA, 0xF8, 0xF9),

                    BorderBrush     = RecordViewDialogViewModel.BrDivider,

                    BorderThickness = new Thickness(1),

                    CornerRadius    = new CornerRadius(8),

                    Padding         = new Thickness(12, 10, 12, 10),

                    Margin          = new Thickness(0, 10, 0, 2)

                };

                var needsSp = new StackPanel();

                needsSp.Children.Add(new TextBlock

                {

                    Text = "IDENTIFIED NEEDS & PROBLEMS",

                    FontSize = 10, FontWeight = FontWeights.Bold,

                    Foreground = RecordViewDialogViewModel.BrLabel,

                    Margin = new Thickness(0, 0, 0, 4),

                    FontFamily = new FontFamily("Segoe UI")

                });

                needsSp.Children.Add(new TextBlock

                {

                    Text = r.NeedsAndProblems.Trim(),

                    FontSize = 12.5, Foreground = RecordViewDialogViewModel.BrValue,

                    TextWrapping = TextWrapping.Wrap,

                    FontFamily = new FontFamily("Segoe UI")

                });

                needsBox.Child = needsSp;

                inner.Children.Add(needsBox);

            }



            card.Child = inner;

            return card;

        }



        private Border CreateEmptyFamilyCard()

        {

            var card  = MakeCard();

            var inner = new StackPanel();

            inner.Children.Add(MakeSectionHeader("Family Composition"));



            var emptyTb = new TextBlock

            {

                Text = "No dependent family members or children registered for this record.",

                FontSize = 12,

                Foreground = RecordViewDialogViewModel.BrLabel,

                FontStyle = FontStyles.Italic,

                FontFamily = new FontFamily("Segoe UI"),

                Margin = new Thickness(4, 10, 4, 4)

            };

            inner.Children.Add(emptyTb);



            card.Child = inner;

            return card;

        }



                private Border CreateFamilyCard(System.Collections.Generic.List<FamilyMember> members)

        {

            var card  = MakeCard();

            var inner = new StackPanel();

            inner.Children.Add(MakeSectionHeader($"Family Composition ({members.Count} {(members.Count == 1 ? "Dependent" : "Dependents")})"));



            // 5 Data columns + 4 Vertical Column Lines = 9 Grid Columns

            // Col 0: Name & Rel (2.2*)

            // Col 1: Divider (1px)

            // Col 2: Demographics (1.3*)

            // Col 3: Divider (1px)

            // Col 4: Education & Employment (2.4*) -> generous width so text never cuts

            // Col 5: Divider (1px)

            // Col 6: Monthly Income (1.0*)

            // Col 7: Divider (1px)

            // Col 8: Special Needs / PWD (1.9*)



            var tableContainer = new Border

            {

                Background      = Brushes.White,

                BorderBrush     = RecordViewDialogViewModel.Clr(0xD8, 0xC8, 0xD0),

                BorderThickness = new Thickness(1.2),

                CornerRadius    = new CornerRadius(12),

                ClipToBounds    = true,

                Margin          = new Thickness(0, 10, 0, 4)

            };



            var tableStack = new StackPanel();



            // ── Header Banner (with matching top rounded corners) ──

            var headerBorder = new Border

            {

                Background   = RecordViewDialogViewModel.Clr(0x70, 0x29, 0x43),

                CornerRadius = new CornerRadius(11, 11, 0, 0),

                Padding      = new Thickness(0, 10, 0, 10)

            };



            var headerGrid = new Grid();

            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.2, GridUnitType.Star), MinWidth = 150 });

            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });

            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star), MinWidth = 110 });

            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });

            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.4, GridUnitType.Star), MinWidth = 160 });

            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });

            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star), MinWidth = 85 });

            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });

            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.9, GridUnitType.Star), MinWidth = 140 });



            string[] colLabels = { "NAME & RELATIONSHIP", "DEMOGRAPHICS", "EDUCATION & EMPLOYMENT", "MONTHLY INCOME", "SPECIAL NEEDS (PWD)" };

            int[] dataCols = { 0, 2, 4, 6, 8 };



            for (int i = 0; i < colLabels.Length; i++)

            {

                var tb = new TextBlock

                {

                    Text              = colLabels[i],

                    FontSize          = 10,

                    FontWeight        = FontWeights.Bold,

                    Foreground        = Brushes.White,

                    TextWrapping      = TextWrapping.Wrap,

                    Margin            = new Thickness(10, 0, 10, 0),

                    FontFamily        = new FontFamily("Segoe UI"),

                    VerticalAlignment = VerticalAlignment.Center

                };

                if (i == 3) tb.HorizontalAlignment = HorizontalAlignment.Right;

                Grid.SetColumn(tb, dataCols[i]);

                headerGrid.Children.Add(tb);



                // Add header vertical divider line if not last column

                if (i < colLabels.Length - 1)

                {

                    var div = new Border

                    {

                        Width             = 1,

                        Background        = new SolidColorBrush(Color.FromArgb(0x44, 0xFF, 0xFF, 0xFF)),

                        VerticalAlignment = VerticalAlignment.Stretch

                    };

                    Grid.SetColumn(div, dataCols[i] + 1);

                    headerGrid.Children.Add(div);

                }

            }



            headerBorder.Child = headerGrid;

            tableStack.Children.Add(headerBorder);



            // ── Data Rows ──

            bool alt = false;

            for (int mIdx = 0; mIdx < members.Count; mIdx++)

            {

                var m = members[mIdx];

                bool isLast = (mIdx == members.Count - 1);



                var rowBorder = new Border

                {

                    Background      = alt ? RecordViewDialogViewModel.Clr(0xFD, 0xFB, 0xFC) : Brushes.White,

                    BorderBrush     = RecordViewDialogViewModel.Clr(0xEE, 0xE4, 0xE8),

                    BorderThickness = new Thickness(0, 0, 0, isLast ? 0 : 1),

                    CornerRadius    = isLast ? new CornerRadius(0, 0, 11, 11) : new CornerRadius(0),

                    Padding         = new Thickness(0, 9, 0, 9)

                };



                var rowGrid = new Grid();

                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.2, GridUnitType.Star), MinWidth = 150 });

                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });

                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star), MinWidth = 110 });

                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });

                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.4, GridUnitType.Star), MinWidth = 160 });

                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });

                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star), MinWidth = 85 });

                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });

                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.9, GridUnitType.Star), MinWidth = 140 });



                // Add 1px vertical column divider lines

                int[] divCols = { 1, 3, 5, 7 };

                foreach (var dc in divCols)

                {

                    var colDiv = new Border

                    {

                        Width             = 1,

                        Background        = RecordViewDialogViewModel.Clr(0xEA, 0xDE, 0xE3),

                        VerticalAlignment = VerticalAlignment.Stretch

                    };

                    Grid.SetColumn(colDiv, dc);

                    rowGrid.Children.Add(colDiv);

                }



                // ── Col 0: Name & Relationship ──

                var col0 = new StackPanel { Margin = new Thickness(10, 2, 10, 2), VerticalAlignment = VerticalAlignment.Center };

                var nameRow = new StackPanel { Orientation = Orientation.Horizontal };

                nameRow.Children.Add(new TextBlock

                {

                    Text         = string.IsNullOrWhiteSpace(m.MemberName) ? "—" : m.MemberName,

                    FontSize     = 13,

                    FontWeight   = FontWeights.Bold,

                    Foreground   = RecordViewDialogViewModel.BrValue,

                    TextWrapping = TextWrapping.Wrap,

                    FontFamily   = new FontFamily("Segoe UI")

                });

                if (m.HasSpecialNeeds)

                {

                    var pwdBadge = new Border

                    {

                        Background        = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43)),

                        CornerRadius      = new CornerRadius(3),

                        Padding           = new Thickness(5, 1, 5, 1),

                        Margin            = new Thickness(6, 0, 0, 0),

                        VerticalAlignment = VerticalAlignment.Center,

                        ToolTip           = !string.IsNullOrWhiteSpace(m.SpecialNeedsCategory) ? $"Person with Special Needs: {m.SpecialNeedsCategory}" : "Person with Special Needs (PWD)"

                    };

                    pwdBadge.Child = new TextBlock

                    {

                        Text       = "PWD",

                        FontSize   = 9,

                        FontWeight = FontWeights.Bold,

                        Foreground = Brushes.White,

                        FontFamily = new FontFamily("Segoe UI")

                    };

                    nameRow.Children.Add(pwdBadge);

                }

                col0.Children.Add(nameRow);



                string relText = !string.IsNullOrWhiteSpace(m.Relationship) ? m.Relationship : "Dependent";

                string civilText = !string.IsNullOrWhiteSpace(m.CivilStatus) ? $" • {m.CivilStatus}" : "";

                col0.Children.Add(new TextBlock

                {

                    Text       = $"{relText}{civilText}",

                    FontSize   = 11.5,

                    Foreground = RecordViewDialogViewModel.BrLabel,

                    Margin     = new Thickness(0, 2, 0, 0),

                    FontFamily = new FontFamily("Segoe UI")

                });

                Grid.SetColumn(col0, 0);

                rowGrid.Children.Add(col0);



                // ── Col 2: Demographics (Age, Sex, Birthdate) ──

                var col2 = new StackPanel { Margin = new Thickness(10, 2, 10, 2), VerticalAlignment = VerticalAlignment.Center };

                string ageText = !string.IsNullOrWhiteSpace(m.Age) ? $"{m.Age} yrs old" : "—";

                string sexText = !string.IsNullOrWhiteSpace(m.Sex) ? $" • {m.Sex}" : "";

                col2.Children.Add(new TextBlock

                {

                    Text       = $"{ageText}{sexText}",

                    FontSize   = 12.5,

                    FontWeight = FontWeights.SemiBold,

                    Foreground = RecordViewDialogViewModel.BrValue,

                    FontFamily = new FontFamily("Segoe UI")

                });

                string dobText = !string.IsNullOrWhiteSpace(m.Birthdate) ? $"Born: {m.Birthdate}" : "—";

                col2.Children.Add(new TextBlock

                {

                    Text       = dobText,

                    FontSize   = 11,

                    Foreground = RecordViewDialogViewModel.BrLabel,

                    Margin     = new Thickness(0, 2, 0, 0),

                    FontFamily = new FontFamily("Segoe UI")

                });

                Grid.SetColumn(col2, 2);

                rowGrid.Children.Add(col2);



                // ── Col 4: Education & Employment (Word-wrapped and generous width) ──

                var col4 = new StackPanel { Margin = new Thickness(10, 2, 10, 2), VerticalAlignment = VerticalAlignment.Center };

                string eduText = !string.IsNullOrWhiteSpace(m.EducationEmployment) ? m.EducationEmployment : "Not specified";

                col4.Children.Add(new TextBlock

                {

                    Text         = eduText,

                    FontSize     = 12,

                    FontWeight   = FontWeights.Medium,

                    Foreground   = RecordViewDialogViewModel.BrValue,

                    TextWrapping = TextWrapping.Wrap,

                    FontFamily   = new FontFamily("Segoe UI")

                });

                Grid.SetColumn(col4, 4);

                rowGrid.Children.Add(col4);



                // ── Col 6: Monthly Income ──

                var col6 = new StackPanel { Margin = new Thickness(10, 2, 10, 2), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };

                string incText = string.IsNullOrWhiteSpace(m.Income) || m.Income == "—" ? "—" : (m.Income.StartsWith("₱") ? m.Income : "₱ " + m.Income);

                col6.Children.Add(new TextBlock

                {

                    Text                = incText,

                    FontSize            = 12.5,

                    FontWeight          = FontWeights.SemiBold,

                    Foreground          = incText == "—" ? RecordViewDialogViewModel.BrMutedVal : RecordViewDialogViewModel.BrValue,

                    HorizontalAlignment = HorizontalAlignment.Right,

                    FontFamily          = new FontFamily("Segoe UI")

                });

                Grid.SetColumn(col6, 6);

                rowGrid.Children.Add(col6);



                // ── Col 8: Special Needs / Disability ──

                var col8 = new StackPanel { Margin = new Thickness(10, 2, 10, 2), VerticalAlignment = VerticalAlignment.Center };

                if (m.HasSpecialNeeds)

                {

                    var badge = new Border

                    {

                        Background          = new SolidColorBrush(Color.FromRgb(0xFA, 0xF0, 0xF3)),

                        BorderBrush         = new SolidColorBrush(Color.FromRgb(0xEA, 0xD3, 0xDC)),

                        BorderThickness     = new Thickness(1),

                        CornerRadius        = new CornerRadius(4),

                        Padding             = new Thickness(7, 2.5, 7, 2.5),

                        HorizontalAlignment = HorizontalAlignment.Left

                    };

                    string catText = !string.IsNullOrWhiteSpace(m.SpecialNeedsCategory) ? m.SpecialNeedsCategory : "Special Needs (PWD)";

                    badge.Child = new TextBlock

                    {

                        Text         = catText,

                        FontSize     = 11,

                        FontWeight   = FontWeights.SemiBold,

                        Foreground   = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43)),

                        TextWrapping = TextWrapping.Wrap,

                        FontFamily   = new FontFamily("Segoe UI")

                    };

                    col8.Children.Add(badge);



                    if (!string.IsNullOrWhiteSpace(m.SpecialNeedsDescription))

                    {

                        col8.Children.Add(new TextBlock

                        {

                            Text         = m.SpecialNeedsDescription,

                            FontSize     = 10.5,

                            Foreground   = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),

                            TextWrapping = TextWrapping.Wrap,

                            Margin       = new Thickness(2, 3, 0, 0),

                            FontFamily   = new FontFamily("Segoe UI")

                        });

                    }

                }

                else

                {

                    col8.Children.Add(new TextBlock

                    {

                        Text       = "None",

                        FontSize   = 12,

                        Foreground = RecordViewDialogViewModel.BrMutedVal,

                        FontFamily = new FontFamily("Segoe UI")

                    });

                }

                Grid.SetColumn(col8, 8);

                rowGrid.Children.Add(col8);



                rowBorder.Child = rowGrid;

                tableStack.Children.Add(rowBorder);

                alt = !alt;

            }



            tableContainer.Child = tableStack;

            inner.Children.Add(tableContainer);



            card.Child = inner;

            return card;

        }



private Border CreateSystemInfoCard(SoloParentRecord r)
        {
            var card = new Border
            {
                Background = Brushes.Transparent,
                Padding    = new Thickness(4, 6, 4, 12),
                Margin     = new Thickness(0, 4, 0, 0)
            };

            var sp = new StackPanel
            {
                Orientation         = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            string updated = r.LastUpdated != DateTime.MinValue ? r.LastUpdated.ToString("MMMM d, yyyy") : "—";
            string createdBy = string.IsNullOrWhiteSpace(r.CreatedBy) ? "System" : r.CreatedBy;

            sp.Children.Add(new TextBlock
            {
                Text       = $"Last Modified: {updated}   •   Encoded By: {createdBy}",
                FontSize   = 11,
                Foreground = RecordViewDialogViewModel.BrLabel,
                FontFamily = new FontFamily("Segoe UI")
            });

            card.Child = sp;
            return card;
        }

        private Border MakeCard() => new Border
        {
            Background      = Brushes.White,
            CornerRadius    = new CornerRadius(10),
            BorderBrush     = RecordViewDialogViewModel.BrCardBdr,
            BorderThickness = new Thickness(1),
            Padding         = new Thickness(18, 14, 18, 14),
            Margin          = new Thickness(0, 0, 0, 12)
        };

        private UIElement MakeSectionHeader(string title)
        {
            var sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin      = new Thickness(0, 0, 0, 10)
            };

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

            return sp;
        }

        private StackPanel MakeCell(string label, string value)
        {
            var sp = new StackPanel { Margin = new Thickness(4, 0, 8, 12) };
            sp.Children.Add(new TextBlock
            {
                Text       = label.ToUpper(),
                FontSize   = 10,
                Foreground = RecordViewDialogViewModel.BrLabel,
                Margin     = new Thickness(0, 0, 0, 3),
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeights.SemiBold
            });

            bool isDash = string.IsNullOrWhiteSpace(value) || value == "—";
            sp.Children.Add(new TextBlock
            {
                Text         = isDash ? "—" : value,
                FontSize     = 13,
                FontWeight   = FontWeights.SemiBold,
                Foreground   = isDash ? RecordViewDialogViewModel.BrMutedVal : RecordViewDialogViewModel.BrValue,
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
