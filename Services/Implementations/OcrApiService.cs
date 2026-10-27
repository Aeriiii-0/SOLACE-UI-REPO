using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SOLUM_UI.Models;
using SOLUM_UI.Services.Interfaces;

namespace SOLUM_UI.Services.Implementations
{
    public class OcrApiService : IOcrService
    {
        private static readonly Lazy<OcrApiService> _instance = new Lazy<OcrApiService>(() => new OcrApiService());
        public static OcrApiService Instance => _instance.Value;

        public string ServerUrl
        {
            get => OcrService.ServerUrl;
            set => OcrService.ServerUrl = value;
        }

        public OcrWarmupProgress LatestWarmupProgress => OcrService.LatestWarmupProgress;

        public event Action<OcrWarmupProgress> WarmupProgressChanged
        {
            add => OcrService.WarmupProgressChanged += value;
            remove => OcrService.WarmupProgressChanged -= value;
        }

        public Task<bool> IsServerOnlineAsync() => OcrService.IsServerOnlineAsync();

        public Task<bool> EnsureServerRunningAsync(int timeoutSeconds) => OcrService.EnsureServerRunningAsync(timeoutSeconds);

        public void EnsureEngineProvisioned() => OcrService.EnsureEngineProvisioned();

        public Task<OcrFormResponse> ExtractFormAsync(string imagePath, string mode = "global", CancellationToken ct = default)
            => OcrService.ExtractFormAsync(imagePath, mode);

        public Task<OcrRoiResult> ExtractRoiAsync(string imagePath, double[] bboxNorm, string fieldType = "text", CancellationToken ct = default)
        {
            if (bboxNorm != null && bboxNorm.Length >= 4)
            {
                return OcrService.ExtractRoiAsync(imagePath, bboxNorm[0], bboxNorm[1], bboxNorm[2], bboxNorm[3]);
            }
            return Task.FromResult<OcrRoiResult>(null);
        }

        public Dictionary<string, double[]> GetTemplateBoundingBoxes() => OcrService.GetTemplateBoundingBoxes();

        public void SaveTemplateBoundingBoxes(Dictionary<string, double[]> boxes) => OcrService.SaveTemplateBoundingBoxes(boxes);

        public string CleanPersonName(string raw) => OcrService.CleanPersonName(raw);

        public string CleanAgeString(string raw) => OcrService.CleanAgeString(raw);

        public string FormatPhoneNumber(string raw) => OcrService.FormatPhoneNumber(raw);

        public string FuzzyMatchExtension(string raw) => OcrService.FuzzyMatchExtension(raw);

        public string FuzzyMatchCivilStatus(string raw) => OcrService.FuzzyMatchCivilStatus(raw);

        public string FuzzyMatchSex(string raw) => OcrService.FuzzyMatchSex(raw);

        public string FuzzyMatchEducation(string raw) => OcrService.FuzzyMatchEducation(raw);

        public string FuzzyMatchReligion(string raw) => OcrService.FuzzyMatchReligion(raw);

        public string FuzzyMatchIncome(string raw) => OcrService.FuzzyMatchIncome(raw);

        public string FuzzyMatchBarangay(string raw) => OcrService.FuzzyMatchBarangay(raw);

        public string FuzzyMatchRelationship(string raw) => OcrService.FuzzyMatchRelationship(raw);

        public bool TryParseOcrDate(string raw, out DateTime date) => OcrService.TryParseOcrDate(raw, out date);

        public string NormalizeDateString(string raw, out DateTime? date) => OcrService.NormalizeDateString(raw, out date);

        public int CalculateAge(DateTime birthdate) => OcrService.CalculateAge(birthdate);

        public bool IsOcrNotApplicable(string val) => OcrService.IsOcrNotApplicable(val);
    }
}
