using System;

namespace AeFinder;

public class DateTimeHelper
{
    public static long ToUnixTimeMilliseconds(DateTime value)
    {
        var span = value - DateTime.UnixEpoch;
        return (long) span.TotalMilliseconds;
    }

    public static DateTime FromUnixTimeMilliseconds(long value)
    {
        return DateTime.UnixEpoch.AddMilliseconds(value);
    }

    public static DateTime AddHours(DateTime value, long hours)
    {
        return (DateTime.MaxValue - value).TotalHours <= hours
            ? DateTime.MaxValue
            : value.AddHours(hours);
    }
}