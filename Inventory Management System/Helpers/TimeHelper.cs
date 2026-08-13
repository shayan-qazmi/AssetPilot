//Helpers / TimeHelper.cs
using Inventory_Management_System.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Inventory_Management_System.Helpers
{
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo PakistanTz = ResolvePakistanTimeZone();
        private static TimeSpan? _dbToUtcOffset; // how far the DB's stored time is BEHIND true UTC

        private static TimeZoneInfo ResolvePakistanTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time"); }
            catch
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi"); }
                catch { return TimeZoneInfo.CreateCustomTimeZone("PKT", TimeSpan.FromHours(5), "Pakistan Standard Time", "PKT"); }
            }
        }

        // Asks the DB itself how far its stored timestamps are from true UTC,
        // instead of assuming — the server's SYSTEM time_zone isn't always UTC.
        private static TimeSpan GetDbToUtcOffset()
        {
            if (_dbToUtcOffset.HasValue) return _dbToUtcOffset.Value;
            try
            {
                using var db = new InventoryDbContext();
                var row = db.Database.SqlQueryRaw<DbTimeCheck>(
                    "SELECT NOW() AS DbNow, UTC_TIMESTAMP() AS DbUtcNow").First();
                _dbToUtcOffset = row.DbUtcNow - row.DbNow; // e.g. +4h if DB stores UTC-4
            }
            catch { _dbToUtcOffset = TimeSpan.Zero; } // fall back to "assume already UTC" if the check fails
            return _dbToUtcOffset.Value;
        }

        public static DateTime ToPakistanTime(DateTime dbTime)
        {
            var trueUtc = dbTime + GetDbToUtcOffset();
            trueUtc = DateTime.SpecifyKind(trueUtc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(trueUtc, PakistanTz);
        }

        public static DateTime? ToPakistanTime(DateTime? dbTime) => dbTime.HasValue ? ToPakistanTime(dbTime.Value) : null;

        public static DateTime NowPakistan() => ToPakistanTime(DateTime.UtcNow - GetDbToUtcOffset());

        private class DbTimeCheck
        {
            public DateTime DbNow { get; set; }
            public DateTime DbUtcNow { get; set; }
        }
    }
}