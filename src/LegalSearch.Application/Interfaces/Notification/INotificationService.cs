namespace LegalSearch.Application.Interfaces.Notification
{
    public interface INotificationService
    {
        Task SendNotificationToUser(Guid userId, Domain.Entities.Notification.Notification notification);
        Task SendNotificationToRole(string roleName, Domain.Entities.Notification.Notification notification);
    }
}
