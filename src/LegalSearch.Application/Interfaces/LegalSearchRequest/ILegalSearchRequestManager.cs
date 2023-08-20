using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;

namespace LegalSearch.Application.Interfaces.LegalSearchRequest
{
    public interface ILegalSearchRequestManager
    {
        Task<Domain.Entities.LegalRequest.LegalRequest?> GetLegalSearchRequest(Guid requestId);
        Task<bool> UpdateLegalSearchRequest(Domain.Entities.LegalRequest.LegalRequest legalRequest);
        Task<bool> AddNewLegalSearchRequest(Domain.Entities.LegalRequest.LegalRequest legalRequest);
        Task<LegalSearchRootResponsePayload> GetLegalRequestsForSolicitor(ViewRequestAnalyticsPayload viewRequestAnalyticsPayload, Guid solicitorId);
    }
}
