using LegalSearch.Domain.Enums.Role;
using System.ComponentModel.DataAnnotations;

namespace LegalSearch.Application.Models.Requests.CSO
{
    public class EscalateRequest
    {
        [Required]
        public NotificationRecipientType RecipientType { get; set; }
        [Required]
        public Guid RequestId { get; set; }
    }
}
