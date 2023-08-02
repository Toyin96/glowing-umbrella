using LegalSearch.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalSearch.Domain.Entities.User.Solicitor
{
    public class Address : BaseEntity
    {
        [ForeignKey("Solicitor")]
        public Guid UserId { get; set; }
        public string Street { get; set; }
        [ForeignKey("State")]
        public Guid StateId { get; set; }
        public State State { get; set; }
    }
}
