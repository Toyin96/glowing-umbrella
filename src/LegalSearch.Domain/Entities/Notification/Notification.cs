using LegalSearch.Domain.Common;
using LegalSearch.Domain.Enums.Notification;

namespace LegalSearch.Domain.Entities.Notification
{
    public class Notification : BaseEntity
    {
        public string? Title { get; set; }
        public NotificationType NotificationType { get; set; }
        public string SolId { get; set; }
        public string? RecipientRole { get; set; } // New property for role-based recipients
        public string RecipientUserId { get; set; }
        public string RecipientUserEmail { get; set; }
        public string Message { get; set; }
        public bool IsBroadcast { get; set; }
        public bool IsRead { get; set; }
        public string? MetaData { get; set; }
    }
}
