using System;
using System.Collections.Generic;
using System.Linq;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Services
{
    public enum RecordSortField
    {
        Name,
        Status,
        ValidUntil,
        LastUpdated,
        Barangay
    }

    public class RecordSortEventArgs : EventArgs
    {
        public RecordSortField Field { get; }
        public bool IsAscending { get; }

        public RecordSortEventArgs(RecordSortField field, bool isAscending)
        {
            Field = field;
            IsAscending = isAscending;
        }
    }

    /// <summary>
    /// Pure sorting and ordering helper adhering to Strict MVVM.
    /// Handles record ranking, expiry calculations, and API parameter mapping.
    /// </summary>
    public static class RecordSortingHelper
    {
        public static DateTime GetValidUntilDate(SoloParentRecordViewModel vm)
        {
            if (vm?.RawModel == null) return DateTime.MaxValue;
            if (vm.RawModel.CurrentTermExpiresAt.HasValue && vm.RawModel.CurrentTermExpiresAt.Value != DateTime.MinValue)
                return vm.RawModel.CurrentTermExpiresAt.Value.Date;
            if (vm.RawModel.LastUpdated != DateTime.MinValue)
                return vm.RawModel.LastUpdated.AddYears(1).Date;
            return DateTime.MaxValue.Date;
        }

        public static DateTime GetLastUpdatedDate(SoloParentRecordViewModel vm)
        {
            if (vm?.RawModel == null) return DateTime.MinValue;
            if (vm.RawModel.LastUpdated != DateTime.MinValue)
                return vm.RawModel.LastUpdated;
            if (vm.RawModel.DateOfApplication != DateTime.MinValue)
                return vm.RawModel.DateOfApplication;
            return DateTime.MinValue;
        }

        public static int GetStatusRank(SoloParentRecordViewModel vm)
        {
            string s = vm?.Status?.Trim() ?? string.Empty;
            // Valid / Active ranks first (0), Inactive / Terminated ranks second (1)
            return (string.Equals(s, "Valid", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(s, "Active", StringComparison.OrdinalIgnoreCase)) ? 0 : 1;
        }

        public static List<SoloParentRecordViewModel> Sort(
            IEnumerable<SoloParentRecordViewModel> records,
            RecordSortField field,
            bool isAscending)
        {
            if (records == null) return new List<SoloParentRecordViewModel>();

            IOrderedEnumerable<SoloParentRecordViewModel> ordered;

            switch (field)
            {
                case RecordSortField.Status:
                    ordered = isAscending
                        ? records.OrderBy(GetStatusRank).ThenBy(r => r.Surname).ThenBy(r => r.FirstName)
                        : records.OrderByDescending(GetStatusRank).ThenBy(r => r.Surname).ThenBy(r => r.FirstName);
                    break;

                case RecordSortField.ValidUntil:
                    // Ascending: earliest expiry dates (almost expiring / expired) appear at the very top
                    ordered = isAscending
                        ? records.OrderBy(GetValidUntilDate).ThenBy(r => r.Surname)
                        : records.OrderByDescending(GetValidUntilDate).ThenBy(r => r.Surname);
                    break;

                case RecordSortField.LastUpdated:
                    // Descending: newest update first
                    ordered = isAscending
                        ? records.OrderBy(GetLastUpdatedDate).ThenBy(r => r.Surname)
                        : records.OrderByDescending(GetLastUpdatedDate).ThenBy(r => r.Surname);
                    break;

                case RecordSortField.Barangay:
                    ordered = isAscending
                        ? records.OrderBy(r => r.Barangay ?? string.Empty).ThenBy(r => r.Surname)
                        : records.OrderByDescending(r => r.Barangay ?? string.Empty).ThenBy(r => r.Surname);
                    break;

                case RecordSortField.Name:
                default:
                    ordered = isAscending
                        ? records.OrderBy(r => r.Surname ?? string.Empty).ThenBy(r => r.FirstName ?? string.Empty)
                        : records.OrderByDescending(r => r.Surname ?? string.Empty).ThenBy(r => r.FirstName ?? string.Empty);
                    break;
            }

            return ordered.ToList();
        }

        public static (string sortBy, string sortOrder) MapToApiParams(RecordSortField field, bool isAscending)
        {
            string order = isAscending ? "asc" : "desc";
            switch (field)
            {
                case RecordSortField.Status:
                    return ("status", order);
                case RecordSortField.ValidUntil:
                    return ("currenttermexpiresat", order);
                case RecordSortField.LastUpdated:
                    // In backend API, "datecreated" is used for record timestamp
                    return ("datecreated", order);
                case RecordSortField.Barangay:
                    return ("barangay", order);
                case RecordSortField.Name:
                default:
                    return ("LastName", order);
            }
        }
    }
}
