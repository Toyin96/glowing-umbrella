﻿using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Entities.User.Solicitor;

namespace LegalSearch.Application.Interfaces.User
{
    public interface ISolicitorRetrievalService
    {
        Task<IEnumerable<SolicitorRetrievalResponse>> DetermineSolicitors(LegalRequest request);
        Task<SolicitorAssignment> GetNextSolicitorInLine(Guid requestId, int currentOrder = 0);
        Task<SolicitorAssignment> GetCurrentSolicitorMappedToRequest(Guid requestId, Guid solicitorId);

        Task<IEnumerable<SolicitorRetrievalResponse>> FetchSolicitorsInSameRegion(Guid regionId);
        Task<IEnumerable<Guid>> GetRequestsToReroute();

    }
}
