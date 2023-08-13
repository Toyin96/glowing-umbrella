using LegalSearch.Domain.Common;

namespace LegalSearch.Domain.Entities.Location
{
    public class Branch : BaseEntity
    {
        public int BranchId { get; set; }
        public string SolId { get; set; }
        public string Address { get; set; }
    }
}
