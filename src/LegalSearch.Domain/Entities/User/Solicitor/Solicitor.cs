using System.ComponentModel.DataAnnotations.Schema;

namespace LegalSearch.Domain.Entities.User.Solicitor
{
    public class Solicitor : User
    {
        public string Address { get; set; }
        public Guid FirmId { get; set; }
        public Firm Firm { get; set; }
        [ForeignKey("State")]
        public Guid StateId { get; set; }
        public State State { get; set; }
        [ForeignKey("Region")]
        public Guid RegionId { get; set; }
        public Region Region { get; set; }
        public string BankAccount { get; set; }

        // Role properties
        public Guid? RoleId { get; set; }
        public Role.Role Role { get; set; }
    }
}
