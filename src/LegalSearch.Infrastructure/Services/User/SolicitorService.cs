using Azure;
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
using LegalSearch.Domain.Entities.Role;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Domain.Enums.Role;
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

        public async Task<StatusResponse> EditSolicitorProfile(EditSolicitorProfileByLegalTeamRequest request)
        {
            try
            {
                bool isProfileUpdated = await _solicitorProfileManager.EditSolicitorProfile(request, request.SolicitorId);

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
            var solicitor = await _appDbContext.Users
                .Include(x => x.Firm)
                .ThenInclude(x => x!.State)
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (solicitor == null)
                return new ObjectResponse<SolicitorProfileDto>("Solicitor not found", ResponseCodes.DataNotFound);

            // get role
            var roles = await _userManager.GetRolesAsync(solicitor);

            if (roles == null || roles?[0] != nameof(RoleType.Solicitor))
                return new ObjectResponse<SolicitorProfileDto>("User role not found", ResponseCodes.DataNotFound);

            var solicitorRegion = await _appDbContext.Regions.FindAsync(solicitor.State.RegionId);

            return new ObjectResponse<SolicitorProfileDto>("Operation was successful", ResponseCodes.Success)
            {
                Data = new SolicitorProfileDto
                {
                    SolicitorId = solicitor.Id,
                    FirstName = solicitor.FirstName,
                    LastName = solicitor.LastName,
                    Firm = solicitor.Firm!.Name,
                    FirmId = solicitor.Firm.Id,
                    SolicitorEmail = solicitor.Email!,
                    Status = solicitor.ProfileStatus,
                    SolicitorAddress = solicitor.Firm.Address,
                    SolicitorPhoneNumber = solicitor.PhoneNumber!,
                    BankAccountNumber = solicitor.BankAccount,
                    SolicitorState = solicitor.Firm.State!.Name,
                    SolicitorStateId = solicitor.Firm.StateId.HasValue ? solicitor.Firm.StateId.Value : solicitor.State.Id,
                    SolicitorStateOfCoverageId = solicitor.Firm.StateOfCoverageId,
                    SolicitorRegion = solicitorRegion?.Name!
                }
            };
        }


        public async Task<ListResponse<SolicitorProfileDto>> ViewSolicitors(ViewSolicitorsRequestFilter viewSolicitorsRequestFilter)
        {
            var solicitors = await SolicitorFilter(viewSolicitorsRequestFilter);

            return new ListResponse<SolicitorProfileDto>("Operation was successful", ResponseCodes.Success)
            {
                Data = solicitors,
                Total = solicitors.Count
            };
        }

        private async Task<List<SolicitorProfileDto>> SolicitorFilter(ViewSolicitorsRequestFilter request)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(nameof(RoleType.Solicitor));

            var query = _appDbContext.Users
                .Where(user => usersInRole.Contains(user)); // Filter users by role

            if (request.RegionId != null)
            {
                query = query.Where(user => user.State != null && user.State.RegionId == request.RegionId);
            }

            if (request.FirmId != null)
            {
                query = query.Where(user => user.Firm != null && user.Firm.Id == request.FirmId);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(user => user.ProfileStatus == request.Status.ToString());
            }

            var solicitors = await query
                .Include(user => user.Firm)
                .Include(user => user.State)
                    .ThenInclude(state => state.Region)
                .Select(x => new SolicitorProfileDto
                {
                    SolicitorId = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    FirmId = x.Firm.Id,
                    Firm = x.Firm.Name,
                    SolicitorEmail = x.Email,
                    SolicitorPhoneNumber = x.PhoneNumber,
                    SolicitorState = x.State!.Name,
                    BankAccountNumber = x.BankAccount,
                    SolicitorStateId = x.Firm.StateId.HasValue ? x.Firm.StateId.Value : x.State.Id,
                    SolicitorStateOfCoverageId = x.Firm.StateOfCoverageId,
                    SolicitorAddress = x.Firm.Address,
                    SolicitorRegion = x.State!.Region!.Name,
                    Status = x.ProfileStatus,
                })
                .ToListAsync();

            return solicitors ?? new List<SolicitorProfileDto> ();
        }
    }
}
