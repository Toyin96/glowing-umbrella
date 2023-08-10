using Fcmb.Shared.Auth.Models.Requests;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.User.Solicitor;
using System.Security.Claims;

namespace LegalSearch.Application.Interfaces.Auth
{
    public interface IUserAuthService<TUser> where TUser : Domain.Entities.User.User
    {
        Task<bool> AssignRoleAsync(Solicitor user, string roleName);
        Task<bool> AddClaimsAsync(string email, IEnumerable<Claim> claims);
        Task<Solicitor> GetUserByEmailAsync(string email);
        Task<ClaimsIdentity> GetClaimsIdentityForSolicitor(Domain.Entities.User.User user);
        Task<IList<string>> GetRolesForUserAsync(Domain.Entities.User.User user);
        Task<ObjectResponse<StaffLoginResponse>> FCMBLoginAsync(LoginRequest model, bool isCso = false);
    }
}
