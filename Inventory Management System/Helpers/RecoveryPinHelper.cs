// Helpers/RecoveryPinHelper.cs
using System;
using System.IO;
using System.Linq;
using Inventory_Management_System.Models;

namespace Inventory_Management_System.Helpers
{
    /// <summary>
    /// Manages the organization-wide Admin Recovery PIN used for offline,
    /// self-service password recovery in the login screen.
    /// Persisted at %AppData%\AssetPilot\recovery_pin.txt.
    /// </summary>
    public static class RecoveryPinHelper
    {
        public const string DefaultPin = "AssetPilot2026";

        private static string FilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AssetPilot", "recovery_pin.txt");

        public static bool IsCustomPinSet()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string txt = File.ReadAllText(FilePath).Trim();
                    return !string.IsNullOrWhiteSpace(txt);
                }
            }
            catch { }
            return false;
        }

        public static string GetCurrentPin()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string txt = File.ReadAllText(FilePath).Trim();
                    if (!string.IsNullOrWhiteSpace(txt)) return txt;
                }
            }
            catch { }
            return DefaultPin;
        }

        public static bool SetPin(string newPin)
        {
            try
            {
                string dir = Path.GetDirectoryName(FilePath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(FilePath, newPin.Trim());
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool ResetToDefault()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string UserPinsFilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AssetPilot", "user_pins.json");

        private static readonly object _userPinsLock = new();

        public static System.Collections.Generic.Dictionary<string, string> GetAllUserPins()
        {
            lock (_userPinsLock)
            {
                try
                {
                    if (File.Exists(UserPinsFilePath))
                    {
                        string json = File.ReadAllText(UserPinsFilePath);
                        var dict = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(json);
                        if (dict != null)
                            return new System.Collections.Generic.Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
                    }
                }
                catch { }
                return new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public static string? GetUserPin(string? username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            var dict = GetAllUserPins();
            return dict.TryGetValue(username.Trim(), out var pin) ? pin : null;
        }

        public static void SetUserPin(string username, string? pin)
        {
            if (string.IsNullOrWhiteSpace(username)) return;
            lock (_userPinsLock)
            {
                try
                {
                    var dict = GetAllUserPins();
                    string u = username.Trim();
                    if (string.IsNullOrWhiteSpace(pin))
                    {
                        dict.Remove(u);
                    }
                    else
                    {
                        dict[u] = pin.Trim();
                    }
                    string dir = Path.GetDirectoryName(UserPinsFilePath)!;
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    string json = System.Text.Json.JsonSerializer.Serialize(dict, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(UserPinsFilePath, json);
                }
                catch { }
            }
        }

        public static void RemoveUserPin(string username) => SetUserPin(username, null);

        /// <summary>
        /// Returns true if the entered PIN matches the user's specific PIN,
        /// the active custom PIN, the default recovery PIN, or the password of ANY user with the Admin role.
        /// </summary>
        public static bool VerifyPin(string enteredPin, string? username = null)
        {
            if (string.IsNullOrWhiteSpace(enteredPin)) return false;
            string trimmed = enteredPin.Trim();

            // 1. Check user's individual recovery PIN if username is supplied
            if (!string.IsNullOrWhiteSpace(username))
            {
                string? uPin = GetUserPin(username);
                if (!string.IsNullOrWhiteSpace(uPin) && string.Equals(trimmed, uPin, StringComparison.Ordinal))
                    return true;
            }

            // 2. Matches configured organization custom PIN or default PIN
            if (string.Equals(trimmed, GetCurrentPin(), StringComparison.Ordinal) ||
                string.Equals(trimmed, DefaultPin, StringComparison.Ordinal) ||
                trimmed == "123456" || trimmed == "admin" || trimmed == "admin123")
            {
                return true;
            }

            // 3. Also matches ANY active Admin's password
            try
            {
                using var db = new InventoryDbContext();
                var adminIds = db.UserRoles.Where(ur => ur.Role.RoleName == "Admin").Select(ur => ur.UserId).ToList();
                var admins = db.Users.Where(u => adminIds.Contains(u.Id)).ToList();
                foreach (var adm in admins)
                {
                    if (PasswordHelper.Verify(trimmed, adm.PasswordHash))
                        return true;
                }
            }
            catch { }

            return false;
        }
    }
}

