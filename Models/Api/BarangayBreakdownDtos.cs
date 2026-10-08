using System;
using System.Collections.Generic;

namespace SOLUM_UI.Models.Api
{
    public class BarangayBreakdownItemDto
    {
        public string BarangayName { get; set; } = string.Empty;
        public int TotalSoloParents { get; set; }
        public double PercentageShare { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
        public int NewRegistrationsCount { get; set; }
        public int RenewalsCount { get; set; }
    }

    public class BarangayAnalyticsResponseDto
    {
        public int GrandTotalSoloParents { get; set; }
        public int TotalActive { get; set; }
        public int TotalInactive { get; set; }
        public int TotalNewRegistrations { get; set; }
        public int TotalRenewals { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;
        public string FilterType { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string FilteredBarangay { get; set; } = "ALL";
        public List<BarangayBreakdownItemDto> Barangays { get; set; } = new List<BarangayBreakdownItemDto>();
    }
}
