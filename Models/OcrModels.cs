using System;
using System.Collections.Generic;

namespace SOLUM_UI.Models
{
    public class OcrFieldResult
    {
        public string Value { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Rating { get; set; } = "Low";
        public string Category { get; set; } = "General";
        public double[] Bbox { get; set; } = new double[4];
    }

    public class OcrCircumstanceResult
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

    public class OcrFormResponse
    {
        public string Status { get; set; } = "error";
        public string FormId { get; set; } = "CSWDO-066-2";
        public double OverallConfidence { get; set; }
        public string OverallRating { get; set; } = "Low";
        public string PreviewImageBase64 { get; set; } = string.Empty;
        public Dictionary<string, OcrFieldResult> Fields { get; set; } = new Dictionary<string, OcrFieldResult>(StringComparer.OrdinalIgnoreCase);
        public OcrCircumstanceResult Circumstance { get; set; } = new OcrCircumstanceResult();
        public List<FamilyMember> FamilyMembers { get; set; } = new List<FamilyMember>();
    }

    public class OcrRoiResult
    {
        public string Status { get; set; } = "error";
        public string Value { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Rating { get; set; } = "Low";
        public double[] Bbox { get; set; } = new double[4];
    }
}
