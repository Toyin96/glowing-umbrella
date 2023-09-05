using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Domain.Enums.User;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Infrastructure.Managers
{
    public class SolicitorManager : ISolicitorManager
    {
        private readonly AppDbContext _appDbContext;

        public SolicitorManager(AppDbContext appDbContexti)
        {
            _appDbContext = appDbContexti;
        }

        public async Task<IEnumerable<SolicitorRetrievalResponse>> DetermineSolicitors(LegalRequest request)
        {
            // Implement logic to determine solicitors based on request criteria
            List<Guid> firms = new List<Guid>();

            switch (request.RequestType)
            {
                case nameof(RequestType.Corporate):
                    firms = await _appDbContext.Firms.Where(x => x.StateOfCoverageId == request.BusinessLocation)
                                                     .Select(x => x.Id)
                                                     .ToListAsync();
                    break;
                case nameof(RequestType.BusinessName):
                    if (request.BusinessLocation == request.RegistrationLocation)
                    {
                        firms = await _appDbContext.Firms.Where(x => x.StateOfCoverageId == request.BusinessLocation)
                                                         .Select(x => x.Id)
                                                         .ToListAsync();
                    }
                    else
                    {
                        firms = await _appDbContext.Firms.Where(x => x.StateOfCoverageId == request.RegistrationLocation)
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
                                                                     .Where(x => firms.Contains(x.Firm.Id) 
                                                                     && x.ProfileStatus == ProfileStatusType.Active.ToString()
                                                                     && x.OnboardingStatus == OnboardingStatusType.Completed)
                                                                     .Select(x => new SolicitorRetrievalResponse
                                                                     {
                                                                         SolicitorId = x.Id,
                                                                     })
                                                                     .ToListAsync();



            return solicitorIds;
        }

        public async Task<bool> EditSolicitorProfile(EditSolicitorProfileByLegalTeamRequest request, Guid userId)
        {
            // get solicitor profile
            var solicitor = await _appDbContext.Users
                                               .Include(x => x.Firm)
                                               .Include(x => x.State)
                                                   .ThenInclude(x => x.Region)
                                               .FirstOrDefaultAsync(x => x.Id == userId);

            if (solicitor == null) return false;

            solicitor.FirstName = request.FirstName;
            solicitor.LastName = request.LastName;
            solicitor.Firm!.Name = request.FirmName;
            solicitor.Email = request.Email;
            solicitor.PhoneNumber = request.PhoneNumber; 
            solicitor.StateId = request.State;
            solicitor.State!.RegionId = request.Region;
            solicitor.Firm.Address = request.Address;
            solicitor.BankAccount = request.AccountNumber;

            // persist changes
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<SolicitorRetrievalResponse>> FetchSolicitorsInSameRegion(Guid regionId)
        {

            var userIds = _appDbContext.Users
                                        .Include(x => x.Firm)
                                        .Include(x => x.Firm.State)
                                            .ThenInclude(x => x.Region)
                                        .Where(u => u.Firm.Id != default && u.Firm.StateId != null 
                                        && u.Firm.State.RegionId == regionId && u.ProfileStatus == ProfileStatusType.Active.ToString())
                                        .Select(u => new SolicitorRetrievalResponse
                                        {
                                            SolicitorId = u.Id,
                                        })
                                        .ToList();

            return userIds ?? Enumerable.Empty<SolicitorRetrievalResponse>();


            //var states = await _appDbContext.States
            //                                .Where(x => x.RegionId == regionId)
            //                                .Select(x => x.Id)
            //                                .ToListAsync();

            //if (states == null || states?.Count == 0)
            //{
            //    return Enumerable.Empty<SolicitorRetrievalResponse>();
            //}

            //// query firms
            //List<Guid> firms = await _appDbContext.Firms.Where(x => x.StateId.HasValue && states.Contains(x.StateId.Value))
            //                                                              .Select(x => x.Id).ToListAsync();

            //// query solicitors
            //List<SolicitorRetrievalResponse> solicitorsIds = await _appDbContext.Users
            //                                                                    .Include(x => x.Firm)
            //                                                                    .Where(x => firms.Contains(x.Firm.Id))
            //                                                                    .Select(x => new SolicitorRetrievalResponse
            //                                                                    {
            //                                                                        SolicitorId = x.Id
            //                                                                    }).ToListAsync();

            //return solicitorsIds ?? Enumerable.Empty<SolicitorRetrievalResponse>();
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
            var currentDateTime = TimeUtils.GetCurrentLocalTime(); // current time
            SolicitorAssignment? assignment;

            if (currentOrder == 0)
            {
                assignment = await _appDbContext.SolicitorAssignments
                                   .Where(a => a.RequestId == requestId && !a.IsAccepted
                                   && a.AssignedAt <= currentDateTime)
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

        public async Task<IEnumerable<Guid>> GetUnattendedAcceptedRequestsForTheTimeFrame(DateTime timeframe)
        {
            /*
             this method queries requests that have been accepted by solicitors but 
            left unattended within the timeframe. Time can be past 24 hours(1 day) or past 72 hours (3 days)
             */

            var requestIds = await _appDbContext.SolicitorAssignments
                                                .Where(a => a.IsAccepted && a.AssignedAt <= timeframe)
                                                .Select(a => a.RequestId)
                                                .Distinct()
                                                .ToListAsync();

            return requestIds ?? Enumerable.Empty<Guid>();
        }
    }
}
