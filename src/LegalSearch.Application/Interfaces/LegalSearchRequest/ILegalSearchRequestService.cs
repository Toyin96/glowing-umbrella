﻿using Fcmb.Shared.Models.Responses;

namespace LegalSearch.Application.Interfaces.LegalSearchRequest
{
    public interface ILegalSearchRequestService
    {
        Task<ObjectResponse<string>> CreateNewRequest(Models.Requests.LegalSearchRequest legalSearchRequest);
    }
}
