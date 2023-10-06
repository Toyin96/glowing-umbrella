using LegalSearch.Application.Interfaces.BackgroundService;
using LegalSearch.Application.Interfaces.FCMBService;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Requests.CSO;
using LegalSearch.Application.Models.Requests.Notification;
using LegalSearch.Application.Models.Requests.User;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Application.Models.Responses.ZSM;
using LegalSearch.Domain.ApplicationMessages;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Domain.Enums.Notification;
using LegalSearch.Domain.Enums.Role;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LegalSearch.Infrastructure.Services.BackgroundService
{
    public class BackgroundService : IBackgroundService
    {
        private readonly AppDbContext _appDbContext;
        private readonly IEnumerable<INotificationService> _notificationServices;
        private readonly ISolicitorManager _solicitorManager;
        private readonly IStateRetrieveService _stateRetrieveService;
        private readonly ILegalSearchRequestManager _legalSearchRequestManager;
        private readonly IFCMBService _fCMBService;
        private readonly ILegalSearchRequestPaymentLogManager _legalSearchRequestPaymentLogManager;
        private readonly UserManager<Domain.Entities.User.User> _userManager;
        private readonly IZonalManagerService _zonalManagerService;
        private readonly IEmailService _emailService;
        private readonly FCMBServiceAppConfig _options;
        private readonly string _successStatusCode = "00";
        private readonly string _successStatusDescription = "SUCCESS";
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve };

        public BackgroundService(AppDbContext appDbContext,
            IEnumerable<INotificationService> notificationService,
            ISolicitorManager solicitorManager,
            IStateRetrieveService stateRetrieveService,
            ILegalSearchRequestManager legalSearchRequestManager,
            IFCMBService fCMBService, IOptions<FCMBServiceAppConfig> options,
            ILegalSearchRequestPaymentLogManager legalSearchRequestPaymentLogManager,
            UserManager<Domain.Entities.User.User> userManager,
            IZonalManagerService zonalManagerService,
            IEmailService emailService)
        {
            _appDbContext = appDbContext;
            _notificationServices = notificationService;
            _solicitorManager = solicitorManager;
            _stateRetrieveService = stateRetrieveService;
            _legalSearchRequestManager = legalSearchRequestManager;
            _fCMBService = fCMBService;
            _legalSearchRequestPaymentLogManager = legalSearchRequestPaymentLogManager;
            _userManager = userManager;
            _zonalManagerService = zonalManagerService;
            _emailService = emailService;
            _options = options.Value;
        }
        public async Task AssignRequestToSolicitorsJob(Guid requestId)
        {
            try
            {
                // Load the request and perform assignment logic
                var request = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);

                if (request == null) return;

                // check if request had been completed
                if (request.Status == nameof(RequestStatusType.Completed)) return;

                // Solicitor assignment logic is done here
                var solicitors = await _solicitorManager.DetermineSolicitors(request);

                if (solicitors == null || solicitors?.ToList()?.Count == 0)
                {
                    // reroute to other states in the same region
                    // Fetch solicitors in other states within the same region
                    var region = await _stateRetrieveService.GetRegionOfState(request.BusinessLocation);
                    solicitors = await _solicitorManager.FetchSolicitorsInSameRegion(region);

                    var solicitorsList = solicitors.ToList();

                    if (solicitorsList == null || solicitorsList?.Count == 0)
                    {
                        // update legalSearch request here
                        request.AssignedSolicitorId = Guid.Empty;
                        request.Status = RequestStatusType.UnAssigned.ToString();
                        await _legalSearchRequestManager.UpdateLegalSearchRequest(request);

                        // Route to Legal Perfection Team
                        await NotifyLegalPerfectionTeam(request);
                        return;
                    }

                    // Assign order to new solicitors in the same region
                    await AssignOrdersAsync(requestId, solicitorsList!);

                    // route request to first solicitor based on order arrangement
                    await PushRequestToNextSolicitorInOrder(requestId);

                    return; // end process
                }

                // Update the request status and assigned orders to available solicitor(s)
                await AssignOrdersAsync(requestId, solicitors.ToList());

                // route request to first solicitor based on order arrangement
                await PushRequestToNextSolicitorInOrder(requestId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception was thrown inside AssignRequestToSolicitorsJob. See:::{JsonSerializer.Serialize(ex, _serializerOptions)}");
            }
        }

        public async Task CheckAndRerouteRequestsJob()
        {
            List<Guid> solicitorAssignmentRecords = new List<Guid>();

            try
            {
                // Implement logic to query for requests with elapsed SLA and re-assign them accordingly
                var requestsToReroute = await _solicitorManager.GetRequestsToReroute();

                foreach (var request in requestsToReroute)
                {
                    // Get the legalRequest entity and check its status
                    var legalSearchRequest = await _legalSearchRequestManager.GetLegalSearchRequest(request);

                    if (legalSearchRequest!.Status == nameof(RequestStatusType.UnAssigned)
                        || legalSearchRequest.Status != nameof(RequestStatusType.LawyerAccepted))
                    {
                        /*
                         This request has been routed to legalPerfection team either due to:
                            1. No matching solicitor
                                    OR
                          The solicitor has pushed it back to the CSO for more info
                         */
                        continue;
                    }

                    // get the currently assigned solicitor, know his/her order and route it to the next order
                    var currentlyAssignedSolicitor = await _solicitorManager.GetCurrentSolicitorMappedToRequest(request,
                        legalSearchRequest.AssignedSolicitorId);

                    if (currentlyAssignedSolicitor != null)
                    {
                        solicitorAssignmentRecords.Add(currentlyAssignedSolicitor.Id);
                    }

                    // get current assignment order
                    int currentAssignmentOrder = currentlyAssignedSolicitor != null ? currentlyAssignedSolicitor.Order : 0;

                    await PushRequestToNextSolicitorInOrder(request, currentAssignmentOrder);
                }

                if (requestsToReroute?.Any() == true)
                {
                    await _solicitorManager.UpdateManySolicitorAssignmentStatuses(solicitorAssignmentRecords);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception was thrown inside CheckAndRerouteRequestsJob. See:::{JsonSerializer.Serialize(ex, _serializerOptions)}");
            }
        }

        public async Task PushRequestToNextSolicitorInOrder(Guid requestId, int currentAssignedSolicitorOrder = 0)
        {
            try
            {
                var request = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);

                if (request == null) return;

                // Get the next solicitor in line
                var nextSolicitor = await _solicitorManager.GetNextSolicitorInLine(requestId, currentAssignedSolicitorOrder);

                if (nextSolicitor == null)
                {
                    //mark request as 'UnAssigned'
                    request.AssignedSolicitorId = Guid.Empty;
                    request.Status = RequestStatusType.UnAssigned.ToString();
                    request.UpdatedAt = TimeUtils.GetCurrentLocalTime();
                    await _legalSearchRequestManager.UpdateLegalSearchRequest(request);

                    // Route to Legal Perfection Team
                    await NotifyLegalPerfectionTeam(request!);

                    return;
                }

                // logged time request was assigned to solicitor
                nextSolicitor.AssignedAt = TimeUtils.GetCurrentLocalTime();
                nextSolicitor.IsCurrentlyAssigned = true;
                nextSolicitor.UpdatedAt = TimeUtils.GetCurrentLocalTime();

                // Update the request status and assigned solicitor(s)
                request = UpdateLegalSearchRecordAfterBeingAssignedToSolicitor(request, nextSolicitor);

                // Send notification to the solicitor
                var notification = new Domain.Entities.Notification.Notification
                {
                    Title = ConstantTitle.NewRequestAssignmentTitle,
                    NotificationType = NotificationType.AssignedToSolicitor,
                    RecipientUserId = nextSolicitor.SolicitorId.ToString(),
                    RecipientUserEmail = nextSolicitor.SolicitorEmail,
                    SolId = request.BranchId,
                    Message = ConstantMessage.NewRequestAssignmentMessage,
                    MetaData = JsonSerializer.Serialize(request, _serializerOptions)
                };

                // Notify solicitor of new request
                _notificationServices.ToList().ForEach(x => x.NotifyUser(request.InitiatorId, notification));

                await _appDbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                Console.WriteLine($"An exception was thrown inside PushRequestToNextSolicitorInOrder. See:::{JsonSerializer.Serialize(ex, _serializerOptions)}");
            }
        }

        private static LegalRequest UpdateLegalSearchRecordAfterBeingAssignedToSolicitor(LegalRequest? request, SolicitorAssignment nextSolicitor)
        {
            request!.Status = RequestStatusType.AssignedToLawyer.ToString();
            request.DateAssignedToSolicitor = nextSolicitor.AssignedAt;
            request.DateDue = TimeUtils.CalculateDateDueForRequest(); // 3 days from present time
            request.AssignedSolicitorId = nextSolicitor.SolicitorId; // Assuming you have a property to track assigned solicitor

            return request;
        }

        private async Task AssignOrdersAsync(Guid requestId, List<SolicitorRetrievalResponse> solicitors, int batchSize = 100)
        {
            try
            {
                if (solicitors.Count == 0)
                {
                    // No solicitors to assign
                    return;
                }

                // Perform Fisher-Yates shuffle on the solicitors list
                var random = new Random();
                for (int i = solicitors.Count - 1; i >= 1; i--)
                {
                    int j = random.Next(i + 1);
                    var temp = solicitors[i];
                    solicitors[i] = solicitors[j];
                    solicitors[j] = temp;
                }

                var batchCount = (int)Math.Ceiling((double)solicitors.Count / batchSize);

                for (int batchIndex = 0; batchIndex < batchCount; batchIndex++)
                {
                    var batchSolicitors = solicitors.Skip(batchIndex * batchSize).Take(batchSize);
                    var assignments = new List<SolicitorAssignment>();

                    for (int i = 0; i < batchSolicitors.Count(); i++)
                    {
                        var solicitorId = batchSolicitors.ElementAt(i).SolicitorId;
                        var assignment = new SolicitorAssignment
                        {
                            SolicitorId = solicitorId,
                            SolicitorEmail = batchSolicitors.ElementAt(i).SolicitorEmail,
                            RequestId = requestId,
                            Order = (batchIndex * batchSize) + i + 1, // Start order from 1
                            AssignedAt = TimeUtils.GetCurrentLocalTime(),
                            IsAccepted = false
                        };
                        assignments.Add(assignment);
                    }

                    _appDbContext.SolicitorAssignments.AddRange(assignments);
                    await _appDbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"An exception was thrown inside AssignOrdersAsync. See:::{JsonSerializer.Serialize(ex, _serializerOptions)}");
            }
        }

        private async Task NotifyLegalPerfectionTeam(LegalRequest request)
        {
            // form notification request
            var notification = new Domain.Entities.Notification.Notification
            {
                Title = ConstantTitle.UnAssignedRequestTitleForCso,
                IsBroadcast = true,
                SolId = request.BranchId,
                RecipientRole = nameof(RoleType.LegalPerfectionTeam),
                NotificationType = NotificationType.UnAssignedRequest,
                Message = ConstantMessage.UnAssignedRequestMessage,
                MetaData = JsonSerializer.Serialize(request, _serializerOptions)
            };

            // get legal perfection team email addresses
            var users = await _userManager.GetUsersInRoleAsync(nameof(RoleType.LegalPerfectionTeam));
            var emails = users?.Select(x => x?.Email).ToList();

            // Notify LegalPerfectionTeam of new request was unassigned
            _notificationServices.ToList().ForEach(x => x.NotifyUsersInRole(nameof(RoleType.LegalPerfectionTeam), notification, emails));
        }

        public async Task NotificationReminderForUnAttendedRequestsJob()
        {
            try
            {
                #region Send a reminder notification after 24hours that a request has been assigned

                // resolves time to 24 hours ago
                var requestsAcceptedTwentyFoursAgo = await _solicitorManager.GetUnattendedAcceptedRequestsForTheTimeFrame(TimeUtils.GetTwentyFourHoursElapsedTime(), false);

                if (requestsAcceptedTwentyFoursAgo != null && requestsAcceptedTwentyFoursAgo.Any())
                {
                    await ProcessNotifications(requestsAcceptedTwentyFoursAgo);
                }
                #endregion
            }
            catch (Exception ex)
            {

                Console.WriteLine($"An exception was thrown inside NotificationReminderForUnAttendedRequestsJob. See:::{JsonSerializer.Serialize(ex, _serializerOptions)}");
            }
        }

        private async Task ProcessNotifications(IEnumerable<Guid> requests)
        {
            // get the associated legalRequests
            Dictionary<UserMiniDto, LegalRequest> solicitorRequestsDictionary = new Dictionary<UserMiniDto, LegalRequest>();

            foreach (var request in requests.ToList())
            {
                var legalSearchRequest = await _legalSearchRequestManager.GetLegalSearchRequest(request);

                // could not find legal search request
                if (legalSearchRequest == null) continue;

                // get solicitor assignment record
                var solicitorAssignmentRecord = await _solicitorManager.GetCurrentSolicitorMappedToRequest(legalSearchRequest.Id,
                    legalSearchRequest.AssignedSolicitorId);

                solicitorRequestsDictionary.Add(new UserMiniDto
                {
                    UserId = solicitorAssignmentRecord.SolicitorId,
                    UserEmail = solicitorAssignmentRecord.SolicitorEmail
                }, legalSearchRequest);
            }

            // process notification to solicitor in parallel
            Parallel.ForEach(solicitorRequestsDictionary, async individualSolicitorRequestsDictionary =>
            {
                // Send notification to the solicitor
                var notification = new Domain.Entities.Notification.Notification
                {
                    Title = ConstantTitle.ReminderNotificationTitleOnPendingAssignedRequestForSolicitor,
                    NotificationType = NotificationType.OutstandingRequestAfter24Hours,
                    RecipientUserEmail = individualSolicitorRequestsDictionary.Key.UserEmail,
                    SolId = individualSolicitorRequestsDictionary.Value.BranchId,
                    Message = ConstantMessage.RequestPendingWithSolicitorMessage,
                    RecipientUserId = individualSolicitorRequestsDictionary.Key.UserId.ToString(),
                    MetaData = JsonSerializer.Serialize(individualSolicitorRequestsDictionary.Value, _serializerOptions)
                };

                _notificationServices.ToList().ForEach(x => x.NotifyUser(individualSolicitorRequestsDictionary.Value.InitiatorId, notification));
            });
        }

        public async Task PushBackRequestToCSOJob(Guid requestId)
        {
            // Load the request and perform assignment logic
            var request = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);

            if (request == null) return;

            var user = await _appDbContext.Users.FindAsync(request.InitiatorId.ToString());

            var notification = new Domain.Entities.Notification.Notification
            {
                Title = ConstantTitle.AdditionalInformationNeededOnAssignedRequestTitle,
                NotificationType = NotificationType.RequestReturnedToCso,
                RecipientUserId = request.InitiatorId.ToString(),
                RecipientUserEmail = user.Email,
                SolId = request.BranchId,
                Message = ConstantMessage.RequestRoutedBackToCSOMessage,
                MetaData = JsonSerializer.Serialize(request, _serializerOptions)
            };

            // get staff id
            _notificationServices.ToList().ForEach(x => x.NotifyUser(request.AssignedSolicitorId, notification));
        }

        public async Task InitiatePaymentToSolicitorJob(Guid requestId)
        {
            try
            {
                // step 1: Get request
                var request = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);
                var solicitor = await _appDbContext.Users.FirstOrDefaultAsync(x => x.Id == request!.AssignedSolicitorId);

                // step 2: Generate remove lien request payload
                RemoveLienFromAccountRequest removeLienRequest = GenerateRemoveLienRequest(request!.CustomerAccountNumber, request.LienId!);

                // step 3: Push request to remove lien from customer's account
                var response = await _fCMBService.RemoveLien(removeLienRequest);

                // step 4: Validate remove lien endpoint response
                var lienValidationResponse = ValidateRemoveLien(response);

                // step 5: Make decision on the outcome of response validation
                var paymentLogRequest = new LegalSearchRequestPaymentLog
                {
                    SourceAccountName = request.CustomerAccountName,
                    SourceAccountNumber = request.CustomerAccountNumber,
                    DestinationAccountName = solicitor?.FirstName ?? solicitor!.FullName,
                    DestinationAccountNumber = solicitor?.BankAccount!,
                    LienAmount = removeLienRequest.LienId,
                    PaymentStatus = PaymentStatusType.RemoveLien,
                    LienId = removeLienRequest.LienId,
                    CurrencyCode = removeLienRequest.CurrencyCode
                };

                if (!lienValidationResponse.isSuccessful)
                {
                    paymentLogRequest.PaymentResponseMetadata = lienValidationResponse.errorMessage;
                }
                else
                {
                    // generate payment request
                    IntrabankTransferRequest paymentRequest = GeneratePaymentRequest(paymentLogRequest, requestId);

                    // process credit to solicitor's account
                    var paymentResponse = await _fCMBService.InitiateTransfer(paymentRequest);

                    // validate payment response
                    var paymentResponseValidation = ValidatePaymentResponse(paymentResponse);

                    if (!paymentResponseValidation.isSuccessful)
                    {
                        // update record
                        paymentLogRequest.PaymentStatus = PaymentStatusType.MakePayment;
                        paymentLogRequest.PaymentResponseMetadata = paymentResponseValidation.errorMessage;
                    }
                    else
                    {
                        paymentLogRequest.PaymentStatus = PaymentStatusType.PaymentMade;
                        paymentLogRequest.PaymentResponseMetadata = JsonSerializer.Serialize(paymentResponse, _serializerOptions);
                        paymentLogRequest.TransactionStan = paymentResponse!.Data.Stan;
                        paymentLogRequest.TranId = paymentResponse!.Data.TranId;
                        paymentLogRequest.TransferNarration = paymentRequest.Narration;
                        paymentLogRequest.TransferRequestId = paymentRequest.CustomerReference;
                        paymentLogRequest.TransferAmount = paymentRequest.Amount;
                    }
                }

                // log payment record
                await _legalSearchRequestPaymentLogManager.AddLegalSearchRequestPaymentLog(paymentLogRequest);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"An exception was thrown inside AssignOrdersAsync. See:::{JsonSerializer.Serialize(ex, _serializerOptions)}");
            }
        }

        private IntrabankTransferRequest GeneratePaymentRequest(LegalSearchRequestPaymentLog paymentLogRequest, Guid requestId)
        {
            return new IntrabankTransferRequest
            {
                DebitAccountNo = paymentLogRequest.SourceAccountNumber,
                CreditAccountNo = paymentLogRequest.DestinationAccountNumber,
                IsFees = false,
                Charges = new List<Charge>(),
                Amount = Convert.ToDecimal(_options.LegalSearchAmount),
                Currency = _options.CurrencyCode,
                Narration = $"{_options.LegalSearchReasonCode} Payment for {requestId}",
                Remark = _options.LegalSearchPaymentRemarks,
                CustomerReference = $"{_options.LegalSearchReasonCode}{TimeUtils.GetCurrentLocalTime().Ticks}"
            };
        }

        private (bool isSuccessful, string? errorMessage) ValidateRemoveLien(RemoveLienFromAccountResponse? response)
        {
            if (response == null)
                return (false, "Lien endpoint returned null when trying to remove lien placed on client's account");

            if (response?.Code != _successStatusCode)
                return (false, "Request was not successful when trying to remove lien placed on client's account");

            if (response?.Code == _successStatusCode && response?.Description == _successStatusDescription)
                return (true, null);

            return (false, null);
        }

        private (bool isSuccessful, string? errorMessage) ValidatePaymentResponse(IntrabankTransferResponse? response)
        {
            if (response == null)
                return (false, "Payment endpoint returned null when trying to initiate transfer on client's account");

            if (response?.Code != _successStatusCode)
                return (false, "Request was not successful when trying to initiate transfer on client's account");

            if (response?.Code == _successStatusCode && response?.Data != null && response?.Data?.TranId != null)
                return (true, null);

            return (false, null);
        }

        private RemoveLienFromAccountRequest GenerateRemoveLienRequest(string customerAccountNumber, string lienId)
        {
            return new RemoveLienFromAccountRequest
            {
                RequestID = $"{_options.LegalSearchReasonCode}{TimeUtils.GetCurrentLocalTime().Ticks}",
                AccountNo = customerAccountNumber,
                LienId = lienId,
                CurrencyCode = _options.CurrencyCode,
                Rmks = _options.LegalSearchRemarks,
                ReasonCode = _options.LegalSearchReasonCode,
            };
        }

        public async Task ManuallyAssignRequestToSolicitorJob(Guid requestId, UserMiniDto solicitorInfo)
        {
            // get request
            var legalSearchRequest = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);

            if (legalSearchRequest == null)
                return;

            // create solicitor assignment
            SolicitorAssignment assignment = GenerateSolicitorAssignmentRecord(requestId, solicitorInfo);

            _appDbContext.SolicitorAssignments.Add(assignment);

            // Update the request status and assigned solicitor(s)
            legalSearchRequest = UpdateLegalSearchRecordAfterBeingAssignedToSolicitor(legalSearchRequest, assignment);

            legalSearchRequest.AssignedSolicitorId = solicitorInfo.UserId;

            // persist changes
            await _appDbContext.SaveChangesAsync();

            // notify solicitor of new request assignment
            var notification = new Domain.Entities.Notification.Notification
            {
                Title = ConstantTitle.NewRequestAssignmentTitle,
                RecipientUserId = solicitorInfo.UserId.ToString(),
                RecipientUserEmail = solicitorInfo.UserEmail,
                SolId = legalSearchRequest.BranchId,
                NotificationType = NotificationType.AssignedToSolicitor,
                Message = ConstantMessage.NewRequestAssignmentMessage,
                MetaData = JsonSerializer.Serialize(legalSearchRequest, _serializerOptions)
            };

            // get email addresses of the LegalPerfectionTeam
            var users = await _userManager.GetUsersInRoleAsync(nameof(RoleType.LegalPerfectionTeam));
            var emails = users?.Select(x => x?.Email).ToList();

            _notificationServices.ToList().ForEach(x => x.NotifyUsersInRole(nameof(RoleType.LegalPerfectionTeam), notification, emails));
        }

        private static SolicitorAssignment GenerateSolicitorAssignmentRecord(Guid requestId, UserMiniDto solicitorInfo)
        {
            return new SolicitorAssignment
            {
                SolicitorId = solicitorInfo.UserId,
                SolicitorEmail = solicitorInfo.UserEmail,
                RequestId = requestId,
                Order = 1, // Start order from 1
                AssignedAt = TimeUtils.GetCurrentLocalTime(),
                IsAccepted = false
            };
        }

        public async Task RequestEscalationJob(EscalateRequest request)
        {
            var legalRequest = await _legalSearchRequestManager.GetLegalSearchRequest(request.RequestId);

            if (legalRequest == null) return;

            var notification = await GenerateNotificationPayload(request, legalRequest);

            if (notification == null) return;

            // get email addresses of the LegalPerfectionTeam
            var users = await _userManager.GetUsersInRoleAsync(nameof(RoleType.LegalPerfectionTeam));
            var emails = users?.Select(x => x?.Email).ToList();

            var notificationTasks = request.RecipientType switch
            {
                NotificationRecipientType.Solicitor => _notificationServices.Select(x => x.NotifyUser(legalRequest.InitiatorId, notification)),
                NotificationRecipientType.LegalPerfectionTeam => _notificationServices.Select(x => x.NotifyUsersInRole(nameof(RoleType.LegalPerfectionTeam), notification, emails)),
                _ => Enumerable.Empty<Task>()  // Return an empty enumerable of tasks if the type is unknown
            };

            await Task.WhenAll(notificationTasks);
        }

        private async Task<Domain.Entities.Notification.Notification?> GenerateNotificationPayload(EscalateRequest request, LegalRequest legalRequest)
        {
            // Get assigned solicitor
            var solicitorAssignmentRecord = await _solicitorManager.GetCurrentSolicitorMappedToRequest(legalRequest.Id, legalRequest.AssignedSolicitorId);

            if (solicitorAssignmentRecord == null)
            {
                return null; // Return null if solicitorAssignmentRecord is null
            }

            // Formulate notification payload
            return new Domain.Entities.Notification.Notification
            {
                Title = ConstantTitle.ReminderNotificationTitleOnPendingAssignedRequestForSolicitor,
                RecipientUserEmail = solicitorAssignmentRecord.SolicitorEmail,
                RecipientUserId = solicitorAssignmentRecord.SolicitorId.ToString(),
                SolId = legalRequest.BranchId,
                NotificationType = ((solicitorAssignmentRecord.AssignedAt < TimeUtils.GetTwentyFourHoursElapsedTime()) && (solicitorAssignmentRecord.AssignedAt > TimeUtils.GetSeventyTwoHoursElapsedTime()))
                ? NotificationType.OutstandingRequestAfter24Hours : NotificationType.RequestWithElapsedSLA,
                Message = ConstantMessage.RequestPendingWithSolicitorMessage,
                MetaData = JsonSerializer.Serialize(request, _serializerOptions)
            };
        }

        public async Task GenerateDailySummaryForZonalServiceManagers()
        {
            var zonalServiceManagers = await _zonalManagerService.GetZonalServiceManagers();

            if (!zonalServiceManagers.Data.Any()) return;

            zonalServiceManagers.Data.ToList().ForEach(x =>
            {
                x.EmailAddress = "onagoruwam@gmail.com";
            });

            foreach (var zonalServiceManager in zonalServiceManagers.Data)
            {
                try
                {
                    var branchIds = await _appDbContext.Branches
                        .Where(x => x.ZonalServiceManagerId == zonalServiceManager.Id)
                        .Select(x => x.SolId)
                        .ToListAsync();

                    if (!branchIds.Any()) continue;

                    var requests = _appDbContext.LegalSearchRequests
                        .Where(x => branchIds.Contains(x.BranchId)
                                    && x.CreatedAt.Date <= TimeUtils.GetCurrentLocalTime().Date);

                    if (!requests.Any()) continue; // skipping for current ZSM because there are no matching requests

                    var zsmReportModel = await ProcessRequestsForZonalServiceManager(requests);

                    await SendReportToZonalServiceManager(zonalServiceManager, zsmReportModel);
                }
                catch (Exception ex)
                {
                    // Handle the exception (log, notify, etc.)
                    Console.WriteLine($"Error processing Zonal Service Manager {zonalServiceManager.Id}: {ex.Message}");
                }
            }
        }

        private async Task<ZonalServiceManagerReportModel> ProcessRequestsForZonalServiceManager(IQueryable<LegalRequest> requests)
        {
            try
            {
                var currentTime = TimeUtils.GetCurrentLocalTime();

                // Materialize the IQueryable to a list
                var requestList = await requests.ToListAsync();

                var countsTasks = new List<Task<int>>
                {
                    Task.Run(() => requestList.Count(x => x.Status == RequestStatusType.AssignedToLawyer.ToString())),
                    Task.Run(() => requestList.Count(x => (x.Status == RequestStatusType.BackToCso.ToString())
                                                        || (x.Status == RequestStatusType.Initiated.ToString() && x.RequestSource == RequestSourceType.Finacle))),
                    Task.Run(() => requestList.Count(x => x.Status == RequestStatusType.AssignedToLawyer.ToString() &&
                                                        x.DateDue != null && x.DateDue > currentTime)),
                    Task.Run(() => requestList.Count(x => x.Status == RequestStatusType.Completed.ToString())),
                };

                await Task.WhenAll(countsTasks);

                return new ZonalServiceManagerReportModel
                {
                    RequestsPendingWithSolicitorCount = countsTasks[0].Result,
                    RequestsPendingWithCsoCount = countsTasks[1].Result,
                    RequestsWithElapsedSlaCount = countsTasks[2].Result,
                    CompletedRequestsCount = countsTasks[3].Result,
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing requests for Zonal Service Manager: {ex.Message}");
                throw; // Re-throw the exception to propagate it
            }
        }


        private async Task SendReportToZonalServiceManager(ZonalServiceManagerMiniDto zonalServiceManager, ZonalServiceManagerReportModel zsmReportModel)
        {
            var emailTemplate = EmailTemplates.GetDailyReportEmailTemplateForZsm();

            var keys = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("{{date}}", TimeUtils.GetCurrentLocalTime().ToString("D")),
                new KeyValuePair<string, string>("{{ZonalServiceManagerName}}", zonalServiceManager.Name),
                new KeyValuePair<string, string>("{{CompletedRequestCount}}", zsmReportModel.CompletedRequestsCount.ToString()),
                new KeyValuePair<string, string>("{{RequestsPendingWithSolicitorCount}}", zsmReportModel.RequestsPendingWithSolicitorCount.ToString()),
                new KeyValuePair<string, string>("{{RequestsPendingWithCsoCount}}", zsmReportModel.RequestsPendingWithCsoCount.ToString()),
                new KeyValuePair<string, string>("{{RequestsWithElapsedSlaCount}}", zsmReportModel.RequestsWithElapsedSlaCount.ToString())
            };

            emailTemplate = await emailTemplate.UpdatePlaceHolders(keys);

            // generate email model
            var emailModel = GenerateEmailModel(emailTemplate, zonalServiceManager);

            // send email
            await _emailService.SendEmailAsync(emailModel);
        }

        private SendEmailRequest GenerateEmailModel(string emailTemplate, ZonalServiceManagerMiniDto zonalServiceManager)
        {
            return new SendEmailRequest
            {
                From = "ebusiness@fcmb.com",
                Body = emailTemplate,
                To = zonalServiceManager.EmailAddress,
                Bcc = !string.IsNullOrWhiteSpace(zonalServiceManager.AlternateEmailAddress) ? new List<string> { zonalServiceManager.AlternateEmailAddress } : new List<string>(),
                Subject = "Legal Search Daily Summary Report"
            };
        }

    }
}
