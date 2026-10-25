using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SOLUM_UI.Services;

namespace SOLUM_UI.Views.Components
{
    public partial class RecordSortControl : UserControl
    {
        public event EventHandler<RecordSortEventArgs> SortChanged;

        public RecordSortField SelectedField { get; private set; } = RecordSortField.Barangay;
        public bool IsAscending { get; private set; } = true;

        private static readonly SolidColorBrush ActiveBg = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));
        private static readonly SolidColorBrush SelectedItemBg = new SolidColorBrush(Color.FromRgb(0xFD, 0xF0, 0xF3));
        private static readonly SolidColorBrush TransparentBrush = new SolidColorBrush(Colors.Transparent);
        private static readonly SolidColorBrush MaroonBrush = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));

        public RecordSortControl()
        {
            InitializeComponent();
            UpdateUI();
        }

        private void SortDropBtn_Click(object sender, RoutedEventArgs e)
        {
            SortPopup.IsOpen = true;
        }

        private void DirectionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                bool newAsc = btn.Tag?.ToString() == "ASC";
                if (IsAscending != newAsc)
                {
                    IsAscending = newAsc;
                    UpdateUI();
                    SortChanged?.Invoke(this, new RecordSortEventArgs(SelectedField, IsAscending));
                }
            }
        }

        private void SortOpt_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && Enum.TryParse<RecordSortField>(elem.Tag?.ToString(), out var field))
            {
                if (SelectedField == field)
                {
                    // Toggling the same field reverses the direction
                    IsAscending = !IsAscending;
                }
                else
                {
                    SelectedField = field;
                    // Default to ascending, except LastUpdated where newest (descending) is natural
                    IsAscending = field != RecordSortField.LastUpdated;
                }

                UpdateUI();
                SortPopup.IsOpen = false;
                SortChanged?.Invoke(this, new RecordSortEventArgs(SelectedField, IsAscending));
            }
        }

        public void SetSort(RecordSortField field, bool isAscending)
        {
            SelectedField = field;
            IsAscending = isAscending;
            UpdateUI();
            SortChanged?.Invoke(this, new RecordSortEventArgs(SelectedField, IsAscending));
        }

        private void UpdateUI()
        {
            // Update Direction Pills
            ApplyPillStyle(BtnAscending, IsAscending);
            ApplyPillStyle(BtnDescending, !IsAscending);

            // Dynamic Subtitles
            if (TxtBarangaySubtitle != null)
                TxtBarangaySubtitle.Text = IsAscending ? "A to Z" : "Z to A (Reversed)";
            if (TxtLastUpdatedSubtitle != null)
                TxtLastUpdatedSubtitle.Text = IsAscending ? "Oldest created first (Reversed)" : "Recently created first (Newest)";

            // Update Field Item Selection
            UpdateOptionRow(OptBarangay, IndBarangay, SelectedField == RecordSortField.Barangay);
            UpdateOptionRow(OptLastUpdated, IndLastUpdated, SelectedField == RecordSortField.LastUpdated);

            // Update Trigger Label
            string arrow = IsAscending ? "▲" : "▼";
            string fieldName;
            switch (SelectedField)
            {
                case RecordSortField.LastUpdated:
                    fieldName = "Date Created";
                    break;
                case RecordSortField.Barangay:
                default:
                    fieldName = "Barangay";
                    break;
            }
            if (TxtSortLabel != null)
                TxtSortLabel.Text = $"Sort: {fieldName} {arrow}";
        }

        private void ApplyPillStyle(Button btn, bool isActive)
        {
            if (btn == null) return;
            var border = btn.Template?.FindName("PillBg", btn) as Border;
            var text = btn.Template?.FindName("PillTxt", btn) as TextBlock;
            if (border != null) border.Background = isActive ? ActiveBg : TransparentBrush;
            if (text != null) text.Foreground = isActive ? Brushes.White : MaroonBrush;
        }

        private void UpdateOptionRow(Border row, TextBlock ind, bool isSelected)
        {
            if (row != null) row.Background = isSelected ? SelectedItemBg : TransparentBrush;
            if (ind != null) ind.Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
