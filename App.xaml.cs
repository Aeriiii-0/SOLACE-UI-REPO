using System.Windows;
using SOLUM_UI.Services;

namespace SOLUM_UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Launch local background OCR engine if not already running
            _ = OcrService.EnsureServerRunningAsync();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up background OCR process when application exits
            OcrService.StopLocalServerProcess();
            base.OnExit(e);
        }
    }
}
