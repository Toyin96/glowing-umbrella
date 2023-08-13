using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Application.Models.Responses.Solicitor;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Entities.User.Solicitor;

namespace LegalSearch.Application.Interfaces.User
{
    public interface ISolicitorService
    {
        Task<ObjectResponse<SolicitorProfileDto>> ViewSolicitorProfile(Guid userId);
        Task<StatusResponse> EditSolicitorProfile(EditSolicitoProfileRequest editSolicitoProfileRequest, Guid userId);
    }
}
