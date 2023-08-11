using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.User.Solicitor;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LegalSearch.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SolicitorsController : ControllerBase
    {
        private readonly IGeneralAuthService<Solicitor> _generalAuthService;

        public SolicitorsController(IGeneralAuthService<Solicitor> generalAuthService)
        {
            _generalAuthService = generalAuthService;
        }

        /// <summary>
        /// This endpoint onboards a solicitor with a default password, based on the details in the payload
        /// </summary>
        /// <remarks>
        /// The solicitor receives an email to complete the onboarding process and to change their default password
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Solicitor/AcceptRequest")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ObjectResponse<SolicitorOnboardResponse>>> AcceptRequest([FromBody] SolicitorOnboardRequest request)
        {
            //var response = await _solicitorAuthService.OnboardSolicitorAsync(request);
            //return HandleResponse(response);
            return null;
        }

        /// <summary>
        /// This endpoint onboards a solicitor with a default password, based on the details in the payload
        /// </summary>
        /// <remarks>
        /// The solicitor receives an email to complete the onboarding process and to change their default password
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Solicitor/RejectRequest")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ObjectResponse<SolicitorOnboardResponse>>> OnboardSolicitor([FromBody] SolicitorOnboardRequest request)
        {
            //var response = await _solicitorAuthService.OnboardSolicitorAsync(request);
            //return HandleResponse(response);
            return null;
        }
    }
}
