using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Application.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LegalSearch.Api.Controllers
{
    [Route("api/[controller]")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ApiController]
    public class LocationsController : BaseController
    {
        private readonly IStateRetrieveService _stateRetrieveService;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateRetrieveService"></param>
        public LocationsController(IStateRetrieveService stateRetrieveService)
        {
            _stateRetrieveService = stateRetrieveService;
        }

        /// <summary>
        /// Retrieves the states in the database
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetStates")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ListResponse<StateResponse>>> GetStates()
        {
            var response = await _stateRetrieveService.GetStatesAsync();
            return HandleResponse(response);
        }

        /// <summary>
        /// Retrieves the regions in a state
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetRegionsUnderState/{stateId}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ListResponse<LgaResponse>>> GetRegionsUnderState(Guid stateId)
        {
            var response = await _stateRetrieveService.GetRegionsAsync(stateId);
            return HandleResponse(response);
        }
    }
}
