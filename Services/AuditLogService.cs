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

        public void Log(AuditAction action, string description,
            string performedBy = "System", string recordName = "",
            string fieldChanged = "", string oldValue = "", string newValue = "")
        {
            Logs.Insert(0, new AuditLog
            {
                Id = _nextId++,
                Action = action,
                Description = description,
                PerformedBy = performedBy,
                RecordName = recordName,
                FieldChanged = fieldChanged,
                OldValue = oldValue,
                NewValue = newValue,
                Timestamp = DateTime.Now
            });
        }

        public void LogCreate(string recordName, string by)
            => Log(AuditAction.Create, "New record added: " + recordName, by, recordName);

        public void LogUpdate(string recordName, string by,
            string fieldChanged = "", string oldValue = "", string newValue = "")
            => Log(AuditAction.Update, "Record updated: " + recordName, by, recordName,
                   fieldChanged, oldValue, newValue);

        public void LogDelete(string recordName, string by)
            => Log(AuditAction.Delete, "Record deleted: " + recordName, by, recordName);

        public void LogView(string recordName, string by)
            => Log(AuditAction.View, "Record viewed: " + recordName, by, recordName);

        public void LogLogin(string by)
            => Log(AuditAction.Login, "User logged in", by);

        public void LogLogout(string by)
            => Log(AuditAction.Logout, "User logged out", by);

        public void LogSystem(string description)
            => Log(AuditAction.System, description, "System");

        public IEnumerable<AuditLog> GetRecent(int count = 5)
            => Logs.Take(count);

        public IEnumerable<AuditLog> GetForRecord(string recordName)
            => Logs.Where(l => l.RecordName == recordName);

        private void SeedDummyLogs()
        {
            var seed = new[]
            {
                new AuditLog { Id = _nextId++, Action = AuditAction.Update,
                    Description  = "Record updated: Mario Santos",
                    PerformedBy  = "Officer J. Diaz", RecordName = "Mario Santos",
                    FieldChanged = "First Name", OldValue = "Mario", NewValue = "Maria",
                    Timestamp    = DateTime.Now.AddMinutes(-2) },

                new AuditLog { Id = _nextId++, Action = AuditAction.Create,
                    Description = "New record added: Juan Dela Cruz",
                    PerformedBy = "Admin", RecordName = "Juan Dela Cruz",
                    Timestamp   = DateTime.Now.AddMinutes(-40) },

                new AuditLog { Id = _nextId++, Action = AuditAction.System,
                    Description = "OCR batch scan complete — 24 documents processed",
                    PerformedBy = "System", RecordName = "",
                    Timestamp   = DateTime.Now.AddHours(-2) },

                new AuditLog { Id = _nextId++, Action = AuditAction.Delete,
                    Description = "Record deleted: Pedro Reyes",
                    PerformedBy = "Admin", RecordName = "Pedro Reyes",
                    Timestamp   = DateTime.Now.AddHours(-3) },

                new AuditLog { Id = _nextId++, Action = AuditAction.Login,
                    Description = "User logged in",
                    PerformedBy = "Officer M. Cruz",
                    Timestamp   = DateTime.Now.AddHours(-4) },

                new AuditLog { Id = _nextId++, Action = AuditAction.Update,
                    Description  = "Record updated: Ana Reyes",
                    PerformedBy  = "Officer J. Diaz", RecordName = "Ana Reyes",
                    FieldChanged = "Status", OldValue = "Inactive", NewValue = "Active",
                    Timestamp    = DateTime.Now.AddHours(-5) },

                new AuditLog { Id = _nextId++, Action = AuditAction.Create,
                    Description = "New record added: Liza Manalang",
                    PerformedBy = "Admin", RecordName = "Liza Manalang",
                    Timestamp   = DateTime.Now.AddHours(-6) },

                new AuditLog { Id = _nextId++, Action = AuditAction.View,
                    Description = "Record viewed: Carlos Bautista",
                    PerformedBy = "Officer M. Cruz", RecordName = "Carlos Bautista",
                    Timestamp   = DateTime.Now.AddHours(-7) },

                new AuditLog { Id = _nextId++, Action = AuditAction.Update,
                    Description  = "Record updated: Rosa Mendoza",
                    PerformedBy  = "Admin", RecordName = "Rosa Mendoza",
                    FieldChanged = "Address", OldValue = "123 Old St", NewValue = "456 New Ave",
                    Timestamp    = DateTime.Now.AddHours(-8) },

                new AuditLog { Id = _nextId++, Action = AuditAction.Logout,
                    Description = "User logged out",
                    PerformedBy = "Officer J. Diaz",
                    Timestamp   = DateTime.Now.AddHours(-9) },

                new AuditLog { Id = _nextId++, Action = AuditAction.System,
                    Description = "Daily backup completed successfully",
                    PerformedBy = "System",
                    Timestamp   = DateTime.Now.AddHours(-10) },

                new AuditLog { Id = _nextId++, Action = AuditAction.Create,
                    Description = "New record added: Ben Torres",
                    PerformedBy = "Admin", RecordName = "Ben Torres",
                    Timestamp   = DateTime.Now.AddDays(-1) },
            };

            foreach (var log in seed.Reverse())
                Logs.Insert(0, log);
        }
    }
}
