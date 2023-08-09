using Fcmb.Shared.Auth.Models.Requests;
using Fcmb.Shared.Auth.Models.Responses;
using Fcmb.Shared.Models.Responses;
using Fcmb.Shared.Utilities;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.Role;
using LegalSearch.Domain.Entities.User.CustomerServiceOfficer;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums.Role;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Security.Claims;

namespace LegalSearch.Infrastructure.Services.User
{
    public class SolicitorAuthService : ISolicitorAuthService<Solicitor>
    {
        private readonly UserManager<Domain.Entities.User.User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IJwtTokenService _jwtTokenHelper;
        private readonly IStateRetrieveService _stateRetrieveService;
        private readonly IAuthService _authService;
        private readonly ILogger<SolicitorAuthService> _logger;
        private readonly IBranchRetrieveService _branchRetrieveService;

        public SolicitorAuthService(UserManager<Domain.Entities.User.User> userManager,
            RoleManager<Role> roleManager, IJwtTokenService jwtTokenHelper,
            IStateRetrieveService stateRetrieveService, IAuthService authService,
            ILogger<SolicitorAuthService> logger, IBranchRetrieveService branchRetrieveService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtTokenHelper = jwtTokenHelper;
            _stateRetrieveService = stateRetrieveService;
            _authService = authService;
            _logger = logger;
            _branchRetrieveService = branchRetrieveService;
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

        public async Task<ObjectResponse<StaffLoginResponse>> FCMBLoginAsync(LoginRequest request, bool isCso = false)
        {
            ObjectResponse<AdLoginResponse> result = await _authService.LoginAsync(request);

            if (result.Code is ResponseCodes.Success)
            {
                _logger.LogInformation("{Username} successfully logged in", request.Email);

                var staffLoginResponse = await HandleSuccessfulLoginAsync(result.Data, request.Email, isCso);

                return staffLoginResponse;
            }

            return new ObjectResponse<StaffLoginResponse>("Staff login failed", result.Code);
        }

        private async Task<ObjectResponse<StaffLoginResponse>> HandleSuccessfulLoginAsync(AdLoginResponse adLoginResponse, string userEmail, bool isCso = false)
        {
            var shadowUser = await _userManager.FindByEmailAsync(userEmail);

            if (shadowUser == null)
            {
                var staff = MapToCustomerServiceOfficer(adLoginResponse);

                var userCreationStatus = await _userManager.CreateAsync(staff);

                if (!userCreationStatus.Succeeded)
                {
                    _logger.LogError($"Error creating shadow user for staff with email: {userEmail}");
                    return new ObjectResponse<StaffLoginResponse>("Error creating shadow user for staff", ResponseCodes.ServiceError);
                }

                var role = isCso ? nameof(RoleType.Cso) : nameof(RoleType.LegalPerfectionTeam);

                // assign role to staff
                await AssignRoleToUserAsync(staff, role);

                await UpdateUserLastLoginTime(staff);

                // create jwt token for staff
                var identity = await GetClaimsIdentity(staff);
                var jwtToken = _jwtTokenHelper.GenerateJwtToken(identity);

                return await CreateStaffLoginResponse(staff, jwtToken);
            }

            // update staff's last login date
            await UpdateUserLastLoginTime(shadowUser);

            // create jwt token for staff
            var staffIdentity = await GetClaimsIdentity(shadowUser);
            var staffJwtToken = _jwtTokenHelper.GenerateJwtToken(staffIdentity);

            return await CreateStaffLoginResponse(shadowUser, staffJwtToken);
        }

        private async Task UpdateUserLastLoginTime(Domain.Entities.User.User user)
        {
            // update staff's last login date
            user.LastLogin = DateTime.UtcNow.AddHours(1);
            await _userManager.UpdateAsync(user);
        }

        private CustomerServiceOfficer MapToCustomerServiceOfficer(AdLoginResponse adLoginResponse)
        {
            return new CustomerServiceOfficer
            {
                FirstName = adLoginResponse.StaffName,
                UserName = adLoginResponse.DisplayName,
                ManagerName = adLoginResponse.ManagerName,
                ManagerDepartment = adLoginResponse.ManagerDepartment,
                Department = adLoginResponse.Department,
                StaffId = adLoginResponse.StaffId,
                BranchId = adLoginResponse.BranchId,
                SolId = adLoginResponse.Sol,
                PhoneNumber = adLoginResponse.MobileNo
            };
        }

        private async Task AssignRoleToUserAsync(CustomerServiceOfficer staff, string roleType)
        {
            var role = await _roleManager.FindByNameAsync(roleType);

            if (role == null)
            {
                throw new ApplicationException("Staff login failed; role must be added first");
            }

            await _userManager.AddToRoleAsync(staff, roleType);
        }

        private async Task<ObjectResponse<StaffLoginResponse>> CreateStaffLoginResponse(Domain.Entities.User.User staff, string jwtToken)
        {
            var roleName = nameof(RoleType.Cso);
            var role = await _roleManager.FindByNameAsync(roleName);

            // get branch name
            var branchId = Convert.ToInt32(staff.BranchId);
            var branch = await _branchRetrieveService.GetBranchById(branchId);

            return new ObjectResponse<StaffLoginResponse>("Successfully Logged In Staff")
            {
                Data = new StaffLoginResponse
                {
                    Token = jwtToken,
                    Role = roleName,
                    DisplayName = staff.FirstName,
                    LastLoginDate = staff.LastLogin.HasValue ? staff.LastLogin.Value : null,
                    Branch = branch?.Address ?? staff.BranchId,
                    Permissions = role.Permissions.Select(x => x.Permission).ToList(),
                    SolId = staff.SolId
                }
            };
        }

        public async Task<ClaimsIdentity> GetClaimsIdentity(Domain.Entities.User.User user)
        {
            var roles = await GetRolesForUserAsync(user);

            var claims = new List<Claim>();

            claims.Add(new Claim("UserId", user.Id.ToString()));
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
            var state = await _stateRetrieveService.GetStateById(request.Firm.StateId);

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
                    Street = request.Firm.Street,
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

                // update last login
                await UpdateUserLastLoginTime(newSolicitor);

                // Onboarding and role assignment succeeded
                return new ObjectResponse<SolicitorOnboardResponse>("Solicitor onboarding and role assignment succeeded", ResponseCodes.Success)
                {
                    Data = new SolicitorOnboardResponse
                    {
                        SolicitorId = newSolicitor.Id,
                        FirstName = newSolicitor.FirstName,
                        LastName = newSolicitor.LastName,
                        Email = newSolicitor.Email,
                        PhoneNumber = newSolicitor.PhoneNumber,
                        AccountNumber = newSolicitor.BankAccount,
                        Firm = newSolicitor.Firm.Name,
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
                return (SignInResult.Failed, ResponseCodes.InvalidCredentials, "The email is invalid, please try again!");
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
                return (SignInResult.Failed, ResponseCodes.InvalidCredentials, "The password is invalid, please try again!");
            }

            // update user's last login
            await UpdateUserLastLoginTime(user);

            // Login successful
            return (SignInResult.Success, ResponseCodes.Success, "Successfully authenticated user");
        }

    }
}
