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

        private static ToastNotification _currentProgress;

        private const double ToastWidth = 320;
        private const double ToastHeight = 80;
        private const double ToastSpacing = 10;
        private const double RightMargin = 20;
        private const double BottomMargin = 20;

        private const string IconCheckCircle = "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M11,16.5L18,9.5L16.59,8.09L11,13.67L7.91,10.59L6.5,12L11,16.5Z";
        private const string IconCloseCircle = "M12,2C17.53,2 22,6.47 22,12C22,17.53 17.53,22 12,22C6.47,22 2,17.53 2,12C2,6.47 6.47,2 12,2M15.59,7L12,10.59L8.41,7L7,8.41L10.59,12L7,15.59L8.41,17L12,13.41L15.59,17L17,15.59L13.41,12L17,8.41L15.59,7Z";
        private const string IconAlert = "M13,14H11V10H13M13,18H11V16H13M1,21H23L12,2L1,21Z";
        private const string IconPlusCircle = "M17,13H13V17H11V13H7V11H11V7H13V11H17M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private const string IconPencil = "M20.71,7.04C21.1,6.65 21.1,6 20.71,5.63L18.37,3.29C18,2.9 17.35,2.9 16.96,3.29L15.12,5.12L18.87,8.87M3,17.25V21H6.75L17.81,9.93L14.06,6.18L3,17.25Z";
        private const string IconTrash = "M19,4H15.5L14.5,3H9.5L8.5,4H5V6H19M6,19A2,2 0 0,0 8,21H16A2,2 0 0,0 18,19V7H6V19Z";
        private const string IconRefresh = "M17.65,6.35C16.2,4.9 14.21,4 12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20C15.73,20 18.84,17.45 19.73,14H17.65C16.83,16.33 14.61,18 12,18A6,6 0 0,1 6,12A6,6 0 0,1 12,6C13.66,6 15.14,6.69 16.22,7.78L13,11H20V4L17.65,6.35Z";
        private const string IconDownload = "M5,20H19V18H5M19,9H15V3H9V9H5L12,16L19,9Z";
        private const string IconKey = "M7,14A2,2 0 0,1 5,12A2,2 0 0,1 7,10A2,2 0 0,1 9,12A2,2 0 0,1 7,14M12.65,10C11.83,7.67 9.61,6 7,6A6,6 0 0,0 1,12A6,6 0 0,0 7,18C9.61,18 11.83,16.33 12.65,14H17V18H21V14H23V10H12.65Z";
        private const string IconCopy = "M19,21H8V7H19M19,5H8A2,2 0 0,0 6,7V21A2,2 0 0,0 8,23H19A2,2 0 0,0 21,21V7A2,2 0 0,0 19,5M16,1H4A2,2 0 0,0 2,3V17H4V3H16V1Z";
        private const string IconClock = "M12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22C6.47,22 2,17.5 2,12A10,10 0 0,1 12,2M12.5,7V12.25L17,14.92L16.25,16.15L11,13V7H12.5Z";

        private const string ColorSuccess = "#27AE60";
        private const string ColorUpdate = "#D99A00";
        private const string ColorDelete = "#E53935";
        private const string ColorExport = "#2F6FDE";
        private const string ColorWarning = "#F57F17";
        private const string ColorBrand = "#702943";
        private const string ColorNeutral = "#64748B";

        private DispatcherTimer _timer;

        public ToastNotification()
        {
            InitializeComponent();
            this.Owner = null;
        }

        public static void Show(string title, string message, ToastType type,
                                double durationSeconds = 3.5)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_currentProgress != null)
                {
                    var prev = _currentProgress;
                    _currentProgress = null;
                    prev._timer?.Stop();
                    prev.Close();
                }

                var toast = new ToastNotification();
                toast.TxtTitle.Text = title;
                toast.TxtMessage.Text = message;
                toast.ApplyType(type, title, message);
                toast.PositionToast();
                toast.StartTimer(durationSeconds);

                lock (_lock) { _active.Add(toast); }

                if (type == ToastType.Info)
                    _currentProgress = toast;

                toast.Closed += (s, e) =>
                {
                    lock (_lock) { _active.Remove(toast); }
                    if (ReferenceEquals(_currentProgress, toast))
                        _currentProgress = null;
                    RepositionAll();
                };

                toast.Show();
            });
        }

        private static void Resolve(ToastType type, string title, string message, out string color, out string icon)
        {
            if (type == ToastType.Error)
            {
                color = ColorDelete; icon = IconCloseCircle; return;
            }
            if (type == ToastType.Warning)
            {
                color = ColorWarning; icon = IconAlert; return;
            }

            string text = ((title ?? string.Empty) + " " + (message ?? string.Empty)).ToLowerInvariant();

            if (text.Contains("delet") || text.Contains("remov"))
            {
                color = ColorDelete; icon = IconTrash; return;
            }
            if (text.Contains("re-enable") || text.Contains("renew") || text.Contains("restor") ||
                text.Contains("reactivat") || text.Contains("refresh") || text.Contains("reload"))
            {
                color = ColorSuccess; icon = IconRefresh; return;
            }
            if (text.Contains("export") || text.Contains("download"))
            {
                color = ColorExport; icon = IconDownload; return;
            }
            if (text.Contains("creat") || text.Contains("added") || text.Contains("adding") ||
                text.Contains("add ") || text.Contains("register"))
            {
                color = ColorSuccess; icon = IconPlusCircle; return;
            }
            if (text.Contains("updat") || text.Contains("edit") || text.Contains("sav") || text.Contains("chang"))
            {
                color = ColorUpdate; icon = IconPencil; return;
            }
            if (text.Contains("copied") || text.Contains("copy"))
            {
                color = ColorNeutral; icon = IconCopy; return;
            }
            if (text.Contains("reset") || text.Contains("password") || text.Contains("generat"))
            {
                color = ColorBrand; icon = IconKey; return;
            }

            if (type == ToastType.Success)
            {
                color = ColorSuccess; icon = IconCheckCircle; return;
            }
            color = ColorBrand; icon = IconClock;
        }

        private static SolidColorBrush Frozen(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        private static Color TintOver(Color c, double amount)
        {
            byte Mix(byte v) => (byte)Math.Round(255 + (v - 255) * amount);
            return Color.FromRgb(Mix(c.R), Mix(c.G), Mix(c.B));
        }

        private void ApplyType(ToastType type, string title, string message)
        {
            Resolve(type, title, message, out string colorHex, out string iconData);

            Color accent = (Color)ColorConverter.ConvertFromString(colorHex);
            var accentBrush = Frozen(accent);
            var tintBrush = Frozen(TintOver(accent, 0.14));

            var geometry = Geometry.Parse(iconData);
            geometry.Freeze();

            RootBorder.BorderBrush = accentBrush;
            IconTile.Background = tintBrush;
            IconPath.Data = geometry;
            IconPath.Fill = accentBrush;
        }

        private void PositionToast()
        {
            var area = SystemParameters.WorkArea;
            int index;
            lock (_lock) { index = _active.Count; }
            Left = area.Right - ToastWidth - RightMargin;
            Top = area.Bottom - BottomMargin - ToastHeight - index * (ToastHeight + ToastSpacing);
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
                        _active[i].Top = area.Bottom - BottomMargin - ToastHeight
                                         - i * (ToastHeight + ToastSpacing);
                    }
                }
            });
        }

        private void StartTimer(double seconds)
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(seconds)
            };
            _timer.Tick += (s, e) => { _timer.Stop(); Close(); };
            _timer.Start();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            Close();
        }
    }
}