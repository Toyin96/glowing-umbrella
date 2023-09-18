﻿using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Requests.CSO;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses.Solicitor;
using LegalSearch.Domain.Enums.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LegalSearch.Api.Controllers
{
    /// <summary>
    /// This controller houses endpoints that can be called by both the CSO and Legal Perfection Team
    /// </summary>
    /// <seealso cref="LegalSearch.Api.Controllers.BaseController" />
    [Authorize(AuthenticationSchemes = "Bearer", Roles = $"{nameof(RoleType.LegalPerfectionTeam)}, {nameof(RoleType.Cso)}")]
    [Route("api/[controller]")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ApiController]
    public class CommonsController : BaseController
    {
        private readonly ILegalSearchRequestService _legalSearchRequestService;
        private readonly ISolicitorService _solicitorService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommonsController"/> class.
        /// </summary>
        /// <param name="legalSearchRequestService">The legal search request service.</param>
        /// <param name="solicitorService">The solicitor service.</param>
        public CommonsController(ILegalSearchRequestService legalSearchRequestService, ISolicitorService solicitorService)
        {
            _legalSearchRequestService = legalSearchRequestService;
            _solicitorService = solicitorService;
        }

        /// <summary>
        /// This endpoint allows the CSO to escalate a legal search request and also gives room for the Legal Perfection Team to escalate request to a solicitor
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [HttpPost("EscalateRequest")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> EscalateRequest([FromBody] EscalateRequest request)
        {
            var result = await _legalSearchRequestService.EscalateRequest(request);
            return HandleResponse(result);
        }

        /// <summary>
        /// This endpoint allows the CSO and Legal Perfection Team to edit/update a legal search request
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [HttpPost("UpdateRequest")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> UpdateRequest([FromForm] UpdateRequest request)
        {
            var result = await _legalSearchRequestService.UpdateRequestByStaff(request);
            return HandleResponse(result);
        }

        /// <summary>
        /// Endpoint to cancel a legal search request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [HttpPost("CancelRequest")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> CancelRequest([FromBody] CancelRequest request)
        {
            var result = await _legalSearchRequestService.CancelLegalSearchRequest(request);
            return HandleResponse(result);
        }

        /// <summary>
        /// Endpoint to view solicitors based on search params.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [HttpGet("ViewSolicitors")]
        public async Task<ActionResult<ListResponse<SolicitorProfileDto>>> ViewSolicitors([FromQuery] ViewSolicitorsRequestFilter request)
        {
            var response = await _solicitorService.ViewSolicitors(request);
            return HandleResponse(response);
        }

        /// <summary>
        /// Endpoint to update solicitor's profile
        /// </summary>
        /// <remarks>
        /// The solicitor receives a message notifying the solicitor if the request was successful or not
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("EditSolicitorProfile")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> EditProfile([FromBody] EditSolicitorProfileByLegalTeamRequest request)
        {
            var response = await _solicitorService.EditSolicitorProfile(request);
            return HandleResponse(response);
        }

        /// <summary>
        /// This endpoint allows the CSO and Legal Perfection Team to activate/deactivate a solicitor
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [HttpPost("ActivateOrDeactivateSolicitor")]
        public async Task<ActionResult<StatusResponse>> ActivateOrDeactivateSolicitor([FromBody] ActivateOrDeactivateSolicitorRequest request)
        {
            var response = await _solicitorService.ActivateOrDeactivateSolicitor(request);
            return HandleResponse(response);
        }
    }
}
