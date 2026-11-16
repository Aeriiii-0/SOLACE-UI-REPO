using System;

namespace SOLUM_UI.DTOs.Archive
{
    public class RecycleBinItemDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Barangay { get; set; } = string.Empty;
        public string PhilsysCardNumber { get; set; }
        public DateTime DeletionRequestedAt { get; set; }
        public DateTime ReinstatementDeadline { get; set; }
        public int DaysRemaining { get; set; }
        public string DeletionReason { get; set; } = string.Empty;
    }
}
