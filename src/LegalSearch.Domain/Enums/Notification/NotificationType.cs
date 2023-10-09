namespace LegalSearch.Domain.Enums.Notification
{
    public enum NotificationType
    {
        NewRequest = 1,
        AssignedToSolicitor = 2,
        OutstandingRequestAfter24Hours = 3,
        RequestWithElapsedSLA = 4,
        RequestReturnedToCso = 5,
        ManualSolicitorAssignment = 6,
        CompletedRequest = 7,
        UnAssignedRequest = 8
    }
}
