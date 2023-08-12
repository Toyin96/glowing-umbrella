using LegalSearch.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalSearch.Domain.Entities.User.Solicitor
{
    public class Firm : BaseEntity
    {
        public string Name { get; set; }
        public ICollection<User> Users { get; set; } = new List<User>();
        public string Address { get; set; }

        [ForeignKey("State")]
        public Guid? StateId { get; set; }
        public State? State { get; set; }
    }
}
