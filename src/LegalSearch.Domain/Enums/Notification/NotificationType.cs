namespace LegalSearch.Domain.Enums.Notification
{
    public enum NotificationType
    {
        NewRequest = 1,
        AssignedToSolicitor = 2,
        RequestWithElapsedSLA = 3,
        RequestReturnedToCso = 4,
        ManualSolicitorAssignment = 5,
        CompletedRequest = 6,
        UnAssignedRequest = 7
    }
}
