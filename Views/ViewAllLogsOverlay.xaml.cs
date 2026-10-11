using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI
{
    public partial class ViewAllLogsOverlay : UserControl
    {
        public static readonly RoutedEvent CloseRequestedEvent =
            EventManager.RegisterRoutedEvent(
                "CloseRequested",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ViewAllLogsOverlay));

        public event RoutedEventHandler CloseRequested
        {
            add    => AddHandler(CloseRequestedEvent, value);
            remove => RemoveHandler(CloseRequestedEvent, value);
        }

        public ViewAllLogsOverlay()
        {
            InitializeComponent();

            // Only load if the DashboardPage hasn't already pre-fetched.
            Loaded += async (s, e) =>
            {
                SuppressDatePickerWatermarks();
                if (DataContext is AuditLogViewModel vm && !vm.IsInitialized)
                    await vm.LoadAsync();
            };
        }

        /// <summary>
        /// Suppresses the default "Select a date" watermark in all DatePickerTextBox controls
        /// to avoid overlapping with our custom MM/DD/YYYY placeholder.
        /// </summary>
        /// <summary>
        /// Watermark suppression is now handled via XAML ControlTemplate in BrandDatePicker style.
        /// This method is kept for compatibility but no longer needed.
        /// </summary>
        private void SuppressDatePickerWatermarks()
        {
            // XAML template now properly suppresses native watermark by wrapping ScrollViewer in Grid
            // This prevents rendering of PART_Watermark element, so no code-behind suppression needed
        }

        /// <summary>
        /// Helper method to find all visual children of a given type.
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                    if (child is T typed)
                    {
                        yield return typed;
                    }
                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CloseRequestedEvent));
        }
    }
}
