using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SOLUM_UI.Models;

namespace SOLUM_UI
{
    public partial class RecordViewDialog : Window
    {
        private readonly SoloParentRecord _record;
        public bool OpenedEdit { get; private set; }

        private static readonly SolidColorBrush LabelBrush  = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
        private static readonly SolidColorBrush ValueBrush  = new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x11));
        private static readonly SolidColorBrush CardBorder  = new SolidColorBrush(Color.FromRgb(0xE8, 0xEC, 0xF2));
        private static readonly SolidColorBrush HeadBrush   = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22));

        public RecordViewDialog(SoloParentRecord record)
        {
            InitializeComponent();
            _record = record;
            Loaded += (s, e) => { ApplySize(); BuildContent(); };
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => ApplySize();
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) { }

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
            DialogShell.MaxHeight = screenH * 0.88;
            DialogShell.Width     = Math.Min(screenW * 0.68, 860);
        }

        private void BuildContent()
        {
            var r = _record;

            TxtRecordName.Text = r.Name ?? string.Empty;
            TxtSex.Text        = r.Sex ?? "—";

            int age = 0;
            if (r.DateOfBirth != DateTime.MinValue)
            {
                age = DateTime.Today.Year - r.DateOfBirth.Year;
                if (r.DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
            }
            TxtAge.Text = age > 0 ? age.ToString() : "—";
            TxtDob.Text = r.DateOfBirth != DateTime.MinValue
                ? r.DateOfBirth.ToString("MM/dd/yyyy") : "—";

            CardPanel.Children.Clear();

            AddCard("Personal Information", new[]
            {
                ("Contact Number", r.ContactNumber),
                ("Barangay",       r.Barangay),
                ("Address",        r.Address),
                ("Civil Status",   r.CivilStatus),
            });

            AddCard("Identity Details", new[]
            {
                ("Last Name",     r.Surname),
                ("First Name",    r.FirstName),
                ("Middle Name",   r.MiddleName),
                ("Extension",     r.ExtensionName),
                ("Place of Birth",r.PlaceOfBirth),
            });

            AddCard("Record", new[]
            {
                ("SP ID",        r.Id),
                ("Status",       r.Status),
                ("Last Updated", r.LastUpdated != DateTime.MinValue ? r.LastUpdated.ToString("MMMM d, yyyy") : "—"),
            });
        }

        private void AddCard(string title, (string Label, string Value)[] fields)
        {
            var card = new Border
            {
                Background      = Brushes.White,
                CornerRadius    = new CornerRadius(12),
                BorderBrush     = CardBorder,
                BorderThickness = new Thickness(1),
                Padding         = new Thickness(22, 18, 22, 18),
                Margin          = new Thickness(0, 0, 0, 16)
            };

            var inner = new StackPanel();

            var heading = new TextBlock
            {
                Text       = title,
                FontSize   = 14,
                FontWeight = FontWeights.Bold,
                Foreground = HeadBrush,
                Margin     = new Thickness(0, 0, 0, 16)
            };
            inner.Children.Add(heading);

            var fieldGrid = new Grid();
            fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int row = 0;
            int col = 0;
            foreach (var (label, value) in fields)
            {
                if (col == 0)
                    fieldGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var cell = MakeFieldCell(label, value);
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, col);
                fieldGrid.Children.Add(cell);

                col++;
                if (col > 1) { col = 0; row++; }
            }

            if (col == 1)
                fieldGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            inner.Children.Add(fieldGrid);
            card.Child = inner;
            CardPanel.Children.Add(card);
        }

        private StackPanel MakeFieldCell(string label, string value)
        {
            var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };

            sp.Children.Add(new TextBlock
            {
                Text       = label,
                FontSize   = 12,
                Foreground = LabelBrush,
                Margin     = new Thickness(0, 0, 0, 3)
            });

            sp.Children.Add(new TextBlock
            {
                Text         = string.IsNullOrWhiteSpace(value) ? "—" : value,
                FontSize     = 14,
                FontWeight   = FontWeights.SemiBold,
                Foreground   = ValueBrush,
                TextWrapping = TextWrapping.Wrap
            });

            return sp;
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            OpenedEdit = true;
            DialogResult = true;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
