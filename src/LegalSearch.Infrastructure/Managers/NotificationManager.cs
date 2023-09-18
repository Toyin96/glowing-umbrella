using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Domain.Entities.Notification;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Infrastructure.Services.Notification
{
    public class NotificationManager : INotificationManager
    {
        private readonly AppDbContext _appDbContext;

        public NotificationManager(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }
        public async Task<bool> AddMultipleNotifications(List<Domain.Entities.Notification.Notification> requests)
        {
            await _appDbContext.Notifications.AddRangeAsync(requests);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> AddNotification(Domain.Entities.Notification.Notification notification)
        {
            await _appDbContext.Notifications.AddAsync(notification);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<Domain.Entities.Notification.Notification>> GetPendingNotificationsForRole(string role)
        {
            var pendingNotifications = await _appDbContext.Notifications
                                                          .Where(n => !n.IsRead
                                                          && n.RecipientRole == role && n.IsBroadcast)
                                                          .ToListAsync();

            return pendingNotifications ?? Enumerable.Empty<Domain.Entities.Notification.Notification>();
        }

        public async Task<IEnumerable<Domain.Entities.Notification.Notification>> GetPendingNotificationsForUser(string userId)
        {
            var pendingNotifications = await _appDbContext.Notifications
                                   .Where(n => n.RecipientUserId == userId && !n.IsRead)
                                   .ToListAsync();

            return pendingNotifications ?? Enumerable.Empty<Domain.Entities.Notification.Notification>();

        }

        public async Task<bool> MarkAllNotificationAsRead(string userId)
        {
            var notifications = await _appDbContext.Notifications.Where(x => x.RecipientUserId == userId).ToListAsync();
            if (notifications != null)
            {
                notifications.ForEach(x => x.IsRead = true);
                return await _appDbContext.SaveChangesAsync() > 0;
            }
            return false;
        }

        public async Task<bool> MarkNotificationAsRead(Guid id)
        {
            var notification = await _appDbContext.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                return await _appDbContext.SaveChangesAsync() > 0;
            }
            return false;
        }
    }
}
