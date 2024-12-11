namespace AeFinder.Grains;

public static class DateTimeExtension
{
    public static DateTime ToMonthDate(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(-dateTime.Day + 1);
    }
}