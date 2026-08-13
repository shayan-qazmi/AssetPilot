//Helpers/TimeHelper.cs
using System;

namespace Inventory_Management_System.Helpers
{
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo PakistanTz = ResolvePakistanTimeZone();

        private static TimeZoneInfo ResolvePakistanTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time"); }
            catch
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi"); }
                catch { return TimeZoneInfo.CreateCustomTimeZone("PKT", TimeSpan.FromHours(5), "Pakistan Standard Time", "PKT"); }
            }
        }

        public static DateTime ToPakistanTime(DateTime dbTime)
        {
            var utc = DateTime.SpecifyKind(dbTime, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, PakistanTz);
        }

        public static DateTime? ToPakistanTime(DateTime? dbTime) => dbTime.HasValue ? ToPakistanTime(dbTime.Value) : null;

        public static DateTime NowPakistan() => ToPakistanTime(DateTime.UtcNow);
    }
}