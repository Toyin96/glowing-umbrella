using LegalSearch.Domain.Entities.User.Solicitor;

namespace LegalSearch.Application.Interfaces.User
{
    public interface ISolicitorAssignmentManager
    {
        Task<SolicitorAssignment> GetSolicitorAssignmentById(Guid id);
    }
}
