﻿using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Requests.LegalPerfectionTeam;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses.Solicitor;
using LegalSearch.Domain.Enums.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        public LegalPerfectionTeamsController(ISolicitorService solicitorService)
        {
            _solicitorService = solicitorService;
        }

        [HttpPost("ManuallyAssignRequestToSolicitor")]
        public async Task<ActionResult<StatusResponse>> ManuallyAssignRequestToSolicitor([FromBody] ManuallyAssignRequestToSolicitorRequest request)
        {
            var response = await _solicitorService.ManuallyAssignRequestToSolicitor(request);
            return HandleResponse(response);
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
