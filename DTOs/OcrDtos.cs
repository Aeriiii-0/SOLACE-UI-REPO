using System;
using System.Collections.Generic;

namespace SOLUM_UI.DTOs
{
    public class OcrFieldResultDto
    {
        public string Value { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Rating { get; set; } = "Low";
        public string Category { get; set; } = "General";
        public double[] Bbox { get; set; } = new double[4];
    }

    public class OcrCircumstanceResultDto
    {
        public bool Detected { get; set; } = false;
        public string Code { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public double Confidence { get; set; } = 0.0;
        public string SubfieldCause { get; set; } = string.Empty;
        public string SubfieldDate { get; set; } = string.Empty;
        public string SubfieldDisability { get; set; } = string.Empty;
        public string SubfieldPeriod { get; set; } = string.Empty;
        public string SubfieldStayAbroad { get; set; } = string.Empty;
        public bool SubfieldNullity { get; set; } = false;
        public bool SubfieldAnnulment { get; set; } = false;
    }

    public class OcrFamilyMemberDto
    {
        public string MemberName { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public string Birthdate { get; set; } = string.Empty;
        public string Age { get; set; } = string.Empty;
        public string CivilStatus { get; set; } = string.Empty;
        public string EducationEmployment { get; set; } = string.Empty;
        public string Income { get; set; } = "0";
        public bool SpecialNeeds { get; set; } = false;
        public string DisabilityType { get; set; } = string.Empty;
    }

    public class OcrFormResponseDto
    {
        public string Status { get; set; } = "error";
        public string FormId { get; set; } = "CSWDO-066-2";
        public double OverallConfidence { get; set; }
        public string OverallRating { get; set; } = "Low";
        public double ExecutionTimeSeconds { get; set; }
        public string PreviewImageBase64 { get; set; } = string.Empty;
        public Dictionary<string, OcrFieldResultDto> Fields { get; set; } = new Dictionary<string, OcrFieldResultDto>(StringComparer.OrdinalIgnoreCase);
        public OcrCircumstanceResultDto Circumstance { get; set; } = new OcrCircumstanceResultDto();
        public List<OcrFamilyMemberDto> FamilyMembers { get; set; } = new List<OcrFamilyMemberDto>();
    }

    public class OcrWarmupProgressDto
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

    public class OcrRoiResultDto
    {
        public string Status { get; set; } = "error";
        public string Value { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Rating { get; set; } = "Low";
        public double ExecutionTimeSeconds { get; set; }
        public double[] Bbox { get; set; } = new double[4];
    }
}
