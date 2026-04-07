using System;

namespace ZynstormECFPlatform.Common;

public static class DateTimeExtensions
{
    private static readonly TimeZoneInfo DrTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Western Standard Time");

    /// <summary>
    /// Converts a UTC DateTime to Dominican Republic local time.
    /// </summary>
    public static DateTime ToDrTime(this DateTime utcDateTime)
    {
        if (utcDateTime.Kind == DateTimeKind.Unspecified)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, DrTimeZone);
    }

    /// <summary>
    /// Returns the current local time in Dominican Republic.
    /// </summary>
    public static DateTime DrNow => ToDrTime(DateTime.UtcNow);
}
