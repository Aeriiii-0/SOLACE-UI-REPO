using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace SOLUM_UI
{
    public enum ToastType { Success, Info, Warning, Error }

    public partial class ToastNotification : Window
    {
        private static readonly List<ToastNotification> _active = new List<ToastNotification>();
        private static readonly object _lock = new object();

        private const double ToastHeight = 72;
        private const double ToastSpacing = 8;
        private const double RightMargin = 20;
        private const double BottomMargin = 20;

        private DispatcherTimer _timer;

        public ToastNotification()
        {
            InitializeComponent();
        }

        public static void Show(string title, string message, ToastType type)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var toast = new ToastNotification();
                toast.TxtTitle.Text = title;
                toast.TxtMessage.Text = message;
                toast.ApplyType(type);
                toast.PositionToast();
                toast.StartTimer();
                lock (_lock) { _active.Add(toast); }
                toast.Closed += (s, e) =>
                {
                    lock (_lock) { _active.Remove(toast); }
                    RepositionAll();
                };
                toast.Show();
            });
        }

        private void ApplyType(ToastType type)
        {
            string color;
            string icon;
            switch (type)
            {
                case ToastType.Success:
                    color = "#27AE60";
                    icon  = "✓";
                    break;
                case ToastType.Warning:
                    color = "#F57F17";
                    icon  = "⚠";
                    break;
                case ToastType.Error:
                    color = "#E53935";
                    icon  = "✕";
                    break;
                default:
                    color = "#702943";
                    icon  = "i";
                    break;
            }
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            AccentBar.Background  = brush;
            IconCircle.Background = brush;
            TxtIcon.Text          = icon;
        }

        private void PositionToast()
        {
            var area = SystemParameters.WorkArea;
            int index;
            lock (_lock) { index = _active.Count; }
            Left = area.Right - Width - RightMargin;
            Top  = area.Bottom - BottomMargin - ToastHeight - index * (ToastHeight + ToastSpacing);
        }

        private static void RepositionAll()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var area = SystemParameters.WorkArea;
                lock (_lock)
                {
                    for (int i = 0; i < _active.Count; i++)
                    {
                        _active[i].Top = area.Bottom - BottomMargin - ToastHeight - i * (ToastHeight + ToastSpacing);
                    }
                }
            });
        }

        private void StartTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                Close();
            };
            _timer.Start();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            Close();
        }
    }
}
