using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LegalSearch.Api.Controllers
{
    /// <summary>
    /// Controller for managing legal search requests from Finacle.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class FinacleController : BaseController
    {
        private readonly ILegalSearchRequestService _legalSearchRequestService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FinacleController"/> class.
        /// </summary>
        /// <param name="legalSearchRequestService">The legal search request service.</param>
        public FinacleController(ILegalSearchRequestService legalSearchRequestService)
        {
            _legalSearchRequestService = legalSearchRequestService;
        }

        /// <summary>
        /// Creates a new Legal Search Request from Finacle with the provided details.
        /// </summary>
        /// <param name="finacleLegalSearchRequest">The Finacle Legal Search Request details.</param>
        /// <returns>A response indicating the status of the request creation.</returns>
        [HttpPost("CreateLegalSearchRequest")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> CreateLegalRequest(FinacleLegalSearchRequest finacleLegalSearchRequest)
        {
            var result = await _legalSearchRequestService.CreateNewRequestFromFinacle(finacleLegalSearchRequest);
            return HandleResponse(result);
        }
    }
}
