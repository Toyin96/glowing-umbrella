using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Infrastructure.Services.User
{
    internal class SolicitorRetrievalService : ISolicitorRetrievalService
    {
        private readonly AppDbContext _appDbContext;

        public SolicitorRetrievalService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }
        public async Task<IEnumerable<SolicitorRetrievalResponse>> DetermineSolicitors(LegalRequest request)
        {
            // Implement logic to determine solicitors based on request criteria
            List<Guid> firms = new List<Guid>();

            switch (request.RequestType)
            {
                case nameof(RequestType.Corporate):
                    firms = await _appDbContext.Firms.Where(x => x.StateId == request.BusinessLocation)
                                                     .Select(x => x.Id)
                                                     .ToListAsync();
                    break;
                case nameof(RequestType.BusinessName):
                    if (request.BusinessLocation == request.RegistrationLocation)
                    {
                        firms = await _appDbContext.Firms.Where(x => x.StateId == request.BusinessLocation)
                                                         .Select(x => x.Id)
                                                         .ToListAsync();
                    }
                    else
                    {
                        firms = await _appDbContext.Firms.Where(x => x.StateId == request.RegistrationLocation)
                                                         .Select(x => x.Id)
                                                         .ToListAsync();
                    }
                    break;
                default:
                    break;
            }

            if (firms.Count == 0)
            {
                return Enumerable.Empty<SolicitorRetrievalResponse>();
            }

            // get solicitors
            List<SolicitorRetrievalResponse> solicitorIds = await _appDbContext.Users.Include(x => x.Firm)
                                                                     .Where(x => firms.Contains(x.Firm.Id))
                                                                     .Select(x => new SolicitorRetrievalResponse
                                                                     {
                                                                         SolicitorId = x.Id,
                                                                     })
                                                                     .ToListAsync();



            return solicitorIds;
        }

        public async Task<IEnumerable<SolicitorRetrievalResponse>> FetchSolicitorsInSameRegion(Guid regionId)
        {
            var states = await _appDbContext.States
                                            .Where(x => x.RegionId == regionId)
                                            .Select(x => x.Id)
                                            .ToListAsync();

            if (states == null || states?.Count == 0)
            {
                return Enumerable.Empty<SolicitorRetrievalResponse>();
            }

            // query firms
            List<Guid> firms = await _appDbContext.Firms.Where(x => states.Contains(x.StateId))
                                                                          .Select(x => x.Id).ToListAsync();

            // query solicitors
            List<SolicitorRetrievalResponse> solicitorsIds = await _appDbContext.Users
                                                                                .Include(x => x.Firm)
                                                                                .Where(x => firms.Contains(x.Firm.Id))
                                                                                .Select(x => new SolicitorRetrievalResponse
                                                                                {
                                                                                    SolicitorId = x.Id
                                                                                }).ToListAsync();

            return solicitorsIds ?? Enumerable.Empty<SolicitorRetrievalResponse>();
        }

        public async Task<SolicitorAssignment> GetCurrentSolicitorMappedToRequest(Guid requestId, Guid solicitorId)
        {

            var assignment = await _appDbContext.SolicitorAssignments
                                                .Where(a => a.RequestId == requestId && !a.IsAccepted && a.SolicitorId == solicitorId)
                                                .FirstOrDefaultAsync();

            return assignment;
        }

        public async Task<SolicitorAssignment> GetNextSolicitorInLine(Guid requestId, int currentOrder = 0)
        {
            var currentDateTime = DateTime.UtcNow.AddHours(1); // current time
            SolicitorAssignment? assignment;

            if (currentOrder == 0)
            {
                 assignment = await _appDbContext.SolicitorAssignments
                                    .Where(a => a.RequestId == requestId && !a.IsAccepted && a.AssignedAt <= currentDateTime)
                                    .OrderBy(a => a.Order)
                                    .FirstOrDefaultAsync();
            }
            else
            {
                assignment = await _appDbContext.SolicitorAssignments
                                    .Where(a => a.RequestId == requestId && !a.IsAccepted
                                    && a.AssignedAt <= currentDateTime && a.Order > currentOrder)
                                    .OrderBy(a => a.Order)
                                    .FirstOrDefaultAsync();
            }

            return assignment;
        }

        public async Task<IEnumerable<Guid>> GetRequestsToReroute()
        {
            var twentyMinutesAgo = DateTime.UtcNow.AddHours(1).AddMinutes(-20); // 20 minutes ago

            var requestIds = await _appDbContext.SolicitorAssignments
                                                .Where(a => !a.IsAccepted && a.AssignedAt <= twentyMinutesAgo)
                                                .Select(a => a.RequestId)
                                                .Distinct()
                                                .ToListAsync();

            return requestIds ?? Enumerable.Empty<Guid>();
        }
    }
}
