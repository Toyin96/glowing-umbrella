using Fcmb.Shared.Models.Responses;

namespace LegalSearch.Application.Interfaces.LegalSearchRequest
{
    public interface ILegalSearchRequestService
    {
        Task<StatusResponse> CreateNewRequest(Models.Requests.LegalSearchRequest legalSearchRequest, string userId);
        Task<StatusResponse> AcceptLegalSearchRequest(Models.Requests.Solicitor.AcceptRequest acceptRequest);
        Task<StatusResponse> RejectLegalSearchRequest(Models.Requests.Solicitor.RejectRequest rejectRequest);
        Task<StatusResponse> PushBackLegalSearchRequestForMoreInfo(Models.Requests.Solicitor.ReturnRequest returnRequest, Guid solicitorId);
    }
}
