// Services/DbConnectionSettingsService.cs
using Inventory_Management_System.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Inventory_Management_System.Services
{
    public static class DbConnectionSettingsService
    {
        private static readonly string FolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QInventory");

        private static readonly string FilePath = Path.Combine(FolderPath, "dbconnection.json");

        public static bool SettingsExist() => File.Exists(FilePath);

        public static DbConnectionSettings Load()
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<DbConnectionSettings>(json) ?? new DbConnectionSettings();
        }

        public static void Save(DbConnectionSettings settings, string plainPassword)
        {
            Directory.CreateDirectory(FolderPath);
            settings.EncryptedPassword = Encrypt(plainPassword);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static string DecryptPassword(DbConnectionSettings settings) => Decrypt(settings.EncryptedPassword);

        public static string BuildConnectionString(DbConnectionSettings settings, string plainPassword) =>
            $"server={settings.Server};port={settings.Port};database={settings.Database};user={settings.Username};password={plainPassword};";

        /// One-time migration: if no settings file exists yet (first run after
        /// this update), seed it from what used to be hardcoded in
        /// InventoryDbContext, so existing installs don't break on upgrade.
        public static void SeedIfMissing(string server, int port, string database, string username, string password, string serverVersion)
        {
            if (SettingsExist()) return;
            Save(new DbConnectionSettings
            {
                Server = server,
                Port = port,
                Database = database,
                Username = username,
                ServerVersion = serverVersion
            }, password);
        }

        /// Uses EF Core's own Database.CanConnect() instead of a separate
        /// MySqlConnector dependency, so no extra NuGet package is required.
        public static bool TestConnection(DbConnectionSettings settings, string plainPassword, out string error)
        {
            error = "";
            try
            {
                var connStr = BuildConnectionString(settings, plainPassword);
                var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Models.InventoryDbContext>();
                optionsBuilder.UseMySql(connStr, Microsoft.EntityFrameworkCore.ServerVersion.Parse(settings.ServerVersion));

                using var db = new Models.InventoryDbContext(optionsBuilder.Options);
                return db.Database.CanConnect();
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string Encrypt(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return "";
            var bytes = Encoding.UTF8.GetBytes(plain);
            return Convert.ToBase64String(ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser));
        }

        private static string Decrypt(string cipher)
        {
            if (string.IsNullOrEmpty(cipher)) return "";
            var bytes = Convert.FromBase64String(cipher);
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser));
        }
    }
}