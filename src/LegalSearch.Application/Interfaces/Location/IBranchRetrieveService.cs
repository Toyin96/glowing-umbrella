using LegalSearch.Domain.Entities.Location;

namespace LegalSearch.Application.Interfaces.Location
{
    public interface IBranchRetrieveService
    {
        Task<Branch> GetBranchBySolId(string id);
    }
}
