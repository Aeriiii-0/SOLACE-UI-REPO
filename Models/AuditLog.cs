using System;
using System.Collections.Generic;

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

        /// <summary>
        /// Collection of detailed field-level changes for this audit entry.
        /// Used when multiple fields are modified in a single update.
        /// </summary>
        public List<ChangeDetail> Changes { get; set; } = new List<ChangeDetail>();

        public bool HasChangeDetail =>
            !string.IsNullOrWhiteSpace(FieldChanged) ||
            !string.IsNullOrWhiteSpace(OldValue)     ||
            !string.IsNullOrWhiteSpace(NewValue)     ||
            Changes.Count > 0;

        public string ActionTitle
        {
            get => ActionLabel;
            set { }
        }

        public bool HasTargetId => !string.IsNullOrWhiteSpace(RecordId);
        public string TargetIdDisplay
        {
            get => HasTargetId ? RecordId : "—";
            set { }
        }

        public bool HasTargetName => !string.IsNullOrWhiteSpace(RecordName) && !string.Equals(RecordName, RecordId, StringComparison.OrdinalIgnoreCase);

        public bool HasTargetType => !string.IsNullOrWhiteSpace(TargetType);

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
            set { }
        }

        public string TimestampFormatted
        {
            get => Timestamp.ToString("MMM dd, yyyy  hh:mm tt");
            set { }
        }

        public string PerformedByDisplay
        {
            get => !string.IsNullOrWhiteSpace(PerformedBy) ? PerformedBy : "System";
            set { }
        }

        public string DateDisplay
        {
            get => $"{TimestampFormatted} ({TimeAgo})";
            set { }
        }

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
                    case AuditAction.Create:  return "Create Record";
                    case AuditAction.Update:  return "Update Record";
                    case AuditAction.Delete:  return "Delete Record";
                    case AuditAction.View:    return "View Record";
                    case AuditAction.Login:   return "Login";
                    case AuditAction.Logout:  return "Logout";
                    case AuditAction.System:  return "System Action";
                    default:                  return "Activity";
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
                case "CREATE_SOLO_PARENT":   return "Create Solo Parent";
                case "UPDATE_SOLO_PARENT":   return "Update Solo Parent";
                case "DELETE_SOLO_PARENT":   return "Delete Solo Parent";
                case "RENEW_SOLO_PARENT":    return "Renew Solo Parent";
                default:
                    return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                        raw.Replace('_', ' ').ToLowerInvariant());
            }
        }

        // ── SVG icon paths (Material Design, 24×24 viewbox) ───────────────────

        /// <summary>SVG path data for the action dot. Rendered as a scaled Path in the UI.</summary>
        public string DotIcon
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(RawActionType))
                    return IconForRaw(RawActionType);
                switch (Action)
                {
                    case AuditAction.Create:  return _iconAdd;
                    case AuditAction.Update:  return _iconEdit;
                    case AuditAction.Delete:  return _iconDelete;
                    case AuditAction.View:    return _iconView;
                    case AuditAction.Login:   return _iconLogin;
                    case AuditAction.Logout:  return _iconLogout;
                    case AuditAction.System:  return _iconSystem;
                    default:                  return _iconUnknown;
                }
            }
        }

        private static string IconForRaw(string raw)
        {
            switch (raw.ToUpperInvariant())
            {
                case "LOGIN":               return _iconLogin;
                case "LOGOUT":              return _iconLogout;
                case "REFRESH_TOKEN":       return _iconRefresh;
                case "CHANGE_PASSWORD":     return _iconPassword;
                case "REGISTER_USER":       return _iconUserAdd;
                case "REGISTER_ADMIN":      return _iconAdminAdd;
                case "UPDATE_USER":         return _iconUserEdit;
                case "DISABLE_USER":        return _iconUserOff;
                case "CREATE_SOLO_PARENT":  return _iconAdd;
                case "UPDATE_SOLO_PARENT":  return _iconEdit;
                case "DELETE_SOLO_PARENT":  return _iconDelete;
                case "RENEW_SOLO_PARENT":   return _iconRenew;
                default:                    return _iconUnknown;
            }
        }

        // Login — arrow right into box
        private const string _iconLogin    = "M10,17V14H3V10H10V7L15,12L10,17M10,2H19A2,2 0 0,1 21,4V20A2,2 0 0,1 19,22H10A2,2 0 0,1 8,20V18H10V20H19V4H10V6H8V4A2,2 0 0,1 10,2Z";
        // Logout — arrow left out of box
        private const string _iconLogout   = "M17,17V14H10V10H17V7L22,12L17,17M13,2A2,2 0 0,1 15,4V6H13V4H4V20H13V18H15V20A2,2 0 0,1 13,22H4A2,2 0 0,1 2,20V4A2,2 0 0,1 4,2H13Z";
        // Refresh token — circular arrows
        private const string _iconRefresh  = "M17.65,6.35C16.2,4.9 14.21,4 12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20C15.73,20 18.84,17.45 19.73,14H17.65C16.83,16.33 14.61,18 12,18A6,6 0 0,1 6,12A6,6 0 0,1 12,6C13.66,6 15.14,6.69 16.22,7.78L13,11H20V4L17.65,6.35Z";
        // Change password — padlock
        private const string _iconPassword = "M18,8H17V6A5,5 0 0,0 7,6V8H6A2,2 0 0,0 4,10V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V10A2,2 0 0,0 18,8M12,17A2,2 0 0,1 10,15A2,2 0 0,1 12,13A2,2 0 0,1 14,15A2,2 0 0,1 12,17M15.1,8H8.9V6A3.1,3.1 0 0,1 12,2.9A3.1,3.1 0 0,1 15.1,6V8Z";
        // Register user — person + plus
        private const string _iconUserAdd  = "M15,14C12.33,14 7,15.33 7,18V20H23V18C23,15.33 17.67,14 15,14M6,10V7H4V10H1V12H4V15H6V12H9V10M15,12A4,4 0 0,0 19,8A4,4 0 0,0 15,4A4,4 0 0,0 11,8A4,4 0 0,0 15,12Z";
        // Register admin — person + shield
        private const string _iconAdminAdd = "M12,1L3,5V11C3,16.55 6.84,21.74 12,23C17.16,21.74 21,16.55 21,11V5L12,1M12,7A2,2 0 0,1 14,9A2,2 0 0,1 12,11A2,2 0 0,1 10,9A2,2 0 0,1 12,7M18,15.58C18,17.81 15.32,19.64 12,19.64C8.68,19.64 6,17.81 6,15.58V15C6,13.75 7.9,12.67 10.56,12.22C11,13.24 11.46,13.94 12,13.94C12.54,13.94 13,13.24 13.44,12.22C16.1,12.67 18,13.75 18,15V15.58Z";
        // Update user — person + pencil
        private const string _iconUserEdit = "M21.7,13.35L20.7,14.35L18.65,12.3L19.65,11.3C19.86,11.09 20.21,11.09 20.42,11.3L21.7,12.58C21.91,12.79 21.91,13.14 21.7,13.35M12,18.94L18.07,12.88L20.12,14.93L14.06,21H12V18.94M12,14C9.33,14 4,15.33 4,18V20H10V18.19L14.79,13.4C13.9,13.14 12.96,13 12,13M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12A4,4 0 0,1 8,8A4,4 0 0,1 12,4Z";
        // Disable user — person + block
        private const string _iconUserOff  = "M10,4A4,4 0 0,1 14,8A4,4 0 0,1 10,12A4,4 0 0,1 6,8A4,4 0 0,1 10,4M17,17.25C17,14.92 13.86,13 10,13C6.14,13 3,14.92 3,17.25V19H17V17.25M20.5,14.5L22,16L19,19L17.5,17.5L19,16L17.5,14.5L19,13L20.5,14.5M19,11L17.5,12.5L16,11L17.5,9.5L16,8L17.5,6.5L19,8L20.5,6.5L22,8L20.5,9.5L22,11L20.5,12.5L19,11Z";
        // Create record — file + plus
        private const string _iconAdd      = "M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20M11,13H9V11H11V9H13V11H15V13H13V15H11V13Z";
        // Edit record — pencil
        private const string _iconEdit     = "M14.06,9L15,9.94L5.92,19H5V18.08L14.06,9M17.66,3C17.41,3 17.15,3.1 16.96,3.29L15.13,5.12L18.88,8.87L20.71,7.04C21.1,6.65 21.1,6 20.71,5.63L18.37,3.29C18.17,3.09 17.92,3 17.66,3M14.06,6.19L3,17.25V21H6.75L17.81,9.94L14.06,6.19Z";
        // Delete record — trash
        private const string _iconDelete   = "M19,4H15.5L14.5,3H9.5L8.5,4H5V6H19M6,19A2,2 0 0,0 8,21H16A2,2 0 0,0 18,19V7H6V19Z";
        // Renew — circular refresh with extra arc
        private const string _iconRenew    = "M12,4C14.2,4 16.2,4.85 17.7,6.3L15,9H22V2L19.5,4.5C17.6,2.95 15.2,2 12.5,2C7,2 2.5,6.2 2,11.5L4,11.7C4.4,7.3 7.8,4 12,4M12,20C9.8,20 7.8,19.15 6.3,17.7L9,15H2V22L4.5,19.5C6.4,21.05 8.8,22 11.5,22C17,22 21.5,17.8 22,12.5L20,12.3C19.6,16.7 16.2,20 12,20Z";
        // View — eye
        private const string _iconView     = "M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9M12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17M12,4.5C7,4.5 2.73,7.61 1,12C2.73,16.39 7,19.5 12,19.5C17,19.5 21.27,16.39 23,12C21.27,7.61 17,4.5 12,4.5Z";
        // System — cog
        private const string _iconSystem   = "M12,15.5A3.5,3.5 0 0,1 8.5,12A3.5,3.5 0 0,1 12,8.5A3.5,3.5 0 0,1 15.5,12A3.5,3.5 0 0,1 12,15.5M19.43,12.97C19.47,12.65 19.5,12.33 19.5,12C19.5,11.67 19.47,11.34 19.43,11L21.54,9.37C21.73,9.22 21.78,8.95 21.66,8.73L19.66,5.27C19.54,5.05 19.27,4.96 19.05,5.05L16.56,6.05C16.04,5.66 15.5,5.32 14.87,5.07L14.5,2.42C14.46,2.18 14.25,2 14,2H10C9.75,2 9.54,2.18 9.5,2.42L9.13,5.07C8.5,5.32 7.96,5.66 7.44,6.05L4.95,5.05C4.73,4.96 4.46,5.05 4.34,5.27L2.34,8.73C2.21,8.95 2.27,9.22 2.46,9.37L4.57,11C4.53,11.34 4.5,11.67 4.5,12C4.5,12.33 4.53,12.65 4.57,12.97L2.46,14.63C2.27,14.78 2.21,15.05 2.34,15.27L4.34,18.73C4.46,18.95 4.73,19.03 4.95,18.95L7.44,17.94C7.96,18.34 8.5,18.68 9.13,18.93L9.5,21.58C9.54,21.82 9.75,22 10,22H14C14.25,22 14.46,21.82 14.5,21.58L14.87,18.93C15.5,18.68 16.04,18.34 16.56,17.94L19.05,18.95C19.27,19.03 19.54,18.95 19.66,18.73L21.66,15.27C21.78,15.05 21.73,14.78 21.54,14.63L19.43,12.97Z";
        // Unknown — question circle
        private const string _iconUnknown  = "M11,18H13V16H11V18M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,20C7.59,20 4,16.41 4,12C4,7.59 7.59,4 12,4C16.41,4 20,7.59 20,12C20,16.41 16.41,20 12,20M12,6A4,4 0 0,0 8,10H10A2,2 0 0,1 12,8A2,2 0 0,1 14,10C14,12 11,11.75 11,15H13C13,12.75 16,12.5 16,10A4,4 0 0,0 12,6Z";
    }
}
