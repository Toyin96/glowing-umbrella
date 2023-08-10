using LegalSearch.Application.Models.Responses;

namespace LegalSearch.Application.Interfaces.Notification
{
    public interface INotificationManager
    {
        Task<bool> AddNotification(Domain.Entities.Notification.Notification notification);
        Task<bool> AddMultipleNotifications(List<Domain.Entities.Notification.Notification> requests);
        Task<IEnumerable<Domain.Entities.Notification.Notification>> GetPendingNotificationsForRole(string role);
        Task<List<NotificationResponse>> GetPendingNotificationsForUser(string userId);
        Task<bool> MarkNotificationAsRead(Guid id);
        Task<bool> MarkAllNotificationAsRead(string userId);
    }
}
