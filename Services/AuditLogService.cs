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

        private AuditLogService() { SeedDummyLogs(); }

        public ObservableCollection<AuditLog> Logs { get; }
            = new ObservableCollection<AuditLog>();

        private int _nextId = 1;

        /// <summary>Core entry point. All convenience methods funnel here.</summary>
        public void Log(AuditAction action, string description,
            string performedBy   = "System",
            string performedByRole = "",
            string recordId      = "",
            string recordName    = "",
            string fieldChanged  = "",
            string oldValue      = "",
            string newValue      = "")
        {
            Logs.Insert(0, new AuditLog
            {
                Id             = _nextId++,
                Action         = action,
                Description    = description,
                PerformedBy    = performedBy,
                PerformedByRole = performedByRole,
                RecordId       = recordId,
                RecordName     = recordName,
                FieldChanged   = fieldChanged,
                OldValue       = oldValue,
                NewValue       = newValue,
                Timestamp      = DateTime.Now
            });
        }

        /// <summary>Logs a new record creation with who created it and their role.</summary>
        public void LogCreate(string recordId, string recordName, string by, string role = "")
            => Log(AuditAction.Create,
                   $"Created record: {recordName} [{recordId}]",
                   by, role, recordId, recordName);

        /// <summary>Logs an update with the specific field that changed, previous and new values.</summary>
        public void LogUpdate(string recordId, string recordName, string by, string role = "",
            string field = "", string oldVal = "", string newVal = "")
        {
            string desc = string.IsNullOrWhiteSpace(field)
                ? $"Updated record: {recordName} [{recordId}]"
                : $"Updated {recordName} [{recordId}] — {field}";
            Log(AuditAction.Update, desc, by, role, recordId, recordName, field, oldVal, newVal);
        }

        /// <summary>Logs deletion including who deleted and the record identity.</summary>
        public void LogDelete(string recordId, string recordName, string by, string role = "")
            => Log(AuditAction.Delete,
                   $"Deleted record: {recordName} [{recordId}]",
                   by, role, recordId, recordName);

        /// <summary>Logs a view event — kept lightweight as views are high-frequency.</summary>
        public void LogView(string recordId, string recordName, string by, string role = "")
            => Log(AuditAction.View,
                   $"Viewed record: {recordName} [{recordId}]",
                   by, role, recordId, recordName);

        /// <summary>Logs a successful login with timestamp context.</summary>
        public void LogLogin(string by, string role = "")
            => Log(AuditAction.Login, $"User logged in: {by}", by, role);

        /// <summary>Logs a logout event.</summary>
        public void LogLogout(string by, string role = "")
            => Log(AuditAction.Logout, $"User logged out: {by}", by, role);

        /// <summary>Logs a user account action (create/edit/delete user) from the admin panel.</summary>
        public void LogUserAdmin(string action, string targetUser, string by, string role = "")
            => Log(AuditAction.System,
                   $"User account {action}: {targetUser} (by {by})",
                   by, role, "", targetUser);

        /// <summary>General system event — used for ML runs, backups, OCR, etc.</summary>
        public void LogSystem(string description)
            => Log(AuditAction.System, description, "System");

        /// <summary>Returns the N most recent logs across all users.</summary>
        public IEnumerable<AuditLog> GetRecent(int count = 5)
            => Logs.Take(count);

        /// <summary>Returns all logs for a specific SP record by ID.</summary>
        public IEnumerable<AuditLog> GetForRecord(string recordId)
            => Logs.Where(l => l.RecordId == recordId);

        /// <summary>
        /// Returns logs scoped by role: admins see everything;
        /// basic users see only their own actions.
        /// </summary>
        public IEnumerable<AuditLog> GetForUser(string userName, string role)
        {
            bool isAdmin = string.Equals(role, "Administrator", StringComparison.OrdinalIgnoreCase);
            return isAdmin
                ? Logs.AsEnumerable()
                : Logs.Where(l => string.Equals(l.PerformedBy, userName, StringComparison.OrdinalIgnoreCase));
        }

        private void SeedDummyLogs()
        {
            var seed = new[]
            {
                new AuditLog { Id=_nextId++, Action=AuditAction.Update,
                    Description  = "Updated First Name on Santos, Maria Lim [SP-001]: \"Mario\" → \"Maria\"",
                    PerformedBy  = "Officer J. Diaz", PerformedByRole = "Administrator",
                    RecordId="SP-001", RecordName="Santos, Maria Lim",
                    FieldChanged = "First Name", OldValue = "Mario", NewValue = "Maria",
                    Timestamp    = DateTime.Now.AddMinutes(-2) },

                new AuditLog { Id=_nextId++, Action=AuditAction.Create,
                    Description  = "Created record: Dela Cruz, Juan Reyes Jr. [SP-002]",
                    PerformedBy  = "Admin", PerformedByRole = "Administrator",
                    RecordId="SP-002", RecordName="Dela Cruz, Juan Reyes Jr.",
                    Timestamp    = DateTime.Now.AddMinutes(-40) },

                new AuditLog { Id=_nextId++, Action=AuditAction.System,
                    Description  = "OCR batch scan complete — 24 documents processed",
                    PerformedBy  = "System",
                    Timestamp    = DateTime.Now.AddHours(-2) },

                new AuditLog { Id=_nextId++, Action=AuditAction.Delete,
                    Description  = "Deleted record: Pedro Reyes [SP-OLD-01]",
                    PerformedBy  = "Admin", PerformedByRole = "Administrator",
                    RecordId="SP-OLD-01", RecordName="Pedro Reyes",
                    Timestamp    = DateTime.Now.AddHours(-3) },

                new AuditLog { Id=_nextId++, Action=AuditAction.Login,
                    Description  = "User logged in: Officer M. Cruz",
                    PerformedBy  = "Officer M. Cruz", PerformedByRole = "Basic User",
                    Timestamp    = DateTime.Now.AddHours(-4) },

                new AuditLog { Id=_nextId++, Action=AuditAction.Update,
                    Description  = "Updated Status on Reyes, Ana Mendoza [SP-003]: \"Inactive\" → \"Valid\"",
                    PerformedBy  = "Officer J. Diaz", PerformedByRole = "Administrator",
                    RecordId="SP-003", RecordName="Reyes, Ana Mendoza",
                    FieldChanged = "Status", OldValue = "Inactive", NewValue = "Valid",
                    Timestamp    = DateTime.Now.AddHours(-5) },

                new AuditLog { Id=_nextId++, Action=AuditAction.Create,
                    Description  = "Created record: Lim, Cynthia Tan [SP-006]",
                    PerformedBy  = "Admin", PerformedByRole = "Administrator",
                    RecordId="SP-006", RecordName="Lim, Cynthia Tan",
                    Timestamp    = DateTime.Now.AddHours(-6) },

                new AuditLog { Id=_nextId++, Action=AuditAction.View,
                    Description  = "Viewed record: Bautista, Carlos Ocampo [SP-007]",
                    PerformedBy  = "Officer M. Cruz", PerformedByRole = "Basic User",
                    RecordId="SP-007", RecordName="Bautista, Carlos Ocampo",
                    Timestamp    = DateTime.Now.AddHours(-7) },

                new AuditLog { Id=_nextId++, Action=AuditAction.Update,
                    Description  = "Updated Address on Martinez, Rosa Cruz [SP-005]: \"123 Old St\" → \"456 New Ave\"",
                    PerformedBy  = "Admin", PerformedByRole = "Administrator",
                    RecordId="SP-005", RecordName="Martinez, Rosa Cruz",
                    FieldChanged = "Address", OldValue = "123 Old St", NewValue = "456 New Ave",
                    Timestamp    = DateTime.Now.AddHours(-8) },

                new AuditLog { Id=_nextId++, Action=AuditAction.Logout,
                    Description  = "User logged out: Officer J. Diaz",
                    PerformedBy  = "Officer J. Diaz", PerformedByRole = "Administrator",
                    Timestamp    = DateTime.Now.AddHours(-9) },

                new AuditLog { Id=_nextId++, Action=AuditAction.System,
                    Description  = "Daily backup completed successfully",
                    PerformedBy  = "System",
                    Timestamp    = DateTime.Now.AddHours(-10) },

                new AuditLog { Id=_nextId++, Action=AuditAction.Create,
                    Description  = "Created record: Torres, Benjamin Ramos [SP-009]",
                    PerformedBy  = "Admin", PerformedByRole = "Administrator",
                    RecordId="SP-009", RecordName="Torres, Benjamin Ramos",
                    Timestamp    = DateTime.Now.AddDays(-1) },

                new AuditLog { Id=_nextId++, Action=AuditAction.System,
                    Description  = "User account created: Officer M. Cruz (by Admin)",
                    PerformedBy  = "Admin", PerformedByRole = "Administrator",
                    Timestamp    = DateTime.Now.AddDays(-2) },
            };

            foreach (var log in seed.Reverse())
                Logs.Insert(0, log);
        }
    }
}
