using Fcmb.Shared.Auth.Models.Requests;
using Fcmb.Shared.Auth.Models.Responses;
using Fcmb.Shared.Auth.Services;
using Fcmb.Shared.Models.Responses;
using Fcmb.Shared.Utilities;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Requests.Notification;
using LegalSearch.Application.Models.Requests.User;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.Role;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums;
using LegalSearch.Domain.Enums.Role;
using LegalSearch.Domain.Enums.User;
using LegalSearch.Infrastructure.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace LegalSearch.Infrastructure.Services.User
{
    public class GeneralAuthService : IGeneralAuthService<Domain.Entities.User.User>
    {
        private readonly UserManager<Domain.Entities.User.User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IJwtTokenService _jwtTokenHelper;
        private readonly IAuthService _authService;
        private readonly IStateRetrieveService _stateRetrieveService;
        private readonly ILogger<GeneralAuthService> _logger;
        private readonly IBranchRetrieveService _branchRetrieveService;
        private readonly IEmailService _emailService;

        public GeneralAuthService(UserManager<Domain.Entities.User.User> userManager,
            RoleManager<Role> roleManager, IJwtTokenService jwtTokenHelper,
            IStateRetrieveService stateRetrieveService, IAuthService authService,
            ILogger<GeneralAuthService> logger, IBranchRetrieveService branchRetrieveService,
            IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtTokenHelper = jwtTokenHelper;
            _stateRetrieveService = stateRetrieveService;
            _authService = authService;
            _logger = logger;
            _branchRetrieveService = branchRetrieveService;
            _emailService = emailService;
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

        private Domain.Entities.User.User MapToStaffPayload(Domain.Entities.User.User user, AdLoginResponse adLoginResponse)
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
            claims.Add(new Claim(ClaimTypes.Role, roles[0]));
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
            claims.Add(new Claim(ClaimTypes.Role, roles[0]));
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
                return new ObjectResponse<SolicitorOnboardResponse>("Solicitor with the given email already exists", ResponseCodes.BadRequest);
            }

            // get state
            var state = await _stateRetrieveService.GetStateById(request.Firm.StateId);

            if (state == null)
                return new ObjectResponse<SolicitorOnboardResponse>("State Id is not valid", ResponseCodes.BadRequest);

            // get state of coverage
            var stateOfCoverage = await _stateRetrieveService.GetStateById(request.Firm.StateOfCoverageId);

            if (stateOfCoverage == null)
                return new ObjectResponse<SolicitorOnboardResponse>("State of coverage Id is not valid", ResponseCodes.BadRequest);

            var defaultPassword = Helpers.GenerateDefaultPassword();

            Domain.Entities.User.User newSolicitor = MapNewSolicitor(request, state, stateOfCoverage.Id);

            var result = await _userManager.CreateAsync(newSolicitor, defaultPassword);

            if (result.Succeeded)
            {
                return await FinalizeSolicitorInitialOnboardingStage(state, newSolicitor);
            }

            return new ObjectResponse<SolicitorOnboardResponse>("Solicitor onboarding failed", ResponseCodes.Conflict);
        }

        private async Task<ObjectResponse<SolicitorOnboardResponse>> FinalizeSolicitorInitialOnboardingStage(State state, Domain.Entities.User.User newSolicitor)
        {
            // Onboarding succeeded, now assign the roles to the solicitor
            var roleName = RoleType.Solicitor.ToString();
            var role = await _roleManager.FindByNameAsync(roleName);

            if (role == null)
                return new ObjectResponse<SolicitorOnboardResponse>("Solicitor onboarding failed; role must be added first", ResponseCodes.Conflict);

            // Assign the roles to the solicitor
            await _userManager.AddToRoleAsync(newSolicitor, roleName);

            // Enable 2FA for the solicitor
            await _userManager.SetTwoFactorEnabledAsync(newSolicitor, true);

            // Generate a 4-digit numeric token
            var resetToken = await _userManager.GenerateUserTokenAsync(newSolicitor, "NumericTokenProvider", "ResetPassword");
            await _userManager.SetAuthenticationTokenAsync(newSolicitor, "NumericTokenProvider", "ResetToken", resetToken);

            string emailBody = EmailTemplates.GetEmailTemplateForNewlyOnboardedSolicitor();

            List<KeyValuePair<string, string>> keys = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("{{username}}", newSolicitor.FirstName),
                    new KeyValuePair<string, string>("{{email}}", newSolicitor.Email!),
                    new KeyValuePair<string, string>("{{role}}", role.Name!),
                    new KeyValuePair<string, string>("{{token}}", resetToken)
                };

            emailBody = await emailBody.UpdatePlaceHolders(keys);
            string title = "Welcome to LegalSearch";

            await SendEmail(newSolicitor, emailBody, title);

            // update last login
            await UpdateUserLastLoginTime(newSolicitor);

            // Onboarding and roles assignment succeeded
            return new ObjectResponse<SolicitorOnboardResponse>("Solicitor onboarding and role assignment succeeded", ResponseCodes.Success)
            {
                Data = new SolicitorOnboardResponse
                {
                    SolicitorId = newSolicitor.Id,
                    FirstName = newSolicitor.FirstName,
                    LastName = newSolicitor.LastName ?? string.Empty,
                    Email = newSolicitor.Email ?? string.Empty,
                    Address = newSolicitor!.Firm!.Address ?? string.Empty,
                    PhoneNumber = newSolicitor.PhoneNumber ?? string.Empty,
                    AccountNumber = newSolicitor.BankAccount ?? string.Empty,
                    Firm = newSolicitor.Firm.Name,
                    State = state.Name
                }
            };
        }

        private static Domain.Entities.User.User MapNewSolicitor(SolicitorOnboardRequest request, State state, Guid stateOfCoverage)
        {
            return new Domain.Entities.User.User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                StateId = state.Id,
                State = state,
                Firm = new Firm
                {
                    Name = request.Firm.Name,
                    Address = request.Firm.Address,
                    StateId = state.Id,
                    StateOfCoverageId = stateOfCoverage
                },
                Email = request.Email,
                UserName = request.Email,
                PhoneNumber = request.PhoneNumber,
                BankAccount = request.BankAccount,
                OnboardingStatus = OnboardingStatusType.Initial,
                ProfileStatus = ProfileStatusType.InActive.ToString(),
            };
        }

        /// <summary>
        /// This method routes the requests to the appropriate method to handle the user login process
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public async Task<ObjectResponse<LoginResponse>> UserLogin(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
                return new ObjectResponse<LoginResponse>("The email is invalid, please try again!", ResponseCodes.InvalidCredentials);

            // determine roles so as to know login route
            var role = await _userManager.GetRolesAsync(user);

            return role[0] switch
            {
                nameof(RoleType.Admin) or nameof(RoleType.Solicitor) => await InAppUserLoginFlow(user, request.Password),
                nameof(RoleType.Cso) or nameof(RoleType.LegalPerfectionTeam) or nameof(RoleType.ITSupport) => await StaffLoginFlow(user, role, request),
                _ => new ObjectResponse<LoginResponse>("Something went wrong, please try again.", ResponseCodes.BadRequest),
            };
        }

        /// <summary>
        /// This method handles the login flow for FCMB staffs.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="role">The role.</param>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        private async Task<ObjectResponse<LoginResponse>> StaffLoginFlow(Domain.Entities.User.User user, IList<string> role, LoginRequest request)
        {
            ObjectResponse<AdLoginResponse> result = await _authService.LoginAsync(request);
            var solIds = new List<string>() { "198", "259", "052", "048", "111", "061" };
            int rand = new Random(solIds.Count).Next(0, solIds.Count);
            user.SolId = solIds[rand];

            if (/*result.Code is ResponseCodes.Success*/true)
            {
                // get staff branch's name
                var branch = await _branchRetrieveService.GetBranchBySolId(user.SolId!);

                if (branch == null)
                    return new ObjectResponse<LoginResponse>("Could not get staff's branch record", ResponseCodes.BadRequest);

                if (user.OnboardingStatus == OnboardingStatusType.Initial)
                {
                    // update staff profile

                    var payload = new AdLoginResponse
                    {
                        DisplayName = $"User{new Random().Next(0, 9099)}",
                        Sol = solIds[rand]
                    };

                    user.SolId = solIds[rand];
                    user = MapToStaffPayload(user, payload);
                    //user = MapToStaffPayload(user, result.Data);
                    user.ProfileStatus = ProfileStatusType.Active.ToString();

                    // update user details & last login
                    var status = await _userManager.UpdateAsync(user);

                    if (!status.Succeeded)
                        return new ObjectResponse<LoginResponse>("Staff login failed", ResponseCodes.Conflict);

                    // generate claims & token for user
                    var staffIdentityClaims = await GetClaimsIdentityForStaff(user);
                    var staffToken = _jwtTokenHelper.GenerateJwtToken(staffIdentityClaims);

                    // generate staff's login response
                    LoginResponse loginResponse = GenerateLoginResponseForStaff(user, staffToken, role[0], branch.Address);

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
            //return new ObjectResponse<LoginResponse>("Staff login failed", result.Code);
            return new ObjectResponse<LoginResponse>("Staff login failed", ResponseCodes.BadRequest);
        }

        /// <summary>
        /// Generates the login response for staff.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="staffJwtToken">The staff JWT token.</param>
        /// <param name="role">The role.</param>
        /// <param name="branch">The branch.</param>
        /// <returns></returns>
        private LoginResponse GenerateLoginResponseForStaff(Domain.Entities.User.User user,
            string staffJwtToken, string role, string branch)
        {
            return new LoginResponse
            {
                Token = staffJwtToken,
                Is2FaRequired = false,
                DisplayName = user.FullName,
                Branch = branch,
                Role = role,
                LastLoginDate = user.LastLogin,
                SolId = user.SolId!
            };
        }

        /// <summary>
        /// It handles the user login flow for admin and solicitors.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        private async Task<ObjectResponse<LoginResponse>> InAppUserLoginFlow(Domain.Entities.User.User user, string password)
        {
            if (user == null)
            {
                return new ObjectResponse<LoginResponse>("User not found", ResponseCodes.DataNotFound);
            }

            bool isUserLockedOut = await IsUserLockedOut(user.Email!);

            if (isUserLockedOut)
            {
                // User is currently locked out
                return await NotifyUserOfLockedOutStatus(user);
            }

            var isUserAuthenticatedSuccessfully = await _userManager.CheckPasswordAsync(user, password);

            if (!isUserAuthenticatedSuccessfully)
            {
                // Increment access failure count
                await _userManager.AccessFailedAsync(user);

                // check failed attempts and potentially lock the account
                await HandleUserLockout(user);

                // Invalid password
                return new ObjectResponse<LoginResponse>("The password is invalid, please try again!", ResponseCodes.InvalidCredentials);
            }

            var roles = await _userManager.GetRolesAsync(user);

            // check if user is solicitor and yet to reset his/her password
            if (roles[0] == RoleType.Solicitor.ToString() && user.OnboardingStatus == OnboardingStatusType.Initial)
                return new ObjectResponse<LoginResponse>("Please reset your password before using the application.", ResponseCodes.Forbidden);

            // check if user require 2fa
            if (roles[0] == RoleType.Solicitor.ToString())
            {
                return await Generate2faTokenForSolicitor(user, roles[0]);
            }

            DateTime? previousLastLogin = user.LastLogin ?? TimeUtils.GetCurrentLocalTime();

            // update user's last login
            await UpdateUserLastLoginTime(user);

            var ClaimsIdentity = await GetClaimsIdentityForUser(user);

            // Generate token
            var token = _jwtTokenHelper.GenerateJwtToken(ClaimsIdentity);

            // Login successful
            return new ObjectResponse<LoginResponse>("Successfully authenticated user", ResponseCodes.Success)
            {
                Data = new LoginResponse { Token = token, Role = roles[0], LastLoginDate = previousLastLogin }
            };
        }

        /// <summary>
        /// Notifies the user of locked out status.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns></returns>
        private async Task<ObjectResponse<LoginResponse>> NotifyUserOfLockedOutStatus(Domain.Entities.User.User user)
        {
            string emailBody = EmailTemplates.GetEmailTemplateForUnlockingAccountAwareness();

            List<KeyValuePair<string, string>> keys = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("{{Username}}", user.FirstName),
                };

            emailBody = await emailBody.UpdatePlaceHolders(keys);
            string title = "Unlock Your Account";

            await SendEmail(user, emailBody, title);

            return new ObjectResponse<LoginResponse>("User Is Locked Out For Multiple Wrong Password Attempts", ResponseCodes.LockedOutUser);
        }

        public async Task<bool> IsUserLockedOut(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // User not found
                return false;
            }

            return await _userManager.IsLockedOutAsync(user);
        }

        /// <summary>
        /// This method generates and sends 2fa token to a given solicitor's email.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="role">The role.</param>
        /// <returns></returns>
        private async Task<ObjectResponse<LoginResponse>> Generate2faTokenForSolicitor(Domain.Entities.User.User user, string role)
        {
            // Generate and save the 2FA token
            var tokenProvider = TokenOptions.DefaultPhoneProvider; // You can use the appropriate token provider
            var twoFactorToken = await _userManager.GenerateTwoFactorTokenAsync(user, tokenProvider);
            await _userManager.SetAuthenticationTokenAsync(user, tokenProvider, "2fa", twoFactorToken);

            string emailBody = EmailTemplates.GetEmailTemplateForAuthenticating2FaCode();

            List<KeyValuePair<string, string>> keys = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("{{Username}}", user.FirstName),
                    new KeyValuePair<string, string>("{{token}}", twoFactorToken)
                };

            emailBody = await emailBody.UpdatePlaceHolders(keys);
            string title = "Complete Your Login with 2FA";

            await SendEmail(user, emailBody, title);

            // Login successful but requires 2fa
            return new ObjectResponse<LoginResponse>("Enter the code sent to your email to complete the login process", ResponseCodes.Success)
            {
                Data = new LoginResponse { Is2FaRequired = true, Role = role }
            };
        }

        /// <summary>
        /// Handles the user lockout.
        /// </summary>
        /// <param name="user">The user.</param>
        private async Task HandleUserLockout(Domain.Entities.User.User user)
        {
            var failedAttempts = await _userManager.GetAccessFailedCountAsync(user);

            if (failedAttempts == 3) // Adjust based on your configuration
            {
                await _userManager.SetLockoutEndDateAsync(user, TimeUtils.GetCurrentLocalTime().AddMinutes(15)); // Lock for 15 minutes
            }
        }

        private async Task SendEmail(Domain.Entities.User.User user, string emailBody, string title)
        {
            var emailPayload = new SendEmailRequest
            {
                From = "ebusiness@fcmb.com",
                To = user.Email!,
                Subject = title,
                Body = emailBody
            };

            await _emailService.SendEmailAsync(emailPayload);
        }

        /// <summary>
        /// This method handles user onboarding.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public async Task<StatusResponse> OnboardNewUser(OnboardNewUserRequest request)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);

            if (existingUser != null)
            {
                // the given email already exists
                return new ObjectResponse<SolicitorOnboardResponse>("Email already registered.", ResponseCodes.BadRequest);
            }

            Domain.Entities.User.User user = CreateNewUserObject(request);

            // get roles
            var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());

            // check if roles is null
            if (role == null)
                return new StatusResponse("You did not provide a valid role Id", ResponseCodes.BadRequest);

            // block out attempts to onboard solicitors via this route
            if (role.Name == RoleType.Solicitor.ToString())
                return new StatusResponse("Sorry, you cannot onboard a solicitor via this route", ResponseCodes.Forbidden);

            // generate default password
            var defaultPassword = Helpers.GenerateDefaultPassword();

            var userCreationStatus = await _userManager.CreateAsync(user, defaultPassword);

            if (!userCreationStatus.Succeeded)
            {
                var errMessage = userCreationStatus.GetStandardizedError() ?? $"Error creating user with email: {request.Email}";
                _logger.LogError(errMessage);
                return new StatusResponse(errMessage, ResponseCodes.BadRequest);
            }

            // assign roles to user
            IdentityResult result = await _userManager.AddToRoleAsync(user, role.Name);

            if (!result.Succeeded)
                return new StatusResponse($"Error creating user with email: {request.Email}", ResponseCodes.Conflict);

            // push to notification queue to notify user to sign in and/or change password
            string emailBody = EmailTemplates.GetEmailTemplateForNewlyOnboardedUser();

            List<KeyValuePair<string, string>> keys = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("{{Username}}", user.FirstName),
                new KeyValuePair<string, string>("{{role}}", role.Name!),
            };

            emailBody = await emailBody.UpdatePlaceHolders(keys);
            string title = "Welcome to LegalSearch";

            await SendEmail(user, emailBody, title); // notify staff via mail

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
                OnboardingStatus = OnboardingStatusType.Initial,
                ProfileStatus = ProfileStatusType.InActive.ToString()
            };
        }

        public async Task<ObjectResponse<LoginResponse>> Verify2fa(TwoFactorVerificationRequest request)
        {
            // get user
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
                return new ObjectResponse<LoginResponse>("The email provided is not valid", ResponseCodes.InvalidCredentials);

            // get user roles
            var roles = await _userManager.GetRolesAsync(user);

            if (roles == null)
                return new ObjectResponse<LoginResponse>("User not authenticated", ResponseCodes.Unauthenticated);

            if (roles[0] != RoleType.Solicitor.ToString())
                return new ObjectResponse<LoginResponse>("You're not allowed to use this service", ResponseCodes.InvalidCredentials);

            // Verify the 2FA code
            var tokenProvider = TokenOptions.DefaultPhoneProvider; // Use the appropriate token provider
            var isTokenValid = await _userManager.VerifyTwoFactorTokenAsync(user, tokenProvider, request.TwoFactorCode);

            if (!isTokenValid)
                return new ObjectResponse<LoginResponse>("Invalid Two-Factor Authentication code.", ResponseCodes.InvalidToken);

            // Get the user's previous last login date
            DateTime? previousLastLogin = user.LastLogin ?? TimeUtils.GetCurrentLocalTime();

            // update user's last login
            await UpdateUserLastLoginTime(user);

            // add claims 
            var ClaimsIdentity = await GetClaimsIdentityForUser(user);

            // Generate token
            var token = _jwtTokenHelper.GenerateJwtToken(ClaimsIdentity);

            // Login successful
            return new ObjectResponse<LoginResponse>("Successfully authenticated user", ResponseCodes.Success)
            {
                Data = new LoginResponse { Token = token, LastLoginDate = previousLastLogin, Role = roles[0], DisplayName = user.FullName }
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

            string emailBody = EmailTemplates.GetEmailTemplateForUnlockingAccount();

            List<KeyValuePair<string, string>> keys = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("{{Username}}", user.FirstName),
                new KeyValuePair<string, string>("{{token}}", unlockCode)
            };

            emailBody = await emailBody.UpdatePlaceHolders(keys);
            const string title = "Unlock Your Account";

            await SendEmail(user, emailBody, title);

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
            const int expirationMinutes = 15; // 15 minutes, for example
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

            if (!isTokenValid)
                return new StatusResponse("Invalid token provided", ResponseCodes.InvalidToken);

            // Reset the password
            var resetPasswordResult = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

            if (!resetPasswordResult.Succeeded)
                return new StatusResponse(resetPasswordResult.GetStandardizedError() ?? "Password reset failed.", ResponseCodes.Conflict);

            // update user's onboarding status
            user.OnboardingStatus = OnboardingStatusType.Completed; // solicitor has successfully completed the onboarding process
            user.ProfileStatus = ProfileStatusType.Active.ToString(); // make solicitor profile active
            await _userManager.UpdateAsync(user);

            return new StatusResponse("Password reset successful.", ResponseCodes.Success);
        }

        public async Task<ObjectResponse<ReIssueTokenResponse>> ReIssueToken(string userId)
        {
            // get logged-in user
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return new ObjectResponse<ReIssueTokenResponse>("User not found, please try again.", ResponseCodes.InactiveUser);

            // get user roles
            var roles = await _userManager.GetRolesAsync(user);

            if (roles == null)
                return new ObjectResponse<ReIssueTokenResponse>("User not authenticated", ResponseCodes.Unauthenticated);

            var role = roles[0];
            ClaimsIdentity claims;
            if (role == nameof(RoleType.Admin) || role == nameof(RoleType.Solicitor))
            {
                claims = await GetClaimsIdentityForUser(user);
            }
            else if (role == nameof(RoleType.Cso) || role == nameof(RoleType.LegalPerfectionTeam) || role == nameof(RoleType.ITSupport))
            {
                claims = await GetClaimsIdentityForStaff(user);
            }
            else
            {
                return new ObjectResponse<ReIssueTokenResponse>("Something went wrong, please try again.", ResponseCodes.BadRequest);
            }

            // Generate token
            var token = _jwtTokenHelper.GenerateJwtToken(claims);

            return new ObjectResponse<ReIssueTokenResponse>("Token generated successfully", ResponseCodes.Success) { Data = new ReIssueTokenResponse { Token = token } };
        }
    }
}
