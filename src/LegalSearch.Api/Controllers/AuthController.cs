using Fcmb.Shared.Auth.Models.Requests;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Domain.Entities.User.Solicitor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LegalSearch.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class AuthController : BaseController
    {
        private readonly IGeneralAuthService<Solicitor> _solicitorAuthService;
        private readonly IUserAuthService<User> _userAuthService;

        public AuthController(IGeneralAuthService<Solicitor> solicitorAuthService,
            IUserAuthService<User> userAuthService)
        {
            _solicitorAuthService = solicitorAuthService;
            _userAuthService = userAuthService;
        }

        [HttpPost("Solicitor/login")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<ObjectResponse<LoginResponse>>> SolicitorLogin([FromBody] LoginRequest request)
        {
            var response = await _solicitorAuthService.SolicitorLogin(request);
            return HandleResponse(response);
        }


        [HttpPost("CSO/login")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<ObjectResponse<StaffLoginResponse>>> CSOLogin([FromBody] LoginRequest request)
        {
            var response = await _userAuthService.FCMBLoginAsync(request, true);
            return HandleResponse(response);
        }


        [HttpPost("LegalPerfectionTeam/login")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<ObjectResponse<StaffLoginResponse>>> LegalPerfectionTeamLogin([FromBody] LoginRequest request)
        {
            var response = await _userAuthService.FCMBLoginAsync(request);
            return HandleResponse(response);
        }
    }
}
