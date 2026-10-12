using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SOLUM_UI.Models.Api
{
    /// <summary>
    /// Mirrors the API audit log document structure.
    /// Supports both root-level properties (from recent API specs)
    /// and nested Metadata/PerformedBy/Target structures for backward compatibility.
    /// </summary>
    public class AuditLogDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("actionType")]
        public string ActionType { get; set; }

        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        // Root-level properties (API contract)
        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("targetType")]
        public string TargetType { get; set; }

        [JsonProperty("targetId")]
        public string TargetId { get; set; }

        [JsonProperty("targetName")]
        public string TargetName { get; set; }

        [JsonProperty("ipAddress")]
        public string IpAddress { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("changes")]
        public List<ChangeItemDto> Changes { get; set; } = new List<ChangeItemDto>();

        // Nested objects (backward compatibility)
        [JsonProperty("metadata")]
        public AuditLogMetadata Metadata { get; set; }

        [JsonProperty("performedBy")]
        public AuditLogActor PerformedBy { get; set; }

        [JsonProperty("target")]
        public AuditLogTarget Target { get; set; }
    }

    /// <summary>
    /// A single field-level change as returned by the API.
    /// Supports both "field" and "fieldName" JSON keys.
    /// </summary>
    public class ChangeItemDto
    {
        private string _fieldName;

        [JsonProperty("field")]
        public string Field
        {
            get => _fieldName;
            set => _fieldName = value;
        }

        [JsonProperty("fieldName")]
        public string FieldName
        {
            get => _fieldName;
            set => _fieldName = value;
        }

        [JsonProperty("oldValue")]
        public string OldValue { get; set; }

        [JsonProperty("newValue")]
        public string NewValue { get; set; }
    }

    public class AuditLogMetadata
    {
        [JsonProperty("ipAddress")]
        public string IpAddress { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        // Legacy single-field change data
        [JsonProperty("fieldChanged")]
        public string FieldChanged { get; set; }

        [JsonProperty("oldValue")]
        public string OldValue { get; set; }

        [JsonProperty("newValue")]
        public string NewValue { get; set; }

        // Multi-field change list
        [JsonProperty("changes")]
        public List<ChangeItemDto> Changes { get; set; } = new List<ChangeItemDto>();
    }

    public class AuditLogActor
    {
        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }
    }

    public class AuditLogTarget
    {
        [JsonProperty("targetId")]
        public string TargetId { get; set; }

        [JsonProperty("targetType")]
        public string TargetType { get; set; }

        [JsonProperty("targetName")]
        public string TargetName { get; set; }
    }

    /// <summary>
    /// Mirrors PagedAuditLogsResponse from the API.
    /// </summary>
    public class PagedAuditLogsResponse
    {
        public List<AuditLogDto> Items { get; set; } = new List<AuditLogDto>();
        public DateTimeOffset? NextCursorTimestamp { get; set; }
        public string NextCursorId { get; set; }
        public bool HasMore { get; set; }
    }
}
