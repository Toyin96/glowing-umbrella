using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LegalSearch.Api.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [Route("api/[controller]")]
    [ValidateAntiForgeryToken]
    [ApiController]
    public class RolesController : BaseController
    {
        private readonly IRoleService _roleService;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roleService"></param>
        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpPost("[action]")]
        public async Task<ActionResult<ObjectResponse<RoleResponse>>> AddRole([FromBody] RoleRequest roleRequest)
        {
            var response = await _roleService.CreateRoleAsync(roleRequest);
            return HandleResponse(response);
        }

        /// <summary>
        /// Retrieves Roles based on the filter request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
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
