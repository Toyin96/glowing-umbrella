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
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="generalAuthService">The general authentication service.</param>
        public AuthController(IGeneralAuthService<User> generalAuthService)
        {
            _solicitorAuthService = generalAuthService;
        }

        /// <summary>
        /// Allows a user to log in by providing valid login credentials.
        /// </summary>
        /// <param name="request">The login request containing user credentials.</param>
        /// <returns>A response indicating the login status and information.</returns>
        [HttpPost("User/login")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<ObjectResponse<LoginResponse>>> UserLogin([FromBody] LoginRequest request)
        {
            var response = await _solicitorAuthService.UserLogin(request);
            return HandleResponse(response);
        }

        /// <summary>
        /// Reissues an authentication token for an authenticated user.
        /// </summary>
        /// <remarks>
        /// Requires a valid authentication token (Bearer token) to access.
        /// </remarks>
        /// <returns>A response containing a new authentication token.</returns>
        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("User/ReIssueToken")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<ObjectResponse<ReIssueTokenResponse>>> ReIssueToken()
        {
            string? userId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.UserId))?.Value;
            var response = await _solicitorAuthService.ReIssueToken(userId!);
            return HandleResponse(response);
        }

        /// <summary>
        /// Allows a user to request an unlock code to unlock their account.
        /// </summary>
        /// <param name="request">The request containing user information.</param>
        /// <returns>A response indicating the status of the request.</returns>
        [HttpPost("User/request-unlock-code")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<StatusResponse>> RequestUnlockCode([FromBody] RequestUnlockCodeRequest request)
        {
            var response = await _solicitorAuthService.RequestUnlockCode(request);
            return HandleResponse(response);
        }

        /// <summary>
        /// Unlocks a user's account by providing a valid unlock code.
        /// </summary>
        /// <param name="request">The request containing the unlock code.</param>
        /// <returns>A response indicating the status of the unlock request.</returns>
        [HttpPost("User/UnlockAccount")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<StatusResponse>> UnlockAccount([FromBody] UnlockAccountRequest request)
        {
            var response = await _solicitorAuthService.UnlockCode(request);
            return HandleResponse(response);
        }

        /// <summary>
        /// Verifies a two-factor authentication (2FA) code provided by a user.
        /// </summary>
        /// <param name="request">The request containing the 2FA verification code.</param>
        /// <returns>A response indicating the status of the 2FA verification.</returns>
        [HttpPost("Solicitor/VerifyTwoFactor")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<ObjectResponse<LoginResponse>>> VerifyTwoFactor([FromBody] TwoFactorVerificationRequest request)
        {
            var response = await _solicitorAuthService.Verify2fa(request);
            return HandleResponse(response);
        }

        /// <summary>
        /// Resets a user's password based on the provided reset password request.
        /// </summary>
        /// <param name="request">The request containing information for password reset.</param>
        /// <returns>A response indicating the status of the password reset.</returns>
        [HttpPost("Solicitor/ResetPassword")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<StatusResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var response = await _solicitorAuthService.ResetPassword(request);
            return HandleResponse(response);
        }

        /// <summary>
        /// Onboards a new user into the system. Requires admin authentication.
        /// </summary>
        /// <param name="request">The request containing information for onboarding a new user.</param>
        /// <returns>A response indicating the status of the onboarding process.</returns>
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
        /// Onboards a solicitor with a default password based on the details in the request payload.
        /// </summary>
        /// <remarks>
        /// This endpoint allows the Legal Search Team to onboard a solicitor with a default password. 
        /// The solicitor will receive an email to complete the onboarding process and change their default password.
        /// </remarks>
        /// <param name="request">The details for onboarding the solicitor.</param>
        /// <returns>A response indicating the outcome of the onboarding process.</returns>
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
