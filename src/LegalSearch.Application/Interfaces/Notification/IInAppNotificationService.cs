using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Responses;

namespace LegalSearch.Application.Interfaces.Notification
{
    public interface IInAppNotificationService
    {
        Task<StatusResponse> MarkNotificationAsRead(Guid notificationId);
        Task<StatusResponse> MarkAllNotificationsAsRead(string userId);
        Task<ListResponse<NotificationResponse>> GetNotificationsForUser(string userId);
    }
}
