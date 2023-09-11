using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Entities.User.Solicitor;

namespace LegalSearch.Application.Interfaces.User
{
    public interface ISolicitorManager
    {
        Task<bool> EditSolicitorProfile(EditSolicitorProfileByLegalTeamRequest editSolicitorProfileRequest, Guid userId);
        Task<IEnumerable<SolicitorRetrievalResponse>> DetermineSolicitors(LegalRequest request);
        Task<SolicitorAssignment> GetNextSolicitorInLine(Guid requestId, int currentOrder = 0);
        Task<SolicitorAssignment> GetCurrentSolicitorMappedToRequest(Guid requestId, Guid solicitorId);
        Task<IEnumerable<SolicitorRetrievalResponse>> FetchSolicitorsInSameRegion(Guid regionId);
        Task<IEnumerable<Guid>> GetRequestsToReroute();
        Task<IEnumerable<Guid>> GetUnattendedAcceptedRequestsForTheTimeFrame(DateTime timeframe, bool isSlaElapsed);
    }
}
