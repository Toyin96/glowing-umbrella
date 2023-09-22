using LegalSearch.Domain.Common;
namespace LegalSearch.Domain.Entities.User.Solicitor
{
    public class SolicitorAssignment : BaseEntity
    {
        public Guid SolicitorId { get; set; }
        public string SolicitorEmail { get; set; }
        public Guid RequestId { get; set; }
        public int Order { get; set; }
        public DateTime AssignedAt { get; set; }
        public bool IsAccepted { get; set; }
    }
}
