using Fcmb.Shared.Utilities;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Models.Requests.CSO;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Application.Models.Responses.CSO;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Domain.Entities.User.Solicitor;
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

        public async Task<LegalSearchRootResponsePayload> GetLegalRequestsForSolicitor(SolicitorRequestAnalyticsPayload request, Guid solicitorId)
        {
            // Step 1: Create the query to fetch legal requests
            IQueryable<LegalRequest> rawQuery = _appDbContext.LegalSearchRequests
                                                          .Include(x => x.Discussions)
                                                          .Include(x => x.SupportingDocuments)
                                                          .Include(x => x.SupportingDocuments);

            var query = rawQuery;

            // Step 2: Retrieve the solicitor assignments for the given solicitor
            var solicitorAssignments = await _appDbContext.SolicitorAssignments
                                                          .Where(assignment => assignment.SolicitorId == solicitorId)
                                                          .ToListAsync();

            // Step 3: Check if no solicitor assignments were found
            if (solicitorAssignments == null || solicitorAssignments.Count == 0)
            {
                // No solicitor assignments found, return empty summary
                return new LegalSearchRootResponsePayload
                {
                    LegalSearchRequests = new List<LegalSearchResponsePayload>(),
                    TotalRequestsCount = 0,
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
            else
            {
                query = query.Where(x => assignedRequestIds.Contains(x.Id));
            }

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
                    x.RegistrationNumber,
                    x.Status,
                    x.CustomerAccountName,
                    x.CustomerAccountNumber,
                    x.BusinessLocation,
                    x.RegistrationLocation,
                    x.CreatedAt,
                    x.DateDue,
                    x.ReasonForCancelling,
                    x.DateOfCancellation,
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
                RegistrationNumber = x.RegistrationNumber,
                BusinessLocation = stateNames.ContainsKey(x.BusinessLocation) ? stateNames[x.BusinessLocation] : string.Empty,
                RegistrationLocation = stateNames.ContainsKey(x.RegistrationLocation) ? stateNames[x.RegistrationLocation] : string.Empty,
                BusinessLocationId = x.BusinessLocation,
                RegistrationLocationId = x.RegistrationLocation,
                DateCreated = x.CreatedAt,
                ReasonOfCancellation = x.ReasonForCancelling,
                DateOfCancellation = x.DateOfCancellation,
                DateDue = x.DateDue.HasValue ? x.DateDue : DateTime.MinValue,
                Discussions = x.Discussions.Select(x => new DiscussionDto { Conversation = x.Conversation }).ToList(),
                SupportingDocuments = x.SupportingDocuments.Select(x => new RegistrationDocumentDto { FileName = x.FileName, FileContent = x.FileContent, FileType = x.FileType }).ToList(),
                RegistrationDocuments = x.RegistrationDocuments.Select(x => new RegistrationDocumentDto { FileName = x.FileName, FileContent = x.FileContent, FileType = x.FileType }).ToList(),
            }).ToList();

            // get the requests query 
            var assignedRequestsCountQuery = rawQuery.Where(x => x.Status == RequestStatusType.LawyerAccepted.ToString() && x.AssignedSolicitorId == solicitorId);
            var completedRequestsCountQuery = rawQuery.Where(x => x.Status == RequestStatusType.Completed.ToString() && x.AssignedSolicitorId == solicitorId);
            var newRequestsCountQuery = rawQuery.Where(x => x.Status == RequestStatusType.AssignedToLawyer.ToString() && x.AssignedSolicitorId == solicitorId);
            var rejectedRequestsCountQuery = rawQuery.Where(x => x.Status == RequestStatusType.LawyerRejected.ToString() && x.AssignedSolicitorId == solicitorId);
            var returnedRequestsCountQuery = rawQuery.Where(x => x.Status == RequestStatusType.BackToCso.ToString() && x.AssignedSolicitorId == solicitorId);

            // Step 12: Calculate the counts for the three categories
            var assignedRequestsCount = assignedRequestsCountQuery != null ? await assignedRequestsCountQuery.CountAsync() : 0;
            var completedRequestsCount = completedRequestsCountQuery != null ? await completedRequestsCountQuery.CountAsync() : 0;
            var rejectedRequestsCount = rejectedRequestsCountQuery != null ? await rejectedRequestsCountQuery.CountAsync() : 0;
            var returnedRequestsCount = returnedRequestsCountQuery != null ? await returnedRequestsCountQuery.CountAsync() : 0;
            var newRequestsCount = newRequestsCountQuery != null ? await newRequestsCountQuery.CountAsync() : 0;

            // Calculate requests by month
            var requestsByMonth = CalculateRequestsByMonth(mappedResponse, TimeUtils.GetCurrentLocalTime());

            // Step 13: Map the response data to the response payload
            var summary = new LegalSearchRootResponsePayload
            {
                AssignedRequestsCount = assignedRequestsCount,
                CompletedRequestsCount = completedRequestsCount,
                RejectedRequestsCount = rejectedRequestsCount,
                ReturnedRequestsCount = returnedRequestsCount,
                NewRequestsCount = newRequestsCount,
                LegalSearchRequests = mappedResponse,
                TotalRequestsCount = mappedResponse.Count,
                WithinSLACount = mappedResponse.Count(x => x.DateDue != null && currentTime > x.DateCreated && currentTime < x.DateDue?.AddHours(-3)),
                ElapsedSLACount = mappedResponse.Count(x => x.DateDue != null && currentTime > x.DateDue),
                Within3HoursToDueCount = mappedResponse.Count(x => x.DateDue != null && currentTime > x.DateDue?.AddHours(-3) && currentTime <= x.DateDue),
                RequestsByMonth = requestsByMonth 
            };

            return summary;
        }

        private Dictionary<string, Dictionary<string, int>> CalculateRequestsByMonth(List<LegalSearchResponsePayload> mappedResponse, DateTime currentTime)
        {
            // Initialize the dictionary with all months and counts set to zero
            var allMonths = Enumerable.Range(1, 12).Select(month => new DateTime(currentTime.Year, month, 1));

            var requestsByMonth = allMonths.ToDictionary(
                month => month.ToString("MMM"),
                _ => new Dictionary<string, int> { { "new", 0 }, { "comp", 0 } }
            );

            // Update the counts for months that have requests
            foreach (var request in mappedResponse)
            {
                var monthKey = request.DateCreated.ToString("MMM");
                if (requestsByMonth.ContainsKey(monthKey))
                {
                    if (request.Status == RequestStatusType.AssignedToLawyer.ToString())
                    {
                        requestsByMonth[monthKey]["new"]++;
                    }
                    else if (request.Status == RequestStatusType.Completed.ToString())
                    {
                        requestsByMonth[monthKey]["comp"]++;
                    }
                }
            }

            return requestsByMonth;
        }

        private Dictionary<string, Dictionary<string, int>> CalculateRequestsByMonthForCso(List<LegalSearchResponsePayload> allRecords, DateTime currentTime)
        {
            // Initialize the dictionary with all months and counts set to zero
            var allMonths = Enumerable.Range(1, 12).Select(month => new DateTime(currentTime.Year, month, 1));

            var requestsByMonth = allMonths.ToDictionary(
                month => month.ToString("MMM"),
                _ => new Dictionary<string, int> { { "open", 0 }, { "completed", 0 } }
            );

            // Update the counts for months that have requests
            foreach (var request in allRecords)
            {
                var monthKey = request.DateCreated.ToString("MMM");
                if (requestsByMonth.ContainsKey(monthKey))
                {
                    if (request.Status != RequestStatusType.Completed.ToString()
                        && request.Status != RequestStatusType.Cancelled.ToString())
                    {
                        requestsByMonth[monthKey]["open"]++;
                    }
                    else if (request.Status == RequestStatusType.Completed.ToString())
                    {
                        requestsByMonth[monthKey]["completed"]++;
                    }
                }
            }

            return requestsByMonth;
        }

        private static IQueryable<LegalRequest> FilterQueryBasedOnRequestStatus(SolicitorRequestAnalyticsPayload request, Guid solicitorId, IQueryable<LegalRequest> query, List<Guid> assignedRequestIds)
        {
            switch (request.RequestStatus)
            {
                case SolicitorRequestStatusType.NewRequest:
                    query = query.Where(x => x.Status == RequestStatusType.AssignedToLawyer.ToString()
                    && x.AssignedSolicitorId == solicitorId);
                    break;
                case SolicitorRequestStatusType.AssignedToLawyer:
                    query = query.Where(x => x.Status == RequestStatusType.LawyerAccepted.ToString()
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
                case SolicitorRequestStatusType.Returned:
                    query = query.Where(x => x.Status == RequestStatusType.BackToCso.ToString()
                    && x.AssignedSolicitorId == solicitorId);
                    break;
            }

            return query;
        }

        private static IQueryable<LegalRequest> FilterQueryBasedOnCsoRequestStatus(CsoDashboardAnalyticsRequest request, IQueryable<LegalRequest> query)
        {
            switch (request.CsoRequestStatusType)
            {
                case CsoRequestStatusType.PendingWithCso:
                    query = query.Where(x => (x.Status == RequestStatusType.BackToCso.ToString()) ||
                                    (x.Status == RequestStatusType.UnAssigned.ToString() && x.RequestSource == RequestSourceType.Finacle));
                    break;
                case CsoRequestStatusType.PendingWithSolicitor:
                    query = query.Where(x => x.Status == RequestStatusType.LawyerAccepted.ToString());
                    break;
                case CsoRequestStatusType.CancelledRequest:
                    query = query.Where(x => x.Status == RequestStatusType.Cancelled.ToString());
                    break;
                case CsoRequestStatusType.Completed:
                    query = query.Where(x => x.Status == RequestStatusType.Completed.ToString());
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

        public async Task<CsoRootResponsePayload> GetLegalRequestsForStaff(CsoDashboardAnalyticsRequest request)
        {
            // Step 1: Create the query to fetch legal requests
            IQueryable<LegalRequest> rawQuery = _appDbContext.LegalSearchRequests
                                                            .Include(x => x.Discussions)
                                                            .Include(x => x.RegistrationDocuments)
                                                            .Include(x => x.SupportingDocuments);

            var query = rawQuery;

            // Step 2: Apply filtering based on the request payload
            query = ApplyFilters(request, query);

            // Step 3: Apply pagination to the query as per the request 
            query = query.Paginate(request);

            // Step 4: Get distinct state IDs
            var locationGuids = query.Select(x => x.BusinessLocation)
                                    .Union(query.Select(x => x.RegistrationLocation))
                                    .Distinct()
                                    .ToList();

            // Step 5: Create a dictionary of states based on the locationGuids
            var stateDictionary = await _appDbContext.States
                                                     .Where(location => locationGuids.Contains(location.Id))
                                                     .ToDictionaryAsync(location => location.Id, location => location);

            // Step 6: Fetch the requested data
            List<LegalSearchResponsePayload> response = await GenerateLegalSearchResponsePayload(query, stateDictionary);

            var allRecords = await GenerateLegalSearchResponsePayload(rawQuery, stateDictionary);

            // Step 7: Calculate counts
            var counts = await CalculateCounts(response, query, allRecords);

            // step 8: Generate the requests bar chart
            var requestsByMonth = CalculateRequestsByMonthForCso(allRecords, TimeUtils.GetCurrentLocalTime());

            // Step 9: Create the final payload
            var finalPayload = new CsoRootResponsePayload
            {
                CompletedRequests = counts.completedRequestsCount,
                OpenRequests = counts.openRequestsCount,
                RequestsCountBarChart = requestsByMonth,
                PendingRequests = counts.pendingRequestsCount,
                LegalSearchRequests = response,
                TotalRequests = counts.totalRequests,
                WithinSLACount = counts.withinSLACount,
                ElapsedSLACount = counts.elapsedSLACount,
                Within3HoursToSLACount = counts.within3HoursToDueCount,
                RequestsWithLawyersFeedbackCount = counts.requestsWithLawyersFeedbackCount
            };

            return finalPayload;
        }

        private static async Task<List<LegalSearchResponsePayload>> GenerateLegalSearchResponsePayload(IQueryable<LegalRequest> query, Dictionary<Guid, State> stateDictionary)
        {
            return await query
            .Select(legalSearch => new LegalSearchResponsePayload
            {
                Id = legalSearch.Id,
                RequestInitiator = legalSearch.RequestInitiator!,
                RequestType = legalSearch.RequestType!,
                RegistrationDate = legalSearch.RegistrationDate,
                RequestStatus = legalSearch.Status,
                RegistrationNumber = legalSearch.RegistrationNumber,
                CustomerAccountName = legalSearch.CustomerAccountName,
                CustomerAccountNumber = legalSearch.CustomerAccountNumber,
                BusinessLocation = stateDictionary.ContainsKey(legalSearch.BusinessLocation) ? stateDictionary[legalSearch.BusinessLocation].Name : string.Empty,
                RegistrationLocation = stateDictionary.ContainsKey(legalSearch.RegistrationLocation) ? stateDictionary[legalSearch.RegistrationLocation].Name : string.Empty,
                BusinessLocationId = legalSearch.BusinessLocation,
                RegistrationLocationId = legalSearch.RegistrationLocation,
                DateCreated = legalSearch.CreatedAt,
                ReasonOfCancellation = legalSearch.ReasonForCancelling,
                DateOfCancellation = legalSearch.DateOfCancellation,
                DateDue = legalSearch.DateDue,
                RegistrationDocuments = legalSearch.RegistrationDocuments.Select(x => new RegistrationDocumentDto
                {
                    FileContent = x.FileContent,
                    FileName = x.FileName,
                    FileType = x.FileType
                }).ToList(),
                SupportingDocuments = legalSearch.SupportingDocuments.Select(x => new RegistrationDocumentDto
                {
                    FileContent = x.FileContent,
                    FileName = x.FileName,
                    FileType = x.FileType
                }).ToList(),
                Discussions = legalSearch.Discussions.Select(x => new DiscussionDto
                { Conversation = x.Conversation }).ToList(),
            })
            .ToListAsync();
        }

        private IQueryable<LegalRequest> ApplyFilters(CsoDashboardAnalyticsRequest request, IQueryable<LegalRequest> query)
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
                query = FilterQueryBasedOnCsoRequestStatus(request, query);
            }

            return query;
        }

        private IQueryable<LegalRequest> ApplyFilters(CsoBranchDashboardAnalyticsRequest request, IQueryable<LegalRequest> query)
        {
            if (request.StartPeriod.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= request.StartPeriod);
            }

            if (request.EndPeriod.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= request.EndPeriod);
            }

            if (request.BranchId != null)
            {
                query = query.Where(x => x.BranchId == request.BranchId);
            }

            return query;
        }

        private async Task<(int totalRequests, int withinSLACount, int elapsedSLACount, int within3HoursToDueCount, int requestsWithLawyersFeedbackCount, int pendingRequestsCount, int completedRequestsCount,
            int openRequestsCount)> CalculateCounts(List<LegalSearchResponsePayload> response, IQueryable<LegalRequest> query, List<LegalSearchResponsePayload> allRecords)
        {
            var currentTime = TimeUtils.GetCurrentLocalTime();

            // Calculate the total number of requests based on the response list.
            var totalRequests = response.Count;

            // Calculate the number of requests within SLA (Service Level Agreement).
            var withinSLACount = response.Count(x => x.DateDue != null &&
                currentTime > x.DateCreated &&
                currentTime < (x.DateDue?.AddHours(-3)));

            // Calculate the number of requests where the SLA has elapsed.
            var elapsedSLACount = response.Count(x => x.DateDue != null &&
                currentTime > x.DateDue);

            // Calculate the number of requests within 3 hours to due date.
            var within3HoursToDueCount = response.Count(x => x.DateDue != null &&
                currentTime > (x.DateDue?.AddHours(-3)) &&
                currentTime <= x.DateDue);

            // Filter and count pending requests based on specific criteria.
            var pendingRequestsCountQuery = query
                .Where(x => (x.Status == RequestStatusType.BackToCso.ToString()) ||
                            (x.Status == RequestStatusType.UnAssigned.ToString() && x.RequestSource == RequestSourceType.Finacle));

            var pendingRequestsCount = await pendingRequestsCountQuery.CountAsync();

            // Filter and count requests with lawyers' feedback based on specific criteria.
            var requestsWithLawyersFeedbackQuery = query.Where(x => x.Status == RequestStatusType.BackToCso.ToString());
            var requestsWithLawyersFeedbackCount = await requestsWithLawyersFeedbackQuery.CountAsync();

            // Count completed and open requests based on the entire dataset (allRecords).
            var completedRequestsCount = allRecords.Count(x => x.Status == RequestStatusType.Completed.ToString());
            var openRequestsCount = allRecords.Count(x => x.Status != RequestStatusType.Completed.ToString());

            // Return the calculated counts as a tuple.
            return (totalRequests, withinSLACount, elapsedSLACount, within3HoursToDueCount, requestsWithLawyersFeedbackCount, pendingRequestsCount, completedRequestsCount, openRequestsCount);
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

        public async Task<BranchLegalSearchResponsePayload> GetBranchLegalRequestsForStaff(CsoBranchDashboardAnalyticsRequest request)
        {
            // Step 1: Create the query to fetch legal requests
            IQueryable<LegalRequest> rawQuery = _appDbContext.LegalSearchRequests
                                                            .Include(x => x.Discussions)
                                                            .Include(x => x.RegistrationDocuments)
                                                            .Include(x => x.SupportingDocuments);

            var query = rawQuery;

            // Step 2: Apply filtering based on the request payload
            query = ApplyFilters(request, query);

            // Step 3: Apply pagination to the query as per the request 
            query = query.Paginate(request);

            // Step 4: Get distinct state IDs
            var locationGuids = query.Select(x => x.BusinessLocation)
                                    .Union(query.Select(x => x.RegistrationLocation))
                                    .Distinct()
                                    .ToList();

            // Step 5: Create a dictionary of states based on the locationGuids
            var stateDictionary = await _appDbContext.States
                                                     .Where(location => locationGuids.Contains(location.Id))
                                                     .ToDictionaryAsync(location => location.Id, location => location);

            // Step 6: Fetch the requested data
            List<LegalSearchResponsePayload> response = await GenerateLegalSearchResponsePayload(query, stateDictionary);

            rawQuery = rawQuery.Where(x => x.BranchId == request.BranchId);
            var allRecords = await GenerateLegalSearchResponsePayload(rawQuery, stateDictionary);

            // Step 7: Calculate counts
            var counts = await CalculateCounts(response, query, allRecords);

            // step 8: Generate the requests bar chart
            var requestsByMonth = CalculateRequestsByMonthForCso(allRecords, TimeUtils.GetCurrentLocalTime());

            // Step 9: Create the final payload
            var finalPayload = new BranchLegalSearchResponsePayload
            {
                CompletedRequestsCount = counts.completedRequestsCount,
                OpenRequestsCount = counts.openRequestsCount,
                RequestsCountBarChart = requestsByMonth,
                LegalSearchRequests = response,
            };

            return finalPayload;
        }
    }
}
