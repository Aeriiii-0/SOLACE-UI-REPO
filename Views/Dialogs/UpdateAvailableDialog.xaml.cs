using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using AutoUpdaterDotNET;

namespace SOLUM_UI.Views.Dialogs
{
    public partial class UpdateAvailableDialog : Window
    {
        private readonly UpdateInfoEventArgs _args;
        private WebClient _webClient;
        private string _downloadedFilePath;
        private bool _isUpdateInitiated = false;

        public UpdateAvailableDialog(UpdateInfoEventArgs args)
        {
            InitializeComponent();
            _args = args ?? throw new ArgumentNullException(nameof(args));
            // Populate version comparison
            TxtInstalledVersion.Text = $"v{_args.InstalledVersion}";
            TxtLatestVersion.Text = $"v{_args.CurrentVersion}";
            // Fetch changelog text
            LoadChangelogAsync();
        }

        private async void LoadChangelogAsync()
        {
            if (string.IsNullOrWhiteSpace(_args.ChangelogURL))
            {
                TxtChangelog.Text = "No release notes available for this version.";
                return;
            }
            try
            {
                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "SOLUM-UI-Updater");
                    string html = await client.DownloadStringTaskAsync(_args.ChangelogURL);
                    // Convert basic HTML to readable plain text for the release notes card
                    string clean = Regex.Replace(html, "<style.*?</style>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    clean = Regex.Replace(clean, @"<br\s*/?>", Environment.NewLine, RegexOptions.IgnoreCase);
                    clean = Regex.Replace(clean, @"</li>", Environment.NewLine, RegexOptions.IgnoreCase);
                    clean = Regex.Replace(clean, @"<li[^>]*>", " • ", RegexOptions.IgnoreCase);
                    clean = Regex.Replace(clean, @"<h\d[^>]*>(.*?)</h\d>", "$1" + Environment.NewLine, RegexOptions.IgnoreCase);
                    clean = Regex.Replace(clean, "<.*?>", string.Empty);
                    clean = WebUtility.HtmlDecode(clean).Trim();
                    TxtChangelog.Text = !string.IsNullOrWhiteSpace(clean) ? clean : "Performance improvements and bug fixes.";
                }
            }
            catch
            {
                TxtChangelog.Text = "SOLUM UI update with recent improvements and fixes.";
            }
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            ExitApplication();
        }

        private void BtnExitApp_Click(object sender, RoutedEventArgs e)
        {
            ExitApplication();
        }

        private void ExitApplication()
        {
            if (_webClient != null && _webClient.IsBusy)
            {
                _webClient.CancelAsync();
            }
            CleanTempFile();
            Application.Current.Shutdown();
            Environment.Exit(0);
        }

        private void BtnDownloadNow_Click(object sender, RoutedEventArgs e)
        {
            // Transition to Custom Downloading UI
            PnlPrompt.Visibility = Visibility.Collapsed;
            PnlDownloading.Visibility = Visibility.Visible;
            StartDownload();
        }

        private void StartDownload()
        {
            if (string.IsNullOrWhiteSpace(_args.DownloadURL))
            {
                MessageBox.Show("Unable to find download package for this update. SOLUM UI will now exit.", "Update Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                ExitApplication();
                return;
            }
            try
            {
                string extension = Path.GetExtension(new Uri(_args.DownloadURL).AbsolutePath);
                if (string.IsNullOrEmpty(extension)) extension = ".zip";
                _downloadedFilePath = Path.Combine(Path.GetTempPath(), $"SOLUM_Update_{Guid.NewGuid():N}{extension}");
                _webClient = new WebClient();
                _webClient.Headers.Add("User-Agent", "SOLUM-UI-Updater");
                _webClient.DownloadProgressChanged += OnDownloadProgressChanged;
                _webClient.DownloadFileCompleted += OnDownloadFileCompleted;
                TxtDownloadStatus.Text = "Contacting server and downloading update package...";
                _webClient.DownloadFileAsync(new Uri(_args.DownloadURL), _downloadedFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to begin download: {ex.Message}. SOLUM UI will now exit.", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ExitApplication();
            }
        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateProgressBar.Value = e.ProgressPercentage;
                TxtProgressPercent.Text = $"{e.ProgressPercentage}%";

                double receivedMb = e.BytesReceived / (1024.0 * 1024.0);
                double totalMb = e.TotalBytesToReceive / (1024.0 * 1024.0);
                if (totalMb > 0)
                {
                    TxtProgressBytes.Text = $"{receivedMb:N1} MB / {totalMb:N1} MB";
                }
                else
                {
                    TxtProgressBytes.Text = $"{receivedMb:N1} MB";
                }
                TxtDownloadStatus.Text = $"Downloading update from GitHub... ({e.ProgressPercentage}%)";
            });
        }

