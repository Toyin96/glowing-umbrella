using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Requests.LegalPerfectionTeam;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses.Solicitor;
using LegalSearch.Domain.Enums.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegalSearch.Api.Controllers
{
    /// <summary>
    /// Controller for legal perfection teams.
    /// </summary>
    [Authorize(AuthenticationSchemes = "Bearer", Roles = nameof(RoleType.LegalPerfectionTeam))]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class LegalPerfectionTeamsController : BaseController
    {
        private readonly ISolicitorService _solicitorService;

        /// <summary>
        /// Initializes a new instance of the <see cref="LegalPerfectionTeamsController"/> class.
        /// </summary>
        /// <param name="solicitorService">The solicitor service.</param>
        public LegalPerfectionTeamsController(ISolicitorService solicitorService)
        {
            _solicitorService = solicitorService;
        }

        /// <summary>
        /// Manually assigns a request to a solicitor.
        /// </summary>
        /// <param name="request">The request containing assignment details.</param>
        /// <returns>A response indicating the status of the assignment.</returns>
        [HttpPost("ManuallyAssignRequestToSolicitor")]
        public async Task<ActionResult<StatusResponse>> ManuallyAssignRequestToSolicitor([FromBody] ManuallyAssignRequestToSolicitorRequest request)
        {
            var response = await _solicitorService.ManuallyAssignRequestToSolicitor(request);
            return HandleResponse(response);
        }

        /// <summary>
        /// Gets the profiles of solicitors based on their profile statuses.
        /// </summary>
        /// <param name="request">The request containing region filter.</param>
        /// <returns>A response containing a list of solicitor profiles.</returns>
        [HttpGet("ViewMappedSolicitorsProfiles")]
        public async Task<ActionResult<ListResponse<SolicitorProfileResponseDto>>> ViewMappedSolicitorsProfiles([FromQuery] ViewSolicitorsBasedOnRegionRequestFilter request)
        {
            var response = await _solicitorService.ViewMappedSolicitorsProfiles(request);
            return HandleResponse(response);
        }
    }
}
