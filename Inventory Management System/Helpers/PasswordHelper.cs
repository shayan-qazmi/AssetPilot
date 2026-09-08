using DocumentFormat.OpenXml.Math;

namespace Inventory_Management_System.Helpers
{
    public static class PasswordHelper
    {
        public static string Hash(string plainPassword) =>
            BCryptNet.BCrypt.HashPassword(plainPassword);

        public static bool IsBcryptHash(string? storedValue) =>
            !string.IsNullOrEmpty(storedValue) &&
            (storedValue.StartsWith("$2a$") || storedValue.StartsWith("$2b$") || storedValue.StartsWith("$2y$"));

        /// Verifies against a bcrypt hash, or — for accounts not yet
        /// migrated — a legacy plaintext value already sitting in PasswordHash.
        public static bool Verify(string plainPassword, string? storedValue)
        {
            if (string.IsNullOrEmpty(storedValue)) return false;

            if (IsBcryptHash(storedValue))
            {
                try { return BCryptNet.BCrypt.Verify(plainPassword, storedValue); }
                catch { return false; }
            }

            return storedValue == plainPassword; // legacy plaintext account
        }
    }
}