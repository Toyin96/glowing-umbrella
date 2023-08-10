using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LegalSearch.Api.Controllers
{
    /// <summary>
    /// 
    /// </summary>    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class CustomerServiceOfficersController : BaseController
    {
        private readonly ILegalSearchRequestService _legalSearchRequestService;

        /// <summary>
        /// 
        /// </summary>
        public CustomerServiceOfficersController(ILegalSearchRequestService legalSearchRequestService)
        {
            _legalSearchRequestService = legalSearchRequestService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpPost("AddNewRequest")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ObjectResponse<string>>> CreateNewRequest([FromForm] LegalSearchRequest request)
        {
            //get userId 
            string? userId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.UserId))?.Value;

            var result = await _legalSearchRequestService.CreateNewRequest(request, userId);
            return HandleResponse(result);
        }
    }
}
