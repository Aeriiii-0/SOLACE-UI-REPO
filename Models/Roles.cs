using System;

namespace SOLUM_UI.Models
{
    public static class Roles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Admin = "Admin";
        public const string Caseworker = "Caseworker";
        public const string BarangaySecretary = "BarangaySecretary";
        public const string BarangayPresident = "BarangayPresident";

        // Legacy aliases
        public const string Encoder = "Encoder";
        public const string Administrator = "Administrator";

        public static bool IsSuperAdmin(string role) =>
            string.Equals(role, SuperAdmin, StringComparison.OrdinalIgnoreCase);

        public static bool IsAdmin(string role) =>
            string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, Administrator, StringComparison.OrdinalIgnoreCase);

        public static bool IsAdminOrHigher(string role) =>
            IsSuperAdmin(role) || IsAdmin(role);

        public static bool IsCaseworker(string role) =>
            string.Equals(role, Caseworker, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, Encoder, StringComparison.OrdinalIgnoreCase);

        public static bool IsBarangaySecretary(string role) =>
            string.Equals(role, BarangaySecretary, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, BarangayPresident, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "BARANGAY SECRETARY", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "BARANGAY PRESIDENT", StringComparison.OrdinalIgnoreCase);
    }
}
