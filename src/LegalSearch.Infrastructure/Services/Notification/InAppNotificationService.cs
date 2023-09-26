using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Responses;

namespace LegalSearch.Infrastructure.Services.Notification
{
    internal class InAppNotificationService : IInAppNotificationService
    {
        private readonly INotificationManager _notificationManager;

        public InAppNotificationService(INotificationManager notificationManager)
        {
            _notificationManager = notificationManager;
        }


        public async Task<ListResponse<NotificationResponse>> GetNotificationsForUser(string userId)
        {
            var notifications = await _notificationManager.GetPendingNotificationsForUser(userId);

            return new ListResponse<NotificationResponse>("Operation was successful", ResponseCodes.Success)
            {
                Data = notifications.ToList(),
                Total = notifications.Count(),
            };
        }

        public async Task<StatusResponse> MarkAllNotificationsAsRead(string userId)
        {
            var isSuccessful = await _notificationManager.MarkAllNotificationAsRead(userId);

            if (!isSuccessful)
                return new StatusResponse("Something went wrong. Please try again later", ResponseCodes.BadRequest);

            return new StatusResponse("Operation was successful", ResponseCodes.Success);
        }

        public async Task<StatusResponse> MarkNotificationAsRead(Guid notificationId)
        {
            var isSuccessful = await _notificationManager.MarkNotificationAsRead(notificationId);

            if (!isSuccessful)
                return new StatusResponse("Something went wrong. Please try again later", ResponseCodes.BadRequest);

            return new StatusResponse("Operation was successful", ResponseCodes.Success);
        }
    }
}
