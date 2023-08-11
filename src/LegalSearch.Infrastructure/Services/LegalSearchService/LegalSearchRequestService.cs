using Fcmb.Shared.Models.Responses;
using Hangfire;
using LegalSearch.Application.Interfaces.BackgroundService;
using LegalSearch.Application.Interfaces.FCMBService;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
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
        private readonly ISolicitorAssignmentManager _solicitorAssignmentManager1;

        public LegalSearchRequestService(AppDbContext appDbContext,
            ILogger<LegalSearchRequestService> logger, IFCMBService fCMBService,
            UserManager<Domain.Entities.User.User> userManager,
            ILegalSearchRequestManager legalSearchRequestManager, 
            ISolicitorAssignmentManager solicitorAssignmentManager,
            ISolicitorAssignmentManager solicitorAssignmentManager1)
        {
            _appDbContext = appDbContext;
            _logger = logger;
            _fCMBService = fCMBService;
            _userManager = userManager;
            _legalSearchRequestManager = legalSearchRequestManager;
            _solicitorAssignmentManager = solicitorAssignmentManager;
            _solicitorAssignmentManager1 = solicitorAssignmentManager1;
        }

        public async Task<StatusResponse> AcceptLegalSearchRequest(AcceptRequest acceptRequest)
        {
            try
            {
                // get legal request
                var request = await _legalSearchRequestManager.GetLegalSearchRequest(acceptRequest.RequestId);

                if (request == null)
                    return new StatusResponse("Could not find request", ResponseCodes.ServiceError);

                // check if request is currently assigned to solicitor
                if (request.AssignedSolicitorId != acceptRequest.SolicitorId)
                    return new StatusResponse("Sorry you cannot accept a request not assigned to you", ResponseCodes.ServiceError);

                if (request.Status != nameof(RequestStatusType.Lawyer))
                    return new StatusResponse("Sorry you cannot perform this action at this time.", ResponseCodes.ServiceError);

                // update request status
                request.Status = nameof(RequestStatusType.LawyerAccepted);
                var isRequestUpdated = await _legalSearchRequestManager.UpdateLegalSearchRequest(request);

                if (!isRequestUpdated)
                    return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);

                return new StatusResponse("Request was successfully accepted", ResponseCodes.Success);
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
                // get staff Id and add to payload

                // Validate customer's account status and funding
                //_fCMBService.GetAccountNameInquiry();

                // place lien on customer's account

                // get user
                var user = await _userManager.FindByIdAsync(userId);
                var branch = _appDbContext.Branches.First(x => x.SolId == user.SolId)?.Address;

                if (branch == null)
                    return new ObjectResponse<string>("Request could not be created", ResponseCodes.ServiceError);

                var newLegalSearchRequest = MapRequestToLegalRequest(legalSearchRequest);
                newLegalSearchRequest.Branch = branch;
                newLegalSearchRequest.RequestInitiator = user.FirstName;
                
                // add the files
                List<SupportingDocument> documents = await ProcessFiles(legalSearchRequest.SupportingDocuments);

                // attach document to request object
                documents.ForEach(x => newLegalSearchRequest.SupportingDocuments.Add(x));

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

        public async Task<StatusResponse> RejectLegalSearchRequest(RejectRequest rejectRequest)
        {
            try
            {
                // get legal request
                var request = await _legalSearchRequestManager.GetLegalSearchRequest(rejectRequest.RequestId);

                if (request == null)
                    return new StatusResponse("Could not find request", ResponseCodes.ServiceError);

                // check if request is currently assigned to solicitor
                if (request.AssignedSolicitorId != rejectRequest.SolicitorId) 
                    return new StatusResponse("Sorry you cannot reject a request not assigned to you", ResponseCodes.ServiceError);

                // get solicitor assignment record
                var solicitorAssignmentRecord = await _solicitorAssignmentManager.GetSolicitorAssignmentBySolicitorId(request.AssignedSolicitorId);

                // check if request is currently assigned to solicitor
                if (solicitorAssignmentRecord == null)
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
                CustomerAccount = request.CustomerAccount,
                Status = nameof(RequestStatusType.Initiated),
                AdditionalInformation = request.AdditionalInformation,
            };
        }

        private async Task<List<SupportingDocument>> ProcessFiles(List<IFormFile> files)
        {
            var documents = new List<SupportingDocument>();

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
    }
}
