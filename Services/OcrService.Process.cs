using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace SOLUM_UI.Services
{
    public static partial class OcrService
    {
        private static Process _serverProcess;
        private static readonly SemaphoreSlim _warmupLock = new SemaphoreSlim(1, 1);
        private static Task<bool> _ongoingWarmupTask = null;

        private static void ReportWarmup(string stage, string detail, int percent, double elapsed, double remaining, bool isReady = false, bool hasFailed = false, string error = null)
        {
            var p = new OcrWarmupProgress
            {
                Stage = stage,
                Detail = detail,
                Percent = Math.Max(0, Math.Min(100, percent)),
                ElapsedSeconds = Math.Round(elapsed, 1),
                EstimatedSecondsRemaining = Math.Max(0, Math.Round(remaining, 1)),
                IsReady = isReady,
                HasFailed = hasFailed,
                ErrorMessage = error
            };
            _latestProgress = p;
            try
            {
                WarmupProgressChanged?.Invoke(p);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[OcrService] WarmupProgressChanged handler exception: " + ex.Message);
            }
        }

        public static async Task<bool> IsServerOnlineAsync()
        {
            try
            {
                using (var response = await _http.GetAsync(ServerUrl + "/health"))
                {
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void EnsureEngineProvisioned()
        {
            try
            {
                string localAppDataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SOLUM", "OcrEngine"
                );

                if (Directory.Exists(localAppDataDir) &&
                    (File.Exists(Path.Combine(localAppDataDir, "ocr_server.exe")) || File.Exists(Path.Combine(localAppDataDir, "server.py"))))
                {
                    return; // Already set up!
                }

                // Check for bundled zip archives in the app folder
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] zipCandidates = new[]
                {
                    Path.Combine(baseDir, "OcrEngine.zip"),
                    Path.Combine(baseDir, "OcrService.zip"),
                    Path.Combine(baseDir, "ocr_runtime.zip"),
                    Path.Combine(baseDir, "ocr_package.zip")
                };

                foreach (var zip in zipCandidates)
                {
                    if (File.Exists(zip))
                    {
                        Directory.CreateDirectory(localAppDataDir);
                        ZipFile.ExtractToDirectory(zip, localAppDataDir);
                        Debug.WriteLine("[OcrService] Successfully unpacked bundled engine to " + localAppDataDir);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[OcrService] Engine provisioning note: " + ex.Message);
            }
        }

        public static async Task<bool> EnsureServerRunningAsync(int timeoutSeconds = 25)
        {
            if (await IsServerOnlineAsync())
            {
                ReportWarmup("OCR Engine Ready", "Local OpenVINO AI service is active and responsive.", 100, 0, 0, isReady: true);
                return true;
            }

            await _warmupLock.WaitAsync();
            try
            {
                if (await IsServerOnlineAsync())
                {
                    ReportWarmup("OCR Engine Ready", "Local OpenVINO AI service is active and responsive.", 100, 0, 0, isReady: true);
                    return true;
                }

                if (_ongoingWarmupTask != null && !_ongoingWarmupTask.IsCompleted)
                {
                    return await _ongoingWarmupTask;
                }

                _ongoingWarmupTask = RunWarmupCoreAsync(timeoutSeconds);
                return await _ongoingWarmupTask;
            }
            finally
            {
                _warmupLock.Release();
            }
        }

        private static async Task<bool> RunWarmupCoreAsync(int timeoutSeconds)
        {
            var sw = Stopwatch.StartNew();
            const double targetExpectedDuration = 7.0;

            try
            {
                ReportWarmup(
                    "Checking Engine Components",
                    "Verifying local AI runtime and model files...",
                    15,
                    sw.Elapsed.TotalSeconds,
                    targetExpectedDuration
                );

                EnsureEngineProvisioned();

                ReportWarmup(
                    "Launching AI Service",
                    "Starting background Python / OpenVINO process...",
                    25,
                    sw.Elapsed.TotalSeconds,
                    8.0
                );

                var startResult = StartLocalServerProcess();
                if (!startResult.Success)
                {
                    sw.Stop();
                    ReportWarmup(
                        "OCR Engine Not Found",
                        startResult.ErrorMessage,
                        0,
                        sw.Elapsed.TotalSeconds,
                        0,
                        hasFailed: true,
                        error: startResult.ErrorMessage
                    );
                    return false;
                }

                int iterations = Math.Max(10, timeoutSeconds * 2);
                double totalTimeout = (double)timeoutSeconds;
                double normalBootEstimate = 8.0;

                for (int i = 0; i < iterations; i++)
                {
                    await Task.Delay(500);

                    double elapsed = sw.Elapsed.TotalSeconds;

                    // 1. Check if process terminated prematurely
                    if (_serverProcess != null && _serverProcess.HasExited)
                    {
                        sw.Stop();
                        string err = "";
                        try { err = _serverProcess.StandardError.ReadToEnd(); } catch { }
                        if (string.IsNullOrWhiteSpace(err))
                            err = $"Process terminated unexpectedly with exit code {_serverProcess.ExitCode}.";

                        ReportWarmup(
                            "OCR Engine Stopped",
                            $"OCR server exited: {err.Trim()}",
                            0,
                            elapsed,
                            0,
                            hasFailed: true,
                            error: err
                        );
                        return false;
                    }

                    // 2. Dynamic, accurate countdown
                    double remaining;
                    int pct;
                    if (elapsed < normalBootEstimate)
                    {
                        remaining = Math.Max(1.0, Math.Round(normalBootEstimate - elapsed));
                        pct = 30 + (int)((elapsed / normalBootEstimate) * 60);
                    }
                    else
                    {
                        remaining = Math.Max(1.0, Math.Round(totalTimeout - elapsed));
                        pct = Math.Min(98, 90 + (int)(((elapsed - normalBootEstimate) / Math.Max(1.0, totalTimeout - normalBootEstimate)) * 8));
                    }

                    string stage = elapsed < 3.0
                        ? "Starting AI Service"
                        : "Warming Up Neural Models";

                    string detail = elapsed < 3.0
                        ? "Initializing server runtime environment..."
                        : "Loading OpenVINO DBNet & PaddleOCR models into memory...";

                    ReportWarmup(stage, detail, pct, elapsed, remaining);

                    if (await IsServerOnlineAsync())
                    {
                        sw.Stop();
                        ReportWarmup(
                            "OCR Engine Ready",
                            $"AI models loaded successfully ({sw.Elapsed.TotalSeconds:F1}s). Ready for instant scanning.",
                            100,
                            sw.Elapsed.TotalSeconds,
                            0,
                            isReady: true
                        );
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[OcrService] Warmup exception: " + ex.Message);
            }

            sw.Stop();
            bool finalCheck = await IsServerOnlineAsync();
            if (finalCheck)
            {
                ReportWarmup(
                    "OCR Engine Ready",
                    $"AI models loaded successfully ({sw.Elapsed.TotalSeconds:F1}s). Ready for scanning.",
                    100,
                    sw.Elapsed.TotalSeconds,
                    0,
                    isReady: true
                );
                return true;
            }

            ReportWarmup(
                "OCR Engine Offline",
                "The local engine could not be contacted after " + timeoutSeconds + "s. Click Retry or verify Python setup.",
                0,
                sw.Elapsed.TotalSeconds,
                0,
                hasFailed: true,
                error: "Server did not respond on " + ServerUrl
            );

            return false;
        }

        public static void KillStaleServerProcesses()
        {
            try
            {
                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    _serverProcess.Kill();
                    _serverProcess.Dispose();
                    _serverProcess = null;
                }
            }
            catch { }
        }

        public static (bool Success, string ErrorMessage) StartLocalServerProcess()
        {
            if (_serverProcess != null && !_serverProcess.HasExited) return (true, null);

            string ocrDir = FindOcrDirectory();

            // 1. Check for standalone compiled binary (e.g. PyInstaller distribution)
            string exePath = Path.Combine(ocrDir, "ocr_server.exe");
            if (File.Exists(exePath))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = ocrDir,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    RedirectStandardError = true
                };
                try
                {
                    _serverProcess = Process.Start(psi);
                    return (true, null);
                }
                catch (Exception ex)
                {
                    return (false, $"Failed to launch ocr_server.exe: {ex.Message}");
                }
            }

            // 2. Check for local Python virtual environment, portable runtime, or system Python
            string serverPy = Path.Combine(ocrDir, "server.py");
            if (!File.Exists(serverPy))
            {
                return (false, $"OCR server script 'server.py' was not found in: {ocrDir}");
            }

            string pythonExe = FindPythonExecutable(ocrDir);
            if (string.IsNullOrEmpty(pythonExe) || (!File.Exists(pythonExe) && !IsExecutableOnPath(pythonExe)))
            {
                return (false, "Local Python runtime not found on this machine. OCR requires Python 3.9 - 3.12 or a standalone engine.");
            }

            var pyPsi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{serverPy}\"",
                WorkingDirectory = ocrDir,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            try
            {
                _serverProcess = Process.Start(pyPsi);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Could not start Python ({pythonExe}): {ex.Message}");
            }
        }

        private static bool IsExecutableOnPath(string exe)
        {
            if (string.IsNullOrEmpty(exe)) return false;
            if (File.Exists(exe)) return true;
            string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in pathEnv.Split(';'))
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                try
                {
                    string full = Path.Combine(dir.Trim(), exe);
                    if (File.Exists(full)) return true;
                    if (!exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(full + ".exe")) return true;
                }
                catch { }
            }
            return false;
        }

        public static string FindOcrDirectory()
        {
            // 1. Check user local app data directory (where auto-provisioning unpacks)
            string localAppDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SOLUM", "OcrEngine"
            );
            if (Directory.Exists(localAppDataDir) &&
                (File.Exists(Path.Combine(localAppDataDir, "ocr_server.exe")) || File.Exists(Path.Combine(localAppDataDir, "server.py"))))
            {
                return localAppDataDir;
            }

            // 2. Check AppDomain BaseDirectory and search upwards up to 5 levels
            string current = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 5; i++)
            {
                if (string.IsNullOrEmpty(current)) break;
                string candidate = Path.Combine(current, "OcrService");
                if (Directory.Exists(candidate) && (File.Exists(Path.Combine(candidate, "server.py")) || File.Exists(Path.Combine(candidate, "ocr_server.exe"))))
                {
                    return candidate;
                }
                var parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }

            // 3. Check current working directory and search upwards
            string cwd = Directory.GetCurrentDirectory();
            for (int i = 0; i < 5; i++)
            {
                if (string.IsNullOrEmpty(cwd)) break;
                string candidate = Path.Combine(cwd, "OcrService");
                if (Directory.Exists(candidate) && (File.Exists(Path.Combine(candidate, "server.py")) || File.Exists(Path.Combine(candidate, "ocr_server.exe"))))
                {
                    return candidate;
                }
                var parent = Directory.GetParent(cwd);
                if (parent == null) break;
                cwd = parent.FullName;
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OcrService");
        }

        private static string FindPythonExecutable(string ocrDir)
        {
            // 1. Check embedded/portable python-runtime inside OcrService directory
            string portablePython = Path.Combine(ocrDir, "python-runtime", "python.exe");
            if (File.Exists(portablePython)) return portablePython;

            // 2. Check venv inside OcrService directory
            string venvPython = Path.Combine(ocrDir, "venv", "Scripts", "python.exe");
            if (File.Exists(venvPython)) return venvPython;

            // 3. Check direct python.exe in ocrDir
            string directPython = Path.Combine(ocrDir, "python.exe");
            if (File.Exists(directPython)) return directPython;

            // 4. Check user local app data installations (default Windows python.org installer for non-admin users)
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] userPythonVersions = new[] { "Python313", "Python312", "Python311", "Python310", "Python39" };
            foreach (var ver in userPythonVersions)
            {
                string p = Path.Combine(localAppData, "Programs", "Python", ver, "python.exe");
                if (File.Exists(p)) return p;
            }

            // 5. Check common machine-wide Python installation paths
            string[] knownPaths = new[]
            {
                @"C:\Python312\python.exe",
                @"C:\Program Files\Python312\python.exe",
                @"C:\Python311\python.exe",
                @"C:\Program Files\Python311\python.exe",
                @"C:\Python310\python.exe",
                @"C:\Program Files\Python310\python.exe",
                @"C:\Python39\python.exe",
                @"C:\Program Files\Python39\python.exe"
            };

            foreach (var p in knownPaths)
            {
                if (File.Exists(p)) return p;
            }

            // 6. Check Windows Python launcher
            string pyLauncher = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "py.exe");
            if (File.Exists(pyLauncher)) return pyLauncher;

            if (IsExecutableOnPath("python.exe")) return "python.exe";
            if (IsExecutableOnPath("python")) return "python";

            return null;
        }

        public static void StopLocalServerProcess()
        {
            try
            {
                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    _serverProcess.Kill();
                    _serverProcess.Dispose();
                    _serverProcess = null;
                }
            }
            catch { }
        }
    }
}
