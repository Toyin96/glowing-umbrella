﻿using LegalSearch.Application.Interfaces.BackgroundService;
using LegalSearch.Application.Interfaces.FCMBService;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.ApplicationMessages;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Domain.Enums.Notification;
using LegalSearch.Domain.Enums.Role;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LegalSearch.Infrastructure.Services.BackgroundService
{
    internal class BackgroundService : IBackgroundService
    {
        private readonly AppDbContext _appDbContext;
        private readonly INotificationService _notificationService;
        private readonly ISolicitorManager _solicitorManager;
        private readonly IStateRetrieveService _stateRetrieveService;
        private readonly ILegalSearchRequestManager _legalSearchRequestManager;
        private readonly IFCMBService _fCMBService;
        private readonly ILegalSearchRequestPaymentLogManager _legalSearchRequestPaymentLogManager;
        private readonly FCMBServiceAppConfig _options;
        private readonly string _successStatusCode = "00";
        private readonly string _successStatusDescription = "SUCCESS";

        public BackgroundService(AppDbContext appDbContext,
            INotificationService notificationService,
            ISolicitorManager solicitorManager,
            IStateRetrieveService stateRetrieveService, 
            ILegalSearchRequestManager legalSearchRequestManager,
            IFCMBService fCMBService, IOptions<FCMBServiceAppConfig> options,
            ILegalSearchRequestPaymentLogManager legalSearchRequestPaymentLogManager)
        {
            _appDbContext = appDbContext;
            _notificationService = notificationService;
            _solicitorManager = solicitorManager;
            _stateRetrieveService = stateRetrieveService;
            _legalSearchRequestManager = legalSearchRequestManager;
            _fCMBService = fCMBService;
            _legalSearchRequestPaymentLogManager = legalSearchRequestPaymentLogManager;
            _options = options.Value;
        }
        public async Task AssignRequestToSolicitorsJob(Guid requestId)
        {
            // Load the request and perform assignment logic
            var request = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);
            if (request == null) return;

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

        public async Task CheckAndRerouteRequestsJob()
        {
            // Implement logic to query for requests assigned to lawyers for 20 minutes
            var requestsToReroute = await _solicitorManager.GetRequestsToReroute();

            foreach (var request in requestsToReroute)
            {
                // Get the legalRequest entity and check its status
                var legalSearchRequest = await _legalSearchRequestManager.GetLegalSearchRequest(request);

                if (legalSearchRequest.Status == RequestStatusType.UnAssigned.ToString())
                {
                    /*
                     This request has been routed to legalPerfection team either due to:
                        1. No matching solicitor
                        2. Every single matching solicitor has been assigned to it but no one accepted it on time.
                     */
                    continue;
                }

                // get the currently assigned solicitor, know his/her order and route it to the next order
                var currentlyAssignedSolicitor = await _solicitorManager.GetCurrentSolicitorMappedToRequest(request, 
                    legalSearchRequest.AssignedSolicitorId);

                await PushRequestToNextSolicitorInOrder(request, currentlyAssignedSolicitor.Order);
            }
        }

        public async Task PushRequestToNextSolicitorInOrder(Guid requestId, int currentAssignedSolicitorOrder = 0)
        {
            var request = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);

            // Get the next solicitor in line
            var nextSolicitor = await _solicitorManager.GetNextSolicitorInLine(requestId, currentAssignedSolicitorOrder);

            if (nextSolicitor == null)
            {
                // Route to Legal Perfection Team
                await NotifyLegalPerfectionTeam(request!);
                return;
            }

            // logged time request was assigned to solicitor
            nextSolicitor.AssignedAt = TimeUtils.GetCurrentLocalTime();

            // Update the request status and assigned solicitor(s)
            request!.Status = RequestStatusType.AssignedToLawyer.ToString();
            request.DateAssignedToSolicitor = nextSolicitor.AssignedAt;
            request.DateDue = TimeUtils.CalculateDateDueForRequest(); // 3 days from present time
            request.AssignedSolicitorId = nextSolicitor.SolicitorId; // Assuming you have a property to track assigned solicitor

            // Send notification to the solicitor
            var notification = new Domain.Entities.Notification.Notification
            {
                Title = "New Request",
                NotificationType = NotificationType.AssignedToSolicitor,
                RecipientUserId = nextSolicitor.SolicitorId.ToString(),
                Message = ConstantMessage.NewRequestAssignmentMessage,
                MetaData = JsonSerializer.Serialize(request)
            };

            // Notify solicitor of new request
            await _notificationService.SendNotificationToUser(nextSolicitor.SolicitorId, notification);

            await _appDbContext.SaveChangesAsync();
        }

        private async Task AssignOrdersAsync(Guid requestId, List<SolicitorRetrievalResponse> solicitors, int batchSize = 100)
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
                        RequestId = requestId,
                        Order = batchIndex * batchSize + i + 1, // Start order from 1
                        AssignedAt = TimeUtils.GetCurrentLocalTime(),
                        IsAccepted = false
                    };
                    assignments.Add(assignment);
                }

                _appDbContext.SolicitorAssignments.AddRange(assignments);
                await _appDbContext.SaveChangesAsync();
            }
        }

        private async Task NotifyLegalPerfectionTeam(LegalRequest request)
        {
            // form notification request
            var notification = new Domain.Entities.Notification.Notification
            {
                Title = "UnAssigned Request",
                IsBroadcast = true,
                NotificationType = NotificationType.UnAssignedRequest,
                Message = ConstantMessage.UnAssignedRequestMessage,
                MetaData = JsonSerializer.Serialize(request)
            };

            // Notify LegalPerfectionTeam of new request was unassigned
            await _notificationService.SendNotificationToRole(nameof(RoleType.LegalPerfectionTeam), notification);

            // update legalSearch request here
            request.AssignedSolicitorId = default;
            request.Status = RequestStatusType.UnAssigned.ToString();
            await _legalSearchRequestManager.UpdateLegalSearchRequest(request);
        }

        public async Task NotificationReminderForUnAttendedRequestsJob()
        {
            #region Send a reminder notification after 24hours that a request has been assigned

            // resolves time to 24 hours ago
            var requestsAcceptedTwentyFoursAgo = await _solicitorManager.GetUnattendedAcceptedRequestsForTheTimeFrame(TimeUtils.GetTwentyHoursElapsedTime());

            if (requestsAcceptedTwentyFoursAgo != null && requestsAcceptedTwentyFoursAgo.Any())
            {
                // get the associated legalRequests
                Dictionary<Guid, LegalRequest> solicitorRequestsDictionary = new Dictionary<Guid, LegalRequest>();

                foreach (var request in requestsAcceptedTwentyFoursAgo.ToList())
                {
                    var legalSearchRequest = await _legalSearchRequestManager.GetLegalSearchRequest(request);

                    // could not find legal search request
                    if (legalSearchRequest == null) continue;

                    solicitorRequestsDictionary.Add(legalSearchRequest.AssignedSolicitorId, legalSearchRequest);
                }

                // process notification to solicitor in parallel
                Parallel.ForEach(solicitorRequestsDictionary, async individualSolicitorRequestsDictionary =>
                {
                    // Send notification to the solicitor
                    var notification = new Domain.Entities.Notification.Notification
                    {
                        Title = "Reminder Notification on Pending Request",
                        NotificationType = NotificationType.AssignedToSolicitor,
                        Message = ConstantMessage.RequestPendingWithSolicitorMessage,
                        MetaData = JsonSerializer.Serialize(individualSolicitorRequestsDictionary.Value)
                    };

                    await _notificationService.SendNotificationToUser(individualSolicitorRequestsDictionary.Key, notification);
                });
            }
            #endregion

            #region Re-route request to another solicitor after request SLA have elapsed

            // get assigned requests that have been unattended for the 72 hours (3 days)
            var requestsWithElapsedSLA = await _solicitorManager.GetUnattendedAcceptedRequestsForTheTimeFrame(TimeUtils.GetSeventyTwoHoursElapsedTime());

            // check if any matching request was returned
            if (requestsWithElapsedSLA != null && requestsWithElapsedSLA.Any())
            {
                Dictionary<Guid, int> elapsedSLARequestsDictionary = new Dictionary<Guid, int>();

                foreach (var requestId in requestsWithElapsedSLA.ToList())
                {
                    // get the associated legal search
                    var legalSearchRequest = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);

                    // could not find legal search request
                    if (legalSearchRequest == null) continue;

                    // get the currently assigned solicitor to know his/her order
                    var currentlyAssignedSolicitor = await _solicitorManager.GetCurrentSolicitorMappedToRequest(requestId,
                        legalSearchRequest.AssignedSolicitorId);

                    elapsedSLARequestsDictionary.Add(requestId, currentlyAssignedSolicitor.Order);
                }

                // process each request serially
                foreach (var request in elapsedSLARequestsDictionary)
                {
                    // pass the request to another solicitor in-line based on the current solicitor's order
                    await PushRequestToNextSolicitorInOrder(request.Key, request.Value);
                }
            }

            #endregion
        }

        public async Task PushBackRequestToCSOJob(Guid requestId)
        {
            // Load the request and perform assignment logic
            var request = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);
            if (request == null) return;

            var notification = new Domain.Entities.Notification.Notification
            {
                Title = "Request Needs Additional Information",
                NotificationType = NotificationType.RequestReturnedToCso,
                Message = ConstantMessage.RequestRoutedBackToCSOMessage,
                MetaData = JsonSerializer.Serialize(request)
            };

            // get staff id
            await _notificationService.SendNotificationToUser(request.InitiatorId, notification);
        }

        public async Task InitiatePaymentToSolicitorJob(Guid requestId)
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

            if (lienValidationResponse.isSuccessful == false)
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

                if (paymentResponseValidation.isSuccessful == false)
                {
                    // update record
                    paymentLogRequest.PaymentStatus = PaymentStatusType.MakePayment;
                    paymentLogRequest.PaymentResponseMetadata = paymentResponseValidation.errorMessage;
                }
                else
                {
                    paymentLogRequest.PaymentStatus = PaymentStatusType.PaymentMade;
                    paymentLogRequest.PaymentResponseMetadata = JsonSerializer.Serialize(paymentResponse);
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
                return (false, "Lien endpoint returned null when trying to initiate transfer on client's account");

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
    }
}
