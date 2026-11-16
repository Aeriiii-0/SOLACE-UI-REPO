using System;
using System.Windows;
using System.Windows.Input;
using SOLUM_UI.Core;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class ViewAllLogsDialog : Window
    {
        public ViewAllLogsDialog(object vm = null, Window owner = null)
        {
            InitializeComponent();
            Owner = owner ?? Application.Current?.MainWindow;
            if (vm != null)
            {
                LogsView.DataContext = vm;
            }
            Loaded += (s, e) => WindowPositionHelper.CoverOwnerWindow(this);
            SizeChanged += (s, e) => WindowPositionHelper.CoverOwnerWindow(this);
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    Close();
                }
            };
        }

        private void Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                Close();
            }
        }

        private void LogsView_CloseRequested(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static void Show(object vm, Window owner = null)
        {
            var dialog = new ViewAllLogsDialog(vm, owner);
            WindowPositionHelper.CoverOwnerWindow(dialog);
            dialog.ShowDialog();
        }
    }
}
