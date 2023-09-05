using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Text.Json;

namespace LegalSearch.Infrastructure.Services.Notification
{
    public class NotificationHub : Hub, INotificationService
    {
        private readonly INotificationManager _notificationService;
        private readonly UserManager<Domain.Entities.User.User> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly Dictionary<string, List<Domain.Entities.Notification.Notification>> _pendingNotifications = new Dictionary<string, List<Domain.Entities.Notification.Notification>>();

        public NotificationHub(INotificationManager notificationService,
            UserManager<Domain.Entities.User.User> userManager, 
            IJwtTokenService jwtTokenService)
        {
            _notificationService = notificationService;
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
        }

        // Method to send notifications to connected clients on a particular role
        public async Task SendNotificationToRole(string roleName, Domain.Entities.Notification.Notification notification)
        {
            var jsonNotification = JsonSerializer.Serialize(notification);
            await Clients.Group(roleName).SendAsync("ReceiveNotification", jsonNotification);
        }

        public async Task SendNotificationToUser(Guid userId, Domain.Entities.Notification.Notification notification)
        {
            var jwtToken = Context.GetHttpContext()?.Request?.Headers["Authorization"].ToString()?.Replace("Bearer ", "");

            var principal = _jwtTokenService.ValidateJwtToken(jwtToken);

            string? id = principal.FindFirst(nameof(ClaimType.UserId))?.Value?.ToString();

            if (userId.ToString() != id)
            {
                // User is not logged in, store the notification for later retrieval
                await StorePendingNotification(userId.ToString(), notification);
            }
            else
            {
                // User is authenticated and connected, send the notification immediately
                var jsonNotification = JsonSerializer.Serialize(notification);
                await Clients.User(userId.ToString()).SendAsync("ReceiveNotification", jsonNotification);
            }
        }

        private async Task StorePendingNotification(string userId, Domain.Entities.Notification.Notification notification)
        {
            if (!_pendingNotifications.ContainsKey(userId))
            {
                _pendingNotifications[userId] = new List<Domain.Entities.Notification.Notification>();
            }
            _pendingNotifications[userId].Add(notification);

            // Store the pending notification in the database
            await _notificationService.AddNotification(notification);
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;

            // Retrieve the user's role
            var userRole = Context.User!.Claims.SingleOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            if (!string.IsNullOrEmpty(userRole))
            {
                // Retrieve pending notifications for the connected user's role
                var pendingNotificationsForRole = await _notificationService.GetPendingNotificationsForRole(userRole);

                // Retrieve pending notifications for the connected user individually
                var pendingNotificationsForUser = await _notificationService.GetPendingNotificationsForUser(userId);

                // Combine and send both sets of pending notifications to the connected user
                var pendingNotificationsForRoleList = pendingNotificationsForRole.ToList();
                var allPendingNotifications = pendingNotificationsForRoleList.Concat(pendingNotificationsForUser).ToList();
                foreach (var notification in allPendingNotifications)
                {
                    var jsonNotification = JsonSerializer.Serialize(notification);
                    await Clients.User(userId).SendAsync("ReceiveNotification", jsonNotification);
                }

                // Clear pending notifications for the connected user
                await _notificationService.MarkAllNotificationAsRead(userId);
            }

            await base.OnConnectedAsync();
        }
    }
}
