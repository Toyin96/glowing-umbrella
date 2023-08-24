using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Application.Models.Responses.Solicitor;
using LegalSearch.Domain.Enums;
using LegalSearch.Domain.Enums.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LegalSearch.Api.Controllers
{
    /// <summary>
    /// This is the solicitor controller that hoses all solicitor related actions.
    /// </summary>
    [Authorize(AuthenticationSchemes = "Bearer", Roles = nameof(RoleType.Solicitor))]
    [Route("api/[controller]")]
    [ApiController]
    public class SolicitorsController : BaseController
    {
        private readonly ILegalSearchRequestService _legalSearchRequestService;
        private readonly ISolicitorService _solicitorService;

        /// <summary>
        /// Constructs the service needed upon instantiation of the SolicitorsController class.
        /// </summary>
        /// <param name="solicitorRetrievalService"></param>
        /// <param name="legalSearchRequestService"></param>
        public SolicitorsController(ILegalSearchRequestService legalSearchRequestService,
            ISolicitorService solicitorRetrievalService)
        {
            _legalSearchRequestService = legalSearchRequestService;
            _solicitorService = solicitorRetrievalService;
        }

        /// <summary>
        /// This endpoint allows a solicitor accepts a legal search request that has been assigned to him/her
        /// </summary>
        /// <remarks>
        /// The solicitor receives a message notifying the solicitor if the request was successful or not
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Solicitor/AcceptRequest")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> AcceptRequest([FromBody] AcceptRequest request)
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.UserId))?.Value;
            request.SolicitorId = Guid.Parse(userId!);
            var response = await _legalSearchRequestService.AcceptLegalSearchRequest(request);
            return HandleResponse(response);
        }

        /// <summary>
        /// This endpoint allows a solicitor rejects a legal search request that has been assigned to him/her
        /// </summary>
        /// <remarks>
        /// The solicitor receives a message notifying the solicitor if the request was successful or not
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Solicitor/RejectRequest")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> RejectRequest([FromBody] RejectRequest request)
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.UserId))?.Value;
            request.SolicitorId = Guid.Parse(userId);
            var response = await _legalSearchRequestService.RejectLegalSearchRequest(request);
            return HandleResponse(response);
        }

        /// <summary>
        /// This endpoint allows a solicitor push back for request to the CSO for additional information and/or clarification
        /// </summary>
        /// <remarks>
        /// The solicitor receives a message notifying the solicitor if the request was successful or not
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Solicitor/RequestAdditionalInformation")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> RequestAdditionalInformation([FromForm] ReturnRequest request)
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.UserId))?.Value;
            var response = await _legalSearchRequestService.PushBackLegalSearchRequestForMoreInfo(request, Guid.Parse(userId!));
            return HandleResponse(response);
        }

        /// <summary>
        /// This endpoint allows a solicitor submits the report of the request he/she has been assigned to
        /// </summary>
        /// <remarks>
        /// The Initiating CSO receives a notification when the solicitor has successfully handled the request.
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Solicitor/SubmitRequestReport")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> SubmitRequestReport([FromForm] SubmitLegalSearchReport request)
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.UserId))!.Value;
            var response = await _legalSearchRequestService.SubmitRequestReport(request, Guid.Parse(userId));
            return HandleResponse(response);
        }

        /// <summary>
        /// This endpoint allows a solicitor push back for request to the CSO for additional information and/or clarification
        /// </summary>
        /// <remarks>
        /// The solicitor receives a message notifying the solicitor if the request was successful or not
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("Solicitor/ViewProfile")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ObjectResponse<SolicitorProfileDto>>> ViewProfile()
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.UserId))!.Value;
            var response = await _solicitorService.ViewSolicitorProfile(Guid.Parse(userId));
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
        [HttpPost("Solicitor/EditProfile")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> EditProfile([FromBody]EditSolicitoProfileRequest request)
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.UserId))!.Value;
            var response = await _solicitorService.EditSolicitorProfile(request, Guid.Parse(userId));
            return HandleResponse(response);
        }

        /// <summary>
        /// Endpoint to get legal search request summaries for solicitor
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("Solicitor/ViewRequestAnalytics")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ObjectResponse<LegalSearchRootResponsePayload>>> ViewRequestAnalytics([FromQuery] SolicitorRequestAnalyticsPayload request)
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.UserId))!.Value;
            var response = await _legalSearchRequestService.GetLegalRequestsForSolicitor(request, Guid.Parse(userId));
            return HandleResponse(response);
        }
    }
}
