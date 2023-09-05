using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Requests.CSO;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Application.Models.Responses.CSO;
using LegalSearch.Domain.Enums;
using LegalSearch.Domain.Enums.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LegalSearch.Api.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = nameof(RoleType.Cso))]
    [Route("api/[controller]")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class CustomerServiceOfficersController : BaseController
    {
        private readonly ILegalSearchRequestService _legalSearchRequestService;

        public CustomerServiceOfficersController(ILegalSearchRequestService legalSearchRequestService)
        {
            _legalSearchRequestService = legalSearchRequestService;
        }

        [HttpPost("AddNewRequest")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> CreateNewRequest([FromForm] LegalSearchRequest request)
        {
            //get userId 
            string? userId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.UserId))?.Value;

            var result = await _legalSearchRequestService.CreateNewRequest(request, userId);
            return HandleResponse(result);
        }

        [HttpPost("UpdateRequest")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> UpdateRequest([FromForm] UpdateRequest request)
        {
            var result = await _legalSearchRequestService.UpdateRequestByCso(request);
            return HandleResponse(result);
        }

        [HttpPost("UpdateFinacleRequest")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> UpdateFinacleRequest([FromForm] UpdateFinacleLegalRequest request)
        {
            //get userId 
            string? userId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.UserId))?.Value;

            var result = await _legalSearchRequestService.UpdateFinacleRequestByCso(request, userId);
            return HandleResponse(result);
        }

        /// <summary>
        /// This endpoint performs a name inquiry on a FCMB account number
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <returns>
        /// It returns the name associated with the account, the account status as well as the account balance
        /// </returns>
        [HttpGet("PerformNameInquiryOnAccount/{accountNumber}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ObjectResponse<GetAccountInquiryResponse>>> PerformNameInquiryOnAccount(string accountNumber)
        {
            var result = await _legalSearchRequestService.PerformNameInquiryOnAccount(accountNumber);
            return HandleResponse(result);
        }

        /// <summary>
        /// This endpoint gets legal search requests created from Finacle under CSO branch
        /// </summary>
        /// <param name="request"></param>
        /// <returns>
        /// It returns legal search requests created from Finacle under CSO branch
        /// </returns>
        [HttpGet("GetFinacleRequests")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ListResponse<FinacleLegalSearchResponsePayload>>> GetFinacleRequests([FromQuery] GetFinacleRequest request)
        {
            string? solId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.SolId))?.Value;
            var result = await _legalSearchRequestService.GetFinacleLegalRequestsForCso(request, solId!);
            return HandleResponse(result);
        }
    }
}
