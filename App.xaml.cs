using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using AutoUpdaterDotNET;
using SOLUM_UI.Services;
using SOLUM_UI.Views.Dialogs;

namespace SOLUM_UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configure en-PH culture so that all WPF controls (DatePicker, formatting) use form standard MM/dd/yyyy
            var culture = new CultureInfo("en-PH");
            culture.DateTimeFormat.ShortDatePattern = "MM/dd/yyyy";
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

            // Initialize AutoUpdater in background with custom downloading UI
            InitializeAutoUpdater();
        }

        private void InitializeAutoUpdater()
        {
            try
            {
                string enable = ConfigurationManager.AppSettings["EnableAutoUpdate"];
                if (string.Equals(enable, "false", StringComparison.OrdinalIgnoreCase))
                    return;

                string updateUrl = ConfigurationManager.AppSettings["UpdateXmlUrl"];
                if (string.IsNullOrWhiteSpace(updateUrl))
                    return;

                AutoUpdater.ReportErrors = false; // Do not show error alert if internet/server is unreachable
                AutoUpdater.AppCastURL = updateUrl;

                // Hook custom update & downloading interface
                AutoUpdater.CheckForUpdateEvent += AutoUpdater_CheckForUpdateEvent;
                AutoUpdater.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AutoUpdater] Initialization failed: {ex.Message}");
            }
        }

        private static void AutoUpdater_CheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args != null && args.Error == null && args.IsUpdateAvailable)
            {
                Current.Dispatcher.Invoke(() =>
                {
                    var dlg = new UpdateAvailableDialog(args);
                    dlg.ShowDialog();
                });
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
