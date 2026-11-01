using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SOLUM_UI.Models;

namespace SOLUM_UI.Services
{
    public class ScannerDeviceInfo
    {
        public string DeviceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
    }

    public static class WiaScannerService
    {
        // WIA COM Property IDs
        private const int WIA_SCAN_COLOR_MODE                = 6146; // 1 = Color, 2 = Grayscale, 4 = B&W
        private const int WIA_HORIZONTAL_SCAN_RESOLUTION_DPI = 6147;
        private const int WIA_VERTICAL_SCAN_RESOLUTION_DPI   = 6148;
        private const int WIA_HORIZONTAL_SCAN_START_PIXEL   = 6149;
        private const int WIA_VERTICAL_SCAN_START_PIXEL     = 6150;
        private const int WIA_HORIZONTAL_SCAN_SIZE_PIXELS    = 6151;
        private const int WIA_VERTICAL_SCAN_SIZE_PIXELS      = 6152;
        private const int WIA_DPS_DOCUMENT_HANDLING_SELECT   = 3088; // 1 = Flatbed, 2 = Feeder
        private const int WIA_DPS_PAGES                      = 3096; // Number of pages to scan
        private const int WIA_DPS_HORIZONTAL_BED_SIZE        = 3074; // Thousandths of an inch
        private const int WIA_DPS_VERTICAL_BED_SIZE          = 3075; // Thousandths of an inch

        // WIA Format GUIDs
        public const string WIA_FORMAT_PNG  = "{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}";
        public const string WIA_FORMAT_BMP  = "{B96B3CAB-0728-11D3-9D7B-0000F81EF32E}";
        public const string WIA_FORMAT_JPEG = "{B96B3CAF-0728-11D3-9D7B-0000F81EF32E}";

        // WIA Device Types
        private const int WIA_DEVICE_TYPE_SCANNER = 1;

        /// <summary>
        /// Retrieves a list of all WIA-compatible scanner devices currently detected on the system.
        /// </summary>
        public static Task<List<ScannerDeviceInfo>> GetConnectedScannersAsync()
        {
            return RunInStaAsync(() =>
            {
                var list = new List<ScannerDeviceInfo>();
                try
                {
                    Type managerType = Type.GetTypeFromProgID("WIA.DeviceManager");
                    if (managerType == null)
                        return list;

                    dynamic deviceManager = Activator.CreateInstance(managerType);
                    int count = (int)deviceManager.DeviceInfos.Count;

                    for (int i = 1; i <= count; i++)
                    {
                        try
                        {
                            dynamic info = deviceManager.DeviceInfos[i];
                            int type = (int)info.Type;

                            if (type == WIA_DEVICE_TYPE_SCANNER)
                            {
                                string devId = info.DeviceID?.ToString() ?? string.Empty;
                                string name = GetPropertyValue(info.Properties, "Name") ?? "Scanner Device";
                                string desc = GetPropertyValue(info.Properties, "Description") ?? string.Empty;
                                string mfg = GetPropertyValue(info.Properties, "Manufacturer") ?? string.Empty;

                                list.Add(new ScannerDeviceInfo
                                {
                                    DeviceId = devId,
                                    Name = name,
                                    Description = desc,
                                    Manufacturer = mfg
                                });
                            }
                        }
                        catch
                        {
                            // Skip devices that fail to enumerate
                        }
                    }
                }
                catch
                {
                    // WIA subsystem error or no drivers installed
                }

                return list;
            });
        }

