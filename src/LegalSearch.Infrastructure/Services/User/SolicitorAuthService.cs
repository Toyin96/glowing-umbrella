using Fcmb.Shared.Models.Responses;
using Fcmb.Shared.Utilities;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;
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
        private readonly IJwtTokenService _jwtTokenHelper;
        private readonly IStateRetrieveService _stateRetrieveService;

        public SolicitorAuthService(UserManager<Domain.Entities.User.User> userManager, 
            RoleManager<Role> roleManager, IJwtTokenService jwtTokenHelper,
            IStateRetrieveService stateRetrieveService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtTokenHelper = jwtTokenHelper;
            _stateRetrieveService = stateRetrieveService;
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

        public ClaimsIdentity GetClaimsIdentity(Domain.Entities.User.User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FirstName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()) // Add the user's role as a claim
            };

            var identity = new ClaimsIdentity(claims, "JWT");

            return identity;
        }

        public async Task<IList<string>> GetRolesForUserAsync(Domain.Entities.User.User user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<Solicitor> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email) as Solicitor;
        }

        public async Task<ObjectResponse<SolicitorOnboardResponse>> OnboardSolicitorAsync(SolicitorOnboardRequest request)
        {
            var existingSolicitor = await _userManager.FindByEmailAsync(request.Email);

            if (existingSolicitor != null)
            {
                // Solicitor with the given email already exists
                return new ObjectResponse<SolicitorOnboardResponse>("Solicitor with the given email already exists", ResponseCodes.ServiceError);
            }

            // get state
            var state = await _stateRetrieveService.GetStateById(request.StateId);

            if (state == null)
                return new ObjectResponse<SolicitorOnboardResponse>("State ", ResponseCodes.ServiceError);

            var defaultPassword = Helpers.GenerateDefaultPassword();

            var newSolicitor = new Solicitor
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Firm = new Firm 
                { 
                    Name = request.Firm.Name,
                    Address = new Address
                    {
                        Street = request.Firm.Address.Street,
                        State = state,
                        StateId = state.Id
                    }
                },
                Address = new Address 
                { 
                    Street = request.Address.Street,
                    State = state,
                    StateId = state.Id
                },
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                BankAccount = request.BankAccount,
                // Set other properties as needed during onboarding
            };

            var result = await _userManager.CreateAsync(newSolicitor, defaultPassword);

            if (result.Succeeded)
            {
                // Onboarding succeeded, now assign the role to the solicitor
                var roleName = "Solicitor"; // The role name for solicitors (you can customize this as needed)
                var role = await _roleManager.FindByNameAsync(roleName);

                if (role == null)
                {
                    // If the role doesn't exist, create it
                    role = new Role { Name = roleName };
                    await _roleManager.CreateAsync(role);
                }

                // Assign the role to the solicitor
                await _userManager.AddToRoleAsync(newSolicitor, roleName);

                // Onboarding and role assignment succeeded
                return new ObjectResponse<SolicitorOnboardResponse>("Solicitor onboarding and role assignment succeeded", ResponseCodes.Success)
                {
                    Data = new SolicitorOnboardResponse
                    {

                    }
                };
            }

            return new ObjectResponse<SolicitorOnboardResponse>("Solicitor onboarding failed", ResponseCodes.ServiceError);
        }

        public async Task<ObjectResponse<LoginResponse>> SolicitorLogin(SolicitorLoginRequest request)
        {
            (SignInResult signInResult, string responseCodes, string message) loginResult = await UserSignInHelper(request.Email, request.Password);

            if (loginResult.signInResult.Succeeded)
            {
                var user = await GetUserByEmailAsync(request.Email);

                var identity = GetClaimsIdentity(user);
                var jwtToken = _jwtTokenHelper.GenerateJwtToken(identity);

                // Return the JWT token to the client
                return new ObjectResponse<LoginResponse>(loginResult.message, loginResult.responseCodes)
                {
                    Data = new LoginResponse { Token = jwtToken }
                };
            }

            return new ObjectResponse<LoginResponse>(loginResult.message, loginResult.responseCodes);
        }

        private async Task<(SignInResult, string, string)> UserSignInHelper(string email, string password)
        {
            var user = await GetUserByEmailAsync(email);

            if (user == null)
            {
                // User not found
                return (SignInResult.Failed, ResponseCodes.InvalidCredentials, "Invalid Credentials");
            }

            if (user.LockoutEnabled && user.LockoutEnd >= DateTime.UtcNow)
            {
                // User is currently locked out
                // You can also check user.LockoutEnd for the date when the lockout will be lifted.
                return (SignInResult.LockedOut, ResponseCodes.LockedOutUser, "User Is Locked Out For Multiple Wrong Password Attempts");
            }

            var result = await _userManager.CheckPasswordAsync(user, password);

            if (!result)
            {
                // Invalid password
                return (SignInResult.Failed, ResponseCodes.InvalidCredentials, "Invalid Credentials");
            }

            // Login successful
            return (SignInResult.Success, ResponseCodes.Success, "Successfully authenticated user");
        }

    }
}
