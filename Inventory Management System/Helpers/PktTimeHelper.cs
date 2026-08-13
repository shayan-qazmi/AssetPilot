// Helpers/PktTimeHelper.cs
using System;

namespace Inventory_Management_System.Helpers
{
    public static class PktTimeHelper
    {
        private static readonly TimeZoneInfo PktZone = Resolve();

        private static TimeZoneInfo Resolve()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time"); }
            catch
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi"); }
                catch
                {
                    return TimeZoneInfo.CreateCustomTimeZone(
                        "PKT_Fallback", TimeSpan.FromHours(5), "Pakistan Standard Time", "PKT");
                }
            }
        }

        /// Formats a UTC timestamp as "13 Aug 2026, 03:45 PM" in PKT. Returns "—" if null.
        public static string Format(DateTime? utc)
        {
            if (!utc.HasValue) return "—";
            var asUtc = utc.Value.Kind == DateTimeKind.Utc
                ? utc.Value
                : DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(asUtc, PktZone).ToString("dd MMM yyyy, hh:mm tt");
        }
    }
}