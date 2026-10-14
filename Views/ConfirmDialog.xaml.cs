using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SOLUM_UI
{
    public enum ConfirmMode { Add, Edit, Renew }

    public partial class ConfirmDialog : Window
    {
        public bool Confirmed { get; private set; }

        private static readonly SolidColorBrush _label   = Clr(0x77, 0x77, 0x77);
        private static readonly SolidColorBrush _value   = Clr(0x11, 0x11, 0x11);
        private static readonly SolidColorBrush _old     = Clr(0xBB, 0xBB, 0xBB);
        private static readonly SolidColorBrush _border  = Clr(0xE4, 0xE6, 0xEB);
        private static readonly SolidColorBrush _accent  = Clr(0x70, 0x29, 0x43);
        private static readonly SolidColorBrush _bg      = Clr(0xF5, 0xF6, 0xF8);
        private static readonly SolidColorBrush _head    = Clr(0x1A, 0x1A, 0x1A);
        private static readonly SolidColorBrush _chipBg  = Clr(0xF0, 0xE8, 0xEC);
        private static readonly SolidColorBrush _chipTxt = Clr(0x70, 0x29, 0x43);
        private static readonly SolidColorBrush _divider = Clr(0xEE, 0xEE, 0xF2);

        // Mode-specific colors
        private SolidColorBrush _iconBg;    // icon circle background
        private SolidColorBrush _btnColor;  // confirm button
        private SolidColorBrush _changed;   // "after" value color for changed fields

        private static SolidColorBrush Clr(byte r, byte g, byte b) =>
            new SolidColorBrush(Color.FromRgb(r, g, b));

        public ConfirmDialog(string title, string subtitle, string confirmLabel,
                             List<ConfirmField> fields,
                             double screenW, double screenH,
                             double screenLeft, double screenTop,
                             ConfirmMode mode = ConfirmMode.Add)
        {
            InitializeComponent();
            Width  = screenW;
            Height = screenH;
            Left   = screenLeft;
            Top    = screenTop;
            Shell.MaxHeight = screenH * 0.90;

            TxtTitle.Text      = title;
            TxtSubtitle.Text   = subtitle;
            TxtConfirmBtn.Text = confirmLabel;

            // Apply mode colors
            switch (mode)
            {
                case ConfirmMode.Renew:
                    // Green — same as add
                    _iconBg   = Clr(0x27, 0xAE, 0x60);
                    _btnColor = Clr(0x27, 0xAE, 0x60);
                    _changed  = Clr(0x1E, 0x8B, 0x4E);
                    SetIconCircle("#27AE60", "↻");
                    SetConfirmBtn("#27AE60");
                    break;

                case ConfirmMode.Edit:
                    // Amber
                    _iconBg   = Clr(0xF5, 0xA6, 0x23);
                    _btnColor = Clr(0xD4, 0x7D, 0x00);
                    _changed  = Clr(0xB3, 0x6A, 0x00);
                    SetIconCircle("#F5A623", "✎");
                    SetConfirmBtn("#D47D00");

                    int totalChanged = fields != null ? fields.FindAll(f => f.IsChanged).Count : 0;
                    if (totalChanged > 0)
                    {
                        TxtSubtitle.Text = $"{totalChanged} field{(totalChanged == 1 ? "" : "s")} modified. Review your changes before saving.";
                    }
                    else
                    {
                        TxtSubtitle.Text = "Review your record details before saving.";
                    }
                    break;

                default: // Add
                    _iconBg   = Clr(0x27, 0xAE, 0x60);
                    _btnColor = Clr(0x27, 0xAE, 0x60);
                    _changed  = Clr(0x1E, 0x8B, 0x4E);
                    // default XAML icon/btn already green — no change needed
                    break;
            }

            BuildContent(fields);
        }

        private void SetIconCircle(string hexColor, string symbol)
        {
            if (IconCircle != null)
            {
                IconCircle.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(hexColor);
                IconSymbol.Text       = symbol;
            }
        }

        private void SetConfirmBtn(string hexColor)
        {
            // Named elements inside ControlTemplates aren't direct fields;
            // set the Background on the Button itself and let XAML propagate it.
            BtnConfirm.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(hexColor);
            // Also override via tag so the template border picks it up
            BtnConfirm.Tag = hexColor;
        }

        private void BuildContent(List<ConfirmField> fields)
        {
            var summaryName    = fields.Find(f => f.Label == "Last Name")?.NewValue   ?? string.Empty;
            var summaryFirst   = fields.Find(f => f.Label == "First Name")?.NewValue  ?? string.Empty;
            var summarySex     = fields.Find(f => f.Label == "Sex")?.NewValue         ?? string.Empty;
            var summaryAge     = fields.Find(f => f.Label == "Age")?.NewValue         ?? string.Empty;
            var summaryContact = fields.Find(f => f.Label == "Contact No.")?.NewValue ?? string.Empty;

            string fullName = string.IsNullOrWhiteSpace(summaryName)
                ? summaryFirst
                : summaryName + (string.IsNullOrWhiteSpace(summaryFirst) ? "" : ", " + summaryFirst);

            var summaryCard = new Border
            {
                Background      = Brushes.White,
                CornerRadius    = new CornerRadius(14),
                BorderBrush     = _border,
                BorderThickness = new Thickness(1.5),
                Padding         = new Thickness(24, 20, 24, 20),
                Margin          = new Thickness(0, 12, 0, 20)
            };
            var si = new StackPanel();
            si.Children.Add(new TextBlock
            {
                Text       = string.IsNullOrWhiteSpace(fullName) ? "—" : fullName,
                FontSize   = 20, FontWeight = FontWeights.Bold,
                Foreground = _value, FontFamily = new FontFamily("Segoe UI"),
                Margin     = new Thickness(0, 0, 0, 5)
            });
            string sexAgeLine = string.Empty;
            if (!string.IsNullOrWhiteSpace(summarySex) && summarySex != "—") sexAgeLine = summarySex;
            if (!string.IsNullOrWhiteSpace(summaryAge) && summaryAge != "—")
                sexAgeLine += (sexAgeLine.Length > 0 ? ", " : "") + summaryAge + " years old";
            if (!string.IsNullOrWhiteSpace(sexAgeLine))
                si.Children.Add(new TextBlock { Text = sexAgeLine, FontSize = 14, Foreground = _label, FontFamily = new FontFamily("Segoe UI"), Margin = new Thickness(0, 0, 0, 3) });
            if (!string.IsNullOrWhiteSpace(summaryContact) && summaryContact != "—")
                si.Children.Add(new TextBlock { Text = summaryContact, FontSize = 14, Foreground = _label, FontFamily = new FontFamily("Segoe UI") });
            summaryCard.Child = si;
            ContentPanel.Children.Add(summaryCard);

            var sections = new List<string>();
            foreach (var f in fields)
                if (!sections.Contains(f.Section)) sections.Add(f.Section);

            foreach (var section in sections)
            {
                var sectionFields = fields.FindAll(f => f.Section == section);
                int sectionChangedCount = sectionFields.FindAll(f => f.IsChanged).Count;

                var card = new Border
                {
                    Background      = Brushes.White,
                    CornerRadius    = new CornerRadius(14),
                    BorderBrush     = sectionChangedCount > 0 ? Clr(0xF5, 0xC6, 0x7A) : _border,
                    BorderThickness = new Thickness(sectionChangedCount > 0 ? 2 : 1.5),
                    Padding         = new Thickness(24, 18, 24, 8),
                    Margin          = new Thickness(0, 0, 0, 14)
                };
                var cardStack = new StackPanel();

                var hdr = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };
                hdr.Children.Add(new Border { Width = 4, CornerRadius = new CornerRadius(2), Background = sectionChangedCount > 0 ? (_btnColor ?? _accent) : _accent, Margin = new Thickness(0, 2, 12, 2) });
                hdr.Children.Add(new TextBlock { Text = section, FontSize = 15, FontWeight = FontWeights.Bold, Foreground = _head, FontFamily = new FontFamily("Segoe UI"), VerticalAlignment = VerticalAlignment.Center });

                if (sectionChangedCount > 0)
                {
                    var secBadge = new Border
                    {
                        Background = Clr(0xFF, 0xF3, 0xE0),
                        BorderBrush = Clr(0xF5, 0xC6, 0x7A),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(5),
                        Padding = new Thickness(8, 2, 8, 2),
                        Margin = new Thickness(10, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    secBadge.Child = new TextBlock
                    {
                        Text = $"{sectionChangedCount} MODIFIED",
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = Clr(0xB3, 0x6A, 0x00),
                        FontFamily = new FontFamily("Segoe UI")
                    };
                    hdr.Children.Add(secBadge);
                }

                cardStack.Children.Add(hdr);
                cardStack.Children.Add(new Border { Height = 1, Background = _divider, Margin = new Thickness(0, 0, 0, 14) });

                if (section == "Circumstances")
                {
                    bool anyCircChanged = sectionFields.Exists(f => f.IsChanged);
                    if (anyCircChanged)
                    {
                        foreach (var sf in sectionFields)
                        {
                            cardStack.Children.Add(CreateChangedRow(sf));
                        }
                    }
                    else if (sectionFields.Count == 1 && (sectionFields[0].NewValue == "None selected" || sectionFields[0].NewValue == "—"))
                    {
                        cardStack.Children.Add(new TextBlock
                        {
                            Text = "None selected", FontSize = 14, Foreground = _label,
                            FontFamily = new FontFamily("Segoe UI"), Margin = new Thickness(0, 0, 0, 10)
                        });
                    }
                    else
                    {
                        foreach (var sf in sectionFields)
                        {
                            var lines = (sf.NewValue ?? "").Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                            if (lines.Length == 0) lines = new[] { "None selected" };

                            foreach (var line in lines)
                            {
                                var row = new Border
                                {
                                    Background      = _chipBg,
                                    CornerRadius    = new CornerRadius(8),
                                    BorderBrush     = _chipBg,
                                    BorderThickness = new Thickness(1),
                                    Padding         = new Thickness(14, 8, 14, 8),
                                    Margin          = new Thickness(0, 0, 0, 6)
                                };
                                var rowInner = new StackPanel { Orientation = Orientation.Horizontal };
                                rowInner.Children.Add(new TextBlock
                                {
                                    Text         = line,
                                    FontSize     = 12.5,
                                    FontWeight   = FontWeights.SemiBold,
                                    Foreground   = _chipTxt,
                                    FontFamily   = new FontFamily("Segoe UI"),
                                    VerticalAlignment = VerticalAlignment.Center,
                                    TextWrapping = TextWrapping.Wrap
                                });
                                row.Child = rowInner;
                                cardStack.Children.Add(row);
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
                        else
                        {
                            var row = new Border
                            {
                                Background = _bg, CornerRadius = new CornerRadius(8),
                                Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 8)
                            };
                            var rowStack = new StackPanel();
                            rowStack.Children.Add(new TextBlock { Text = sf.Label, FontSize = 11, Foreground = _label, FontFamily = new FontFamily("Segoe UI"), Margin = new Thickness(0, 0, 0, 3) });
                            rowStack.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(sf.NewValue) ? "—" : sf.NewValue, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = _value, FontFamily = new FontFamily("Segoe UI"), TextWrapping = TextWrapping.Wrap });
                            row.Child = rowStack;
                            cardStack.Children.Add(row);
                        }
                    }
                }
                else
                {
                    var fieldsGrid = new Grid();
                    fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    for (int i = 0; i < sectionFields.Count; i++)
                    {
                        var sf  = sectionFields[i];
                        int row = i / 2;
                        int col = i % 2;

                        while (fieldsGrid.RowDefinitions.Count <= row)
                            fieldsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                        var cellContainer = new Border
                        {
                            Margin = new Thickness(0, 0, col == 0 ? 14 : 0, 12),
                            CornerRadius = new CornerRadius(8),
                            Padding = sf.IsChanged ? new Thickness(10, 8, 10, 8) : new Thickness(0, 0, 0, 4),
                            Background = sf.IsChanged ? Clr(0xFF, 0xF9, 0xE6) : Brushes.Transparent,
                            BorderBrush = sf.IsChanged ? Clr(0xF5, 0xC6, 0x7A) : Brushes.Transparent,
                            BorderThickness = new Thickness(sf.IsChanged ? 1.5 : 0)
                        };

                        var cell = new StackPanel();

                        var labelRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
                        labelRow.Children.Add(new TextBlock
                        {
                            Text = sf.Label,
                            FontSize = 11,
                            FontWeight = sf.IsChanged ? FontWeights.Bold : FontWeights.Normal,
                            Foreground = sf.IsChanged ? Clr(0xB3, 0x6A, 0x00) : _label,
                            FontFamily = new FontFamily("Segoe UI")
                        });

                        if (sf.IsChanged)
                        {
                            var badge = new Border
                            {
                                Background = Clr(0xF5, 0xA6, 0x23),
                                CornerRadius = new CornerRadius(4),
                                Padding = new Thickness(5, 1, 5, 1),
                                Margin = new Thickness(6, 0, 0, 0),
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            badge.Child = new TextBlock
                            {
                                Text = "CHANGED",
                                FontSize = 9,
                                FontWeight = FontWeights.Bold,
                                Foreground = Brushes.White,
                                FontFamily = new FontFamily("Segoe UI")
                            };
                            labelRow.Children.Add(badge);
                        }

                        cell.Children.Add(labelRow);

                        if (sf.IsChanged)
                        {
                            var prevRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 2) };
                            prevRow.Children.Add(new TextBlock
                            {
                                Text = "Previous: ",
                                FontSize = 11,
                                Foreground = Clr(0x88, 0x88, 0x88),
                                FontFamily = new FontFamily("Segoe UI")
                            });
                            prevRow.Children.Add(new TextBlock
                            {
                                Text = string.IsNullOrEmpty(sf.OldValue) ? "—" : sf.OldValue,
                                FontSize = 12,
                                Foreground = Clr(0xDC, 0x35, 0x45),
                                TextDecorations = TextDecorations.Strikethrough,
                                FontFamily = new FontFamily("Segoe UI"),
                                TextWrapping = TextWrapping.Wrap
                            });
                            cell.Children.Add(prevRow);

                            var newRow = new StackPanel { Orientation = Orientation.Horizontal };
                            newRow.Children.Add(new TextBlock
                            {
                                Text = "New: ",
                                FontSize = 12,
                                FontWeight = FontWeights.SemiBold,
                                Foreground = _changed ?? Clr(0xB3, 0x6A, 0x00),
                                FontFamily = new FontFamily("Segoe UI")
                            });
                            newRow.Children.Add(new TextBlock
                            {
                                Text = string.IsNullOrEmpty(sf.NewValue) ? "—" : sf.NewValue,
                                FontSize = 14,
                                FontWeight = FontWeights.Bold,
                                Foreground = _changed ?? Clr(0xB3, 0x6A, 0x00),
                                FontFamily = new FontFamily("Segoe UI"),
                                TextWrapping = TextWrapping.Wrap
                            });
                            cell.Children.Add(newRow);
                        }
                        else
                        {
                            cell.Children.Add(new TextBlock
                            {
                                Text = string.IsNullOrEmpty(sf.NewValue) ? "—" : sf.NewValue,
                                FontSize = 14,
                                FontWeight = FontWeights.SemiBold,
                                Foreground = _value,
                                FontFamily = new FontFamily("Segoe UI"),
                                TextWrapping = TextWrapping.Wrap
                            });
                        }

                        cellContainer.Child = cell;
                        Grid.SetRow(cellContainer, row);
                        Grid.SetColumn(cellContainer, col);
                        fieldsGrid.Children.Add(cellContainer);
                    }
                    cardStack.Children.Add(fieldsGrid);
                }

                card.Child = cardStack;
                ContentPanel.Children.Add(card);
            }
        }

        private Border CreateChangedRow(ConfirmField sf)
        {
            var row = new Border
            {
                Background      = Clr(0xFF, 0xF9, 0xE6),
                CornerRadius    = new CornerRadius(8),
                BorderBrush     = Clr(0xF5, 0xC6, 0x7A),
                BorderThickness = new Thickness(1.5),
                Padding         = new Thickness(14, 10, 14, 10),
                Margin          = new Thickness(0, 0, 0, 8)
            };

            var stack = new StackPanel();

            var hdr = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            hdr.Children.Add(new TextBlock
            {
                Text = sf.Label,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Clr(0xB3, 0x6A, 0x00),
                FontFamily = new FontFamily("Segoe UI")
            });

            var badge = new Border
            {
                Background = Clr(0xF5, 0xA6, 0x23),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(5, 1, 5, 1),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            badge.Child = new TextBlock
            {
                Text = "CHANGED",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI")
            };
            hdr.Children.Add(badge);
            stack.Children.Add(hdr);

            var prevPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 2) };
            prevPanel.Children.Add(new TextBlock
            {
                Text = "Previous: ",
                FontSize = 11,
                Foreground = Clr(0x88, 0x88, 0x88),
                FontFamily = new FontFamily("Segoe UI")
            });
            prevPanel.Children.Add(new TextBlock
            {
                Text = string.IsNullOrEmpty(sf.OldValue) ? "—" : sf.OldValue,
                FontSize = 12,
                Foreground = Clr(0xDC, 0x35, 0x45),
                TextDecorations = TextDecorations.Strikethrough,
                FontFamily = new FontFamily("Segoe UI"),
                TextWrapping = TextWrapping.Wrap
            });
            stack.Children.Add(prevPanel);

            var newPanel = new StackPanel { Orientation = Orientation.Horizontal };
            newPanel.Children.Add(new TextBlock
            {
                Text = "New: ",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = _changed ?? Clr(0xB3, 0x6A, 0x00),
                FontFamily = new FontFamily("Segoe UI")
            });
            newPanel.Children.Add(new TextBlock
            {
                Text = string.IsNullOrEmpty(sf.NewValue) ? "—" : sf.NewValue,
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Foreground = _changed ?? Clr(0xB3, 0x6A, 0x00),
                FontFamily = new FontFamily("Segoe UI"),
                TextWrapping = TextWrapping.Wrap
            });
            stack.Children.Add(newPanel);

            row.Child = stack;
            return row;
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
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
        public string Section   { get; set; }
        public string Label     { get; set; }
        public string NewValue  { get; set; }
        public string OldValue  { get; set; }
        public bool   IsChanged { get; set; }
    }
}
