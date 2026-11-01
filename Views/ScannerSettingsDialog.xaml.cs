using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SOLUM_UI.Models;
using SOLUM_UI.Services;

namespace SOLUM_UI
{
    public partial class ScannerSettingsDialog : Window
    {
        public ScannerSettings Settings { get; private set; }
        private List<ScannerDeviceInfo> _availableScanners = new List<ScannerDeviceInfo>();

        // Smooth header dragging fields
        private Point _dragStartPoint;
        private bool _isDragging = false;
        private double _startTransformX = 0;
        private double _startTransformY = 0;

        public ScannerSettingsDialog(ScannerSettings currentSettings = null)
        {
            InitializeComponent();
            Settings = currentSettings ?? ScannerSettings.Load();

            ApplySettingsToUI();
            Loaded += async (s, e) =>
            {
                ApplySize();
                await RefreshDevicesListAsync();
            };
        }

        private void ApplySize()
        {
            double screenW, screenH, screenLeft, screenTop;

            if (Owner != null && Owner.ActualWidth > 0 && Owner.ActualHeight > 0)
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

            if (Math.Abs(Width - screenW) > 0.5) Width = screenW;
            if (Math.Abs(Height - screenH) > 0.5) Height = screenH;
            if (Math.Abs(Left - screenLeft) > 0.5) Left = screenLeft;
            if (Math.Abs(Top - screenTop) > 0.5) Top = screenTop;
        }

        private void Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == BackdropBorder)
            {
                DialogResult = false;
                Close();
            }
        }

        #region Header Dragging
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Do not initiate drag if user clicked a child Button (such as the Close X button)
            var dep = e.OriginalSource as DependencyObject;
            while (dep != null && dep != HeaderBorder)
            {
                if (dep is Button) return;
                dep = VisualTreeHelper.GetParent(dep);
            }

            _isDragging = true;
            _dragStartPoint = e.GetPosition(this);
            _startTransformX = DialogShellTransform.X;
            _startTransformY = DialogShellTransform.Y;
            HeaderBorder.CaptureMouse();
        }

        private void Header_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            Point current = e.GetPosition(this);
            DialogShellTransform.X = _startTransformX + (current.X - _dragStartPoint.X);
            DialogShellTransform.Y = _startTransformY + (current.Y - _dragStartPoint.Y);
        }

        private void Header_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                HeaderBorder.ReleaseMouseCapture();
            }
        }
        #endregion

        private void ApplySettingsToUI()
        {
            if (Settings.Dpi > 200)
                Rb300Dpi.IsChecked = true;
            else
                Rb200Dpi.IsChecked = true;

            switch (Settings.ColorMode)
            {
                case ScanColorMode.Grayscale:
                    CmbColorMode.SelectedIndex = 1;
                    break;
                default:
                    CmbColorMode.SelectedIndex = 0;
                    break;
            }

            CmbPaperSource.SelectedIndex = Settings.PaperSource == ScanPaperSource.Feeder ? 1 : 0;
        }

        private async Task RefreshDevicesListAsync()
        {
            TxtDeviceStatus.Text = "Scanning for connected hardware...";
            CmbDevices.Items.Clear();

            _availableScanners = await WiaScannerService.GetConnectedScannersAsync();

            if (_availableScanners.Count == 0)
            {
                CmbDevices.Items.Add(new ComboBoxItem
                {
                    Content = "No scanner hardware detected",
                    Tag = string.Empty
                });
                CmbDevices.SelectedIndex = 0;
                TxtDeviceStatus.Text = "No WIA scanner devices detected. Ensure device is plugged in and powered on.";
                return;
            }

            int selectedIndex = 0;
            for (int i = 0; i < _availableScanners.Count; i++)
            {
                var scanner = _availableScanners[i];
                string label = string.IsNullOrWhiteSpace(scanner.Manufacturer)
                    ? scanner.Name
                    : $"{scanner.Name} ({scanner.Manufacturer})";

                var item = new ComboBoxItem
                {
                    Content = label,
                    Tag = scanner.DeviceId
                };

                CmbDevices.Items.Add(item);

                if (!string.IsNullOrEmpty(Settings.DeviceId) &&
                    string.Equals(scanner.DeviceId, Settings.DeviceId, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                }
            }

            CmbDevices.SelectedIndex = selectedIndex;
            TxtDeviceStatus.Text = $"{_availableScanners.Count} scanner device(s) available and ready.";
        }

        private async void BtnRefreshDevices_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDevicesListAsync();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // DPI
            Settings.Dpi = (Rb300Dpi.IsChecked == true) ? 300 : 200;

            // Color Mode
            Settings.ColorMode = (CmbColorMode.SelectedIndex == 1)
                ? ScanColorMode.Grayscale
                : ScanColorMode.Color;

            // Paper Source
            Settings.PaperSource = CmbPaperSource.SelectedIndex == 1
                ? ScanPaperSource.Feeder
                : ScanPaperSource.Flatbed;

            // Paper Size locked to official 8.5x13 Long Bond dimension
            Settings.PaperSize = "8.5x13";

            // Selected Device
            if (CmbDevices.SelectedItem is ComboBoxItem selectedItem)
            {
                string devId = selectedItem.Tag?.ToString() ?? string.Empty;
                Settings.DeviceId = devId;

                var matched = _availableScanners.FirstOrDefault(s => s.DeviceId == devId);
                Settings.DeviceName = matched != null ? matched.Name : selectedItem.Content?.ToString();
            }

            Settings.Save();
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
