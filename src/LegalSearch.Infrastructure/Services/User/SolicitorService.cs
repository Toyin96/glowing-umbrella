using Azure.Core;
using Fcmb.Shared.Models.Responses;
using Hangfire;
using LegalSearch.Application.Interfaces.BackgroundService;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests.LegalPerfectionTeam;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses.Solicitor;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Domain.Enums.User;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LegalSearch.Infrastructure.Services.User
{
    internal class SolicitorService : ISolicitorService
    {
        private readonly AppDbContext _appDbContext;
        private readonly ISolicitorManager _solicitorProfileManager;
        private readonly ILogger<SolicitorService> _logger;
        private readonly UserManager<Domain.Entities.User.User> _userManager;
        private readonly ILegalSearchRequestManager _legalSearchRequestManager;

        public SolicitorService(AppDbContext appDbContext, 
            ISolicitorManager solicitorProfileManager, ILogger<SolicitorService> logger,
            UserManager<Domain.Entities.User.User> userManager, ILegalSearchRequestManager legalSearchRequestManager)
        {
            _appDbContext = appDbContext;
            _solicitorProfileManager = solicitorProfileManager;
            _logger = logger;
            _userManager = userManager;
            _legalSearchRequestManager = legalSearchRequestManager;
        }

        public async Task<StatusResponse> ActivateOrDeactivateSolicitor(ActivateOrDeactivateSolicitorRequest request)
        {
            // get solicitor
            var user = await _userManager.FindByIdAsync(request.SolicitorId.ToString());

            if (user == null)
                return new StatusResponse("User not found", ResponseCodes.DataNotFound);

            user.ProfileStatus = request.ActionType switch
            {
                ProfileStatusActionType.Activate => ProfileStatusType.Active.ToString(),
                ProfileStatusActionType.DeActivate => ProfileStatusType.InActive.ToString(),
                _ => user.ProfileStatus,
            };

            var updateStatus = await _userManager.UpdateAsync(user);

            if (updateStatus.Succeeded == false)
                return new StatusResponse("Solicitor's status was not updated. Try again later", ResponseCodes.Conflict);

            return new StatusResponse("Solicitor status has been updated successfully", ResponseCodes.Success);
        }

        public async Task<StatusResponse> EditSolicitorProfile(EditSolicitorProfileRequest request, Guid userId)
        {
            try
            {
                bool isProfileUpdated = await _solicitorProfileManager.EditSolicitorProfile(request, userId);

                if (isProfileUpdated)
                {
                    return new StatusResponse("You have successfully reset your profile", ResponseCodes.Success);
                }

                return new StatusResponse("We could not update your profile at this moment. Please try again", ResponseCodes.ServiceError);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occurred inside EditSolicitorProfile. See reason: {JsonSerializer.Serialize(ex)}");

                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

        public async Task<StatusResponse> ManuallyAssignRequestToSolicitor(ManuallyAssignRequestToSolicitorRequest request)
        {
            // get solicitor
            var user = await _userManager.FindByIdAsync(request.SolicitorId.ToString());

            if (user == null)
                return new StatusResponse("User not found", ResponseCodes.DataNotFound);

            // get request
            var legalSearchRequest = await _legalSearchRequestManager.GetLegalSearchRequest(request.RequestId);

            if (legalSearchRequest == null)
                return new StatusResponse("LegalSearchRequest not found", ResponseCodes.DataNotFound);

            if (legalSearchRequest.Status != RequestStatusType.UnAssigned.ToString())
                return new StatusResponse("Only unassigned requests can be manually assigned to solicitors", ResponseCodes.Conflict);

            BackgroundJob.Enqueue<IBackgroundService>(x => x.ManuallyAssignRequestToSolicitorJob(request.RequestId, request.SolicitorId));

            return new StatusResponse("Request have been pushed to solicitor's tab", ResponseCodes.Success);
        }

        public async Task<ObjectResponse<SolicitorProfileDto>?> ViewSolicitorProfile(Guid userId)
        {
            var solicitor = await _appDbContext.Users.Include(x => x.Firm).ThenInclude(x => x!.State).FirstOrDefaultAsync(x => x.Id == userId);

            if (solicitor == null)
                return new ObjectResponse<SolicitorProfileDto>("Solicitor not found", ResponseCodes.DataNotFound);

            return new ObjectResponse<SolicitorProfileDto>("Operation was successful", ResponseCodes.Success)
            {  
                Data = new SolicitorProfileDto
                {
                    SolicitorName = solicitor.FirstName,
                    Firm = solicitor.Firm!.Name,
                    FirmId = solicitor.Firm.Id,
                    SolicitorEmail = solicitor.Email!,
                    Status = solicitor.ProfileStatus,
                    SolicitorAddress = solicitor.Firm.Address,
                    SolicitorPhoneNumber = solicitor.PhoneNumber!,
                    SolicitorState = solicitor.State!.Name,
                    SolicitorRegion = _appDbContext.Regions.FindAsync(solicitor.State.RegionId).Result?.Name
                }
            };
        }

        public Task<ListResponse<SolicitorProfileDto>> ViewSolicitors(ViewSolicitorsRequestFilter viewSolicitorsRequestFilter)
        {
            throw new NotImplementedException();
        }
    }
}
