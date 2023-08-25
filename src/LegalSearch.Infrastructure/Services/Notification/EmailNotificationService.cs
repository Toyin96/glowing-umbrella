using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Application.Models.Requests.Notification;
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
        private readonly UserManager<Domain.Entities.User.User> _userManager;

        public EmailNotificationService(IHttpClientFactory httpClientFactory, 
            ILogger<EmailNotificationService> logger, UserManager<Domain.Entities.User.User> userManager)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _userManager = userManager;
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

            using var response = await client.PostAsync("https://emailnotificationmcsvc.fcmb-azr-msase.p.azurewebsites.net/fcmb/api/Mail/SendMail",
                requestContent);

            var responseContent = await response.Content.ReadAsStringAsync();


            if (response.IsSuccessStatusCode)
            {
                //var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Response from calling send email endpoint::::{responseContent}");

                return true;
            }

            return false;
        }

        public Task SendNotificationToRole(string roleName, Domain.Entities.Notification.Notification notification)
        {
            throw new NotImplementedException();
        }

        public async Task SendNotificationToUser(Guid userId, Domain.Entities.Notification.Notification notification)
        {
            var client = _httpClientFactory.CreateClient("notificationClient");

            // get user
            var user = _userManager.FindByIdAsync(notification.RecipientUserId);

            // make a decision based on the notification type
            // TODO

            using var response = await client.PostAsync("/Mail/SendMail", 
                new StringContent(JsonSerializer.Serialize(notification), Encoding.UTF8));

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Response from calling send email endpoint::::{responseContent}");
            }

            //TODO: Do something when it fails
        }
    }
}
