using System;

namespace SOLUM_UI.Models
{
    public static class Roles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Admin = "Admin";
        public const string Caseworker = "Caseworker";

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
    }
}
