using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SOLUM_UI.Models;

namespace SOLUM_UI.Services
{
    public class AuditLogService
    {
        private static readonly AuditLogService _instance = new AuditLogService();
        public static AuditLogService Instance => _instance;

        private AuditLogService() { }

        public ObservableCollection<AuditLog> Logs { get; }
            = new ObservableCollection<AuditLog>();

        private int _nextId = 1;

        public void Log(AuditAction action, string description,
            string performedBy     = "System",
            string performedByRole = "",
            string recordId        = "",
            string recordName      = "",
            string fieldChanged    = "",
            string oldValue        = "",
            string newValue        = "")
        {
            Logs.Insert(0, new AuditLog
            {
                Id              = _nextId++,
                Action          = action,
                Description     = description,
                PerformedBy     = performedBy,
                PerformedByRole = performedByRole,
                RecordId        = recordId,
                RecordName      = recordName,
                FieldChanged    = fieldChanged,
                OldValue        = oldValue,
                NewValue        = newValue,
                Timestamp       = DateTime.Now
            });
        }

        public void LogCreate(string recordId, string recordName, string by, string role = "")
            => Log(AuditAction.Create,
                   $"Created solo parent record for {recordName}",
                   by, role, recordId, recordName);

        public void LogUpdate(string recordId, string recordName, string by, string role = "",
            string field = "", string oldVal = "", string newVal = "")
        {
            string desc = string.IsNullOrWhiteSpace(field)
                ? $"Updated record for {recordName}"
                : $"Updated {recordName} — {field}";
            Log(AuditAction.Update, desc, by, role, recordId, recordName, field, oldVal, newVal);
        }
        /// <summary>
        /// Log an update with multiple field-level changes tracked in detail.
        /// </summary>
        public void LogUpdateWithChanges(string recordId, string recordName, string by, string role = "",
            List<ChangeDetail> changes = null)
        {
            var changeCount = changes?.Count ?? 0;
            string desc = changeCount == 0
                ? $"Updated record for {recordName}"
                : $"Updated record for {recordName} — {changeCount} field(s) changed";
            var log = new AuditLog
            {
                Id              = _nextId++,
                Action          = AuditAction.Update,
                Description     = desc,
                PerformedBy     = by,
                PerformedByRole = role,
                RecordId        = recordId,
                RecordName      = recordName,
                Timestamp       = DateTime.Now,
                Changes         = changes ?? new List<ChangeDetail>()
            };
            Logs.Insert(0, log);
        }

        public void LogDelete(string recordId, string recordName, string by, string role = "")
            => Log(AuditAction.Delete,
                   $"Deleted record for {recordName}",
                   by, role, recordId, recordName);

        public void LogSoftDelete(string recordId, string recordName, string by, string role = "", string reason = "")
        {
            string desc = string.IsNullOrWhiteSpace(reason)
                ? $"Moved record for {recordName} to Recycle Bin"
                : $"Moved record for {recordName} to Recycle Bin (Reason: {reason})";
            var changes = !string.IsNullOrWhiteSpace(reason)
                ? new List<ChangeDetail> { new ChangeDetail("DeletionReason", string.Empty, reason) }
                : new List<ChangeDetail>();
            var log = new AuditLog
            {
                Id              = _nextId++,
                Action          = AuditAction.Delete,
                RawActionType   = "DELETE_SOLO_PARENT",
                Description     = desc,
                PerformedBy     = by,
                PerformedByRole = role,
                RecordId        = recordId,
                RecordName      = recordName,
                TargetType      = "SoloParent",
                Timestamp       = DateTime.Now,
                Changes         = changes
            };
            Logs.Insert(0, log);
        }

        public void LogReinstate(string recordId, string recordName, string by, string role = "")
        {
            var log = new AuditLog
            {
                Id              = _nextId++,
                Action          = AuditAction.Update,
                RawActionType   = "REINSTATE_SOLO_PARENT",
                Description     = $"Reinstated record for {recordName} from Recycle Bin",
                PerformedBy     = by,
                PerformedByRole = role,
                RecordId        = recordId,
                RecordName      = recordName,
                TargetType      = "SoloParent",
                Timestamp       = DateTime.Now
            };
            Logs.Insert(0, log);
        }

        public void LogReactivate(string recordId, string recordName, string by, string role = "", string notes = "")
        {
            var changes = !string.IsNullOrWhiteSpace(notes)
                ? new List<ChangeDetail> { new ChangeDetail("Justification", string.Empty, notes) }
                : new List<ChangeDetail>();
            var log = new AuditLog
            {
                Id              = _nextId++,
                Action          = AuditAction.Update,
                RawActionType   = "REACTIVATE_ARCHIVED_SOLO_PARENT",
                Description     = $"Reactivated archived record for {recordName}",
                PerformedBy     = by,
                PerformedByRole = role,
                RecordId        = recordId,
                RecordName      = recordName,
                TargetType      = "SoloParentArchive",
                Timestamp       = DateTime.Now,
                Changes         = changes
            };
            Logs.Insert(0, log);
        }

        public void LogArchive(string recordId, string recordName, string by, string role = "", string reason = "")
        {
            var log = new AuditLog
            {
                Id              = _nextId++,
                Action          = AuditAction.System,
                RawActionType   = "ARCHIVE_SOLO_PARENT",
                Description     = string.IsNullOrWhiteSpace(reason)
                    ? $"Archived record for {recordName} to Municipal Archives"
                    : $"Archived record for {recordName} ({reason})",
                PerformedBy     = by,
                PerformedByRole = role,
                RecordId        = recordId,
                RecordName      = recordName,
                TargetType      = "SoloParentArchive",
                Timestamp       = DateTime.Now
            };
            Logs.Insert(0, log);
        }

        public void LogView(string recordId, string recordName, string by, string role = "")
            => Log(AuditAction.View,
                   $"Viewed profile of {recordName}",
                   by, role, recordId, recordName);

        public void LogLogin(string by, string role = "")
            => Log(AuditAction.Login, $"User logged in: {by}", by, role);

        public void LogLogout(string by, string role = "")
            => Log(AuditAction.Logout, $"User logged out: {by}", by, role);

        public void LogUserAdmin(string action, string targetUser, string by, string role = "", string targetUserId = "")
            => Log(AuditAction.System,
                   $"User account {action}: {targetUser} (by {by})",
                   by, role, targetUserId, targetUser);

        public void LogSystem(string description)
            => Log(AuditAction.System, description, "System");

        public IEnumerable<AuditLog> GetRecent(int count = 5)
            => Logs.Take(count);

        public IEnumerable<AuditLog> GetForRecord(string recordId)
            => Logs.Where(l => l.RecordId == recordId);

        public IEnumerable<AuditLog> GetForUser(string userName, string role)
        {
            bool isAdmin = string.Equals(role, "Administrator", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
            return isAdmin
                ? Logs.AsEnumerable()
                : Logs.Where(l => string.Equals(l.PerformedBy, userName, StringComparison.OrdinalIgnoreCase));
        }
    }
}