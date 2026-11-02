using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using SOLUM_UI.Services;

namespace SOLUM_UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configure en-PH culture so that all WPF controls (DatePicker, formatting) use standard dd/MM/yyyy
            var culture = new CultureInfo("en-PH");
            culture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            culture.DateTimeFormat.DateSeparator = "/";
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

            // Increase connection limit from default (2) to 64 for fast concurrent HTTP calls in .NET Framework
            System.Net.ServicePointManager.DefaultConnectionLimit = 64;
            System.Net.ServicePointManager.SecurityProtocol |=
                System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;
            
            // Launch local background OCR engine if not already running
            _ = OcrService.EnsureServerRunningAsync();

            // Initialize AutoUpdater in background
            InitializeAutoUpdater();
        }

        private void InitializeAutoUpdater()
        {
            try
            {
                string enable = System.Configuration.ConfigurationManager.AppSettings["EnableAutoUpdate"];
                if (string.Equals(enable, "false", System.StringComparison.OrdinalIgnoreCase))
                    return;

                string updateUrl = System.Configuration.ConfigurationManager.AppSettings["UpdateXmlUrl"];
                if (string.IsNullOrWhiteSpace(updateUrl))
                    return;

                AutoUpdaterDotNET.AutoUpdater.ShowRemindLaterButton = true;
                AutoUpdaterDotNET.AutoUpdater.ShowSkipButton = false;
                AutoUpdaterDotNET.AutoUpdater.ReportErrors = false; // Do not show alert if internet/server is unreachable
                AutoUpdaterDotNET.AutoUpdater.AppCastURL = updateUrl;
                AutoUpdaterDotNET.AutoUpdater.Start();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AutoUpdater] Initialization failed: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up background OCR process when application exits
            OcrService.StopLocalServerProcess();
            base.OnExit(e);
        }
    }
}
