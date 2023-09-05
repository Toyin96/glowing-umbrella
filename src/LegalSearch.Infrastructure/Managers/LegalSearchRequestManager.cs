using Fcmb.Shared.Utilities;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Models.Requests.CSO;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Application.Models.Responses.CSO;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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

        public async Task<LegalSearchRootResponsePayload> GetLegalRequestsForSolicitor(SolicitorRequestAnalyticsPayload request, Guid solicitorId)
        {
            // Step 1: Create the query to fetch legal requests
            IQueryable<LegalRequest> query = _appDbContext.LegalSearchRequests
                                                          .Include(x => x.Discussions)
                                                          .Include(x => x.SupportingDocuments)
                                                          .Include(x => x.SupportingDocuments);

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

            // Step 6: Apply filtering to fetch only assigned requests
            query = query.Where(x => assignedRequestIds.Contains(x.Id));

            // Step 7: Apply pagination to the query as per the request 
            query.Paginate(request);

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
                    x.Discussions,
                    x.RegistrationDocuments,
                    x.SupportingDocuments
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
                Id = x.Id,
                RequestInitiator = x.RequestInitiator!,
                RequestType = x.RequestType!,
                RegistrationDate = x.RegistrationDate,
                RequestStatus = x.Status,
                CustomerAccountName = x.CustomerAccountName,
                CustomerAccountNumber = x.CustomerAccountNumber,
                BusinessLocation = stateNames.ContainsKey(x.BusinessLocation) ? stateNames[x.BusinessLocation] : string.Empty,
                RegistrationLocation = stateNames.ContainsKey(x.RegistrationLocation) ? stateNames[x.RegistrationLocation] : string.Empty,
                DateCreated = x.CreatedAt,
                DateDue = x.DateDue.HasValue ? x.DateDue : DateTime.MinValue,
                Discussions = x.Discussions.Select(x => new DiscussionDto { Conversation = x.Conversation }).ToList(), 
                SupportingDocuments = x.SupportingDocuments.Select(x => new RegistrationDocumentDto { FileName = x.FileName, FileContent = x.FileContent, FileType = x.FileType}).ToList(),
                RegistrationDocuments = x.RegistrationDocuments.Select(x => new RegistrationDocumentDto { FileName = x.FileName, FileContent = x.FileContent, FileType = x.FileType }).ToList(),
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

        private static IQueryable<LegalRequest> FilterQueryBasedOnRequestStatus(SolicitorRequestAnalyticsPayload request, Guid solicitorId, IQueryable<LegalRequest> query, List<Guid> assignedRequestIds)
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

        private static IQueryable<LegalRequest> FilterQueryBasedOnCsoRequestStatus(StaffDashboardAnalyticsRequest request, Guid csoId, IQueryable<LegalRequest> query)
        {
            switch (request.CsoRequestStatusType)
            {
                case CsoRequestStatusType.PendingWithCso:
                    query = query.Where(x => x.Status == RequestStatusType.BackToCso.ToString()
                    /*&& x.InitiatorId == csoId*/);
                    break;
                case CsoRequestStatusType.PendingWithSolicitor:
                    query = query.Where(x => x.Status == RequestStatusType.AssignedToLawyer.ToString()
                    /*&& x.InitiatorId == csoId*/);
                    break;
                case CsoRequestStatusType.CancelledRequest:
                    query = query.Where(x => x.Status == RequestStatusType.UnAssigned.ToString()
                    /*&& x.InitiatorId == csoId*/);
                    break;
                case CsoRequestStatusType.Completed:
                    query = query.Where(x => x.Status == RequestStatusType.Completed.ToString() /*&& x.InitiatorId == csoId*/);
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

        public async Task<CsoRootResponsePayload> GetLegalRequestsForCso(StaffDashboardAnalyticsRequest request, Guid csoId)
        {
            // Step 1: Create the query to fetch legal requests
            IQueryable<LegalRequest> query = _appDbContext.LegalSearchRequests;

            // Step 2: Apply filtering based on the request payload
            query = ApplyFilters(request, csoId, query);

            // Step 3: Apply pagination to the query as per the request 
            query = query.Paginate(request);

            // Step 4: Get distinct state IDs
            var businessLocationGuids = query.Select(x => x.BusinessLocation);
            var registrationLocationGuids = query.Select(x => x.RegistrationLocation);

            var allLocationGuids = businessLocationGuids.Concat(registrationLocationGuids).Distinct().ToList();

            // Step 5: Create a dictionary of states based on the locationGuids
            var stateQuery = _appDbContext.States;
            var stateDictionary = await stateQuery
                .Where(x => allLocationGuids.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x);

            // Step 6: Fetch the requested data
            var response = await query
            .Select(x => new LegalSearchResponsePayload
            {
                Id = x.Id,
                RequestInitiator = x.RequestInitiator!,
                RequestType = x.RequestType!,
                RegistrationDate = x.RegistrationDate,
                RequestStatus = x.Status,
                CustomerAccountName = x.CustomerAccountName,
                CustomerAccountNumber = x.CustomerAccountNumber,
                BusinessLocation = stateDictionary.ContainsKey(x.BusinessLocation) ? stateDictionary[x.BusinessLocation].Name : string.Empty,
                RegistrationLocation = stateDictionary.ContainsKey(x.RegistrationLocation) ? stateDictionary[x.RegistrationLocation].Name : string.Empty,
                DateCreated = x.CreatedAt,
                DateDue = x.DateDue,
            })
            .ToListAsync();

            // Step 7: Calculate counts
            var counts = await CalculateCounts(response, csoId, query);

            // Step 8: Create the final payload
            var finalPayload = new CsoRootResponsePayload
            {
                LegalSearchRequests = response,
                TotalRequests = counts.totalRequests,
                WithinSLACount = counts.withinSLACount,
                ElapsedSLACount = counts.elapsedSLACount,
                Within3HoursToDueCount = counts.within3HoursToDueCount,
                RequestsWithLawyersFeedbackCount = counts.requestsWithLawyersFeedbackCount
            };

            return finalPayload;
        }

        private IQueryable<LegalRequest> ApplyFilters(StaffDashboardAnalyticsRequest request, Guid csoId, IQueryable<LegalRequest> query)
        {
            if (request.StartPeriod.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= request.StartPeriod);
            }

            if (request.EndPeriod.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= request.EndPeriod);
            }

            if (request.CsoRequestStatusType.HasValue)
            {
                query = FilterQueryBasedOnCsoRequestStatus(request, csoId, query);
            }

            return query;
        }

        private async Task<(int totalRequests, int withinSLACount, int elapsedSLACount, int within3HoursToDueCount, int requestsWithLawyersFeedbackCount)> CalculateCounts(List<LegalSearchResponsePayload> response, Guid csoId, IQueryable<LegalRequest> query)
        {
            var currentTime = TimeUtils.GetCurrentLocalTime();

            var totalRequests = response.Count;

            var withinSLACount = response.Count(x => x.DateDue != null &&
                currentTime > x.DateCreated &&
                currentTime < (x.DateDue?.AddHours(-3)));

            var elapsedSLACount = response.Count(x => x.DateDue != null &&
                currentTime > x.DateDue);

            var within3HoursToDueCount = response.Count(x => x.DateDue != null &&
                currentTime > (x.DateDue?.AddHours(-3)) &&
                currentTime <= x.DateDue);

            var requestsWithLawyersFeedbackQuery = query.Where(x => x.InitiatorId == csoId && x.Status == RequestStatusType.BackToCso.ToString());

            var requestsWithLawyersFeedbackCount = requestsWithLawyersFeedbackQuery != null ? await requestsWithLawyersFeedbackQuery.CountAsync() : 0;

            return (totalRequests, withinSLACount, elapsedSLACount, within3HoursToDueCount, requestsWithLawyersFeedbackCount);
        }


        public async Task<List<FinacleLegalSearchResponsePayload>> GetFinacleLegalRequestsForCso(GetFinacleRequest request, string solId)
        {
            // step 1: Make request queryable
            IQueryable<LegalRequest> query = _appDbContext.LegalSearchRequests;

            // step 2: filter based on request
            query = FilterRequestQuery(request, solId, query);

            // step 3: paginate query
            query = query.Paginate(request);

            // step 4: get distinct branch names
            var branch = await _appDbContext.Branches
                                            .FirstAsync(x => x.SolId == solId);

            // step 5: convert query to list
            var response = await query.Select(x => new FinacleLegalSearchResponsePayload
            {
                RequestId = x.Id,
                CustomerAccountName = x.CustomerAccountName,
                CustomerAccountNumber = x.CustomerAccountNumber,
                AccountBranchName = branch.Address,
                DateCreated = x.CreatedAt,
                RequestStatus = x.Status,
            }).ToListAsync();

            return response ?? new List<FinacleLegalSearchResponsePayload>();
        }

        private static IQueryable<LegalRequest> FilterRequestQuery(GetFinacleRequest request, string solId, IQueryable<LegalRequest> query)
        {
            query = query.Where(x => x.BranchId == solId && x.RequestSource == RequestSourceType.Finacle);

            if (request.StartPeriod.HasValue && request.EndPeriod.HasValue)
                query = query.Where(x =>
                    x.CreatedAt >= request.StartPeriod.Value && x.CreatedAt <= request.EndPeriod.Value);

            else if (request.StartPeriod.HasValue)
                query = query.Where(x => x.CreatedAt >= request.StartPeriod.Value);

            else if (request.EndPeriod.HasValue)
                query = query.Where(x => x.CreatedAt <= request.EndPeriod.Value);

            return query;
        }
    }
}
