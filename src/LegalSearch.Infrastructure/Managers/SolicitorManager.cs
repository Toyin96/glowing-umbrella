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
                                                                         SolicitorEmail = x.Email
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

            var state = await _appDbContext.States.FirstOrDefaultAsync(x => x.Id == request.State);

            if (state == null) return false;

            solicitor.FirstName = request.FirstName;
            solicitor.LastName = request.LastName;
            solicitor.Firm!.Name = request.FirmName;
            solicitor.Email = request.Email;
            solicitor.PhoneNumber = request.PhoneNumber;
            solicitor.StateId = state.Id;
            solicitor.State.RegionId = state.RegionId;
            solicitor.Firm.Address = request.Address;
            solicitor.BankAccount = request.AccountNumber;

            // persist changes
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<SolicitorRetrievalResponse>> FetchSolicitorsInSameRegion(Guid regionId)
        {

            var userIds = await _appDbContext.Users
                                        .Include(x => x.Firm)
                                        .Include(x => x.Firm.State)
                                            .ThenInclude(x => x.Region)
                                        .Where(u => u.Firm.Id != Guid.Empty && u.Firm.StateId != null
                                        && u.Firm.State.RegionId == regionId && u.ProfileStatus == nameof(ProfileStatusType.Active))
                                        .Select(u => new SolicitorRetrievalResponse
                                        {
                                            SolicitorId = u.Id,
                                            SolicitorEmail = u.Email
                                        })
                                        .ToListAsync();

            return userIds ?? Enumerable.Empty<SolicitorRetrievalResponse>();
        }

        public async Task<SolicitorAssignment> GetCurrentSolicitorMappedToRequest(Guid requestId, Guid solicitorId)
        {
            var solicitorAssignmentRecord = await _appDbContext.SolicitorAssignments
                                                .Where(a => a.RequestId == requestId && !a.IsAccepted
                                                && a.SolicitorId == solicitorId)
                                                .FirstOrDefaultAsync();

            return solicitorAssignmentRecord;
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
            // re-routes requests with elapsed SLA
            var elapsedTime = TimeUtils.GetSeventyTwoHoursElapsedTime();

            var requestIds = await _appDbContext.SolicitorAssignments
                                                .Where(a => a.IsAccepted && a.AssignedAt <= elapsedTime)
                                                .Select(a => a.RequestId)
                                                .Distinct()
                                                .ToListAsync();

            return requestIds ?? Enumerable.Empty<Guid>();
        }

        public async Task<IEnumerable<Guid>> GetUnattendedAcceptedRequestsForTheTimeFrame(DateTime timeframe, bool isSlaElapsed)
        {
            /*
             This method queries requests that have been accepted by solicitors but 
             left unattended within the timeframe. Time can be past 24 hours(1 day) or past 72 hours (3 days)
             */

            var query = _appDbContext.SolicitorAssignments
                                     .Where(a => a.IsAccepted && a.AssignedAt <= timeframe);

            if (!isSlaElapsed)
            {
                DateTime elapsedSlaTime = TimeUtils.GetSeventyTwoHoursElapsedTime();
                query = query.Where(a => a.AssignedAt > elapsedSlaTime);
            }

            var requestIds = await query.Select(a => a.RequestId)
                                        .Distinct()
                                        .ToListAsync();

            return requestIds;
        }
    }
}
