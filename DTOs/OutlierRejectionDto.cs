using System;

namespace SOLUM_UI.DTOs
{
    public class OutlierRejectionDto
    {
        public string SpId { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
        public bool AutoPromoteWaitlist { get; set; } = true;
        public string ReviewerUserName { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
