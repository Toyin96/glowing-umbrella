using LegalSearch.Application.Models.Requests.Notification;

namespace LegalSearch.Application.Interfaces.Notification
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(SendEmailRequest sendEmailRequest);
    }
}
