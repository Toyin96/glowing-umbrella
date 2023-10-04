using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Domain.ApplicationMessages;
using LegalSearch.Domain.Enums.Role;
using LegalSearch.Infrastructure.Utilities;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;

namespace LegalSearch.Infrastructure.Services.Notification
{
    public class NotificationPersistenceService : INotificationService
    {
        private readonly INotificationManager _notificationManager;
        private readonly UserManager<Domain.Entities.User.User> _userManager;

        public NotificationPersistenceService(INotificationManager notificationManager, UserManager<Domain.Entities.User.User> userManager)
        {
            _notificationManager = notificationManager;
            _userManager = userManager;
        }
        public async Task NotifyUsersInRole(string roleName, Domain.Entities.Notification.Notification notification, List<string?>? userEmails = null)
        {
            var notifications = DetermineNotificationsToPersist(notification);

            await _notificationManager.AddMultipleNotifications(notifications);
        }

        public List<Domain.Entities.Notification.Notification> DetermineNotificationsToPersist(Domain.Entities.Notification.Notification notification)
        {
            var notifications = new List<Domain.Entities.Notification.Notification>() { notification};

            var initiatorNotification = new Domain.Entities.Notification.Notification();

            switch (notification.NotificationType)
            {
                case Domain.Enums.Notification.NotificationType.NewRequest:
                    break;
                case Domain.Enums.Notification.NotificationType.AssignedToSolicitor:
                    initiatorNotification.Title = ConstantTitle.NewRequestAssignmentTitleForSolicitor;
                    initiatorNotification.NotificationType = notification.NotificationType;
                    initiatorNotification.SolId = notification.SolId;
                    initiatorNotification.RecipientRole = nameof(RoleType.Cso);
                    initiatorNotification.IsBroadcast = true;
                    initiatorNotification.Message = ConstantMessage.AssignedRequestMessageForCso;
                    initiatorNotification.MetaData = notification.MetaData;
                    break;
                case Domain.Enums.Notification.NotificationType.OutstandingRequestAfter24Hours:
                    initiatorNotification.Title = ConstantTitle.ReminderNotificationTitleOnPendingAssignedRequestForCso;
                    initiatorNotification.NotificationType = notification.NotificationType;
                    initiatorNotification.SolId = notification.SolId;
                    initiatorNotification.RecipientRole = nameof(RoleType.Cso);
                    initiatorNotification.IsBroadcast = true;
                    initiatorNotification.Message = ConstantMessage.ReminderNotificationMessageOnPendingAssignedRequestForCso;
                    initiatorNotification.MetaData = notification.MetaData;
                    break;
                case Domain.Enums.Notification.NotificationType.RequestWithElapsedSLA:
                    break;
                case Domain.Enums.Notification.NotificationType.RequestReturnedToCso:
                    initiatorNotification.Title = ConstantTitle.AdditionalInformationNeededOnAssignedRequestTitle;
                    initiatorNotification.NotificationType = notification.NotificationType;
                    initiatorNotification.SolId = notification.SolId;
                    initiatorNotification.RecipientRole = nameof(RoleType.Cso);
                    initiatorNotification.IsBroadcast = true;
                    initiatorNotification.Message = ConstantMessage.RequestRoutedBackToCSOMessage;
                    initiatorNotification.MetaData = notification.MetaData;
                    break;
                case Domain.Enums.Notification.NotificationType.ManualSolicitorAssignment:
                    break;
                case Domain.Enums.Notification.NotificationType.CompletedRequest:
                    initiatorNotification.Title = ConstantTitle.CompletedRequestTitleForCso;
                    initiatorNotification.NotificationType = notification.NotificationType;
                    initiatorNotification.SolId = notification.SolId;
                    initiatorNotification.RecipientRole = nameof(RoleType.Cso);
                    initiatorNotification.IsBroadcast = true;
                    initiatorNotification.Message = ConstantMessage.CompletedRequestMessage;
                    initiatorNotification.MetaData = notification.MetaData;
                    break;
                case Domain.Enums.Notification.NotificationType.UnAssignedRequest:
                    initiatorNotification.Title = ConstantTitle.UnAssignedRequestTitleForCso;
                    initiatorNotification.NotificationType = notification.NotificationType;
                    initiatorNotification.SolId = notification.SolId;
                    initiatorNotification.RecipientRole = nameof(RoleType.Cso);
                    initiatorNotification.IsBroadcast = true;
                    initiatorNotification.Message = ConstantMessage.UnAssignedRequestMessage;
                    initiatorNotification.MetaData = notification.MetaData;
                    break;
                default:
                    break;
            }

            notifications.Add(initiatorNotification);

            return notifications;
        }

        public async Task NotifyUser(Guid initiatorUserId, Domain.Entities.Notification.Notification notification)
        {
            var notifications = DetermineNotificationsToPersist(notification);

            await _notificationManager.AddMultipleNotifications(notifications);
        }
    }
}
