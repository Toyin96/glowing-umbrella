using LegalSearch.Domain.Common;
using LegalSearch.Domain.Enums.LegalRequest;

namespace LegalSearch.Domain.Entities.LegalRequest
{
    public class LegalRequest : BaseEntity
    {
        public Guid InitiatorId { get; set; } // ID of staff on application.
        public string? StaffId { get; set; } // Staff's ID from bank
        public string? RequestInitiator { get; set; } // staff's name
        public string BranchId { get; set; }
        public Guid AssignedSolicitorId { get; set; }
        public string? RequestType { get; set; }
        public RequestSourceType RequestSource { get; set; }
        public Guid BusinessLocation { get; set; }
        public string? LienId { get; set; }
        public Guid RegistrationLocation { get; set; }
        public required string Status { get; set; }
        public required string CustomerAccountName { get; set; }
        public required string CustomerAccountNumber { get; set; }
        public string? RegistrationNumber { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string? ReasonForRejection { get; set; }
        public DateTime? DateAssignedToSolicitor { get; set; }
        public DateTime? DateDue { get; set; }
        public ICollection<Discussion> Discussions { get; set; } = new List<Discussion>();
        public ICollection<RegistrationDocument> RegistrationDocuments { get; set; } = new List<RegistrationDocument>();
        public ICollection<SupportingDocument> SupportingDocuments { get; set; } = new List<SupportingDocument>();
    }
}
