using Azure.Core;
using Fcmb.Shared.Auth.Models.Requests;
using Fcmb.Shared.Auth.Models.Responses;
using Fcmb.Shared.Models.Responses;
using Fcmb.Shared.Utilities;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Requests.User;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.Role;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums;
using LegalSearch.Domain.Enums.Role;
using LegalSearch.Domain.Enums.User;
using LegalSearch.Infrastructure.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;

namespace LegalSearch.Infrastructure.Services.User
{
    public class GeneralAuthService : IGeneralAuthService<Domain.Entities.User.User>
    {
        private readonly UserManager<Domain.Entities.User.User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IJwtTokenService _jwtTokenHelper;
        private readonly IStateRetrieveService _stateRetrieveService;
        private readonly IAuthService _authService;
        private readonly ILogger<GeneralAuthService> _logger;
        private readonly IBranchRetrieveService _branchRetrieveService;
        private readonly SignInManager<Domain.Entities.User.User> _signInManager;

        public GeneralAuthService(UserManager<Domain.Entities.User.User> userManager,
            RoleManager<Role> roleManager, IJwtTokenService jwtTokenHelper,
            IStateRetrieveService stateRetrieveService, IAuthService authService,
            ILogger<GeneralAuthService> logger, IBranchRetrieveService branchRetrieveService,
            SignInManager<Domain.Entities.User.User> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtTokenHelper = jwtTokenHelper;
            _stateRetrieveService = stateRetrieveService;
            _authService = authService;
            _logger = logger;
            _branchRetrieveService = branchRetrieveService;
            _signInManager = signInManager;
        }
        public async Task<bool> AddClaimsAsync(string email, IEnumerable<Claim> claims)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return false;

            var result = await _userManager.AddClaimsAsync(user, claims);
            return result.Succeeded;
        }

        public async Task<bool> AssignRoleAsync(Domain.Entities.User.User user, string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
                return false;

            var result = await _userManager.AddToRoleAsync(user, role.Name);
            return result.Succeeded;
        }

        private async Task UpdateUserLastLoginTime(Domain.Entities.User.User user)
        {
            // update staff's last login date
            user.LastLogin = TimeUtils.GetCurrentLocalTime();
            await _userManager.UpdateAsync(user);
        }

        private Domain.Entities.User.User MapToCustomerServiceOfficer(Domain.Entities.User.User user, AdLoginResponse adLoginResponse)
        {
            user.UserName = adLoginResponse.DisplayName;
            user.ManagerName = adLoginResponse.ManagerName;
            user.ManagerDepartment = adLoginResponse.ManagerDepartment;
            user.Department = adLoginResponse.Department;
            user.StaffId = adLoginResponse.StaffId;
            user.BranchId = adLoginResponse.BranchId;
            user.SolId = adLoginResponse.Sol;
            user.PhoneNumber = adLoginResponse.MobileNo;
            user.OnboardingStatus = OnboardingStatusType.Completed;
            user.LastLogin = TimeUtils.GetCurrentLocalTime();
            
            return user;
        }

        public async Task<ClaimsIdentity> GetClaimsIdentityForUser(Domain.Entities.User.User user)
        {
            var roles = await GetRolesForUserAsync(user);

            var claims = new List<Claim>();

            claims.Add(new Claim("UserId", user.Id.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, roles.First()));
            claims.Add(new Claim(ClaimTypes.Name, user.FirstName));
            claims.Add(new Claim(ClaimTypes.Email, user.Email!));

            var identity = new ClaimsIdentity(claims, "JWT");

            return identity;
        }

        public async Task<ClaimsIdentity> GetClaimsIdentityForStaff(Domain.Entities.User.User user)
        {
            var roles = await GetRolesForUserAsync(user);

            var claims = new List<Claim>();

            if (!string.IsNullOrWhiteSpace(user.BranchId))
            {
                claims.Add(new Claim(nameof(ClaimType.BranchId), user.BranchId));
            }

            claims.Add(new Claim(nameof(ClaimType.SolId), user.SolId!));
            claims.Add(new Claim(nameof(ClaimType.UserId), user.Id.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, roles.First()));
            claims.Add(new Claim(ClaimTypes.Name, user.FirstName));
            claims.Add(new Claim(ClaimTypes.Email, user.Email!));

            return new ClaimsIdentity(claims, "JWT");
        }

        public async Task<IList<string>> GetRolesForUserAsync(Domain.Entities.User.User user)
        {
            return await _userManager.GetRolesAsync(user);
        }
        
        public async Task<Domain.Entities.User.User?> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
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

