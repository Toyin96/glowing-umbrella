using Azure.Core;
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
using LegalSearch.Domain.Entities.User;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Domain.Enums.Notification;
using LegalSearch.Domain.Enums.Role;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
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
        private readonly IFcmbService _fCMBService;
        private readonly ILegalSearchRequestPaymentLogManager _legalSearchRequestPaymentLogManager;
        private readonly UserManager<Domain.Entities.User.User> _userManager;
        private readonly IZonalManagerService _zonalManagerService;
        private readonly ICustomerManagerService _customerManagerService;
        private readonly IEmailService _emailService;
        private readonly ILogger<BackgroundService> _logger;
        private readonly FCMBConfig _options;
        private readonly string _successStatusCode = "00";
        private readonly string _successStatusDescription = "SUCCESS";
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve };

        public BackgroundService(AppDbContext appDbContext,
            IEnumerable<INotificationService> notificationService,
            ISolicitorManager solicitorManager,
            IStateRetrieveService stateRetrieveService,
            ILegalSearchRequestManager legalSearchRequestManager,
            IFcmbService fCMBService, IOptions<FCMBConfig> options,
            ILegalSearchRequestPaymentLogManager legalSearchRequestPaymentLogManager,
            UserManager<Domain.Entities.User.User> userManager,
            IZonalManagerService zonalManagerService,
            ICustomerManagerService customerManagerService,
            IEmailService emailService,
            ILogger<BackgroundService> logger)
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
            _customerManagerService = customerManagerService;
            _emailService = emailService;
            _logger = logger;
            _options = options.Value;
        }
        public async Task AssignRequestToSolicitorsJob(Guid requestId)
        {
            try
            {
                var request = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);

                if (request == null || request.Status == RequestStatusType.Completed.ToString())
                    return;

                var solicitors = await _solicitorManager.DetermineSolicitors(request);

                if (solicitors == null || !solicitors.Any())
                {
                    await HandleNoSolicitorsFound(request);
                    return;
                }

                await AssignAndRouteRequest(requestId, solicitors.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occurred in AssignRequestToSolicitorsJob: {ex.Message}");
            }
        }

        private async Task HandleNoSolicitorsFound(LegalRequest request)
        {
            if (!request.BusinessLocation.HasValue)
            {
                _logger.LogInformation($"Legal search request with ID {request.Id} does not have a business location, and thus region could not be ascertained");
                return;
            }

            var region = await _stateRetrieveService.GetRegionOfState(request.BusinessLocation.Value);
            var solicitors = await _solicitorManager.FetchSolicitorsInSameRegion(region);

            if (solicitors == null || !solicitors.Any())
            {
                await UpdateRequestAndNotifyLegalPerfectionTeam(request);
                return;
            }

            await AssignAndRouteRequest(request.Id, solicitors.ToList());
        }

        private async Task AssignAndRouteRequest(Guid requestId, List<SolicitorRetrievalResponse> solicitors)
        {
            await AssignOrdersAsync(requestId, solicitors);

            // Route request to first solicitor based on order arrangement
            await PushRequestToNextSolicitorInOrder(requestId);
        }

        private async Task UpdateRequestAndNotifyLegalPerfectionTeam(LegalRequest request)
        {
            request.AssignedSolicitorId = Guid.Empty;
            request.Status = RequestStatusType.UnAssigned.ToString();
            await _legalSearchRequestManager.UpdateLegalSearchRequest(request);

            await NotifyLegalPerfectionTeam(request);
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
                        legalSearchRequest.AssignedSolicitorId ?? Guid.Empty);

                    if (currentlyAssignedSolicitor != null)
                    {
                        solicitorAssignmentRecords.Add(currentlyAssignedSolicitor.Id);
                    }

                    // get current assignment order
                    int currentAssignmentOrder = currentlyAssignedSolicitor != null ? currentlyAssignedSolicitor.Order : 0;

                    await PushRequestToNextSolicitorInOrder(request, currentAssignmentOrder);
                }

                if (solicitorAssignmentRecords?.Any() == true)
                {
                    await _solicitorManager.UpdateManySolicitorAssignmentStatuses(solicitorAssignmentRecords);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception was thrown inside CheckAndRerouteRequestsJob. See:::{JsonSerializer.Serialize(ex, _serializerOptions)}");
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
                _notificationServices.ToList().ForEach(x => x.NotifyUser(notification));

                await _appDbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                _logger.LogError($"An exception was thrown inside PushRequestToNextSolicitorInOrder. See:::{JsonSerializer.Serialize(ex, _serializerOptions)}");
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
                using (var rng = RandomNumberGenerator.Create())
                {
                    for (int i = solicitors.Count - 1; i >= 1; i--)
                    {
                        int j = GetRandomNumber(rng, i + 1);
                        var temp = solicitors[i];
                        solicitors[i] = solicitors[j];
                        solicitors[j] = temp;
                    }
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
                _logger.LogError($"An exception was thrown inside AssignOrdersAsync. See:::{JsonSerializer.Serialize(ex, _serializerOptions)}");
            }
        }

        public static int GetRandomNumber(RandomNumberGenerator rng, int maxValue)
        {
            if (maxValue < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue should be greater than or equal to 1.");
            }

            byte[] randomNumber = new byte[4];
            rng.GetBytes(randomNumber);
            int randomValue = Math.Abs(BitConverter.ToInt32(randomNumber, 0));

            return randomValue % maxValue;
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

                _logger.LogError($"An exception was thrown inside NotificationReminderForUnAttendedRequestsJob. See:::{JsonSerializer.Serialize(ex, _serializerOptions)}");
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
                    legalSearchRequest.AssignedSolicitorId ?? Guid.Empty);

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

                _notificationServices.ToList().ForEach(x => x.NotifyUser(notification));
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
            _notificationServices.ToList().ForEach(x => x.NotifyUser(notification));
        }

        public async Task InitiatePaymentToSolicitorJob(Guid requestId)
        {
            try
            {
                _logger.LogInformation($"Process started for request with ID: {requestId} inside InitiatePaymentToSolicitorJob");

                var request = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);

                if (request is null)
                {
                    _logger.LogInformation("Could not find request with ID: {0} when trying to initiate settlement payment", requestId);
                    return;
                }

                var solicitor = await GetSolicitorById(request.AssignedSolicitorId);

                if (solicitor is null)
                {
                    _logger.LogInformation("Could not find solicitor with ID: {0} when trying to initiate settlement payment", request.AssignedSolicitorId);
                    return;
                }

                var removeLienRequest = GenerateRemoveLienRequest(request!.CustomerAccountNumber, request!.LienId!);

                var response = await _fCMBService.RemoveLien(removeLienRequest);
                var lienValidationResponse = ValidateRemoveLien(response);

                var paymentLogRequest = CreatePaymentRequest(request, solicitor, removeLienRequest, lienValidationResponse);

                await UpdatePaymentRequest(paymentLogRequest, request, lienValidationResponse);

                await _legalSearchRequestPaymentLogManager.AddLegalSearchRequestPaymentLog(paymentLogRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception was thrown inside InitiatePaymentToSolicitorJob. See:::{JsonSerializer.Serialize(ex, _serializerOptions)}");
            }
        }

        private async Task<Domain.Entities.User.User?> GetSolicitorById(Guid? solicitorId)
        {
            if (solicitorId is null)
                return null;

            return await _appDbContext.Users.FirstOrDefaultAsync(x => x.Id == solicitorId);
        }

        private LegalSearchRequestPaymentLog CreatePaymentRequest(LegalRequest request, Domain.Entities.User.User solicitor, RemoveLienFromAccountRequest removeLienRequest, (bool isSuccessful, string? errorMessage) lienValidationResponse)
        {
            return new LegalSearchRequestPaymentLog
            {
                SourceAccountName = request.CustomerAccountName,
                LegalSearchRequestId = request.Id,
                SourceAccountNumber = request.CustomerAccountNumber,
                DestinationAccountName = $"{solicitor?.FirstName} {solicitor?.LastName}",
                DestinationAccountNumber = solicitor!.BankAccount!,
                PaymentStatus = lienValidationResponse.isSuccessful ? PaymentStatusType.RemoveLien : PaymentStatusType.MakePayment,
                TransferAmount = Convert.ToDecimal(_options.LegalSearchAmount),
                LienId = removeLienRequest.LienId,
                CurrencyCode = removeLienRequest.CurrencyCode
            };
        }

        private async Task UpdatePaymentRequest(LegalSearchRequestPaymentLog paymentLogRequest, LegalRequest? request, (bool isSuccessful, string? errorMessage) lienValidationResponse)
        {
            if (!lienValidationResponse.isSuccessful)
            {
                paymentLogRequest.PaymentResponseMetadata = lienValidationResponse.errorMessage;
            }
            else
            {
                var paymentRequest = GeneratePaymentRequest(paymentLogRequest, request!.CustomerAccountName!.First10Characters());
                await ProcessPaymentToSolicitorAccount(paymentLogRequest, paymentRequest);
            }
        }

        private async Task ProcessPaymentToSolicitorAccount(LegalSearchRequestPaymentLog paymentLogRequest, IntrabankTransferRequest paymentRequest)
        {
            var paymentResponse = await _fCMBService.InitiateTransfer(paymentRequest);
            var paymentResponseValidation = ValidatePaymentResponse(paymentResponse);

            if (!paymentResponseValidation.isSuccessful)
            {
                paymentLogRequest.PaymentStatus = PaymentStatusType.MakePayment;
                paymentLogRequest.PaymentResponseMetadata = paymentResponseValidation.errorMessage;
            }
            else
            {
                paymentLogRequest.PaymentStatus = PaymentStatusType.PaymentMade;
                paymentLogRequest.PaymentResponseMetadata = JsonSerializer.Serialize(paymentResponse, _serializerOptions);
                paymentLogRequest.TransactionStan = paymentResponse?.Data?.Stan;
                paymentLogRequest.TranId = paymentResponse?.Data?.TranId;
                paymentLogRequest.TransferNarration = paymentRequest.Narration;
                paymentLogRequest.TransferRequestId = paymentRequest.CustomerReference;
            }
        }

        public async Task RetryFailedLegalSearchRequestSettlementToSolicitor()
        {
            try
            {
                // get pending settlement requests
                var paymentLogRecords = await _legalSearchRequestPaymentLogManager.GetAllLegalSearchRequestPaymentLogNotYetCompleted();

                if (!paymentLogRecords.Any())
                {
                    _logger.LogInformation("No eligible records found for settlement.");
                    return;
                }

                _logger.LogInformation($"Found {paymentLogRecords.Count()} eligible records for settlement");

                // push eligible records for settlement
                foreach (var paymentLogRecord in paymentLogRecords)
                {
                    _logger.LogInformation($"About to retry payment settlement for {paymentLogRecord.DestinationAccountName}");
                    await ReProcessSolicitorSettlement(paymentLogRecord);
                }

                _logger.LogInformation($"Successfully pushed {paymentLogRecords.Count()} records for settlement");
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred during retrying failed legal search request settlement to solicitor.", ex);
            }
        }

        private async Task ReProcessSolicitorSettlement(LegalSearchRequestPaymentLog legalSearchRequestPaymentLog)
        {
            switch (legalSearchRequestPaymentLog.PaymentStatus)
            {
                case PaymentStatusType.RemoveLien:
                    await ReProcessRemoveLien(legalSearchRequestPaymentLog);
                    break;
                case PaymentStatusType.MakePayment:
                    await ReProcessMakePayment(legalSearchRequestPaymentLog);
                    break;
                default:
                    break;
            }
        }

        private async Task ReProcessRemoveLien(LegalSearchRequestPaymentLog paymentLog)
        {
            // Generate remove lien request payload again
            var removeLienRequest = GenerateRemoveLienRequest(paymentLog.SourceAccountNumber, paymentLog.LienId);

            // Push request to remove lien from customer's account again
            var response = await _fCMBService.RemoveLien(removeLienRequest);

            // Validate remove lien endpoint response again
            var lienValidationResponse = ValidateRemoveLien(response);

            // Get Legal Request record
            var request = await _legalSearchRequestManager.GetLegalSearchRequest(paymentLog.LegalSearchRequestId);

            // Handle missing request
            if (request is null)
            {
                _logger.LogWarning("Could not find request with ID: {0} when trying to initiate settlement payment", paymentLog.LegalSearchRequestId);
                return;
            }

            // Get the assigned solicitor
            var solicitor = await GetSolicitorById(request.AssignedSolicitorId);

            // Handle missing solicitor
            if (solicitor is null)
            {
                _logger.LogInformation("Could not find solicitor with ID: {0} when trying to initiate settlement payment", request.AssignedSolicitorId);
                return;
            }

            // Create and update payment log request
            var updatedPaymentLogRequest = await CreateAndUpdatePaymentLogRequest(request, lienValidationResponse, paymentLog);

            // Update the payment log accordingly based on the lien validation response and/or payment response
            await _legalSearchRequestPaymentLogManager.UpdateLegalSearchRequestPaymentLog(updatedPaymentLogRequest);
        }
        private async Task ReProcessMakePayment(LegalSearchRequestPaymentLog paymentLog)
        {
            var paymentRequest = GeneratePaymentRequest(paymentLog, paymentLog.SourceAccountName);
            await ProcessPaymentToSolicitorAccount(paymentLog, paymentRequest);
            await _legalSearchRequestPaymentLogManager.UpdateLegalSearchRequestPaymentLog(paymentLog);
        }

        private async Task<LegalSearchRequestPaymentLog> CreateAndUpdatePaymentLogRequest(LegalRequest request, (bool isSuccessful, string? errorMessage) lienValidationResponse, LegalSearchRequestPaymentLog paymentLog)
        {
            // Update payment log request
            await UpdatePaymentRequest(paymentLog, request, lienValidationResponse);

            return paymentLog;
        }

        private IntrabankTransferRequest GeneratePaymentRequest(LegalSearchRequestPaymentLog paymentLogRequest, string clientName)
        {
            return new IntrabankTransferRequest
            {
                DebitAccountNo = paymentLogRequest.SourceAccountNumber,
                CreditAccountNo = paymentLogRequest.DestinationAccountNumber,
                IsFees = false,
                Charges = new List<Charge>(),
                Amount = paymentLogRequest.TransferAmount,
                Currency = _options.CurrencyCode,
                Narration = $"{_options.LegalSearchReasonCode} Payment for {clientName}",
                Remark = _options.LegalSearchPaymentRemarks,
                CustomerReference = $"{_options.LegalSearchReasonCode}{TimeUtils.GetCurrentLocalTime().Ticks}"
            };
        }

        private (bool isSuccessful, string? errorMessage) ValidateRemoveLien(RemoveLienFromAccountResponse? response)
        {
            if (response == null)
                return (false, "Lien endpoint returned null when trying to remove lien placed on client's account");

            if (response?.Code != _successStatusCode)
                return (false, response?.Description ?? "Request was not successful when trying to remove lien placed on client's account");

            if (response?.Code == _successStatusCode && response?.Description == _successStatusDescription)
                return (true, "Lien removal succeeded");

            return (false, "Request was not successful when trying to remove lien placed on client's account");
        }

        private (bool isSuccessful, string? errorMessage) ValidatePaymentResponse(IntrabankTransferResponse? response)
        {
            if (response == null)
                return (false, "Payment endpoint returned null when trying to initiate transfer on client's account");

            if (response.Code != _successStatusCode)
                return (false, response?.Description ?? "Request was not successful when trying to initiate transfer on client's account");

            if (response.Data != null && response.Data.TranId != null)
                return (true, "The transfer request was successful");

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
                NotificationType = NotificationType.ManualSolicitorAssignment,
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
                NotificationRecipientType.Solicitor => _notificationServices.Select(x => x.NotifyUser(notification)),
                NotificationRecipientType.LegalPerfectionTeam => _notificationServices.Select(x => x.NotifyUsersInRole(nameof(RoleType.LegalPerfectionTeam), notification, emails)),
                _ => Enumerable.Empty<Task>()  // Return an empty enumerable of tasks if the type is unknown
            };

            await Task.WhenAll(notificationTasks);
        }

        private async Task<Domain.Entities.Notification.Notification?> GenerateNotificationPayload(EscalateRequest request, LegalRequest legalRequest)
        {
            // Get assigned solicitor
            var solicitorAssignmentRecord = await _solicitorManager.GetCurrentSolicitorMappedToRequest(legalRequest.Id, legalRequest.AssignedSolicitorId ?? Guid.Empty);

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

                    // ZSM has no branch associated to it
                    if (!branchIds.Any()) continue;

                    var requests = _appDbContext.LegalSearchRequests
                        .Where(x => branchIds.Contains(x.BranchId)
                                    && x.CreatedAt.Date == TimeUtils.GetCurrentLocalTime().Date);

                    var zsmReportModel = await ProcessRequestsForZonalServiceManager(requests);

                    await SendReportToZonalServiceManager(zonalServiceManager, zsmReportModel);
                }
                catch (Exception ex)
                {
                    // Handle the exception (log, notify, etc.)
                    _logger.LogError($"Error processing Zonal Service Manager {zonalServiceManager.Id}: {ex.Message}");
                }
            }
        }

        private async Task<ReportModel> ProcessRequestsForZonalServiceManager(IQueryable<LegalRequest> requests)
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

                // Wait for all tasks to be completed
                await Task.WhenAll(countsTasks);

                return new ReportModel
                {
                    RequestsPendingWithSolicitorCount = countsTasks[0].Result,
                    RequestsPendingWithCsoCount = countsTasks[1].Result,
                    RequestsWithElapsedSlaCount = countsTasks[2].Result,
                    CompletedRequestsCount = countsTasks[3].Result,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing requests for Zonal Service Manager: {ex.Message}");
                throw; // Re-throw the exception to propagate it
            }
        }

        private async Task SendReportToZonalServiceManager(ZonalServiceManagerMiniDto zonalServiceManager, ReportModel zsmReportModel)
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

        private async Task SendReportToCustomerServiceManager(CustomerServiceManagerMiniDto customerServiceManager, ReportModel zsmReportModel)
        {
            var emailTemplate = EmailTemplates.GetDailyReportEmailTemplateForCsm();

            var keys = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("{{date}}", TimeUtils.GetCurrentLocalTime().ToString("D")),
                new KeyValuePair<string, string>("{{CustomerServiceManagerName}}", customerServiceManager.Name),
                new KeyValuePair<string, string>("{{CompletedRequestCount}}", zsmReportModel.CompletedRequestsCount.ToString()),
                new KeyValuePair<string, string>("{{RequestsPendingWithSolicitorCount}}", zsmReportModel.RequestsPendingWithSolicitorCount.ToString()),
                new KeyValuePair<string, string>("{{RequestsPendingWithCsoCount}}", zsmReportModel.RequestsPendingWithCsoCount.ToString()),
                new KeyValuePair<string, string>("{{RequestsWithElapsedSlaCount}}", zsmReportModel.RequestsWithElapsedSlaCount.ToString())
            };

            emailTemplate = await emailTemplate.UpdatePlaceHolders(keys);

            // generate email model
            var emailModel = GenerateEmailModel(emailTemplate, customerServiceManager);

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

        private SendEmailRequest GenerateEmailModel(string emailTemplate, CustomerServiceManagerMiniDto zonalServiceManager)
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

        public async Task GenerateDailySummaryForCustomerServiceManagers()
        {
            var customerServiceManagers = await _customerManagerService.GetCustomerServiceManagers();

            if (!customerServiceManagers.Data.Any()) return;

            // TODO: Remove after testing
            customerServiceManagers.Data.ToList().ForEach(x =>
            {
                x.EmailAddress = "onagoruwam@gmail.com";
            });

            foreach (var customerServiceManager in customerServiceManagers.Data)
            {
                try
                {
                    // Get CSOs under CSM
                    var claim = new Claim(nameof(ClaimType.SolId), customerServiceManager.SolId);
                    
                    var customerServiceOfficers = await _userManager.GetUsersForClaimAsync(claim);

                    // ZSM has no branch associated to it
                    if (!customerServiceOfficers.Any()) continue;

                    // Extract all CSOs IDs under CSM
                    List<Guid> csoIds = customerServiceOfficers.Select(x => x.Id).ToList();

                    var requests = _appDbContext.LegalSearchRequests
                        .Where(x => x.InitiatorId.HasValue && csoIds.Contains(x.InitiatorId.Value)
                                    && x.CreatedAt.Date == TimeUtils.GetCurrentLocalTime().Date);

                    var reportModel = await ProcessRequestsForZonalServiceManager(requests);

                    await SendReportToCustomerServiceManager(customerServiceManager, reportModel);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing Customer Service Manager {customerServiceManager.SolId}: {ex.Message}");
                }
            }
        }
    }
}
