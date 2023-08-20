using Fcmb.Shared.Utilities;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Infrastructure.Managers
{
    internal class LegalSearchRequestManager : ILegalSearchRequestManager
    {
        private readonly AppDbContext _appDbContext;

        public LegalSearchRequestManager(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<bool> AddNewLegalSearchRequest(LegalRequest legalRequest)
        {
            await _appDbContext.LegalSearchRequests.AddAsync(legalRequest);

            return await _appDbContext.SaveChangesAsync() > 0;
        }

        public async Task<LegalSearchRootResponsePayload> GetLegalRequestsForSolicitor(ViewRequestAnalyticsPayload request, Guid solicitorId)
        {
            // Step 1: Create the query to fetch legal requests
            IQueryable<LegalRequest> query = _appDbContext.LegalSearchRequests;

            // Step 2: Retrieve the solicitor assignments for the given solicitor
            var solicitorAssignments = _appDbContext.SolicitorAssignments
                .Where(assignment => assignment.SolicitorId == solicitorId)
                .ToList();

            // Step 3: Check if no solicitor assignments were found
            if (solicitorAssignments == null || solicitorAssignments.Count == 0)
            {
                // No solicitor assignments found, return empty summary
                return new LegalSearchRootResponsePayload
                {
                    LegalSearchRequests = new List<LegalSearchResponsePayload>(),
                    TotalRequests = 0,
                    WithinSLACount = 0,
                    ElapsedSLACount = 0,
                    Within3HoursToDueCount = 0
                };
            }

            // Step 4: Get the list of request IDs assigned to the solicitor
            var assignedRequestIds = solicitorAssignments.Select(assignment => assignment.RequestId).ToList();

            // Step 5: Apply filtering to the query from the request payload
            if (request.StartPeriod.HasValue && request.EndPeriod.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= request.StartPeriod && x.CreatedAt <= request.EndPeriod);
            }
            else if (request.StartPeriod.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= request.StartPeriod);
            }
            else if (request.EndPeriod.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= request.EndPeriod);
            }

            if (request.RequestStatus.HasValue)
            {
                query = FilterQueryBasedOnRequestStatus(request, solicitorId, query, assignedRequestIds);
            }

            // Step 6: Apply pagination to the query as per the request 
            query.Paginate(request);

            // Step 7: Apply filtering to fetch only assigned requests
            query = query.Where(x => assignedRequestIds.Contains(x.Id));

            // Step 8: Fetch the requested data
            var response = await query
                .Select(x => new
                {
                    x.Id,
                    x.RequestInitiator,
                    x.RequestType,
                    x.RegistrationDate,
                    x.Status,
                    x.CustomerAccountName,
                    x.CustomerAccountNumber,
                    x.BusinessLocation,
                    x.RegistrationLocation,
                    x.CreatedAt,
                    x.DateDue,
                })
                .ToListAsync();

            // Step 9: Fetch state names in a single query
            var stateIds = response.SelectMany(x => new[] { x.BusinessLocation, x.RegistrationLocation })
                                   .Distinct()
                                   .ToList();

            var stateNames = _appDbContext.States
                .Where(state => stateIds.Contains(state.Id))
                .ToDictionary(state => state.Id, state => state.Name);

            // Step 10: Get the current time in the application's local time zone
            var currentTime = TimeUtils.GetCurrentLocalTime();

            // Step 11: Map the response data to the response payload
            var mappedResponse = response.Select(x => new LegalSearchResponsePayload
            {
                RequestInitiator = x.RequestInitiator,
                RequestType = x.RequestType,
                RegistrationDate = x.RegistrationDate,
                RequestStatus = x.Status,
                CustomerAccountName = x.CustomerAccountName,
                CustomerAccountNumber = x.CustomerAccountNumber,
                BusinessLocation = stateNames.ContainsKey(x.BusinessLocation) ? stateNames[x.BusinessLocation] : string.Empty,
                RegistrationLocation = stateNames.ContainsKey(x.RegistrationLocation) ? stateNames[x.RegistrationLocation] : string.Empty,
                DateCreated = x.CreatedAt,
                DateDue = x.DateDue.HasValue ? x.DateDue : DateTime.MinValue,
            }).ToList();

            // Step 12: Calculate the counts for the three categories
            var summary = new LegalSearchRootResponsePayload
            {
                LegalSearchRequests = mappedResponse,
                TotalRequests = mappedResponse.Count,
                WithinSLACount = mappedResponse.Count(x => x.DateDue != null && currentTime > x.DateCreated && currentTime < x.DateDue?.AddHours(-3)),
                ElapsedSLACount = mappedResponse.Count(x => x.DateDue != null && currentTime > x.DateDue),
                Within3HoursToDueCount = mappedResponse.Count(x => x.DateDue != null && currentTime > x.DateDue?.AddHours(-3) && currentTime <= x.DateDue)
            };

            return summary;
        }

        private static IQueryable<LegalRequest> FilterQueryBasedOnRequestStatus(ViewRequestAnalyticsPayload request, Guid solicitorId, IQueryable<LegalRequest> query, List<Guid> assignedRequestIds)
        {
            switch (request.RequestStatus)
            {
                case SolicitorRequestStatusType.NewRequest:
                    query = query.Where(x => x.Status == RequestStatusType.Initiated.ToString());
                    break;
                case SolicitorRequestStatusType.AssignedToLawyer:
                    query = query.Where(x => x.Status == RequestStatusType.AssignedToLawyer.ToString()
                    && x.AssignedSolicitorId == solicitorId);
                    break;
                case SolicitorRequestStatusType.Completed:
                    query = query.Where(x => x.Status == RequestStatusType.Completed.ToString()
                    && x.AssignedSolicitorId == solicitorId);
                    break;
                case SolicitorRequestStatusType.LawyerRejected:
                    query = query.Where(x => assignedRequestIds.Contains(x.Id) &&
                    x.AssignedSolicitorId != solicitorId);
                    break;
            }

            return query;
        }

        public async Task<LegalRequest?> GetLegalSearchRequest(Guid requestId)
        {
            return await _appDbContext.LegalSearchRequests
                                      .Include(x => x.Discussions.OrderBy(y => y.CreatedAt))
                                      .Include(x => x.RegistrationDocuments)
                                      .Include(x => x.SupportingDocuments.OrderBy(y => y.CreatedAt))
                                      .FirstOrDefaultAsync(x => x.Id == requestId);
        }

        public async Task<bool> UpdateLegalSearchRequest(LegalRequest legalRequest)
        {
            _appDbContext.LegalSearchRequests.Update(legalRequest);

            return await _appDbContext.SaveChangesAsync() > 0;
        }
    }
}
