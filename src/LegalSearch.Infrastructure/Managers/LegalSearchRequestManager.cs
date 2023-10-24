using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Fcmb.Shared.Utilities;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Models.Requests.CSO;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Application.Models.Responses.CSO;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.Json;

namespace LegalSearch.Infrastructure.Managers
{
    public class LegalSearchRequestManager : ILegalSearchRequestManager
    {
        private readonly AppDbContext _appDbContext;
        private readonly ILogger<LegalSearchRequestManager> _logger;

        public LegalSearchRequestManager(AppDbContext appDbContext, ILogger<LegalSearchRequestManager> logger)
        {
            _appDbContext = appDbContext;
            _logger = logger;
        }

        public async Task<bool> AddNewLegalSearchRequest(LegalRequest legalRequest)
        {
            await _appDbContext.LegalSearchRequests.AddAsync(legalRequest);

            return await _appDbContext.SaveChangesAsync() > 0;
        }

        public async Task<LegalSearchRootResponsePayload> GetLegalRequestsForSolicitor(SolicitorRequestAnalyticsPayload viewRequestAnalyticsPayload, Guid solicitorId)
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
            if (viewRequestAnalyticsPayload.StartPeriod.HasValue && viewRequestAnalyticsPayload.EndPeriod.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= viewRequestAnalyticsPayload.StartPeriod && x.CreatedAt <= viewRequestAnalyticsPayload.EndPeriod);
            }
            else if (viewRequestAnalyticsPayload.StartPeriod.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= viewRequestAnalyticsPayload.StartPeriod);
            }
            else if (viewRequestAnalyticsPayload.EndPeriod.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= viewRequestAnalyticsPayload.EndPeriod);
            }

            if (viewRequestAnalyticsPayload.RequestStatus.HasValue)
            {
                query = FilterQueryBasedOnRequestStatus(viewRequestAnalyticsPayload, solicitorId, query, assignedRequestIds);
            }
            else
            {
                query = query.Where(x => assignedRequestIds.Contains(x.Id));
            }

            // Step 7: Apply pagination to the query as per the request 
            query.Paginate(viewRequestAnalyticsPayload);

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
                    x.AssignedSolicitorId,
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

            var solicitorIds = response.Where(x => x.AssignedSolicitorId != Guid.Empty).SelectMany(x => new[] { x.AssignedSolicitorId })
                                       .Distinct()
                                       .ToList();

            var solicitors = _appDbContext.Users.Where(x => solicitorIds.Contains(x.Id)).ToDictionary(x => x.Id, x => $"{x.FirstName} {x.LastName}");

            var stateNames = _appDbContext.States
                                          .Include(x => x.Region)
                                          .Where(state => stateIds.Contains(state.Id))
                                          .ToDictionary(state => state.Id, state => state);

            // Step 10: Get the current time in the application's local time zone
            var currentTime = TimeUtils.GetCurrentLocalTime();

            // Step 11: Map the response data to the response payload
            var mappedResponse = response.Select(x => new LegalSearchResponsePayload
            {
                Id = x.Id,
                RequestInitiator = x.RequestInitiator!,
                RequestType = x.RequestType!,
                RegistrationDate = x.RegistrationDate,
                Region = x.BusinessLocation.HasValue && stateNames.ContainsKey(x.BusinessLocation.Value) ? stateNames[x.BusinessLocation.Value].Region.Name : string.Empty,
                RegionCode = x.BusinessLocation.HasValue && stateNames.ContainsKey(x.BusinessLocation.Value) ? stateNames[x.BusinessLocation.Value].Region.Id : Guid.Empty,
                RequestStatus = x.Status,
                CustomerAccountName = x.CustomerAccountName,
                CustomerAccountNumber = x.CustomerAccountNumber,
                RegistrationNumber = x.RegistrationNumber ?? string.Empty,
                BusinessLocation = x.BusinessLocation.HasValue && stateNames.ContainsKey(x.BusinessLocation.Value) ? stateNames[x.BusinessLocation.Value].Name : string.Empty,
                RegistrationLocation = x.RegistrationLocation.HasValue && stateNames.ContainsKey(x.RegistrationLocation.Value) ? stateNames[x.RegistrationLocation.Value].Name : string.Empty,
                BusinessLocationId = x.BusinessLocation.HasValue ? x.BusinessLocation.Value : Guid.Empty,
                RegistrationLocationId = x.RegistrationLocation.HasValue ? x.RegistrationLocation.Value : Guid.Empty,
                DateCreated = x.CreatedAt,
                Solicitor = x.AssignedSolicitorId.HasValue && solicitors.ContainsKey(x.AssignedSolicitorId.Value) ? solicitors[x.AssignedSolicitorId.Value] : string.Empty,
                ReasonOfCancellation = x.ReasonForCancelling ?? string.Empty,
                DateOfCancellation = x.DateOfCancellation,
                DateDue = x.DateDue.HasValue ? x.DateDue : DateTime.MinValue,
                Discussions = x.Discussions.Select(x => new DiscussionDto { Conversation = x.Conversation }).ToList(),
                SupportingDocuments = x.SupportingDocuments.Select(x => new RegistrationDocumentDto { FileName = x.FileName, FileContent = x.FileContent, FileType = x.FileType }).ToList(),
                RegistrationDocuments = x.RegistrationDocuments.Select(x => new RegistrationDocumentDto { FileName = x.FileName, FileContent = x.FileContent, FileType = x.FileType }).ToList(),
            }).ToList();

            // get the requests query 
            IQueryable<LegalRequest>? assignedRequestsCountQuery = rawQuery.Where(x => x.Status == RequestStatusType.LawyerAccepted.ToString() && x.AssignedSolicitorId == solicitorId);
            IQueryable<LegalRequest>? completedRequestsCountQuery = rawQuery.Where(x => x.Status == RequestStatusType.Completed.ToString() && x.AssignedSolicitorId == solicitorId);
            IQueryable<LegalRequest>? newRequestsCountQuery = rawQuery.Where(x => x.Status == RequestStatusType.AssignedToLawyer.ToString() && x.AssignedSolicitorId == solicitorId);
            IQueryable<LegalRequest>? rejectedRequestsCountQuery = rawQuery.Where(x => x.Status == RequestStatusType.LawyerRejected.ToString());
            IQueryable<LegalRequest>? returnedRequestsCountQuery = rawQuery.Where(x => x.Status == RequestStatusType.BackToCso.ToString() && x.AssignedSolicitorId == solicitorId);

            // Step 12: Calculate the counts for the three categories
            int assignedRequestsCount = await assignedRequestsCountQuery.CountAsync();
            int completedRequestsCount = await completedRequestsCountQuery.CountAsync();
            int rejectedRequestsCount = await rejectedRequestsCountQuery.CountAsync();
            int returnedRequestsCount = await returnedRequestsCountQuery.CountAsync();
            int newRequestsCount = await newRequestsCountQuery.CountAsync();

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

        private List<MonthlyRequestData> CalculateRequestsByMonth(List<LegalSearchResponsePayload> mappedResponse, DateTime currentTime)
        {
            // Initialize the dictionary with all months and counts set to zero
            var allMonths = Enumerable.Range(1, 12).Select(month => new DateTime(currentTime.Year, month, 1));

            var requestsByMonth = allMonths.Select(month => new MonthlyRequestData
            {
                Name = month.ToString("MMM"),
                New = 0,
                Comp = 0
            }).ToList();

            // Update the counts for months that have requests
            foreach (var request in mappedResponse)
            {
                var monthKey = request.DateCreated.ToString("MMM");
                var monthEntry = requestsByMonth.Find(m => m.Name == monthKey);

                if (monthEntry != null)
                {
                    if (request.RequestStatus == RequestStatusType.AssignedToLawyer.ToString()
                        || request.RequestStatus == RequestStatusType.BackToCso.ToString()
                        || request.RequestStatus == RequestStatusType.LawyerAccepted.ToString())
                    {
                        monthEntry.New++;
                    }
                    else if (request.RequestStatus == RequestStatusType.Completed.ToString())
                    {
                        monthEntry.Comp++;
                    }
                }
            }

            return requestsByMonth;
        }

        private List<MonthlyRequestData> CalculateRequestsByMonthForStaff(List<LegalSearchResponsePayload> allRecords, DateTime currentTime)
        {
            // Initialize the dictionary with all months and counts set to zero
            var allMonths = Enumerable.Range(1, 12).Select(month => new DateTime(currentTime.Year, month, 1));

            var requestsByMonth = allMonths.Select(month => new MonthlyRequestData
            {
                Name = month.ToString("MMM"),
                New = 0,
                Comp = 0
            }).ToList();

            // Update the counts for months that have requests
            foreach (var request in allRecords)
            {
                var monthKey = request.DateCreated.ToString("MMM");
                var monthEntry = requestsByMonth.Find(m => m.Name == monthKey);

                if (monthEntry != null)
                {
                    if (request.RequestStatus != RequestStatusType.Completed.ToString()
                        && request.RequestStatus != RequestStatusType.Cancelled.ToString())
                    {
                        monthEntry.New++;
                    }
                    else if (request.RequestStatus == RequestStatusType.Completed.ToString())
                    {
                        monthEntry.Comp++;
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

        private static IQueryable<LegalRequest> FilterQueryBasedOnCsoRequestStatus(StaffDashboardAnalyticsRequest request, IQueryable<LegalRequest> query)
        {
            switch (request.CsoRequestStatusType)
            {
                case CsoRequestStatusType.PendingWithCso:
                    query = query.Where(x => (x.Status == RequestStatusType.BackToCso.ToString()) ||
                                    (x.Status == RequestStatusType.Initiated.ToString() && x.RequestSource == RequestSourceType.Finacle));
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
                case CsoRequestStatusType.UnAssigned:
                    query = query.Where(x => x.Status == RequestStatusType.UnAssigned.ToString());
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

        public async Task<StaffRootResponsePayload> GetLegalRequestsForStaff(StaffDashboardAnalyticsRequest request)
        {

            // Step 1: Create the query to fetch legal requests
            IQueryable<LegalRequest> rawQuery = _appDbContext.LegalSearchRequests
                                                            .Include(x => x.Discussions)
                                                            .Include(x => x.RegistrationDocuments)
                                                            .Include(x => x.SupportingDocuments);

            var query = rawQuery;

            // Step 2: Apply filtering based on the request payload and pagination
            query = ApplyFilters(request, query).Paginate(request);

            // Step 3: Get distinct state IDs for locations
            var locationGuids = GetDistinctLocationGuids(query);

            // Step 4: Create a dictionary of states based on the locationGuids
            var stateDictionary = await GetStateDictionary(locationGuids);

            // Step 5: Create a dictionary of solicitors based on assigned solicitor IDs
            var solicitors = GetSolicitors(query);

            // Step 6: Fetch the requested data
            List<LegalSearchResponsePayload> response;

            if (request.RegionId != null && Guid.TryParse(request.RegionId, out Guid regionId))
            {
                // Step 6: Filter the query based on the region if specified
                var legalRequests = await query.ToListAsync();

                var regionQuery = GetRegionQuery(legalRequests, stateDictionary, regionId);

                response = await GenerateLegalSearchResponsePayload(regionQuery, stateDictionary, solicitors);
            }
            else
            {
                response = await GenerateLegalSearchResponsePayload(query, stateDictionary, solicitors);
            }

            // Step 7: Apply branch filter if a specific branch is specified in the request
            rawQuery = ApplyBranchFilter(request, rawQuery);
            List<LegalSearchResponsePayload> allRecords;

            if (request.RegionId != null && Guid.TryParse(request.RegionId, out Guid regionCode))
            {
                // Materialize the data into memory and then filter
                var legalRequests = await rawQuery.ToListAsync();

                // Step 8: Filter the raw query based on the region if specified
                var regionQuery = GetRegionQuery(legalRequests, stateDictionary, regionCode);
                allRecords = await GenerateLegalSearchResponsePayload(regionQuery, stateDictionary, solicitors);
            }
            else
            {
                allRecords = await GenerateLegalSearchResponsePayload(rawQuery, stateDictionary, solicitors);
            }

            // Step 9: Calculate counts for different request statuses
            var counts = await CalculateCounts(response, query, allRecords);

            // Step 10: Generate the requests bar chart for the staff
            var requestsByMonth = CalculateRequestsByMonthForStaff(allRecords, TimeUtils.GetCurrentLocalTime());

            // Step 11: Calculate average processing time for all records
            var averageProcessingTime = CalculateAverageProcessingTime(allRecords);

            // Step 12: Create the final payload with all relevant data
            var finalPayload = CreateFinalPayload(counts, requestsByMonth, response, averageProcessingTime);

            return finalPayload;
        }

        private StaffRootResponsePayload CreateFinalPayload((int totalRequests, int withinSLACount, int elapsedSLACount, int within3HoursToDueCount, int requestsWithLawyersFeedbackCount, int pendingRequestsCount, int completedRequestsCount,
            int openRequestsCount) counts, List<MonthlyRequestData> requestsByMonth, List<LegalSearchResponsePayload> response, string averageProcessingTime)
        {
            return new StaffRootResponsePayload
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
                RequestsWithLawyersFeedbackCount = counts.requestsWithLawyersFeedbackCount,
                AverageProcessingTime = averageProcessingTime
            };
        }

        private IQueryable<LegalRequest> ApplyBranchFilter(StaffDashboardAnalyticsRequest request, IQueryable<LegalRequest> query)
        {
            if (request.BranchId != null)
            {
                query = query.Where(x => x.BranchId == request.BranchId);
            }
            return query;
        }

        private List<LegalRequest> GetRegionQuery(List<LegalRequest> query, Dictionary<Guid, State> stateDictionary, Guid regionId)
        {
            return query.Where(x => x.BusinessLocation.HasValue && stateDictionary.ContainsKey(x.BusinessLocation.Value)
                                    && stateDictionary[x.BusinessLocation.Value].RegionId == regionId).ToList();
        }

        private IQueryable<Guid?> GetDistinctLocationGuids(IQueryable<LegalRequest> query)
        {
            return query.Select(x => x.BusinessLocation)
                        .Union(query.Select(x => x.RegistrationLocation))
                        .Distinct();
        }

        private Dictionary<Guid, string> GetSolicitors(IQueryable<LegalRequest> query)
        {
            var solicitorIds = query
                .Where(x => x.AssignedSolicitorId != Guid.Empty)
                .Select(x => x.AssignedSolicitorId)
                .Distinct()
                .ToList();

            return _appDbContext.Users.Where(x => solicitorIds.Contains(x.Id)).ToDictionary(x => x.Id, x => $"{x.FirstName} {x.LastName}");
        }

        private async Task<Dictionary<Guid, State>> GetStateDictionary(IQueryable<Guid?> locationGuids)
        {
            return await _appDbContext.States
                                    .Include(x => x.Region)
                                    .Where(location => locationGuids.Contains(location.Id))
                                    .ToDictionaryAsync(location => location.Id, location => location);
        }

        private string CalculateProcessingTime(LegalSearchResponsePayload record)
        {
            if (record.DateCreated == default || record.RequestSubmissionDate == null)
            {
                return "N/A"; // or any other indicator for data not available
            }

            // Calculate the processing time in hours
            TimeSpan processingTime = record.RequestSubmissionDate.Value - record.DateCreated;
            double processingTimeHours = processingTime.TotalHours;

            // Format the processing time as a string
            return $"{processingTimeHours:F2} hrs";
        }


        private string CalculateAverageProcessingTime(List<LegalSearchResponsePayload> allRecords)
        {
            double totalProcessingTimeHours = 0;
            int validRecordCount = 0;

            foreach (var record in allRecords)
            {
                string processingTimeStr = CalculateProcessingTime(record);

                // Skip records with "N/A" processing time (indicating null or invalid data)
                if (processingTimeStr == "N/A")
                    continue;

                double processingTimeHours = double.Parse(processingTimeStr.Replace(" hrs", ""));
                totalProcessingTimeHours += processingTimeHours;
                validRecordCount++;
            }

            if (validRecordCount == 0)
                return "N/A"; // or any other indicator for no valid data

            double averageProcessingTimeHours = totalProcessingTimeHours / validRecordCount;

            return $"{averageProcessingTimeHours:F2} hrs";
        }


        private async Task<List<LegalSearchResponsePayload>> GenerateLegalSearchResponsePayload(IQueryable<LegalRequest> query, Dictionary<Guid, State> stateDictionary, Dictionary<Guid, string> solicitors)
        {
            try
            {
                var data = await query
                .Select(legalSearch => new LegalSearchResponsePayload
                {
                    Id = legalSearch.Id,
                    RequestInitiator = legalSearch.RequestInitiator!,
                    RequestType = legalSearch.RequestType!,
                    RegistrationDate = legalSearch.RegistrationDate,
                    RequestStatus = legalSearch.Status,
                    RequestSubmissionDate = legalSearch.RequestSubmissionDate,
                    Solicitor = legalSearch.AssignedSolicitorId.HasValue && solicitors.ContainsKey(legalSearch.AssignedSolicitorId.Value) ? solicitors[legalSearch.AssignedSolicitorId.Value] : string.Empty,
                    RegistrationNumber = legalSearch.RegistrationNumber ?? string.Empty,
                    CustomerAccountName = legalSearch.CustomerAccountName,
                    CustomerAccountNumber = legalSearch.CustomerAccountNumber,
                    Region = legalSearch.BusinessLocation.HasValue && stateDictionary.ContainsKey(legalSearch.BusinessLocation.Value) ? stateDictionary[legalSearch.BusinessLocation.Value].Region.Name : string.Empty,
                    RegionCode = legalSearch.BusinessLocation.HasValue && stateDictionary.ContainsKey(legalSearch.BusinessLocation.Value) ? stateDictionary[legalSearch.BusinessLocation.Value].Region.Id : Guid.Empty,
                    BusinessLocation = legalSearch.BusinessLocation.HasValue && stateDictionary.ContainsKey(legalSearch.BusinessLocation.Value) ? stateDictionary[legalSearch.BusinessLocation.Value].Name : string.Empty,
                    RegistrationLocation = legalSearch.RegistrationLocation.HasValue && stateDictionary.ContainsKey(legalSearch.RegistrationLocation.Value) ? stateDictionary[legalSearch.RegistrationLocation.Value].Name : string.Empty,
                    BusinessLocationId = legalSearch.BusinessLocation.HasValue ? legalSearch.BusinessLocation.Value : Guid.Empty,
                    RegistrationLocationId = legalSearch.RegistrationLocation.HasValue ? legalSearch.RegistrationLocation.Value : Guid.Empty,
                    DateCreated = legalSearch.CreatedAt,
                    ReasonOfCancellation = legalSearch.ReasonForCancelling ?? string.Empty,
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

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred inside GenerateLegalSearchResponsePayload. See reason: {JsonSerializer.Serialize(ex)}");
                throw;
            }
        }

        private async Task<List<LegalSearchResponsePayload>> GenerateLegalSearchResponsePayload(List<LegalRequest> query, Dictionary<Guid, State> stateDictionary, Dictionary<Guid, string> solicitors)
        {
            try
            {
                var data = await Task.Run(() =>
                                query.Select(legalSearch => new LegalSearchResponsePayload
                                {
                                    Id = legalSearch.Id,
                                    RequestInitiator = legalSearch.RequestInitiator!,
                                    RequestType = legalSearch.RequestType!,
                                    RegistrationDate = legalSearch.RegistrationDate,
                                    RequestStatus = legalSearch.Status,
                                    RequestSubmissionDate = legalSearch.RequestSubmissionDate,
                                    Solicitor = legalSearch.AssignedSolicitorId.HasValue && solicitors.ContainsKey(legalSearch.AssignedSolicitorId.Value) ? solicitors[legalSearch.AssignedSolicitorId.Value] : string.Empty,
                                    RegistrationNumber = legalSearch.RegistrationNumber ?? string.Empty,
                                    CustomerAccountName = legalSearch.CustomerAccountName,
                                    CustomerAccountNumber = legalSearch.CustomerAccountNumber,
                                    Region = legalSearch.BusinessLocation.HasValue && stateDictionary.ContainsKey(legalSearch.BusinessLocation.Value) ? stateDictionary[legalSearch.BusinessLocation.Value].Region.Name : string.Empty,
                                    RegionCode = legalSearch.BusinessLocation.HasValue && stateDictionary.ContainsKey(legalSearch.BusinessLocation.Value) ? stateDictionary[legalSearch.BusinessLocation.Value].Region.Id : Guid.Empty,
                                    BusinessLocation = legalSearch.BusinessLocation.HasValue && stateDictionary.ContainsKey(legalSearch.BusinessLocation.Value) ? stateDictionary[legalSearch.BusinessLocation.Value].Name : string.Empty,
                                    RegistrationLocation = legalSearch.RegistrationLocation.HasValue && stateDictionary.ContainsKey(legalSearch.RegistrationLocation.Value) ? stateDictionary[legalSearch.RegistrationLocation.Value].Name : string.Empty,
                                    BusinessLocationId = legalSearch.BusinessLocation.HasValue ? legalSearch.BusinessLocation.Value : Guid.Empty,
                                    RegistrationLocationId = legalSearch.RegistrationLocation.HasValue ? legalSearch.RegistrationLocation.Value : Guid.Empty,
                                    DateCreated = legalSearch.CreatedAt,
                                    ReasonOfCancellation = legalSearch.ReasonForCancelling ?? string.Empty,
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
                                }).ToList());

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred inside GenerateLegalSearchResponsePayload. See reason: {JsonSerializer.Serialize(ex)}");
                throw;
            }
        }

        private IQueryable<LegalRequest> ApplyFilters(StaffDashboardAnalyticsRequest request, IQueryable<LegalRequest> query)
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

            if (request.SearchType.HasValue)
            {
                query = FilterQueryBasedOnSearchType(request, query);
            }

            if (request.BranchId != null)
            {
                query = query.Where(x => x.BranchId == request.BranchId);
            }

            return query;
        }

        private IQueryable<LegalRequest> FilterQueryBasedOnSearchType(StaffDashboardAnalyticsRequest request, IQueryable<LegalRequest> query)
        {
            // Exclude these statuses from all cases
            query = query.Where(x => x.Status != RequestStatusType.BackToCso.ToString() && x.Status != RequestStatusType.Cancelled.ToString());

            switch (request.SearchType)
            {
                case SearchType.WithinTAT:
                    query = query.Where(x => x.DateDue.HasValue && x.DateDue.Value >= TimeUtils.GetCurrentLocalTime());
                    break;
                case SearchType.OutsideTAT:
                    query = query.Where(x => x.DateDue.HasValue && x.DateDue.Value < TimeUtils.GetCurrentLocalTime());
                    break;
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
            var completedRequestsCount = allRecords.Count(x => x.RequestStatus == RequestStatusType.Completed.ToString());
            var openRequestsCount = allRecords.Count(x => x.RequestStatus != RequestStatusType.Completed.ToString());

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
    }
}
