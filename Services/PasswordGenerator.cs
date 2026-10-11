using System;
using System.Security.Cryptography;
using System.Text;

namespace SOLUM_UI.Services
{
    /// <summary>
    /// Generates cryptographically secure, readable temporary passwords
    /// compliant with enterprise and ASP.NET Core Identity complexity requirements
    /// (Uppercase, Lowercase, Digits, and Special characters).
    /// </summary>
    public static class PasswordGenerator
    {
        private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // Excludes ambiguous 'I', 'O'
        private const string Lowercase = "abcdefghijkmnopqrstuvwxyz"; // Excludes ambiguous 'l'
        private const string Digits = "23456789";                   // Excludes ambiguous '0', '1'
        private const string Specials = "!@#$%&*+?";
        private static readonly string AllChars = Uppercase + Lowercase + Digits + Specials;

        public static string Generate(int length = 10)
        {
            if (length < 8) length = 8;

            var chars = new char[length];
            var randomBytes = new byte[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            // Guarantee at least 1 of each required character category
            chars[0] = Uppercase[randomBytes[0] % Uppercase.Length];
            chars[1] = Lowercase[randomBytes[1] % Lowercase.Length];
            chars[2] = Digits[randomBytes[2] % Digits.Length];
            chars[3] = Specials[randomBytes[3] % Specials.Length];

            // Fill remaining characters
            for (int i = 4; i < length; i++)
            {
                chars[i] = AllChars[randomBytes[i] % AllChars.Length];
            }

            // Cryptographic Fisher-Yates shuffle so required characters aren't in predictable positions
            using (var rng = RandomNumberGenerator.Create())
            {
                for (int i = length - 1; i > 0; i--)
                {
                    byte[] box = new byte[1];
                    do { rng.GetBytes(box); } while (box[0] >= (256 - (256 % (i + 1))));
                    int j = box[0] % (i + 1);
                    char temp = chars[i];
                    chars[i] = chars[j];
                    chars[j] = temp;
                }
            }

            return new string(chars);
        }
    }
}
