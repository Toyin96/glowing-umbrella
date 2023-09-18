using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Requests.CSO;
using LegalSearch.Application.Models.Requests.LegalPerfectionTeam;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses.CSO;
using LegalSearch.Application.Models.Responses.Solicitor;
using LegalSearch.Domain.Enums;
using LegalSearch.Domain.Enums.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LegalSearch.Api.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer", Roles = nameof(RoleType.LegalPerfectionTeam))]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class LegalPerfectionTeamsController : BaseController
    {
        private readonly ISolicitorService _solicitorService;
        private readonly ILegalSearchRequestService _legalSearchRequestService;

        public LegalPerfectionTeamsController(ISolicitorService solicitorService, ILegalSearchRequestService legalSearchRequestService)
        {
            _solicitorService = solicitorService;
            _legalSearchRequestService = legalSearchRequestService;
        }

        [HttpPost("ManuallyAssignRequestToSolicitor")]
        public async Task<ActionResult<StatusResponse>> ManuallyAssignRequestToSolicitor([FromBody] ManuallyAssignRequestToSolicitorRequest request)
        {
            var response = await _solicitorService.ManuallyAssignRequestToSolicitor(request);
            return HandleResponse(response);
        }

        /// <summary>
        /// Endpoint to get legal search request analytics for Legal Perfection Team
        /// </summary>
        /// <param name="csoDashboardAnalyticsRequest"></param>
        /// <returns></returns>
        [HttpGet("ViewRequestAnalytics")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ObjectResponse<StaffRootResponsePayload>>> ViewRequestAnalytics([FromQuery] StaffDashboardAnalyticsRequest csoDashboardAnalyticsRequest)
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.UserId))!.Value;
            var result = await _legalSearchRequestService.GetLegalRequestsForStaff(csoDashboardAnalyticsRequest, Guid.Parse(userId));

            return HandleResponse(result);
        }

        /// <summary>
        /// Endpoint for viewing registered solicitors and their locations/regions.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [HttpGet("ViewMappedSolicitorsProfiles")]
        public async Task<ActionResult<ListResponse<SolicitorProfileResponseDto>>> ViewMappedSolicitorsProfiles([FromQuery] ViewSolicitorsBasedOnRegionRequestFilter request)
        {
            var response = await _solicitorService.ViewMappedSolicitorsProfiles(request);
            return HandleResponse(response);
        }
    }
}
