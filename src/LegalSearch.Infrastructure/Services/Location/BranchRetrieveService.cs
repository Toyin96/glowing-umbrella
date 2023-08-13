using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Domain.Entities.Location;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Infrastructure.Services.Location
{
    internal class BranchRetrieveService : IBranchRetrieveService
    {
        private readonly AppDbContext _appDbContext;

        public BranchRetrieveService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }
        public async Task<Branch> GetBranchById(int id)
        {
            return await _appDbContext.Branches.FirstOrDefaultAsync(x => x.BranchId == id);
        }
    }
}
