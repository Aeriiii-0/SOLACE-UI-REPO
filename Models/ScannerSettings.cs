using System;
using System.IO;
using System.Web.Script.Serialization;

namespace SOLUM_UI.Models
{
    public enum ScanColorMode
    {
        Color = 1,
        Grayscale = 2,
        BlackAndWhite = 4
    }

    public enum ScanPaperSource
    {
        Flatbed = 1,
        Feeder = 2
    }

    public class ScannerSettings
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = "Auto-detect Default";
        public int Dpi { get; set; } = 300;
        public ScanColorMode ColorMode { get; set; } = ScanColorMode.Color;
        public ScanPaperSource PaperSource { get; set; } = ScanPaperSource.Flatbed;
        public string PaperSize { get; set; } = "A4";

        private static string SettingsFilePath
        {
            get
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SOLUM_UI");

                if (!Directory.Exists(folder))
                {
                    try { Directory.CreateDirectory(folder); } catch { }
                }

                return Path.Combine(folder, "scanner_settings.json");
            }
        }

        public static ScannerSettings Load()
        {
            try
            {
                string path = SettingsFilePath;
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var serializer = new JavaScriptSerializer();
                    var loaded = serializer.Deserialize<ScannerSettings>(json);
                    if (loaded != null)
                    {
                        if (loaded.Dpi <= 0) loaded.Dpi = 300;
                        return loaded;
                    }
                }
            }
            catch
            {
                // Fallback to default
            }

            return new ScannerSettings();
        }

        public void Save()
        {
            try
            {
                string path = SettingsFilePath;
                var serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(this);
                File.WriteAllText(path, json);
            }
            catch
            {
                // Ignored if file write fails
            }
        }
    }
}
