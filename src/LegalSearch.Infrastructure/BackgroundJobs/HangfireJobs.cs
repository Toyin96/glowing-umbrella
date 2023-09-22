using Hangfire;
using LegalSearch.Application.Interfaces.BackgroundService;
using LegalSearch.Infrastructure.Utilities;

namespace LegalSearch.Infrastructure.BackgroundJobs
{

    public static class HangfireJobs
    {
        public static void RegisterRecurringJobs()
        {
            RecurringJob.AddOrUpdate<IBackgroundService>("CheckAndRerouteRequestsJob", x => x.CheckAndRerouteRequestsJob(), Cron.Hourly);
            RecurringJob.AddOrUpdate<IBackgroundService>("NotificationReminderForUnAttendedRequestsJob", x => x.NotificationReminderForUnAttendedRequestsJob(), Cron.Minutely); // PS: this guy is rerouting requests. check it!
            RecurringJob.AddOrUpdate<IBackgroundService>("GenerateDailySummaryForZonalServiceManagers", x => x.GenerateDailySummaryForZonalServiceManagers(), TimeUtils.GetCronExpressionFor10pmDailyWAT);
        }
    }
}
