namespace LegalSearch.Domain.Enums.Notification
{
    public enum NotificationType
    {
        NewRequest = 1,
        RequestPendingWithSolicitor = 2,
        RequestWithElapsedSLA = 3,
        RequestPendingWithCso = 4,
        ManualSolicitorAssignment = 5,
        CompletedRequest = 6,
        UnAssignedRequest = 7
    }
}
