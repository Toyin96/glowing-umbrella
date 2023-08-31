using Fcmb.Shared.Auth.Models.Requests;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Requests.User;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Domain.Enums;
using LegalSearch.Domain.Enums.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LegalSearch.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class AuthController : BaseController
    {
        private readonly IGeneralAuthService<User> _solicitorAuthService;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="generalAuthService"></param>
        /// <param name="userAuthService"></param>
        public AuthController(IGeneralAuthService<User> generalAuthService)
        {
            _solicitorAuthService = generalAuthService;
        }

        [HttpPost("User/login")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<ObjectResponse<LoginResponse>>> UserLogin([FromBody] LoginRequest request)
        {
            var response = await _solicitorAuthService.UserLogin(request);
            return HandleResponse(response);
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("User/ReIssueToken")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<ObjectResponse<ReIssueTokenResponse>>> ReIssueToken()
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.UserId))?.Value;
            var response = await _solicitorAuthService.ReIssueToken(userId);
            return HandleResponse(response);
        }

        [HttpPost("User/request-unlock-code")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<StatusResponse>> RequestUnlockCode([FromBody] RequestUnlockCodeRequest request)
        {
            var response = await _solicitorAuthService.RequestUnlockCode(request);
            return HandleResponse(response);
        }

        [HttpPost("User/UnlockAccount")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<StatusResponse>> UnlockAccount([FromBody] UnlockAccountRequest request)
        {
            var response = await _solicitorAuthService.UnlockCode(request);
            return HandleResponse(response);
        }

        [HttpPost("Solicitor/VerifyTwoFactor")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<ObjectResponse<LoginResponse>>> VerifyTwoFactor([FromBody] TwoFactorVerificationRequest request)
        {
            var response = await _solicitorAuthService.Verify2fa(request);
            return HandleResponse(response);
        }

        [HttpPost("Solicitor/ResetPassword")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<StatusResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var response = await _solicitorAuthService.ResetPassword(request);
            return HandleResponse(response);
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = nameof(RoleType.Admin))]
        [HttpPost("Admin/OnboardNewUser")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<StatusResponse>> OnboardNewUser([FromBody] OnboardNewUserRequest request)
        {
            var response = await _solicitorAuthService.OnboardNewUser(request);
            return HandleResponse(response);
        }

        /// <summary>
        /// This endpoint onboards a solicitor with a default password, based on the details in the payload
        /// </summary>
        /// <remarks>
        /// The solicitor receives an email to complete the onboarding process and to change their default password
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize(AuthenticationSchemes = "Bearer", Roles = nameof(RoleType.LegalPerfectionTeam))]
        [HttpPost("LegalSearchTeam/OnboardSolicitor")]
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
