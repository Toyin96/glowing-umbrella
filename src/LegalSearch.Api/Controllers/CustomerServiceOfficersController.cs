using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Models.Constants;
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
    /// <summary>
    /// Controller for managing legal search requests by Customer Service Officers (CSOs).
    /// </summary>
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = nameof(RoleType.Cso))]
    [Route("api/[controller]")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class CustomerServiceOfficersController : BaseController
    {
        private readonly ILegalSearchRequestService _legalSearchRequestService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomerServiceOfficersController"/> class.
        /// </summary>
        /// <param name="legalSearchRequestService">The legal search request service.</param>
        public CustomerServiceOfficersController(ILegalSearchRequestService legalSearchRequestService)
        {
            _legalSearchRequestService = legalSearchRequestService;
        }

        /// <summary>
        /// Creates a new legal search request.
        /// </summary>
        /// <param name="request">The legal search request details.</param>
        /// <returns>A response indicating the status of the request creation.</returns>
        [HttpPost("AddNewRequest")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> CreateNewRequest([FromForm] LegalSearchRequest request)
        {
            // Get userId 
            string? userId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.UserId))?.Value;

            var result = await _legalSearchRequestService.CreateNewRequest(request, userId);
            return HandleResponse(result);
        }

        /// <summary>
        /// Updates a legal search request in Finacle by CSO.
        /// </summary>
        /// <param name="request">The update request.</param>
        /// <returns>A response indicating the status of the update.</returns>
        [HttpPost("UpdateFinacleRequest")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> UpdateFinacleRequest([FromForm] UpdateFinacleLegalRequest request)
        {
            // Get userId 
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

        /// <summary>
        /// Endpoint to get legal search request analytics for CSO.
        /// </summary>
        /// <param name="csoDashboardAnalyticsRequest">The request for CSO dashboard analytics.</param>
        /// <returns>A response containing legal search request analytics for CSO.</returns>
        [HttpGet("ViewRequestAnalytics")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ObjectResponse<StaffRootResponsePayload>>> ViewRequestAnalytics([FromQuery] StaffDashboardAnalyticsRequest csoDashboardAnalyticsRequest)
        {
            var result = await _legalSearchRequestService.GetLegalRequestsForStaff(csoDashboardAnalyticsRequest);
            return HandleResponse(result);
        }
    }
}
