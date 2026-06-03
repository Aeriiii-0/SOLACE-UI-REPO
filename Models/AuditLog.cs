using System;

namespace SOLUM_UI.Models
{
    public enum AuditAction
    {
        Create, Update, Delete, View, Login, Logout, System
    }

    public class AuditLog
    {
        public int Id { get; set; }
        public AuditAction Action { get; set; }
        public string Description { get; set; }
        public string PerformedBy { get; set; }
        public DateTime Timestamp { get; set; }
        public string RecordName { get; set; }

        
        public string FieldChanged { get; set; }   
        public string OldValue { get; set; }        
        public string NewValue { get; set; }       

      
        public bool HasChangeDetail =>
            !string.IsNullOrWhiteSpace(FieldChanged) ||
            !string.IsNullOrWhiteSpace(OldValue) ||
            !string.IsNullOrWhiteSpace(NewValue);

        public string TimeAgo
        {
            get
            {
                TimeSpan diff = DateTime.Now - Timestamp;
                if (diff.TotalMinutes < 1) return "Just now";
                if (diff.TotalMinutes < 60) return ((int)diff.TotalMinutes) + " mins ago";
                if (diff.TotalHours < 24) return ((int)diff.TotalHours) + " hours ago";
                return ((int)diff.TotalDays) + " days ago";
            }
        }

        public string DotColor
        {
            get
            {
                switch (Action)
                {
                    case AuditAction.Create: return "#28A745";
                    case AuditAction.Update: return "#4A90D9";
                    case AuditAction.Delete: return "#DC3545";
                    case AuditAction.View: return "#888888";
                    case AuditAction.Login: return "#702943";
                    case AuditAction.Logout: return "#888888";
                    case AuditAction.System: return "#FFA500";
                    default: return "#702943";
                }
            }
        }

        public string DotLabel
        {
            get
            {
                switch (Action)
                {
                    case AuditAction.Create: return "+";
                    case AuditAction.Update: return "E";
                    case AuditAction.Delete: return "X";
                    case AuditAction.View: return "V";
                    case AuditAction.Login: return "I";
                    case AuditAction.Logout: return "O";
                    case AuditAction.System: return "!";
                    default: return "?";
                }
            }
        }

        public string ActionLabel
        {
            get
            {
                switch (Action)
                {
                    case AuditAction.Create: return "Created";
                    case AuditAction.Update: return "Updated";
                    case AuditAction.Delete: return "Deleted";
                    case AuditAction.View: return "Viewed";
                    case AuditAction.Login: return "Logged In";
                    case AuditAction.Logout: return "Logged Out";
                    case AuditAction.System: return "System";
                    default: return "Unknown";
                }
            }
        }
    }
}
