using LegalSearch.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalSearch.Domain.Entities.LegalRequest
{
    public class Discussion : BaseEntity
    {
        public string? Conversation { get; set; }

        // configure relationship with legalSearch
        [ForeignKey("LegalRequest")]
        public Guid LegalSearchRequestId { get; set; }
        public LegalRequest? LegalRequest { get; set; }
    }
}