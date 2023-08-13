using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Infrastructure.Managers
{
    internal class LegalSearchRequestManager : ILegalSearchRequestManager
    {
        private readonly AppDbContext _appDbContext;

        public LegalSearchRequestManager(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<bool> AddNewLegalSearchRequest(LegalRequest legalRequest)
        {
            await _appDbContext.LegalSearchRequests.AddAsync(legalRequest);

            return await _appDbContext.SaveChangesAsync() > 0;
        }

        public async Task<LegalRequest?> GetLegalSearchRequest(Guid requestId)
        {
            return await _appDbContext.LegalSearchRequests
                                      .Include(x => x.Discussions.OrderBy(y => y.CreatedAt))
                                      .Include(x => x.SupportingDocuments.OrderBy(y => y.CreatedAt))
                                      .FirstOrDefaultAsync(x => x.Id == requestId);
        }

        public async Task<bool> UpdateLegalSearchRequest(LegalRequest legalRequest)
        {
            _appDbContext.LegalSearchRequests.Update(legalRequest);

            return await _appDbContext.SaveChangesAsync() > 0;
        }
    }
}
