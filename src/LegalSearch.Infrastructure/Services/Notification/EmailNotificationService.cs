using LegalSearch.Application.Interfaces.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalSearch.Infrastructure.Services.Notification
{
    internal class EmailNotificationService : INotificationService
    {
        public Task SendNotificationToRole(string roleName, Domain.Entities.Notification.Notification notification)
        {
            throw new NotImplementedException();
        }

        public Task SendNotificationToUser(Guid userId, Domain.Entities.Notification.Notification notification)
        {
            throw new NotImplementedException();
        }
    }
}
