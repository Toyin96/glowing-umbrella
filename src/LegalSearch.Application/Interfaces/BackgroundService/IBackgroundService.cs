using LegalSearch.Application.Models.Requests.CSO;
using LegalSearch.Application.Models.Requests.User;

namespace LegalSearch.Application.Interfaces.BackgroundService
{
    public interface IBackgroundService
    {
        Task AssignRequestToSolicitorsJob(Guid requestId);
        Task ManuallyAssignRequestToSolicitorJob(Guid requestId, UserMiniDto solicitorInfo);
        Task GenerateDailySummaryForZonalServiceManagers();
        Task PushBackRequestToCSOJob(Guid requestId);
        Task InitiatePaymentToSolicitorJob(Guid requestId);
        Task CheckAndRerouteRequestsJob();
        Task RequestEscalationJob(EscalateRequest request);
        Task NotificationReminderForUnAttendedRequestsJob();
        Task PushRequestToNextSolicitorInOrder(Guid requestId, int currentAssignedSolicitorOrder = 0);
    }
}