        private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.Cancelled)
                {
                    CleanTempFile();
                    return;
                }
                if (e.Error != null)
                {
                    var retry = MessageBox.Show(
                        $"Failed to download the mandatory update: {e.Error.Message}\n\nWould you like to retry the download?\n(Selecting 'No' will exit SOLUM UI)",
                        "Update Error",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Error);
                    if (retry == MessageBoxResult.Yes)
                    {
                        StartDownload();
                    }
                    else
                    {
                        ExitApplication();
                    }
                    return;
                }
                // Download completed successfully
                TxtDownloadStatus.Text = "Download complete! Applying update and restarting SOLUM UI...";
                UpdateProgressBar.Value = 100;
                TxtProgressPercent.Text = "100%";
                BtnCancelDownload.IsEnabled = false;
                ApplyUpdateAndRestart();
            });
        }

        private void ApplyUpdateAndRestart()
        {
            try
            {
                _isUpdateInitiated = true;
                string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
                var currentProc = Process.GetCurrentProcess();
                string exePath = currentProc.MainModule.FileName;
                int currentPid = currentProc.Id;
                string extension = Path.GetExtension(_downloadedFilePath).ToLowerInvariant();
                if (extension == ".zip")
                {
                    // Create an atomic background apply-and-restart batch script that waits for PID exit
                    string batPath = Path.Combine(Path.GetTempPath(), $"solum_apply_{Guid.NewGuid():N}.bat");
                    string batContent = $@"@echo off
setlocal
set ""TARGET_PID={currentPid}""
:: Wait for application process to completely terminate and release file locks
:wait_loop
tasklist /fi ""PID eq %TARGET_PID%"" 2>nul | findstr /i ""%TARGET_PID%"" >nul
if not errorlevel 1 (
    timeout /t 1 /nobreak >nul
    goto wait_loop
)
timeout /t 1 /nobreak >nul
:: Extract update package with retry in case DLL locks linger
set RETRY_COUNT=0
:extract_loop
powershell -NoProfile -ExecutionPolicy Bypass -Command ""Expand-Archive -Path '{_downloadedFilePath}' -DestinationPath '{appDir}' -Force""
if %ERRORLEVEL% neq 0 (
    set /a RETRY_COUNT+=1
    if %RETRY_COUNT% leq 5 (
        timeout /t 1 /nobreak >nul
        goto extract_loop
    )
)
:: Relaunch updated application
start """" ""{exePath}""
del ""{_downloadedFilePath}"" 2>nul
del ""%~f0"" 2>nul
";
                    File.WriteAllText(batPath, batContent);
                    var psi = new ProcessStartInfo
                    {
                        FileName = batPath,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    Process.Start(psi);
                }
                else
                {
                    // If it is an installer (.exe / .msi)
                    var psi = new ProcessStartInfo
                    {
                        FileName = _downloadedFilePath,
                        Arguments = !string.IsNullOrEmpty(_args.InstallerArgs) ? _args.InstallerArgs : "/SILENT",
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                // Shutdown cleanly to release file locks on the running executable and DLLs
                Application.Current.Shutdown();
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not apply update: {ex.Message}. SOLUM UI will now exit.", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ExitApplication();
            }
        }

        private void BtnCancelDownload_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Cancelling the update will close SOLUM UI, as this update is mandatory to continue.\n\nAre you sure you want to exit?",
                "Mandatory Update",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                ExitApplication();
            }
        }

        private void CleanTempFile()
        {
            try
            {
                if (!string.IsNullOrEmpty(_downloadedFilePath) && File.Exists(_downloadedFilePath))
                {
                    File.Delete(_downloadedFilePath);
                }
            }
            catch { }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_isUpdateInitiated)
            {
                // Prevent bypassing the mandatory update dialog via Alt+F4 or window closing
                ExitApplication();
            }
            base.OnClosing(e);
        }
    }
}