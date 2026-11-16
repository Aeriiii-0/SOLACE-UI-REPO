using System;

namespace SOLUM_UI.DTOs.Archive
{
    public class ArchivedSoloParentSummaryDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{LastName}, {FirstName}".Trim(',', ' ');
        public string Barangay { get; set; } = string.Empty;
        public string PhilsysCardNumber { get; set; }
        public DateTime? ApplicationDate { get; set; }
        public DateTime? LastTermExpiresAt { get; set; }
        public DateTime DateArchived { get; set; }
        public string ArchiveReason { get; set; } = string.Empty;
        public string ArchivedBy { get; set; } = string.Empty;
        public bool IsShredded { get; set; }
        public bool IsReactivated { get; set; }
        public DateTime ScheduledShredDate => DateArchived.AddYears(10);
    }
}
