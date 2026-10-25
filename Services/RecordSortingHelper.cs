using System;
using System.Collections.Generic;
using System.Linq;
using SOLUM_UI.ViewModels;

namespace SOLUM_UI.Services
{
    public enum RecordSortField
    {
        Barangay,
        LastUpdated
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
    /// Handles record ranking, date calculations, and API parameter mapping.
    /// </summary>
    public static class RecordSortingHelper
    {
        public static DateTime GetLastUpdatedDate(SoloParentRecordViewModel vm)
        {
            if (vm?.RawModel == null) return DateTime.MinValue;
            if (vm.RawModel.LastUpdated != DateTime.MinValue)
                return vm.RawModel.LastUpdated;
            if (vm.RawModel.DateOfApplication != DateTime.MinValue)
                return vm.RawModel.DateOfApplication;
            return DateTime.MinValue;
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
                case RecordSortField.LastUpdated:
                    // Descending: newest update/created first
                    ordered = isAscending
                        ? records.OrderBy(GetLastUpdatedDate).ThenBy(r => r.Surname)
                        : records.OrderByDescending(GetLastUpdatedDate).ThenBy(r => r.Surname);
                    break;

                case RecordSortField.Barangay:
                default:
                    ordered = isAscending
                        ? records.OrderBy(r => r.Barangay ?? string.Empty).ThenBy(r => r.Surname)
                        : records.OrderByDescending(r => r.Barangay ?? string.Empty).ThenBy(r => r.Surname);
                    break;
            }

            return ordered.ToList();
        }

        public static (string sortBy, string sortOrder) MapToApiParams(RecordSortField field, bool isAscending)
        {
            string order = isAscending ? "asc" : "desc";
            switch (field)
            {
                case RecordSortField.LastUpdated:
                    // In backend API, "datecreated" is used for record timestamp
                    return ("datecreated", order);
                case RecordSortField.Barangay:
                default:
                    return ("barangay", order);
            }
        }
    }
}
