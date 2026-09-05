using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using System;
using System.IO;
using System.Linq;

namespace Inventory_Management_System.Helpers
{
    /// Append-only, DB-independent audit trail for destructive actions.
    /// Deliberately stored as a plain file (not a DB table) so it survives
    /// Clear Logs / Reset App Data and has no delete path in the app.
    public static class SecurityAuditLogger
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AssetPilot", "security_audit.log");

        public static void Log(string action, string details = "")
        {
            try
            {
                string who;
                try
                {
                    using var db = new InventoryDbContext();
                    var u = db.Users.FirstOrDefault(x => x.Id == AuthService.CurrentUserId);
                    who = u != null ? $"{u.FullName} (@{u.Username})" : $"UserId {AuthService.CurrentUserId}";
                }
                catch { who = $"UserId {AuthService.CurrentUserId}"; }

                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {who} | {action}" +
                              (string.IsNullOrWhiteSpace(details) ? "" : $" | {details}");
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SecurityAuditLogger.Log");
            }
        }

        public static string GetLogFilePath() => LogPath;
    }
}