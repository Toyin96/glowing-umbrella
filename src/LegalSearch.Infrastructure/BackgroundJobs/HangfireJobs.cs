using Hangfire;
using LegalSearch.Application.Interfaces.BackgroundService;

namespace LegalSearch.Infrastructure.BackgroundJobs
{

    public static class HangfireJobs
    {
        public static void RegisterRecurringJobs()
        {
             RecurringJob.AddOrUpdate<IBackgroundService>("CheckAndRerouteRequestsJob", x => x.CheckAndRerouteRequestsJob(), Cron.Minutely);
             RecurringJob.AddOrUpdate<IBackgroundService>("NotificationReminderForUnAttendedRequestsJob", x => x.NotificationReminderForUnAttendedRequestsJob(), Cron.Hourly);
        }
    }
}