        /// <summary>
        /// Purges legacy temporary scans from previous sessions to prevent PII accumulation in %TEMP%.
        /// </summary>
        public static void CleanupOrphanedTempScans(string currentScanFile = null)
        {
            try
            {
                string tempDir = Path.GetTempPath();
                var files = Directory.GetFiles(tempDir, "SOLUM_SCAN_*.*");
                foreach (var f in files)
                {
                    if (currentScanFile != null && string.Equals(f, currentScanFile, StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        File.Delete(f);
                    }
                    catch { }
                }
            }
            catch { }
        }

        /// <summary>
        /// Acquires a document from the specified or default scanner using WIA silent transfer (Option A).
        /// Calibrated for 8.5 x 13 inch CSWDO application forms.
        /// </summary>
        public static Task<string> ScanDocumentAsync(ScannerSettings settings, Action<string> onStatusUpdate = null)
        {
            return RunInStaAsync(() =>
            {
                onStatusUpdate?.Invoke("Connecting to scanner hardware...");

                Type managerType = Type.GetTypeFromProgID("WIA.DeviceManager");
                if (managerType == null)
                    throw new InvalidOperationException("Windows Image Acquisition (WIA) service is not available on this Windows system.");

                dynamic deviceManager = Activator.CreateInstance(managerType);
                int count = (int)deviceManager.DeviceInfos.Count;

                if (count == 0)
                    throw new InvalidOperationException("No scanner or printer hardware detected. Please ensure your device is powered on and connected via USB or Local Network.");

                dynamic targetInfo = null;

                // 1. Try to match configured DeviceId
                if (!string.IsNullOrWhiteSpace(settings?.DeviceId))
                {
                    for (int i = 1; i <= count; i++)
                    {
                        try
                        {
                            dynamic info = deviceManager.DeviceInfos[i];
                            if (string.Equals(info.DeviceID?.ToString(), settings.DeviceId, StringComparison.OrdinalIgnoreCase))
                            {
                                targetInfo = info;
                                break;
                            }
                        }
                        catch { }
                    }
                }

                // 2. Fallback to first available scanner
                if (targetInfo == null)
                {
                    for (int i = 1; i <= count; i++)
                    {
                        try
                        {
                            dynamic info = deviceManager.DeviceInfos[i];
                            if ((int)info.Type == WIA_DEVICE_TYPE_SCANNER)
                            {
                                targetInfo = info;
                                break;
                            }
                        }
                        catch { }
                    }
                }

                if (targetInfo == null)
                    throw new InvalidOperationException("No compatible scanner found. Please verify that your scanner driver (WIA) is installed and the device is powered on.");

                // Connect to scanner device
                string scannerName = GetPropertyValue(targetInfo.Properties, "Name") ?? "Scanner";
                onStatusUpdate?.Invoke($"Initializing {scannerName}...");
                dynamic device = targetInfo.Connect();

                if (device == null || device.Items.Count < 1)
                    throw new InvalidOperationException("Unable to establish communication with the scanner head.");

                // Set Document Handling (Flatbed vs Feeder) BEFORE configuring item properties
                // so the driver doesn't revert item resolution and extent
                try
                {
                    int handling = settings?.PaperSource == ScanPaperSource.Feeder ? 2 : 1;
                    SetProperty(device.Properties, WIA_DPS_DOCUMENT_HANDLING_SELECT, handling);
                    if (settings?.PaperSource == ScanPaperSource.Feeder)
                    {
                        SetProperty(device.Properties, WIA_DPS_PAGES, 1);
                    }
                }
                catch { }

                dynamic scanItem = device.Items[1];

                // Configure properties: calibrated for 8.5 x 13 CSWDO form with hardware bed clamping
                ConfigureScanItem(scanItem, settings, device);

                onStatusUpdate?.Invoke("Acquiring document scan from hardware...");

                // Determine best supported image format (prefer PNG for lossless sharpness, then BMP, then JPEG)
                string chosenFormat = WIA_FORMAT_PNG;
                string chosenExt = ".png";

                try
                {
                    var supportedFormats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (scanItem.Formats != null)
                    {
                        int fCount = (int)scanItem.Formats.Count;
                        for (int f = 1; f <= fCount; f++)
                        {
                            string fg = scanItem.Formats[f]?.ToString();
                            if (!string.IsNullOrEmpty(fg)) supportedFormats.Add(fg);
                        }
                    }

                    if (supportedFormats.Count > 0)
                    {
                        if (supportedFormats.Contains(WIA_FORMAT_PNG))
                        {
                            chosenFormat = WIA_FORMAT_PNG;
                            chosenExt = ".png";
                        }
                        else if (supportedFormats.Contains(WIA_FORMAT_BMP))
                        {
                            chosenFormat = WIA_FORMAT_BMP;
                            chosenExt = ".bmp";
                        }
                        else if (supportedFormats.Contains(WIA_FORMAT_JPEG))
                        {
                            chosenFormat = WIA_FORMAT_JPEG;
                            chosenExt = ".jpg";
                        }
                    }
                }
                catch { }

                // Execute transfer directly via scanItem.Transfer() (Option A: Silent in-app transfer)
                dynamic imageFile = null;
                try
                {
                    imageFile = scanItem.Transfer(chosenFormat);
                }
                catch (COMException ex)
                {
                    uint hr = (uint)ex.ErrorCode;
                    if (hr == 0x80210064) // WIA_ERROR_CANCELLED
                    {
                        throw new OperationCanceledException("The scan operation was cancelled.", ex);
                    }
                    string friendly = GetFriendlyComErrorMessage(ex);
                    throw new InvalidOperationException(friendly, ex);
                }

                if (imageFile == null)
                    throw new InvalidOperationException("Scanner hardware did not return an image buffer.");

                onStatusUpdate?.Invoke("Saving scanned image...");

                string tempFile = Path.Combine(Path.GetTempPath(), $"SOLUM_SCAN_{Guid.NewGuid():N}{chosenExt}");
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }

                imageFile.SaveFile(tempFile);

                if (!File.Exists(tempFile) || new FileInfo(tempFile).Length == 0)
                    throw new InvalidOperationException("Scanner returned an empty image file.");

                return tempFile;
            });
        }

