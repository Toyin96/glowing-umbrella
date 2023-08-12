using LegalSearch.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalSearch.Domain.Entities.User.Solicitor
{
    public class State : BaseEntity
    {
        public string Name { get; set; }
        [ForeignKey("Region")]
        public Guid RegionId { get; set; }
        public Region Region { get; set; }

        public ICollection<User>? Users { get; set; } = new List<User>();
        public ICollection<Firm>? Firms { get; set; } = new List<Firm>();
    }

    public class Region : BaseEntity
    {
        public string Name { get; set; }
        public ICollection<State> States { get; set; } = new List<State>();
    }
}
