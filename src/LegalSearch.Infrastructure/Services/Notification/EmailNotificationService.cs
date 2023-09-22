﻿using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Application.Models.Requests.Notification;
using LegalSearch.Application.Models.Requests.User;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace LegalSearch.Infrastructure.Services.Notification
{
    internal class EmailNotificationService : INotificationService, IEmailService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EmailNotificationService> _logger;
        private readonly AppDbContext _context;

        public EmailNotificationService(IHttpClientFactory httpClientFactory, 
            ILogger<EmailNotificationService> logger, AppDbContext context)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _context = context;
        }

        public async Task<bool> SendEmail(SendEmailRequest sendEmailRequest)
        {
            var client = _httpClientFactory.CreateClient("notificationClient");

            var requestContent = new MultipartFormDataContent();
            requestContent.Add(new StringContent(sendEmailRequest.From), "From");
            requestContent.Add(new StringContent(sendEmailRequest.To), "To");
            requestContent.Add(new StringContent(sendEmailRequest.Subject), "Subject");
            requestContent.Add(new StringContent(sendEmailRequest.Body), "Body");

            if (sendEmailRequest.Bcc != null)
            {
                sendEmailRequest.Bcc.ForEach(x => requestContent.Add(new StringContent(x), "Bcc"));
            }
            if (sendEmailRequest.Cc != null)
            {
                sendEmailRequest.Cc.ForEach(x => requestContent.Add(new StringContent(x), "Cc"));
            }

            using var response = await client.PostAsync("/fcmb/api/Mail/SendMail",
                requestContent);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Response from calling send email endpoint::::{responseContent}");

                return true;
            }

            return false;
        }

        public async Task SendNotificationToRole(string roleName, Domain.Entities.Notification.Notification notification, List<string?>? userEmails = null)
        {
            if (userEmails?.Any() == true)
            {
                var client = _httpClientFactory.CreateClient("notificationClient");

                string emailTemplate = GetNotificationTemplateToSend(notification);

                var requestContent = new MultipartFormDataContent();
                requestContent.Add(new StringContent("ebusiness@fcmb.com"), "From");
                requestContent.Add(new StringContent(notification.RecipientUserEmail!), "To");
                requestContent.Add(new StringContent(notification.Title!), "Subject");
                requestContent.Add(new StringContent(emailTemplate), "Body");

                // Add each email in userEmails list as "Bcc"
                foreach (var userEmail in userEmails)
                {
                    requestContent.Add(new StringContent(userEmail), "Bcc");
                }

                using var response = await client.PostAsync("/fcmb/api/Mail/SendMail", requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation($"Response from calling send email endpoint: {responseContent}");
                }
            }
        }

        public async Task SendNotificationToUser(Guid userId, Domain.Entities.Notification.Notification notification)
        {
            var client = _httpClientFactory.CreateClient("notificationClient");

            string emailTemplate = GetNotificationTemplateToSend(notification);

            var requestContent = new MultipartFormDataContent();
            requestContent.Add(new StringContent("ebusiness@fcmb.com"), "From");
            requestContent.Add(new StringContent(notification.RecipientUserEmail!), "To");
            requestContent.Add(new StringContent(notification.Title!), "Subject");
            requestContent.Add(new StringContent(emailTemplate), "Body");


            using var response = await client.PostAsync("/fcmb/api/Mail/SendMail",requestContent);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Response from calling send email endpoint::::{responseContent}");
            }

            //TODO: Do something when it fails
        }

        private static string GetNotificationTemplateToSend(Domain.Entities.Notification.Notification notification)
        {
            return notification.NotificationType switch
            {
                Domain.Enums.Notification.NotificationType.NewRequest => NotificationTemplates.RequestAssignmentNotification(),
                Domain.Enums.Notification.NotificationType.AssignedToSolicitor => NotificationTemplates.GenerateNewRequestNotification(),
                Domain.Enums.Notification.NotificationType.OutstandingRequestAfter24Hours => NotificationTemplates.OutstandingRequestNotification(),
                Domain.Enums.Notification.NotificationType.RequestWithElapsedSLA => NotificationTemplates.RequestwithElapsedSLANotification(),
                Domain.Enums.Notification.NotificationType.RequestReturnedToCso => NotificationTemplates.RequestReturnedNotification(),
                Domain.Enums.Notification.NotificationType.ManualSolicitorAssignment => NotificationTemplates.ManualSolicitorAssignmentNotification(),
                Domain.Enums.Notification.NotificationType.CompletedRequest => NotificationTemplates.RequestCompletedNotification(),
                Domain.Enums.Notification.NotificationType.UnAssignedRequest => NotificationTemplates.UnassignedRequestNotification(),
                _ => string.Empty
            };
        }
    }
}
