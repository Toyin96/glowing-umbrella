﻿using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Responses;

namespace LegalSearch.Application.Interfaces.LegalSearchRequest
{
    public interface ILegalSearchRequestService
    {
        Task<ObjectResponse<GetAccountInquiryResponse>> PerformNameInquiryOnAccount(string accountNumber);
        Task<StatusResponse> CreateNewRequest(Models.Requests.LegalSearchRequest legalSearchRequest, string userId);
        Task<StatusResponse> AcceptLegalSearchRequest(Models.Requests.Solicitor.AcceptRequest acceptRequest);
        Task<StatusResponse> RejectLegalSearchRequest(Models.Requests.Solicitor.RejectRequest rejectRequest);
        Task<StatusResponse> PushBackLegalSearchRequestForMoreInfo(Models.Requests.Solicitor.ReturnRequest returnRequest, Guid solicitorId);
        Task<StatusResponse> SubmitRequestReport(Models.Requests.Solicitor.SubmitLegalSearchReport submiteLegalSearchReport, Guid solicitorId);
    }
}
