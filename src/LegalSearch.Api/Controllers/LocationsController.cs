using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Application.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LegalSearch.Api.Controllers
{
    /// <summary>
    /// API controller for managing locations such as states and regions.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LocationsController : BaseController
    {
        private readonly IStateRetrieveService _stateRetrieveService;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationsController"/> class.
        /// </summary>
        /// <param name="stateRetrieveService">The service for retrieving location information.</param>
        public LocationsController(IStateRetrieveService stateRetrieveService)
        {
            _stateRetrieveService = stateRetrieveService;
        }

        /// <summary>
        /// Retrieves all states in Nigeria.
        /// </summary>
        /// <returns>A list of states in Nigeria.</returns>
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
        /// Retrieves all the regions.
        /// </summary>
        /// <returns>A list of regions.</returns>
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
        /// Retrieves the states under a region based on the provided region ID.
        /// </summary>
        /// <param name="regionId">The unique identifier of the region.</param>
        /// <returns>A list of states under the specified region.</returns>
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
