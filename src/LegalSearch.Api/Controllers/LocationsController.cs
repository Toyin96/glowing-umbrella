using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Application.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LegalSearch.Api.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    //[Authorize(AuthenticationSchemes = "Bearer")]
    [Route("api/[controller]")]
    //[Consumes("application/json")]
    //[Produces("application/json")]
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
        /// Retrieves all states in Nigeria
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetStates")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ListResponse<StateResponse>>> GetStatesAsync()
        {
            var response = await _stateRetrieveService.GetStatesAsync();
            return HandleResponse(response);
        }

        /// <summary>
        /// Retrieves the all the regions
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetRegions")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ListResponse<RegionResponse>>> GetRegionsAsync()
        {
            var response = await _stateRetrieveService.GetRegionsAsync();
            return HandleResponse(response);
        }

        /// <summary>
        /// Retrieves the states under a region
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetStatesUnderRegion/{regionId}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ListResponse<StateResponse>>> GetStatesUnderRegionAsync(Guid regionId)
        {
            var response = await _stateRetrieveService.GetStatesUnderRegionAsync(regionId);
            return HandleResponse(response);
        }
    }
}
