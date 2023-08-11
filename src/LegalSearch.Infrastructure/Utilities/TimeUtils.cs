namespace LegalSearch.Infrastructure.Utilities
{
    public static class TimeUtils
    {
        public static DateTime GetCurrentLocalTime() => DateTime.UtcNow.AddHours(1);
        public static DateTime GetTwentyHoursElapsedTime() => DateTime.UtcNow.AddHours(-24);
        public static DateTime GetSeventyTwoHoursElapsedTime() => DateTime.UtcNow.AddHours(-72);
    }
}
