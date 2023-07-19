using System.Net;
using System.Threading.Tasks;
using Fcmb.Shared.Auth.Models.Requests;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Models.Responses;
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
        private readonly IAuthSetupService authSetupService;

        public AuthController(IAuthSetupService authSetupService)
        {
            this.authSetupService = authSetupService;
        }

        /// <summary>
        /// Logins in a user to the AD based on the username and password
        /// </summary>
        /// <remarks>
        /// A JWT Token is returned which must passed to the other endpoints in the Auth Header for subsequent requests
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("login")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<ObjectResponse<LoginResponse>>> Login([FromBody]LoginRequest request)
        {
            var response = await authSetupService.LoginAsync(request);
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
        [HttpPost("login-guest")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<StatusResponse>> LoginGuest([FromBody] LoginRequest request)
        {
            var response = await authSetupService.GuestLoginAsync(request);
            return HandleResponse(response);
        }
    }
}
