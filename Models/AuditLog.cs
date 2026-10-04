using System;

namespace SOLUM_UI.Models
{
    // Kept for backward-compatibility with the local AuditLogService.
    public enum AuditAction
    {
        Create, Update, Delete, View, Login, Logout, System
    }

    public class AuditLog
    {
        public int         Id              { get; set; }
        public AuditAction Action          { get; set; }

        /// <summary>
        /// Raw action-type string from the API (e.g. "LOGIN", "CREATE_SOLO_PARENT").
        /// When set, all display properties use this instead of the Action enum.
        /// </summary>
        public string      RawActionType   { get; set; }

        public string      Description     { get; set; }
        public string      PerformedBy     { get; set; }
        public string      PerformedByRole { get; set; }
        public DateTime    Timestamp       { get; set; }
        public string      RecordId        { get; set; }
        public string      RecordName      { get; set; }
        public string      FieldChanged    { get; set; }
        public string      OldValue        { get; set; }
        public string      NewValue        { get; set; }
        // Metadata from the API
        public string      IpAddress       { get; set; }
        public string      Status          { get; set; }
        public string      TargetType      { get; set; }

        public bool HasChangeDetail =>
            !string.IsNullOrWhiteSpace(FieldChanged) ||
            !string.IsNullOrWhiteSpace(OldValue)     ||
            !string.IsNullOrWhiteSpace(NewValue);

        public string TimeAgo
        {
            get
            {
                var diff = DateTime.Now - Timestamp;
                if (diff.TotalMinutes < 1)  return "Just now";
                if (diff.TotalMinutes < 60) return (int)diff.TotalMinutes + " mins ago";
                if (diff.TotalHours   < 24) return (int)diff.TotalHours   + " hours ago";
                return (int)diff.TotalDays + " days ago";
            }
        }

        public string TimestampFormatted => Timestamp.ToString("MMM dd, yyyy  hh:mm tt");

        // ── Display helpers ────────────────────────────────────────────────────
        // When RawActionType is set (API path), switch on the raw string.
        // Otherwise fall back to the enum (local AuditLogService path).

        public string DotColor
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(RawActionType))
                    return ColorForRaw(RawActionType);

                switch (Action)
                {
                    case AuditAction.Create:  return "#10B981";
                    case AuditAction.Update:  return "#14B8A6";
                    case AuditAction.Delete:  return "#EF4444";
                    case AuditAction.View:    return "#6B7280";
                    case AuditAction.Login:   return "#3B82F6";
                    case AuditAction.Logout:  return "#6B7280";
                    case AuditAction.System:  return "#F59E0B";
                    default:                  return "#6B7280";
                }
            }
        }

        public string DotLabel
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(RawActionType))
                    return LabelForRaw(RawActionType);

                switch (Action)
                {
                    case AuditAction.Create:  return "+";
                    case AuditAction.Update:  return "E";
                    case AuditAction.Delete:  return "X";
                    case AuditAction.View:    return "V";
                    case AuditAction.Login:   return "→";
                    case AuditAction.Logout:  return "←";
                    case AuditAction.System:  return "!";
                    default:                  return "?";
                }
            }
        }

        public string ActionLabel
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(RawActionType))
                    return LabelTextForRaw(RawActionType);

                switch (Action)
                {
                    case AuditAction.Create:  return "Created";
                    case AuditAction.Update:  return "Updated";
                    case AuditAction.Delete:  return "Deleted";
                    case AuditAction.View:    return "Viewed";
                    case AuditAction.Login:   return "Logged In";
                    case AuditAction.Logout:  return "Logged Out";
                    case AuditAction.System:  return "System";
                    default:                  return "Unknown";
                }
            }
        }

        // ── Raw action-type tables ─────────────────────────────────────────────

        private static string ColorForRaw(string raw)
        {
            switch (raw.ToUpperInvariant())
            {
                case "LOGIN":                return "#3B82F6";   // Blue
                case "LOGOUT":               return "#6B7280";   // Gray
                case "REFRESH_TOKEN":        return "#60A5FA";   // Light Blue
                case "CHANGE_PASSWORD":      return "#F59E0B";   // Amber
                case "REGISTER_USER":        return "#10B981";   // Green
                case "REGISTER_ADMIN":       return "#8B5CF6";   // Purple
                case "UPDATE_USER":          return "#14B8A6";   // Teal
                case "DISABLE_USER":         return "#F97316";   // Orange
                case "CREATE_SOLO_PARENT":   return "#10B981";   // Green
                case "UPDATE_SOLO_PARENT":   return "#14B8A6";   // Teal
                case "DELETE_SOLO_PARENT":   return "#EF4444";   // Red
                case "RENEW_SOLO_PARENT":    return "#059669";   // Emerald Green
                default:                     return "#6B7280";   // Gray fallback
            }
        }

        private static string LabelForRaw(string raw)
        {
            switch (raw.ToUpperInvariant())
            {
                case "LOGIN":                return "→";
                case "LOGOUT":               return "←";
                case "REFRESH_TOKEN":        return "↻";
                case "CHANGE_PASSWORD":      return "🔑";
                case "REGISTER_USER":        return "+";
                case "REGISTER_ADMIN":       return "★";
                case "UPDATE_USER":          return "E";
                case "DISABLE_USER":         return "⊘";
                case "CREATE_SOLO_PARENT":   return "+";
                case "UPDATE_SOLO_PARENT":   return "E";
                case "DELETE_SOLO_PARENT":   return "X";
                case "RENEW_SOLO_PARENT":    return "R";
                default:                     return "?";
            }
        }

        private static string LabelTextForRaw(string raw)
        {
            switch (raw.ToUpperInvariant())
            {
                case "LOGIN":                return "Login";
                case "LOGOUT":               return "Logout";
                case "REFRESH_TOKEN":        return "Refresh Token";
                case "CHANGE_PASSWORD":      return "Change Password";
                case "REGISTER_USER":        return "Register User";
                case "REGISTER_ADMIN":       return "Register Admin";
                case "UPDATE_USER":          return "Update User";
                case "DISABLE_USER":         return "Disable User";
                case "CREATE_SOLO_PARENT":   return "Create Record";
                case "UPDATE_SOLO_PARENT":   return "Update Record";
                case "DELETE_SOLO_PARENT":   return "Delete Record";
                case "RENEW_SOLO_PARENT":    return "Renew Record";
                default:                     return raw; // show as-is if unknown
            }
        }
    }
}
