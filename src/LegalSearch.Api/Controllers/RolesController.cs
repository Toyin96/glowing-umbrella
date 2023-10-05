using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LegalSearch.Api.Controllers
{
    /// <summary>
    /// Controller for managing roles in the system.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : BaseController
    {
        private readonly IRoleService _roleService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RolesController"/> class.
        /// </summary>
        /// <param name="roleService">The role service.</param>
        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        /// <summary>
        /// Adds a new role.
        /// </summary>
        /// <param name="roleRequest">The role request containing role details.</param>
        /// <returns>An action result with the response of the role creation.</returns>
        [HttpPost("[action]")]
        public async Task<ActionResult<ObjectResponse<RoleResponse>>> AddRole([FromBody] RoleRequest roleRequest)
        {
            var response = await _roleService.CreateRoleAsync(roleRequest);
            return HandleResponse(response);
        }

        /// <summary>
        /// Retrieves roles based on the filter request.
        /// </summary>
        /// <param name="request">The filter role request.</param>
        /// <returns>An action result with the response containing a list of roles.</returns>
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<ListResponse<RoleResponse>>> GetRoles([FromQuery] FilterRoleRequest request)
        {
            var response = await _roleService.GetAllRolesAsync(request);
            return HandleResponse(response);
        }
    }
}
