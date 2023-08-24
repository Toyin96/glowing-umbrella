using Fcmb.Shared.Auth.Models.Requests;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.User.Solicitor;
using System.Security.Claims;

namespace LegalSearch.Application.Interfaces.Auth
{
    public interface IUserAuthService<TUser> where TUser : Domain.Entities.User.User
    {
        Task<bool> AssignRoleAsync(Domain.Entities.User.User user, string roleName);
        Task<bool> AddClaimsAsync(string email, IEnumerable<Claim> claims);
        Task<Domain.Entities.User.User> GetUserByEmailAsync(string email);
        Task<ClaimsIdentity> GetClaimsIdentityForUser(Domain.Entities.User.User user);
        Task<IList<string>> GetRolesForUserAsync(Domain.Entities.User.User user);
    }
}
