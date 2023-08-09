using LegalSearch.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalSearch.Domain.Entities.LegalRequest
{
    public class SupportingDocument : BaseEntity
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public byte[] FileContent { get; set; }

        // relationship
        public LegalRequest LegalRequest { get; set; }
        [ForeignKey("LegalRequest")]
        public Guid LegalRequestId { get; set; }
    }
}