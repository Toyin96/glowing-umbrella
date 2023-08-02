using LegalSearch.Application.Interfaces.Auth;
using Microsoft.AspNetCore.Identity;
using LegalSearch.Domain.Entities.Role;
using LegalSearch.Application.Models.Requests;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Constants;
using Microsoft.Extensions.Logging;
using System.Data;
using LegalSearch.Application.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Fcmb.Shared.Utilities;

namespace LegalSearch.Infrastructure.Services.Roles
{
    internal class RoleService : IRoleService
    {
        private readonly RoleManager<Role> _roleManager;
        private readonly ILogger<RoleService> _logger;

        public RoleService(RoleManager<Role> roleManager, ILogger<RoleService> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }
        public async Task<ObjectResponse<RoleResponse>> CreateRoleAsync(RoleRequest roleRequest)
        {
            if (string.IsNullOrEmpty(roleRequest.RoleName))
                throw new ArgumentNullException(nameof(roleRequest.RoleName));

            var roleExists = await _roleManager.RoleExistsAsync(roleRequest.RoleName);
            if (roleExists)
                return new ObjectResponse<RoleResponse>("Role already exists", ResponseCodes.ServiceError);

            var role = new Role { Name = roleRequest.RoleName };

            // Create RolePermission entities based on the given permissions and associate them with the new role
            if (roleRequest.Permissions != null && roleRequest.Permissions.Any())
            {
                foreach (var permission in roleRequest.Permissions)
                {
                    role.Permissions.Add(new RolePermission { Permission = permission });
                }
            }

            var result = await _roleManager.CreateAsync(role);

            if (result.Succeeded)
                return new ObjectResponse<RoleResponse>("Operation was successful", ResponseCodes.Success);
            else
                return new ObjectResponse<RoleResponse>("Failed to create role", ResponseCodes.ServiceError);
        }


        public async Task<ListResponse<RoleResponse>> GetAllRolesAsync(FilterRoleRequest request)
        {
            var roleQuery = _roleManager.Roles.Select(x => new RoleResponse
            {
                RoleId = x.Id,
                Permissions = x.Permissions.Select(p => p.Permission).ToList(), // Extract the permissions from RolePermission entities
                RoleName = x.Name
            });

            roleQuery = FilterRoleQuery(roleQuery, request).Paginate(request);

            var response = await roleQuery.ToListAsync();

            return new ListResponse<RoleResponse>("Successfully Retrieved Roles")
            {
                Data = response,
                Total = response.Count
            };
        }


        public async Task<ObjectResponse<RoleResponse>> GetRoleByNameAsync(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);

            if (role is null)
            {
                _logger.LogInformation("Role with Name {roleName} not found", roleName);

                return new ObjectResponse<RoleResponse>("Role Not Found", ResponseCodes.DataNotFound);
            }

            return new ObjectResponse<RoleResponse>("Successfully Retrieved Role")
            {
                Data = new RoleResponse
                {
                    Permissions = role.Permissions.Select(p => p.Permission).ToList(), // Extract the permissions from RolePermission entities
                    RoleId = role.Id,
                    RoleName = role.Name
                }
            };
        }


        private IQueryable<RoleResponse> FilterRoleQuery(IQueryable<RoleResponse> roleQuery, FilterRoleRequest request)
        {
            if (!string.IsNullOrEmpty(request.RoleName))
                roleQuery = roleQuery.Where(x => x.RoleName.Contains(request.RoleName));

            return roleQuery;
        }
    }
}
