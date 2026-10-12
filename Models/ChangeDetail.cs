using System;
using System.Text.RegularExpressions;

namespace SOLUM_UI.Models
{
    /// <summary>
    /// Represents a single field-level change within an audit log entry.
    /// Tracks the field name, previous value, and new value.
    /// </summary>
    public class ChangeDetail
    {
        /// <summary>Human-readable or raw field name (e.g., "MonthlyIncome", "Civil Status")</summary>
        public string FieldName { get; set; }

        /// <summary>Human-friendly formatted field name (e.g., "Monthly Income", "PhilSys Card Number")</summary>
        public string DisplayFieldName => FormatFieldNameStatic(FieldName);

        /// <summary>Previous value before the change</summary>
        public string OldValue { get; set; }

        /// <summary>Display-friendly previous value (e.g., "—", "Yes", "No")</summary>
        public string DisplayOldValue => FormatValueStatic(OldValue);

        /// <summary>New value after the change</summary>
        public string NewValue { get; set; }

        /// <summary>Display-friendly new value (e.g., "—", "Yes", "No")</summary>
        public string DisplayNewValue => FormatValueStatic(NewValue);

        /// <summary>Timestamp when this change was recorded</summary>
        public DateTime ChangedAt { get; set; }

        /// <summary>Display-friendly representation: "Field: OldValue → NewValue"</summary>
        public string Display =>
            $"{DisplayFieldName}: \"{DisplayOldValue}\" → \"{DisplayNewValue}\"";

        public ChangeDetail() { }

        public ChangeDetail(string fieldName, string oldValue, string newValue)
        {
            FieldName = fieldName;
            OldValue = oldValue;
            NewValue = newValue;
            ChangedAt = DateTime.Now;
        }

        public static string FormatFieldNameStatic(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "—";

            switch (name.Trim())
            {
                case "PhilsysCardNumber":
                case "PhilSysCardNumber":
                case "PhilSysId":
                case "PhilsysId":
                case "PhilSysNumber":
                    return "PhilSys Card Number";
                case "IsPantawidBeneficiary":
                case "PantawidBeneficiary":
                    return "Pantawid Beneficiary (4Ps)";
                case "MonthlyIncome":
                    return "Monthly Income";
                case "HouseholdMonthlyIncome":
                    return "Household Monthly Income";
                case "SoloParentCategory":
                    return "Solo Parent Category";
                case "EducationalAttainment":
                    return "Educational Attainment";
                case "ContactNumber":
                    return "Contact Number";
                case "EmergencyContactNumber":
                    return "Emergency Contact Number";
                case "EmergencyContactName":
                    return "Emergency Contact Name";
                case "EmergencyRelationship":
                    return "Emergency Relationship";
                case "CivilStatus":
                    return "Civil Status";
                case "DateOfBirth":
                    return "Date of Birth";
                case "PlaceOfBirth":
                    return "Place of Birth";
                case "FirstName":
                    return "First Name";
                case "LastName":
                    return "Last Name";
                case "MiddleName":
                    return "Middle Name";
                case "ExtensionName":
                    return "Extension Name";
                case "BloodType":
                    return "Blood Type";
                case "IsActive":
                    return "Account Status";
                case "IsEmployed":
                    return "Employed";
                case "IsSelfEmployed":
                    return "Self-Employed";
                case "IsNotEmployed":
                    return "Not Employed";
                default:
                    if (name.Contains(" ")) return name;
                    return Regex.Replace(name, "(\\B[A-Z])", " $1");
            }
        }

        public static string FormatValueStatic(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return "—";
            if (string.Equals(val, "true", StringComparison.OrdinalIgnoreCase)) return "Yes";
            if (string.Equals(val, "false", StringComparison.OrdinalIgnoreCase)) return "No";
            return val;
        }
    }
}
