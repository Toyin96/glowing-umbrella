using LegalSearch.Application.Models.Requests.CSO;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Application.Models.Responses.CSO;

namespace LegalSearch.Application.Interfaces.LegalSearchRequest
{
    public interface ILegalSearchRequestManager
    {
        Task<Domain.Entities.LegalRequest.LegalRequest?> GetLegalSearchRequest(Guid requestId);
        Task<bool> UpdateLegalSearchRequest(Domain.Entities.LegalRequest.LegalRequest legalRequest);
        Task<bool> AddNewLegalSearchRequest(Domain.Entities.LegalRequest.LegalRequest legalRequest);
        Task<LegalSearchRootResponsePayload> GetLegalRequestsForSolicitor(SolicitorRequestAnalyticsPayload viewRequestAnalyticsPayload, Guid solicitorId);
        Task<StaffRootResponsePayload> GetLegalRequestsForStaff(StaffDashboardAnalyticsRequest request);
        Task<BranchLegalSearchResponsePayload> GetBranchLegalRequestsForStaff(CsoBranchDashboardAnalyticsRequest request);
        Task<List<FinacleLegalSearchResponsePayload>> GetFinacleLegalRequestsForCso(GetFinacleRequest request, string solId);
    }
}
