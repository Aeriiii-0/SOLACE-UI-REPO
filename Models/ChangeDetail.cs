using System;

namespace SOLUM_UI.Models
{
    /// <summary>
    /// Represents a single field-level change within an audit log entry.
    /// Tracks the field name, previous value, and new value.
    /// </summary>
    public class ChangeDetail
    {
        /// <summary>Human-readable field name (e.g., "First Name", "Civil Status")</summary>
        public string FieldName { get; set; }

        /// <summary>Previous value before the change</summary>
        public string OldValue { get; set; }

        /// <summary>New value after the change</summary>
        public string NewValue { get; set; }

        /// <summary>Timestamp when this change was recorded</summary>
        public DateTime ChangedAt { get; set; }

        /// <summary>Display-friendly representation: "Field: OldValue → NewValue"</summary>
        public string Display =>
            $"{FieldName}: \"{OldValue ?? "—"}\" → \"{NewValue ?? "—"}\"";

        public ChangeDetail() { }

        public ChangeDetail(string fieldName, string oldValue, string newValue)
        {
            FieldName = fieldName;
            OldValue = oldValue;
            NewValue = newValue;
            ChangedAt = DateTime.Now;
        }
    }
}
