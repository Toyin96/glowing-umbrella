using LegalSearch.Domain.Common;

namespace LegalSearch.Domain.Entities.Location
{
    public class Branch
    {
        public Guid Id { get; set; }
        public required string SolId { get; set; }
        public required string Address { get; set; }
    }
}
