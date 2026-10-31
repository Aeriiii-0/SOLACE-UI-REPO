using System;
using System.Diagnostics;
using System.Net.Http;

namespace SOLUM_UI.Services
{
    public class OcrWarmupProgress
    {
        public string Stage { get; set; } = "Checking Status";
        public string Detail { get; set; } = "Checking AI OCR engine status...";
        public int Percent { get; set; } = 5;
        public double ElapsedSeconds { get; set; } = 0;
        public double EstimatedSecondsRemaining { get; set; } = 6.0;
        public bool IsReady { get; set; } = false;
        public bool HasFailed { get; set; } = false;
        public string ErrorMessage { get; set; } = null;
    }

    /// <summary>
    /// Solace OCR Service Client.
    /// Modularized into partial classes:
    /// - OcrService.Process.cs: Background Python / OpenVINO process lifecycle and engine warmup.
    /// - OcrService.Api.cs: HTTP REST client for full-form and ROI extraction endpoints.
    /// - OcrService.Template.cs: Form coordinate template JSON serialization and persistence.
    /// - OcrService.Formatting.cs: Normalizers, Philippine fuzzy dictionaries, and record mapping.
    /// </summary>
    public static partial class OcrService
    {
        public static string ServerUrl { get; set; } = "http://127.0.0.1:8000";
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };

        public static event Action<OcrWarmupProgress> WarmupProgressChanged;

        private static OcrWarmupProgress _latestProgress = new OcrWarmupProgress
        {
            Stage = "Ready",
            Detail = "AI Engine is ready.",
            Percent = 100,
            IsReady = true
        };

        public static OcrWarmupProgress LatestWarmupProgress => _latestProgress;
    }
}
