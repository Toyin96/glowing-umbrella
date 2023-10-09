using LegalSearch.Domain.Common;
namespace LegalSearch.Domain.Entities.User.Solicitor
{
    public class SolicitorAssignment : BaseEntity
    {
        public Guid SolicitorId { get; set; }
        public required string SolicitorEmail { get; set; }
        public bool IsCurrentlyAssigned { get; set; }
        public bool HasCompletedLegalSearchRequest { get; set; }
        public Guid RequestId { get; set; }
        public int Order { get; set; }
        public DateTime AssignedAt { get; set; }
        public bool IsAccepted { get; set; }
    }
}
