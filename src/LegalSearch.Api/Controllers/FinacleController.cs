using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LegalSearch.Api.Controllers
{
    [ValidateAntiForgeryToken]
    [ApiController]
    [Route("api/[controller]")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class FinacleController : BaseController
    {
        private readonly ILegalSearchRequestService _legalSearchRequestService;

        public FinacleController(ILegalSearchRequestService legalSearchRequestService)
        {
            _legalSearchRequestService = legalSearchRequestService;
        }

        /// <summary>
        /// This endpoint is consumed by Finacle to create a new Legal Search Request with the barest details
        /// </summary>
        /// <param name="FinacleLegalSearchRequest"></param>
        /// <returns></returns>
        [HttpPost("CreateLegalSearchRequest")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> CreateLegalRequest(FinacleLegalSearchRequest FinacleLegalSearchRequest)
        {
            var result = await _legalSearchRequestService.CreateNewRequestFromFinacle(FinacleLegalSearchRequest);
            return HandleResponse(result);
        }
    }
}
