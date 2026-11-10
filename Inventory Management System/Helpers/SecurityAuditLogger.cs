using Inventory_Management_System.Models;
using Inventory_Management_System.Services;
using System;
using System.Collections.Generic;
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

        /// Maximum size (in bytes) before the log file is rotated.
        /// Default: ~5 MB — keeps the file manageable even after years of use.
        private const long MaxLogSizeBytes = 5 * 1024 * 1024;

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

                // Rotate if the file exceeds the size limit
                RotateIfNeeded();

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

        /// <summary>
        /// Returns the most recent N lines from the security audit log,
        /// newest first. Useful for displaying audit history in the UI.
        /// </summary>
        public static List<string> GetRecentEntries(int count = 100)
        {
            try
            {
                if (!File.Exists(LogPath)) return new List<string>();

                var lines = File.ReadAllLines(LogPath);
                return lines
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Reverse()
                    .Take(count)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Rotates the log file when it exceeds MaxLogSizeBytes.
        /// Renames current file to security_audit_old.log (overwriting any
        /// previous archive), keeping at most two generations on disk.
        /// </summary>
        private static void RotateIfNeeded()
        {
            try
            {
                if (!File.Exists(LogPath)) return;
                var info = new FileInfo(LogPath);
                if (info.Length < MaxLogSizeBytes) return;

                string archivePath = Path.Combine(
                    Path.GetDirectoryName(LogPath)!,
                    "security_audit_old.log");

                // Delete previous archive, then rename current to archive
                if (File.Exists(archivePath)) File.Delete(archivePath);
                File.Move(LogPath, archivePath);
            }
            catch
            {
                // Best-effort rotation — never let this block actual logging
            }
        }
    }
}