using LegalSearch.Application.Models.Responses;
using System;

namespace LegalSearch.Application.Interfaces.Notification
{
    public interface INotificationManager
    {
        Task<bool> AddNotification(Domain.Entities.Notification.Notification notification);
        Task<bool> AddMultipleNotifications(List<Domain.Entities.Notification.Notification> requests);
        Task<IEnumerable<NotificationResponse>> GetPendingNotificationsForRole(string role);
        Task<IEnumerable<NotificationResponse>> GetPendingNotificationsForUser(string userId, string role, string? solId);
        Task<bool> MarkNotificationAsRead(Guid id);
        Task<bool> MarkAllNotificationAsRead(string userId);
    }
}
