using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Enums.Role;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LegalSearch.Infrastructure.Services.Notification
{
    public class NotificationManager : INotificationManager
    {
        private readonly AppDbContext _appDbContext;
        private readonly IServiceProvider _serviceProvider;

        public NotificationManager(AppDbContext appDbContext, IServiceProvider serviceProvider)
        {
            _appDbContext = appDbContext;
            _serviceProvider = serviceProvider;
        }
        public async Task<bool> AddMultipleNotifications(List<Domain.Entities.Notification.Notification> requests)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Notifications.AddRange(requests);
                return await dbContext.SaveChangesAsync() > 0;
            }
        }

        public async Task<bool> AddNotification(Domain.Entities.Notification.Notification notification)
        {
            await _appDbContext.Notifications.AddAsync(notification);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<NotificationResponse>> GetPendingNotificationsForRole(string role)
        {
            var pendingNotifications = await _appDbContext.Notifications
                                                          .Where(n => !n.IsRead
                                                          && n.RecipientRole == role && n.IsBroadcast)
                                                          .Select(x => new NotificationResponse
                                                          {
                                                              NotificationId = x.Id,
                                                              Title = x.Title,
                                                              NotificationType = x.NotificationType,
                                                              RecipientUserId = x.RecipientUserId,
                                                              Message = x.Message,
                                                              DateCreated = x.CreatedAt,
                                                              IsRead = x.IsRead,
                                                              MetaData = x.MetaData
                                                          })
                                                          .OrderByDescending(x => x.DateCreated)
                                                          .ToListAsync();

            return pendingNotifications ?? Enumerable.Empty<NotificationResponse>();
        }

        public async Task<IEnumerable<NotificationResponse>> GetPendingNotificationsForUser(string userId, string role, string? solId)
        {
            IQueryable<Domain.Entities.Notification.Notification> query = _appDbContext.Notifications;

            if (role == RoleType.Solicitor.ToString())
            {
                query = query.Where(n => n.RecipientUserId == userId && !n.IsRead);
            }
            else if (role == RoleType.Cso.ToString())
            {
                query = query.Where(n => n.IsBroadcast && n.RecipientRole == role && !n.IsRead && n.SolId == solId);
            }
            else
            {
                query = query.Where(n => n.IsBroadcast && n.RecipientRole == role && !n.IsRead);
            }

            var pendingNotifications = await query.Select(x => new NotificationResponse
            {
                NotificationId = x.Id,
                Title = x.Title,
                NotificationType = x.NotificationType,
                RecipientUserId = x.RecipientUserId,
                Message = x.Message,
                DateCreated = x.CreatedAt,
                IsRead = x.IsRead,
                MetaData = x.MetaData
            })
            .OrderByDescending(x => x.DateCreated)
            .ToListAsync();

            return pendingNotifications ?? Enumerable.Empty<NotificationResponse>();
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