        private static void ConfigureScanItem(dynamic scanItem, ScannerSettings settings, dynamic device)
        {
            if (scanItem == null || scanItem.Properties == null) return;

            int dpi = settings?.Dpi ?? 200;
            if (dpi <= 0) dpi = 200;

#pragma warning disable CS0618
            ScanColorMode colorMode = settings?.ColorMode ?? ScanColorMode.Color;
            if (colorMode == ScanColorMode.BlackAndWhite) colorMode = ScanColorMode.Grayscale;
#pragma warning restore CS0618

            // 1. Set Color Mode / Intent
            // 1 = Color, 2 = Grayscale
            int intentValue = colorMode == ScanColorMode.Grayscale ? 2 : 1;
            SetProperty(scanItem.Properties, WIA_SCAN_COLOR_MODE, intentValue);

            // 2. Set Data Type & Bit Depth
            // 4103 = WIA_IPA_DATATYPE: 2 = Grayscale, 3 = Color (1 is Dither - avoid!)
            // 4104 = WIA_IPA_DEPTH: 8 (Gray), 24 (Color)
            int dataType = colorMode == ScanColorMode.Grayscale ? 2 : 3;
            int bitDepth = colorMode == ScanColorMode.Grayscale ? 8 : 24;
            SetProperty(scanItem.Properties, 4103, dataType);
            SetProperty(scanItem.Properties, 4104, bitDepth);

            // 3. Set Resolution DPI (Horizontal and Vertical)
            SetProperty(scanItem.Properties, WIA_HORIZONTAL_SCAN_RESOLUTION_DPI, dpi);
            SetProperty(scanItem.Properties, WIA_VERTICAL_SCAN_RESOLUTION_DPI, dpi);

            // 4. Set Start Position X/Y to 0
            SetProperty(scanItem.Properties, WIA_HORIZONTAL_SCAN_START_PIXEL, 0);
            SetProperty(scanItem.Properties, WIA_VERTICAL_SCAN_START_PIXEL, 0);

            // 5. Calculate Extent in Pixels for 8.5 x 13 inch CSWDO Form
            // Check hardware bed size limits if flatbed glass is smaller than 13"
            double targetWidthInches = 8.5;
            double targetHeightInches = 13.0;

            if (settings?.PaperSource != ScanPaperSource.Feeder && device != null)
            {
                try
                {
                    string bedHStr = GetPropertyById(device.Properties, WIA_DPS_VERTICAL_BED_SIZE);
                    if (int.TryParse(bedHStr, out int bedHMilli) && bedHMilli > 0)
                    {
                        double bedH = bedHMilli / 1000.0;
                        if (bedH > 0 && bedH < targetHeightInches)
                        {
                            targetHeightInches = bedH;
                        }
                    }
                }
                catch { }
            }

            int widthPixels = (int)Math.Round(targetWidthInches * dpi);
            int heightPixels = (int)Math.Round(targetHeightInches * dpi);

            SetProperty(scanItem.Properties, WIA_HORIZONTAL_SCAN_SIZE_PIXELS, widthPixels);
            SetProperty(scanItem.Properties, WIA_VERTICAL_SCAN_SIZE_PIXELS, heightPixels);
        }

