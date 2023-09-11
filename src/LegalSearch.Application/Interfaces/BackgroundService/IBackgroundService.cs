using LegalSearch.Application.Models.Requests.CSO;
using LegalSearch.Domain.Entities.LegalRequest;

namespace LegalSearch.Application.Interfaces.BackgroundService
{
    public interface IBackgroundService
    {
        Task AssignRequestToSolicitorsJob(Guid requestId);
        Task ManuallyAssignRequestToSolicitorJob(Guid requestId, Guid solicitorId);
        Task GenerateDailySummaryForZonalServiceManagers();
        Task PushBackRequestToCSOJob(Guid requestId);
        Task InitiatePaymentToSolicitorJob(Guid requestId);
        Task CheckAndRerouteRequestsJob();
        Task RequestEscalationJob(EscalateRequest request);
        Task NotificationReminderForUnAttendedRequestsJob();
        Task PushRequestToNextSolicitorInOrder(Guid requestId, int currentAssignedSolicitorOrder = 0);
    }
}
