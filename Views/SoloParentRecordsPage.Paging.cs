using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SOLUM_UI
{
    public partial class SoloParentRecordsPage : Page
    {
        private const int VisiblePageCircles = 3;

        private static readonly SolidColorBrush PageActiveBrush = new SolidColorBrush(Color.FromRgb(0x70, 0x29, 0x43));

        private void RecordsClipGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Grid grid && grid.ActualWidth > 0 && grid.ActualHeight > 0)
            {
                grid.Clip = new RectangleGeometry(new Rect(0, 0, grid.ActualWidth, grid.ActualHeight), 19, 19);
            }
        }

        private void BuildPageNumbers()
        {
            if (PageNumbersPanel == null) return;

            PageNumbersPanel.Children.Clear();

            int total = Math.Max(1, _totalPages);
            int current = Math.Min(Math.Max(1, _currentPage), total);
            int count = Math.Min(VisiblePageCircles, total);
            int first = Math.Min(Math.Max(1, current - (count / 2)), total - count + 1);

            for (int i = 0; i < count; i++)
            {
                int target = first + i;
                var button = new Button
                {
                    Content = target.ToString(),
                    Style = (Style)FindResource("PageNumberBtn")
                };

                if (target == current)
                {
                    button.Background = PageActiveBrush;
                    button.BorderBrush = PageActiveBrush;
                    button.Foreground = Brushes.White;
                }

                button.Click += (s, e) =>
                {
                    if (_isLoading || target == _currentPage) return;

                    _currentPage = target;
                    _ = LoadRecordsAsync();
                };

                PageNumbersPanel.Children.Add(button);
            }
        }
    }
}
