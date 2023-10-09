using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Responses;
using Microsoft.AspNetCore.Identity;

namespace LegalSearch.Infrastructure.Services.Notification
{
    internal class InAppNotificationService : IInAppNotificationService
    {
        private readonly INotificationManager _notificationManager;
        private readonly UserManager<Domain.Entities.User.User> _userManager;

        public InAppNotificationService(INotificationManager notificationManager, UserManager<Domain.Entities.User.User> userManager)
        {
            _notificationManager = notificationManager;
            _userManager = userManager;
        }


        public async Task<ListResponse<NotificationResponse>> GetNotificationsForUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return new ListResponse<NotificationResponse>("Something went wrong. Please try again", ResponseCodes.Forbidden);

            var roles = await _userManager.GetRolesAsync(user);

            var notifications = await _notificationManager.GetPendingNotificationsForUser(userId, roles[0], user.SolId);

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
