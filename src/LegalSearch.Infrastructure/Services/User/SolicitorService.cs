using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Application.Models.Responses.Solicitor;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Utilities;
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

        public SolicitorService(AppDbContext appDbContext, 
            ISolicitorManager solicitorProfileManager, ILogger<SolicitorService> logger)
        {
            _appDbContext = appDbContext;
            _solicitorProfileManager = solicitorProfileManager;
            _logger = logger;
        }
        public async Task<StatusResponse> EditSolicitorProfile(EditSolicitoProfileRequest editSolicitoProfileRequest, Guid userId)
        {
            try
            {
                bool isProfileUpdated = await _solicitorProfileManager.EditSolicitorProfile(editSolicitoProfileRequest, userId);

                if (isProfileUpdated)
                {
                    return new StatusResponse("You have successfully reset your profile", ResponseCodes.Success);
                }

                return new StatusResponse("We could not update your profile at this moment. Please try again", ResponseCodes.ServiceError);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occured inside EditSolicitorProfile. See reason: {JsonSerializer.Serialize(ex)}");

                return new StatusResponse("Sorry, something went wrong. Please try again later.", ResponseCodes.ServiceError);
            }
        }

        public async Task<ObjectResponse<SolicitorProfileDto>?> ViewSolicitorProfile(Guid userId)
        {
            var solicitor = await _appDbContext.Users.Include(x => x.Firm).ThenInclude(x => x!.State).FirstOrDefaultAsync(x => x.Id == userId);

            if (solicitor == null)
                return new ObjectResponse<SolicitorProfileDto>("Solicitor not found", ResponseCodes.ServiceError);

            return new ObjectResponse<SolicitorProfileDto>("Operation was succesful", ResponseCodes.Success)
            {  
                Data = new SolicitorProfileDto
                {
                    SolicitorName = solicitor.FirstName,
                    Firm = solicitor.Firm!.Name,
                    SolicitorEmail = solicitor.Email!,
                    SolicitorAddress = solicitor.Firm.Address,
                    SolicitorPhoneNumber = solicitor.PhoneNumber!,
                    SolicitorState = solicitor.State!.Name,
                    SolicitorRegion = _appDbContext.Regions.FindAsync(solicitor.State.RegionId).Result?.Name
                }
            };
        }
    }
}
