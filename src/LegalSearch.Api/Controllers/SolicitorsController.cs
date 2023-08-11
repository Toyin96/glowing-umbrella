using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Domain.Entities.User.Solicitor;
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
        private readonly IGeneralAuthService<Solicitor> _generalAuthService;
        private readonly ILegalSearchRequestService _legalSearchRequestService;

        /// <summary>
        /// Constructs the service needed upon instantiation of the SolicitorsController class.
        /// </summary>
        /// <param name="generalAuthService"></param>
        /// <param name="legalSearchRequestService"></param>
        public SolicitorsController(IGeneralAuthService<Solicitor> generalAuthService,
            ILegalSearchRequestService legalSearchRequestService)
        {
            _generalAuthService = generalAuthService;
            _legalSearchRequestService = legalSearchRequestService;
        }

        /// <summary>
        /// This endpoint allows a solicitor accepts a legal search request that has been assigned to him/her
        /// </summary>
        /// <remarks>
        /// The solicitor receives a message notifying the solicitor if the request was succcessful or not
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Solicitor/AcceptRequest")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> AcceptRequest([FromBody] Application.Models.Requests.Solicitor.AcceptRequest request)
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.UserId))?.Value;
            request.SolicitorId = Guid.Parse(userId);
            var response = await _legalSearchRequestService.AcceptLegalSearchRequest(request);
            return HandleResponse(response);
        }

        /// <summary>
        /// This endpoint allows a solicitor rejects a legal search request that has been assigned to him/her
        /// </summary>
        /// <remarks>
        /// The solicitor receives a message notifying the solicitor if the request was succcessful or not
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Solicitor/RejectRequest")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> RejectRequest([FromBody] Application.Models.Requests.Solicitor.RejectRequest request)
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.UserId))?.Value;
            request.SolicitorId = Guid.Parse(userId);
            var response = await _legalSearchRequestService.RejectLegalSearchRequest(request);
            return HandleResponse(response);
        }
    }
}
