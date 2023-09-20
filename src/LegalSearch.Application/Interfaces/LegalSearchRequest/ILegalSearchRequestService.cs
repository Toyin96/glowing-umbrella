using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Requests.CSO;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Application.Models.Responses.CSO;

namespace LegalSearch.Application.Interfaces.LegalSearchRequest
{
    public interface ILegalSearchRequestService
    {
        Task<ListResponse<FinacleLegalSearchResponsePayload>> GetFinacleLegalRequestsForCso(GetFinacleRequest request, string solId);
        Task<ObjectResponse<StaffRootResponsePayload>> GetLegalRequestsForStaff(StaffDashboardAnalyticsRequest request);
        Task<ObjectResponse<byte[]>> GenerateRequestAnalyticsReportForStaff(StaffDashboardAnalyticsRequest request);
        Task<ObjectResponse<byte[]>> GenerateRequestAnalyticsReportForSolicitor(SolicitorRequestAnalyticsPayload request, Guid solicitorId);
        Task<ObjectResponse<LegalSearchRootResponsePayload>> GetLegalRequestsForSolicitor(SolicitorRequestAnalyticsPayload viewRequestAnalyticsPayload, Guid solicitorId);
        Task<ObjectResponse<GetAccountInquiryResponse>> PerformNameInquiryOnAccount(string accountNumber);
        Task<StatusResponse> CreateNewRequestFromFinacle(Models.Requests.FinacleLegalSearchRequest legalSearchRequest);
        Task<StatusResponse> EscalateRequest(EscalateRequest request);
        Task<StatusResponse> CreateNewRequest(Models.Requests.LegalSearchRequest legalSearchRequest, string userId);
        Task<StatusResponse> UpdateFinacleRequestByCso(UpdateFinacleLegalRequest updateFinacleLegalRequest, string userId);
        Task<StatusResponse> UpdateRequestByStaff(UpdateRequest request);
        Task<StatusResponse> CancelLegalSearchRequest(CancelRequest request);
        Task<StatusResponse> AcceptLegalSearchRequest(AcceptRequest request);
        Task<StatusResponse> RejectLegalSearchRequest(RejectRequest request);
        Task<StatusResponse> PushBackLegalSearchRequestForMoreInfo(ReturnRequest returnRequest, Guid solicitorId);
        Task<StatusResponse> SubmitRequestReport(SubmitLegalSearchReport submitLegalSearchReport, Guid solicitorId);
    }
}
