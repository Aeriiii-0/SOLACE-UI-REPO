using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SOLUM_UI
{
    public partial class ConfirmDialog : Window
    {
        public bool Confirmed { get; private set; }

        private static readonly SolidColorBrush _label   = Clr(0x77, 0x77, 0x77);
        private static readonly SolidColorBrush _value   = Clr(0x11, 0x11, 0x11);
        private static readonly SolidColorBrush _changed = Clr(0x1E, 0x8B, 0x4E);
        private static readonly SolidColorBrush _old     = Clr(0xBB, 0xBB, 0xBB);
        private static readonly SolidColorBrush _border  = Clr(0xE4, 0xE6, 0xEB);
        private static readonly SolidColorBrush _accent  = Clr(0x70, 0x29, 0x43);
        private static readonly SolidColorBrush _bg      = Clr(0xF5, 0xF6, 0xF8);
        private static readonly SolidColorBrush _head    = Clr(0x1A, 0x1A, 0x1A);
        private static readonly SolidColorBrush _chipBg  = Clr(0xF0, 0xE8, 0xEC);
        private static readonly SolidColorBrush _chipTxt = Clr(0x70, 0x29, 0x43);
        private static readonly SolidColorBrush _divider = Clr(0xEE, 0xEE, 0xF2);

        private static SolidColorBrush Clr(byte r, byte g, byte b) =>
            new SolidColorBrush(Color.FromRgb(r, g, b));

        public ConfirmDialog(string title, string subtitle, string confirmLabel,
                             List<ConfirmField> fields,
                             double screenW, double screenH,
                             double screenLeft, double screenTop)
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

            BuildContent(fields);
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

                var card = new Border
                {
                    Background      = Brushes.White,
                    CornerRadius    = new CornerRadius(14),
                    BorderBrush     = _border,
                    BorderThickness = new Thickness(1.5),
                    Padding         = new Thickness(24, 18, 24, 8),
                    Margin          = new Thickness(0, 0, 0, 14)
                };
                var cardStack = new StackPanel();

                var hdr = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };
                hdr.Children.Add(new Border { Width = 4, CornerRadius = new CornerRadius(2), Background = _accent, Margin = new Thickness(0, 2, 12, 2) });
                hdr.Children.Add(new TextBlock { Text = section, FontSize = 15, FontWeight = FontWeights.Bold, Foreground = _head, FontFamily = new FontFamily("Segoe UI"), VerticalAlignment = VerticalAlignment.Center });
                cardStack.Children.Add(hdr);

                cardStack.Children.Add(new Border { Height = 1, Background = _divider, Margin = new Thickness(0, 0, 0, 14) });

                if (section == "Circumstances")
                {
                    if (sectionFields.Count == 1 && sectionFields[0].Label == "Circumstances" && sectionFields[0].NewValue == "None selected")
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
                            bool isDetail = sf.Label.StartsWith("   ");
                            var row = new Border
                            {
                                Background      = isDetail ? Brushes.White : _chipBg,
                                CornerRadius    = new CornerRadius(8),
                                BorderBrush     = isDetail ? _border : _chipBg,
                                BorderThickness = new Thickness(1),
                                Padding         = new Thickness(isDetail ? 12 : 14, 7, 14, 7),
                                Margin          = new Thickness(isDetail ? 16 : 0, 0, 0, 6)
                            };
                            var rowInner = new StackPanel { Orientation = Orientation.Horizontal };
                            rowInner.Children.Add(new TextBlock
                            {
                                Text         = sf.Label.Trim() + ":",
                                FontSize     = 12,
                                FontWeight   = isDetail ? FontWeights.Normal : FontWeights.SemiBold,
                                Foreground   = isDetail ? _label : _chipTxt,
                                FontFamily   = new FontFamily("Segoe UI"),
                                Margin       = new Thickness(0, 0, 8, 0),
                                VerticalAlignment = VerticalAlignment.Center
                            });
                            rowInner.Children.Add(new TextBlock
                            {
                                Text         = string.IsNullOrWhiteSpace(sf.NewValue) ? "—" : sf.NewValue,
                                FontSize     = 12,
                                FontWeight   = FontWeights.SemiBold,
                                Foreground   = sf.NewValue == "✓ Ticked" || sf.NewValue == "✓ Yes"
                                                ? Clr(0x1E, 0x8B, 0x4E) : _value,
                                FontFamily   = new FontFamily("Segoe UI"),
                                VerticalAlignment = VerticalAlignment.Center,
                                TextWrapping = TextWrapping.Wrap
                            });
                            row.Child = rowInner;
                            cardStack.Children.Add(row);
                        }
                    }
                }
                else if (section == "Family Composition")
                {
                    foreach (var sf in sectionFields)
                    {
                        var row = new Border
                        {
                            Background = _bg, CornerRadius = new CornerRadius(8),
                            Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 8)
                        };
                        var rowStack = new StackPanel();
                        rowStack.Children.Add(new TextBlock { Text = sf.Label, FontSize = 11, Foreground = _label, FontFamily = new FontFamily("Segoe UI"), Margin = new Thickness(0, 0, 0, 3) });
                        rowStack.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(sf.NewValue) ? "—" : sf.NewValue, FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = _value, FontFamily = new FontFamily("Segoe UI"), TextWrapping = TextWrapping.Wrap });
                        row.Child = rowStack;
                        cardStack.Children.Add(row);
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

                        var cell = new StackPanel { Margin = new Thickness(0, 0, col == 0 ? 20 : 0, 16) };
                        cell.Children.Add(new TextBlock { Text = sf.Label, FontSize = 11, Foreground = _label, FontFamily = new FontFamily("Segoe UI"), Margin = new Thickness(0, 0, 0, 4) });

                        if (sf.IsChanged)
                        {
                            cell.Children.Add(new TextBlock { Text = string.IsNullOrEmpty(sf.OldValue) ? "—" : sf.OldValue, FontSize = 12, Foreground = _old, TextDecorations = TextDecorations.Strikethrough, FontFamily = new FontFamily("Segoe UI"), TextWrapping = TextWrapping.Wrap });
                            cell.Children.Add(new TextBlock { Text = string.IsNullOrEmpty(sf.NewValue) ? "—" : sf.NewValue, FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = _changed, FontFamily = new FontFamily("Segoe UI"), TextWrapping = TextWrapping.Wrap });
                        }
                        else
                        {
                            cell.Children.Add(new TextBlock { Text = string.IsNullOrEmpty(sf.NewValue) ? "—" : sf.NewValue, FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = _value, FontFamily = new FontFamily("Segoe UI"), TextWrapping = TextWrapping.Wrap });
                        }

                        Grid.SetRow(cell, row);
                        Grid.SetColumn(cell, col);
                        fieldsGrid.Children.Add(cell);
                    }
                    cardStack.Children.Add(fieldsGrid);
                }

                card.Child = cardStack;
                ContentPanel.Children.Add(card);
            }
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
