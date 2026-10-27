using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SOLUM_UI.Models;

namespace SOLUM_UI.Services.Interfaces
{
    public interface IOcrService
    {
        string ServerUrl { get; set; }
        OcrWarmupProgress LatestWarmupProgress { get; }
        event Action<OcrWarmupProgress> WarmupProgressChanged;

        Task<bool> IsServerOnlineAsync();
        Task<bool> EnsureServerRunningAsync(int timeoutSeconds);
        void EnsureEngineProvisioned();
        Task<OcrFormResponse> ExtractFormAsync(string imagePath, string mode = "global", CancellationToken ct = default);
        Task<OcrRoiResult> ExtractRoiAsync(string imagePath, double[] bboxNorm, string fieldType = "text", CancellationToken ct = default);
        Dictionary<string, double[]> GetTemplateBoundingBoxes();
        void SaveTemplateBoundingBoxes(Dictionary<string, double[]> boxes);

        string CleanPersonName(string raw);
        string CleanAgeString(string raw);
        string FormatPhoneNumber(string raw);
        string FuzzyMatchExtension(string raw);
        string FuzzyMatchCivilStatus(string raw);
        string FuzzyMatchSex(string raw);
        string FuzzyMatchEducation(string raw);
        string FuzzyMatchReligion(string raw);
        string FuzzyMatchIncome(string raw);
        string FuzzyMatchBarangay(string raw);
        string FuzzyMatchRelationship(string raw);
        bool TryParseOcrDate(string raw, out DateTime date);
        string NormalizeDateString(string raw, out DateTime? date);
        int CalculateAge(DateTime birthdate);
        bool IsOcrNotApplicable(string val);
    }
}
