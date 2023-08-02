using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;

namespace LegalSearch.Application.Interfaces.Auth
{
    public interface IRoleService
    {
        Task<ObjectResponse<RoleResponse>> CreateRoleAsync(RoleRequest roleRequest);
        Task<ListResponse<RoleResponse>> GetAllRolesAsync(FilterRoleRequest request);
        Task<ObjectResponse<RoleResponse>> GetRoleByNameAsync(string roleName);
    }
}
