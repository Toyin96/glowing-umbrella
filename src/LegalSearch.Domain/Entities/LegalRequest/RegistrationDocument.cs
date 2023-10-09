using LegalSearch.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalSearch.Domain.Entities.LegalRequest
{
    public class RegistrationDocument : BaseEntity
    {
        public required string FileName { get; set; }
        public required string FileType { get; set; }
        public required byte[] FileContent { get; set; }

        // relationship
        public LegalRequest? LegalRequest { get; set; }
        [ForeignKey("LegalRequest")]
        public Guid LegalRequestId { get; set; }
    }
}
