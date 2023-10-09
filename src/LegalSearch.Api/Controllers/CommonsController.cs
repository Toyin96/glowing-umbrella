using Azure;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests.CSO;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses.CSO;
using LegalSearch.Application.Models.Responses.Solicitor;
using LegalSearch.Domain.Enums;
using LegalSearch.Domain.Enums.Role;
using LegalSearch.Domain.Enums.User;
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
        /// Endpoint to get legal search request analytics for CSO and Legal Perfection Team
        /// </summary>
        /// <param name="csoDashboardAnalyticsRequest"></param>
        /// <returns></returns>
        [HttpGet("ViewRequestAnalytics")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ObjectResponse<StaffRootResponsePayload>>> ViewRequestAnalytics([FromQuery] StaffDashboardAnalyticsRequest csoDashboardAnalyticsRequest)
        {
            var result = await _legalSearchRequestService.GetLegalRequestsForStaff(csoDashboardAnalyticsRequest);

            return HandleResponse(result);
        }

        /// <summary>
        /// Endpoint to generate legal search report for CSO and Legal Perfection Team
        /// </summary>
        /// <param name="csoDashboardAnalyticsRequest"></param>
        /// <returns></returns>
        [HttpGet("GenerateRequestAnalyticsReport")]
        [ProducesResponseType(typeof(Stream), 200)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<ObjectResponse<StaffRootResponsePayload>>> GenerateRequestAnalyticsReport([FromQuery] StaffDashboardAnalyticsRequest csoDashboardAnalyticsRequest)
        {
            const string file = $"{ReportConstants.LegalSearchReport}.xlsx";
            var response = await _legalSearchRequestService.GenerateRequestAnalyticsReportForStaff(csoDashboardAnalyticsRequest);

            if (response.Code is not ResponseCodes.Success)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            return File(response.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file);
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
        [AllowAnonymous]
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
        /// <param name="solicitorId">The solicitor identifier.</param>
        /// <param name="actionType">Type of the action.</param>
        /// <returns></returns>
        [HttpGet("ActivateOrDeactivateSolicitor/{solicitorId}/{actionType}")]
        public async Task<ActionResult<StatusResponse>> ActivateOrDeactivateSolicitor(string solicitorId, ProfileStatusActionType actionType)
        {
            var request = new ActivateOrDeactivateSolicitorRequest
            {
                SolicitorId = Guid.Parse(solicitorId),
                ActionType = actionType
            };
            var response = await _solicitorService.ActivateOrDeactivateSolicitor(request);
            return HandleResponse(response);
        }
    }
}
