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

        public LegalSearchRequestService(AppDbContext appDbContext,
            ILogger<LegalSearchRequestService> logger, IFCMBService fCMBService,
            UserManager<Domain.Entities.User.User> userManager,
            ILegalSearchRequestManager legalSearchRequestManager, 
            ISolicitorAssignmentManager solicitorAssignmentManager)
        {
            _appDbContext = appDbContext;
            _logger = logger;
            _fCMBService = fCMBService;
            _userManager = userManager;
            _legalSearchRequestManager = legalSearchRequestManager;
            _solicitorAssignmentManager = solicitorAssignmentManager;
        }

        public Task<StatusResponse> AcceptLegalSearchRequest(AcceptRequest acceptRequest)
        {
            throw new NotImplementedException();
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
            catch (Exception)
            {

                throw;
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


                if (request.AssignedSolicitorId != rejectRequest.SolicitorId) 
                    return new StatusResponse("Sorry you cannot reject a request not mapped to you", ResponseCodes.ServiceError); 
                


                // Enqueue the request for background processing
                BackgroundJob.Enqueue<IBackgroundService>(x => x.PushRequestToNextSolicitorInOrder(request.Id, 0));

                return new StatusResponse("Request has been successfully rejected", ResponseCodes.Success);
            }
            catch (Exception)
            {

                throw;
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
