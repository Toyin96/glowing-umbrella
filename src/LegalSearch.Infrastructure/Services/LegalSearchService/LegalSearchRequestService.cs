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
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LegalSearch.Infrastructure.Services.LegalSearchService
{
    internal class LegalSearchRequestService : ILegalSearchRequestService
    {
        private readonly AppDbContext _appDbContext;
        private readonly ILogger<LegalSearchRequestService> _logger;
        private readonly IFCMBService _fCMBService;
        private readonly UserManager<Domain.Entities.User.User> _userManager;
        private readonly ILegalSearchRequestManager _legalSearchRequestManager;
        private readonly ISolicitorAssignmentManager _solicitorAssignmentManager;
        private readonly INotificationService _notificationService;
        private readonly FCMBServiceAppConfig _options;
        private readonly string _successStatusCode = "00";

        public LegalSearchRequestService(AppDbContext appDbContext,
            ILogger<LegalSearchRequestService> logger, IFCMBService fCMBService,
            UserManager<Domain.Entities.User.User> userManager,
            ILegalSearchRequestManager legalSearchRequestManager,
            ISolicitorAssignmentManager solicitorAssignmentManager,
            INotificationService notificationService,
            IOptions<FCMBServiceAppConfig> options)
        {
            _appDbContext = appDbContext;
            _logger = logger;
            _fCMBService = fCMBService;
            _userManager = userManager;
            _legalSearchRequestManager = legalSearchRequestManager;
            _solicitorAssignmentManager = solicitorAssignmentManager;
            _notificationService = notificationService;
            _options = options.Value;
        }

        public async Task<StatusResponse> AcceptLegalSearchRequest(AcceptRequest acceptRequest)
        {
            try
            {
                // get legal request
                var result = await FetchAndValidateRequest(acceptRequest.RequestId, acceptRequest.SolicitorId);

                if (result.errorCode == ResponseCodes.ServiceError)
                    return new StatusResponse(result.errorMessage ?? "Sorry, something went wrong. Please try again later.", result.errorCode);

                // update request status
                var request = result.request;

                // get solicitor assignment record
                var solicitorAssignmentRecord = await _solicitorAssignmentManager.GetSolicitorAssignmentBySolicitorId(request!.AssignedSolicitorId);

                // check if request is currently assigned to solicitor
                if (solicitorAssignmentRecord == null)
                    return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);

                solicitorAssignmentRecord.IsAccepted = true; // solicitor accepts request here

                request!.Status = nameof(RequestStatusType.LawyerAccepted);
                var isRequestUpdated = await _legalSearchRequestManager.UpdateLegalSearchRequest(request);

                if (!isRequestUpdated)
                    return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);

                return new StatusResponse("You have successfully accepted this request", ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occured inside AcceptLegalSearchRequest. See reason: {JsonSerializer.Serialize(ex)}");

                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

        public async Task<StatusResponse> CreateNewRequest(LegalSearchRequest legalSearchRequest, string userId)
        {
            try
            {
                // Validate customer's account status and balance
                var accountInquiryResponse = await _fCMBService.MakeAccountInquiry(legalSearchRequest.CustomerAccountNumber);

                // process name inquiry response to see if the account has enough balance for this action
                (bool isSuccess, string errorMessage) = ProcessAccountInquiryResponse(accountInquiryResponse!);

                // Detailed error response is being returned here if the validation checks were not met
                if (!isSuccess)
                    return new StatusResponse(errorMessage, ResponseCodes.ServiceError);

                // place lien on account in question to cover the cost of the legal search
                AddLienToAccountRequest lienRequest = GenerateLegalSearchLienRequestPayload(legalSearchRequest);

                // System attempts to place lien on customer's account
                //var addLienResponse = await _fCMBService.AddLien(lienRequest);

                // process name inquiry response to see if the account has enough balance for this action
                //(bool isSuccess, string errorMessage) lienVerificationResponse = ProcessLienResponse(addLienResponse!);
                
                // Detailed error response is being returned here if the validation checks were not met
                //if (!lienVerificationResponse.isSuccess)
                //    return new StatusResponse(lienVerificationResponse.errorMessage, ResponseCodes.ServiceError);

                // get the CSO account
                var user = await _userManager.FindByIdAsync(userId);

                // create new legal search request 
                var newLegalSearchRequest = MapRequestToLegalRequest(legalSearchRequest);

                // assign lien ID to request
                //newLegalSearchRequest.LienId = addLienResponse!.Data.LienId;

                //update request payload
                newLegalSearchRequest.BranchId = user.SolId ?? user!.BranchId!;
                newLegalSearchRequest.StaffId = user!.StaffId!;
                newLegalSearchRequest.InitiatorId = user!.Id;
                newLegalSearchRequest.RequestInitiator = user.FirstName;

                // add registration documents and other information here
                await AddAdditionalInfoAndDocuments(legalSearchRequest, newLegalSearchRequest);

                // persist request
                var result = await _legalSearchRequestManager.AddNewLegalSearchRequest(newLegalSearchRequest);

                if (result == false)
                    return new ObjectResponse<string>("Request could not be created", ResponseCodes.ServiceError);

                // Enqueue the request for background processing
                BackgroundJob.Enqueue<IBackgroundService>(x => x.AssignRequestToSolicitorsJob(newLegalSearchRequest.Id));

                return new StatusResponse("Request created successfully", ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occurred inside CreateNewRequest. See reason: {JsonSerializer.Serialize(ex)}");

                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

        private (bool isSuccess, string errorMessage) ProcessLienResponse(AddLienToAccountResponse addLienResponse)
        {
            if (addLienResponse == null)
                return (false, "Something went wrong. Please try again");

            if (addLienResponse != null && addLienResponse.Code != _successStatusCode)
                return (false, addLienResponse.Description);

            if (addLienResponse != null && addLienResponse.Code == _successStatusCode
                && !string.IsNullOrWhiteSpace(addLienResponse?.Data?.LienId))
            {
                return (true, "Lien was successfully applied on customer's account");
            }

            return (false, "Please try again");
        }

        private AddLienToAccountRequest GenerateLegalSearchLienRequestPayload(LegalSearchRequest legalSearchRequest)
        {
            return new AddLienToAccountRequest
            {
                RequestID = TimeUtils.GetCurrentLocalTime().Ticks.ToString(),
                AccountNo = legalSearchRequest.CustomerAccountNumber,
                AmountValue = Convert.ToDecimal(_options.LegalSearchAmount),
                CurrencyCode = nameof(CurrencyType.NGN),
                Rmks = _options.LegalSearchRemarks,
                ReasonCode = _options.LegalSearchReasonCode,
            };
        }

        private AddLienToAccountRequest GenerateLegalSearchLienRequestPayload(UpdateFinacleLegalRequest legalSearchRequest)
        {
            return new AddLienToAccountRequest
            {
                RequestID = TimeUtils.GetCurrentLocalTime().Ticks.ToString(),
                AccountNo = legalSearchRequest.CustomerAccountNumber,
                AmountValue = Convert.ToDecimal(_options.LegalSearchAmount),
                CurrencyCode = nameof(CurrencyType.NGN),
                Rmks = _options.LegalSearchRemarks,
                ReasonCode = _options.LegalSearchReasonCode,
            };
        }

        private (bool isSuccess, string errorMessage) ProcessAccountInquiryResponse(GetAccountInquiryResponse accountInquiryResponse)
        {
            bool isSuccessfullyParsedToDecimal = decimal.TryParse(_options.LegalSearchAmount, out decimal legalSearchAmount);

            if (!isSuccessfullyParsedToDecimal)
                return (false, "Something went wrong. Please try again");

            if (accountInquiryResponse == null)
                return (false, "Something went wrong. Please try again");

            if (accountInquiryResponse != null && accountInquiryResponse.Code != _successStatusCode)
                return (false, accountInquiryResponse.Description);

            if (accountInquiryResponse != null && accountInquiryResponse.Code == _successStatusCode
                && accountInquiryResponse.Data.AvailableBalance < legalSearchAmount)
            {
                return (true, "Customer does not have enough money to perform this action");
            }

            if (accountInquiryResponse != null && accountInquiryResponse.Code == _successStatusCode
                && accountInquiryResponse.Data.AvailableBalance >= legalSearchAmount)
            {
                return (true, "Name & balance inquiry was successful");
            }

            return (false, "Please try again");
        }

        private async Task AddAdditionalInfoAndDocuments(LegalSearchRequest legalSearchRequest,LegalRequest newLegalSearchRequest)
        {
            if (legalSearchRequest.AdditionalInformation != null)
            {
                newLegalSearchRequest.Discussions.Add(new Discussion { Conversation = legalSearchRequest.AdditionalInformation });
            }

            // add the registration document
            if (legalSearchRequest.RegistrationDocuments != null)
            {
                List<RegistrationDocument> documents = await ProcessRegistrationDocument(legalSearchRequest.RegistrationDocuments);

                // attach document to request object
                documents.ForEach(x => newLegalSearchRequest.RegistrationDocuments.Add(x));
            }

            // add the supporting documents
            if (legalSearchRequest.SupportingDocuments != null)
            {
                List<SupportingDocument> documents = await ProcessSupportingDocuments(legalSearchRequest.SupportingDocuments);

                // attach document to request object
                documents.ForEach(x => newLegalSearchRequest.SupportingDocuments.Add(x));
            }
        }

        private async Task AddAdditionalInfoAndDocuments(UpdateFinacleLegalRequest legalSearchRequest, LegalRequest newLegalSearchRequest)
        {
            if (legalSearchRequest.AdditionalInformation != null)
            {
                newLegalSearchRequest.Discussions.Add(new Discussion { Conversation = legalSearchRequest.AdditionalInformation });
            }

            // add the registration document
            if (legalSearchRequest.RegistrationDocuments != null)
            {
                List<RegistrationDocument> documents = await ProcessRegistrationDocument(legalSearchRequest.RegistrationDocuments);

                // attach document to request object
                documents.ForEach(x => newLegalSearchRequest.RegistrationDocuments.Add(x));
            }

            // add the supporting documents
            if (legalSearchRequest.SupportingDocuments != null)
            {
                List<SupportingDocument> documents = await ProcessSupportingDocuments(legalSearchRequest.SupportingDocuments);

                // attach document to request object
                documents.ForEach(x => newLegalSearchRequest.SupportingDocuments.Add(x));
            }
        }

        public async Task<StatusResponse> PushBackLegalSearchRequestForMoreInfo(ReturnRequest returnRequest, Guid solicitorId)
        {
            try
            {
                var result = await FetchAndValidateRequest(returnRequest.RequestId, solicitorId);

                if (result.errorCode == ResponseCodes.ServiceError)
                    return new StatusResponse(result.errorMessage ?? "Sorry, something went wrong. Please try again later.", result.errorCode);

                var request = result!.request;

                // add the files & feedback if any
                if (returnRequest.SupportingDocuments.Any())
                {
                    var supportingDocuments = await ProcessSupportingDocuments(returnRequest.SupportingDocuments);

                    supportingDocuments.ForEach(x =>
                    {
                        request!.SupportingDocuments.Add(x);
                    });
                }

                if (!string.IsNullOrEmpty(returnRequest.Feedback))
                {
                    request!.Discussions.Add(new Discussion { Conversation = returnRequest.Feedback });
                }

                // update request
                request!.Status = nameof(RequestStatusType.BackToCso);
                bool isRequestUpdated = await _legalSearchRequestManager.UpdateLegalSearchRequest(request!);

                if (isRequestUpdated == false)
                    return new StatusResponse("An error occurred while sending request. Please try again later.", result.errorCode);

                // Enqueue the request for background processing
                BackgroundJob.Enqueue<IBackgroundService>(x => x.PushBackRequestToCSOJob(request!.Id));

                return new StatusResponse("Request has been successfully pushed back to staff for additional information/clarification"
                    , ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occurred inside PushBackLegalSearchRequestForMoreInfo. See reason: {JsonSerializer.Serialize(ex)}");

                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

        public async Task<StatusResponse> RejectLegalSearchRequest(RejectRequest rejectRequest)
        {
            try
            {
                // get legal request
                var result = await FetchAndValidateRequest(rejectRequest.RequestId, rejectRequest.SolicitorId);

                if (result.errorCode == ResponseCodes.ServiceError)
                    return new StatusResponse(result.errorMessage ?? "Sorry, something went wrong. Please try again later.", result.errorCode);

                var request = result.request;

                // get solicitor assignment record
                var solicitorAssignmentRecord = await _solicitorAssignmentManager.GetSolicitorAssignmentBySolicitorId(request!.AssignedSolicitorId);

                // check if request is currently assigned to solicitor
                if (solicitorAssignmentRecord == null)
                    return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);

                request.ReasonForRejection = request.ReasonForRejection;
                request.Status = nameof(RequestStatusType.LawyerRejected);
                var isRequestUpdated = await _legalSearchRequestManager.UpdateLegalSearchRequest(request);

                if (!isRequestUpdated)
                    return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);

                // Enqueue the request for background processing
                BackgroundJob.Enqueue<IBackgroundService>(x => x.PushRequestToNextSolicitorInOrder(request.Id, solicitorAssignmentRecord.Order));

                return new StatusResponse("Operation is successful", ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occured inside RejectLegalSearchRequest. See reason: {JsonSerializer.Serialize(ex)}");

                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }
        public async Task<StatusResponse> SubmitRequestReport(SubmitLegalSearchReport submitLegalSearchReport, Guid solicitorId)
        {
            try
            {
                // get legal request
                var result = await FetchAndValidateAcceptedRequest(submitLegalSearchReport.RequestId, solicitorId);

                if (result.errorCode == ResponseCodes.ServiceError)
                    return new StatusResponse(result.errorMessage ?? "Sorry, something went wrong. Please try again later.", result.errorCode);

                var request = result.request;

                // verify that customer request have a lien ID
                if (request!.LienId == null)
                    return new StatusResponse("An error occurred while sending report. Please try again later.", result.errorCode);

                // add the files & feedback if any
                if (submitLegalSearchReport.RegistrationDocuments.Any())
                {
                    var supportingDocuments = await ProcessSupportingDocuments(submitLegalSearchReport.RegistrationDocuments);

                    supportingDocuments.ForEach(x =>
                    {
                        request!.SupportingDocuments.Add(x);
                    });
                }

                if (!string.IsNullOrEmpty(submitLegalSearchReport.Feedback))
                {
                    request!.Discussions.Add(new Discussion { Conversation = submitLegalSearchReport.Feedback });
                }

                // update request
                request!.Status = nameof(RequestStatusType.Completed);
                bool isRequestUpdated = await _legalSearchRequestManager.UpdateLegalSearchRequest(request!);

                if (isRequestUpdated == false)
                    return new StatusResponse("An error occurred while sending report. Please try again later.", result.errorCode);

                // Notify of the request update
                var notification = new Domain.Entities.Notification.Notification
                {
                    Title = "Request has been completed",
                    NotificationType = NotificationType.CompletedRequest,
                    Message = ConstantMessage.CompletedRequestMessage,
                    MetaData = JsonSerializer.Serialize(request)
                };

                // Push request to credit solicitor's account upon completion of request
                BackgroundJob.Enqueue<IBackgroundService>(x => x.InitiatePaymentToSolicitorJob(submitLegalSearchReport.RequestId));

                // notify the Initiating CSO
                await NotifyClient(request.InitiatorId, notification);

                return new StatusResponse("You have successfully submitted the report for this request"
                    , ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occurred inside SubmitRequestReport. See reason: {JsonSerializer.Serialize(ex)}");

                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

        private async Task<(LegalRequest? request, string? errorMessage, string errorCode)> FetchAndValidateRequest(Guid requestId, Guid solicitorId)
        {
            // get legal request
            var request = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);

            if (request == null)
                return (request, "Could not find request", BaseResponseCodes.ServiceError);

            // check if request is currently assigned to solicitor
            if (request.AssignedSolicitorId != solicitorId)
                return (null, "Sorry you cannot accept a request not assigned to you", ResponseCodes.ServiceError);

            if (request.Status != nameof(RequestStatusType.AssignedToLawyer))
                return (null, "Sorry you cannot perform this action at this time.", ResponseCodes.ServiceError);

            return (request, null, ResponseCodes.Success);
        }

        private async Task<(LegalRequest? request, string? errorMessage, string errorCode)> FetchAndValidateAcceptedRequest(Guid requestId, Guid solicitorId)
        {
            // get legal request
            var request = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);

            if (request == null)
                return (request, "Could not find request", BaseResponseCodes.ServiceError);

            // check if request is currently assigned to solicitor
            if (request.AssignedSolicitorId != solicitorId)
                return (null, "Sorry you cannot accept a request not assigned to you", ResponseCodes.ServiceError);

            if (request.Status != nameof(RequestStatusType.LawyerAccepted))
                return (null, "Sorry you cannot perform this action at this time.", ResponseCodes.ServiceError);

            return (request, null, ResponseCodes.Success);
        }

        private async Task<List<SupportingDocument>> ProcessSupportingDocuments(List<IFormFile> files)
        {
            dynamic documents = new List<SupportingDocument>();

            foreach (var formFile in files)
            {
                if (formFile.Length == 0)
                {
                    continue;
                }

                using (var memoryStream = new MemoryStream())
                {
                    await formFile.CopyToAsync(memoryStream);
                    var fileContent = memoryStream.ToArray();

                    var fileType = Path.GetExtension(formFile.FileName).ToLower();

                    documents.Add(new SupportingDocument
                    {
                        FileName = formFile.FileName,
                        FileContent = fileContent,
                        FileType = fileType
                    });

                }
            }

            return documents;
        }

        private async Task<List<RegistrationDocument>> ProcessRegistrationDocument(List<IFormFile> files)
        {
            dynamic documents = new List<RegistrationDocument>();

            foreach (var formFile in files)
            {
                if (formFile.Length == 0)
                {
                    continue;
                }

                using (var memoryStream = new MemoryStream())
                {
                    await formFile.CopyToAsync(memoryStream);
                    var fileContent = memoryStream.ToArray();

                    var fileType = Path.GetExtension(formFile.FileName).ToLower();

                    documents.Add(new RegistrationDocument
                    {
                        FileName = formFile.FileName,
                        FileContent = fileContent,
                        FileType = fileType
                    });

                }
            }

            return documents;
        }
        private async Task NotifyClient(Guid userId, Domain.Entities.Notification.Notification notification)
        {
            //send notification to client
            await _notificationService.SendNotificationToUser(userId, notification);
        }
        private LegalRequest MapRequestToLegalRequest(LegalSearchRequest request)
        {
            return new LegalRequest
            {
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
        }

        public async Task<ObjectResponse<GetAccountInquiryResponse>> PerformNameInquiryOnAccount(string accountNumber)
        {
            try
            {
                // Validate customer's account status and balance
                var accountInquiryResponse = await _fCMBService.MakeAccountInquiry(accountNumber);

                if (accountInquiryResponse == null)
                    return new ObjectResponse<GetAccountInquiryResponse>("Something went wrong. Please try again.", ResponseCodes.ServiceError);

                // add legal search amount to response payload
                accountInquiryResponse.Data.LegalSearchAmount = Convert.ToDecimal(_options.LegalSearchAmount);

                return new ObjectResponse<GetAccountInquiryResponse>("Operation was successful", ResponseCodes.Success)
                {
                    Data = accountInquiryResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occurred inside PerformNameInquiryOnAccount. See reason: {JsonSerializer.Serialize(ex)}");

                return new ObjectResponse<GetAccountInquiryResponse>("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

        public async Task<ObjectResponse<LegalSearchRootResponsePayload>> GetLegalRequestsForSolicitor(SolicitorRequestAnalyticsPayload viewRequestAnalyticsPayload, Guid solicitorId)
        {
            var response = await _legalSearchRequestManager.GetLegalRequestsForSolicitor(viewRequestAnalyticsPayload, solicitorId);

            return new ObjectResponse<LegalSearchRootResponsePayload>("Successfully Retrieved Legal Search Requests")
            {
                Data = response,
            };
        }

        public async Task<ObjectResponse<CsoRootResponsePayload>> GetLegalRequestsForCso(CsoDashboardAnalyticsRequest request, Guid csoId)
        {
            var response = await _legalSearchRequestManager.GetLegalRequestsForCso(request, csoId);

            return new ObjectResponse<CsoRootResponsePayload>("Successfully Retrieved Legal Search Requests")
            {
                Data = response,
            };
        }

        public async Task<StatusResponse> CreateNewRequestFromFinacle(FinacleLegalSearchRequest legalSearchRequest)
        {
            var newLegalSearchRequest = MapFinacleRequestToLegalRequest(legalSearchRequest);

            // persist request
            var result = await _legalSearchRequestManager.AddNewLegalSearchRequest(newLegalSearchRequest);

            if (result == false)
                return new ObjectResponse<string>("Request could not be created", ResponseCodes.ServiceError);

            return new StatusResponse("Request created successfully", ResponseCodes.Success);
        }

        private LegalRequest MapFinacleRequestToLegalRequest(FinacleLegalSearchRequest request)
        {
            return new LegalRequest
            {
                RequestSource = RequestSourceType.Finacle,
                BranchId = request.BranchId,
                CustomerAccountName = request.CustomerAccountName,
                CustomerAccountNumber = request.CustomerAccountNumber,
                Status = RequestStatusType.UnAssigned.ToString(),
            };
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

        public async Task<StatusResponse> UpdateFinacleRequestByCso(UpdateFinacleLegalRequest legalSearchRequest, string userId)
        {
            try
            {
                // fetch legal search request 
                var legalSearch = await _legalSearchRequestManager.GetLegalSearchRequest(legalSearchRequest.RequestId);

                if (legalSearch == null)
                    return new StatusResponse("No matching Legal search record found with the ID provided", ResponseCodes.ServiceError);

                // Validate customer's account status and balance
                var accountInquiryResponse = await _fCMBService.MakeAccountInquiry(legalSearchRequest.CustomerAccountNumber);

                // process name inquiry response to see if the account has enough balance for this action
                (bool isSuccess, string errorMessage) = ProcessAccountInquiryResponse(accountInquiryResponse!);

                // Detailed error response is being returned here if the validation checks were not met
                if (!isSuccess)
                    return new StatusResponse(errorMessage, ResponseCodes.ServiceError);

                // place lien on account in question to cover the cost of the legal search
                AddLienToAccountRequest lienRequest = GenerateLegalSearchLienRequestPayload(legalSearchRequest);

                // System attempts to place lien on customer's account
                var addLienResponse = await _fCMBService.AddLien(lienRequest);

                // process name inquiry response to see if the account has enough balance for this action
                (bool isSuccess, string errorMessage) lienVerificationResponse = ProcessLienResponse(addLienResponse!);

                // Detailed error response is being returned here if the validation checks were not met
                if (!lienVerificationResponse.isSuccess)
                    return new StatusResponse(lienVerificationResponse.errorMessage, ResponseCodes.ServiceError);

                // get the CSO account
                var user = await _userManager.FindByIdAsync(userId);

                // update legal search record 
                legalSearch = await UpdateLegalSearchRecord(legalSearch, legalSearchRequest);

                // assign lien ID, staff ID to request
                legalSearch.LienId = addLienResponse!.Data.LienId;
                legalSearch.InitiatorId = user!.Id;
                legalSearch.StaffId = user.StaffId;
                legalSearch.UpdatedAt = TimeUtils.GetCurrentLocalTime();
                legalSearch.Status = RequestStatusType.Initiated.ToString();

                var result = await _legalSearchRequestManager.UpdateLegalSearchRequest(legalSearch);

                if (result == false)
                    return new ObjectResponse<string>("Request could not be created", ResponseCodes.ServiceError);

                // Enqueue the request for background processing
                BackgroundJob.Enqueue<IBackgroundService>(x => x.AssignRequestToSolicitorsJob(legalSearch.Id));

                return new StatusResponse("Request updated successfully, and queued for processing", ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occurred inside CreateNewRequest. See reason: {JsonSerializer.Serialize(ex)}");

                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

        private async Task<LegalRequest> UpdateLegalSearchRecord(LegalRequest legalSearch, UpdateFinacleLegalRequest legalSearchRequest)
        {
            legalSearch.RequestType = legalSearchRequest.RequestType;
            legalSearch.CustomerAccountName = legalSearchRequest.CustomerAccountName;
            legalSearch.CustomerAccountNumber = legalSearchRequest.CustomerAccountNumber;
            legalSearch.BusinessLocation = legalSearchRequest.BusinessLocation;
            legalSearch.RegistrationLocation = legalSearchRequest.RegistrationLocation;
            legalSearch.RegistrationNumber = legalSearchRequest.RegistrationNumber;
            legalSearch.RegistrationDate = legalSearchRequest.RegistrationDate;

            // add additional information, registration & supporting documents
            await AddAdditionalInfoAndDocuments(legalSearchRequest, legalSearch);

            return legalSearch;
        }

        public async Task<StatusResponse> CancelLegalSearchRequest(CancelRequest request)
        {
            var response = await ValidateCancelLegalSearchRequest(request);

            return new StatusResponse(response.errorMessage, response.status);
        }

        private async Task<(string status, string errorMessage)> ValidateCancelLegalSearchRequest(CancelRequest request)
        {
            // get request
            var legalSearchRequest = await _legalSearchRequestManager.GetLegalSearchRequest(request.RequestId);

            // validate request
            if (legalSearchRequest == null)
                return (ResponseCodes.NotFound, "Request not found");

            legalSearchRequest.Status = RequestStatusType.UnAssigned.ToString();
            legalSearchRequest.ReasonForRejection = request.Reason;

            // persist changes
            bool updateStatus = await _legalSearchRequestManager.UpdateLegalSearchRequest(legalSearchRequest);

            if (!updateStatus)
            {
                return (ResponseCodes.Conflict, "Request could not be cancelled at this time. Please try again");
            }

            return (ResponseCodes.Success, "Request have been cancelled successfully");
        }
    }
}
