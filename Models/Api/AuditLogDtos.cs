using System;
using System.Collections.Generic;

namespace SOLUM_UI.Models.Api
{
    /// <summary>
    /// Mirrors the actual API audit log document structure.
    /// </summary>
    public class AuditLogDto
    {
        public string          ActionType  { get; set; }
        public AuditLogMetadata  Metadata  { get; set; }
        public AuditLogActor   PerformedBy { get; set; }
        public AuditLogTarget  Target      { get; set; }
        public DateTimeOffset  Timestamp   { get; set; }
    }

    /// <summary>
    /// A single field-level change as returned by the API in metadata.changes[].
    /// </summary>
    public class ChangeItemDto
    {
        public string FieldName { get; set; }
        public string OldValue  { get; set; }
        public string NewValue  { get; set; }
    }

    public class AuditLogMetadata
    {
        public string IpAddress    { get; set; }
        public string Status       { get; set; }
        // Legacy single-field change data (kept for backward compatibility)
        public string FieldChanged { get; set; }
        public string OldValue     { get; set; }
        public string NewValue     { get; set; }
        // Multi-field change list returned by the API for UPDATE actions
        public List<ChangeItemDto> Changes { get; set; } = new List<ChangeItemDto>();
    }

    public class AuditLogActor
    {
        public string UserId { get; set; }
        public string Role   { get; set; }
    }

    public class AuditLogTarget
    {
        public string TargetId   { get; set; }
        public string TargetType { get; set; }
        public string TargetName { get; set; }   // may be present
    }

    /// <summary>
    /// Mirrors PagedAuditLogsResponse from the API.
    /// </summary>
    public class PagedAuditLogsResponse
    {
        public List<AuditLogDto> Items               { get; set; } = new List<AuditLogDto>();
        public DateTimeOffset?   NextCursorTimestamp  { get; set; }
        public string            NextCursorId         { get; set; }
        public bool              HasMore              { get; set; }
    }
}
