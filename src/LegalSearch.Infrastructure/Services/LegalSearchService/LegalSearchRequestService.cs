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
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses;
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
                (bool isSuccess, string errorMessage) nameInquiryVeificationResponse = ProcessAccountInquiryResponse(accountInquiryResponse);

                // Detailed error response is being returned here if the validation checks were not met
                if (!nameInquiryVeificationResponse.isSuccess)
                    return new StatusResponse(nameInquiryVeificationResponse.errorMessage, ResponseCodes.ServiceError);

                // place lien on account in question to cover the cost of the legal search
                AddLienToAccountRequest lienRequest = GenerateLegalSearchLienRequestPayload(legalSearchRequest);

                // System attempts to place lien on customer's account
                var addLienResponse = await _fCMBService.AddLien(lienRequest);

                // process name inquiry response to see if the account has enough balance for this action
                (bool isSuccess, string errorMessage) lienVeificationResponse = ProcessLienResponse(addLienResponse);
                
                // Detailed error response is being returned here if the validation checks were not met
                if (!lienVeificationResponse.isSuccess)
                    return new StatusResponse(lienVeificationResponse.errorMessage, ResponseCodes.ServiceError);

                // get the CSO account
                var user = await _userManager.FindByIdAsync(userId);
                var branch = _appDbContext.Branches.First(x => x.SolId == user!.SolId)?.Address;

                if (branch == null)
                    return new ObjectResponse<string>("Request could not be created", ResponseCodes.ServiceError);

                // create new legal search request 
                var newLegalSearchRequest = MapRequestToLegalRequest(legalSearchRequest);

                // assign lien ID to request
                newLegalSearchRequest.LienId = addLienResponse.Data.LienId;

                // add registration documents and other information here
                await AddAdditionalInfoAndDocuments(legalSearchRequest, user, branch, newLegalSearchRequest);

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
                _logger.LogError($"An exception occured inside CreateNewRequest. See reason: {JsonSerializer.Serialize(ex)}");

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
                return (true, "Name & balance inquiry was successsful");
            }

            return (false, "Please try again");
        }

        private async Task AddAdditionalInfoAndDocuments(LegalSearchRequest legalSearchRequest, Domain.Entities.User.User? user, string? branch, LegalRequest newLegalSearchRequest)
        {
            if (legalSearchRequest.AdditionalInformation != null)
            {
                newLegalSearchRequest.Discussions.Add(new Discussion { Conversation = legalSearchRequest.AdditionalInformation });
            }

            newLegalSearchRequest.Branch = branch;
            newLegalSearchRequest.InitiatorId = user!.Id;
            newLegalSearchRequest.RequestInitiator = user.FirstName;

            // add the files
            if (legalSearchRequest.AdditionalInformation != null)
            {
                List<RegistrationDocument> documents = await ProcessFile(legalSearchRequest.RegistrationDocuments);

                // attach document to request object
                documents.ForEach(x => newLegalSearchRequest.RegistrationDocuments.Add(x));
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
                    var supportingDocuments = await ProcessFiles(returnRequest.SupportingDocuments);

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
                    return new StatusResponse("An error occured while sending request. Please try again later.", result.errorCode);

                // Enqueue the request for background processing
                BackgroundJob.Enqueue<IBackgroundService>(x => x.PushBackRequestToCSOJob(request!.Id));

                return new StatusResponse("Request has been successfully pushed back to staff for additional information/clarification"
                    , ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occured inside PushBackLegalSearchRequestForMoreInfo. See reason: {JsonSerializer.Serialize(ex)}");

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

                // add the files & feedback if any
                if (submitLegalSearchReport.RegistrationDocuments.Any())
                {
                    var supportingDocuments = await ProcessFiles(submitLegalSearchReport.RegistrationDocuments);

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
                    return new StatusResponse("An error occured while sending report. Please try again later.", result.errorCode);

                // Notify of the request update
                var notification = new Domain.Entities.Notification.Notification
                {
                    Title = "Request has been completed",
                    NotificationType = NotificationType.CompletedRequest,
                    Message = ConstantMessage.CompletedRequestMessage,
                    MetaData = JsonSerializer.Serialize(request)
                };

                // TODO: credit solicitor's account

                // notify the Initiating CSO
                await NotifyClient(request.InitiatorId, notification);

                return new StatusResponse("You have successfully submitted the report for this request"
                    , ResponseCodes.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occured inside SubmitRequestReport. See reason: {JsonSerializer.Serialize(ex)}");

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

        private async Task<List<SupportingDocument>> ProcessFiles(List<IFormFile> files)
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

        private async Task<List<RegistrationDocument>> ProcessFile(List<IFormFile> files)
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
                StaffId = request.StaffId,
                RequestType = request.RequestType,
                BusinessLocation = request.BusinessLocation,
                RegistrationDate = request.RegistrationDate,
                RegistrationLocation = request.RegistrationLocation,
                RegistrationNumber = request.RegistrationNumber,
                CustomerAccountName = request.CustomerAccountName,
                CustomerAccountNumber = request.CustomerAccountNumber,
                Status = nameof(RequestStatusType.Initiated),
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

                return new ObjectResponse<GetAccountInquiryResponse>("Operation was successful", ResponseCodes.Success)
                {
                    Data = accountInquiryResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occured inside PerformNameInquiryOnAccount. See reason: {JsonSerializer.Serialize(ex)}");

                return new ObjectResponse<GetAccountInquiryResponse>("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }
    }
}
