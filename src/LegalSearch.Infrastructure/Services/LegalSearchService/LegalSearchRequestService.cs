using Fcmb.Shared.Models.Constants;
using Fcmb.Shared.Models.Responses;
using Hangfire;
using LegalSearch.Application.Interfaces.BackgroundService;
using LegalSearch.Application.Interfaces.FCMBService;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Requests.CSO;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Application.Models.Responses.CSO;
using LegalSearch.Domain.ApplicationMessages;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Domain.Enums.Notification;
using LegalSearch.Infrastructure.File.Report;
using LegalSearch.Infrastructure.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LegalSearch.Infrastructure.Services.LegalSearchService
{
    public class LegalSearchRequestService : ILegalSearchRequestService
    {
        private readonly ILogger<LegalSearchRequestService> _logger;
        private readonly IFcmbService _fCMBService;
        private readonly UserManager<Domain.Entities.User.User> _userManager;
        private readonly ILegalSearchRequestManager _legalSearchRequestManager;
        private readonly ISolicitorAssignmentManager _solicitorAssignmentManager;
        private readonly IEnumerable<INotificationService> _notificationService;
        private readonly FCMBServiceAppConfig _options;
        private readonly string _successStatusCode = "00";
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve };


        public LegalSearchRequestService(ILogger<LegalSearchRequestService> logger, IFcmbService fCMBService,
            UserManager<Domain.Entities.User.User> userManager,
            ILegalSearchRequestManager legalSearchRequestManager,
            ISolicitorAssignmentManager solicitorAssignmentManager,
            IEnumerable<INotificationService> notificationService,
            IOptions<FCMBServiceAppConfig> options)
        {
            _logger = logger;
            _fCMBService = fCMBService;
            _userManager = userManager;
            _legalSearchRequestManager = legalSearchRequestManager;
            _solicitorAssignmentManager = solicitorAssignmentManager;
            _notificationService = notificationService;
            _options = options.Value;
        }

        public async Task<StatusResponse> AcceptLegalSearchRequest(AcceptRequest request)
        {
            try
            {
                // Step 1: Get legal search request and validate
                var result = await FetchAndValidateRequest(request.RequestId, request.SolicitorId, ActionType.AcceptRequest);

                // Step 2: Handle validation result
                if (result.errorCode == ResponseCodes.ServiceError)
                {
                    _logger.LogError("Validation error while accepting legal search request: {ErrorMessage}", result.errorMessage);
                    return new StatusResponse(result.errorMessage ?? "Sorry, something went wrong. Please try again later.", result.errorCode);
                }

                // Update legal search request status
                var legalSearchRequest = result.request;

                // Get solicitor assignment record
                var solicitorAssignmentRecord = await _solicitorAssignmentManager.GetSolicitorAssignmentBySolicitorId(legalSearchRequest!.AssignedSolicitorId ?? Guid.Empty, legalSearchRequest.Id);

                // Check if legal search request is currently assigned to solicitor
                if (solicitorAssignmentRecord == null)
                {
                    _logger.LogError("Failed to find solicitor assignment record for legal search request.");
                    return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
                }

                // Solicitor accepts legal search request here
                solicitorAssignmentRecord.IsAccepted = true;

                // Update legal search request status
                legalSearchRequest!.Status = nameof(RequestStatusType.LawyerAccepted);
                var isRequestUpdated = await _legalSearchRequestManager.UpdateLegalSearchRequest(legalSearchRequest);

                // Check if request update was successful
                if (!isRequestUpdated)
                {
                    _logger.LogError("Failed to update legal search request status.");
                    return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
                }

                // Step 3: Return success response
                return new StatusResponse("You have successfully accepted this request", ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                // Step 4: Handle exceptions and log error
                _logger.LogError(ex, "An exception occurred inside AcceptLegalSearchRequest");
                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

        public async Task<StatusResponse> CreateNewRequest(LegalSearchRequest legalSearchRequest, string userId)
        {
            try
            {
                // Get the CSO account
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                    return new StatusResponse("No user found", ResponseCodes.Unauthenticated);

                // Step 1: Validate customer's account status and balance
                var accountInquiryResponse = await _fCMBService.MakeAccountInquiry(legalSearchRequest.CustomerAccountNumber);

                // Process account inquiry response to see if the account has enough balance for this action
                (bool isSuccess, string errorMessage) = ProcessAccountInquiryResponse(accountInquiryResponse!);

                // Detailed error response is being returned here if the validation checks were not met
                if (!isSuccess)
                    return new StatusResponse(errorMessage, ResponseCodes.ServiceError);

                // Step 2: Generate lien request to cover the cost of the legal search
                AddLienToAccountRequest lienRequest = GenerateLegalSearchLienRequestPayload(legalSearchRequest);

                // System attempts to place a lien on the customer's account
                var addLienResponse = await _fCMBService.AddLien(lienRequest);

                // Process lien response to see if the account has enough balance for this action
                (bool isSuccess, string errorMessage) lienVerificationResponse = ProcessLienResponse(addLienResponse!);

                // Detailed error response is being returned here if the validation checks were not met
                if (!lienVerificationResponse.isSuccess)
                    return new StatusResponse(lienVerificationResponse.errorMessage, ResponseCodes.ServiceError);

                // Step 4: Create a new legal search request 
                var newLegalSearchRequest = MapRequestToLegalRequest(legalSearchRequest, user);

                // Assign lien ID to the legal search request
                newLegalSearchRequest.LienId = addLienResponse!.Data.LienId;

                // Update legal search request payload
                newLegalSearchRequest.StaffId = user!.StaffId!;
                newLegalSearchRequest.InitiatorId = user!.Id;
                newLegalSearchRequest.RequestInitiator = user.FirstName;

                // Step 5: Add registration documents and other information here
                await AddAdditionalInfoAndDocuments(legalSearchRequest, newLegalSearchRequest);

                // Step 6: Persist legal search request
                var result = await _legalSearchRequestManager.AddNewLegalSearchRequest(newLegalSearchRequest);

                // Log Step 6 completion
                _logger.LogInformation("Step 6: Legal search request persisted successfully.");

                // Check if the request was successfully created
                if (!result)
                {
                    _logger.LogError("Step 7: Request could not be created.");
                    return new ObjectResponse<string>("Request could not be created", ResponseCodes.ServiceError);
                }

                // Step 7: Enqueue the legal search request for background processing
                BackgroundJob.Enqueue<IBackgroundService>(x => x.AssignRequestToSolicitorsJob(newLegalSearchRequest.Id));

                // Log Step 7 completion
                _logger.LogInformation("Step 7: Request enqueued for background processing.");

                // Step 8: Return a success response
                _logger.LogInformation("Step 8: Returning success response.");
                return new StatusResponse("Request created successfully", ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                // Step 9: Handle exceptions and log errors
                _logger.LogError(ex, "An exception occurred inside CreateNewRequest");
                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

        private (bool isSuccess, string errorMessage) ProcessLienResponse(AddLienToAccountResponse addLienResponse)
        {
            try
            {
                _logger.LogInformation("Processing lien response...");

                if (addLienResponse == null)
                {
                    _logger.LogError("Lien response is null. Something went wrong. Please try again.");
                    return (false, "Something went wrong. Please try again");
                }

                if (addLienResponse.Code != _successStatusCode)
                {
                    _logger.LogError($"Lien response code: {addLienResponse.Code}. Description: {addLienResponse.Description}");
                    return (false, addLienResponse.Description);
                }

                if (!string.IsNullOrWhiteSpace(addLienResponse?.Data?.LienId))
                {
                    _logger.LogInformation("Lien was successfully applied on the customer's account.");
                    return (true, "Lien was successfully applied on customer's account");
                }

                _logger.LogError("Lien response data is null or LienId is empty. Please try again.");
                return (false, "Please try again");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while processing lien response.");
                return (false, "Something went wrong. Please try again");
            }
        }

        private AddLienToAccountRequest GenerateLegalSearchLienRequestPayload(LegalSearchRequest legalSearchRequest)
        {
            try
            {
                _logger.LogInformation("Generating legal search lien request payload...");

                var requestId = $"{_options.LegalSearchReasonCode}{TimeUtils.GetCurrentLocalTime().Ticks}";
                var reasonCode = $"{_options.LegalSearchReasonCode}";

                var lienRequest = new AddLienToAccountRequest
                {
                    RequestID = requestId,
                    AccountNo = legalSearchRequest.CustomerAccountNumber,
                    AmountValue = Convert.ToDecimal(_options.LegalSearchAmount),
                    CurrencyCode = nameof(CurrencyType.NGN),
                    Rmks = _options.LegalSearchRemarks,
                    ReasonCode = reasonCode
                };

                _logger.LogInformation("Legal search lien request payload generated successfully.");
                return lienRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while generating legal search lien request payload.");
                throw; // Re-throw the exception for upper layers to handle
            }
        }

        private AddLienToAccountRequest GenerateLegalSearchLienRequestPayloadForFinacleRequest(UpdateFinacleLegalRequest legalSearchRequest)
        {
            try
            {
                var lienRequest = new AddLienToAccountRequest
                {
                    RequestID = TimeUtils.GetCurrentLocalTime().Ticks.ToString(),
                    AccountNo = legalSearchRequest.CustomerAccountNumber,
                    AmountValue = Convert.ToDecimal(_options.LegalSearchAmount),
                    CurrencyCode = nameof(CurrencyType.NGN),
                    Rmks = _options.LegalSearchRemarks,
                    ReasonCode = _options.LegalSearchReasonCode
                };

                _logger.LogInformation("Generated legal search lien request payload: {@LienRequest}", lienRequest);
                return lienRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while generating the legal search lien request payload.");
                throw; // Re-throw the exception for upper layers to handle
            }
        }

        private (bool isSuccess, string errorMessage) ProcessAccountInquiryResponse(GetAccountInquiryResponse accountInquiryResponse)
        {
            try
            {
                bool isSuccessfullyParsedToDecimal = decimal.TryParse(_options.LegalSearchAmount, out decimal legalSearchAmount);

                if (!isSuccessfullyParsedToDecimal)
                    return (false, "Something went wrong. Please try again");

                if (accountInquiryResponse == null)
                    return (false, "Something went wrong. Please try again");

                if (accountInquiryResponse is not null && accountInquiryResponse.Code != _successStatusCode)
                    return (false, accountInquiryResponse.Description);

                if (accountInquiryResponse is not null && accountInquiryResponse.Code == _successStatusCode
                    && accountInquiryResponse.Data.AvailableBalance < legalSearchAmount)
                {
                    _logger.LogInformation("Account inquiry successful. Customer does not have enough money to perform this action.");
                    return (true, "Customer does not have enough money to perform this action");
                }

                if (accountInquiryResponse is not null && accountInquiryResponse.Code == _successStatusCode
                    && accountInquiryResponse.Data.AvailableBalance >= legalSearchAmount)
                {
                    _logger.LogInformation("Account inquiry successful. Name & balance inquiry was successful.");
                    return (true, "Name & balance inquiry was successful");
                }

                _logger.LogWarning("Account inquiry returned an unknown response. Please try again.");
                return (false, "Please try again");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while processing account inquiry response.");
                throw; // Re-throw the exception for upper layers to handle
            }
        }

        private async Task AddAdditionalInfoAndDocuments(LegalSearchRequest legalSearchRequest, LegalRequest newLegalSearchRequest)
        {
            try
            {
                _logger.LogInformation("Adding additional information and documents to the legal search request.");

                if (legalSearchRequest.AdditionalInformation != null)
                {
                    newLegalSearchRequest.Discussions.Add(new Discussion { Conversation = legalSearchRequest.AdditionalInformation });
                    _logger.LogInformation("Additional information added to the legal search request.");
                }

                // add the registration document
                if (legalSearchRequest.RegistrationDocuments != null)
                {
                    List<RegistrationDocument> documents = await ProcessRegistrationDocument(legalSearchRequest.RegistrationDocuments);

                    // attach document to legalSearchRequest object
                    documents.ForEach(x =>
                    {
                        newLegalSearchRequest.RegistrationDocuments.Add(x);
                        _logger.LogInformation("Registration document added to the legal search request.");
                    });
                }

                // add the supporting documents
                if (legalSearchRequest.SupportingDocuments != null)
                {
                    List<SupportingDocument> documents = await ProcessSupportingDocuments(legalSearchRequest.SupportingDocuments);

                    // attach document to legalSearchRequest object
                    documents.ForEach(x =>
                    {
                        newLegalSearchRequest.SupportingDocuments.Add(x);
                        _logger.LogInformation("Supporting document added to the legal search request.");
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while adding additional information and documents to the legal search request.");
                throw; // Re-throw the exception for upper layers to handle
            }
        }

        private async Task AddAdditionalInfoAndDocuments(UpdateRequest legalSearchRequest, LegalRequest newLegalSearchRequest)
        {
            try
            {
                _logger.LogInformation("Adding additional information and documents to the legal search request.");

                if (legalSearchRequest.AdditionalInformation != null)
                {
                    newLegalSearchRequest.Discussions.Add(new Discussion { Conversation = legalSearchRequest.AdditionalInformation });
                    _logger.LogInformation("Additional information added to the legal search request.");
                }

                // add the registration document
                if (legalSearchRequest.RegistrationDocuments != null)
                {
                    List<RegistrationDocument> documents = await ProcessRegistrationDocument(legalSearchRequest.RegistrationDocuments);

                    // attach document to legalSearchRequest object
                    documents.ForEach(x =>
                    {
                        newLegalSearchRequest.RegistrationDocuments.Add(x);
                        _logger.LogInformation("Registration document added to the legal search request.");
                    });
                }

                // add the supporting documents
                if (legalSearchRequest.SupportingDocuments != null)
                {
                    List<SupportingDocument> documents = await ProcessSupportingDocuments(legalSearchRequest.SupportingDocuments);

                    // attach document to legalSearchRequest object
                    documents.ForEach(x =>
                    {
                        newLegalSearchRequest.SupportingDocuments.Add(x);
                        _logger.LogInformation("Supporting document added to the legal search request.");
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while adding additional information and documents to the legal search request.");
                throw; // Re-throw the exception for upper layers to handle
            }
        }

        private async Task AddAdditionalInfoAndDocuments(UpdateFinacleLegalRequest legalSearchRequest, LegalRequest newLegalSearchRequest)
        {
            try
            {
                _logger.LogInformation("Adding additional information and documents to the legal search request.");

                if (legalSearchRequest.AdditionalInformation != null)
                {
                    newLegalSearchRequest.Discussions.Add(new Discussion { Conversation = legalSearchRequest.AdditionalInformation });
                    _logger.LogInformation("Additional information added to the legal search request.");
                }

                // add the registration document
                if (legalSearchRequest.RegistrationDocuments != null)
                {
                    List<RegistrationDocument> documents = await ProcessRegistrationDocument(legalSearchRequest.RegistrationDocuments);

                    // attach document to legalSearchRequest object
                    documents.ForEach(x =>
                    {
                        newLegalSearchRequest.RegistrationDocuments.Add(x);
                        _logger.LogInformation("Registration document added to the legal search request.");
                    });
                }

                // add the supporting documents
                if (legalSearchRequest.SupportingDocuments != null)
                {
                    List<SupportingDocument> documents = await ProcessSupportingDocuments(legalSearchRequest.SupportingDocuments);

                    // attach document to legalSearchRequest object
                    documents.ForEach(x =>
                    {
                        newLegalSearchRequest.SupportingDocuments.Add(x);
                        _logger.LogInformation("Supporting document added to the legal search request.");
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while adding additional information and documents to the legal search request.");
                throw; // Re-throw the exception for upper layers to handle
            }
        }

        public async Task<StatusResponse> PushBackLegalSearchRequestForMoreInfo(ReturnRequest returnRequest, Guid solicitorId)
        {
            try
            {
                _logger.LogInformation("Pushing back legal search request for more information.");

                var result = await FetchAndValidateRequest(returnRequest.RequestId, solicitorId, ActionType.ReturnRequest);

                if (result.errorCode == ResponseCodes.ServiceError)
                {
                    _logger.LogError("An error occurred while fetching and validating the request.");
                    return new StatusResponse(result.errorMessage ?? "Sorry, something went wrong. Please try again later.", result.errorCode);
                }

                var request = result!.request;

                // add the files & feedback if any
                if (returnRequest.SupportingDocuments.Any())
                {
                    _logger.LogInformation("Processing and adding supporting documents to the request.");

                    var supportingDocuments = await ProcessSupportingDocuments(returnRequest.SupportingDocuments);

                    supportingDocuments.ForEach(x => request!.SupportingDocuments.Add(x));

                    _logger.LogInformation("Supporting documents added to the request.");
                }

                if (!string.IsNullOrEmpty(returnRequest.Feedback))
                {
                    _logger.LogInformation("Adding feedback to the request.");
                    request!.Discussions.Add(new Discussion { Conversation = returnRequest.Feedback });
                }

                // update legalSearchRequest
                request!.Status = nameof(RequestStatusType.BackToCso);
                bool isRequestUpdated = await _legalSearchRequestManager.UpdateLegalSearchRequest(request!);

                if (!isRequestUpdated)
                {
                    _logger.LogError("An error occurred while updating the request.");
                    return new StatusResponse("An error occurred while sending request. Please try again later.", ResponseCodes.ServiceError);
                }

                // Enqueue the legalSearchRequest for background processing
                BackgroundJob.Enqueue<IBackgroundService>(x => x.PushBackRequestToCSOJob(request!.Id));

                _logger.LogInformation("Request has been successfully pushed back to staff for additional information/clarification.");
                return new StatusResponse("Request has been successfully pushed back to staff for additional information/clarification", ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred inside PushBackLegalSearchRequestForMoreInfo.");
                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

        public async Task<StatusResponse> RejectLegalSearchRequest(RejectRequest request)
        {
            try
            {
                _logger.LogInformation("Rejecting legal search request.");

                // get legal legalSearchRequest
                var result = await FetchAndValidateRequest(request.RequestId, request.SolicitorId, ActionType.RejectRequest);

                if (result.errorCode == ResponseCodes.ServiceError)
                {
                    _logger.LogError("An error occurred while fetching and validating the request.");
                    return new StatusResponse(result.errorMessage ?? "Sorry, something went wrong. Please try again later.", result.errorCode);
                }

                var legalSearchRequest = result.request;

                // get solicitor assignment record
                var solicitorAssignmentRecord = await _solicitorAssignmentManager.GetSolicitorAssignmentBySolicitorId(request.SolicitorId, request.RequestId);

                // check if legalSearchRequest is currently assigned to solicitor
                if (solicitorAssignmentRecord == null)
                {
                    _logger.LogError("Solicitor assignment record not found.");
                    return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
                }

                legalSearchRequest.ReasonForRejection = request.RejectionMessage;
                legalSearchRequest.Status = nameof(RequestStatusType.LawyerRejected);
                legalSearchRequest.AssignedSolicitorId = Guid.Empty; // reset the AssignedSolicitorId here
                var isRequestUpdated = await _legalSearchRequestManager.UpdateLegalSearchRequest(legalSearchRequest);

                solicitorAssignmentRecord.IsCurrentlyAssigned = false;
                var hasSolicitorAssignmentRecordUpdated = await _solicitorAssignmentManager.UpdateSolicitorAssignmentRecord(solicitorAssignmentRecord);

                if (!hasSolicitorAssignmentRecordUpdated)
                {
                    _logger.LogError("An error occurred while updating the solicitor assignment record.");
                    return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
                }

                if (!isRequestUpdated)
                {
                    _logger.LogError("An error occurred while updating the request.");
                    return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
                }

                // Enqueue the legalSearchRequest for background processing
                BackgroundJob.Enqueue<IBackgroundService>(x => x.PushRequestToNextSolicitorInOrder(legalSearchRequest.Id, solicitorAssignmentRecord.Order));

                _logger.LogInformation("Legal search request successfully rejected.");
                return new StatusResponse("Operation is successful", ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred inside RejectLegalSearchRequest.");
                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }
        public async Task<StatusResponse> SubmitRequestReport(SubmitLegalSearchReport submitLegalSearchReport, Guid solicitorId)
        {
            try
            {
                _logger.LogInformation("Submitting legal search report.");

                // get legal legalSearchRequest
                var result = await FetchAndValidateAcceptedRequest(submitLegalSearchReport.RequestId, solicitorId);

                if (result.errorCode == ResponseCodes.ServiceError)
                {
                    _logger.LogError("An error occurred while fetching and validating the request.");
                    return new StatusResponse(result.errorMessage ?? "Sorry, something went wrong. Please try again later.", result.errorCode);
                }

                var request = result.request;

                // get the solicitor that initiated the request
                var solicitor = await _userManager.FindByIdAsync(solicitorId.ToString());
                if (solicitor == null)
                {
                    _logger.LogError("User not found.");
                    return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.BadRequest);
                }

                // verify that customer legalSearchRequest have a lien ID
                if (request!.LienId == null)
                {
                    _logger.LogError($"Legal search request with ID: {request.Id} does not have a lien ID.");
                    return new StatusResponse("An error occurred while sending report. Please try again later.", ResponseCodes.BadRequest);
                }

                // add the files & feedback if any
                if (submitLegalSearchReport.RegistrationDocuments.Any())
                {
                    var supportingDocuments = await ProcessSupportingDocuments(submitLegalSearchReport.RegistrationDocuments);

                    supportingDocuments.ForEach(x => request!.SupportingDocuments.Add(x));
                }

                if (!string.IsNullOrEmpty(submitLegalSearchReport.Feedback))
                {
                    request!.Discussions.Add(new Discussion { Conversation = submitLegalSearchReport.Feedback });
                }

                // update legalSearchRequest
                request!.Status = nameof(RequestStatusType.Completed);
                request.RequestSubmissionDate = TimeUtils.GetCurrentLocalTime();
                bool isRequestUpdated = await _legalSearchRequestManager.UpdateLegalSearchRequest(request!);

                if (!isRequestUpdated)
                {
                    _logger.LogError($"An error occurred while updating the legal search request with ID: {request.Id}");
                    return new StatusResponse("An error occurred while sending report. Please try again later.", ResponseCodes.BadRequest);
                }

                // get solicitor assignment record
                var solicitorAssignmentRecord = await _solicitorAssignmentManager.GetSolicitorAssignmentBySolicitorId(solicitorId, request.Id);

                if (solicitorAssignmentRecord == null)
                {
                    _logger.LogError($"An error occurred while fetching the solicitor assignment record for the legal search request with ID: {request.Id}");
                    return new StatusResponse("An error occurred while sending report. Please try again later.", ResponseCodes.BadRequest);
                }

                // mark request has been completed
                solicitorAssignmentRecord.HasCompletedLegalSearchRequest = true;
                bool isRecordUpdated = await _solicitorAssignmentManager.UpdateSolicitorAssignmentRecord(solicitorAssignmentRecord);

                if (!isRecordUpdated)
                {
                    _logger.LogError($"An error occurred while updating solicitor assignment record for legal search request with ID: {request.Id}");
                    return new StatusResponse("An error occurred while sending report. Please try again later.", ResponseCodes.BadRequest);
                }

                // Notify the solicitor
                var notification = new Domain.Entities.Notification.Notification
                {
                    Title = ConstantTitle.CompletedRequestTitleForCso,
                    RecipientUserId = request.AssignedSolicitorId.ToString(),
                    RecipientUserEmail = solicitor.Email,
                    NotificationType = NotificationType.CompletedRequest,
                    Message = ConstantMessage.CompletedRequestMessageForSolicitor,
                    MetaData = JsonSerializer.Serialize(request, _serializerOptions)
                };

                // Push legalSearchRequest to credit solicitor's account upon completion of legalSearchRequest
                BackgroundJob.Enqueue<IBackgroundService>(x => x.InitiatePaymentToSolicitorJob(submitLegalSearchReport.RequestId));

                // notify the Initiating CSO
                NotifyClient(request.AssignedSolicitorId ?? Guid.Empty, notification);

                _logger.LogInformation("Legal search report successfully submitted.");
                return new StatusResponse("You have successfully submitted the report for this request", ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred inside SubmitRequestReport.");
                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

        private async Task<(LegalRequest? request, string? errorMessage, string errorCode)> FetchAndValidateRequest(Guid requestId, Guid solicitorId, ActionType actionType)
        {
            try
            {
                _logger.LogInformation($"Fetching and validating legal search request for RequestId: {requestId}, SolicitorId: {solicitorId}, ActionType: {actionType}");

                // get legal legalSearchRequest
                var request = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);

                if (request == null)
                {
                    _logger.LogError("Could not find the request.");
                    return (request, "Could not find request", BaseResponseCodes.ServiceError);
                }

                switch (actionType)
                {
                    case ActionType.AcceptRequest:
                        if (request.AssignedSolicitorId != solicitorId)
                        {
                            _logger.LogError("Solicitor cannot accept a request not assigned to them.");
                            return (null, "Sorry you cannot accept a request not assigned to you", ResponseCodes.ServiceError);
                        }

                        if (request.Status != nameof(RequestStatusType.AssignedToLawyer))
                        {
                            _logger.LogError("Cannot accept a request that is not assigned to the lawyer.");
                            return (null, "Something went wrong, please try again later", ResponseCodes.ServiceError);
                        }
                        break;
                    case ActionType.RejectRequest:
                        if (request.AssignedSolicitorId != solicitorId)
                        {
                            _logger.LogError("Solicitor cannot reject a request not assigned to them.");
                            return (null, "Sorry you cannot reject a request not assigned to you", ResponseCodes.ServiceError);
                        }

                        if (request.Status != nameof(RequestStatusType.AssignedToLawyer))
                        {
                            _logger.LogError("Cannot reject a request that is not assigned to the lawyer.");
                            return (null, "You've already accepted this request so you cannot reject it", ResponseCodes.ServiceError);
                        }
                        break;
                    case ActionType.ReturnRequest:
                        if (request.AssignedSolicitorId != solicitorId)
                        {
                            _logger.LogError("Solicitor cannot return a request not assigned to them.");
                            return (null, "Sorry you cannot return a request not assigned to you", ResponseCodes.ServiceError);
                        }

                        if (request.Status != nameof(RequestStatusType.LawyerAccepted))
                        {
                            _logger.LogError("Cannot return a request that is not in 'LawyerAccepted' status.");
                            return (null, "You need to accept the request before you can return it for additional information", ResponseCodes.ServiceError);
                        }
                        break;
                    default:
                        break;
                }

                _logger.LogInformation("Legal search request successfully fetched and validated.");
                return (request, null, ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while fetching and validating the request.");
                return (null, "Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

        private async Task<(LegalRequest? request, string? errorMessage, string errorCode)> FetchAndValidateAcceptedRequest(Guid requestId, Guid solicitorId)
        {
            try
            {
                _logger.LogInformation($"Fetching and validating accepted legal search request for RequestId: {requestId}, SolicitorId: {solicitorId}");

                // get legal legalSearchRequest
                var request = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);

                if (request == null)
                {
                    _logger.LogError("Could not find the request.");
                    return (request, "Could not find request", BaseResponseCodes.ServiceError);
                }

                // check if the request is missing an InitiatorId
                if (request.InitiatorId == null)
                {
                    _logger.LogError($"Solicitor tried submitting a report for request with ID: {request.Id}, but the request lacks an initiator ID.");
                    return (null, "Sorry, you cannot submit a report for this request at this time", ResponseCodes.ServiceError);
                }

                // check if legalSearchRequest is currently assigned to solicitor
                if (request.AssignedSolicitorId != solicitorId)
                {
                    _logger.LogError("Solicitor cannot submit a report for a request not assigned to them.");
                    return (null, "Sorry you cannot submit a report for a request not assigned to you", ResponseCodes.ServiceError);
                }

                switch (request.Status)
                {
                    case nameof(RequestStatusType.BackToCso):
                        _logger.LogError("Cannot submit a report for a request that is not in 'LawyerAccepted' status.");
                        return (null, "Apologies, you are unable to submit a report for a legal search request that has been redirected back to the CSO for further information.", ResponseCodes.ServiceError);
                    case nameof(RequestStatusType.Completed):
                        _logger.LogError("Cannot submit a report for a request that is not in 'Completed' status.");
                        return (null, "Sorry, you cannot submit a report for a legal search request that has already been completed.", ResponseCodes.ServiceError);
                    default:
                        break;
                }

                if (request.Status != nameof(RequestStatusType.LawyerAccepted))
                {
                    _logger.LogError("Cannot submit a report for a request that is not in 'LawyerAccepted' status.");
                    return (null, "Sorry, you cannot submit a report for a legal search request that you're yet to accept", ResponseCodes.ServiceError);
                }

                _logger.LogInformation("Accepted legal search request successfully fetched and validated.");
                return (request, null, ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while fetching and validating the accepted request.");
                return (null, "Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }


        private async Task<List<SupportingDocument>> ProcessSupportingDocuments(List<IFormFile> files)
        {
            try
            {
                _logger.LogInformation("Processing supporting documents.");

                var documents = new List<SupportingDocument>();

                foreach (var formFile in files)
                {
                    if (formFile.Length == 0)
                    {
                        _logger.LogWarning("Skipping a file with zero length.");
                        continue;
                    }

                    using var memoryStream = new MemoryStream();
                    await formFile.CopyToAsync(memoryStream);
                    var fileContent = memoryStream.ToArray();

                    var fileType = Path.GetExtension(formFile.FileName).ToLower();

                    var supportingDocument = new SupportingDocument
                    {
                        FileName = formFile.FileName,
                        FileContent = fileContent,
                        FileType = fileType
                    };

                    documents.Add(supportingDocument);

                    _logger.LogInformation($"Processed supporting document: FileName={formFile.FileName}, FileType={fileType}");
                }

                _logger.LogInformation("Supporting documents processed successfully.");
                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while processing supporting documents.");
                throw;  // Re-throw the exception after logging
            }
        }

        private async Task<List<RegistrationDocument>> ProcessRegistrationDocument(List<IFormFile> files)
        {
            try
            {
                _logger.LogInformation("Processing registration documents.");

                var documents = new List<RegistrationDocument>();

                foreach (var formFile in files)
                {
                    if (formFile.Length == 0)
                    {
                        _logger.LogWarning("Skipping a file with zero length.");
                        continue;
                    }

                    using var memoryStream = new MemoryStream();
                    await formFile.CopyToAsync(memoryStream);
                    var fileContent = memoryStream.ToArray();

                    var fileType = Path.GetExtension(formFile.FileName).ToLower();

                    var registrationDocument = new RegistrationDocument
                    {
                        FileName = formFile.FileName,
                        FileContent = fileContent,
                        FileType = fileType
                    };

                    documents.Add(registrationDocument);

                    _logger.LogInformation($"Processed registration document: FileName={formFile.FileName}, FileType={fileType}");
                }

                _logger.LogInformation("Registration documents processed successfully.");
                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while processing registration documents.");
                throw;  // Re-throw the exception after logging
            }
        }

        private void NotifyClient(Guid userId, Domain.Entities.Notification.Notification notification)
        {
            try
            {
                _logger.LogInformation($"Sending notification to user with ID: {userId}");

                // Send notification to client
                _notificationService.ToList().ForEach(x => x.NotifyUser(notification));

                _logger.LogInformation("Notification sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while sending notification.");
                throw;  // Re-throw the exception after logging
            }
        }

        private LegalRequest MapRequestToLegalRequest(LegalSearchRequest request, Domain.Entities.User.User user)
        {
            try
            {
                _logger.LogInformation("Mapping LegalSearchRequest to LegalRequest...");

                var legalRequest = new LegalRequest
                {
                    BranchId = user.SolId ?? user!.BranchId!,
                    RequestType = request.RequestType,
                    BusinessLocation = request.BusinessLocation,
                    RegistrationDate = request.RegistrationDate,
                    RegistrationLocation = request.RegistrationLocation,
                    RequestSource = RequestSourceType.Staff,
                    RegistrationNumber = request.RegistrationNumber,
                    CustomerAccountName = request.CustomerAccountName,
                    CustomerAccountNumber = request.CustomerAccountNumber,
                    Status = RequestStatusType.Initiated.ToString(),
                };

                _logger.LogInformation("Mapping completed successfully.");
                return legalRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while mapping LegalSearchRequest to LegalRequest.");
                throw;  // Re-throw the exception after logging
            }
        }

        public async Task<ObjectResponse<GetAccountInquiryResponse>> PerformNameInquiryOnAccount(string accountNumber)
        {
            try
            {
                // Validate customer's account status and balance
                var accountInquiryResponse = await _fCMBService.MakeAccountInquiry(accountNumber);

                if (accountInquiryResponse?.Data == null)
                {
                    _logger.LogError("Something went wrong during account inquiry. Response is null or missing data.");
                    return new ObjectResponse<GetAccountInquiryResponse>("Something went wrong. Please try again.", ResponseCodes.ServiceError);
                }

                // Log the legal search amount being added to the response payload
                _logger.LogInformation($"Adding legal search amount to response payload: {_options.LegalSearchAmount}");
                accountInquiryResponse.Data.LegalSearchAmount = Convert.ToDecimal(_options.LegalSearchAmount);

                _logger.LogInformation("Name inquiry operation was successful.");
                return new ObjectResponse<GetAccountInquiryResponse>("Operation was successful", ResponseCodes.Success)
                {
                    Data = accountInquiryResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An exception occurred inside PerformNameInquiryOnAccount at {TimeUtils.GetCurrentLocalTime}");
                return new ObjectResponse<GetAccountInquiryResponse>("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }


        public async Task<ObjectResponse<LegalSearchRootResponsePayload>> GetLegalRequestsForSolicitor(SolicitorRequestAnalyticsPayload viewRequestAnalyticsPayload, Guid solicitorId)
        {
            try
            {
                _logger.LogInformation($"Fetching legal requests for solicitor with ID: {solicitorId}");

                var response = await _legalSearchRequestManager.GetLegalRequestsForSolicitor(viewRequestAnalyticsPayload, solicitorId);

                _logger.LogInformation("Successfully retrieved legal search requests for solicitor.");

                return new ObjectResponse<LegalSearchRootResponsePayload>("Successfully Retrieved Legal Search Requests")
                {
                    Data = response,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An exception occurred inside GetLegalRequestsForSolicitor for solicitor ID: {solicitorId}");
                return new ObjectResponse<LegalSearchRootResponsePayload>("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

        public async Task<ObjectResponse<StaffRootResponsePayload>> GetLegalRequestsForStaff(StaffDashboardAnalyticsRequest request)
        {
            try
            {
                _logger.LogInformation("Fetching legal requests for staff.");

                var response = await _legalSearchRequestManager.GetLegalRequestsForStaff(request);

                _logger.LogInformation("Successfully retrieved legal search requests for staff.");

                return new ObjectResponse<StaffRootResponsePayload>("Successfully Retrieved Legal Search Requests")
                {
                    Data = response,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred inside GetLegalRequestsForStaff.");
                return new ObjectResponse<StaffRootResponsePayload>("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }


        public async Task<StatusResponse> CreateNewRequestFromFinacle(FinacleLegalSearchRequest legalSearchRequest)
        {
            try
            {
                _logger.LogInformation("Creating a new legal search request from Finacle.");

                var newLegalSearchRequest = MapFinacleRequestToLegalRequest(legalSearchRequest);

                // persist legalSearchRequest
                var result = await _legalSearchRequestManager.AddNewLegalSearchRequest(newLegalSearchRequest);

                if (!result)
                {
                    _logger.LogError("Request could not be created.");
                    return new ObjectResponse<string>("Request could not be created", ResponseCodes.ServiceError);
                }

                _logger.LogInformation("Request created successfully.");

                return new StatusResponse("Request created successfully", ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred inside CreateNewRequestFromFinacle.");
                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }


        private LegalRequest MapFinacleRequestToLegalRequest(FinacleLegalSearchRequest request)
        {
            try
            {
                _logger.LogInformation("Mapping Finacle request to LegalRequest.");

                var legalRequest = new LegalRequest
                {
                    RequestSource = RequestSourceType.Finacle,
                    BranchId = request.BranchId,
                    CustomerAccountName = request.CustomerAccountName,
                    CustomerAccountNumber = request.CustomerAccountNumber,
                    Status = RequestStatusType.Initiated.ToString()
                };

                _logger.LogInformation("Finacle request successfully mapped to LegalRequest.");

                return legalRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred inside MapFinacleRequestToLegalRequest.");
                throw; // Re-throw the exception for higher-level handling
            }
        }

        public async Task<ListResponse<FinacleLegalSearchResponsePayload>> GetFinacleLegalRequestsForCso(GetFinacleRequest request, string solId)
        {
            var response = await _legalSearchRequestManager.GetFinacleLegalRequestsForCso(request, solId);

            return new ListResponse<FinacleLegalSearchResponsePayload>("Successfully Retrieved Finacle legal search requests")
            {
                Data = response,
                Total = response.Count
            };
        }

        public async Task<StatusResponse> UpdateFinacleRequestByCso(UpdateFinacleLegalRequest updateFinacleLegalRequest, string userId)
        {
            try
            {
                _logger.LogInformation("Updating Finacle request by CSO.");

                // Fetch legal search request 
                var legalSearch = await _legalSearchRequestManager.GetLegalSearchRequest(updateFinacleLegalRequest.RequestId);

                if (legalSearch == null)
                {
                    _logger.LogError("No matching Legal search record found with the ID provided.");
                    return new StatusResponse("No matching Legal search record found with the ID provided", ResponseCodes.ServiceError);
                }

                if (legalSearch.RequestSource == RequestSourceType.Staff)
                {
                    _logger.LogError("You can't edit this request via this route.");
                    return new StatusResponse("You can't edit this request via this route", ResponseCodes.Forbidden);
                }

                // Validate customer's account status and balance
                var accountInquiryResponse = await _fCMBService.MakeAccountInquiry(updateFinacleLegalRequest.CustomerAccountNumber);

                // Process name inquiry response to see if the account has enough balance for this action
                (bool isSuccess, string errorMessage) = ProcessAccountInquiryResponse(accountInquiryResponse!);

                // Detailed error response is being returned here if the validation checks were not met
                if (!isSuccess)
                {
                    _logger.LogError($"Validation failed: {errorMessage}");
                    return new StatusResponse(errorMessage, ResponseCodes.ServiceError);
                }

                // Place lien on account in question to cover the cost of the legal search
                AddLienToAccountRequest lienRequest = GenerateLegalSearchLienRequestPayloadForFinacleRequest(updateFinacleLegalRequest);

                // System attempts to place lien on customer's account
                var addLienResponse = await _fCMBService.AddLien(lienRequest);

                // Process lien response to see if the lien was successfully applied
                (bool isLienSuccess, string lienErrorMessage) = ProcessLienResponse(addLienResponse!);

                // Detailed error response is being returned here if the validation checks were not met
                if (!isLienSuccess)
                {
                    _logger.LogError($"Lien processing failed: {lienErrorMessage}");
                    return new StatusResponse(lienErrorMessage, ResponseCodes.ServiceError);
                }

                // Get the CSO account
                var user = await _userManager.FindByIdAsync(userId);

                // Update legal search record 
                legalSearch = await UpdateLegalSearchRecord(legalSearch, updateFinacleLegalRequest);

                // Assign lien ID, staff ID to legal search request
                legalSearch.LienId = addLienResponse!.Data.LienId;
                legalSearch.InitiatorId = user!.Id;
                legalSearch.StaffId = user.StaffId;
                legalSearch.UpdatedAt = TimeUtils.GetCurrentLocalTime();
                legalSearch.Status = RequestStatusType.Initiated.ToString();

                var result = await _legalSearchRequestManager.UpdateLegalSearchRequest(legalSearch);

                if (!result)
                {
                    _logger.LogError("Request could not be updated.");
                    return new ObjectResponse<string>("Request could not be updated", ResponseCodes.ServiceError);
                }

                // Enqueue the legal search request for background processing
                BackgroundJob.Enqueue<IBackgroundService>(x => x.AssignRequestToSolicitorsJob(legalSearch.Id));

                _logger.LogInformation("Request updated successfully, and queued for processing.");
                return new StatusResponse("Request updated successfully, and queued for processing", ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occurred inside UpdateFinacleRequestByCso. See reason: {JsonSerializer.Serialize(ex)}");
                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

        private async Task<LegalRequest> UpdateLegalSearchRecord(LegalRequest legalSearch, UpdateFinacleLegalRequest legalSearchRequest)
        {
            try
            {
                // Update legal search record with information from the update request
                legalSearch.RequestType = legalSearchRequest.RequestType;
                legalSearch.CustomerAccountName = legalSearchRequest.CustomerAccountName;
                legalSearch.CustomerAccountNumber = legalSearchRequest.CustomerAccountNumber;
                legalSearch.BusinessLocation = legalSearchRequest.BusinessLocation;
                legalSearch.RegistrationLocation = legalSearchRequest.RegistrationLocation;
                legalSearch.RegistrationNumber = legalSearchRequest.RegistrationNumber;
                legalSearch.RegistrationDate = legalSearchRequest.RegistrationDate;

                // Add additional information, registration & supporting documents
                await AddAdditionalInfoAndDocuments(legalSearchRequest, legalSearch);

                return legalSearch;
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during the update process
                _logger.LogError($"An exception occurred inside UpdateLegalSearchRecord. See reason: {JsonSerializer.Serialize(ex)}");
                throw; // Re-throw the exception to handle it at a higher level
            }
        }

        /// <summary>
        /// Cancels a legal search request based on the provided cancellation request.
        /// </summary>
        /// <param name="request">The cancellation request details.</param>
        /// <returns>A status response indicating the result of the cancellation action.</returns>
        public async Task<StatusResponse> CancelLegalSearchRequest(CancelRequest request)
        {
            try
            {
                // Perform the cancellation action
                var (status, errorMessage) = await CancelLegalSearchRequestAction(request);

                // Log the cancellation result
                _logger.LogInformation($"Cancellation result for request ID {request.RequestId}: Status - {status}, Message - {errorMessage}");

                // Return the status response based on the cancellation action result
                return new StatusResponse(errorMessage, status);
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during the cancellation process
                _logger.LogError($"An exception occurred inside CancelLegalSearchRequest. See reason: {JsonSerializer.Serialize(ex)}");

                // Return a generic error response in case of an exception
                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }


        /// <summary>
        /// Performs the cancellation action for a legal search request based on the provided cancellation request.
        /// </summary>
        /// <param name="request">The cancellation request details.</param>
        /// <returns>A tuple containing the cancellation status and an error message (if any).</returns>
        private async Task<(string status, string errorMessage)> CancelLegalSearchRequestAction(CancelRequest request)
        {
            try
            {
                // Retrieve the legal search request
                var legalSearchRequest = await _legalSearchRequestManager.GetLegalSearchRequest(request.RequestId);

                // Validate the existence of the legal search request
                if (legalSearchRequest == null)
                {
                    // Log and return a not found status with an error message
                    _logger.LogWarning($"CancelLegalSearchRequestAction: Request not found for ID {request.RequestId}");
                    return (ResponseCodes.NotFound, "Request not found");
                }

                // Update the status, reason for cancellation, and date of cancellation
                legalSearchRequest.Status = RequestStatusType.Cancelled.ToString();
                legalSearchRequest.ReasonForCancelling = request.Reason;
                legalSearchRequest.DateOfCancellation = TimeUtils.GetCurrentLocalTime();

                // Persist the changes
                bool updateStatus = await _legalSearchRequestManager.UpdateLegalSearchRequest(legalSearchRequest);

                if (!updateStatus)
                {
                    // Log and return a conflict status with an error message
                    _logger.LogWarning($"CancelLegalSearchRequestAction: Request ID {request.RequestId} could not be cancelled at this time.");
                    return (ResponseCodes.Conflict, "Request could not be cancelled at this time. Please try again");
                }

                // Log and return a success status with a success message
                _logger.LogInformation($"CancelLegalSearchRequestAction: Request ID {request.RequestId} successfully cancelled.");
                return (ResponseCodes.Success, "Request has been cancelled successfully");
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during the cancellation process
                _logger.LogError($"An exception occurred inside CancelLegalSearchRequestAction. See reason: {JsonSerializer.Serialize(ex)}");

                // Return a generic error status with an error message
                return (ResponseCodes.ServiceError, "Sorry, something went wrong. Please try again later.");
            }
        }

        /// <summary>
        /// Escalates a legal search request for further action.
        /// </summary>
        /// <param name="request">The escalation request details.</param>
        /// <returns>A response indicating the success of the operation.</returns>
        public async Task<StatusResponse> EscalateRequest(EscalateRequest request)
        {
            try
            {
                // Retrieve the legal search request
                var legalSearchRequest = await _legalSearchRequestManager.GetLegalSearchRequest(request.RequestId);

                if (legalSearchRequest == null)
                {
                    // Log and return a bad request status with an error message
                    _logger.LogWarning($"EscalateRequest: No legal search request found for ID {request.RequestId}");
                    return new StatusResponse("No legal search request was found with the given ID", ResponseCodes.BadRequest);
                }

                // Enqueue the request for escalation in the background
                BackgroundJob.Enqueue<IBackgroundService>(x => x.RequestEscalationJob(request));

                // Log and return a success status with a success message
                _logger.LogInformation($"EscalateRequest: Legal search request ID {request.RequestId} successfully escalated.");
                return new StatusResponse("Operation was successful", ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during the escalation process
                _logger.LogError($"An exception occurred inside EscalateRequest. See reason: {JsonSerializer.Serialize(ex)}");

                // Return a generic error status with an error message
                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

        /// <summary>
        /// Updates a legal search request by staff and queues it for processing.
        /// </summary>
        /// <param name="request">The updated request details.</param>
        /// <returns>A response indicating the success of the operation.</returns>
        public async Task<StatusResponse> UpdateRequestByStaff(UpdateRequest request)
        {
            try
            {
                // Get the legal search request by its ID
                var legalSearchRequest = await _legalSearchRequestManager.GetLegalSearchRequest(request.RequestId);

                if (legalSearchRequest == null)
                {
                    // Log and return a bad request status with an error message
                    _logger.LogWarning($"UpdateRequestByStaff: No request found for ID {request.RequestId}");
                    return new StatusResponse("No request matches the provided request ID", ResponseCodes.BadRequest);
                }

                // Update the legal search request with the provided details
                legalSearchRequest = await UpdateLegalSearchRequest(legalSearchRequest, request);

                // Update the legal search request in the database
                var result = await _legalSearchRequestManager.UpdateLegalSearchRequest(legalSearchRequest);

                if (!result)
                {
                    // Log and return an error status with an error message
                    _logger.LogError($"UpdateRequestByStaff: Request ID {request.RequestId} could not be updated");
                    return new ObjectResponse<string>("Request could not be updated", ResponseCodes.ServiceError);
                }

                // Enqueue the legal search request for further processing
                BackgroundJob.Enqueue<IBackgroundService>(x => x.AssignRequestToSolicitorsJob(legalSearchRequest.Id));

                // Log and return a success status with a success message
                _logger.LogInformation($"UpdateRequestByStaff: Request ID {request.RequestId} updated successfully and queued for processing");
                return new StatusResponse("Request updated successfully, and queued for processing", ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during the update process
                _logger.LogError($"An exception occurred inside UpdateRequestByStaff. See reason: {JsonSerializer.Serialize(ex)}");

                // Return a generic error status with an error message
                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

        /// <summary>
        /// Updates a legal search request with the provided details.
        /// </summary>
        /// <param name="legalSearchRequest">The existing legal search request to update.</param>
        /// <param name="request">The updated request details.</param>
        /// <returns>The updated legal search request.</returns>
        private async Task<LegalRequest> UpdateLegalSearchRequest(LegalRequest legalSearchRequest, UpdateRequest request)
        {
            try
            {
                // Update key request details
                legalSearchRequest.CustomerAccountName = request.CustomerAccountName;
                legalSearchRequest.CustomerAccountNumber = request.CustomerAccountNumber;
                legalSearchRequest.RequestType = request.RequestType;
                legalSearchRequest.BusinessLocation = request.BusinessLocation;
                legalSearchRequest.RegistrationLocation = request.RegistrationLocation;
                legalSearchRequest.ReasonForCancelling = request.ReasonForCancelling;
                legalSearchRequest.ReasonForRejection = request.ReasonForRejection;
                legalSearchRequest.RegistrationNumber = request.RegistrationNumber;
                legalSearchRequest.RegistrationDate = request.RegistrationDate;

                // Update documents associated with the request
                UpdateCurrentDocuments(request, legalSearchRequest);

                // Add additional information and documents if available
                await AddAdditionalInfoAndDocuments(request, legalSearchRequest);

                return legalSearchRequest;
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during the update process
                _logger.LogError($"An exception occurred inside UpdateLegalSearchRequest. See reason: {JsonSerializer.Serialize(ex)}");
                throw; // Re-throw the exception for higher-level handling
            }
        }

        /// <summary>
        /// Removes documents from the provided collection that are not present in the request file names.
        /// </summary>
        /// <param name="documents">The collection of documents to remove from.</param>
        /// <param name="requestFileNames">The set of file names from the request.</param>
        private void RemoveDocumentsNotInRequest(ICollection<SupportingDocument> documents, HashSet<string> requestFileNames)
        {
            try
            {
                // Identify documents to remove that are not present in the request
                var documentsToRemove = documents.Where(x => !requestFileNames.Contains(x.FileName)).ToList();

                // Remove the identified documents from the collection
                foreach (var documentToRemove in documentsToRemove)
                {
                    documents.Remove(documentToRemove);
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during the document removal process
                _logger.LogError($"An exception occurred inside RemoveDocumentsNotInRequest. See reason: {JsonSerializer.Serialize(ex)}");
                throw; // Re-throw the exception for higher-level handling
            }
        }

        /// <summary>
        /// Removes registration documents from the provided collection that are not present in the request file names.
        /// </summary>
        /// <param name="documents">The collection of registration documents to remove from.</param>
        /// <param name="requestFileNames">The set of file names from the request.</param>
        private void RemoveDocumentsNotInRequest(ICollection<RegistrationDocument> documents, HashSet<string> requestFileNames)
        {
            try
            {
                // Identify registration documents to remove that are not present in the request
                var documentsToRemove = documents.Where(x => !requestFileNames.Contains(x.FileName)).ToList();

                // Remove the identified registration documents from the collection
                foreach (var documentToRemove in documentsToRemove)
                {
                    documents.Remove(documentToRemove);
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during the document removal process
                _logger.LogError($"An exception occurred inside RemoveDocumentsNotInRequest. See reason: {JsonSerializer.Serialize(ex)}");
                throw; // Re-throw the exception for higher-level handling
            }
        }

        /// <summary>
        /// Updates the current documents of the legal search request based on the provided update request.
        /// </summary>
        /// <param name="request">The update request containing the new document information.</param>
        /// <param name="legalSearchRequest">The legal search request to be updated.</param>
        private void UpdateCurrentDocuments(UpdateRequest request, LegalRequest legalSearchRequest)
        {
            try
            {
                // Update registration documents
                if (request.RegistrationDocuments != null && request.RegistrationDocuments.Any())
                {
                    // Create a set of unique file names from request.RegistrationDocuments
                    var registrationDocumentFileNames = new HashSet<string>(request.RegistrationDocuments.Select(item => item.FileName));

                    // Remove documents not in request.RegistrationDocuments
                    RemoveDocumentsNotInRequest(legalSearchRequest.RegistrationDocuments, registrationDocumentFileNames);
                }

                // Update supporting documents
                if (request.SupportingDocuments != null && request.SupportingDocuments.Any())
                {
                    // Create a set of unique file names from request.SupportingDocuments
                    var supportingDocumentFileNames = new HashSet<string>(request.SupportingDocuments.Select(item => item.FileName));

                    // Remove documents not in request.SupportingDocuments
                    RemoveDocumentsNotInRequest(legalSearchRequest.SupportingDocuments, supportingDocumentFileNames);
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during document update
                _logger.LogError($"An exception occurred inside UpdateCurrentDocuments. See reason: {JsonSerializer.Serialize(ex)}");
                throw; // Re-throw the exception for higher-level handling
            }
        }

        /// <summary>
        /// Generates a request analytics report for staff based on the provided staff dashboard analytics request.
        /// </summary>
        /// <param name="request">The staff dashboard analytics request.</param>
        /// <returns>An object response containing the generated report as a byte array.</returns>
        public async Task<ObjectResponse<byte[]>> GenerateRequestAnalyticsReportForStaff(StaffDashboardAnalyticsRequest request)
        {
            try
            {
                // Fetch legal search requests for staff
                var response = await _legalSearchRequestManager.GetLegalRequestsForStaff(request);

                // Create a memory stream to hold the report data
                await using var outputStream = new MemoryStream();

                // Generate the report and write it to the memory stream
                ReportFileGenerator.WriteLegalSearchReportToStreamForStaff(outputStream, response);

                return new ObjectResponse<byte[]>("Successfully Generated Audit Report")
                {
                    Data = outputStream.ToArray()
                };
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during report generation
                _logger.LogError($"An exception occurred inside GenerateRequestAnalyticsReportForStaff. See reason: {JsonSerializer.Serialize(ex)}");

                return new ObjectResponse<byte[]>("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }


        /// <summary>
        /// Generates a request analytics report for a solicitor based on the provided solicitor request analytics payload.
        /// </summary>
        /// <param name="request">The solicitor request analytics payload.</param>
        /// <param name="solicitorId">The ID of the solicitor.</param>
        /// <returns>An object response containing the generated report as a byte array.</returns>
        public async Task<ObjectResponse<byte[]>> GenerateRequestAnalyticsReportForSolicitor(SolicitorRequestAnalyticsPayload request, Guid solicitorId)
        {
            try
            {
                // Fetch legal search requests for the solicitor
                var response = await _legalSearchRequestManager.GetLegalRequestsForSolicitor(request, solicitorId);

                // Create a memory stream to hold the report data
                await using var outputStream = new MemoryStream();

                // Generate the report and write it to the memory stream
                ReportFileGenerator.WriteLegalSearchReportToStreamForSolicitor(outputStream, response);

                return new ObjectResponse<byte[]>("Successfully Generated Audit Report")
                {
                    Data = outputStream.ToArray()
                };
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during report generation
                _logger.LogError($"An exception occurred inside GenerateRequestAnalyticsReportForSolicitor. See reason: {JsonSerializer.Serialize(ex)}");

                return new ObjectResponse<byte[]>("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

    }
}
