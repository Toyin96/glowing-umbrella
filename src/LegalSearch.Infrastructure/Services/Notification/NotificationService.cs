using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.Notification;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalSearch.Infrastructure.Services.Notification
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _appDbContext;

        public NotificationService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }
        public Task<bool> AddMultipleNotifications(List<Domain.Entities.Notification.Notification> requests)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AddNotification(Domain.Entities.Notification.Notification notification)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Domain.Entities.Notification.Notification>> GetPendingNotificationsForRole(string role)
        {
            var pendingNotifications = await _appDbContext.Notifications
                                                          .Where(n => n.IsRead == false 
                                                          && n.RecipientRole == role && n.IsBroadcast)
                                                          .ToListAsync();

            return pendingNotifications ?? Enumerable.Empty<Domain.Entities.Notification.Notification>();
        }

        public async Task<List<NotificationResponse>> GetPendingNotificationsForUser(string userId)
        {
            return await _appDbContext.Notifications
                                   .Where(n => n.RecipientUserId == userId && !n.IsRead)
                                   .Select(x => new NotificationResponse
                                   {
                                       Title = x.Title,
                                       NotificationType = x.NotificationType,
                                       RecipientUserId = userId,
                                       Message = x.Message,
                                       IsRead = x.IsRead,
                                       MetaData = x.MetaData    
                                   }).ToListAsync();
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
