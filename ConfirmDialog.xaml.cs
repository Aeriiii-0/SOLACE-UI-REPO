using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SOLUM_UI
{
    public partial class ConfirmDialog : Window
    {
        public bool Confirmed { get; private set; }

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

            Shell.MaxHeight = screenH * 0.85;

            TxtTitle.Text      = title;
            TxtSubtitle.Text   = subtitle;
            BtnConfirm.Content = confirmLabel;

            BuildContent(fields);
        }

        private static SolidColorBrush Brush(byte r, byte g, byte b) =>
            new SolidColorBrush(Color.FromRgb(r, g, b));

        private void BuildContent(List<ConfirmField> fields)
        {
            var sections = new List<string>();
            foreach (var f in fields)
                if (!sections.Contains(f.Section))
                    sections.Add(f.Section);

            foreach (var section in sections)
            {
                if (ContentPanel.Children.Count > 0)
                    ContentPanel.Children.Add(new Border { Height = 10 });

                var sectionFields = fields.FindAll(f => f.Section == section);

                var card = new Border
                {
                    Background      = Brush(0xFF, 0xFF, 0xFF),
                    CornerRadius    = new CornerRadius(10),
                    BorderBrush     = Brush(0xEE, 0xEE, 0xEE),
                    BorderThickness = new Thickness(1),
                    Padding         = new Thickness(20, 16, 20, 16)
                };

                var cardStack = new StackPanel();

                var sectionRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };
                sectionRow.Children.Add(new Border
                {
                    Width = 3, Background = Brush(0x70, 0x29, 0x43),
                    CornerRadius = new CornerRadius(2), Margin = new Thickness(0, 0, 10, 0)
                });
                sectionRow.Children.Add(new TextBlock
                {
                    Text = section.ToUpper(), FontSize = 10, FontWeight = FontWeights.Bold,
                    Foreground = Brush(0x70, 0x29, 0x43), FontFamily = new FontFamily("Segoe UI"),
                    VerticalAlignment = VerticalAlignment.Center
                });
                cardStack.Children.Add(sectionRow);

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

                    var fieldBlock = new StackPanel
                    {
                        Margin = new Thickness(col == 0 ? 0 : 10, 0, col == 0 ? 10 : 0, 12)
                    };
                    fieldBlock.Children.Add(new TextBlock
                    {
                        Text = sf.Label, FontSize = 10, Foreground = Brush(0x99, 0x99, 0x99),
                        FontFamily = new FontFamily("Segoe UI"), Margin = new Thickness(0, 0, 0, 3)
                    });

                    if (sf.IsChanged)
                    {
                        fieldBlock.Children.Add(new TextBlock
                        {
                            Text = string.IsNullOrEmpty(sf.OldValue) ? "—" : sf.OldValue,
                            FontSize = 12, Foreground = Brush(0xBB, 0xBB, 0xBB),
                            TextDecorations = TextDecorations.Strikethrough,
                            FontFamily = new FontFamily("Segoe UI"), TextWrapping = TextWrapping.Wrap
                        });
                        fieldBlock.Children.Add(new TextBlock
                        {
                            Text = string.IsNullOrEmpty(sf.NewValue) ? "—" : sf.NewValue,
                            FontSize = 13, FontWeight = FontWeights.SemiBold,
                            Foreground = Brush(0x27, 0xAE, 0x60),
                            FontFamily = new FontFamily("Segoe UI"), TextWrapping = TextWrapping.Wrap
                        });
                    }
                    else
                    {
                        fieldBlock.Children.Add(new TextBlock
                        {
                            Text = string.IsNullOrEmpty(sf.NewValue) ? "—" : sf.NewValue,
                            FontSize = 13, FontWeight = FontWeights.SemiBold,
                            Foreground = Brush(0x11, 0x11, 0x11),
                            FontFamily = new FontFamily("Segoe UI"), TextWrapping = TextWrapping.Wrap
                        });
                    }

                    Grid.SetRow(fieldBlock, row);
                    Grid.SetColumn(fieldBlock, col);
                    fieldsGrid.Children.Add(fieldBlock);
                }

                cardStack.Children.Add(fieldsGrid);
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
