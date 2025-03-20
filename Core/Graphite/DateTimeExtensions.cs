namespace Kontur.TestAnalytics.Core.Graphite;

internal static class DateTimeExtensions
{
    public static long ToEpochTime(this DateTime dateTime)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (long)(dateTime.ToUniversalTime() - epoch).TotalSeconds;
    }
}
