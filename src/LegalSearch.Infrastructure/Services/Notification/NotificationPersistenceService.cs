using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Domain.ApplicationMessages;
using LegalSearch.Domain.Enums.Notification;
using LegalSearch.Domain.Enums.Role;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LegalSearch.Infrastructure.Services.Notification
{
    public class NotificationPersistenceService : INotificationService
    {
        private readonly INotificationManager _notificationManager;
        private readonly ILogger<NotificationPersistenceService> _logger;
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve };

        public NotificationPersistenceService(INotificationManager notificationManager, ILogger<NotificationPersistenceService> logger)
        {
            _notificationManager = notificationManager;
            _logger = logger;
        }
        public async Task NotifyUsersInRole(string roleName, Domain.Entities.Notification.Notification notification, List<string?>? userEmails = null)
        {
            try
            {
                var notifications = DetermineNotificationsToPersist(notification);

                await _notificationManager.AddMultipleNotifications(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Exception occurred inside NotifyUsersInRole. See reason: {JsonSerializer.Serialize(ex, _serializerOptions)}");
            }
        }

        public List<Domain.Entities.Notification.Notification> DetermineNotificationsToPersist(Domain.Entities.Notification.Notification notification)
        {
            var notifications = new List<Domain.Entities.Notification.Notification>() { notification };

            var initiatorNotification = new Domain.Entities.Notification.Notification();

            switch (notification.NotificationType)
            {
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
                    initiatorNotification.Title = ConstantTitle.NewRequestAssignmentTitle;
                    initiatorNotification.NotificationType = NotificationType.NewRequest;
                    initiatorNotification.IsBroadcast = true;
                    initiatorNotification.Message = ConstantMessage.NewRequestAssignmentMessageForStaff;
                    initiatorNotification.MetaData = notification.MetaData;
                    break;
                case Domain.Enums.Notification.NotificationType.NewRequest:
                    initiatorNotification.Title = ConstantTitle.NewRequestAssignmentTitle;
                    initiatorNotification.NotificationType = NotificationType.NewRequest;
                    initiatorNotification.SolId = notification?.SolId;
                    initiatorNotification.RecipientUserId = notification.RecipientUserId;
                    initiatorNotification.IsBroadcast = true;
                    initiatorNotification.Message = ConstantMessage.NewRequestAssignmentMessageForStaff;
                    initiatorNotification.MetaData = notification.MetaData;
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

        public async Task NotifyUser(Domain.Entities.Notification.Notification notification)
        {
            var notifications = DetermineNotificationsToPersist(notification);

            await _notificationManager.AddMultipleNotifications(notifications);
        }
    }
}
