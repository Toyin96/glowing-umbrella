namespace LegalSearch.Infrastructure.Utilities
{
    public static class TimeUtils
    {
        public static DateTime GetCurrentLocalTime() => DateTime.UtcNow.AddHours(1);
        public static DateTime CalculateDateDueForRequest() => DateTime.UtcNow.AddDays(3);
        public static DateTime GetTwentyFourHoursElapsedTime() => DateTime.UtcNow.AddDays(-1);
        public static DateTime GetSeventyTwoHoursElapsedTime() => DateTime.UtcNow.AddDays(-3);


        // Cron Expressions
        public static string GetCronExpressionFor10pmDailyWAT => "0 22 * * *";
    }
}
