namespace LegalSearch.Application.Interfaces.Notification
{
    public interface INotificationService
    {

        /// <summary>
        /// Notifies the user.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <returns></returns>
        Task NotifyUser(Domain.Entities.Notification.Notification notification);
        /// <summary>
        /// Notifies the users in role.
        /// </summary>
        /// <param name="roleName">Name of the role.</param>
        /// <param name="notification">The notification.</param>
        /// <param name="userEmails">The user emails.</param>
        /// <returns></returns>
        Task NotifyUsersInRole(string roleName, Domain.Entities.Notification.Notification notification, List<string?>? userEmails = null);
    }
}
