using System;

namespace ZynstormECFPlatform.Common;

public static class DateTimeExtensions
{
    private static readonly string[] DominicanTimeZoneIds = new[] {
        "America/Santo_Domingo", 
        "SA Western Standard Time" 
    };

    public static DateTime ConvertUtcToDominicanLocal(this DateTime date)
    {
        foreach (var timeZoneId in DominicanTimeZoneIds)
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(date, DateTimeKind.Utc), timeZone);
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return date.ToLocalTime();
    }

    public static DateTime ConvertDominicanLocalToUtc(this DateTime date)
    {
        foreach (var timeZoneId in DominicanTimeZoneIds)
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var utcDate = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(date, DateTimeKind.Unspecified), timeZone);
                return DateTime.SpecifyKind(utcDate, DateTimeKind.Unspecified);
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return DateTime.SpecifyKind(date.ToUniversalTime(), DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Converts a UTC DateTime to Dominican Republic local time.
    /// </summary>
    public static DateTime ToDrTime(this DateTime dateTime)
    {
        return dateTime.ConvertUtcToDominicanLocal();
    }

    /// <summary>
    /// Returns the current local time in Dominican Republic.
    /// </summary>
    public static DateTime DrNow => DateTime.UtcNow.ConvertUtcToDominicanLocal();
}
