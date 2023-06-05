using System.Net;
using System.Threading.Tasks;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace LegalSearch.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Consumes("application/json")] 
    [Produces("application/json")] 
    public class UsersController : BaseController
    {
        private readonly IUserSetupService userSetupService;
        
        // GET
        public UsersController(IUserSetupService userSetupService)
        {
            this.userSetupService = userSetupService;
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
        public async Task<ActionResult<ObjectResponse<SolicitorOnboardResponse>>> OnboardSolicitor([FromBody]SolicitorOnboardRequest request)
        {
            var response = await userSetupService.OnboardSolicitorAsync(request);
            return HandleResponse(response);
        }
    }
}
