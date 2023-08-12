using LegalSearch.Domain.Common;

namespace LegalSearch.Domain.Entities.LegalRequest
{
    public class LegalRequest : BaseEntity
    {
        public Guid InitiatorId { get; set; } // ID of staff on application.
        public string StaffId { get; set; } // Staff's ID from bank
        public string RequestInitiator { get; set; } // staff's name
        public string Branch { get; set; }
        public Guid AssignedSolicitorId { get; set; }
        public string RequestType { get; set; }
        public Guid BusinessLocation { get; set; }
        public Guid RegistrationLocation { get; set; }
        public string Status { get; set; }
        public string CustomerAccountName { get; set; }
        public string CustomerAccountNumber { get; set; }
        public string RegistrationNumber { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string? AdditionalInformation { get; set; }
        public string? ReasonForRejection { get; set; }
        public DateTime? DateAssignedToSolicitor { get; set; }
        public DateTime? DateDue { get; set; }
        public ICollection<Discussion> Discussions { get; set; } = new List<Discussion>();
        public ICollection<SupportingDocument> SupportingDocuments { get; set; } = new List<SupportingDocument>();
    }
}
