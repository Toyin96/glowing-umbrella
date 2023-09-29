using LegalSearch.Domain.Enums.Notification;

namespace LegalSearch.Application.Models.Responses
{
    public class NotificationResponse
    {
        public Guid NotificationId { get; set; }
        public string? Title { get; set; }
        public NotificationType NotificationType { get; set; }
        public string RecipientUserId { get; set; }
        public string Message { get; set; }
        public DateTime DateCreated { get; set; }
        public bool IsRead { get; set; }
        public string? MetaData { get; set; }
    }
}
