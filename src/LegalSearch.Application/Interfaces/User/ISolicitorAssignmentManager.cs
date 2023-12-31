﻿using LegalSearch.Domain.Entities.User.Solicitor;

namespace LegalSearch.Application.Interfaces.User
{
    public interface ISolicitorAssignmentManager
    {
        Task<SolicitorAssignment?> GetSolicitorAssignmentBySolicitorId(Guid solicitorId, Guid requestId);
        Task<bool> UpdateSolicitorAssignmentRecord(SolicitorAssignment solicitorAssignment);
    }
}
