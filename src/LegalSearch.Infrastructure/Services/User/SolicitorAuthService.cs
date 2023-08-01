using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Domain.Entities.Role;
using LegalSearch.Domain.Entities.User.Solicitor;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace LegalSearch.Infrastructure.Services.User
{
    public class SolicitorAuthService : ISolicitorAuthService<Solicitor>
    {
        private readonly UserManager<Domain.Entities.User.User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public SolicitorAuthService(UserManager<Domain.Entities.User.User> userManager, RoleManager<Role> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public async Task<bool> AddClaimsAsync(string email, IEnumerable<Claim> claims)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return false;

            var result = await _userManager.AddClaimsAsync(user, claims);
            return result.Succeeded;
        }

        public async Task<bool> AssignRoleAsync(Solicitor user, string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
                return false;

            var result = await _userManager.AddToRoleAsync(user, role.Name);
            return result.Succeeded;
        }

        public async Task<bool> CreateUserAsync(string email, string password)
        {
            var user = Activator.CreateInstance<Solicitor>();
            user.UserName = email;
            user.Email = email;

            var result = await _userManager.CreateAsync(user, password);
            return result.Succeeded;
        }

        public async Task<IList<string>> GetRolesAsync(Solicitor user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<Solicitor> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email) as Solicitor;
        }
    }
}
