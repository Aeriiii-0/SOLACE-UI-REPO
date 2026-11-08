using System.Threading.Tasks;
using SOLUM_UI.Models.Api;
using SOLUM_UI.Services.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Text;
using SOLUM_UI.Models;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class RecordViewDialog : Window
    {
        private readonly RecordViewDialogViewModel _vm;

        private static readonly SolidColorBrush Maroon = Hex("#702943");
        private static readonly SolidColorBrush BandBg = Hex("#F8ECF0");
        private static readonly SolidColorBrush RowLine = Hex("#EDE6EA");

        public bool OpenedEdit => _vm.OpenedEdit;

        public bool Renewed => _vm.Renewed;

        public bool OpenedRenew => _vm.OpenedRenew;

        public bool IsQuickRenew => _vm.IsQuickRenew;

        public bool OpenedDelete => _vm.OpenedDelete;

        public SoloParentRecord RenewalResult => _vm.RenewalResult;

        public RecordViewDialog(SoloParentRecord record)
        {
            InitializeComponent();
            _vm = new RecordViewDialogViewModel(record);
            DataContext = _vm;
            _vm.RequestClose += result => { DialogResult = result; Close(); };
            Loaded += (s, e) => { ApplySize(); BuildContent(); };
        }

        private static SolidColorBrush Hex(string hex)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => ApplySize();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) { }

        private void ActionsPopup_Opened(object sender, EventArgs e)
        {
            ActionsPopup.HorizontalOffset = BtnActions.ActualWidth - 180;
        }

        private void MenuRenew_Click(object sender, RoutedEventArgs e) => RunToolbarAction("renew");

        private void MenuEdit_Click(object sender, RoutedEventArgs e) => RunToolbarAction("edit", "update");

        private void MenuDelete_Click(object sender, RoutedEventArgs e) => RunToolbarAction("delete", "trash", "remove");

        private void RunToolbarAction(params string[] keys)
        {
            BtnActions.IsChecked = false;
            Dispatcher.BeginInvoke(new Action(() => InvokeToolbarButton(keys)), DispatcherPriority.Background);
        }

        private void InvokeToolbarButton(string[] keys)
        {
            HiddenToolbar.ApplyTemplate();
            HiddenToolbar.UpdateLayout();
            foreach (var b in FindChildren<System.Windows.Controls.Primitives.ButtonBase>(HiddenToolbar))
            {
                string text = Describe(b).ToLowerInvariant();
                if (!keys.Any(k => text.Contains(k))) continue;
                try
                {
                    var onClick = typeof(System.Windows.Controls.Primitives.ButtonBase).GetMethod(
                        "OnClick",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    onClick?.Invoke(b, null);
                }
                catch { }
                return;
            }
        }

        private static string Describe(DependencyObject d)
        {
            var sb = new StringBuilder();
            if (d is FrameworkElement fe)
            {
                sb.Append(fe.Name).Append(' ');
                sb.Append(fe.ToolTip?.ToString()).Append(' ');
            }
            sb.Append(System.Windows.Automation.AutomationProperties.GetName(d)).Append(' ');
            if (d is ContentControl cc && cc.Content is string str) sb.Append(str).Append(' ');
            foreach (var tb in FindChildren<TextBlock>(d)) sb.Append(tb.Text).Append(' ');
            return sb.ToString();
        }

        private static IEnumerable<T> FindChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) yield break;
            int n = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < n; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T t) yield return t;
                foreach (var sub in FindChildren<T>(child)) yield return sub;
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer sv)
            {
                sv.ScrollToVerticalOffset(sv.VerticalOffset - (e.Delta * 0.7));
                e.Handled = true;
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
                screenW = Math.Min(Owner.ActualWidth, workW);
                screenH = Math.Min(Owner.ActualHeight, workH);
                screenLeft = Math.Max(workLeft, Owner.Left);
                screenTop = Math.Max(workTop, Owner.Top);
            }
            else
            {
                screenW = workW;
                screenH = workH;
                screenLeft = workLeft;
                screenTop = workTop;
            }
            if (Math.Abs(Width - screenW) > 0.5) Width = screenW;
            if (Math.Abs(Height - screenH) > 0.5) Height = screenH;
            if (Math.Abs(Left - screenLeft) > 0.5) Left = screenLeft;
            if (Math.Abs(Top - screenTop) > 0.5) Top = screenTop;
            if (DialogShell != null)
            {
                DialogShell.MaxHeight = Math.Floor(Math.Max(500, Math.Min(screenH - 40, 840)));
                DialogShell.Width = Math.Round(Math.Min(Math.Max(screenW * 0.74, 950), 1040) - 70);
            }
        }

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

            SolidColorBrush statusText;
            SolidColorBrush statusFill;
            SolidColorBrush statusTrack;
            if (string.Equals(status, "Valid", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                statusText = Hex("#16794C");
                statusFill = Hex("#0F6B43");
                statusTrack = Hex("#D8F0E3");
            }
            else if (string.Equals(status, "For Renewal", StringComparison.OrdinalIgnoreCase))
            {
                statusText = Hex("#B45309");
                statusFill = Hex("#D97706");
                statusTrack = Hex("#FDE9C2");
            }
            else
            {
                statusText = Hex("#4B5563");
                statusFill = Hex("#6B7280");
                statusTrack = Hex("#E5E7EB");
            }
            TxtStatusBadge.Foreground = statusText;
            ProgressFill.Background = statusFill;
            ProgressTrack.Background = statusTrack;

            string validUntil = r.CurrentTermExpiresAt.HasValue
                ? r.CurrentTermExpiresAt.Value.ToString("MMM d, yyyy")
                : r.LastUpdated != DateTime.MinValue
                    ? r.LastUpdated.AddYears(1).ToString("MMM d, yyyy")
                    : "Not applicable";
            TxtValidUntil.Text = validUntil;

            DateTime? expDate = null;
            if (r.CurrentTermExpiresAt.HasValue)
                expDate = r.CurrentTermExpiresAt.Value.Date;
            else if (r.LastUpdated != DateTime.MinValue)
                expDate = r.LastUpdated.AddYears(1).Date;

            double fraction = 0;
            if (expDate.HasValue)
            {
                int daysLeft = (expDate.Value - DateTime.Today).Days;
                if (daysLeft >= 0)
                    TxtDaysLeft.Text = daysLeft == 1 ? "1 day left" : $"{daysLeft} days left";
                else
                    TxtDaysLeft.Text = Math.Abs(daysLeft) == 1 ? "Expired 1 day ago" : $"Expired {Math.Abs(daysLeft)} days ago";
                fraction = Math.Max(0, Math.Min(1, daysLeft / 365.0));
            }
            else
            {
                TxtDaysLeft.Text = "—";
            }
            ProgressFillCol.Width = new GridLength(fraction, GridUnitType.Star);
            ProgressRestCol.Width = new GridLength(1 - fraction, GridUnitType.Star);

            TxtSpId.Text = string.Empty;
            TxtSpId.Visibility = Visibility.Collapsed;
            CardPanel.Children.Clear();

            AddCard("Identity & Personal Information", new[]
            {
                ("Last name",      r.Surname),
                ("First name",     r.FirstName),
                ("Middle name",    r.MiddleName),
                ("Extension name", r.ExtensionName),
                ("Sex",            r.Sex),
                ("Civil status",   r.CivilStatus),
                ("Date of birth",  r.DateOfBirth != DateTime.MinValue ? r.DateOfBirth.ToString("MMMM d, yyyy") : "Not applicable"),
                ("Age",            age > 0 ? $"{age} years old" : "Not applicable"),
                ("Place of birth", r.PlaceOfBirth),
                ("Citizenship",    string.IsNullOrWhiteSpace(r.Citizenship) ? "Filipino" : r.Citizenship),
            });

            AddCard("Background & Socio-Economic", new[]
            {
                ("Educational attainment", r.EducationalAttainment),
                ("Religion",               r.Religion),
                ("Occupation",             r.Occupation),
                ("Monthly income",         string.IsNullOrWhiteSpace(r.MonthlyIncome) ? "Not applicable" : "₱ " + r.MonthlyIncome),
                ("Employment status",      r.EmploymentStatusDisplay),
                ("PhilSys card no.",       r.PhilSysNumber),
            });

            AddCard("Address & Contact", new[]
            {
                ("Address",     r.Address),
                ("Barangay",    r.Barangay),
                ("Contact no.", string.IsNullOrWhiteSpace(r.ContactNumber) ? "Not applicable" : SOLUM_UI.Services.OcrService.FormatPhoneNumber(r.ContactNumber)),
            });

            AddCard("Emergency Contact", new[]
            {
                ("Contact person", r.EmergencyContactName),
                ("Relationship",   r.EmergencyRelationship),
                ("Address",        r.EmergencyAddress),
                ("Contact no.",    string.IsNullOrWhiteSpace(r.EmergencyContactNumber) ? "Not applicable" : SOLUM_UI.Services.OcrService.FormatPhoneNumber(r.EmergencyContactNumber)),
            });

            AddCircumstancesCard(r.CircumstancesDisplay);

            AddClassificationsCard(r);

            AddFamilyCard(r.FamilyMembers);

            AddCard("Assessment / Needs & Income", new[]
            {
                ("Needs & problems",        string.IsNullOrWhiteSpace(r.NeedsAndProblems) || r.NeedsAndProblems.Equals("None specified", StringComparison.OrdinalIgnoreCase) ? "Not applicable" : r.NeedsAndProblems),
                ("Other sources of income", string.IsNullOrWhiteSpace(r.OtherIncomeSource) || r.OtherIncomeSource.Equals("None", StringComparison.OrdinalIgnoreCase) ? "Not applicable" : r.OtherIncomeSource),
            });

            AddCard("System Information", new[]
            {
                ("Application type",     r.ApplicationType),
                ("Application date",     r.DateOfApplication != DateTime.MinValue ? r.DateOfApplication.ToString("yyyy-MM-dd") : "-"),
                ("Date encoded",         r.DateCreated != DateTime.MinValue ? r.DateCreated.ToString("yyyy-MM-dd") : (r.LastUpdated != DateTime.MinValue ? r.LastUpdated.ToString("yyyy-MM-dd") : "-")),
                ("Current term expiry",  validUntil),
                ("Encoded by",           !string.IsNullOrWhiteSpace(r.CreatedBy) ? r.CreatedBy : "Not applicable"),
                ("Last modified by",     !string.IsNullOrWhiteSpace(r.LastModifiedBy) ? r.LastModifiedBy : "Not applicable"),
                ("Last updated",         r.LastUpdated != DateTime.MinValue ? r.LastUpdated.ToString("MMMM d, yyyy") : "Not applicable"),
                ("Record status",        !string.IsNullOrWhiteSpace(r.Status) ? r.Status : "Not applicable"),
            });
        }

        private Border BuildCard(string title, out StackPanel body)
        {
            var outer = new Border
            {
                Background = Brushes.White,
                BorderBrush = Maroon,
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(14),
                Margin = new Thickness(0, 0, 0, 18)
            };
            var root = new StackPanel();
            var header = new Border
            {
                Background = BandBg,
                BorderBrush = Maroon,
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(13, 13, 0, 0),
                Padding = new Thickness(16, 10, 16, 10)
            };
            header.Child = new TextBlock
            {
                Text = title.ToUpper(),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Maroon,
                FontFamily = new FontFamily("Segoe UI")
            };
            body = new StackPanel();
            root.Children.Add(header);
            root.Children.Add(body);
            outer.Child = root;
            return outer;
        }

        private void AddCard(string title, (string Label, string Value)[] fields)
        {
            var card = BuildCard(title, out var body);
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int rowCount = (fields.Length + 1) / 2;
            int row = 0, col = 0;
            foreach (var (label, value) in fields)
            {
                if (col == 0)
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var cell = MakeCell(label, value, row < rowCount - 1);
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, col);
                grid.Children.Add(cell);
                col++;
                if (col > 1) { col = 0; row++; }
            }
            body.Children.Add(grid);
            CardPanel.Children.Add(card);
        }

        private void AddCircumstancesCard(string display)
        {
            var card = BuildCard("Qualifying Circumstances", out var body);
            var wrap = new WrapPanel { Margin = new Thickness(16, 12, 16, 6) };
            if (!string.IsNullOrWhiteSpace(display) && display != "—" && !display.Equals("Not applicable", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var line in display.Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var chip = new Border
                    {
                        Background = RecordViewDialogViewModel.BrChipBg,
                        BorderBrush = RecordViewDialogViewModel.BrChipBdr,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(10, 5, 10, 5),
                        Margin = new Thickness(0, 0, 8, 8)
                    };
                    var sp = new StackPanel { Orientation = Orientation.Horizontal };
                    sp.Children.Add(new TextBlock
                    {
                        Text = "✓ ",
                        FontSize = 11,
                        FontWeight = FontWeights.Bold,
                        Foreground = RecordViewDialogViewModel.BrChipTxt,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    sp.Children.Add(new TextBlock
                    {
                        Text = line.Trim(),
                        FontSize = 11.5,
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
                    Text = "Not applicable",
                    FontSize = 12.5,
                    Foreground = RecordViewDialogViewModel.BrMutedVal,
                    FontFamily = new FontFamily("Segoe UI"),
                    Margin = new Thickness(0, 0, 0, 8)
                });
            }
            body.Children.Add(wrap);
            CardPanel.Children.Add(card);
        }

        private void AddClassificationsCard(SoloParentRecord r)
        {
            var card = BuildCard("Additional Classifications", out var body);

            bool hasAny = r.IsPantawidBeneficiary || r.IsIndigenous || r.IsLGBT;
            if (!hasAny)
            {
                body.Children.Add(new TextBlock
                {
                    Text = "Not applicable",
                    FontSize = 12.5,
                    Foreground = RecordViewDialogViewModel.BrMutedVal,
                    Margin = new Thickness(16, 12, 16, 12),
                    FontFamily = new FontFamily("Segoe UI")
                });
            }
            else
            {
                var wrap = new WrapPanel { Margin = new Thickness(16, 12, 16, 6) };
                if (r.IsPantawidBeneficiary)
                    wrap.Children.Add(MakeActiveClassificationBadge("Pantawid (4Ps) Beneficiary", Color.FromRgb(0x27, 0xAE, 0x60)));
                if (r.IsIndigenous)
                    wrap.Children.Add(MakeActiveClassificationBadge("Indigenous Person", Color.FromRgb(0x70, 0x29, 0x43)));
                if (r.IsLGBT)
                    wrap.Children.Add(MakeActiveClassificationBadge("LGBTQ+ Member", Color.FromRgb(0x8E, 0x44, 0xAD)));
                body.Children.Add(wrap);
            }
            CardPanel.Children.Add(card);
        }

        private Border MakeActiveClassificationBadge(string title, Color activeColor)
        {
            var bdr = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 8, 8),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Color.FromArgb(20, activeColor.R, activeColor.G, activeColor.B)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, activeColor.R, activeColor.G, activeColor.B))
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new Ellipse
            {
                Width = 7,
                Height = 7,
                Fill = new SolidColorBrush(activeColor),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            sp.Children.Add(new TextBlock
            {
                Text = $"✓ {title}",
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(activeColor),
                FontFamily = new FontFamily("Segoe UI"),
                VerticalAlignment = VerticalAlignment.Center
            });
            bdr.Child = sp;
            return bdr;
        }

        private void AddFamilyCard(List<FamilyMember> members)
        {
            int totalMembers = members?.Count ?? 0;

            int snCount = members?.Count(m => m.HasSpecialNeeds) ?? 0;

            string countText = totalMembers == 1 ? "1 DEPENDENT" : $"{totalMembers} DEPENDENTS";
            if (snCount > 0)
            {
                countText += $" · {snCount} WITH SPECIAL NEEDS (PWD)";
            }

            var card = BuildCard($"Family Composition ({countText})", out var body);

            if (members != null && members.Count > 0)
            {
                var dependents = members
                    .Where(m => RecordDialog.IsChildOrDependent(m.Relationship))
                    .ToList();
                if (dependents.Count > 0 && !dependents.Any(m => (int.TryParse(m.Age, out int a) && a <= 22) || m.HasSpecialNeeds))
                {
                    var overAgeBanner = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xFB, 0xEB)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),
                        BorderThickness = new Thickness(1.2),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(12, 8, 12, 8),
                        Margin = new Thickness(16, 12, 16, 0)
                    };
                    var bannerSp = new StackPanel { Orientation = Orientation.Horizontal };
                    bannerSp.Children.Add(new TextBlock
                    {
                        Text = "⚠",
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xB4, 0x53, 0x09)),
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 0, 6, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    bannerSp.Children.Add(new TextBlock
                    {
                        Text = "All dependents are 23 or older (Over Age Threshold) — subsidy eligibility may be impacted.",
                        FontSize = 11.5,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xB4, 0x53, 0x09)),
                        FontFamily = new FontFamily("Segoe UI"),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    overAgeBanner.Child = bannerSp;
                    body.Children.Add(overAgeBanner);
                }
            }
            if (totalMembers == 0)
            {
                body.Children.Add(new TextBlock
                {
                    Text = "Not applicable",
                    FontSize = 12.5,
                    Foreground = RecordViewDialogViewModel.BrMutedVal,
                    Margin = new Thickness(16, 12, 16, 12),
                    FontFamily = new FontFamily("Segoe UI")
                });
                CardPanel.Children.Add(card);
                return;
            }

            var table = new StackPanel { Margin = new Thickness(14, 8, 14, 14) };

            string[] colLabels = { "NAME & RELATIONSHIP", "BIRTHDATE & AGE", "EDUCATION & INCOME", "SPECIAL NEEDS (PWD)" };
            double[] widths = { 2.4, 1.8, 2.0, 2.2 };
            var headerGrid = new Grid { Margin = new Thickness(0, 8, 0, 6) };
            for (int c = 0; c < colLabels.Length; c++)
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(widths[c], GridUnitType.Star) });
            headerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < colLabels.Length; c++)
            {
                var tb = new TextBlock
                {
                    Text = colLabels[c],
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = RecordViewDialogViewModel.BrHead,
                    Margin = new Thickness(6, 0, 6, 0),
                    FontFamily = new FontFamily("Segoe UI")
                };
                Grid.SetColumn(tb, c);
                headerGrid.Children.Add(tb);
            }
            table.Children.Add(headerGrid);
            table.Children.Add(new Border { Height = 1, Background = RecordViewDialogViewModel.BrDivider, Margin = new Thickness(0, 2, 0, 4) });

            bool alt = false;
            foreach (var m in members)
            {
                var rowBorder = new Border
                {
                    Background = alt ? RecordViewDialogViewModel.BrRowAlt : Brushes.White,
                    BorderBrush = alt ? RecordViewDialogViewModel.BrDivider : Brushes.Transparent,
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(6, 9, 6, 9),
                    Margin = new Thickness(0, 1, 0, 1)
                };
                var rowGrid = new Grid();
                for (int c = 0; c < colLabels.Length; c++)
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(widths[c], GridUnitType.Star) });
                rowGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var col0 = new StackPanel { Margin = new Thickness(6, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
                col0.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(m.MemberName) ? "Not applicable" : m.MemberName,
                    FontSize = 12.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = RecordViewDialogViewModel.BrValue,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    FontFamily = new FontFamily("Segoe UI")
                });

                string relInfo = !string.IsNullOrWhiteSpace(m.Relationship) ? m.Relationship : "Dependent";
                if (!string.IsNullOrWhiteSpace(m.CivilStatus) && !m.CivilStatus.Equals("—", StringComparison.OrdinalIgnoreCase))
                    relInfo += $" · {m.CivilStatus}";
                col0.Children.Add(new TextBlock
                {
                    Text = relInfo,
                    FontSize = 11,
                    Foreground = RecordViewDialogViewModel.BrLabel,
                    Margin = new Thickness(0, 2, 0, 0),
                    FontFamily = new FontFamily("Segoe UI")
                });
                Grid.SetColumn(col0, 0);
                rowGrid.Children.Add(col0);

                var col1 = new StackPanel { Margin = new Thickness(6, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
                col1.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(m.Birthdate) ? "Not applicable" : m.Birthdate,
                    FontSize = 12,
                    Foreground = RecordViewDialogViewModel.BrValue,
                    FontFamily = new FontFamily("Segoe UI")
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
                    Text = sexAge,
                    FontSize = 11,
                    Foreground = RecordViewDialogViewModel.BrLabel,
                    Margin = new Thickness(0, 2, 0, 0),
                    FontFamily = new FontFamily("Segoe UI")
                });
                Grid.SetColumn(col1, 1);
                rowGrid.Children.Add(col1);

                var col2 = new StackPanel { Margin = new Thickness(6, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
                col2.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(m.EducationEmployment) ? "Not applicable" : m.EducationEmployment,
                    FontSize = 12,
                    Foreground = RecordViewDialogViewModel.BrValue,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    FontFamily = new FontFamily("Segoe UI")
                });

                string incDisplay = "No income";
                if (decimal.TryParse(m.Income, out var incVal))
                    incDisplay = incVal > 0 ? $"₱ {incVal:N2}/mo" : "No income";
                else if (!string.IsNullOrWhiteSpace(m.Income) && m.Income != "0")
                    incDisplay = m.Income;
                col2.Children.Add(new TextBlock
                {
                    Text = incDisplay,
                    FontSize = 11,
                    Foreground = RecordViewDialogViewModel.BrLabel,
                    Margin = new Thickness(0, 2, 0, 0),
                    FontFamily = new FontFamily("Segoe UI")
                });
                Grid.SetColumn(col2, 2);
                rowGrid.Children.Add(col2);

                UIElement snElement;
                if (m.HasSpecialNeeds)
                {
                    var snPanel = new StackPanel { Margin = new Thickness(6, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
                    var snBadge = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xC7)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0xFC, 0xD3, 0x4D)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(8, 3, 8, 3),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    var badgeStack = new StackPanel { Orientation = Orientation.Horizontal };
                    badgeStack.Children.Add(new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06)),
                        Margin = new Thickness(0, 0, 5, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    string catText = !string.IsNullOrWhiteSpace(m.SpecialNeedsCategory) ? m.SpecialNeedsCategory : "Special Needs (PWD)";
                    badgeStack.Children.Add(new TextBlock
                    {
                        Text = catText,
                        FontSize = 10.5,
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
                            Text = m.SpecialNeedsDescription.Trim(),
                            FontSize = 10,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x78, 0x35, 0x0F)),
                            Margin = new Thickness(2, 3, 0, 0),
                            TextWrapping = TextWrapping.Wrap,
                            FontFamily = new FontFamily("Segoe UI")
                        });
                    }
                    snElement = snPanel;
                }
                else
                {
                    snElement = new TextBlock
                    {
                        Text = "Not applicable",
                        FontSize = 11.5,
                        Foreground = RecordViewDialogViewModel.BrMutedVal,
                        Margin = new Thickness(6, 0, 6, 0),
                        FontFamily = new FontFamily("Segoe UI"),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
                Grid.SetColumn(snElement, 3);
                rowGrid.Children.Add(snElement);
                rowBorder.Child = rowGrid;
                table.Children.Add(rowBorder);
                alt = !alt;
            }
            body.Children.Add(table);
            CardPanel.Children.Add(card);
        }

        private Border MakeCell(string label, string value, bool showDivider)
        {
            var cell = new Border
            {
                Padding = new Thickness(18, 8, 18, 9),
                BorderBrush = RowLine,
                BorderThickness = new Thickness(0, 0, 0, showDivider ? 1 : 0)
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 11,
                Foreground = RecordViewDialogViewModel.BrLabel,
                Margin = new Thickness(0, 0, 0, 1),
                FontFamily = new FontFamily("Segoe UI")
            });

            bool isBlank = string.IsNullOrWhiteSpace(value) || value == "—" || value.Equals("None", StringComparison.OrdinalIgnoreCase);

            string displayVal = isBlank ? "Not applicable" : value;
            sp.Children.Add(new TextBlock
            {
                Text = displayVal,
                FontSize = 14,
                FontWeight = FontWeights.Normal,
                Foreground = isBlank ? RecordViewDialogViewModel.BrMutedVal : RecordViewDialogViewModel.BrValue,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Segoe UI")
            });
            cell.Child = sp;
            return cell;
        }

        private void DeleteCancel_Click(object sender, RoutedEventArgs e)
        {
            _vm.HandleDeleteCancel();
        }

        private void DeleteConfirm_Click(object sender, RoutedEventArgs e)
        {
            _vm.HandleDeleteConfirm();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _vm.HandleClose();
        }

        private List<AuditLogDto> _cachedTimeline = null;

        private bool _isLoadingTimeline = false;

        private void TabProfile_Click(object sender, RoutedEventArgs e)
        {
            CardPanel.Visibility = Visibility.Visible;
            TimelineContainer.Visibility = Visibility.Collapsed;
        }

        private async void TabTimeline_Click(object sender, RoutedEventArgs e)
        {
            CardPanel.Visibility = Visibility.Collapsed;
            TimelineContainer.Visibility = Visibility.Visible;
            if (_cachedTimeline == null)
            {
                await LoadTimelineAsync();
            }
        }

        private async void BtnRefreshTimeline_Click(object sender, RoutedEventArgs e)
        {
            _cachedTimeline = null;
            await LoadTimelineAsync();
        }

        private async Task LoadTimelineAsync()
        {
            if (_isLoadingTimeline) return;

            string recordId = _vm.Record?.Id;
            if (string.IsNullOrWhiteSpace(recordId))
            {
                TimelineLoadingPanel.Visibility = Visibility.Collapsed;
                TimelineEmptyPanel.Visibility = Visibility.Visible;
                return;
            }
            _isLoadingTimeline = true;
            TimelineLoadingPanel.Visibility = Visibility.Visible;
            TimelineEmptyPanel.Visibility = Visibility.Collapsed;
            TimelineErrorPanel.Visibility = Visibility.Collapsed;
            TimelineItemsPanel.Children.Clear();
            try
            {
                var resp = await SoloParentApiService.Instance.GetSoloParentTimelineAsync(recordId, _vm.Record?.FullName ?? _vm.Record?.Name);
                if (resp != null && resp.Succeeded && resp.Data != null)
                {
                    _cachedTimeline = resp.Data;
                    TimelineLoadingPanel.Visibility = Visibility.Collapsed;
                    if (_cachedTimeline.Count == 0)
                    {
                        TimelineEmptyPanel.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        RenderTimeline(_cachedTimeline);
                    }
                }
                else
                {
                    TimelineLoadingPanel.Visibility = Visibility.Collapsed;
                    TimelineErrorPanel.Visibility = Visibility.Visible;

                    string err = (resp?.Errors != null && resp.Errors.Count > 0)
                        ? string.Join("\n", resp.Errors)
                        : "Failed to load timeline from server.";
                    TxtTimelineError.Text = err;
                }
            }
            catch (Exception ex)
            {
                TimelineLoadingPanel.Visibility = Visibility.Collapsed;
                TimelineErrorPanel.Visibility = Visibility.Visible;
                TxtTimelineError.Text = ex.Message;
            }
            finally
            {
                _isLoadingTimeline = false;
            }
        }

        private void RenderTimeline(List<AuditLogDto> logs)
        {
            TimelineItemsPanel.Children.Clear();
            var sortedLogs = logs.OrderByDescending(x => x.Timestamp).ToList();
            for (int i = 0; i < sortedLogs.Count; i++)
            {
                bool isLast = (i == sortedLogs.Count - 1);
                var node = BuildTimelineNode(sortedLogs[i], isLast);
                TimelineItemsPanel.Children.Add(node);
            }
        }

        private UIElement BuildTimelineNode(AuditLogDto log, bool isLast)
        {
            string actionType = log.ActionType ?? "ACTIVITY";

            string title = "Record Activity";

            string badgeText = "LOG";
            Color badgeBgColor = Color.FromRgb(0xF1, 0xF5, 0xF9);
            Color badgeFgColor = Color.FromRgb(0x47, 0x55, 0x69);
            Color badgeBdrColor = Color.FromRgb(0xCB, 0xD5, 0xE1);
            Color dotColor = Color.FromRgb(0x64, 0x74, 0x8B);

            string dotSymbol = "●";
            if (actionType.IndexOf("CREATE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                title = "Record Created / Registered";
                badgeText = "CREATED";
                badgeBgColor = Color.FromRgb(0xDC, 0xFC, 0xE7);
                badgeFgColor = Color.FromRgb(0x15, 0x80, 0x3D);
                badgeBdrColor = Color.FromRgb(0x86, 0xEF, 0xAC);
                dotColor = Color.FromRgb(0x10, 0xB9, 0x81);
                dotSymbol = "+";
            }
            else if (actionType.IndexOf("UPDATE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                title = "Profile Updated";
                badgeText = "UPDATED";
                badgeBgColor = Color.FromRgb(0xE0, 0xF2, 0xFE);
                badgeFgColor = Color.FromRgb(0x03, 0x69, 0xA1);
                badgeBdrColor = Color.FromRgb(0xBA, 0xE6, 0xFD);
                dotColor = Color.FromRgb(0x02, 0x84, 0xC7);
                dotSymbol = "✎";
            }
            else if (actionType.IndexOf("RENEW", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                title = "Term Renewed";
                badgeText = "RENEWED";
                badgeBgColor = Color.FromRgb(0xFA, 0xEE, 0xF2);
                badgeFgColor = Color.FromRgb(0x70, 0x29, 0x43);
                badgeBdrColor = Color.FromRgb(0xDF, 0x9B, 0xB1);
                dotColor = Color.FromRgb(0x70, 0x29, 0x43);
                dotSymbol = "↻";
            }
            else if (actionType.IndexOf("DELETE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                title = "Record Archived / Deleted";
                badgeText = "DELETED";
                badgeBgColor = Color.FromRgb(0xFE, 0xE2, 0xE2);
                badgeFgColor = Color.FromRgb(0xB9, 0x1C, 0x1C);
                badgeBdrColor = Color.FromRgb(0xFC, 0xA5, 0xA5);
                dotColor = Color.FromRgb(0xEF, 0x44, 0x44);
                dotSymbol = "✕";
            }
            else
            {
                title = actionType.Replace('_', ' ');
                title = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title.ToLower());
            }
            var outerGrid = new Grid { Margin = new Thickness(0, 0, 0, 0) };
            outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var trackGrid = new Grid();
            if (!isLast)
            {
                var line = new Rectangle
                {
                    Width = 2,
                    Fill = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Margin = new Thickness(0, 16, 0, 0)
                };
                trackGrid.Children.Add(line);
            }
            var dotBorder = new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(13),
                Background = new SolidColorBrush(dotColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 8, 0, 0)
            };
            dotBorder.Child = new TextBlock
            {
                Text = dotSymbol,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            trackGrid.Children.Add(dotBorder);
            Grid.SetColumn(trackGrid, 0);
            outerGrid.Children.Add(trackGrid);

            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                BorderBrush = RecordViewDialogViewModel.BrCardBdr,
                BorderThickness = new Thickness(1.2),
                Padding = new Thickness(16, 14, 16, 14),
                Margin = new Thickness(6, 0, 0, 14)
            };
            var cardStack = new StackPanel();

            var headGrid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var titleSp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            titleSp.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Foreground = RecordViewDialogViewModel.BrValue,
                FontFamily = new FontFamily("Segoe UI")
            });
            var badgeBorder = new Border
            {
                Background = new SolidColorBrush(badgeBgColor),
                BorderBrush = new SolidColorBrush(badgeBdrColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 3, 8, 3),
                VerticalAlignment = VerticalAlignment.Center
            };
            badgeBorder.Child = new TextBlock
            {
                Text = badgeText,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(badgeFgColor),
                FontFamily = new FontFamily("Segoe UI")
            };
            Grid.SetColumn(titleSp, 0);
            Grid.SetColumn(badgeBorder, 1);
            headGrid.Children.Add(titleSp);
            headGrid.Children.Add(badgeBorder);
            cardStack.Children.Add(headGrid);

            DateTime localDt = log.Timestamp.LocalDateTime;

            string dateStr = localDt.ToString("MMMM d, yyyy · h:mm tt");

            string actorUserId = log.PerformedBy?.UserId ?? string.Empty;

            string actorRole = !string.IsNullOrWhiteSpace(log.PerformedBy?.Role) ? log.PerformedBy.Role : "User";

            string actorDisplay = !string.IsNullOrWhiteSpace(log.PerformedBy?.Name)
                ? log.PerformedBy.Name
                : (!string.IsNullOrWhiteSpace(log.PerformedBy?.Email)
                    ? log.PerformedBy.Email
                    : (!string.IsNullOrWhiteSpace(actorUserId) ? actorUserId : "System"));
            var metaSp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            metaSp.Children.Add(new TextBlock
            {
                Text = dateStr,
                FontSize = 11.5,
                Foreground = RecordViewDialogViewModel.BrLabel,
                FontFamily = new FontFamily("Segoe UI")
            });
            metaSp.Children.Add(new TextBlock
            {
                Text = "  •  By: ",
                FontSize = 11.5,
                Foreground = RecordViewDialogViewModel.BrMutedVal,
                FontFamily = new FontFamily("Segoe UI")
            });
            metaSp.Children.Add(new TextBlock
            {
                Text = $"{actorDisplay} ({actorRole})",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = RecordViewDialogViewModel.BrHead,
                FontFamily = new FontFamily("Segoe UI")
            });
            cardStack.Children.Add(metaSp);

            if (log.Changes != null && log.Changes.Count > 0)
            {
                var diffBox = new Border
                {
                    Background = RecordViewDialogViewModel.BrRowAlt,
                    BorderBrush = RecordViewDialogViewModel.BrDivider,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 10, 12, 10),
                    Margin = new Thickness(0, 4, 0, 0)
                };
                var diffStack = new StackPanel();
                diffStack.Children.Add(new TextBlock
                {
                    Text = $"MODIFICATIONS ({log.Changes.Count} field{(log.Changes.Count > 1 ? "s" : "")})",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = RecordViewDialogViewModel.BrLabel,
                    Margin = new Thickness(0, 0, 0, 8),
                    FontFamily = new FontFamily("Segoe UI")
                });
                foreach (var ch in log.Changes)
                {
                    string fieldName = !string.IsNullOrWhiteSpace(ch.Field) ? ch.Field : (!string.IsNullOrWhiteSpace(ch.FieldName) ? ch.FieldName : "Field");

                    string oldVal = !string.IsNullOrWhiteSpace(ch.OldValue) ? ch.OldValue : "—";

                    string newVal = !string.IsNullOrWhiteSpace(ch.NewValue) ? ch.NewValue : "—";
                    var chRow = new Grid { Margin = new Thickness(0, 2, 0, 4) };
                    chRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
                    chRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    var fieldTxt = new TextBlock
                    {
                        Text = fieldName,
                        FontSize = 11.5,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = RecordViewDialogViewModel.BrValue,
                        FontFamily = new FontFamily("Segoe UI"),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    var valSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                    var oldBdr = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(0xFE, 0xEE, 0xEE)),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(6, 2, 6, 2),
                        Margin = new Thickness(0, 0, 6, 0)
                    };
                    oldBdr.Child = new TextBlock
                    {
                        Text = oldVal,
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xB9, 0x1C, 0x1C)),
                        TextDecorations = TextDecorations.Strikethrough,
                        FontFamily = new FontFamily("Segoe UI")
                    };
                    var arrowTxt = new TextBlock
                    {
                        Text = "➔",
                        FontSize = 11,
                        Foreground = RecordViewDialogViewModel.BrMutedVal,
                        Margin = new Thickness(0, 0, 6, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    var newBdr = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xFC, 0xE7)),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(6, 2, 6, 2)
                    };
                    newBdr.Child = new TextBlock
                    {
                        Text = newVal,
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x15, 0x80, 0x3D)),
                        FontFamily = new FontFamily("Segoe UI")
                    };
                    valSp.Children.Add(oldBdr);
                    valSp.Children.Add(arrowTxt);
                    valSp.Children.Add(newBdr);
                    Grid.SetColumn(fieldTxt, 0);
                    Grid.SetColumn(valSp, 1);
                    chRow.Children.Add(fieldTxt);
                    chRow.Children.Add(valSp);
                    diffStack.Children.Add(chRow);
                }
                diffBox.Child = diffStack;
                cardStack.Children.Add(diffBox);
            }
            card.Child = cardStack;
            Grid.SetColumn(card, 1);
            outerGrid.Children.Add(card);
            return outerGrid;
        }
    }
}