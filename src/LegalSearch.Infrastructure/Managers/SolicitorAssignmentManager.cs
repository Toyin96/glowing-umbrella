using LegalSearch.Application.Interfaces.User;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Infrastructure.Persistence;

namespace LegalSearch.Infrastructure.Managers
{
    public class SolicitorAssignmentManager : ISolicitorAssignmentManager
    {
        private readonly AppDbContext _appDbContext;

        public SolicitorAssignmentManager(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }
        public Task<SolicitorAssignment> GetSolicitorAssignmentById(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