        private static void SetProperty(dynamic properties, int propertyId, object value)
        {
            if (properties == null) return;
            try
            {
                foreach (dynamic prop in properties)
                {
                    if ((int)prop.PropertyID == propertyId)
                    {
                        prop.Value = value;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WiaScannerService] Could not set property {propertyId}: {ex.Message}");
            }
        }

        private static string GetPropertyValue(dynamic properties, string propertyName)
        {
            if (properties == null) return null;
            try
            {
                foreach (dynamic prop in properties)
                {
                    if (string.Equals(prop.Name?.ToString(), propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        return prop.get_Value()?.ToString();
                    }
                }
            }
            catch { }
            return null;
        }

        private static string GetPropertyById(dynamic properties, int propertyId)
        {
            if (properties == null) return null;
            try
            {
                foreach (dynamic prop in properties)
                {
                    if ((int)prop.PropertyID == propertyId)
                    {
                        return prop.get_Value()?.ToString();
                    }
                }
            }
            catch { }
            return null;
        }

        private static string GetFriendlyComErrorMessage(COMException ex)
        {
            uint hr = (uint)ex.ErrorCode;

            switch (hr)
            {
                case 0x80210001: // WIA_ERROR_GENERAL
                    return "A general hardware error occurred on the scanner. Please verify the connection and power.";
                case 0x80210002: // WIA_ERROR_PAPER_JAM
                    return "Paper jam detected in the scanner. Please clear the paper path.";
                case 0x80210003: // WIA_ERROR_PAPER_EMPTY
                    return "The document feeder tray is empty. Please place the CSWDO form in the feeder or on the flatbed glass.";
                case 0x80210004: // WIA_ERROR_PAPER_PROBLEM
                    return "An issue was detected with document feeding. Please check the alignment in the feeder tray.";
                case 0x80210005: // WIA_ERROR_OFFLINE
                    return "The scanner is currently offline or in sleep mode. Please wake or turn on the device.";
                case 0x80210006: // WIA_ERROR_BUSY
                    return "The scanner is currently busy with another operation. Please wait a moment and try again.";
                case 0x80210007: // WIA_ERROR_WARMING_UP
                    return "The scanner lamp is currently warming up. Please try again in a few seconds.";
                case 0x80210008: // WIA_ERROR_USER_INTERVENTION
                    return "The scanner requires manual intervention (e.g. lid open or cover ajar).";
                case 0x8021000A: // WIA_ERROR_DEVICE_COMMUNICATION
                    return "Lost communication with scanner hardware. Please verify the USB or network cable.";
                case 0x80210015: // WIA_S_NO_DEVICE_AVAILABLE
                    return "No scanner hardware was detected. Please ensure your device is powered on and connected.";
                case 0x80210016: // WIA_ERROR_COVER_OPEN
                    return "The scanner document cover or lid is open. Please close it and try again.";
                case 0x80210064: // WIA_ERROR_CANCELLED
                    return "The scan operation was cancelled.";
                default:
                    return $"Scanner communication failed (Error: 0x{hr:X8}): {ex.Message}";
            }
        }

        /// <summary>
        /// Executes an action within a dedicated Single-Threaded Apartment (STA) thread for COM compatibility.
        /// </summary>
        private static Task<T> RunInStaAsync<T>(Func<T> action)
        {
            var tcs = new TaskCompletionSource<T>();
            var thread = new Thread(() =>
            {
                try
                {
                    tcs.SetResult(action());
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            return tcs.Task;
        }
    }
}
