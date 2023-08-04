using Fcmb.Shared.Auth.Models.Requests;
using Fcmb.Shared.Auth.Models.Responses;
using Fcmb.Shared.Auth.Services;
using Fcmb.Shared.Models.Responses;
using Fcmb.Shared.Utilities;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Application.Models.Auth;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.Role;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Domain.Entities.User.CustomerServiceOfficer;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums.Role;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace LegalSearch.Infrastructure.Services.User
{
    public class SolicitorAuthService : ISolicitorAuthService<Solicitor>
    {
        private readonly UserManager<Domain.Entities.User.User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IJwtTokenService _jwtTokenHelper;
        private readonly IStateRetrieveService _stateRetrieveService;
        private readonly Application.Interfaces.Auth.IAuthService _authService;
        private readonly ILogger<SolicitorAuthService> _logger;

        public SolicitorAuthService(UserManager<Domain.Entities.User.User> userManager, 
            RoleManager<Role> roleManager, IJwtTokenService jwtTokenHelper,
            IStateRetrieveService stateRetrieveService, Application.Interfaces.Auth.IAuthService authService,
            ILogger<SolicitorAuthService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtTokenHelper = jwtTokenHelper;
            _stateRetrieveService = stateRetrieveService;
            _authService = authService;
            _logger = logger;
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

        public async Task<ObjectResponse<StaffLoginResponse>> FCMBLoginAsync(LoginRequest request)
        {
            ObjectResponse<AdLoginResponse> result = await _authService.LoginAsync(request);

            if (result.Code is ResponseCodes.Success)
            {
                _logger.LogInformation("{Username} successfully logged in", request.Email);

                // create shadow user for staff if not already created
                var shadowUser = await _userManager.FindByEmailAsync(request.Email);

                if (shadowUser == null)
                {
                    // Create shadow (i.e CSO) user account

                    CustomerServiceOfficer staff = new CustomerServiceOfficer
                    {
                        FirstName = result.Data.StaffName,
                       // LastName = result.Data.StaffName,
                        ManagerName = result.Data.ManagerName,
                        ManagerDepartment = result.Data.ManagerDepartment,
                        Department = result.Data.Department,
                        StaffId = result.Data.StaffId,
                        BranchId = result.Data.BranchId,
                        Sol = result.Data.Sol,
                        PhoneNumber = result.Data.MobileNo
                    };

                    var userCreationStatus = await _userManager.CreateAsync(staff);

                    if (!userCreationStatus.Succeeded)
                    {
                        _logger.LogError($"Error creating shadow user for staff with email: {request.Email}");
                        return new ObjectResponse<StaffLoginResponse>("Error creating shadow user for staff", ResponseCodes.ServiceError);
                    }

                    // Assign roles (if not already assigned)
                    var roleName = nameof(RoleType.Cso);
                    var role = await _roleManager.FindByNameAsync(roleName);

                    if (role == null)
                        return new ObjectResponse<StaffLoginResponse>("Staff login failed; role must be added first", ResponseCodes.ServiceError);

                    // Assign the role to the CSO
                    await _userManager.AddToRoleAsync(staff, roleName);

                    var identity = await GetClaimsIdentity(staff);
                    var jwtToken = _jwtTokenHelper.GenerateJwtToken(identity);

                    return new ObjectResponse<StaffLoginResponse>("Successfully Logged In Staff")
                    {
                        Data = new StaffLoginResponse
                        {
                            Token = jwtToken,
                            Role = 
                        }
                    };


                }

                return new ObjectResponse<StaffLoginResponse>("Successfully Logged In Staff")
                {
                    Data = new StaffLoginResponse
                    {
                        Token = token
                    }
                };
            }
        }

        public async Task<ClaimsIdentity> GetClaimsIdentity(Domain.Entities.User.User user)
        {
            var roles = await GetRolesForUserAsync(user);

            var claims = new List<Claim>();

            claims.Add(new Claim(ClaimTypes.Role, roles.First()));
            claims.Add(new Claim(ClaimTypes.Name, user.FirstName));
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

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
                return new ObjectResponse<SolicitorOnboardResponse>("State Id is not valid", ResponseCodes.ServiceError);

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
                UserName = request.Email,
                PhoneNumber = request.PhoneNumber,
                BankAccount = request.BankAccount,
            };

            var result = await _userManager.CreateAsync(newSolicitor, defaultPassword);

            if (result.Succeeded)
            {
                // Onboarding succeeded, now assign the role to the solicitor
                var roleName = nameof(RoleType.Solicitor); 
                var role = await _roleManager.FindByNameAsync(roleName);

                if (role == null)
                    return new ObjectResponse<SolicitorOnboardResponse>("Solicitor onboarding failed; role must be added first", ResponseCodes.ServiceError);

                // Assign the role to the solicitor
                await _userManager.AddToRoleAsync(newSolicitor, roleName);

                // Onboarding and role assignment succeeded
                return new ObjectResponse<SolicitorOnboardResponse>("Solicitor onboarding and role assignment succeeded", ResponseCodes.Success)
                {
                    Data = new SolicitorOnboardResponse
                    {
                        SolicitorId = newSolicitor.Id,
                        FirstName = newSolicitor.FirstName,
                        LastName = newSolicitor.LastName,
                        Email = newSolicitor.Email,
                        PhoneNumber=newSolicitor.PhoneNumber,
                        AccountNumber = newSolicitor.BankAccount,
                        State = newSolicitor.Address.State.Name,
                        Firm = newSolicitor.Firm.Name,
                        Address = newSolicitor.Address.Street
                    }
                };
            }

            return new ObjectResponse<SolicitorOnboardResponse>("Solicitor onboarding failed", ResponseCodes.ServiceError);
        }

        public async Task<ObjectResponse<LoginResponse>> SolicitorLogin(LoginRequest request)
        {
            (SignInResult signInResult, string responseCodes, string message) loginResult = await UserSignInHelper(request.Email, request.Password);

            if (loginResult.signInResult.Succeeded)
            {
                var user = await GetUserByEmailAsync(request.Email);

                var identity = await GetClaimsIdentity(user);
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
