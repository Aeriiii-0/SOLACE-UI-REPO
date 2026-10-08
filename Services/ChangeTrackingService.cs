using System;
using System.Collections.Generic;
using SOLUM_UI.Models;

namespace SOLUM_UI.Services
{
    /// <summary>
    /// Service for detecting and capturing field-level changes between two SoloParentRecord instances.
    /// Compares before/after values and generates ChangeDetail entries for audit logging.
    /// </summary>
    public class ChangeTrackingService
    {
        /// <summary>
        /// Compares two SoloParentRecord instances and returns a list of ChangeDetail
        /// entries for all fields that differ.
        /// </summary>
        public List<ChangeDetail> DetectChanges(SoloParentRecord before, SoloParentRecord after)
        {
            var changes = new List<ChangeDetail>();

            if (before == null || after == null)
                return changes;

            // Identity & Metadata
            CompareField(changes, "Last Name", before.Surname, after.Surname);
            CompareField(changes, "First Name", before.FirstName, after.FirstName);
            CompareField(changes, "Middle Name", before.MiddleName, after.MiddleName);
            CompareField(changes, "Extension Name", before.ExtensionName, after.ExtensionName);

            // Personal Information
            CompareField(changes, "Sex", before.Sex, after.Sex);
            CompareField(changes, "Civil Status", before.CivilStatus, after.CivilStatus);
            CompareField(changes, "Date of Birth", FormatDate(before.DateOfBirth), FormatDate(after.DateOfBirth));
            CompareField(changes, "Place of Birth", before.PlaceOfBirth, after.PlaceOfBirth);
            CompareField(changes, "Citizenship", before.Citizenship, after.Citizenship);
            CompareField(changes, "Blood Type", before.BloodType, after.BloodType);
            CompareField(changes, "Height", before.Height, after.Height);
            CompareField(changes, "Weight", before.Weight, after.Weight);

            // Background
            CompareField(changes, "Educational Attainment", before.EducationalAttainment, after.EducationalAttainment);
            CompareField(changes, "PhilSys Number", before.PhilSysNumber, after.PhilSysNumber);
            CompareField(changes, "Religion", before.Religion, after.Religion);
            CompareField(changes, "Occupation", before.Occupation, after.Occupation);
            CompareField(changes, "Monthly Income", before.MonthlyIncome, after.MonthlyIncome);
            CompareField(changes, "Employment Status - Employed", FormatBool(before.IsEmployed), FormatBool(after.IsEmployed));
            CompareField(changes, "Employment Status - Self-Employed", FormatBool(before.IsSelfEmployed), FormatBool(after.IsSelfEmployed));
            CompareField(changes, "Employment Status - Not Employed", FormatBool(before.IsNotEmployed), FormatBool(after.IsNotEmployed));

            // Address & Contact
            CompareField(changes, "Address", before.Address, after.Address);
            CompareField(changes, "Barangay", before.Barangay, after.Barangay);
            CompareField(changes, "Contact Number", before.ContactNumber, after.ContactNumber);

            // Emergency Contact
            CompareField(changes, "Emergency Contact Name", before.EmergencyContactName, after.EmergencyContactName);
            CompareField(changes, "Emergency Contact Relationship", before.EmergencyRelationship, after.EmergencyRelationship);
            CompareField(changes, "Emergency Contact Address", before.EmergencyAddress, after.EmergencyAddress);
            CompareField(changes, "Emergency Contact Number", before.EmergencyContactNumber, after.EmergencyContactNumber);

            // Children & Family
            CompareField(changes, "Number of Children", before.Children.ToString(), after.Children.ToString());

            // Circumstances
            CompareField(changes, "Circumstance A1 - Rape", FormatBool(before.CircumstanceA1), FormatBool(after.CircumstanceA1));
            CompareField(changes, "Circumstance A2 - Death of Spouse", FormatBool(before.CircumstanceA2), FormatBool(after.CircumstanceA2));
            CompareField(changes, "Circumstance A2 Cause", before.CircumstanceA2Cause, after.CircumstanceA2Cause);
            CompareField(changes, "Circumstance A2 Date", FormatDate(before.CircumstanceA2Date), FormatDate(after.CircumstanceA2Date));
            CompareField(changes, "Circumstance A3 - Detention of Spouse", FormatBool(before.CircumstanceA3), FormatBool(after.CircumstanceA3));
            CompareField(changes, "Circumstance A4 - Physical & Mental Incapacity", FormatBool(before.CircumstanceA4), FormatBool(after.CircumstanceA4));
            CompareField(changes, "Circumstance A4 Disability", before.CircumstanceA4Disability, after.CircumstanceA4Disability);
            CompareField(changes, "Circumstance A5 - Legal/De Facto Separation", FormatBool(before.CircumstanceA5), FormatBool(after.CircumstanceA5));
            CompareField(changes, "Circumstance A5 Period", before.CircumstanceA5Period, after.CircumstanceA5Period);
            CompareField(changes, "Circumstance A6 - Declaration", FormatBool(before.CircumstanceA6), FormatBool(after.CircumstanceA6));
            CompareField(changes, "Circumstance A6 Nullity", FormatBool(before.CircumstanceA6Nullity), FormatBool(after.CircumstanceA6Nullity));
            CompareField(changes, "Circumstance A6 Annulment", FormatBool(before.CircumstanceA6Annulment), FormatBool(after.CircumstanceA6Annulment));
            CompareField(changes, "Circumstance A7 - Abandonment", FormatBool(before.CircumstanceA7), FormatBool(after.CircumstanceA7));
            CompareField(changes, "Circumstance B - OFW Related", FormatBool(before.CircumstanceB), FormatBool(after.CircumstanceB));
            CompareField(changes, "Circumstance B Stay Abroad", before.CircumstanceBStayAbroad, after.CircumstanceBStayAbroad);
            CompareField(changes, "Circumstance C - Unmarried Parent", FormatBool(before.CircumstanceC), FormatBool(after.CircumstanceC));
            CompareField(changes, "Circumstance D - Legal Guardian", FormatBool(before.CircumstanceD), FormatBool(after.CircumstanceD));
            CompareField(changes, "Circumstance E - Relative 4th Degree", FormatBool(before.CircumstanceE), FormatBool(after.CircumstanceE));
            CompareField(changes, "Circumstance F - Pregnant", FormatBool(before.CircumstanceF), FormatBool(after.CircumstanceF));

            // Special Status
            CompareField(changes, "LGBT", FormatBool(before.IsLGBT), FormatBool(after.IsLGBT));
            CompareField(changes, "Pantawid Beneficiary", FormatBool(before.IsPantawidBeneficiary), FormatBool(after.IsPantawidBeneficiary));
            CompareField(changes, "Indigenous", FormatBool(before.IsIndigenous), FormatBool(after.IsIndigenous));

            // Needs & Other Income
            CompareField(changes, "Needs & Problems", before.NeedsAndProblems, after.NeedsAndProblems);
            CompareField(changes, "Other Income Source", before.OtherIncomeSource, after.OtherIncomeSource);

            // Status
            CompareField(changes, "Status", before.Status, after.Status);

            return changes;
        }

        /// <summary>
        /// Compares two field values and adds a ChangeDetail to the list if they differ.
        /// </summary>
        private void CompareField(List<ChangeDetail> changes, string fieldName, string oldValue, string newValue)
        {
            if (!string.Equals(oldValue ?? "", newValue ?? "", StringComparison.Ordinal))
            {
                changes.Add(new ChangeDetail(fieldName, oldValue, newValue));
            }
        }

        /// <summary>Formats a DateTime for comparison, or returns null if the date is unset.</summary>
        private string FormatDate(DateTime dt)
        {
            return dt == DateTime.MinValue ? null : dt.ToString("yyyy-MM-dd");
        }

        /// <summary>Formats a boolean as "Yes" or "No", or null if false (to reduce noise).</summary>
        private string FormatBool(bool value)
        {
            return value ? "Yes" : null;
        }
    }
}
