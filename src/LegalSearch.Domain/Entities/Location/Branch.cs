using LegalSearch.Domain.Common;
using LegalSearch.Domain.Entities.User;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalSearch.Domain.Entities.Location
{
    public class Branch : BaseEntity
    {
        public required string SolId { get; set; }
        public required string Address { get; set; }

        [ForeignKey("ZonalServiceManager")]
        public Guid? ZonalServiceManagerId { get; set; }
        public ZonalServiceManager? ZonalServiceManager { get; set; }
    }
}
