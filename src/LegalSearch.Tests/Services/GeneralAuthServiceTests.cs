using Fcmb.Shared.Auth.Models.Requests;
using k8s.Authentication;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Requests.Notification;
using LegalSearch.Domain.Entities.Location;
using LegalSearch.Domain.Entities.Role;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums.Role;
using LegalSearch.Domain.Enums.User;
using LegalSearch.Infrastructure.Services.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace LegalSearch.Test.Services
{
    public class GeneralAuthServiceTests
    {
        [Fact]
        public async Task AssignRoleAsync_WithValidRole_ShouldReturnTrue()
        {
            // Arrange
            var fcmbConfig = new FCMBConfig
            {
                BaseUrl = "Your BaseUrl",
                ClientId = "Your ClientId",
                SecretKey = "Your SecretKey",
                FrontendBaseUrl = "Your FrontendBaseUrl",
                SubscriptionKey = "Your SubscriptionKey",
                Password = "Your Password",
                SLAPeriod = "Your SLAPeriod",
                CurrencyCode = "Your CurrencyCode",
                LegalSearchAmount = "Your LegalSearchAmount",
                LegalSearchRemarks = "Your LegalSearchRemarks",
                LegalSearchPaymentRemarks = "Your LegalSearchPaymentRemarks",
                LegalSearchReasonCode = "Your LegalSearchReasonCode",
                AuthConfig = new AuthConfig
                {
                    AuthUrl = "Your AuthUrl",
                    AuthClientId = "Your AuthClientId",
                    AuthSecretKey = "Your AuthSecretKey"
                },
                EmailConfig = new EmailConfig
                {
                    EmailUrl = "Your EmailUrl",
                    SenderEmail = "Your SenderEmail",
                    SenderName = "Your SenderName"
                }
            };

            var options = Options.Create(fcmbConfig);

            var user = new User() { FirstName = "test"}; 
            var roleName = "YourRoleName"; // Specify the role name
            var role = new Role(roleName); // Create a role instance

            // Mock UserManager
            var userManager = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                null, null, null, null, null, null, null, null);

            userManager.Setup(um => um.AddToRoleAsync(user, roleName))
                .ReturnsAsync(IdentityResult.Success); // Simulate a successful role assignment

            // Mock RoleManager
            var roleManager = new Mock<RoleManager<Role>>(
                new Mock<IRoleStore<Role>>().Object, null, null, null, null);

            roleManager.Setup(rm => rm.FindByNameAsync(roleName))
                .ReturnsAsync(role); // Simulate finding the role

            // Create an instance of GeneralAuthService with the mocked services
            var authService = new GeneralAuthService(
                userManager.Object,
                roleManager.Object,
                null, // You can add other mocked dependencies here
                null,
                null,
                null,
                null,
                null,
                options);

            // Act
            bool result = await authService.AssignRoleAsync(user, roleName);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task AddClaimsAsync_WithValidUser_ShouldReturnTrue()
        {
            // Arrange
            var fcmbConfig = new FCMBConfig
            {
                BaseUrl = "Your BaseUrl",
                ClientId = "Your ClientId",
                SecretKey = "Your SecretKey",
                FrontendBaseUrl = "Your FrontendBaseUrl",
                SubscriptionKey = "Your SubscriptionKey",
                Password = "Your Password",
                SLAPeriod = "Your SLAPeriod",
                CurrencyCode = "Your CurrencyCode",
                LegalSearchAmount = "Your LegalSearchAmount",
                LegalSearchRemarks = "Your LegalSearchRemarks",
                LegalSearchPaymentRemarks = "Your LegalSearchPaymentRemarks",
                LegalSearchReasonCode = "Your LegalSearchReasonCode",
                AuthConfig = new AuthConfig
                {
                    AuthUrl = "Your AuthUrl",
                    AuthClientId = "Your AuthClientId",
                    AuthSecretKey = "Your AuthSecretKey"
                },
                EmailConfig = new EmailConfig
                {
                    EmailUrl = "Your EmailUrl",
                    SenderEmail = "Your SenderEmail",
                    SenderName = "Your SenderName"
                }
            };

            var options = Options.Create(fcmbConfig);

            var user = new User() { FirstName = "test" }; 
            var claims = new List<Claim>
            {
                new Claim("ClaimType1", "ClaimValue1"),
                new Claim("ClaimType2", "ClaimValue2")
            };

            // Mock UserManager
            var userManager = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                null, null, null, null, null, null, null, null);

            userManager.Setup(um => um.FindByEmailAsync(user.Email))
                .ReturnsAsync(user); // Simulate finding the user

            userManager.Setup(um => um.AddClaimsAsync(user, claims))
                .ReturnsAsync(IdentityResult.Success); // Simulate a successful claim addition

            // Create an instance of GeneralAuthService with the mocked services
            var authService = new GeneralAuthService(
                userManager.Object,
                null, // Mock the RoleManager here
                null,
                null,
                null,
                null,
                null,
                null,
                options);

            // Act
            bool result = await authService.AddClaimsAsync(user.Email, claims);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetUserByEmailAsync_WithValidEmail_ShouldReturnUser()
        {
            // Arrange
            var fcmbConfig = new FCMBConfig
            {
                BaseUrl = "Your BaseUrl",
                ClientId = "Your ClientId",
                SecretKey = "Your SecretKey",
                FrontendBaseUrl = "Your FrontendBaseUrl",
                SubscriptionKey = "Your SubscriptionKey",
                Password = "Your Password",
                SLAPeriod = "Your SLAPeriod",
                CurrencyCode = "Your CurrencyCode",
                LegalSearchAmount = "Your LegalSearchAmount",
                LegalSearchRemarks = "Your LegalSearchRemarks",
                LegalSearchPaymentRemarks = "Your LegalSearchPaymentRemarks",
                LegalSearchReasonCode = "Your LegalSearchReasonCode",
                AuthConfig = new AuthConfig
                {
                    AuthUrl = "Your AuthUrl",
                    AuthClientId = "Your AuthClientId",
                    AuthSecretKey = "Your AuthSecretKey"
                },
                EmailConfig = new EmailConfig
                {
                    EmailUrl = "Your EmailUrl",
                    SenderEmail = "Your SenderEmail",
                    SenderName = "Your SenderName"
                }
            };

            var options = Options.Create(fcmbConfig);

            var userEmail = "user@example.com"; // Specify the user's email
            var user = new User() { FirstName = "test", Email = userEmail };

            // Mock UserManager
            var userManager = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                null, null, null, null, null, null, null, null);

            userManager.Setup(um => um.FindByEmailAsync(userEmail))
                .ReturnsAsync(user); // Simulate finding the user

            // Create an instance of GeneralAuthService with the mocked services
            var authService = new GeneralAuthService(
                userManager.Object,
                null, // Mock the RoleManager here
                null,
                null,
                null,
                null,
                null,
                null,
                options);

            // Act
            User result = await authService.GetUserByEmailAsync(userEmail);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userEmail, result.Email);
        }

        [Fact]
        public async Task GetClaimsIdentityForUser_WithValidUser_ShouldReturnClaimsIdentity()
        {
            // Arrange
            var fcmbConfig = new FCMBConfig
            {
                BaseUrl = "Your BaseUrl",
                ClientId = "Your ClientId",
                SecretKey = "Your SecretKey",
                FrontendBaseUrl = "Your FrontendBaseUrl",
                SubscriptionKey = "Your SubscriptionKey",
                Password = "Your Password",
                SLAPeriod = "Your SLAPeriod",
                CurrencyCode = "Your CurrencyCode",
                LegalSearchAmount = "Your LegalSearchAmount",
                LegalSearchRemarks = "Your LegalSearchRemarks",
                LegalSearchPaymentRemarks = "Your LegalSearchPaymentRemarks",
                LegalSearchReasonCode = "Your LegalSearchReasonCode",
                AuthConfig = new AuthConfig
                {
                    AuthUrl = "Your AuthUrl",
                    AuthClientId = "Your AuthClientId",
                    AuthSecretKey = "Your AuthSecretKey"
                },
                EmailConfig = new EmailConfig
                {
                    EmailUrl = "Your EmailUrl",
                    SenderEmail = "Your SenderEmail",
                    SenderName = "Your SenderName"
                }
            };

            var options = Options.Create(fcmbConfig);

            var userEmail = "user@example.com"; // Specify the user's email
            var user = new User() { FirstName = "test", Email = userEmail, Id = Guid.NewGuid() };
            var userManager = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                null, null, null, null, null, null, null, null);

            // Mock the UserManager's GetRolesAsync method to return some roles
            userManager.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Role1", "Role2" });

            // Create an instance of GeneralAuthService with the mocked services
            var authService = new GeneralAuthService(
                userManager.Object,
                null, // Mock the RoleManager here
                null,
                null,
                null,
                null,
                null,
                null,
                options);

            // Act
            ClaimsIdentity result = await authService.GetClaimsIdentityForUser(user);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Claims.Count() == 4);
            Assert.True(result.HasClaim(c => c.Type == "UserId"));
            Assert.True(result.HasClaim(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"));
            Assert.True(result.HasClaim(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"));
            Assert.True(result.HasClaim(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"));
        }

        [Fact]
        public async Task OnboardSolicitorAsync_WithValidRequest_ShouldSucceed()
        {
            // Arrange
            var fcmbConfig = new FCMBConfig
            {
                BaseUrl = "Your BaseUrl",
                ClientId = "Your ClientId",
                SecretKey = "Your SecretKey",
                FrontendBaseUrl = "Your FrontendBaseUrl",
                SubscriptionKey = "Your SubscriptionKey",
                Password = "Your Password",
                SLAPeriod = "Your SLAPeriod",
                CurrencyCode = "Your CurrencyCode",
                LegalSearchAmount = "Your LegalSearchAmount",
                LegalSearchRemarks = "Your LegalSearchRemarks",
                LegalSearchPaymentRemarks = "Your LegalSearchPaymentRemarks",
                LegalSearchReasonCode = "Your LegalSearchReasonCode",
                AuthConfig = new AuthConfig
                {
                    AuthUrl = "Your AuthUrl",
                    AuthClientId = "Your AuthClientId",
                    AuthSecretKey = "Your AuthSecretKey"
                },
                EmailConfig = new EmailConfig
                {
                    EmailUrl = "Your EmailUrl",
                    SenderEmail = "Your SenderEmail",
                    SenderName = "Your SenderName"
                }
            };

            var options = Options.Create(fcmbConfig);

            var request = new SolicitorOnboardRequest() 
            { 
                FirstName = "test",
                Email = "test@example.com",
                BankAccount = "0123456789",
                Firm = new FirmRequest 
                {
                    Address = "test address",
                    Name = "sample firm"
                }
            }; 

            Region region = new Region() { Name = "South West" };
            var userEmail = "user@example.com"; // Specify the user's email
            var user = new User() { FirstName = "test", Email = userEmail }; 
            var userManager = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                null, null, null, null, null, null, null, null);

            // Mock the UserManager's FindByEmailAsync method
            userManager.Setup(um => um.FindByEmailAsync(request.Email))
                .ReturnsAsync((User)null); 

            // Mock RoleManager
            var roleName = nameof(RoleType.Solicitor); 
            var role = new Role(roleName); 
            var roleManager = new Mock<RoleManager<Role>>(
                new Mock<IRoleStore<Role>>().Object, null, null, null, null);
            roleManager.Setup(rm => rm.FindByNameAsync(roleName))
                .ReturnsAsync(role); 

            // Mock the UserManager's CreateAsync method to simulate successful user creation
            userManager.Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            userManager.Setup(um => um.AddToRoleAsync(user, roleName))
                .ReturnsAsync(IdentityResult.Success);

            userManager.Setup(x => x.GenerateUserTokenAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("1234");

            // Mock the StateRetrieveService to return a state
            var stateRetrieveService = new Mock<IStateRetrieveService>();
            stateRetrieveService.Setup(srs => srs.GetStateById(request.Firm.StateId))
                .ReturnsAsync(new State() { Name = "Lagos", Region = region });

            // mock email service
            var emailMock = new Mock<IEmailService>();

            // Mock the StateRetrieveService to return a state of coverage
            var stateOfCoverageRetrieveService = new Mock<IStateRetrieveService>();
            stateOfCoverageRetrieveService.Setup(srs => srs.GetStateById(request.Firm.StateOfCoverageId))
                .ReturnsAsync(new State() { Name = "Ogun", Region = region });

            // Mock other services and dependencies as needed

            // Create an instance of GeneralAuthService with the mocked services
            var authService = new GeneralAuthService(
                userManager.Object,
                roleManager.Object,
                null,
                stateRetrieveService.Object,
                null,
                null,
                null,
                emailMock.Object,
                options);

            // Act
            var response = await authService.OnboardSolicitorAsync(request);

            // Assert
            Assert.Equal(ResponseCodes.Success, response.Code);
        }

        [Fact]
        public async Task NotifyUserOfLockedOutStatus_WithValidUser_ShouldReturnResponse()
        {
            // Arrange
            var fcmbConfig = new FCMBConfig
            {
                BaseUrl = "Your BaseUrl",
                ClientId = "Your ClientId",
                SecretKey = "Your SecretKey",
                FrontendBaseUrl = "Your FrontendBaseUrl",
                SubscriptionKey = "Your SubscriptionKey",
                Password = "Your Password",
                SLAPeriod = "Your SLAPeriod",
                CurrencyCode = "Your CurrencyCode",
                LegalSearchAmount = "Your LegalSearchAmount",
                LegalSearchRemarks = "Your LegalSearchRemarks",
                LegalSearchPaymentRemarks = "Your LegalSearchPaymentRemarks",
                LegalSearchReasonCode = "Your LegalSearchReasonCode",
                AuthConfig = new AuthConfig
                {
                    AuthUrl = "Your AuthUrl",
                    AuthClientId = "Your AuthClientId",
                    AuthSecretKey = "Your AuthSecretKey"
                },
                EmailConfig = new EmailConfig
                {
                    EmailUrl = "Your EmailUrl",
                    SenderEmail = "Your SenderEmail",
                    SenderName = "Your SenderName"
                }
            };

            var options = Options.Create(fcmbConfig);

            var userEmail = "user@example.com"; // Specify the user's email
            var user = new User() { FirstName = "test", Email = userEmail };

            // Mock your email service
            var emailService = new Mock<IEmailService>();
            emailService
                .Setup(es => es.SendEmailAsync(It.IsAny<SendEmailRequest>()))
                .ReturnsAsync(true); 

            // Create an instance of GeneralAuthService with the mocked services
            var authService = new GeneralAuthService(
                null, 
                null,
                null,
                null,
                null,
                null,
                null,
                emailService.Object,
                options);

            // Act
            var response = await authService.NotifyUserOfLockedOutStatus(user);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(ResponseCodes.LockedOutUser, response.Code);
        }

        [Fact]
        public async Task IsUserLockedOut_WithValidUser_ShouldReturnTrue()
        {
            // Arrange
            var fcmbConfig = new FCMBConfig
            {
                BaseUrl = "Your BaseUrl",
                ClientId = "Your ClientId",
                SecretKey = "Your SecretKey",
                FrontendBaseUrl = "Your FrontendBaseUrl",
                SubscriptionKey = "Your SubscriptionKey",
                Password = "Your Password",
                SLAPeriod = "Your SLAPeriod",
                CurrencyCode = "Your CurrencyCode",
                LegalSearchAmount = "Your LegalSearchAmount",
                LegalSearchRemarks = "Your LegalSearchRemarks",
                LegalSearchPaymentRemarks = "Your LegalSearchPaymentRemarks",
                LegalSearchReasonCode = "Your LegalSearchReasonCode",
                AuthConfig = new AuthConfig
                {
                    AuthUrl = "Your AuthUrl",
                    AuthClientId = "Your AuthClientId",
                    AuthSecretKey = "Your AuthSecretKey"
                },
                EmailConfig = new EmailConfig
                {
                    EmailUrl = "Your EmailUrl",
                    SenderEmail = "Your SenderEmail",
                    SenderName = "Your SenderName"
                }
            };

            var options = Options.Create(fcmbConfig);

            var email = "testuser@example.com";
            var user = new User() { FirstName = "test", Email = email };

            // Mock UserManager
            var userManager = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                null, null, null, null, null, null, null, null);

            var emailService = new Mock<IEmailService>();
            emailService
                .Setup(es => es.SendEmailAsync(It.IsAny<SendEmailRequest>()))
                .ReturnsAsync(true);

            userManager.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync(user); // Simulate finding the user

            userManager.Setup(um => um.IsLockedOutAsync(user))
                .ReturnsAsync(true); // Simulate user being locked out

            // Create an instance of GeneralAuthService with the mocked services
            var authService = new GeneralAuthService(
                userManager.Object,
                null, // Mock other dependencies as needed
                null,
                null,
                null,
                null,
                null,
                emailService.Object,
                options);

            // Act
            bool isLockedOut = await authService.IsUserLockedOut(email);

            // Assert
            Assert.True(isLockedOut);
        }

        [Fact]
        public async Task Generate2faTokenForSolicitor_WithValidUserAndRole_ShouldReturnResponse()
        {
            // Arrange
            var fcmbConfig = new FCMBConfig
            {
                BaseUrl = "Your BaseUrl",
                ClientId = "Your ClientId",
                SecretKey = "Your SecretKey",
                FrontendBaseUrl = "Your FrontendBaseUrl",
                SubscriptionKey = "Your SubscriptionKey",
                Password = "Your Password",
                SLAPeriod = "Your SLAPeriod",
                CurrencyCode = "Your CurrencyCode",
                LegalSearchAmount = "Your LegalSearchAmount",
                LegalSearchRemarks = "Your LegalSearchRemarks",
                LegalSearchPaymentRemarks = "Your LegalSearchPaymentRemarks",
                LegalSearchReasonCode = "Your LegalSearchReasonCode",
                AuthConfig = new AuthConfig
                {
                    AuthUrl = "Your AuthUrl",
                    AuthClientId = "Your AuthClientId",
                    AuthSecretKey = "Your AuthSecretKey"
                },
                EmailConfig = new EmailConfig
                {
                    EmailUrl = "Your EmailUrl",
                    SenderEmail = "Your SenderEmail",
                    SenderName = "Your SenderName"
                }
            };

            var options = Options.Create(fcmbConfig);

            var userEmail = "user@example.com"; // Specify the user's email
            var user = new User() { FirstName = "test", Email = userEmail };
            var role = "Solicitor";

            // Mock UserManager
            var userManager = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                null, null, null, null, null, null, null, null);

            userManager.Setup(um => um.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider))
                .ReturnsAsync("123456"); // Simulate generating a 2FA token

            userManager.Setup(um => um.SetAuthenticationTokenAsync(user, TokenOptions.DefaultPhoneProvider, "2fa", "123456"))
                .ReturnsAsync(IdentityResult.Success); // Simulate setting the 2FA token

            // Mock your email service
            var emailService = new Mock<IEmailService>();
            emailService
                .Setup(es => es.SendEmailAsync(It.IsAny<SendEmailRequest>()))
                .ReturnsAsync(true); 

            // Create an instance of GeneralAuthService with the mocked services
            var authService = new GeneralAuthService(
                userManager.Object,
                null, // Mock other dependencies as needed
                null,
                null,
                null,
                null,
                null,
                emailService.Object,
                options);

            // Act
            var response = await authService.Generate2faTokenForSolicitor(user, role);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(ResponseCodes.Success, response.Code);
        }

        [Fact]
        public async Task UserLogin_WithValidCredentials_ShouldGenerate2faTokenForSolicitor()
        {
            // Arrange
            var fcmbConfig = new FCMBConfig
            {
                BaseUrl = "Your BaseUrl",
                ClientId = "Your ClientId",
                SecretKey = "Your SecretKey",
                FrontendBaseUrl = "Your FrontendBaseUrl",
                SubscriptionKey = "Your SubscriptionKey",
                Password = "Your Password",
                SLAPeriod = "Your SLAPeriod",
                CurrencyCode = "Your CurrencyCode",
                LegalSearchAmount = "Your LegalSearchAmount",
                LegalSearchRemarks = "Your LegalSearchRemarks",
                LegalSearchPaymentRemarks = "Your LegalSearchPaymentRemarks",
                LegalSearchReasonCode = "Your LegalSearchReasonCode",
                AuthConfig = new AuthConfig
                {
                    AuthUrl = "Your AuthUrl",
                    AuthClientId = "Your AuthClientId",
                    AuthSecretKey = "Your AuthSecretKey"
                },
                EmailConfig = new EmailConfig
                {
                    EmailUrl = "Your EmailUrl",
                    SenderEmail = "Your SenderEmail",
                    SenderName = "Your SenderName"
                }
            };

            var options = Options.Create(fcmbConfig);

            var email = "testuser@example.com";
            var password = "testpassword";
            var user = new User { Email = email, FirstName = "test", OnboardingStatus = OnboardingStatusType.Completed };

            // Mock UserManager
            var userManager = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                null, null, null, null, null, null, null, null);

            // Mock your email service
            var emailService = new Mock<IEmailService>();
            emailService
                .Setup(es => es.SendEmailAsync(It.IsAny<SendEmailRequest>()))
                .ReturnsAsync(true);

            userManager.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync(user);

            userManager.Setup(um => um.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            userManager.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { nameof(RoleType.Solicitor) });

            userManager.Setup(um => um.GenerateTwoFactorTokenAsync(user, It.IsAny<string>()))
                .ReturnsAsync("123456");

            userManager.Setup(um => um.SetAuthenticationTokenAsync(user, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            // Mock other dependencies, e.g., the JWT token service

            // Create an instance of GeneralAuthService with the mocked services
            var authService = new GeneralAuthService(
                userManager.Object,
                null, // Mock other dependencies as needed
                null,
                null,
                null,
                null,
                null,
                emailService.Object,
                options);

            var request = new LoginRequest { Email = email, Password = password };

            // Act
            var response = await authService.UserLogin(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(ResponseCodes.Success, response.Code);
        }

        [Fact]
        public async Task StaffLoginFlow_WithValidStaffUserAndRole_ShouldReturnResponse()
        {
            // Arrange
            var fcmbConfig = new FCMBConfig
            {
                BaseUrl = "Your BaseUrl",
                ClientId = "Your ClientId",
                SecretKey = "Your SecretKey",
                FrontendBaseUrl = "Your FrontendBaseUrl",
                SubscriptionKey = "Your SubscriptionKey",
                Password = "Your Password",
                SLAPeriod = "Your SLAPeriod",
                CurrencyCode = "Your CurrencyCode",
                LegalSearchAmount = "Your LegalSearchAmount",
                LegalSearchRemarks = "Your LegalSearchRemarks",
                LegalSearchPaymentRemarks = "Your LegalSearchPaymentRemarks",
                LegalSearchReasonCode = "Your LegalSearchReasonCode",
                AuthConfig = new AuthConfig
                {
                    AuthUrl = "Your AuthUrl",
                    AuthClientId = "Your AuthClientId",
                    AuthSecretKey = "Your AuthSecretKey"
                },
                EmailConfig = new EmailConfig
                {
                    EmailUrl = "Your EmailUrl",
                    SenderEmail = "Your SenderEmail",
                    SenderName = "Your SenderName"
                }
            };

            var solId = "061";
            var branch = new Branch() { SolId = solId, Address = "sample address" };

            var branchServiceMock = new Mock<IBranchRetrieveService>();
            branchServiceMock.Setup(x => x.GetBranchBySolId(It.IsAny<string>())).ReturnsAsync(branch);


            var options = Options.Create(fcmbConfig);

            var jwtServiceMock = new Mock<IJwtTokenService>();
            var loggerMock = new Mock<ILogger<GeneralAuthService>>();

            var email = "staff@example.com";
            var role = new List<string> { "StaffRole" };
            var user = new User { Email = email, FirstName = "test", OnboardingStatus = OnboardingStatusType.Completed };

            // Mock UserManager
            var userManager = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                null, null, null, null, null, null, null, null);

            userManager.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync(user);

            userManager.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(role);

            // Mock other dependencies, e.g., the JWT token service

            // Create an instance of GeneralAuthService with the mocked services
            var authService = new GeneralAuthService(
                userManager.Object,
                null, // Mock other dependencies as needed
                jwtServiceMock.Object,
                null,
                null,
                loggerMock.Object,
                branchServiceMock.Object,
                null,
                options);

            var request = new LoginRequest { Email = email, Password = "staffpassword" };

            // Act
            var response = await authService.StaffLoginFlow(user, role, request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(ResponseCodes.Success, response.Code);
        }
    }
}
