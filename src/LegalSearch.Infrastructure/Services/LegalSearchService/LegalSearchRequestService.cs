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
        private static Random random = new Random();


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

        public async Task<StatusResponse> AcceptLegalSearchRequest(AcceptRequest request)
        {
            try
            {
                // get legal legalSearchRequest
                var result = await FetchAndValidateRequest(request.RequestId, request.SolicitorId, ActionType.AcceptRequest);

                if (result.errorCode == ResponseCodes.ServiceError)
                    return new StatusResponse(result.errorMessage ?? "Sorry, something went wrong. Please try again later.", result.errorCode);

                // update legalSearchRequest status
                var legalSearchRequest = result.request;

                // get solicitor assignment record
                var solicitorAssignmentRecord = await _solicitorAssignmentManager.GetSolicitorAssignmentBySolicitorId(legalSearchRequest!.AssignedSolicitorId);

                // check if legalSearchRequest is currently assigned to solicitor
                if (solicitorAssignmentRecord == null)
                    return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);

                solicitorAssignmentRecord.IsAccepted = true; // solicitor accepts legalSearchRequest here

                legalSearchRequest!.Status = nameof(RequestStatusType.LawyerAccepted);
                var isRequestUpdated = await _legalSearchRequestManager.UpdateLegalSearchRequest(legalSearchRequest);

                if (!isRequestUpdated)
                    return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);

                return new StatusResponse("You have successfully accepted this request", ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occurred inside AcceptLegalSearchRequest. See reason: {JsonSerializer.Serialize(ex)}");

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

                // create new legal search legalSearchRequest 
                var newLegalSearchRequest = MapRequestToLegalRequest(legalSearchRequest);

                // assign lien ID to legalSearchRequest
                //newLegalSearchRequest.LienId = addLienResponse!.Data.LienId;

                //update legalSearchRequest payload
                newLegalSearchRequest.BranchId = user.SolId ?? user!.BranchId!;
                newLegalSearchRequest.StaffId = user!.StaffId!;
                newLegalSearchRequest.InitiatorId = user!.Id;
                newLegalSearchRequest.RequestInitiator = user.FirstName;

                // add registration documents and other information here
                await AddAdditionalInfoAndDocuments(legalSearchRequest, newLegalSearchRequest);

                // persist legalSearchRequest
                var result = await _legalSearchRequestManager.AddNewLegalSearchRequest(newLegalSearchRequest);

                if (!result)
                    return new ObjectResponse<string>("Request could not be created", ResponseCodes.ServiceError);

                // Enqueue the legalSearchRequest for background processing
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

            if (addLienResponse is not null && addLienResponse.Code != _successStatusCode)
                return (false, addLienResponse.Description);

            if (addLienResponse is not null && addLienResponse.Code == _successStatusCode
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
                RequestID = $"{_options.LegalSearchReasonCode}{TimeUtils.GetCurrentLocalTime().Ticks}",
                AccountNo = legalSearchRequest.CustomerAccountNumber,
                AmountValue = Convert.ToDecimal(_options.LegalSearchAmount),
                CurrencyCode = nameof(CurrencyType.NGN),
                Rmks = _options.LegalSearchRemarks,
                ReasonCode = $"{_options.LegalSearchReasonCode}{GenerateUnique5DigitNumber()}"
            };
        }

        private static int GenerateUnique5DigitNumber()
        {
            return random.Next(10000, 100000); // Generate a random 5-digit number
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

            if (accountInquiryResponse is not null && accountInquiryResponse.Code != _successStatusCode)
                return (false, accountInquiryResponse.Description);

            if (accountInquiryResponse is not null && accountInquiryResponse.Code == _successStatusCode
                && accountInquiryResponse.Data.AvailableBalance < legalSearchAmount)
            {
                return (true, "Customer does not have enough money to perform this action");
            }

            if (accountInquiryResponse is not null && accountInquiryResponse.Code == _successStatusCode
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

                // attach document to legalSearchRequest object
                documents.ForEach(x => newLegalSearchRequest.RegistrationDocuments.Add(x));
            }

            // add the supporting documents
            if (legalSearchRequest.SupportingDocuments != null)
            {
                List<SupportingDocument> documents = await ProcessSupportingDocuments(legalSearchRequest.SupportingDocuments);

                // attach document to legalSearchRequest object
                documents.ForEach(x => newLegalSearchRequest.SupportingDocuments.Add(x));
            }
        }

        private async Task AddAdditionalInfoAndDocuments(UpdateRequest legalSearchRequest, LegalRequest newLegalSearchRequest)
        {
            if (legalSearchRequest.AdditionalInformation != null)
            {
                newLegalSearchRequest.Discussions.Add(new Discussion { Conversation = legalSearchRequest.AdditionalInformation });
            }

            // add the registration document
            if (legalSearchRequest.RegistrationDocuments != null)
            {
                List<RegistrationDocument> documents = await ProcessRegistrationDocument(legalSearchRequest.RegistrationDocuments);

                // attach document to legalSearchRequest object
                documents.ForEach(x => newLegalSearchRequest.RegistrationDocuments.Add(x));
            }

            // add the supporting documents
            if (legalSearchRequest.SupportingDocuments != null)
            {
                List<SupportingDocument> documents = await ProcessSupportingDocuments(legalSearchRequest.SupportingDocuments);

                // attach document to legalSearchRequest object
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

                // attach document to legalSearchRequest object
                documents.ForEach(x => newLegalSearchRequest.RegistrationDocuments.Add(x));
            }

            // add the supporting documents
            if (legalSearchRequest.SupportingDocuments != null)
            {
                List<SupportingDocument> documents = await ProcessSupportingDocuments(legalSearchRequest.SupportingDocuments);

                // attach document to legalSearchRequest object
                documents.ForEach(x => newLegalSearchRequest.SupportingDocuments.Add(x));
            }
        }

        public async Task<StatusResponse> PushBackLegalSearchRequestForMoreInfo(ReturnRequest returnRequest, Guid solicitorId)
        {
            try
            {
                var result = await FetchAndValidateRequest(returnRequest.RequestId, solicitorId, ActionType.ReturnRequest);

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

                // update legalSearchRequest
                request!.Status = nameof(RequestStatusType.BackToCso);
                bool isRequestUpdated = await _legalSearchRequestManager.UpdateLegalSearchRequest(request!);

                if (isRequestUpdated == false)
                    return new StatusResponse("An error occurred while sending request. Please try again later.", result.errorCode);

                // Enqueue the legalSearchRequest for background processing
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

        public async Task<StatusResponse> RejectLegalSearchRequest(RejectRequest request)
        {
            try
            {
                // get legal legalSearchRequest
                var result = await FetchAndValidateRequest(request.RequestId, request.SolicitorId, ActionType.RejectRequest);

                if (result.errorCode == ResponseCodes.ServiceError)
                    return new StatusResponse(result.errorMessage ?? "Sorry, something went wrong. Please try again later.", result.errorCode);

                var legalSearchRequest = result.request;

                // get solicitor assignment record
                var solicitorAssignmentRecord = await _solicitorAssignmentManager.GetSolicitorAssignmentBySolicitorId(legalSearchRequest!.AssignedSolicitorId);

                // check if legalSearchRequest is currently assigned to solicitor
                if (solicitorAssignmentRecord == null)
                    return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);

                legalSearchRequest.ReasonForRejection = request.RejectionMessage;
                legalSearchRequest.Status = nameof(RequestStatusType.LawyerRejected);
                var isRequestUpdated = await _legalSearchRequestManager.UpdateLegalSearchRequest(legalSearchRequest);

                if (!isRequestUpdated)
                    return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);

                // Enqueue the legalSearchRequest for background processing
                BackgroundJob.Enqueue<IBackgroundService>(x => x.PushRequestToNextSolicitorInOrder(legalSearchRequest.Id, solicitorAssignmentRecord.Order));

                return new StatusResponse("Operation is successful", ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occurred inside RejectLegalSearchRequest. See reason: {JsonSerializer.Serialize(ex)}");

                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }
        public async Task<StatusResponse> SubmitRequestReport(SubmitLegalSearchReport submitLegalSearchReport, Guid solicitorId)
        {
            try
            {
                // get legal legalSearchRequest
                var result = await FetchAndValidateAcceptedRequest(submitLegalSearchReport.RequestId, solicitorId);

                if (result.errorCode == ResponseCodes.ServiceError)
                    return new StatusResponse(result.errorMessage ?? "Sorry, something went wrong. Please try again later.", result.errorCode);

                var request = result.request;

                // get the cso that initiated the request
                var cso = await _userManager.FindByIdAsync(request.InitiatorId.ToString());
                if (cso == null)
                    return new StatusResponse(result.errorMessage ?? "Sorry, something went wrong. Please try again later.", result.errorCode);

                // verify that customer legalSearchRequest have a lien ID
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

                // update legalSearchRequest
                request!.Status = nameof(RequestStatusType.Completed);
                request.RequestSubmissionDate = TimeUtils.GetCurrentLocalTime();
                bool isRequestUpdated = await _legalSearchRequestManager.UpdateLegalSearchRequest(request!);

                if (!isRequestUpdated)
                    return new StatusResponse("An error occurred while sending report. Please try again later.", result.errorCode);

                // Notify the COS of completion of legalSearchRequest update
                var notification = new Domain.Entities.Notification.Notification
                {
                    Title = "Request has been completed",
                    RecipientUserId = request.InitiatorId.ToString(),
                    RecipientUserEmail = cso.Email,
                    NotificationType = NotificationType.CompletedRequest,
                    Message = ConstantMessage.CompletedRequestMessage,
                    MetaData = JsonSerializer.Serialize(request)
                };

                // Push legalSearchRequest to credit solicitor's account upon completion of legalSearchRequest
                BackgroundJob.Enqueue<IBackgroundService>(x => x.InitiatePaymentToSolicitorJob(submitLegalSearchReport.RequestId));

                // notify the Initiating CSO
                await NotifyClient(request.AssignedSolicitorId, notification);

                return new StatusResponse("You have successfully submitted the report for this request"
                    , ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occurred inside SubmitRequestReport. See reason: {JsonSerializer.Serialize(ex)}");

                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

        private async Task<(LegalRequest? request, string? errorMessage, string errorCode)> FetchAndValidateRequest(Guid requestId, Guid solicitorId, ActionType actionType)
        {
            // get legal legalSearchRequest
            var request = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);

            if (request == null)
                return (request, "Could not find request", BaseResponseCodes.ServiceError);

            switch (actionType)
            {
                case ActionType.AcceptRequest:
                    if (request.AssignedSolicitorId != solicitorId)
                        return (null, "Sorry you cannot accept a request not assigned to you", ResponseCodes.ServiceError);

                    if (request.Status != nameof(RequestStatusType.AssignedToLawyer))
                        return (null, "Something went wrong, please try again later", ResponseCodes.ServiceError);
                    break;
                case ActionType.RejectRequest:
                    if (request.AssignedSolicitorId != solicitorId)
                        return (null, "Sorry you cannot reject a request not assigned to you", ResponseCodes.ServiceError);

                    if (request.Status != nameof(RequestStatusType.AssignedToLawyer))
                        return (null, "You've already accepted this request so you cannot reject it", ResponseCodes.ServiceError);
                    break;
                case ActionType.ReturnRequest:
                    if (request.AssignedSolicitorId != solicitorId)
                        return (null, "Sorry you cannot return a request not assigned to you", ResponseCodes.ServiceError);

                    if (request.Status != nameof(RequestStatusType.LawyerAccepted))
                        return (null, "You need to accept the request before you can return it for additional information", ResponseCodes.ServiceError);
                    break;
                default:
                    break;
            }

            return (request, null, ResponseCodes.Success);
        }

        private async Task<(LegalRequest? request, string? errorMessage, string errorCode)> FetchAndValidateAcceptedRequest(Guid requestId, Guid solicitorId)
        {
            // get legal legalSearchRequest
            var request = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);

            if (request == null)
                return (request, "Could not find request", BaseResponseCodes.ServiceError);

            // check if legalSearchRequest is currently assigned to solicitor
            if (request.AssignedSolicitorId != solicitorId)
                return (null, "Sorry you cannot accept a request not assigned to you", ResponseCodes.ServiceError);

            if (request.Status != nameof(RequestStatusType.LawyerAccepted))
                return (null, "Sorry, you can't submit a report; it's being routed back to the CSO for more information.", ResponseCodes.ServiceError);

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

                using var memoryStream = new MemoryStream();
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

                using var memoryStream = new MemoryStream();
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

                if (accountInquiryResponse == null || accountInquiryResponse?.Data == null)
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

        public async Task<ObjectResponse<StaffRootResponsePayload>> GetLegalRequestsForStaff(StaffDashboardAnalyticsRequest request)
        {
            var response = await _legalSearchRequestManager.GetLegalRequestsForStaff(request);

            return new ObjectResponse<StaffRootResponsePayload>("Successfully Retrieved Legal Search Requests")
            {
                Data = response,
            };
        }

        public async Task<StatusResponse> CreateNewRequestFromFinacle(FinacleLegalSearchRequest legalSearchRequest)
        {
            var newLegalSearchRequest = MapFinacleRequestToLegalRequest(legalSearchRequest);

            // persist legalSearchRequest
            var result = await _legalSearchRequestManager.AddNewLegalSearchRequest(newLegalSearchRequest);

            if (!result)
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

        public async Task<StatusResponse> UpdateFinacleRequestByCso(UpdateFinacleLegalRequest request, string userId)
        {
            try
            {
                // fetch legal search legalSearchRequest 
                var legalSearch = await _legalSearchRequestManager.GetLegalSearchRequest(request.RequestId);

                if (legalSearch == null)
                    return new StatusResponse("No matching Legal search record found with the ID provided", ResponseCodes.ServiceError);

                if (legalSearch.RequestSource == RequestSourceType.Staff)
                    return new StatusResponse("You can't edit this request via this route", ResponseCodes.Forbidden);

                // Validate customer's account status and balance
                var accountInquiryResponse = await _fCMBService.MakeAccountInquiry(request.CustomerAccountNumber);

                // process name inquiry response to see if the account has enough balance for this action
                (bool isSuccess, string errorMessage) = ProcessAccountInquiryResponse(accountInquiryResponse!);

                // Detailed error response is being returned here if the validation checks were not met
                if (!isSuccess)
                    return new StatusResponse(errorMessage, ResponseCodes.ServiceError);

                // place lien on account in question to cover the cost of the legal search
                AddLienToAccountRequest lienRequest = GenerateLegalSearchLienRequestPayload(request);

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
                legalSearch = await UpdateLegalSearchRecord(legalSearch, request);

                // assign lien ID, staff ID to legalSearchRequest
                legalSearch.LienId = addLienResponse!.Data.LienId;
                legalSearch.InitiatorId = user!.Id;
                legalSearch.StaffId = user.StaffId;
                legalSearch.UpdatedAt = TimeUtils.GetCurrentLocalTime();
                legalSearch.Status = RequestStatusType.Initiated.ToString();

                var result = await _legalSearchRequestManager.UpdateLegalSearchRequest(legalSearch);

                if (!result)
                    return new ObjectResponse<string>("Request could not be updated", ResponseCodes.ServiceError);

                // Enqueue the legalSearchRequest for background processing
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
            var (status, errorMessage) = await CancelLegalSearchRequestAction(request);

            return new StatusResponse(errorMessage, status);
        }

        private async Task<(string status, string errorMessage)> CancelLegalSearchRequestAction(CancelRequest request)
        {
            // get legalSearchRequest
            var legalSearchRequest = await _legalSearchRequestManager.GetLegalSearchRequest(request.RequestId);

            // validate legalSearchRequest
            if (legalSearchRequest == null)
                return (ResponseCodes.NotFound, "Request not found");

            legalSearchRequest.Status = RequestStatusType.Cancelled.ToString();
            legalSearchRequest.ReasonForCancelling = request.Reason;
            legalSearchRequest.DateOfCancellation = TimeUtils.GetCurrentLocalTime();    

            // persist changes
            bool updateStatus = await _legalSearchRequestManager.UpdateLegalSearchRequest(legalSearchRequest);

            if (!updateStatus)
            {
                return (ResponseCodes.Conflict, "Request could not be cancelled at this time. Please try again");
            }

            return (ResponseCodes.Success, "Request have been cancelled successfully");
        }

        public async Task<StatusResponse> EscalateRequest(EscalateRequest request)
        {
            var legalSearchRequest = await _legalSearchRequestManager.GetLegalSearchRequest(request.RequestId);

            if (legalSearchRequest == null)
                return new StatusResponse("No legal search request was found with the given ID", ResponseCodes.BadRequest);

            // push to notification queue
            BackgroundJob.Enqueue<IBackgroundService>(x => x.RequestEscalationJob(request));

            return new StatusResponse("Operation was successful", ResponseCodes.Success);
        }

        public async Task<StatusResponse> UpdateRequestByStaff(UpdateRequest request)
        {
            // get legalSearchRequest
            var legalSearchRequest = await _legalSearchRequestManager.GetLegalSearchRequest(request.RequestId);

            if (legalSearchRequest == null)
                return new StatusResponse("No request match the requestID you provided", ResponseCodes.BadRequest);

            legalSearchRequest = await UpdateLegalSearchRequest(legalSearchRequest, request);

            var result = await _legalSearchRequestManager.UpdateLegalSearchRequest(legalSearchRequest);

            if (!result)
                return new ObjectResponse<string>("Request could not be updated", ResponseCodes.ServiceError);

            // Enqueue the legalSearchRequest for background processing
            BackgroundJob.Enqueue<IBackgroundService>(x => x.AssignRequestToSolicitorsJob(legalSearchRequest.Id));

            return new StatusResponse("Request updated successfully, and queued for processing", ResponseCodes.Success);
        }

        private async Task<LegalRequest> UpdateLegalSearchRequest(LegalRequest legalSearchRequest, UpdateRequest request)
        {
            legalSearchRequest.CustomerAccountName = request.CustomerAccountName;   
            legalSearchRequest.CustomerAccountNumber = request.CustomerAccountNumber;
            legalSearchRequest.RequestType = request.RequestType;
            legalSearchRequest.BusinessLocation = request.BusinessLocation;
            legalSearchRequest.RegistrationLocation = request.RegistrationLocation;
            legalSearchRequest.ReasonForCancelling = request.ReasonForCancelling;
            legalSearchRequest.ReasonForRejection = request.ReasonForRejection;
            legalSearchRequest.RegistrationNumber = request.RegistrationNumber;
            legalSearchRequest.RegistrationDate = request.RegistrationDate;

            UpdateCurrentDocuments(request, legalSearchRequest);

            await AddAdditionalInfoAndDocuments(request, legalSearchRequest);

            return legalSearchRequest;
        }

        private void RemoveDocumentsNotInRequest(ICollection<SupportingDocument> documents, HashSet<string> requestFileNames)
        {
            var documentsToRemove = documents.Where(x => !requestFileNames.Contains(x.FileName)).ToList();

            foreach (var documentToRemove in documentsToRemove)
            {
                documents.Remove(documentToRemove);
            }
        }

        private void RemoveDocumentsNotInRequest(ICollection<RegistrationDocument> documents, HashSet<string> requestFileNames)
        {
            var documentsToRemove = documents.Where(x => !requestFileNames.Contains(x.FileName)).ToList();

            foreach (var documentToRemove in documentsToRemove)
            {
                documents.Remove(documentToRemove);
            }
        }

        private void UpdateCurrentDocuments(UpdateRequest request, LegalRequest legalSearchRequest)
        {
            if (request.RegistrationDocuments != null && request.RegistrationDocuments.Any())
            {
                // Create a set of unique file names from legalSearchRequest.RegistrationDocuments
                var registrationDocumentFileNames = new HashSet<string>(request.RegistrationDocuments.Select(item => item.FileName));

                // Remove documents not in legalSearchRequest.RegistrationDocuments
                RemoveDocumentsNotInRequest(legalSearchRequest.RegistrationDocuments, registrationDocumentFileNames);
            }

            if (request.SupportingDocuments != null && request.SupportingDocuments.Any())
            {
                // Create a set of unique file names from legalSearchRequest.SupportingDocuments
                var SupportingDocumentFileNames = new HashSet<string>(request.SupportingDocuments.Select(item => item.FileName));

                // Remove documents not in legalSearchRequest.SupportingDocuments
                RemoveDocumentsNotInRequest(legalSearchRequest.SupportingDocuments, SupportingDocumentFileNames);
            }
        }

        public async Task<ObjectResponse<byte[]>> GenerateRequestAnalyticsReportForStaff(StaffDashboardAnalyticsRequest request)
        {
            var response = await _legalSearchRequestManager.GetLegalRequestsForStaff(request);

            await using var outputStream = new MemoryStream();

            ReportFileGenerator.WriteLegalSearchReportToStreamForStaff(outputStream, response);

            return new ObjectResponse<byte[]>("Successfully Generated Audit Report")
            {
                Data = outputStream.ToArray()
            };
        }

        public async Task<ObjectResponse<byte[]>> GenerateRequestAnalyticsReportForSolicitor(SolicitorRequestAnalyticsPayload request, Guid solicitorId)
        {
            var response = await _legalSearchRequestManager.GetLegalRequestsForSolicitor(request, solicitorId);

            await using var outputStream = new MemoryStream();

            ReportFileGenerator.WriteLegalSearchReportToStreamForSolicitor(outputStream, response);

            return new ObjectResponse<byte[]>("Successfully Generated Audit Report")
            {
                Data = outputStream.ToArray()
            };
        }
    }
}
