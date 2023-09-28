using LegalSearch.Application.Interfaces.User;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Infrastructure.Managers
{
    public class SolicitorAssignmentManager : ISolicitorAssignmentManager
    {
        private readonly AppDbContext _appDbContext;

        public SolicitorAssignmentManager(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }
        public async Task<SolicitorAssignment?> GetSolicitorAssignmentBySolicitorId(Guid id)
        {
            return await _appDbContext.SolicitorAssignments.FirstOrDefaultAsync(x => x.SolicitorId == id);
        }

        public async Task<bool> UpdateSolicitorAssignmentRecord(SolicitorAssignment solicitorAssignment)
        {
            _appDbContext.SolicitorAssignments.Update(solicitorAssignment);

            return await _appDbContext.SaveChangesAsync() > 0;
        }
    }
}
