using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.User.Solicitor;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LegalSearch.Api.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class UsersController : BaseController
    {
        private readonly ISolicitorAuthService<Solicitor> _solicitorAuthService;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="solicitorAuthService"></param>
        public UsersController(ISolicitorAuthService<Solicitor> solicitorAuthService)
        {
            _solicitorAuthService = solicitorAuthService;
        }

        /// <summary>
        /// This endpoint onboards a solicitor with a default password, based on the details in the payload
        /// </summary>
        /// <remarks>
        /// The solicitor receives an email to complete the onboarding process and to change their default password
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("solicitor/onboard")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ObjectResponse<SolicitorOnboardResponse>>> OnboardSolicitor([FromBody] SolicitorOnboardRequest request)
        {
            var response = await _solicitorAuthService.OnboardSolicitorAsync(request);
            return HandleResponse(response);
        }
    }
}
