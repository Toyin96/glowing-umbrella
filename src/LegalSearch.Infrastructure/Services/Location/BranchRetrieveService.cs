using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Domain.Entities.Location;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Infrastructure.Services.Location
{
    public class BranchRetrieveService : IBranchRetrieveService
    {
        private readonly AppDbContext _appDbContext;

        public BranchRetrieveService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }
        public async Task<Branch?> GetBranchBySolId(string id)
        {
            return await _appDbContext.Branches.FirstOrDefaultAsync(x => x.SolId == id);
        }
    }
}
