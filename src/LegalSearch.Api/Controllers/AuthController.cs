using System.Net;
using Fcmb.Shared.Auth.Models.Requests;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.User.Solicitor;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegalSearch.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    [Consumes("application/json")] 
    [Produces("application/json")] 
    public class AuthController : BaseController
    {
        private readonly ISolicitorAuthService<Solicitor> _solicitorAuthService;

        public AuthController(ISolicitorAuthService<Solicitor> solicitorAuthService)
        {
            _solicitorAuthService = solicitorAuthService;
        }

        /// <summary>
        /// Endpoint to sign-in a solicitor based on the username and password
        /// </summary>
        /// <remarks>
        /// A JWT Token is returned which must passed to the other endpoints in the Auth Header for subsequent requests
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Solicitor/login")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<ObjectResponse<LoginResponse>>> SolicitorLogin([FromBody] LoginRequest request)
        {
            var response = await _solicitorAuthService.SolicitorLogin(request);
            return HandleResponse(response);
        }

        /// <summary>
        /// Initiates a login by a guest. A 2FA token is sent to the supplied email if the credentials are correct
        /// </summary>
        /// <param name="request"></param>
        /// <remarks>
        /// The user will get a mail containing the 2FA token which will be passed to the login-guest-2fa API for verification
        /// </remarks>
        /// <returns></returns>
        [HttpPost("CSO/login")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<StatusResponse>> LoginGuest([FromBody] LoginRequest request)
        {
            var response = await _authenticationService.FCMBLoginAsync(request);
            return HandleResponse(response);
        }
    }
}
