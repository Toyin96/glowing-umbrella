using Fcmb.Shared.Models.Responses;
using Hangfire;
using LegalSearch.Application.Interfaces.BackgroundService;
using LegalSearch.Application.Interfaces.FCMBService;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LegalSearch.Infrastructure.Services.LegalSearchService
{
    internal class LegalSearchRequestService : ILegalSearchRequestService
    {
        private readonly AppDbContext _appDbContext;
        private readonly ILogger<LegalSearchRequestService> _logger;
        private readonly IFCMBService _fCMBService;

        public LegalSearchRequestService(AppDbContext appDbContext,
            ILogger<LegalSearchRequestService> logger, IFCMBService fCMBService)
        {
            _appDbContext = appDbContext;
            _logger = logger;
            _fCMBService = fCMBService;
        }
        public async Task<ObjectResponse<string>> CreateNewRequest(LegalSearchRequest legalSearchRequest)
        {
            try
            {
                // get staff Id and add to payload

                // Validate customer's account status and funding
                //_fCMBService.GetAccountNameInquiry();

                // place lien on customer's account

                var newLegalSearchRequest = MapRequestToLegalRequest(legalSearchRequest);

                // add the files
                List<SupportingDocument> documents = await ProcessFiles(legalSearchRequest.SupportingDocuments);

                // attach document to request object
                documents.ForEach(x => newLegalSearchRequest.SupportingDocuments.Add(x));

                // persist request
                await _appDbContext.LegalSearchRequests.AddAsync(newLegalSearchRequest);

                var result = await _appDbContext.SaveChangesAsync() > 0;

                if (result == false)
                    return new ObjectResponse<string>("Request could not be created", ResponseCodes.ServiceError);

                // Enqueue the request for background processing
                BackgroundJob.Enqueue<IBackgroundService>(x => x.AssignRequestToSolicitors(newLegalSearchRequest.Id));

                return new ObjectResponse<string>("Request created successfully", ResponseCodes.Success);
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
