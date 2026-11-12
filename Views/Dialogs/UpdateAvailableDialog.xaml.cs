using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
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
                    client.Headers.Add("User-Agent", "Solace-Updater");
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
                TxtChangelog.Text = "Solace update with recent improvements and fixes.";
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

        private static bool IsTrustedDownloadUri(Uri uri)
        {
            if (uri == null || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return false;

            string host = uri.Host ?? string.Empty;
            if (string.Equals(host, "github.com", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string updateXmlUrl = ConfigurationManager.AppSettings["UpdateXmlUrl"];
            if (Uri.TryCreate(updateXmlUrl, UriKind.Absolute, out Uri feedUri) &&
                string.Equals(host, feedUri.Host, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private void StartDownload()
        {
            if (string.IsNullOrWhiteSpace(_args.DownloadURL) ||
                !Uri.TryCreate(_args.DownloadURL, UriKind.Absolute, out Uri downloadUri) ||
                !IsTrustedDownloadUri(downloadUri))
            {
                MessageBox.Show("Untrusted or invalid update download URL. Solace will now exit.", "Update Security Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ExitApplication();
                return;
            }
            try
            {
                string extension = Path.GetExtension(downloadUri.AbsolutePath).ToLowerInvariant();
                if (extension != ".zip" && extension != ".exe" && extension != ".msi") extension = ".zip";
                _downloadedFilePath = Path.Combine(Path.GetTempPath(), $"SOLUM_Update_{Guid.NewGuid():N}{extension}");
                _webClient = new WebClient();
                _webClient.Headers.Add("User-Agent", "Solace-Updater");
                _webClient.DownloadProgressChanged += OnDownloadProgressChanged;
                _webClient.DownloadFileCompleted += OnDownloadFileCompleted;
                TxtDownloadStatus.Text = "Contacting server and downloading update package...";
                _webClient.DownloadFileAsync(downloadUri, _downloadedFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to begin download: {ex.Message}. Solace will now exit.", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        $"Failed to download the mandatory update: {e.Error.Message}\n\nWould you like to retry the download?\n(Selecting 'No' will exit Solace)",
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
                TxtDownloadStatus.Text = "Download complete! Applying update and restarting Solace...";
                UpdateProgressBar.Value = 100;
                TxtProgressPercent.Text = "100%";
                BtnCancelDownload.IsEnabled = false;
                ApplyUpdateAndRestart();
            });
        }

        private bool VerifyDownloadedChecksum()
        {
            if (_args.CheckSum == null || string.IsNullOrWhiteSpace(_args.CheckSum.Value))
                return true;

            string algoName = string.IsNullOrWhiteSpace(_args.CheckSum.HashingAlgorithm)
                ? "SHA256"
                : _args.CheckSum.HashingAlgorithm.Trim().ToUpperInvariant();

            using (HashAlgorithm hasher = algoName == "SHA512" ? (HashAlgorithm)SHA512.Create()
                : algoName == "SHA1" ? (HashAlgorithm)SHA1.Create()
                : algoName == "MD5" ? (HashAlgorithm)MD5.Create()
                : SHA256.Create())
            using (var fs = File.OpenRead(_downloadedFilePath))
            {
                byte[] hash = hasher.ComputeHash(fs);
                string actual = BitConverter.ToString(hash).Replace("-", string.Empty);
                return string.Equals(actual, _args.CheckSum.Value.Trim(), StringComparison.OrdinalIgnoreCase);
            }
        }

        private static void ValidateZipEntries(string zipPath, string destinationDir)
        {
            string fullDestRoot = Path.GetFullPath(destinationDir);
            if (!fullDestRoot.EndsWith(Path.DirectorySeparatorChar.ToString()))
                fullDestRoot += Path.DirectorySeparatorChar;

            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    string fullEntryPath = Path.GetFullPath(Path.Combine(destinationDir, entry.FullName));
                    if (!fullEntryPath.StartsWith(fullDestRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException("Update package contains an unsafe path traversal entry: " + entry.FullName);
                    }
                }
            }
        }

        private void ApplyUpdateAndRestart()
        {
            try
            {
                if (!VerifyDownloadedChecksum())
                {
                    CleanTempFile();
                    MessageBox.Show("Downloaded update failed integrity checksum verification. Solace will now exit.", "Update Security Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ExitApplication();
                    return;
                }

                _isUpdateInitiated = true;
                string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
                var currentProc = Process.GetCurrentProcess();
                string exePath = currentProc.MainModule.FileName;
                int currentPid = currentProc.Id;
                string extension = Path.GetExtension(_downloadedFilePath).ToLowerInvariant();
                if (extension == ".zip")
                {
                    ValidateZipEntries(_downloadedFilePath, appDir);

                    // Create an atomic background apply-and-restart batch script that waits for PID exit
                    string batPath = Path.Combine(Path.GetTempPath(), $"solum_apply_{Guid.NewGuid():N}.bat");
                    string batContent = $@"@echo off
setlocal
set ""TARGET_PID={currentPid}""
set ""SOLUM_ZIP={_downloadedFilePath}""
set ""SOLUM_APPDIR={appDir}""
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
powershell -NoProfile -ExecutionPolicy Bypass -Command ""Expand-Archive -LiteralPath $env:SOLUM_ZIP -DestinationPath $env:SOLUM_APPDIR -Force""
if %ERRORLEVEL% neq 0 (
    set /a RETRY_COUNT+=1
    if %RETRY_COUNT% leq 5 (
        timeout /t 1 /nobreak >nul
        goto extract_loop
    )
)
:: Relaunch updated application
start """" ""{exePath}""
del ""%SOLUM_ZIP%"" 2>nul
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
                MessageBox.Show($"Could not apply update: {ex.Message}. Solace will now exit.", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ExitApplication();
            }
        }

        private void BtnCancelDownload_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Cancelling the update will close Solace, as this update is mandatory to continue.\n\nAre you sure you want to exit?",
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