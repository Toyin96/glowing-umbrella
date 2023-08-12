namespace LegalSearch.Application.Interfaces.BackgroundService
{
    public interface IBackgroundService
    {
        Task AssignRequestToSolicitorsJob(Guid requestId);
        Task PushBackRequestToCSOJob(Guid requestId);
        Task CheckAndRerouteRequestsJob();
        Task NotificationReminderForUnAttendedRequestsJob();
        Task PushRequestToNextSolicitorInOrder(Guid requestId, int currentAssignedSolicitorOrder = 0);
    }
}
