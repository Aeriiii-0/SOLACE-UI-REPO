using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SOLUM_UI.Models;
using SOLUM_UI.Services;

namespace SOLUM_UI
{
    public partial class ScannerSettingsDialog : Window
    {
        public ScannerSettings Settings { get; private set; }
        private List<ScannerDeviceInfo> _availableScanners = new List<ScannerDeviceInfo>();

        public ScannerSettingsDialog(ScannerSettings currentSettings = null)
        {
            InitializeComponent();
            Settings = currentSettings ?? ScannerSettings.Load();

            ApplySettingsToUI();
            Loaded += async (s, e) => await RefreshDevicesListAsync();
        }

        private void ApplySettingsToUI()
        {
            if (Settings.Dpi <= 150)
                Rb150Dpi.IsChecked = true;
            else if (Settings.Dpi <= 200)
                Rb200Dpi.IsChecked = true;
            else
                Rb300Dpi.IsChecked = true;

            switch (Settings.ColorMode)
            {
                case ScanColorMode.Grayscale:
                    CmbColorMode.SelectedIndex = 1;
                    break;
                case ScanColorMode.BlackAndWhite:
                    CmbColorMode.SelectedIndex = 2;
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
            if (Rb150Dpi.IsChecked == true)
                Settings.Dpi = 150;
            else if (Rb200Dpi.IsChecked == true)
                Settings.Dpi = 200;
            else
                Settings.Dpi = 300;

            // Color Mode
            switch (CmbColorMode.SelectedIndex)
            {
                case 1:
                    Settings.ColorMode = ScanColorMode.Grayscale;
                    break;
                case 2:
                    Settings.ColorMode = ScanColorMode.BlackAndWhite;
                    break;
                default:
                    Settings.ColorMode = ScanColorMode.Color;
                    break;
            }

            // Paper Source
            Settings.PaperSource = CmbPaperSource.SelectedIndex == 1
                ? ScanPaperSource.Feeder
                : ScanPaperSource.Flatbed;

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
