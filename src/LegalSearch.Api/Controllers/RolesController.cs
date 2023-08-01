using Azure;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LegalSearch.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : BaseController
    {
        private readonly IRoleService _roleService;

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
    }
}
