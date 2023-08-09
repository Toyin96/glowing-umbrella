using LegalSearch.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalSearch.Domain.Entities.User.Solicitor
{
    public class Firm : BaseEntity
    {
        public string Name { get; set; }
        public string Street { get; set; }
        [ForeignKey("State")]
        public Guid StateId { get; set; }
    }
}