            var newSolicitor = new Domain.Entities.User.User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Firm = new Firm
                {
                    Name = request.Firm.Name,
                    Address = request.Firm.Address,
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
                var roleName = RoleType.Solicitor.ToString();
                var role = await _roleManager.FindByNameAsync(roleName);

                if (role == null)
                    return new ObjectResponse<SolicitorOnboardResponse>("Solicitor onboarding failed; role must be added first", ResponseCodes.ServiceError);

                // Assign the role to the solicitor
                await _userManager.AddToRoleAsync(newSolicitor, roleName);

                // Enable 2FA for the solicitor
                await _userManager.SetTwoFactorEnabledAsync(newSolicitor, true);

                // Generate a 4-digit numeric token
                var resetToken = await _userManager.GenerateUserTokenAsync(newSolicitor, "NumericTokenProvider", "ResetPassword");
                await _userManager.SetAuthenticationTokenAsync(newSolicitor, "NumericTokenProvider", "ResetToken", resetToken);

                // TODO: Send the password reset token to the user's email
                // Construct the confirmation link using the token
                //var confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = newSolicitor.Id, token = emailConfirmationToken }, Request.Scheme);

                //// Send the email with the confirmation link
                //var emailBody = $"Click the link to confirm your email: {confirmationLink}";
                //await _emailService.SendEmailAsync(newSolicitor.Email, "Confirm Your Email", emailBody);

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
                        Address = newSolicitor.Firm.Address,
                        PhoneNumber = newSolicitor.PhoneNumber,
                        AccountNumber = newSolicitor.BankAccount,
                        Firm = newSolicitor.Firm.Name,
                        State = state.Name
                    }
                };
            }

            return new ObjectResponse<SolicitorOnboardResponse>("Solicitor onboarding failed", ResponseCodes.ServiceError);
        }

        public async Task<ObjectResponse<LoginResponse>> UserLogin(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
                return new ObjectResponse<LoginResponse>("The email is invalid, please try again!", ResponseCodes.InvalidCredentials);

            // determine role so as to know login route
            var role = await _userManager.GetRolesAsync(user);

            return role.First() switch
            {
                nameof(RoleType.Admin) or nameof(RoleType.Solicitor) => await InAppUserLoginFlow(user, request.Password),
                nameof(RoleType.Cso) or nameof(RoleType.LegalPerfectionTeam) or nameof(RoleType.ITSupport) => await StaffLoginFlow(user, role, request),
                _ => new ObjectResponse<LoginResponse>("Something went wrong, please try again.", ResponseCodes.ServiceError),
            };
        }

        private async Task<ObjectResponse<LoginResponse>> StaffLoginFlow(Domain.Entities.User.User user, IList<string> role, LoginRequest request)
        {
            ObjectResponse<AdLoginResponse> result = await _authService.LoginAsync(request);

            // get staff branch's name
            var branch = await _branchRetrieveService.GetBranchBySolId(user.SolId!);

            if (branch == null)
                return new ObjectResponse<LoginResponse>("Could not get staff's branch record", ResponseCodes.ServiceError);

            if (result.Code is ResponseCodes.Success)
            {
                if (user.OnboardingStatus == OnboardingStatusType.Initial)
                {
                    // update staff profile
                    user = MapToCustomerServiceOfficer(user, result.Data);

                    // update user details & last login
                    var status = await _userManager.UpdateAsync(user);

                    if (!status.Succeeded)
                        return new ObjectResponse<LoginResponse>("Staff login failed", ResponseCodes.ServiceError);

                    // generate claims & token for user
                    var staffIdentityClaims = await GetClaimsIdentityForStaff(user);
                    var staffToken = _jwtTokenHelper.GenerateJwtToken(staffIdentityClaims);

                    // generate staff's login response
                    LoginResponse loginResponse = GenerateLoginResponseForStaff(user, staffToken, role.First(), branch.Address);

                    // update user's last login
                    await UpdateUserLastLoginTime(user);

                    return new ObjectResponse<LoginResponse>("Successfully authenticated staff", ResponseCodes.Success)
                    {
                        Data = loginResponse
                    };
                }

                _logger.LogInformation("{Username} successfully logged in", request.Email);

                // create jwt token for staff
                var staffIdentity = await GetClaimsIdentityForStaff(user);
                var staffJwtToken = _jwtTokenHelper.GenerateJwtToken(staffIdentity);

                // generate staff's login response
                LoginResponse staffLoginResponse = GenerateLoginResponseForStaff(user, staffJwtToken, role.First(), branch.Address);

                // update user's last login
                await UpdateUserLastLoginTime(user);

                return new ObjectResponse<LoginResponse>("Successfully authenticated staff", ResponseCodes.Success)
                {
                    Data = staffLoginResponse
                };
            }

            // failure route
            return new ObjectResponse<LoginResponse>("Staff login failed", result.Code);
        }

        private LoginResponse GenerateLoginResponseForStaff(Domain.Entities.User.User user, 
            string staffJwtToken, string role, string branch)
        {
            return new LoginResponse
            {
                Token = staffJwtToken,
                is2FaRequired = false,
                DisplayName = user.FullName,
                Branch = branch,
                Role = role,
                LastLoginDate = user.LastLogin,
                SolId = user.SolId!
            };
        }

        private async Task<ObjectResponse<LoginResponse>> InAppUserLoginFlow(Domain.Entities.User.User user, string password)
        {
            if (user == null)
            {
                return new ObjectResponse<LoginResponse>("User not found", ResponseCodes.ServiceError);
            }

            if (user.LockoutEnabled && user.LockoutEnd >= TimeUtils.GetCurrentLocalTime())
            {
                // User is currently locked out
                // You can also check user.LockoutEnd for the date when the lockout will be lifted.
                return new ObjectResponse<LoginResponse>("User Is Locked Out For Multiple Wrong Password Attempts", ResponseCodes.LockedOutUser);
            }

            var authResult = await _userManager.CheckPasswordAsync(user, password);

            if (!authResult)
            {
                // Invalid password
                return new ObjectResponse<LoginResponse>("The password is invalid, please try again!", ResponseCodes.InvalidCredentials);
            }

            // check if user require 2fa
            var role = await _userManager.GetRolesAsync(user);

            if (role.First() == RoleType.Solicitor.ToString())
            {
                // Generate and save the 2FA token
                var tokenProvider = TokenOptions.DefaultPhoneProvider; // You can use the appropriate token provider
                var twoFactorToken = await _userManager.GenerateTwoFactorTokenAsync(user, tokenProvider);
                await _userManager.SetAuthenticationTokenAsync(user, tokenProvider, "2fa", twoFactorToken);

                //TODO: send 2fa token to user's email

                // Login successful but requires 2fa
                return new ObjectResponse<LoginResponse>("Enter the code sent to your email to complete the login process", ResponseCodes.Success)
                {
                    Data = new LoginResponse { is2FaRequired = true }
                };
            }

            // update user's last login
            await UpdateUserLastLoginTime(user);

            var ClaimsIdentity = await GetClaimsIdentityForUser(user);

            // Generate token
            var token = _jwtTokenHelper.GenerateJwtToken(ClaimsIdentity);

            // Login successful
            return new ObjectResponse<LoginResponse>("Successfully authenticated user", ResponseCodes.Success)
            {
                Data = new LoginResponse { Token = token }
            };
        }

        public async Task<StatusResponse> OnboardNewUser(OnboardNewUserRequest request)
        {
            Domain.Entities.User.User user = CreateNewUserObject(request);

            // block out attempts to onboard solicitors via this route
            if (request.UserRole == RoleType.Solicitor)
                return new StatusResponse("Sorry, you cannot onboard a solicitor via this route", ResponseCodes.ServiceError);

            // generate default password
            var defaultPassword = Helpers.GenerateDefaultPassword();

            var userCreationStatus = await _userManager.CreateAsync(user, defaultPassword);

            if (!userCreationStatus.Succeeded)
            {
                var errMessage = userCreationStatus?.Errors?.ToArray()?[0]?.Description ?? $"Error creating user with email: {request.Email}";
                _logger.LogError(errMessage);
                return new StatusResponse(errMessage, ResponseCodes.ServiceError);
            }

            // assign role to user
            IdentityResult result = await _userManager.AddToRoleAsync(user, request.UserRole.ToString());

            if (!result.Succeeded)
                return new StatusResponse($"Error creating user with email: {request.Email}", ResponseCodes.ServiceError);

            // push to notification queue to notify user to sign in and/or change password if role is solicitor and also 2fa token & code
            //BackgroundJob.Enqueue<IBackgroundService>(x => x.n);

            return new StatusResponse("User onboarded successfully.", ResponseCodes.Success);
        }

        private Domain.Entities.User.User CreateNewUserObject(OnboardNewUserRequest request)
        {
            return new Domain.Entities.User.User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                UserName = request.Email,
                OnboardingStatus = OnboardingStatusType.Initial
            };
        }

        public async Task<ObjectResponse<LoginResponse>> Verify2fa(TwoFactorVerificationRequest request)
        {
            // get user
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
                return new ObjectResponse<LoginResponse>("The email provided is not valid", ResponseCodes.InvalidCredentials);

            // get user role
            var roles = await _userManager.GetRolesAsync(user);

            if (roles == null)
                return new ObjectResponse<LoginResponse>("User not authenticated", ResponseCodes.ServiceError);

            if (roles.First() != RoleType.Solicitor.ToString())
                return new ObjectResponse<LoginResponse>("You're not allowed to use this service", ResponseCodes.InvalidCredentials);

            // Verify the 2FA code
            var tokenProvider = TokenOptions.DefaultPhoneProvider; // Use the appropriate token provider
            var isTokenValid = await _userManager.VerifyTwoFactorTokenAsync(user, tokenProvider, request.TwoFactorCode);

            if (isTokenValid == false)
                return new ObjectResponse<LoginResponse>("Invalid Two-Factor Authentication code.", ResponseCodes.InvalidToken);

            // update user's last login
            await UpdateUserLastLoginTime(user);

            // add claims 
            var ClaimsIdentity = await GetClaimsIdentityForUser(user);

            // Generate token
            var token = _jwtTokenHelper.GenerateJwtToken(ClaimsIdentity);

            // Login successful
            return new ObjectResponse<LoginResponse>("Successfully authenticated user", ResponseCodes.Success)
            {
                Data = new LoginResponse { Token = token, LastLoginDate = user.LastLogin, Role = roles.First(), DisplayName = user.FullName }
            };
        }

        public async Task<StatusResponse> RequestUnlockCode(RequestUnlockCodeRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
                return new StatusResponse("Please enter a valid email address", ResponseCodes.InvalidCredentials);

            // Generate a unique unlock code and save it in the database
            string unlockCode = GenerateUnlockCode();
            await SaveUnlockCodeInDatabase(user, unlockCode);

            // TODO: Send an email to the user with the unlock code
           // await SendUnlockCodeEmail(user.Email, unlockCode);

            return new StatusResponse("Unlock code sent to your email.", ResponseCodes.Success);
        }

        private string GenerateUnlockCode()
        {
            // Generate a random 6-digit unlock code
            Random random = new Random();
            int code = random.Next(100000, 999999);
            return code.ToString();
        }

        private async Task SaveUnlockCodeInDatabase(Domain.Entities.User.User user, string unlockCode)
        {
            // Store the unlock code in the database
            user.UnlockCode = unlockCode;
            user.UnlockCodeExpiration = TimeUtils.GetCurrentLocalTime().Add(UnlockCodeExpirationMinutes());

            await _userManager.UpdateAsync(user);
        }

        private TimeSpan UnlockCodeExpirationMinutes()
        {
            int expirationMinutes = 15; // 15 minutes, for example
            return TimeSpan.FromMinutes(expirationMinutes);
        }

        public async Task<StatusResponse> UnlockCode(UnlockAccountRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
                return new StatusResponse("The email provided is not valid", ResponseCodes.InvalidCredentials);

            // Verify the unlock code
            var isUnlockCodeValid = ValidateUnlockCode(user, request.UnlockCode);

            if (!isUnlockCodeValid)
                return new StatusResponse("Invalid unlock code", ResponseCodes.InvalidToken);

            // Clear the unlock code and expiration
            user.AccessFailedCount = 0;
            user.UnlockCode = null;
            user.UnlockCodeExpiration = null;

            // Update the lockout end time to null to fully lift the lockout
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(-1)); // A far past date is used to lift the lockout

            // Update user in the database
            await _userManager.UpdateAsync(user);

            return new StatusResponse("Account unlocked successfully. Now, you can login to your account.", ResponseCodes.Success);
        }

        private bool ValidateUnlockCode(Domain.Entities.User.User user, string unlockCode)
        {
            if (user.UnlockCode != unlockCode)
                return false;

            if (user.UnlockCodeExpiration == null || user.UnlockCodeExpiration < DateTime.UtcNow)
                return false;

            return true;
        }

        public async Task<StatusResponse> ResetPassword(ResetPasswordRequest request)
        {
            // Find the user by email
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
                return new StatusResponse("Invalid email", ResponseCodes.InvalidCredentials);

            var isTokenValid = await _userManager.VerifyUserTokenAsync(user, "NumericTokenProvider", "ResetPassword", request.Token);

            if (isTokenValid == false)
                return new StatusResponse("Invalid token provided", ResponseCodes.InvalidToken);

            // Reset the password
            var resetPasswordResult = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

            if (!resetPasswordResult.Succeeded)
                return new StatusResponse("Password reset failed.", ResponseCodes.ServiceError);

            return new StatusResponse("Password reset successful.", ResponseCodes.Success);
        }
    }
}
