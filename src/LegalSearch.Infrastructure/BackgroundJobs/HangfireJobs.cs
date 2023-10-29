using Hangfire;
using LegalSearch.Application.Interfaces.BackgroundService;
using LegalSearch.Infrastructure.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace LegalSearch.Infrastructure.BackgroundJobs
{
    [ExcludeFromCodeCoverage]
    public static class HangfireJobs
    {
        public static void RegisterRecurringJobs()
        {
            RecurringJob.AddOrUpdate<IBackgroundService>("CheckAndRerouteRequestsJob", x => x.CheckAndRerouteRequestsJob(), Cron.Hourly);
            RecurringJob.AddOrUpdate<IBackgroundService>("NotificationReminderForUnAttendedRequestsJob", x => x.NotificationReminderForUnAttendedRequestsJob(), Cron.Minutely);
            RecurringJob.AddOrUpdate<IBackgroundService>("GenerateDailySummaryForZonalServiceManagers", x => x.GenerateDailySummaryForZonalServiceManagers(), TimeUtils.GetCronExpressionForElevenThirtyPmDailyWAT);
            RecurringJob.AddOrUpdate<IBackgroundService>("GenerateDailySummaryForCustomerServiceManagers", x => x.GenerateDailySummaryForCustomerServiceManagers(), TimeUtils.GetCronExpressionForElevenFourtyPmDailyWAT);
            RecurringJob.AddOrUpdate<IBackgroundService>("RetryFailedLegalSearchRequestSettlementToSolicitor", x => x.RetryFailedLegalSearchRequestSettlementToSolicitor(), Cron.Hourly);
        }
    }
}
