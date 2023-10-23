using LegalSearch.Domain.Common;

namespace LegalSearch.Domain.Entities.User
{
    public class CustomerServiceManager : BaseEntity
    {
        public required string Name { get; set; }
        public required string SolId { get; set; }
        public required string EmailAddress { get; set; }
        public required string AlternateEmailAddress { get; set; }
    }
}
