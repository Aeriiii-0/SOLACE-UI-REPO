using System;

namespace SOLUM_UI.DTOs.Archive
{
    public class LifecyclePipelineResultDto
    {
        public int ProcessedDeletions { get; set; }
        public int ProcessedInactives { get; set; }
        public int ProcessedShredded { get; set; }
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        public string Message { get; set; } = string.Empty;
    }
}
