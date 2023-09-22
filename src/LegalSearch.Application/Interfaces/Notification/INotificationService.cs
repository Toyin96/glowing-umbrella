using LegalSearch.Application.Models.Requests.User;

namespace LegalSearch.Application.Interfaces.Notification
{
    public interface INotificationService
    {
        /// <summary>
        /// Sends the notification to two users (i.e the initiator & the recipient).
        /// </summary>
        /// <param name="initiatorUserId">The user identifier.</param>
        /// <param name="notification">The notification.</param>
        /// <returns></returns>
        Task SendNotificationToUser(Guid initiatorUserId, Domain.Entities.Notification.Notification notification);
        Task SendNotificationToRole(string roleName, Domain.Entities.Notification.Notification notification, List<string?>? userEmails = null);
    }
}
