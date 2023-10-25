using LegalSearch.Domain.Common;
using LegalSearch.Domain.Entities.Location;

namespace LegalSearch.Domain.Entities.User
{
    public class ZonalServiceManager : BaseEntity
    {
        public required string Name { get; set; }
        public required string EmailAddress { get; set; }
        public string? AlternateEmailAddress { get; set; }
        public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    }
}
