using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Requests.LegalPerfectionTeam;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses.Solicitor;

namespace LegalSearch.Application.Interfaces.User
{
    public interface ISolicitorService
    {
        Task<ObjectResponse<SolicitorProfileDto>> ViewSolicitorProfile(Guid userId);
        Task<StatusResponse> ManuallyAssignRequestToSolicitor(ManuallyAssignRequestToSolicitorRequest manuallyAssignRequestToSolicitorRequest);
        Task<StatusResponse> ActivateOrDeactivateSolicitor(ActivateOrDeactivateSolicitorRequest activateOrDeactivateSolicitorRequest);
        Task<ListResponse<SolicitorProfileDto>> ViewSolicitors(ViewSolicitorsRequestFilter viewSolicitorsRequestFilter);
        Task<StatusResponse> EditSolicitorProfile(EditSolicitorProfileByLegalTeamRequest editSolicitorProfileRequest);
    }
}
