using System;

namespace ZynstormECFPlatform.Common;

public static class DateTimeExtensions
{
    private static readonly TimeZoneInfo DrTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Western Standard Time");

    /// <summary>
    /// Converts a UTC DateTime to Dominican Republic local time.
    /// </summary>
    public static DateTime ToDrTime(this DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Unspecified)
        {
            dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }
        else if (dateTime.Kind == DateTimeKind.Local)
        {
            dateTime = dateTime.ToUniversalTime();
        }

        return TimeZoneInfo.ConvertTimeFromUtc(dateTime, DrTimeZone);
    }

    /// <summary>
    /// Returns the current local time in Dominican Republic.
    /// </summary>
    public static DateTime DrNow => ToDrTime(DateTime.UtcNow);
}
