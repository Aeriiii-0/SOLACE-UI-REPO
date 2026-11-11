using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SOLUM_UI
{
    public enum ConfirmMode { Add, Edit, Renew }

    public partial class ConfirmDialog : Window
    {
        public bool Confirmed { get; private set; }

        // ── Neutral colors (same in every mode) ───────────────────────────
        private static readonly SolidColorBrush _label = Clr(0x77, 0x77, 0x77);
        private static readonly SolidColorBrush _value = Clr(0x11, 0x11, 0x11);
        private static readonly SolidColorBrush _oldVal = Clr(0x80, 0x80, 0x80);
        private static readonly SolidColorBrush _bg = Clr(0xF5, 0xF6, 0xF8);
        private static readonly SolidColorBrush _divider = Clr(0xEE, 0xEE, 0xF2);

        // ── Mode colors: green = Add/Renew, yellow-orange = Edit ──────────
        // _accent  : modal border, section header band, icon circle, confirm button
        // _tint    : light background for changed cells / chips / badge
        // _chipTxt : dark readable text on the tint
        // _changed : label + highlight color for changed fields
        private SolidColorBrush _accent;
        private SolidColorBrush _tint;
        private SolidColorBrush _chipTxt;
        private SolidColorBrush _changed;

        private readonly ConfirmMode _mode;
        private readonly int _totalChanged;

        private static SolidColorBrush Clr(byte r, byte g, byte b) =>
            new SolidColorBrush(Color.FromRgb(r, g, b));

        public ConfirmDialog(string title, string subtitle, string confirmLabel,
                             List<ConfirmField> fields,
                             double screenW, double screenH,
                             double screenLeft, double screenTop,
                             ConfirmMode mode = ConfirmMode.Add)
        {
            InitializeComponent();
            _mode = mode;
            _totalChanged = fields != null ? fields.FindAll(f => f.IsChanged).Count : 0;
            Width = screenW;
            Height = screenH;
            Left = screenLeft;
            Top = screenTop;

            // Same footprint as the Profile details modal (~63% wide, max 1210; ~88% tall).
            // Rounded to whole pixels so edges and text stay sharp.
            double shellW = Math.Round(Math.Max(720, Math.Min(1210, screenW * 0.63)));
            Shell.Width = shellW;
            Shell.MaxWidth = shellW; // also clamps anything WindowPositionHelper.CoverOwnerWindow applies
            Shell.MaxHeight = Math.Round(screenH * 0.88);

            // Show dim overlay when confirm/review changes modal opens
            Loaded += (s, e) => MainWindow.ShowDimOverlay(fullWindow: true);
            Closed += (s, e) => MainWindow.HideDimOverlay();

            TxtTitle.Text = title;
            TxtSubtitle.Text = subtitle;
            TxtConfirmBtn.Text = confirmLabel;

            switch (mode)
            {
                case ConfirmMode.Edit:
                    // Yellow-orange
                    _accent = Clr(0xC9, 0x77, 0x00);
                    _tint = Clr(0xFF, 0xF4, 0xE0);
                    _chipTxt = Clr(0x7A, 0x45, 0x00);
                    _changed = Clr(0xA8, 0x5F, 0x00);
                    IconSymbol.Text = "✎";
                    if (_totalChanged > 0)
                    {
                        TxtSubtitle.Text = $"{_totalChanged} field{(_totalChanged == 1 ? "" : "s")} modified. Review your changes before saving.";
                    }
                    else
                    {
                        TxtSubtitle.Text = "No changes were made to this record. Modify at least one field to save.";
                        BtnConfirm.IsEnabled = false;
                        BtnConfirm.Opacity = 0.45;
                        BtnConfirm.ToolTip = "No changes to save.";
                    }
                    break;

                case ConfirmMode.Renew:
                    // Green — same as Add
                    _accent = Clr(0x1E, 0x8B, 0x4E);
                    _tint = Clr(0xE6, 0xF6, 0xEC);
                    _chipTxt = Clr(0x14, 0x66, 0x3A);
                    _changed = Clr(0x1E, 0x8B, 0x4E);
                    IconSymbol.Text = "↻";
                    break;

                default: // Add
                    _accent = Clr(0x1E, 0x8B, 0x4E);
                    _tint = Clr(0xE6, 0xF6, 0xEC);
                    _chipTxt = Clr(0x14, 0x66, 0x3A);
                    _changed = Clr(0x1E, 0x8B, 0x4E);
                    break;
            }

            // One accent drives the whole modal
            Shell.BorderBrush = _accent;
            Shell.BorderThickness = new Thickness(2);
            IconCircle.Background = _accent;
            BtnConfirm.Background = _accent;

            BuildContent(fields);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            SOLUM_UI.Core.WindowPositionHelper.CoverOwnerWindow(this, Shell);
        }

        // ── Small helper so every text element is built the same way ──────
        private static TextBlock Tb(string text, double size, FontWeight weight, Brush fg, bool wrap = false)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = size,
                FontWeight = weight,
                Foreground = fg,
                FontFamily = new FontFamily("Segoe UI"),
                TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap
            };
        }

        // "Separated → Single" (old struck through, new bold) as one wrapping TextBlock
        private TextBlock ChangedValue(ConfirmField sf, double size)
        {
            var tb = new TextBlock
            {
                FontSize = size,
                FontFamily = new FontFamily("Segoe UI"),
                TextWrapping = TextWrapping.Wrap
            };
            tb.Inlines.Add(new Run(string.IsNullOrEmpty(sf.OldValue) ? "—" : sf.OldValue)
            {
                Foreground = _oldVal,
                TextDecorations = TextDecorations.Strikethrough
            });
            tb.Inlines.Add(new Run("  →  ") { Foreground = _label });
            tb.Inlines.Add(new Run(string.IsNullOrEmpty(sf.NewValue) ? "—" : sf.NewValue)
            {
                Foreground = _value,
                FontWeight = FontWeights.SemiBold
            });
            return tb;
        }

        // "Civil status · modified" label line
        private TextBlock ModifiedLabel(string label)
        {
            var tb = new TextBlock
            {
                FontSize = 12,
                FontFamily = new FontFamily("Segoe UI"),
                Margin = new Thickness(0, 0, 0, 4)
            };
            tb.Inlines.Add(new Run(label) { Foreground = _changed });
            tb.Inlines.Add(new Run("  ·  modified") { Foreground = _changed, FontWeight = FontWeights.SemiBold });
            return tb;
        }

        private void BuildContent(List<ConfirmField> fields)
        {
            var summaryName = fields.Find(f => f.Label == "Last Name")?.NewValue ?? string.Empty;
            var summaryFirst = fields.Find(f => f.Label == "First Name")?.NewValue ?? string.Empty;
            var summarySex = fields.Find(f => f.Label == "Sex")?.NewValue ?? string.Empty;
            var summaryAge = fields.Find(f => f.Label == "Age")?.NewValue ?? string.Empty;
            var summaryContact = fields.Find(f => f.Label == "Contact No.")?.NewValue ?? string.Empty;

            string fullName = string.IsNullOrWhiteSpace(summaryName)
                ? summaryFirst
                : summaryName + (string.IsNullOrWhiteSpace(summaryFirst) ? "" : ", " + summaryFirst);

            // ── Summary header (name + sex / age / contact), like profile details ──
            var summaryGrid = new Grid { Margin = new Thickness(0, 18, 0, 18) };
            summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var si = new StackPanel();
            var nameTb = Tb(string.IsNullOrWhiteSpace(fullName) ? "—" : fullName.ToUpperInvariant(),
                            26, FontWeights.SemiBold, _value, true);
            nameTb.Margin = new Thickness(0, 0, 0, 4);
            si.Children.Add(nameTb);

            string sexAgeLine = string.Empty;
            if (!string.IsNullOrWhiteSpace(summarySex) && summarySex != "—") sexAgeLine = summarySex;
            if (!string.IsNullOrWhiteSpace(summaryAge) && summaryAge != "—")
                sexAgeLine += (sexAgeLine.Length > 0 ? "  ·  " : "") + summaryAge + " years old";
            if (!string.IsNullOrWhiteSpace(summaryContact) && summaryContact != "—")
                sexAgeLine += (sexAgeLine.Length > 0 ? "  ·  " : "") + summaryContact;
            if (!string.IsNullOrWhiteSpace(sexAgeLine))
                si.Children.Add(Tb(sexAgeLine, 15, FontWeights.Normal, _label));
            Grid.SetColumn(si, 0);
            summaryGrid.Children.Add(si);

            // Mode badge (visual only)
            var modeBadge = new Border
            {
                Background = _tint,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 5, 12, 5),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(16, 4, 0, 0),
                Child = Tb(_mode == ConfirmMode.Renew ? "RENEWAL" : (_mode == ConfirmMode.Edit ? "EDITING" : "NEW APPLICANT"),
                           12, FontWeights.Bold, _chipTxt)
            };
            Grid.SetColumn(modeBadge, 1);
            summaryGrid.Children.Add(modeBadge);
            ContentPanel.Children.Add(summaryGrid);

            // ── Sections ──────────────────────────────────────────────────
            var sections = new List<string>();
            foreach (var f in fields)
                if (!sections.Contains(f.Section)) sections.Add(f.Section);

            foreach (var section in sections)
            {
                var sectionFields = fields.FindAll(f => f.Section == section);
                int sectionChangedCount = sectionFields.FindAll(f => f.IsChanged).Count;

                // Card: accent border, header band in the SAME accent color
                var card = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(12),
                    BorderBrush = _accent,
                    BorderThickness = new Thickness(2),
                    Margin = new Thickness(0, 0, 0, 16)
                };
                var outer = new StackPanel();

                // Header band: accent background, white bold text (readable)
                var hdr = new StackPanel { Orientation = Orientation.Horizontal };
                var hdrText = Tb(section.ToUpperInvariant(), 13, FontWeights.Bold, Brushes.White);
                hdrText.VerticalAlignment = VerticalAlignment.Center;
                hdr.Children.Add(hdrText);
                if (sectionChangedCount > 0)
                {
                    hdr.Children.Add(new Border
                    {
                        Background = Brushes.White,
                        CornerRadius = new CornerRadius(5),
                        Padding = new Thickness(8, 2, 8, 2),
                        Margin = new Thickness(12, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = Tb($"{sectionChangedCount} MODIFIED", 10, FontWeights.Bold, _chipTxt)
                    });
                }
                outer.Children.Add(new Border
                {
                    Background = _accent,
                    CornerRadius = new CornerRadius(10, 10, 0, 0),
                    Padding = new Thickness(20, 12, 20, 12),
                    Child = hdr
                });

                // Body container. Chip / row sections get padding; the field grid is full-bleed.
                var cardStack = new StackPanel { Margin = new Thickness(20, 16, 20, 8) };
                outer.Children.Add(cardStack);

                if (section == "Circumstances")
                {
                    bool anyCircChanged = sectionFields.Exists(f => f.IsChanged);
                    if (anyCircChanged)
                    {
                        foreach (var sf in sectionFields)
                            cardStack.Children.Add(CreateChangedRow(sf));
                    }
                    else if (sectionFields.Count == 1 && (sectionFields[0].NewValue == "None selected" || sectionFields[0].NewValue == "—"))
                    {
                        var none = Tb("None selected", 15, FontWeights.Normal, _label);
                        none.Margin = new Thickness(0, 0, 0, 10);
                        cardStack.Children.Add(none);
                    }
                    else
                    {
                        foreach (var sf in sectionFields)
                        {
                            var lines = (sf.NewValue ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            if (lines.Length == 0) lines = new[] { "None selected" };
                            foreach (var line in lines)
                            {
                                cardStack.Children.Add(new Border
                                {
                                    Background = _tint,
                                    CornerRadius = new CornerRadius(8),
                                    Padding = new Thickness(14, 9, 14, 9),
                                    Margin = new Thickness(0, 0, 0, 8),
                                    Child = Tb(line, 14, FontWeights.SemiBold, _chipTxt, true)
                                });
                            }
                        }
                    }
                }
                else if (section == "Family Composition")
                {
                    foreach (var sf in sectionFields)
                    {
                        if (sf.IsChanged)
                        {
                            cardStack.Children.Add(CreateChangedRow(sf));
                        }
                        else if (sf.Label.IndexOf("Notice", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 sf.Label.IndexOf("Eligibility", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            var rowStack = new StackPanel();
                            var l = Tb(sf.Label, 12, FontWeights.Bold, Clr(0xB4, 0x53, 0x09));
                            l.Margin = new Thickness(0, 0, 0, 3);
                            rowStack.Children.Add(l);
                            rowStack.Children.Add(Tb(string.IsNullOrWhiteSpace(sf.NewValue) ? "—" : sf.NewValue,
                                                     14, FontWeights.SemiBold, Clr(0x92, 0x40, 0x0E), true));
                            cardStack.Children.Add(new Border
                            {
                                Background = Clr(0xFF, 0xFB, 0xEB),
                                BorderBrush = Clr(0xF5, 0x9E, 0x0B),
                                BorderThickness = new Thickness(1),
                                CornerRadius = new CornerRadius(8),
                                Padding = new Thickness(14, 10, 14, 10),
                                Margin = new Thickness(0, 0, 0, 8),
                                Child = rowStack
                            });
                        }
                        else
                        {
                            var rowStack = new StackPanel();
                            var l = Tb(sf.Label, 12, FontWeights.Normal, _label);
                            l.Margin = new Thickness(0, 0, 0, 3);
                            rowStack.Children.Add(l);
                            rowStack.Children.Add(Tb(string.IsNullOrWhiteSpace(sf.NewValue) ? "—" : sf.NewValue,
                                                     15, FontWeights.SemiBold, _value, true));
                            cardStack.Children.Add(new Border
                            {
                                Background = _bg,
                                CornerRadius = new CornerRadius(8),
                                Padding = new Thickness(14, 10, 14, 10),
                                Margin = new Thickness(0, 0, 0, 8),
                                Child = rowStack
                            });
                        }
                    }
                }
                else
                {
                    // Full-bleed 2-column label/value grid with hairline row dividers (profile-details look)
                    cardStack.Margin = new Thickness(0);
                    int lastRow = (sectionFields.Count - 1) / 2;

                    var fieldsGrid = new Grid();
                    fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    for (int i = 0; i < sectionFields.Count; i++)
                    {
                        var sf = sectionFields[i];
                        int row = i / 2;
                        int col = i % 2;
                        while (fieldsGrid.RowDefinitions.Count <= row)
                            fieldsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                        bool isLastRow = row == lastRow;
                        var cellContainer = new Border
                        {
                            Padding = new Thickness(20, 12, 20, 12),
                            Background = sf.IsChanged ? _tint : Brushes.Transparent,
                            BorderBrush = _divider,
                            BorderThickness = new Thickness(0, 0, 0, isLastRow ? 0 : 1),
                            CornerRadius = isLastRow
                                ? (col == 0 ? new CornerRadius(0, 0, 0, 10) : new CornerRadius(0, 0, 10, 0))
                                : new CornerRadius(0)
                        };

                        var cell = new StackPanel();
                        if (sf.IsChanged)
                        {
                            cell.Children.Add(ModifiedLabel(sf.Label));
                            cell.Children.Add(ChangedValue(sf, 16));
                        }
                        else
                        {
                            var l = Tb(sf.Label, 12, FontWeights.Normal, _label);
                            l.Margin = new Thickness(0, 0, 0, 4);
                            cell.Children.Add(l);
                            cell.Children.Add(Tb(string.IsNullOrEmpty(sf.NewValue) ? "—" : sf.NewValue,
                                                 16, FontWeights.Normal, _value, true));
                        }
                        cellContainer.Child = cell;
                        Grid.SetRow(cellContainer, row);
                        Grid.SetColumn(cellContainer, col);
                        fieldsGrid.Children.Add(cellContainer);
                    }
                    cardStack.Children.Add(fieldsGrid);
                }

                card.Child = outer;
                ContentPanel.Children.Add(card);
            }
        }

        // Highlighted row for changed values in chip/row-style sections
        private Border CreateChangedRow(ConfirmField sf)
        {
            var stack = new StackPanel();
            stack.Children.Add(ModifiedLabel(sf.Label));
            stack.Children.Add(ChangedValue(sf, 15));
            return new Border
            {
                Background = _tint,
                CornerRadius = new CornerRadius(8),
                BorderBrush = _accent,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 8),
                Child = stack
            };
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (_mode == ConfirmMode.Edit && _totalChanged == 0) return;
            Confirmed = true;
            Close();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            Close();
        }
    }

    public class ConfirmField
    {
        public string Section { get; set; }
        public string Label { get; set; }
        public string NewValue { get; set; }
        public string OldValue { get; set; }
        public bool IsChanged { get; set; }
    }
}