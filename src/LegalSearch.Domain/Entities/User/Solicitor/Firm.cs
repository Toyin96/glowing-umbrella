using LegalSearch.Domain.Common;

namespace LegalSearch.Domain.Entities.User.Solicitor
{
    public class Firm : BaseEntity
    {
        public string Name { get; set; }
        public Address Address { get; set; }
    }
}
